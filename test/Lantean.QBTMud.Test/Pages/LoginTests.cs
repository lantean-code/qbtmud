using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class LoginTests : RazorComponentTestBase<Login>
    {
        private readonly IApiClient _apiClient;
        private readonly IRenderedComponent<Login> _target;

        public LoginTests()
        {
            _apiClient = Mock.Of<IApiClient>();

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);

            _target = RenderPage();
        }

        [Fact]
        public async Task GIVEN_ValidCredentials_WHEN_Submitted_THEN_LogsInAndNavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/login");

            await SetCredentials(_target, "Username", "Password");

            var form = _target.FindComponent<EditForm>();

            await _target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.Login("Username", "Password"), Times.Once);
            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_InvalidCredentials_WHEN_Submitted_THEN_ShowsInvalidCredentialsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.Login("Username", "Password"))
                .ThrowsAsync(new HttpRequestException("Failure", null, HttpStatusCode.BadRequest));

            await SetCredentials(_target, "Username", "Password");

            var form = _target.FindComponent<EditForm>();

            await _target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            _target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(_target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Invalid Username or Password.");
            });
        }

        [Fact]
        public async Task GIVEN_ForbiddenResponse_WHEN_Submitted_THEN_ShowsForbiddenError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.Login("Username", "Password"))
                .ThrowsAsync(new HttpRequestException("Failure", null, HttpStatusCode.Forbidden));

            await SetCredentials(_target, "Username", "Password");

            var form = _target.FindComponent<EditForm>();

            await _target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            _target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(_target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Requests from this client are currently unavailable.");
            });
        }

        [Fact]
        public async Task GIVEN_UnexpectedFailure_WHEN_Submitted_THEN_ShowsGenericError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.Login("Username", "Password"))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            await SetCredentials(_target, "Username", "Password");

            var form = _target.FindComponent<EditForm>();

            await _target.InvokeAsync(() => form.Instance.OnValidSubmit.InvokeAsync());

            _target.WaitForAssertion(() =>
            {
                var alert = FindComponentByTestId<MudAlert>(_target, "LoginError");
                GetChildContentText(alert.Instance.ChildContent).Should().Be("Unable to log in, server is probably unreachable.");
            });
        }

        [Fact]
        public void GIVEN_NoError_WHEN_Rendered_THEN_HidesAlert()
        {
            _target.FindComponents<MudAlert>().Should().BeEmpty();
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
