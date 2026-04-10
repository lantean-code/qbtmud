using System.Net;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class LoginTests : RazorComponentTestBase<Login>
    {
        private readonly IApiClient _apiClient;

        public LoginTests()
        {
            _apiClient = Mock.Of<IApiClient>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
        }

        [Fact]
        public async Task GIVEN_ValidCredentials_WHEN_Submitted_THEN_LogsInAndNavigatesHome()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoginAsync("Username", "Password"))
                .Returns(Task.CompletedTask);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/login");
            var target = RenderPage();

            await SetCredentials(target, "Username", "Password");

            var form = target.FindComponent<EditForm>();

            await target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.LoginAsync("Username", "Password"), Times.Once);
            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_LegacyInvalidCredentialsResponse_WHEN_Submitted_THEN_ShowsInvalidCredentialsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoginAsync("Username", "Password"))
                .ReturnsFailure(ApiFailureKind.AuthenticationRejected, "Failure", HttpStatusCode.BadRequest, LoginFailureReason.InvalidCredentials);

            var target = RenderPage();
            await SetCredentials(target, "Username", "Password");

            var form = target.FindComponent<EditForm>();

            await target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Invalid Username or Password.");
            });
        }

        [Fact]
        public async Task GIVEN_UnauthorizedResponse_WHEN_Submitted_THEN_ShowsInvalidCredentialsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoginAsync("Username", "Password"))
                .ReturnsFailure(ApiFailureKind.AuthenticationRejected, "Failure", HttpStatusCode.Unauthorized, LoginFailureReason.InvalidCredentials);

            var target = RenderPage();
            await SetCredentials(target, "Username", "Password");

            var form = target.FindComponent<EditForm>();

            await target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Invalid Username or Password.");
            });
        }

        [Fact]
        public async Task GIVEN_ForbiddenResponseWithMessage_WHEN_Submitted_THEN_ShowsServerMessage()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoginAsync("Username", "Password"))
                .ReturnsFailure(ApiFailureKind.AccessDenied, "Your IP address has been banned after too many failed authentication attempts.", HttpStatusCode.Forbidden, LoginFailureReason.BannedClient);

            var target = RenderPage();
            await SetCredentials(target, "Username", "Password");

            var form = target.FindComponent<EditForm>();

            await target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Your IP address has been banned after too many failed authentication attempts.");
            });
        }

        [Fact]
        public async Task GIVEN_UnexpectedFailure_WHEN_Submitted_THEN_ShowsGenericError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoginAsync("Username", "Password"))
                .ReturnsFailure(ApiFailureKind.UnexpectedResponse, "Failure");

            var target = RenderPage();
            await SetCredentials(target, "Username", "Password");

            var form = target.FindComponent<EditForm>();

            await target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Unable to log in, server is probably unreachable.");
            });
        }

        [Fact]
        public void GIVEN_NoError_WHEN_Rendered_THEN_HidesAlert()
        {
            var target = RenderPage();

            target.FindComponents<MudAlert>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_HiddenPassword_WHEN_ToggleVisibilityClicked_THEN_ShouldTogglePasswordInputType()
        {
            var target = RenderPage();
            var passwordField = FindComponentByTestId<MudTextField<string>>(target, "Password");

            passwordField.Instance.InputType.Should().Be(InputType.Password);

            await target.InvokeAsync(() => passwordField.Instance.OnAdornmentClick.InvokeAsync(new MouseEventArgs()));

            passwordField.Instance.InputType.Should().Be(InputType.Text);

            await target.InvokeAsync(() => passwordField.Instance.OnAdornmentClick.InvokeAsync(new MouseEventArgs()));

            passwordField.Instance.InputType.Should().Be(InputType.Password);
        }

        private IRenderedComponent<Login> RenderPage()
        {
            return TestContext.Render<Login>();
        }

        private static async Task SetCredentials(IRenderedComponent<Login> target, string username, string password)
        {
            var usernameField = FindComponentByTestId<MudTextField<string>>(target, "Username");
            var passwordField = FindComponentByTestId<MudTextField<string>>(target, "Password");

            await target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync(username));
            await target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync(password));
        }
    }
}
