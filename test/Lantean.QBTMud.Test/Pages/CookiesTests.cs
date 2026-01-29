using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class CookiesTests : RazorComponentTestBase<Cookies>
    {
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;

        public CookiesTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll(typeof(ISnackbar));
            TestContext.Services.AddSingleton(_snackbar);
        }

        [Fact]
        public void GIVEN_CookiesLoaded_WHEN_Rendered_THEN_SortsAndFormatsEntries()
        {
            var expirationSeconds = new DateTimeOffset(2020, 1, 2, 3, 4, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var cookies = new[]
            {
                new ApplicationCookie("b", "b.com", "/b", "value", null),
                new ApplicationCookie("b", "a.com", "/a", "value", expirationSeconds),
                new ApplicationCookie("a", "a.com", "/b", "value", null)
            };

            var target = RenderPage(cookies);

            var table = target.FindComponent<MudTable<Cookies.CookieEntry>>();
            var items = table.Instance.Items.Should().NotBeNull().And.Subject!.ToList();

            items[0].Domain.Should().Be("a.com");
            items[0].Path.Should().Be("/a");
            items[0].Name.Should().Be("b");
            items[1].Domain.Should().Be("a.com");
            items[1].Path.Should().Be("/b");
            items[1].Name.Should().Be("a");
            items[2].Domain.Should().Be("b.com");
            items[2].Path.Should().Be("/b");
            items[2].Name.Should().Be("b");

            var expectedExpiration = DateTimeOffset.FromUnixTimeSeconds(expirationSeconds).LocalDateTime
                .ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            items[0].ExpirationInput.Should().Be(expectedExpiration);
            items[0].Id.Should().NotBe(Guid.Empty);
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
        public async Task GIVEN_AddClicked_WHEN_Invoked_THEN_AddsEntry()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var table = target.FindComponent<MudTable<Cookies.CookieEntry>>();
            table.Instance.Items.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_RemoveClicked_WHEN_Invoked_THEN_RemovesEntry()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var deleteButton = FindRowDeleteButton(target);
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            var table = target.FindComponent<MudTable<Cookies.CookieEntry>>();
            table.Instance.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ClearAllClicked_WHEN_Invoked_THEN_ClearsEntries()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var clearButton = FindIconButton(target, Icons.Material.Filled.DeleteSweep);
            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            var table = target.FindComponent<MudTable<Cookies.CookieEntry>>();
            table.Instance.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SaveWithMissingName_WHEN_Clicked_THEN_ShowsWarningAndSkipsSave()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Cookie name is required.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveWithInvalidExpiration_WHEN_Clicked_THEN_ShowsWarningAndSkipsSave()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var nameField = FindNameField(target);
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Name"));

            var expirationField = FindExpirationField(target);
            await target.InvokeAsync(() => expirationField.Instance.ValueChanged.InvokeAsync("Invalid"));

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Expiration date must be a valid date and time.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveSuccess_WHEN_Clicked_THEN_SavesAndReloads()
        {
            var cookies = Array.Empty<ApplicationCookie>();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(cookies)
                .ReturnsAsync(cookies);

            List<ApplicationCookie> savedCookies = [];
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Callback<IEnumerable<ApplicationCookie>>(entries => savedCookies = entries.ToList())
                .Returns(Task.CompletedTask);

            var target = RenderPage(cookies, configureApi: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            await SetFieldValue(target, field => field.Placeholder == "example.com", " Domain ");
            await SetFieldValue(target, field => field.Placeholder == "/", " /path ");
            await SetFieldValue(target, field => field.Required, " Name ");
            await SetFieldValue(target, field => field.InputType == InputType.DateTimeLocal, "2020-01-02T03:04");

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            savedCookies.Should().ContainSingle();
            savedCookies[0].Name.Should().Be("Name");
            savedCookies[0].Domain.Should().Be("Domain");
            savedCookies[0].Path.Should().Be("/path");
            savedCookies[0].Value.Should().BeNull();

            var expectedDate = DateTime.ParseExact("2020-01-02T03:04", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            var expectedOffset = new DateTimeOffset(expectedDate, DateTimeOffset.Now.Offset).ToUnixTimeSeconds();
            savedCookies[0].ExpirationDate.Should().Be(expectedOffset);

            Mock.Get(_apiClient).Verify(client => client.GetApplicationCookies(), Times.Exactly(2));
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Cookies saved.", Severity.Success, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveWithEmptyDomainAndPath_WHEN_Clicked_THEN_SavesNullValues()
        {
            var cookies = Array.Empty<ApplicationCookie>();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(cookies)
                .ReturnsAsync(cookies);

            List<ApplicationCookie> savedCookies = [];
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Callback<IEnumerable<ApplicationCookie>>(entries => savedCookies = entries.ToList())
                .Returns(Task.CompletedTask);

            var target = RenderPage(cookies, configureApi: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            await SetFieldValue(target, field => field.Placeholder == "example.com", " ");
            await SetFieldValue(target, field => field.Placeholder == "/", " ");
            await SetFieldValue(target, field => field.Required, "Name");
            await SetFieldValue(target, field => field.InputType == InputType.DateTimeLocal, string.Empty);

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            savedCookies.Should().ContainSingle();
            savedCookies[0].Name.Should().Be("Name");
            savedCookies[0].Domain.Should().BeNull();
            savedCookies[0].Path.Should().BeNull();
            savedCookies[0].ExpirationDate.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_SaveFails_WHEN_Clicked_THEN_ShowsError()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var nameField = FindNameField(target);
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Name"));

            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to save cookies. Please try again.", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveThrowsUnexpectedException_WHEN_Clicked_THEN_Throws()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var nameField = FindNameField(target);
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Name"));

            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);

            Func<Task> act = () => target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Failure");
        }

        [Fact]
        public async Task GIVEN_ReloadClicked_WHEN_Invoked_THEN_LoadsCookiesAgain()
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
        public void GIVEN_LoadCookiesThrowsUnexpectedException_WHEN_Rendered_THEN_Throws()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationCookies())
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Action act = () => RenderPage(Array.Empty<ApplicationCookie>(), configureApi: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("Failure");
        }

        [Fact]
        public async Task GIVEN_SaveInProgress_WHEN_AddRemoveClearInvoked_THEN_NoChanges()
        {
            var pendingSave = new TaskCompletionSource();
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(pendingSave.Task);

            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var nameField = FindNameField(target);
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Name"));

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            var saveTask = target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());
            target.WaitForAssertion(() => saveButton.Instance.Disabled.Should().BeTrue());
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => saveButton.Instance.Disabled.Should().BeTrue());

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());
            var deleteButton = FindRowDeleteButton(target);
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());
            var clearButton = FindIconButton(target, Icons.Material.Filled.DeleteSweep);
            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            var table = target.FindComponent<MudTable<Cookies.CookieEntry>>();
            table.Instance.Items.Should().ContainSingle();

            pendingSave.SetResult();
            await saveTask;

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SaveInProgress_WHEN_SaveInvokedAgain_THEN_SkipsSecondSave()
        {
            var pendingSave = new TaskCompletionSource();
            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(pendingSave.Task);

            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            var nameField = FindNameField(target);
            await target.InvokeAsync(() => nameField.Instance.ValueChanged.InvokeAsync("Name"));

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            var saveTask = target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()), Times.Once);

            pendingSave.SetResult();
            await saveTask;
        }

        [Fact]
        public void GIVEN_NoCookies_WHEN_Rendered_THEN_DisablesSaveAndClear()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());

            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);
            var clearButton = FindIconButton(target, Icons.Material.Filled.DeleteSweep);

            saveButton.Instance.Disabled.Should().BeTrue();
            clearButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoCookies_WHEN_SaveInvoked_THEN_SavesEmptyList()
        {
            var cookies = Array.Empty<ApplicationCookie>();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetApplicationCookies())
                .ReturnsAsync(cookies)
                .ReturnsAsync(cookies);

            Mock.Get(_apiClient)
                .Setup(client => client.SetApplicationCookies(It.IsAny<IEnumerable<ApplicationCookie>>()))
                .Returns(Task.CompletedTask);

            var target = RenderPage(cookies, configureApi: false);
            var saveButton = FindIconButton(target, Icons.Material.Filled.Save);

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.SetApplicationCookies(It.Is<IEnumerable<ApplicationCookie>>(entries => !entries.Any())),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ExpirationValidation_WHEN_Evaluated_THEN_ReturnsExpectedResults()
        {
            var target = RenderPage(Array.Empty<ApplicationCookie>());
            var addButton = FindIconButton(target, Icons.Material.Filled.Add);
            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());
            var expirationField = FindExpirationField(target);
            var validation = expirationField.Instance.Validation as Func<string, IEnumerable<string>>;

            validation.Should().NotBeNull();

            var empty = validation!(string.Empty);
            empty.Should().BeEmpty();

            var valid = validation("2020-01-02T03:04");
            valid.Should().BeEmpty();

            Action act = () => validation("invalid").ToList();
            act.Should().Throw<FormatException>().WithMessage("Expiration date must be a valid date and time.");
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

        private static IRenderedComponent<MudIconButton> FindIconButton(IRenderedComponent<Cookies> component, string icon)
        {
            return component.FindComponents<MudIconButton>().Single(button => button.Instance.Icon == icon);
        }

        private static IRenderedComponent<MudIconButton> FindRowDeleteButton(IRenderedComponent<Cookies> component)
        {
            return component.FindComponents<MudIconButton>().First(button => button.Instance.Icon == Icons.Material.Filled.Delete);
        }

        private static IRenderedComponent<MudTextField<string>> FindNameField(IRenderedComponent<Cookies> component)
        {
            return component.FindComponents<MudTextField<string>>().Single(field => field.Instance.Required);
        }

        private static IRenderedComponent<MudTextField<string>> FindExpirationField(IRenderedComponent<Cookies> component)
        {
            return component.FindComponents<MudTextField<string>>().Single(field => field.Instance.InputType == InputType.DateTimeLocal);
        }

        private static async Task SetFieldValue(IRenderedComponent<Cookies> component, Func<MudTextField<string>, bool> predicate, string value)
        {
            var field = component.FindComponents<MudTextField<string>>().Single(f => predicate(f.Instance));
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }
    }
}
