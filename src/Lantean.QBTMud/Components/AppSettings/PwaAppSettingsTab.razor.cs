using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class PwaAppSettingsTab
    {
        private const string _pwaInstallTestSnackbarClass = "pwa-install-snackbar";
        private const string _pwaInstallTestSnackbarKey = "pwa-install-snackbar-test";

        [Parameter]
        public bool IsActive { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected IPwaInstallPromptService PwaInstallPromptService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        protected bool IsLoadingPwaStatus { get; private set; }

        protected bool IsRequestingPwaInstall { get; private set; }

        protected bool HasLoadedPwaStatus { get; private set; }

        protected PwaInstallPromptState PwaState { get; private set; } = new();

        protected bool CanRequestPwaInstall => PwaState.CanPrompt && !PwaState.IsInstalled;

        protected bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        private bool _wasActive;
        private int _loadedReloadToken = -1;

        protected override async Task OnParametersSetAsync()
        {
            var shouldRefresh = IsActive && (!_wasActive || _loadedReloadToken != ReloadToken);
            _wasActive = IsActive;

            if (!shouldRefresh)
            {
                return;
            }

            _loadedReloadToken = ReloadToken;
            await RefreshPwaStatusAsync();
        }

        protected async Task RefreshPwaStatusAsync()
        {
            if (IsLoadingPwaStatus)
            {
                return;
            }

            IsLoadingPwaStatus = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                PwaState = await PwaInstallPromptService.GetInstallPromptStateAsync();
                HasLoadedPwaStatus = true;
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslatePwa("Unable to read app install status."), Severity.Warning);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslatePwa("Unable to read app install status."), Severity.Warning);
            }
            finally
            {
                IsLoadingPwaStatus = false;
            }
        }

        protected async Task RequestPwaInstallAsync()
        {
            if (IsRequestingPwaInstall || !CanRequestPwaInstall)
            {
                return;
            }

            IsRequestingPwaInstall = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var outcome = await PwaInstallPromptService.RequestInstallPromptAsync();
                SnackbarWorkflow.ShowTransientMessage(TranslatePwa("Install prompt result: %1", outcome), Severity.Info);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslatePwa("Unable to request app install."), Severity.Warning);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslatePwa("Unable to request app install."), Severity.Warning);
            }
            finally
            {
                IsRequestingPwaInstall = false;
                await RefreshPwaStatusAsync();
            }
        }

        protected void ShowInstallSnackbarTest()
        {
#if DEBUG
            var componentParameters = new Dictionary<string, object>
            {
                [nameof(PwaInstallPromptSnackbarContent.CanPromptInstall)] = true,
                [nameof(PwaInstallPromptSnackbarContent.ShowIosInstructions)] = false,
                [nameof(PwaInstallPromptSnackbarContent.OnInstallClicked)] = EventCallback.Factory.Create<MouseEventArgs>(this, RequestPwaInstallAsync),
                [nameof(PwaInstallPromptSnackbarContent.OnDismissClicked)] = EventCallback.Factory.Create<MouseEventArgs>(this, DismissInstallSnackbarTestAsync)
            };

            SnackbarWorkflow.ShowComponent<PwaInstallPromptSnackbarContent>(
                componentParameters,
                Severity.Normal,
                options =>
                {
                    options.RequireInteraction = true;
                    options.ShowCloseIcon = false;
                    options.HideIcon = true;
                    options.SnackbarVariant = Variant.Outlined;
                    options.SnackbarTypeClass = _pwaInstallTestSnackbarClass;
                },
                _pwaInstallTestSnackbarKey);
#endif
        }

        private string GetPwaStatusText()
        {
            if (!HasLoadedPwaStatus)
            {
                return TranslatePwa("Unknown");
            }

            return PwaState.IsInstalled
                ? TranslatePwa("Installed")
                : TranslatePwa("Not installed");
        }

        private string GetPwaPromptStatusText()
        {
            if (!HasLoadedPwaStatus)
            {
                return TranslatePwa("Unknown");
            }

            return CanRequestPwaInstall
                ? TranslatePwa("Install prompt available")
                : TranslatePwa("Install prompt unavailable");
        }

        private string GetPwaPlatformText()
        {
            if (!HasLoadedPwaStatus)
            {
                return TranslatePwa("Unknown");
            }

            return PwaState.IsIos
                ? TranslatePwa("iOS browser")
                : TranslatePwa("Non-iOS browser");
        }

        private Task DismissInstallSnackbarTestAsync()
        {
            SnackbarWorkflow.Hide(_pwaInstallTestSnackbarKey);
            return Task.CompletedTask;
        }

        private string TranslatePwa(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppPwaInstallPrompt", source, arguments);
        }
    }
}
