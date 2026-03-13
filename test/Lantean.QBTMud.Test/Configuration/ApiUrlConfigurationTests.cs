using AwesomeAssertions;
using Lantean.QBTMud.Configuration;
using Microsoft.Extensions.Configuration;

namespace Lantean.QBTMud.Test.Configuration
{
    public sealed class ApiUrlConfigurationTests
    {
        [Fact]
        public void GIVEN_NullConfiguration_WHEN_GetApiBaseAddress_THEN_ThrowsArgumentNullException()
        {
            IConfiguration? configuration = null;

            Action action = () =>
            {
                _ = ApiUrlConfiguration.GetApiBaseAddress(
                    configuration!,
                    new Uri("https://app.example/"),
                    new Uri("https://fallback.example/"));
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_RelativeApplicationBaseAddress_WHEN_GetApiBaseAddress_THEN_ThrowsArgumentException()
        {
            var configuration = CreateConfiguration();

            Action action = () =>
            {
                _ = ApiUrlConfiguration.GetApiBaseAddress(
                    configuration,
                    new Uri("/ui", UriKind.Relative),
                    new Uri("https://fallback.example/"));
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_RelativeDefaultApiHostBaseAddress_WHEN_GetApiBaseAddress_THEN_ThrowsArgumentException()
        {
            var configuration = CreateConfiguration();

            Action action = () =>
            {
                _ = ApiUrlConfiguration.GetApiBaseAddress(
                    configuration,
                    new Uri("https://app.example/ui/"),
                    new Uri("/qbt", UriKind.Relative));
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_MissingApiBaseUrl_WHEN_GetApiBaseAddress_THEN_UsesDefaultHostBaseAddress()
        {
            var configuration = CreateConfiguration();

            var result = ApiUrlConfiguration.GetApiBaseAddress(
                configuration,
                new Uri("https://app.example/ui/"),
                new Uri("https://fallback.example/qbt/"));

            result.AbsoluteUri.Should().Be("https://fallback.example/qbt/api/v2/");
        }

        [Theory]
        [InlineData("https://api.example/qbt/", "https://api.example/qbt/api/v2/")]
        [InlineData("https://api.example/qbt", "https://api.example/qbt/api/v2/")]
        [InlineData("http://api.example/qbt/", "http://api.example/qbt/api/v2/")]
        [InlineData("https://api.example/qbt/api/v2", "https://api.example/qbt/api/v2/")]
        [InlineData("https://api.example/qbt/api/v2/", "https://api.example/qbt/api/v2/")]
        [InlineData("/qbt/", "https://app.example/qbt/api/v2/")]
        [InlineData("./qbt/", "https://app.example/ui/qbt/api/v2/")]
        [InlineData("https://api.example/qbt/?x=1#part", "https://api.example/qbt/api/v2/")]
        public void GIVEN_ConfiguredApiBaseUrl_WHEN_GetApiBaseAddress_THEN_NormalizesExpectedAddress(string value, string expected)
        {
            var configuration = CreateConfiguration(("Api:BaseUrl", value));

            var result = ApiUrlConfiguration.GetApiBaseAddress(
                configuration,
                new Uri("https://app.example/ui/"),
                new Uri("https://fallback.example/"));

            result.AbsoluteUri.Should().Be(expected);
        }

        [Fact]
        public void GIVEN_InvalidConfiguredApiBaseUrl_WHEN_GetApiBaseAddress_THEN_FallsBackToDefaultHostBaseAddress()
        {
            var configuration = CreateConfiguration(("Api:BaseUrl", "://bad"));

            var result = ApiUrlConfiguration.GetApiBaseAddress(
                configuration,
                new Uri("https://app.example/ui/"),
                new Uri("https://fallback.example/qbt/"));

            result.AbsoluteUri.Should().Be("https://fallback.example/qbt/api/v2/");
        }

        [Theory]
        [InlineData("qbt/")]
        [InlineData("qbt.example.com/qbt/")]
        [InlineData("localhost:8080/qbt/")]
        [InlineData("ftp://api.example/qbt/")]
        public void GIVEN_AmbiguousOrUnsupportedConfiguredApiBaseUrl_WHEN_GetApiBaseAddress_THEN_FallsBackToDefaultHostBaseAddress(string value)
        {
            var configuration = CreateConfiguration(("Api:BaseUrl", value));

            var result = ApiUrlConfiguration.GetApiBaseAddress(
                configuration,
                new Uri("https://app.example/ui/"),
                new Uri("https://fallback.example/qbt/"));

            result.AbsoluteUri.Should().Be("https://fallback.example/qbt/api/v2/");
        }

        [Fact]
        public void GIVEN_RelativeApiBaseUrlAndUnsupportedApplicationScheme_WHEN_GetApiBaseAddress_THEN_FallsBackToDefaultHostBaseAddress()
        {
            var configuration = CreateConfiguration(("Api:BaseUrl", "/qbt/"));

            var result = ApiUrlConfiguration.GetApiBaseAddress(
                configuration,
                new Uri("ftp://app.example/ui/"),
                new Uri("https://fallback.example/qbt/"));

            result.AbsoluteUri.Should().Be("https://fallback.example/qbt/api/v2/");
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
