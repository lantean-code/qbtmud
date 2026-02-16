using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace Lantean.QBTMud
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

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

            Uri baseAddress;
#if DEBUG
#pragma warning disable S1075 // URIs should not be hardcoded - used for debugging only
            baseAddress = new Uri("http://localhost:8080");
#pragma warning restore S1075 // URIs should not be hardcoded
#else
            baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
#endif

            builder.Services.AddTransient<CookieHandler>();
            builder.Services.AddScoped<HttpLogger>();
            builder.Services
                .AddScoped(sp => sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("API"))
                .AddHttpClient("API", client => client.BaseAddress = new Uri(baseAddress, "api/v2/"))
                .AddHttpMessageHandler<CookieHandler>()
                .RemoveAllLoggers()
                .AddLogger<HttpLogger>(wrapHandlersPipeline: true);
            builder.Services.AddHttpClient("Assets", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

            builder.Services.AddScoped<IApiClient, ApiClient>();
            builder.Services.AddScoped<IDialogWorkflow, DialogWorkflow>();
            builder.Services.AddBrowserCapabilities();
            builder.Services.AddSingleton<IThemeFontCatalog, ThemeFontCatalog>();
            builder.Services.AddScoped<IThemeManagerService, ThemeManagerService>();
            builder.Services.AddScoped<ILanguageInitializationService, LanguageInitializationService>();
            builder.Services.AddScoped<IAppWarmupService, AppWarmupService>();

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
            builder.Services.AddScoped<ISessionStorageService, SessionStorageService>();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddTransient<IKeyboardService, KeyboardService>();

#if DEBUG
            builder.Logging.SetMinimumLevel(LogLevel.Information);
#else
            builder.Logging.SetMinimumLevel(LogLevel.Error);
#endif

            var host = builder.Build();
            await host.Services.GetRequiredService<IAppWarmupService>().WarmupAsync();
            await host.RunAsync();
        }
    }
}
