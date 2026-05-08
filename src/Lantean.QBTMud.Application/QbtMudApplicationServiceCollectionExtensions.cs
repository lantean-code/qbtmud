using Lantean.QBTMud.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lantean.QBTMud.Application
{
    /// <summary>
    /// Service registration extensions for qbtmud application services.
    /// </summary>
    public static class QbtMudApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds qbtmud application services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddQbtMudApplication(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddScoped<IAppSettingsService, AppSettingsService>();
            services.TryAddScoped<IAppSettingsStateService, AppSettingsStateService>();
            services.TryAddScoped<ITorrentQueryState, TorrentQueryState>();
            services.TryAddScoped<IQBittorrentPreferencesStateService, QBittorrentPreferencesStateService>();
            services.TryAddScoped<IWelcomeWizardStateService, WelcomeWizardStateService>();
            services.TryAddScoped<IWelcomeWizardPlanBuilder, WelcomeWizardPlanBuilder>();
            services.TryAddScoped<IStorageDiagnosticsService, StorageDiagnosticsService>();
            services.TryAddScoped<IStorageRoutingService, StorageRoutingService>();
            services.TryAddScoped<ISettingsStorageService, SettingsStorageService>();
            services.TryAddScoped<ISpeedHistoryService, SpeedHistoryService>();

            services.TryAddSingleton<IStorageCatalogService, StorageCatalogService>();
            services.TryAddSingleton<ITorrentDataManager, TorrentDataManager>();
            services.TryAddSingleton<IPeerDataManager, PeerDataManager>();
            services.TryAddSingleton<IPreferencesDataManager, PreferencesDataManager>();
            services.TryAddSingleton<IRssDataManager, RssDataManager>();
            services.TryAddSingleton<IPeriodicTimerFactory, PeriodicTimerFactory>();
            services.TryAddSingleton<IManagedTimerRegistry, ManagedTimerRegistry>();
            services.TryAddSingleton<IManagedTimerFactory, ManagedTimerFactory>();
            services.TryAddScoped<ITorrentCompletionNotificationService, TorrentCompletionNotificationService>();

            return services;
        }
    }
}
