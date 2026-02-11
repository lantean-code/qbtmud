using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class CookiePropertiesDialog
    {
        private static readonly string[] ExpirationFormats =
        [
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss"
        ];

        private bool _initialized;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Parameter]
        public ApplicationCookie? Cookie { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        protected string? Domain { get; set; }

        protected string? Path { get; set; }

        protected string? Name { get; set; }

        protected string? Value { get; set; }

        protected string? ExpirationInput { get; set; }

        protected override void OnParametersSet()
        {
            if (_initialized)
            {
                return;
            }

            Domain = Cookie?.Domain;
            Path = Cookie?.Path;
            Name = Cookie?.Name;
            Value = Cookie?.Value;
            ExpirationInput = FormatExpiration(Cookie?.ExpirationDate);
            _initialized = true;
        }

        protected IEnumerable<string> ValidateExpiration(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            if (DateTime.TryParseExact(value, ExpirationFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _))
            {
                return [];
            }

            return [Translate("Expiration date must be a valid date and time.")];
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return;
            }

            if (!TryGetExpirationDate(ExpirationInput, out var expirationDate))
            {
                return;
            }

            var cookie = new ApplicationCookie(
                Name.Trim(),
                Normalize(Domain),
                Normalize(Path),
                Value,
                expirationDate);

            MudDialog.Close(DialogResult.Ok(cookie));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }

        private static string? FormatExpiration(long? expirationDate)
        {
            if (expirationDate is null || expirationDate <= 0)
            {
                return null;
            }

            return DateTimeOffset.FromUnixTimeSeconds(expirationDate.Value)
                .LocalDateTime
                .ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static bool TryGetExpirationDate(string? input, out long? expirationDate)
        {
            expirationDate = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                return true;
            }

            if (!DateTime.TryParseExact(input, ExpirationFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var localDateTime))
            {
                return false;
            }

            var localOffset = new DateTimeOffset(localDateTime, DateTimeOffset.Now.Offset);
            expirationDate = localOffset.ToUnixTimeSeconds();
            return true;
        }

        private string Translate(string value, params object[] args)
        {
            return WebUiLocalizer.Translate("AppCookies", value, args);
        }
    }
}
