using AwesomeAssertions;
using Lantean.QBTMud.Infrastructure.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ApiUrlResolverTests
    {
        private readonly ApiUrlResolver _target;

        public ApiUrlResolverTests()
        {
            _target = new ApiUrlResolver(new Uri("https://api.example/qbt"));
        }

        [Fact]
        public void GIVEN_RelativeApiBaseAddress_WHEN_Constructed_THEN_ThrowsArgumentException()
        {
            Action action = () =>
            {
                _ = new ApiUrlResolver(new Uri("/qbt", UriKind.Relative));
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_ApiBaseAddressWithoutTrailingSlash_WHEN_Constructed_THEN_NormalizesApiBaseAddress()
        {
            _target.ApiBaseAddress.AbsoluteUri.Should().Be("https://api.example/qbt/");
        }

        [Fact]
        public void GIVEN_ApiBaseAddressWithTrailingSlash_WHEN_Constructed_THEN_PreservesApiBaseAddress()
        {
            var target = new ApiUrlResolver(new Uri("https://api.example/qbt/"));

            target.ApiBaseAddress.AbsoluteUri.Should().Be("https://api.example/qbt/");
        }

        [Fact]
        public void GIVEN_WhitespaceRelativePath_WHEN_BuildAbsoluteUrl_THEN_ThrowsArgumentException()
        {
            Action action = () =>
            {
                _ = _target.BuildAbsoluteUrl("   ");
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_RelativePathThatBecomesEmpty_WHEN_BuildAbsoluteUrl_THEN_ThrowsArgumentException()
        {
            Action action = () =>
            {
                _ = _target.BuildAbsoluteUrl("  /   ");
            };

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GIVEN_RelativePathWithWhitespaceAndLeadingSlash_WHEN_BuildAbsoluteUrl_THEN_ReturnsNormalizedAbsoluteUrl()
        {
            var result = _target.BuildAbsoluteUrl(" /torrentcreator/torrentFile?taskID=TaskId ");

            result.Should().Be("https://api.example/qbt/torrentcreator/torrentFile?taskID=TaskId");
        }
    }
}
