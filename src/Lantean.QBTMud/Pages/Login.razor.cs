using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;

namespace Lantean.QBTMud.Pages
{
    public partial class Login
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        protected LoginForm Model { get; set; } = new LoginForm();

        protected string? ApiError { get; set; }

        protected Task LoginClick(EditContext context)
        {
            return DoLogin(Model.Username, Model.Password);
        }

        private async Task DoLogin(string username, string password)
        {
            try
            {
                await ApiClient.Login(username, password);

                NavigationManager.NavigateToHome();
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
            {
                ApiError = WebUiLocalizer.Translate("Login", "Invalid Username or Password.");
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
            {
                ApiError = "Requests from this client are currently unavailable.";
            }
            catch
            {
                ApiError = WebUiLocalizer.Translate("Login", "Unable to log in, server is probably unreachable.");
            }
        }
    }
}
