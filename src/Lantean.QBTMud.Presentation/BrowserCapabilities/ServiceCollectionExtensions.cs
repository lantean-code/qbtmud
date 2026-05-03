using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lantean.QBTMud.BrowserCapabilities
{
    /// <summary>
    /// Service registration extensions for browser capability detection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds browser capability detection services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddBrowserCapabilities(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddScoped<IBrowserCapabilitiesService, BrowserCapabilitiesService>();
            return services;
        }
    }
}
