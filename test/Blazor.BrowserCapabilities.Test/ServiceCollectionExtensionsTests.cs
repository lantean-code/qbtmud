using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Blazor.BrowserCapabilities.Test
{
    public sealed class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _target;

        public ServiceCollectionExtensionsTests()
        {
            _target = new ServiceCollection();
        }

        [Fact]
        public void GIVEN_NullServiceCollection_WHEN_AddBrowserCapabilities_THEN_ShouldThrowArgumentNullException()
        {
            IServiceCollection? services = null;

            Action action = () =>
            {
                _ = services!.AddBrowserCapabilities();
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_ServiceCollection_WHEN_AddBrowserCapabilities_THEN_ShouldRegisterBrowserCapabilitiesService()
        {
            _target.AddBrowserCapabilities();

            var descriptor = _target.Single(service => service.ServiceType == typeof(IBrowserCapabilitiesService));

            descriptor.ImplementationType.Should().Be(typeof(BrowserCapabilitiesService));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void GIVEN_ServiceAlreadyRegistered_WHEN_AddBrowserCapabilities_THEN_ShouldNotOverrideExistingRegistration()
        {
            _target.AddScoped(_ => Mock.Of<IBrowserCapabilitiesService>());

            _target.AddBrowserCapabilities();

            _target.Count(service => service.ServiceType == typeof(IBrowserCapabilitiesService)).Should().Be(1);
        }
    }
}
