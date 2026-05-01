using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lantean.QBTMud.Test
{
    public sealed class AppTests
    {
        [Fact]
        public void GIVEN_PresentationPageRoute_WHEN_RouterUsesAdditionalAssemblies_THEN_ShouldDiscoverPresentationPage()
        {
            using var testContext = new BunitContext();
            RouteData? routeData = null;
            var navigationManager = testContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/about");

            testContext.Render<Router>(parameters => parameters
                .Add(router => router.AppAssembly, typeof(App).Assembly)
                .Add(router => router.AdditionalAssemblies, [typeof(PresentationAssemblyMarker).Assembly])
                .Add(router => router.Found, foundRouteData => builder => routeData = foundRouteData));

            routeData.Should().NotBeNull();
            routeData!.PageType.Should().Be(typeof(About));
        }
    }
}
