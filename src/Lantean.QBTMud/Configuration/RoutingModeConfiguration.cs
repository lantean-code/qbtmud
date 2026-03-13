namespace Lantean.QBTMud.Configuration
{
    internal static class RoutingModeConfiguration
    {
        private const string _routingModeConfigurationKey = "Routing:Mode";

        public static RoutingMode GetRoutingMode(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var configuredValue = configuration[_routingModeConfigurationKey];
            if (Enum.TryParse<RoutingMode>(configuredValue, ignoreCase: true, out var routingMode))
            {
                return routingMode;
            }

            return RoutingMode.Hash;
        }
    }
}
