using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
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
                .AddHttpClient("API", client => client.BaseAddress = new Uri(baseAddress, "/api/v2/"))
                .AddHttpMessageHandler<CookieHandler>()
                .RemoveAllLoggers()
                .AddLogger<HttpLogger>(wrapHandlersPipeline: true);

            builder.Services.AddScoped<ApiClient>();
            builder.Services.AddScoped<IApiClient, ApiClient>();

            builder.Services.AddSingleton<IDataManager, DataManager>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddTransient<IKeyboardService, KeyboardService>();

#if DEBUG
            builder.Logging.SetMinimumLevel(LogLevel.Information);
#else
            builder.Logging.SetMinimumLevel(LogLevel.Error);
#endif

            MudGlobal.InputDefaults.ShrinkLabel = true;
            await builder.Build().RunAsync();
        }
    }
}