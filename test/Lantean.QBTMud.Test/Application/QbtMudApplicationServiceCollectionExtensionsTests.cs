using AwesomeAssertions;
using Lantean.QBTMud.Application;
using Lantean.QBTMud.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lantean.QBTMud.Test.Application
{
    public sealed class QbtMudApplicationServiceCollectionExtensionsTests
    {
        [Fact]
        public void GIVEN_ApplicationServices_WHEN_AddQbtMudApplication_THEN_ShouldRegisterTorrentCompletionNotificationsAsScoped()
        {
            var services = new ServiceCollection();

            services.AddQbtMudApplication();

            var descriptor = services.Single(service => service.ServiceType == typeof(ITorrentCompletionNotificationService));
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
            descriptor.ImplementationType.Should().Be(typeof(TorrentCompletionNotificationService));
        }
    }
}
