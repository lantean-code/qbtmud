using System.Diagnostics;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class PwaAppSettingsTab
    {
        [Parameter]
        public bool IsActive { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Inject]
        protected IPwaInstallPromptService PwaInstallPromptService { get; set; } = default!;

        [Inject]
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

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

        protected Task ShowInstallPromptTestAsync()
        {
#if DEBUG
            return ShowInstallPromptTestCoreAsync();
#else
            return Task.CompletedTask;
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

        private string TranslatePwa(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppPwaInstallPrompt", source, arguments);
        }

#if DEBUG

        private async Task ShowInstallPromptTestCoreAsync()
        {
            try
            {
                await SettingsStorage.RemoveItemAsync(PwaInstallPromptStorageKeys.Dismissed);
                PwaState = await PwaInstallPromptService.ShowInstallPromptTestAsync();
                HasLoadedPwaStatus = true;
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
        }

#endif
    }
}
