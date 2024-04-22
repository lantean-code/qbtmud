using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Blazored;
using Blazored.LocalStorage;

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
            builder.Services
                .AddScoped(sp => sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("API"))
                .AddHttpClient("API", client => client.BaseAddress = new Uri(baseAddress, "/api/v2/")).AddHttpMessageHandler<CookieHandler>();

            builder.Services.AddScoped<ApiClient>();
            builder.Services.AddScoped<IApiClient, ApiClient>();

            builder.Services.AddSingleton<IDataManager, DataManager>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();

            await builder.Build().RunAsync();
        }
    }
}
