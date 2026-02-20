using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Pages
{
    public partial class Cookies
    {
        private const string ActionsColumnHeader = "Actions";

        private readonly Dictionary<string, RenderFragment<RowContext<CookieRow>>> _columnRenderFragments = [];
        private readonly List<CookieRow> _cookies = [];
        private bool _isBusy;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected DynamicTable<CookieRow>? Table { get; set; }

        protected bool IsBusy
        {
            get { return _isBusy; }
        }

        protected bool HasCookies
        {
            get { return _cookies.Count > 0; }
        }

        protected IReadOnlyList<CookieRow> CookieRows
        {
            get { return _cookies; }
        }

        protected IEnumerable<ColumnDefinition<CookieRow>> Columns
        {
            get { return GetColumnDefinitions(); }
        }

        public Cookies()
        {
            _columnRenderFragments.Add(ActionsColumnHeader, ActionsColumn);
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadCookiesAsync();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task Reload()
        {
            await LoadCookiesAsync();
        }

        protected async Task AddCookie()
        {
            if (_isBusy)
            {
                return;
            }

            var title = LanguageLocalizer.Translate("CookiesDialog", "Add Cookie");
            var cookie = await DialogWorkflow.ShowCookiePropertiesDialog(title, null);
            if (cookie is null)
            {
                return;
            }

            var nextCookies = _cookies.Select(row => row.Cookie).Append(cookie).ToList();
            await PersistCookiesAsync(nextCookies);
        }

        protected async Task EditCookie(CookieRow row)
        {
            if (_isBusy)
            {
                return;
            }

            var updatedCookie = await DialogWorkflow.ShowCookiePropertiesDialog(Translate("Edit Cookie"), row.Cookie);
            if (updatedCookie is null)
            {
                return;
            }

            var nextCookies = _cookies
                .Select(cookie => cookie.Id == row.Id ? updatedCookie : cookie.Cookie)
                .ToList();

            await PersistCookiesAsync(nextCookies);
        }

        protected async Task DeleteCookie(CookieRow row)
        {
            if (_isBusy)
            {
                return;
            }

            var nextCookies = _cookies
                .Where(cookie => cookie.Id != row.Id)
                .Select(cookie => cookie.Cookie)
                .ToList();

            await PersistCookiesAsync(nextCookies);
        }

        private async Task LoadCookiesAsync()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                await LoadCookiesCoreAsync();
            }
            catch (HttpRequestException)
            {
                SnackbarWorkflow.ShowTransientMessage(Translate("Unable to load cookies. Please try again."), Severity.Error);
            }
            finally
            {
                _isBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task PersistCookiesAsync(IEnumerable<ApplicationCookie> nextCookies)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                await ApiClient.SetApplicationCookies(nextCookies);
                await LoadCookiesCoreAsync();
            }
            catch (HttpRequestException)
            {
                SnackbarWorkflow.ShowTransientMessage(Translate("Unable to update cookies. Please try again."), Severity.Error);
            }
            finally
            {
                _isBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadCookiesCoreAsync()
        {
            _cookies.Clear();

            var cookies = await ApiClient.GetApplicationCookies();
            foreach (var cookie in cookies.OrderBy(c => c.Domain, StringComparer.OrdinalIgnoreCase)
                                          .ThenBy(c => c.Path, StringComparer.OrdinalIgnoreCase)
                                          .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                _cookies.Add(new CookieRow(Guid.NewGuid(), cookie));
            }
        }

        private IEnumerable<ColumnDefinition<CookieRow>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Header, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                if (string.Equals(columnDefinition.Header, ActionsColumnHeader, StringComparison.Ordinal))
                {
                    columnDefinition.DisplayHeader = Translate("Actions");
                }

                yield return columnDefinition;
            }
        }

        private List<ColumnDefinition<CookieRow>> ColumnsDefinitions
        {
            get { return BuildColumnsDefinitions(); }
        }

        private List<ColumnDefinition<CookieRow>> BuildColumnsDefinitions()
        {
            return
            [
                new ColumnDefinition<CookieRow>(LanguageLocalizer.Translate("CookiesDialog", "Domain"), row => row.Cookie.Domain, id: "domain"),
                new ColumnDefinition<CookieRow>(LanguageLocalizer.Translate("CookiesDialog", "Path"), row => row.Cookie.Path, id: "path"),
                new ColumnDefinition<CookieRow>(LanguageLocalizer.Translate("CookiesDialog", "Name"), row => row.Cookie.Name, id: "name"),
                new ColumnDefinition<CookieRow>(LanguageLocalizer.Translate("CookiesDialog", "Value"), row => row.Cookie.Value, id: "value"),
                new ColumnDefinition<CookieRow>(LanguageLocalizer.Translate("CookiesDialog", "Expiration Date"), row => row.Cookie.ExpirationDate, row => GetExpirationDateText(row.Cookie.ExpirationDate), id: "expiration_date"),
                new ColumnDefinition<CookieRow>(ActionsColumnHeader, row => row, id: "actions")
            ];
        }

        private static string GetExpirationDateText(long? expirationDate)
        {
            if (expirationDate is null || expirationDate <= 0)
            {
                return string.Empty;
            }

            var dateTime = DateTimeOffset.FromUnixTimeSeconds(expirationDate.Value).LocalDateTime;
            return dateTime.ToString("g", CultureInfo.CurrentCulture);
        }

        private string Translate(string value, params object[] args)
        {
            return LanguageLocalizer.Translate("AppCookies", value, args);
        }

        protected sealed class CookieRow
        {
            public CookieRow(Guid id, ApplicationCookie cookie)
            {
                Id = id;
                Cookie = cookie;
            }

            public Guid Id { get; }

            public ApplicationCookie Cookie { get; }
        }
    }
}
