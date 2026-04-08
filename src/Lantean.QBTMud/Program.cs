using Lantean.QBTMud.Configuration;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var routingMode = RoutingModeConfiguration.GetRoutingMode(builder.Configuration);
            if (routingMode == RoutingMode.Hash)
            {
                builder.Services.AddHashRouting();
            }

            builder.Services.AddSingleton(typeof(RoutingMode), routingMode);
            builder.Services.AddMudServices();
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddOptions<WebUiLocalizationOptions>();
            builder.Services.AddHttpClient("WebUiAssets", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
            builder.Services.AddScoped<ILanguageFileLoader, LanguageFileLoader>();
            builder.Services.AddScoped<IAssemblyResourceAccessor, AssemblyResourceAccessor>();
            builder.Services.AddScoped<ILanguageEmbeddedResourceLoader, LanguageEmbeddedResourceLoader>();
            builder.Services.AddScoped<ILanguageResourceProvider, LanguageResourceProvider>();
            builder.Services.AddScoped<ILanguageResourceLoader, LanguageResourceLoader>();
            builder.Services.AddScoped<ILanguageLocalizer, LanguageLocalizer>();
            builder.Services.AddScoped<ILanguageCatalog, LanguageCatalog>();

            var applicationBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            Uri defaultApiHostBaseAddress;
#if DEBUG
#pragma warning disable S1075 // URIs should not be hardcoded - used for debugging only
            defaultApiHostBaseAddress = new Uri("http://localhost:8080");
#pragma warning restore S1075 // URIs should not be hardcoded
#else
            defaultApiHostBaseAddress = applicationBaseAddress;
#endif
            var apiBaseAddress = ApiUrlConfiguration.GetApiBaseAddress(
                builder.Configuration,
                applicationBaseAddress,
                defaultApiHostBaseAddress);

            builder.Services.AddTransient<CookieHandler>();
            builder.Services.AddScoped<HttpLogger>();
            builder.Services.AddSingleton<IApiUrlResolver>(new ApiUrlResolver(apiBaseAddress));
            builder.Services
                .AddHttpClient("API", (sp, client) => client.BaseAddress = sp.GetRequiredService<IApiUrlResolver>().ApiBaseAddress)
                .AddHttpMessageHandler<CookieHandler>()
                .RemoveAllLoggers()
                .AddLogger<HttpLogger>(wrapHandlersPipeline: true);
            builder.Services.AddQBittorrentApiClient("API");
            builder.Services.AddHttpClient("Assets", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
            builder.Services.AddHttpClient("GitHubReleases", client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("qbtmud-webui");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            });

            builder.Services.AddScoped<IWebApiCapabilityService, WebApiCapabilityService>();
            builder.Services.AddScoped<IClientDataStorageAdapter, ClientDataStorageAdapter>();
            builder.Services.AddSingleton<IStorageCatalogService, StorageCatalogService>();
            builder.Services.AddScoped<IStorageRoutingService, StorageRoutingService>();
            builder.Services.AddScoped<IDialogWorkflow, DialogWorkflow>();
            builder.Services.AddBrowserCapabilities();
            builder.Services.AddSingleton<IThemeFontCatalog, ThemeFontCatalog>();
            builder.Services.AddScoped<IThemeManagerService, ThemeManagerService>();
            builder.Services.AddScoped<ILanguageInitializationService, LanguageInitializationService>();
            builder.Services.AddScoped<IAppWarmupService, AppWarmupService>();
            builder.Services.AddScoped<IAppBuildInfoService, AppBuildInfoService>();
            builder.Services.AddScoped<IAppUpdateService, AppUpdateService>();
            builder.Services.AddScoped<AppSettingsService>();
            builder.Services.AddScoped<IAppSettingsService>(serviceProvider => serviceProvider.GetRequiredService<AppSettingsService>());
            builder.Services.AddScoped<LostConnectionWorkflow>();
            builder.Services.AddScoped<ILostConnectionWorkflow>(serviceProvider => serviceProvider.GetRequiredService<LostConnectionWorkflow>());
            builder.Services.AddScoped<IShellSessionWorkflow, ShellSessionWorkflow>();
            builder.Services.AddScoped<IPendingDownloadWorkflow, PendingDownloadWorkflow>();
            builder.Services.AddScoped<IStartupExperienceWorkflow, StartupExperienceWorkflow>();
            builder.Services.AddScoped<IStatusBarWorkflow, StatusBarWorkflow>();
            builder.Services.AddScoped<ITorrentQueryState, TorrentQueryState>();
            builder.Services.AddScoped<IBrowserNotificationService, BrowserNotificationService>();
            builder.Services.AddScoped<IInternalUrlProvider, InternalUrlProvider>();
            builder.Services.AddScoped<IMagnetLinkService, MagnetLinkService>();
            builder.Services.AddScoped<IWelcomeWizardStateService, WelcomeWizardStateService>();
            builder.Services.AddScoped<IWelcomeWizardPlanBuilder, WelcomeWizardPlanBuilder>();
            builder.Services.AddScoped<ITorrentCompletionNotificationService, TorrentCompletionNotificationService>();
            builder.Services.AddScoped<IStorageDiagnosticsService, StorageDiagnosticsService>();
            builder.Services.AddScoped<ISnackbarWorkflow, SnackbarWorkflow>();
            builder.Services.AddScoped<IPwaInstallPromptService, PwaInstallPromptService>();

            builder.Services.AddSingleton<ITorrentDataManager, TorrentDataManager>();
            builder.Services.AddSingleton<IPeerDataManager, PeerDataManager>();
            builder.Services.AddSingleton<IPreferencesDataManager, PreferencesDataManager>();
            builder.Services.AddSingleton<IRssDataManager, RssDataManager>();
            builder.Services.AddSingleton<IPeriodicTimerFactory, PeriodicTimerFactory>();
            builder.Services.AddSingleton<IManagedTimerRegistry, ManagedTimerRegistry>();
            builder.Services.AddSingleton<IManagedTimerFactory, ManagedTimerFactory>();
            builder.Services.AddScoped<ISpeedHistoryService, SpeedHistoryService>();

            builder.Services.AddScoped<IBrowserStorageServiceFactory, BrowserStorageServiceFactory>();
            builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
            builder.Services.AddScoped<ISettingsStorageService, SettingsStorageService>();
            builder.Services.AddScoped<ISessionStorageService, SessionStorageService>();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddTransient<IKeyboardService, KeyboardService>();

#if DEBUG
            builder.Logging.SetMinimumLevel(LogLevel.Information);
#else
            builder.Logging.SetMinimumLevel(LogLevel.Error);
#endif

            var host = builder.Build();
            if (routingMode == RoutingMode.Hash)
            {
                await host.InitializeHashRoutingAsync();
            }
            await host.Services.GetRequiredService<IAppWarmupService>().WarmupAsync();
            await host.RunAsync();
        }
    }
}
