using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class CookiesTests : RazorComponentTestBase<Cookies>
    {
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly IDialogWorkflow _dialogWorkflow;

        public CookiesTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_dialogWorkflow);
        }

        [Fact]
        public void GIVEN_CookiesLoaded_WHEN_Rendered_THEN_SortsEntries()
        {
            var cookies = new[]
            {
                new ApplicationCookie("b", "b.com", "/b", "value", null),
                new ApplicationCookie("b", "a.com", "/a", "value", 0),
                new ApplicationCookie("a", "a.com", "/b", "value", null)
            };

            var target = RenderPage(cookies);
            var markup = target.Markup;

            var domainAIndex = markup.IndexOf("a.com", StringComparison.Ordinal);
            var domainBIndex = markup.LastIndexOf("b.com", StringComparison.Ordinal);

            domainAIndex.Should().BeGreaterThan(-1);
            domainBIndex.Should().BeGreaterThan(-1);
            domainAIndex.Should().BeLessThan(domainBIndex);
        }

        [Fact]
        public void GIVEN_LoadCookiesFails_WHEN_Initialized_THEN_ShowsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .ThrowsAsync(new HttpRequestException("Failure"));

            RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to load cookies. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_DialogCanceled_THEN_SkipsSave()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Add Cookie", null))
                .ReturnsAsync((ApplicationCookie?)null);

            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_DialogReturnsCookie_THEN_SavesAndReloads()
        {
            var initialCookies = new[]
            {
                new ApplicationCookie("Existing", "example.com", "/", "Value", null)
            };
            var addedCookie = new ApplicationCookie("Added", "new.example", "/new", "AddedValue", null);

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(initialCookies)
                .ReturnsAsync(new[] { initialCookies[0], addedCookie });
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(Task.CompletedTask);

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Add Cookie", null))
                .ReturnsAsync(addedCookie);

            var target = RenderPage(initialCookies, configureApi: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(
                    It.Is<IEnumerable<ApplicationCookie>>(cookies => HasExistingAndAddedCookies(cookies))),
                Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_EditClicked_WHEN_DialogReturnsCookie_THEN_SavesAndReloads()
        {
            var existingCookie = new ApplicationCookie("Existing", "example.com", "/", "Value", null);
            var updatedCookie = new ApplicationCookie("Updated", "updated.example", "/updated", "UpdatedValue", 5);

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(new[] { existingCookie })
                .ReturnsAsync(new[] { updatedCookie });

            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Edit Cookie", It.IsAny<ApplicationCookie>()))
                .ReturnsAsync(updatedCookie);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new[] { existingCookie }, configureApi: false);
            var editButton = FindRowEditButton(target);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowCookiePropertiesDialog(
                    "Edit Cookie",
                    It.Is<ApplicationCookie?>(cookie => ReferenceEquals(cookie, existingCookie))),
                Times.Once);
            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(
                    It.Is<IEnumerable<ApplicationCookie>>(cookies => HasOnlyCookie(cookies, updatedCookie))),
                Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_EditClicked_WHEN_DialogCanceled_THEN_SkipsSave()
        {
            var existingCookie = new ApplicationCookie("Existing", "example.com", "/", "Value", null);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Edit Cookie", It.IsAny<ApplicationCookie>()))
                .ReturnsAsync((ApplicationCookie?)null);

            var target = RenderPage(new[] { existingCookie });
            var editButton = FindRowEditButton(target);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteClicked_WHEN_Invoked_THEN_RemovesCookieAndReloads()
        {
            var existingCookie = new ApplicationCookie("Existing", "example.com", "/", "Value", null);
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(new[] { existingCookie })
                .ReturnsAsync(Array.Empty<ApplicationCookie>());
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new[] { existingCookie }, configureApi: false);
            var deleteButton = FindRowDeleteButton(target);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(
                    It.Is<IEnumerable<ApplicationCookie>>(cookies => !cookies.Any())),
                Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_MultipleCookies_WHEN_DeleteClicked_THEN_PreservesRemainingCookie()
        {
            var cookieA = new ApplicationCookie("A", "a.example", "/", "ValueA", null);
            var cookieB = new ApplicationCookie("B", "b.example", "/", "ValueB", null);
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(new[] { cookieA, cookieB })
                .ReturnsAsync(new[] { cookieB });
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(new[] { cookieA, cookieB }, configureApi: false);
            var deleteButton = FindRowDeleteButton(target);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(
                    It.Is<IEnumerable<ApplicationCookie>>(cookies =>
                        cookies.Count() == 1
                        && cookies.Single().Name == "B")),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshClicked_WHEN_Invoked_THEN_LoadsCookiesAgain()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(Array.Empty<ApplicationCookie>())
                .ReturnsAsync(Array.Empty<ApplicationCookie>());

            var target = RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_UpdateFails_WHEN_AddConfirmed_THEN_ShowsError()
        {
            var newCookie = new ApplicationCookie("Name", "Domain", "/", "Value", null);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Add Cookie", null))
                .ReturnsAsync(newCookie);
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to update cookies. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveInProgress_WHEN_DeleteClickedAgain_THEN_SkipsSecondUpdate()
        {
            var existingCookie = new ApplicationCookie("Existing", "example.com", "/", "Value", null);
            var pendingSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .ReturnsAsync(new[] { existingCookie });
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(pendingSave.Task);

            var target = RenderPage(new[] { existingCookie }, configureApi: false);
            var deleteButton = FindRowDeleteButton(target);

            var deleteTask = target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => deleteButton.Instance.Disabled.Should().BeTrue());
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Once);

            pendingSave.SetResult();
            await deleteTask;
        }

        [Fact]
        public async Task GIVEN_LoadInProgress_WHEN_AddClicked_THEN_SkipsAdd()
        {
            var pendingLoad = new TaskCompletionSource<IReadOnlyList<ApplicationCookie>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .Returns(pendingLoad.Task);

            var target = RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowCookiePropertiesDialog(It.IsAny<string>(), It.IsAny<ApplicationCookie?>()), Times.Never);

            pendingLoad.SetResult(Array.Empty<ApplicationCookie>());
            target.WaitForAssertion(() =>
            {
                addButton.Instance.Disabled.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_LoadInProgress_WHEN_ReloadClicked_THEN_SkipsSecondLoad()
        {
            var pendingLoad = new TaskCompletionSource<IReadOnlyList<ApplicationCookie>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .Returns(pendingLoad.Task);

            var target = RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Once);

            pendingLoad.SetResult(Array.Empty<ApplicationCookie>());
            target.WaitForAssertion(() =>
            {
                refreshButton.Instance.Disabled.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_SaveInProgress_WHEN_EditClicked_THEN_SkipsEdit()
        {
            var existingCookie = new ApplicationCookie("Existing", "example.com", "/", "Value", null);
            var pendingSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .ReturnsAsync(new[] { existingCookie });
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(pendingSave.Task);

            var target = RenderPage(new[] { existingCookie }, configureApi: false);
            var deleteButton = FindRowDeleteButton(target);
            var editButton = FindRowEditButton(target);

            var deleteTask = target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                editButton.Instance.Disabled.Should().BeTrue();
            });

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowCookiePropertiesDialog(It.IsAny<string>(), It.IsAny<ApplicationCookie?>()), Times.Never);
            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Once);

            pendingSave.SetResult();
            await deleteTask;
        }

        [Fact]
        public async Task GIVEN_ReloadInProgressAfterAddDialog_WHEN_AddResumes_THEN_SkipsPersist()
        {
            var pendingReload = new TaskCompletionSource<IReadOnlyList<ApplicationCookie>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pendingDialog = new TaskCompletionSource<ApplicationCookie?>(TaskCreationOptions.RunContinuationsAsynchronously);

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(Array.Empty<ApplicationCookie>())
                .Returns(pendingReload.Task);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowCookiePropertiesDialog("Add Cookie", null))
                .Returns(pendingDialog.Task);

            var target = RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            var addTask = target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());
            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogWorkflow).Verify(workflow => workflow.ShowCookiePropertiesDialog("Add Cookie", null), Times.Once);
            });

            var reloadTask = target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());
            target.WaitForAssertion(() =>
            {
                refreshButton.Instance.Disabled.Should().BeTrue();
            });

            pendingDialog.SetResult(new ApplicationCookie("Added", "example.com", "/", "Value", null));
            await addTask;

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Never);

            pendingReload.SetResult(Array.Empty<ApplicationCookie>());
            await reloadTask;
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_Invoked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/cookies");

            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var backButton = FindIconButton(target, Icons.Material.Outlined.NavigateBefore);

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        private IRenderedComponent<Cookies> RenderPage(IReadOnlyList<ApplicationCookie> cookies, bool configureApi = true)
        {
            if (configureApi)
            {
                Mock.Get(_apiClient)
                    .Setup(client => client.GetApplicationCookies())
                    .ReturnsAsync(cookies);
            }

            return TestContext.Render<Cookies>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });
        }

        private static IRenderedComponent<MudIconButton> FindRowEditButton(IRenderedComponent<Cookies> component)
        {
            return component.FindComponents<MudIconButton>().First(button => button.Instance.Icon == Icons.Material.Filled.Edit);
        }

        private static IRenderedComponent<MudIconButton> FindRowDeleteButton(IRenderedComponent<Cookies> component)
        {
            return component.FindComponents<MudIconButton>().First(button => button.Instance.Icon == Icons.Material.Filled.Delete);
        }

        private static bool HasExistingAndAddedCookies(IEnumerable<ApplicationCookie> cookies)
        {
            var list = cookies.ToList();
            return list.Count == 2
                   && list.Any(cookie => cookie.Name == "Existing")
                   && list.Any(cookie => cookie.Name == "Added");
        }

        private static bool HasOnlyCookie(IEnumerable<ApplicationCookie> cookies, ApplicationCookie expectedCookie)
        {
            var list = cookies.ToList();
            return list.Count == 1 && list[0].Equals(expectedCookie);
        }
    }
}
