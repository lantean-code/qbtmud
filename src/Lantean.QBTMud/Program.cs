using Lantean.QBTMud.Application;
using Lantean.QBTMud.Configuration;
using Lantean.QBTMud.Infrastructure;
using Lantean.QBTMud.Infrastructure.Configuration;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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

            builder.Services.AddQbtMudApplication();
            builder.Services.AddQbtMudInfrastructure(apiBaseAddress, applicationBaseAddress, typeof(Program).Assembly);
            builder.Services.AddQbtMudPresentation();

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
