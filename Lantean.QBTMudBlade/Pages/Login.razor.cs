using Lantean.QBitTorrentClient;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Login
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected LoginModel Model { get; set; } = new LoginModel();

        protected string? ApiError { get; set; }

        protected async Task LoginClick(EditContext context)
        {
            await DoLogin(Model.Username, Model.Password);
        }

        private async Task DoLogin(string username, string password)
        {
            try
            {
                await ApiClient.Login(username, password);

                NavigationManager.NavigateTo("/");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ApiError = "Invalid username or password.";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ApiError = "Requests from this client are currently unavailable.";
            }
            catch
            {
                ApiError = "Unable to communicate with the qBittorrent API.";
            }
        }

#if DEBUG
        protected override async Task OnInitializedAsync()
        {
            await DoLogin("admin", "u4FR4ZQCm");
        }
#endif
    }

    public class LoginModel
    {
        [Required]
        [NotNull]
        public string? Username { get; set; }

        [Required]
        [NotNull]
        public string? Password { get; set; }
    }
}