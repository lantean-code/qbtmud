using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

namespace Lantean.QBTMudBlade
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
            baseAddress = new Uri("http://localhost:8080");
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

#if DEBUG
            builder.Logging.SetMinimumLevel(LogLevel.Information);
#else
            builder.Logging.SetMinimumLevel(LogLevel.Error);
#endif

            await builder.Build().RunAsync();
        }
    }
}