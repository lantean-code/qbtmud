using System.Reflection;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Infrastructure.Services;
using Lantean.QBTMud.Infrastructure.Services.Localization;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Infrastructure
{
    /// <summary>
    /// Service registration extensions for qbtmud infrastructure services.
    /// </summary>
    public static class QbtMudInfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Adds qbtmud infrastructure services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiBaseAddress">The resolved qBittorrent Web API base address.</param>
        /// <param name="applicationBaseAddress">The qbtmud application base address.</param>
        /// <param name="resourceAssembly">The assembly that contains embedded fallback resources.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddQbtMudInfrastructure(
            this IServiceCollection services,
            Uri apiBaseAddress,
            Uri applicationBaseAddress,
            Assembly resourceAssembly)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(apiBaseAddress);
            ArgumentNullException.ThrowIfNull(applicationBaseAddress);
            ArgumentNullException.ThrowIfNull(resourceAssembly);

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services
                .AddOptions<AssemblyResourceAccessorOptions>()
                .Configure(options => options.ResourceAssembly = resourceAssembly)
                .Validate(options => options.ResourceAssembly is not null, "The resource assembly option must be configured.");
            services.AddOptions<WebUiLocalizationOptions>();
            services.AddHttpClient("WebUiAssets", client => client.BaseAddress = applicationBaseAddress);
            services.TryAddScoped<ILanguageFileLoader, LanguageFileLoader>();
            services.TryAddScoped<IAssemblyResourceAccessor, AssemblyResourceAccessor>();
            services.TryAddScoped<ILanguageEmbeddedResourceLoader, LanguageEmbeddedResourceLoader>();
            services.TryAddScoped<ILanguageResourceProvider, LanguageResourceProvider>();
            services.TryAddScoped<ILanguageResourceLoader, LanguageResourceLoader>();
            services.TryAddScoped<ILanguageLocalizer, LanguageLocalizer>();
            services.TryAddScoped<ILanguageCatalog, LanguageCatalog>();
            services.TryAddScoped<ILanguageInitializationService, LanguageInitializationService>();

            services.TryAddTransient<CookieHandler>();
            services.TryAddScoped<HttpLogger>();
            services
                .AddOptions<ApiUrlResolverOptions>()
                .Configure(options => options.ApiBaseAddress = apiBaseAddress)
                .Validate(options => options.ApiBaseAddress is { IsAbsoluteUri: true }, "The API base address option must be absolute.");
            services.TryAddSingleton<IApiUrlResolver, ApiUrlResolver>();
            services
                .AddHttpClient("API", client => client.BaseAddress = apiBaseAddress)
                .AddHttpMessageHandler<CookieHandler>()
                .RemoveAllLoggers()
                .AddLogger<HttpLogger>(wrapHandlersPipeline: true);
            services.AddQBittorrentApiClient("API");
            services.AddHttpClient("Assets", client => client.BaseAddress = applicationBaseAddress);
            services.AddHttpClient("GitHubReleases", client =>
            {
                client.BaseAddress = new Uri("https://api.github.com/");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("qbtmud-webui");
                client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            });

            services.TryAddScoped<IWebApiCapabilityService, WebApiCapabilityService>();
            services.TryAddScoped<IClientDataStorageAdapter, ClientDataStorageAdapter>();
            services.TryAddScoped<IBrowserStorageServiceFactory, BrowserStorageServiceFactory>();
            services.TryAddScoped<ILocalStorageService, LocalStorageService>();
            services.TryAddScoped<ILocalStorageEntryAdapter, LocalStorageEntryAdapter>();
            services.TryAddScoped<ISessionStorageService, SessionStorageService>();
            services.TryAddSingleton<IClipboardService, ClipboardService>();
            services.TryAddTransient<IKeyboardService, KeyboardService>();
            services.TryAddScoped<IAppBuildInfoService, AppBuildInfoService>();
            services.TryAddScoped<IAppUpdateService, AppUpdateService>();
            services.TryAddScoped<IBrowserNotificationService, BrowserNotificationService>();
            services.TryAddScoped<IInternalUrlProvider, InternalUrlProvider>();
            services.TryAddScoped<IMagnetLinkService, MagnetLinkService>();
            services.TryAddScoped<IPwaInstallPromptService, PwaInstallPromptService>();
            services.TryAddSingleton<IThemeFontCatalog, ThemeFontCatalog>();
            services.TryAddScoped<IThemeManagerService, ThemeManagerService>();

            return services;
        }
    }
}
