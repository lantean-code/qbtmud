using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Moq;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using QBittorrent.ApiClient;
using ReadmeScreenshots;

namespace ReadmeScreenshots.Test
{
    public sealed class ScreenshotLoginTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_ProductionLoginWithoutTestIds_WHEN_AuthenticationRequired_THEN_ShouldFillActualInputsAndSubmit(bool loginRoute)
        {
            TestIdHelper.For("Username").Should().BeNull();
            await using var testContext = new BunitContext();
            testContext.JSInterop.Mode = JSRuntimeMode.Loose;
            testContext.Services.AddMudServices(options => options.PopoverOptions.CheckForPopoverProvider = false);
            var apiClient = new Mock<IApiClient>(MockBehavior.Strict);
            apiClient.Setup(client => client.LoginAsync("Username", "Password")).ReturnsAsync(ApiResult.CreateSuccess());
            testContext.Services.AddSingleton(apiClient.Object);
            var localizer = new Mock<ILanguageLocalizer>();
            localizer.Setup(service => service.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] _) => source);
            testContext.Services.AddSingleton(localizer.Object);
            var navigationManager = testContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo(loginRoute ? "http://localhost/#/login" : "http://localhost/#/");
            var target = testContext.Render<Login>();
            var fields = target.FindComponents<MudTextField<string>>();
            var usernameField = fields.Single(field => field.Instance.GetState(component => component.InputId) == "username");
            var passwordField = fields.Single(field => field.Instance.GetState(component => component.InputId) == "password");
            var submitButton = target.FindComponents<MudButton>().Single(button => button.Instance.ButtonType == ButtonType.Submit);
            var form = target.FindComponent<EditForm>();
            var page = new Mock<IPage>(MockBehavior.Strict);
            page.SetupGet(component => component.Url).Returns(() => navigationManager.Uri);
            var usernameLocator = new Mock<ILocator>(MockBehavior.Strict);
            var passwordLocator = new Mock<ILocator>(MockBehavior.Strict);
            var submitLocator = new Mock<ILocator>(MockBehavior.Strict);
            var usernameSelector = $"#{usernameField.Instance.GetState(field => field.InputId)}";
            var passwordSelector = $"#{passwordField.Instance.GetState(field => field.InputId)}";
            var submitSelector = $"form button[type='{submitButton.Instance.ButtonType.ToString().ToLowerInvariant()}']";
            page.Setup(component => component.Locator(usernameSelector, null)).Returns(usernameLocator.Object);
            page.Setup(component => component.Locator(passwordSelector, null)).Returns(passwordLocator.Object);
            page.Setup(component => component.Locator(submitSelector, null)).Returns(submitLocator.Object);
            usernameLocator.Setup(locator => locator.WaitForAsync(It.Is<LocatorWaitForOptions>(options => options.State == WaitForSelectorState.Visible)))
                .Returns(Task.CompletedTask);
            usernameLocator.Setup(locator => locator.FillAsync("Username", null))
                .Returns(() => target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("Username")));
            passwordLocator.Setup(locator => locator.FillAsync("Password", null))
                .Returns(() => target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("Password")));
            submitLocator.Setup(locator => locator.ClickAsync(null))
                .Returns(() => target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync()));
            page.Setup(component => component.WaitForFunctionAsync(It.IsAny<string>(), null, null)).ReturnsAsync(Mock.Of<IJSHandle>());
            page.Setup(component => component.WaitForLoadStateAsync(LoadState.NetworkIdle, null)).Returns(Task.CompletedTask);
            page.Setup(component => component.GotoAsync("http://localhost/#/", It.Is<PageGotoOptions>(options => options.WaitUntil == WaitUntilState.NetworkIdle)))
                .ReturnsAsync((IResponse?)null);

            await ScreenshotLogin.EnsureLoggedInAsync(page.Object, "http://localhost", "Username", "Password");

            TestIdHelper.For("Password").Should().BeNull();
            apiClient.Verify(client => client.LoginAsync("Username", "Password"), Times.Once);
            navigationManager.Uri.Should().Be("http://localhost/");
            page.Verify(component => component.GotoAsync("http://localhost/#/", It.IsAny<PageGotoOptions>()), Times.Once);
            usernameLocator.Verify(locator => locator.WaitForAsync(It.IsAny<LocatorWaitForOptions>()), loginRoute ? Times.Never() : Times.Once());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_AuthenticatedPage_WHEN_LoginInputUnavailable_THEN_ShouldSkipLogin(bool playwrightFailure)
        {
            var page = new Mock<IPage>(MockBehavior.Strict);
            var username = new Mock<ILocator>(MockBehavior.Strict);
            page.SetupGet(component => component.Url).Returns("http://localhost/#/");
            page.Setup(component => component.Locator("#username", null)).Returns(username.Object);
            var wait = username.Setup(locator => locator.WaitForAsync(It.IsAny<LocatorWaitForOptions>()));
            if (playwrightFailure)
            {
                wait.ThrowsAsync(new PlaywrightException("No login form"));
            }
            else
            {
                wait.ThrowsAsync(new TimeoutException("No login form"));
            }

            await ScreenshotLogin.EnsureLoggedInAsync(page.Object, "http://localhost", "Username", "Password");

            username.Verify(locator => locator.FillAsync(It.IsAny<string>(), It.IsAny<LocatorFillOptions>()), Times.Never);
            page.Verify(component => component.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_LoginRouteRemainsAfterSubmission_WHEN_LoginCompletes_THEN_ShouldReportFailure()
        {
            var page = new Mock<IPage>(MockBehavior.Strict);
            var field = new Mock<ILocator>(MockBehavior.Strict);
            page.SetupGet(component => component.Url).Returns("http://localhost/#/login");
            page.Setup(component => component.Locator(It.IsAny<string>(), null)).Returns(field.Object);
            field.Setup(locator => locator.FillAsync(It.IsAny<string>(), null)).Returns(Task.CompletedTask);
            field.Setup(locator => locator.ClickAsync(null)).Returns(Task.CompletedTask);
            page.Setup(component => component.WaitForFunctionAsync(It.IsAny<string>(), null, null)).ReturnsAsync(Mock.Of<IJSHandle>());
            page.Setup(component => component.WaitForLoadStateAsync(LoadState.NetworkIdle, null)).Returns(Task.CompletedTask);

            var action = () => ScreenshotLogin.EnsureLoggedInAsync(page.Object, "http://localhost", "Username", "Password");

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Playwright login did not complete.");
            page.Verify(component => component.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>()), Times.Never);
        }
    }
}
