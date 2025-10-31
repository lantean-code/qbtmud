using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Pages
{
    public partial class Cookies
    {
        private static readonly string[] ExpirationFormats =
        [
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss"
        ];

        private readonly List<CookieEntry> _cookies = [];
        private bool _isLoading;
        private bool _isSaving;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected IReadOnlyList<CookieEntry> CookieEntries => _cookies;

        protected bool IsBusy => _isLoading || _isSaving;

        protected bool HasCookies => _cookies.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadCookiesAsync();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task Reload()
        {
            await LoadCookiesAsync();
        }

        protected void AddCookie()
        {
            if (IsBusy)
            {
                return;
            }

            _cookies.Add(new CookieEntry());
        }

        protected void RemoveCookie(CookieEntry entry)
        {
            if (IsBusy)
            {
                return;
            }

            _cookies.Remove(entry);
        }

        protected void ClearAll()
        {
            if (IsBusy)
            {
                return;
            }

            _cookies.Clear();
        }

        protected async Task Save()
        {
            if (_isSaving)
            {
                return;
            }

            if (_cookies.Any(c => string.IsNullOrWhiteSpace(c.Name)))
            {
                Snackbar?.Add("Cookie name is required.", Severity.Warning);
                return;
            }

            List<ApplicationCookie> cookiesToSave;
            try
            {
                cookiesToSave = _cookies.Select(TransformToApplicationCookie).ToList();
            }
            catch (FormatException exception)
            {
                Snackbar?.Add(exception.Message, Severity.Warning);
                return;
            }

            _isSaving = true;
            try
            {
                await ApiClient.SetApplicationCookies(cookiesToSave);
                Snackbar?.Add("Cookies saved.", Severity.Success);
                await LoadCookiesAsync();
            }
            catch (HttpRequestException exception)
            {
                Snackbar?.Add($"Unable to save cookies: {exception.Message}", Severity.Error);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task LoadCookiesAsync()
        {
            _isLoading = true;
            _cookies.Clear();

            try
            {
                var cookies = await ApiClient.GetApplicationCookies();
                foreach (var cookie in cookies.OrderBy(c => c.Domain, StringComparer.OrdinalIgnoreCase)
                                              .ThenBy(c => c.Path, StringComparer.OrdinalIgnoreCase)
                                              .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                {
                    _cookies.Add(CreateEntry(cookie));
                }
            }
            catch (HttpRequestException exception)
            {
                Snackbar?.Add($"Unable to load cookies: {exception.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        private static CookieEntry CreateEntry(ApplicationCookie cookie)
        {
            var expiration = cookie.ExpirationDate.GetValueOrDefault();
            string? expirationInput = null;
            if (expiration > 0)
            {
                var local = DateTimeOffset.FromUnixTimeSeconds(expiration).LocalDateTime;
                expirationInput = local.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            }

            return new CookieEntry
            {
                Domain = cookie.Domain,
                Path = cookie.Path,
                Name = cookie.Name,
                Value = cookie.Value,
                ExpirationInput = expirationInput
            };
        }

        private static ApplicationCookie TransformToApplicationCookie(CookieEntry entry)
        {
            long? expirationSeconds = null;
            if (!string.IsNullOrWhiteSpace(entry.ExpirationInput))
            {
                if (!DateTime.TryParseExact(entry.ExpirationInput, ExpirationFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var localDateTime))
                {
                    throw new FormatException("Expiration date must be a valid date and time.");
                }

                var offset = new DateTimeOffset(localDateTime, DateTimeOffset.Now.Offset);
                expirationSeconds = offset.ToUnixTimeSeconds();
            }

            return new ApplicationCookie(
                entry.Name!.Trim(),
                string.IsNullOrWhiteSpace(entry.Domain) ? null : entry.Domain.Trim(),
                string.IsNullOrWhiteSpace(entry.Path) ? null : entry.Path.Trim(),
                entry.Value,
                expirationSeconds);
        }

        public sealed class CookieEntry
        {
            public CookieEntry()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; }

            public string? Domain { get; set; }

            public string? Path { get; set; }

            public string? Name { get; set; }

            public string? Value { get; set; }

            public string? ExpirationInput { get; set; }
        }
    }
}
