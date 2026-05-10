using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ThemePreviewDialog : IAsyncDisposable
    {
        private const string _previewScope = "root .theme-preview-scope";
        private static readonly KeyboardEvent _arrowLeftKey = new("ArrowLeft");
        private static readonly KeyboardEvent _arrowRightKey = new("ArrowRight");

        private MudTheme _previewTheme = new();
        private int _selectedIndex;
        private bool _isDarkMode;
        private bool _initialized;
        private bool _shortcutsRegistered;
        private bool _disposedValue;

        [CascadingParameter]
        protected IMudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public ThemePreviewDialogRequest Request { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        protected MudTheme PreviewTheme
        {
            get { return _previewTheme; }
        }

        protected ThemePreviewDialogItem CurrentItem
        {
            get { return Request.Items[_selectedIndex]; }
        }

        protected bool IsCatalogueMode
        {
            get { return Request.Mode == ThemePreviewDialogMode.Catalogue; }
        }

        protected bool CanGoPrevious
        {
            get { return IsCatalogueMode && _selectedIndex > 0; }
        }

        protected bool CanGoNext
        {
            get { return IsCatalogueMode && _selectedIndex < Request.Items.Count - 1; }
        }

        protected bool CanApplyCurrentTheme
        {
            get
            {
                return IsCatalogueMode
                    && Request.ApplyThemeAsync is not null
                    && !string.Equals(CurrentItem.ThemeId, Request.CurrentThemeId, StringComparison.Ordinal);
            }
        }

        protected bool CanSaveAndApply
        {
            get { return !IsCatalogueMode && Request.SaveAndApplyThemeAsync is not null && Request.CanSaveAndApply; }
        }

        protected override void OnParametersSet()
        {
            if (!_initialized)
            {
                ArgumentNullException.ThrowIfNull(Request);

                _selectedIndex = FindSelectedIndex();
                _isDarkMode = Request.IsDarkMode;
                _initialized = true;
            }

            _previewTheme = BuildPreviewTheme(CurrentItem.Theme);
            _previewTheme.PseudoCss.Scope = _previewScope;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KeyboardService.RegisterKeypressEvent(_arrowLeftKey, HandleArrowLeftAsync);
                await KeyboardService.RegisterKeypressEvent(_arrowRightKey, HandleArrowRightAsync);
                await KeyboardService.Focus();
                _shortcutsRegistered = true;
            }
        }

        protected string DarkModeIcon
        {
            get { return _isDarkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode; }
        }

        protected string DarkModeTooltip
        {
            get { return _isDarkMode ? Translate("Switch to light preview") : Translate("Switch to dark preview"); }
        }

        protected void Close()
        {
            MudDialog.Close();
        }

        protected void ToggleDarkMode()
        {
            _isDarkMode = !_isDarkMode;
        }

        protected Task ShowPreviousTheme()
        {
            if (!CanGoPrevious)
            {
                return Task.CompletedTask;
            }

            _selectedIndex--;
            _previewTheme = BuildPreviewTheme(CurrentItem.Theme);
            _previewTheme.PseudoCss.Scope = _previewScope;
            return InvokeAsync(StateHasChanged);
        }

        protected Task ShowNextTheme()
        {
            if (!CanGoNext)
            {
                return Task.CompletedTask;
            }

            _selectedIndex++;
            _previewTheme = BuildPreviewTheme(CurrentItem.Theme);
            _previewTheme.PseudoCss.Scope = _previewScope;
            return InvokeAsync(StateHasChanged);
        }

        protected async Task ApplyCurrentTheme()
        {
            if (!CanApplyCurrentTheme)
            {
                return;
            }

            var applied = await Request.ApplyThemeAsync!(CurrentItem.ThemeId);
            if (applied)
            {
                MudDialog.Close();
            }
        }

        protected async Task SaveAndApplyCurrentTheme()
        {
            if (!CanSaveAndApply)
            {
                return;
            }

            var applied = await Request.SaveAndApplyThemeAsync!();
            if (applied)
            {
                MudDialog.Close();
            }
        }

        private Task HandleNavClick(MouseEventArgs args)
        {
            return Task.CompletedTask;
        }

        private Task HandleArrowLeftAsync(KeyboardEvent keyboardEvent)
        {
            return ShowPreviousTheme();
        }

        private Task HandleArrowRightAsync(KeyboardEvent keyboardEvent)
        {
            return ShowNextTheme();
        }

        private int FindSelectedIndex()
        {
            if (Request.Items.Count == 0)
            {
                throw new InvalidOperationException("The theme preview dialog requires at least one preview item.");
            }

            var selectedIndex = Request.Items
                .Select((item, index) => new { item.ThemeId, index })
                .FirstOrDefault(entry => string.Equals(entry.ThemeId, Request.SelectedThemeId, StringComparison.Ordinal))
                ?.index;

            return selectedIndex ?? 0;
        }

        private static MudTheme BuildPreviewTheme(MudTheme? theme)
        {
            return ThemeSerialization.CloneTheme(theme);
        }

        private string Translate(string value)
        {
            return LanguageLocalizer.Translate("AppThemePreviewDialog", value);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposedValue)
            {
                return;
            }

            if (_shortcutsRegistered)
            {
                await KeyboardService.UnregisterKeypressEvent(_arrowLeftKey);
                await KeyboardService.UnregisterKeypressEvent(_arrowRightKey);
                await KeyboardService.UnFocus();
                _shortcutsRegistered = false;
            }

            _disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }
}
