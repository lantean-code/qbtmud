using AwesomeAssertions;
using Lantean.QBTMud.Infrastructure.Configuration;
using Lantean.QBTMud.Infrastructure.Services;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class InternalUrlProviderTests
    {
        private readonly TestNavigationManager _navigationManager;
        private readonly InternalUrlProvider _target;

        public InternalUrlProviderTests()
        {
            _navigationManager = new TestNavigationManager();
            _target = new InternalUrlProvider(_navigationManager, RoutingMode.Path);
        }

        [Fact]
        public void GIVEN_PathRouting_WHEN_GetAbsoluteUrlInvokedWithoutPath_THEN_ShouldReturnRootUrl()
        {
            var result = _target.GetAbsoluteUrl();

            result.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_PathRouting_WHEN_GetAbsoluteUrlInvokedWithQuery_THEN_ShouldReturnQueryOnRootPath()
        {
            var result = _target.GetAbsoluteUrl(query: "download=%s");

            result.Should().Be("http://localhost/?download=%s");
        }

        [Fact]
        public void GIVEN_PathRouting_WHEN_GetAbsoluteUrlInvokedWithPath_THEN_ShouldReturnNormalizedPathUrl()
        {
            var result = _target.GetAbsoluteUrl(" /dialogs/themes ");

            result.Should().Be("http://localhost/dialogs/themes");
        }

        [Fact]
        public void GIVEN_HashRouting_WHEN_GetAbsoluteUrlInvokedWithQuery_THEN_ShouldReturnHashRootUrl()
        {
            var target = new InternalUrlProvider(_navigationManager, RoutingMode.Hash);

            var result = target.GetAbsoluteUrl(query: "download=%s");

            result.Should().Be("http://localhost/#/?download=%s");
        }

        [Fact]
        public void GIVEN_HashRouting_WHEN_GetAbsoluteUrlInvokedWithPath_THEN_ShouldReturnHashPathUrl()
        {
            var target = new InternalUrlProvider(_navigationManager, RoutingMode.Hash);

            var result = target.GetAbsoluteUrl("/dialogs/themes");

            result.Should().Be("http://localhost/#/dialogs/themes");
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                Uri = ToAbsoluteUri(uri).AbsoluteUri;
            }
        }
    }
}
