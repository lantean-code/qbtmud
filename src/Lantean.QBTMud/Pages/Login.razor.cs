using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
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

        protected bool IsSubmitting { get; set; }

        protected InputType PasswordInputType => IsPasswordVisible ? InputType.Text : InputType.Password;

        protected string PasswordAdornmentIcon => IsPasswordVisible ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;

        private bool IsPasswordVisible { get; set; }

        protected async Task LoginClick(EditContext context)
        {
            ApiError = null;
            IsSubmitting = true;

            try
            {
                await DoLogin(Model.Username, Model.Password);
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        protected Task TogglePasswordVisibility(MouseEventArgs args)
        {
            IsPasswordVisible = !IsPasswordVisible;
            return Task.CompletedTask;
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
                ApiError = WebUiLocalizer.Translate("Login", "Unable to log in, server is probably unreachable.");
            }
            catch
            {
                ApiError = WebUiLocalizer.Translate("Login", "Unable to log in, server is probably unreachable.");
            }
        }
    }
}
