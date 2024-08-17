using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Login
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

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

                NavigationManager.NavigateTo("/");
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.BadRequest)
            {
                ApiError = "Invalid username or password.";
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
            {
                ApiError = "Requests from this client are currently unavailable.";
            }
            catch
            {
                ApiError = "Unable to communicate with the qBittorrent API.";
            }
        }

#if DEBUG
        protected override Task OnInitializedAsync()
        {
            return DoLogin("admin", "V9VpmhCvv");
        }
#endif
    }
}