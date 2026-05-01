using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.BrowserCapabilities;
using Lantean.QBTMud.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;

namespace Lantean.QBTMud
{
    /// <summary>
    /// Service registration extensions for qbtmud presentation services.
    /// </summary>
    public static class QbtMudPresentationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds qbtmud presentation services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddQbtMudPresentation(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddMudServices();
            services.AddBrowserCapabilities();

            services.TryAddScoped<IDialogWorkflow, DialogWorkflow>();
            services.TryAddScoped<IAppWarmupService, AppWarmupService>();
            services.TryAddScoped<ILostConnectionWorkflow, LostConnectionWorkflow>();
            services.TryAddScoped<IShellSessionWorkflow, ShellSessionWorkflow>();
            services.TryAddScoped<IPendingDownloadWorkflow, PendingDownloadWorkflow>();
            services.TryAddScoped<IStartupExperienceWorkflow, StartupExperienceWorkflow>();
            services.TryAddScoped<IStatusBarWorkflow, StatusBarWorkflow>();
            services.TryAddScoped<IApiFeedbackWorkflow, ApiFeedbackWorkflow>();
            services.TryAddScoped<ISnackbarWorkflow, SnackbarWorkflow>();

            return services;
        }
    }
}
