using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Pages
{
    public partial class Login
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

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
            var loginResult = await ApiClient.LoginAsync(username, password);
            if (loginResult.IsSuccess)
            {
                NavigationManager.NavigateToHome();
                return;
            }

            var failure = loginResult.Failure;

            if (failure?.TryGetReason<LoginFailureReason>(out var reason) == true
                && reason == LoginFailureReason.InvalidCredentials)
            {
                ApiError = LanguageLocalizer.Translate("Login", "Invalid Username or Password.");
                return;
            }

            if ((failure?.Kind == ApiFailureKind.AccessDenied) && !string.IsNullOrWhiteSpace(failure.UserMessage))
            {
                ApiError = failure.UserMessage;
                return;
            }

            if (failure?.Kind is ApiFailureKind.NoResponse or ApiFailureKind.Timeout or ApiFailureKind.ServerError or ApiFailureKind.UnexpectedResponse)
            {
                ApiError = LanguageLocalizer.Translate("Login", "Unable to log in, server is probably unreachable.");
                return;
            }

            if (failure is not null && !string.IsNullOrWhiteSpace(failure.UserMessage))
            {
                ApiError = failure.UserMessage;
                return;
            }

            ApiError = LanguageLocalizer.Translate("Login", "Unable to log in, server is probably unreachable.");
        }
    }
}
