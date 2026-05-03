using AwesomeAssertions;
using Lantean.QBTMud.Application;
using Lantean.QBTMud.Infrastructure;
using Lantean.QBTMud.Infrastructure.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace Lantean.QBTMud.Test.Host
{
    public sealed class CompositionRootServiceRegistrationTests
    {
        [Fact]
        public void GIVEN_ComposedQbtMudServices_WHEN_ServiceProviderIsValidated_THEN_ShouldConstructRegisteredServices()
        {
            var services = new ServiceCollection();
            var applicationBaseAddress = new Uri("http://localhost/");
            var apiBaseAddress = new Uri("http://localhost/api/v2/");

            services.AddLogging();
            services.AddOptions();
            services.AddSingleton(Mock.Of<IJSRuntime>());
            services.AddSingleton<NavigationManager>(new TestNavigationManager(applicationBaseAddress));
            services.AddSingleton(typeof(RoutingMode), RoutingMode.Path);

            services.AddQbtMudApplication();
            services.AddQbtMudInfrastructure(apiBaseAddress, applicationBaseAddress, typeof(Program).Assembly);
            services.AddQbtMudPresentation();

            var action = () =>
            {
                using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
            };

            action.Should().NotThrow();
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager(Uri baseAddress)
            {
                Initialize(baseAddress.AbsoluteUri, baseAddress.AbsoluteUri);
            }

            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
            }
        }
    }
}
