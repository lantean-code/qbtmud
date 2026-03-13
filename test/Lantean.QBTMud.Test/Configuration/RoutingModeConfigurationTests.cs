using AwesomeAssertions;
using Lantean.QBTMud.Configuration;
using Microsoft.Extensions.Configuration;

namespace Lantean.QBTMud.Test.Configuration
{
    public sealed class RoutingModeConfigurationTests
    {
        [Fact]
        public void GIVEN_NullConfiguration_WHEN_GetRoutingMode_THEN_ThrowsArgumentNullException()
        {
            IConfiguration? configuration = null;

            Action action = () =>
            {
                _ = RoutingModeConfiguration.GetRoutingMode(configuration!);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_MissingRoutingMode_WHEN_GetRoutingMode_THEN_ReturnsHash()
        {
            var configuration = CreateConfiguration();

            var result = RoutingModeConfiguration.GetRoutingMode(configuration);

            result.Should().Be(RoutingMode.Hash);
        }

        [Theory]
        [InlineData("Hash", "Hash")]
        [InlineData("hash", "Hash")]
        [InlineData("Path", "Path")]
        [InlineData("path", "Path")]
        [InlineData("", "Hash")]
        [InlineData("Invalid", "Hash")]
        public void GIVEN_RoutingModeConfigured_WHEN_GetRoutingMode_THEN_ReturnsExpectedMode(string value, string expected)
        {
            var configuration = CreateConfiguration(("Routing:Mode", value));

            var result = RoutingModeConfiguration.GetRoutingMode(configuration);

            result.ToString().Should().Be(expected);
        }

        private static IConfiguration CreateConfiguration(params (string Key, string Value)[] entries)
        {
            var values = entries.ToDictionary(entry => entry.Key, entry => (string?)entry.Value);

            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }
    }
}
