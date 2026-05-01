using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Infrastructure.Services;
using Microsoft.JSInterop;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class MagnetLinkServiceTests
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IInternalUrlProvider _internalUrlProvider;
        private readonly MagnetLinkService _target;

        public MagnetLinkServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict).Object;
            _internalUrlProvider = Mock.Of<IInternalUrlProvider>();
            _target = new MagnetLinkService(_jsRuntime, _internalUrlProvider);
        }

        [Fact]
        public async Task GIVEN_HandlerName_WHEN_RegisterHandler_THEN_ShouldUseInternalTemplateUrl()
        {
            Mock.Get(_internalUrlProvider)
                .Setup(provider => provider.GetAbsoluteUrl(null, "download=%s"))
                .Returns("http://localhost/#/?download=%s");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<MagnetRegistrationResult>(
                    "qbt.registerMagnetHandler",
                    It.Is<object?[]>(arguments => MatchesRegisterArguments(arguments, "http://localhost/#/?download=%s", "HandlerName"))))
                .Returns(ValueTask.FromResult(new MagnetRegistrationResult
                {
                    Status = "success",
                }));

            var result = await _target.RegisterHandler("HandlerName");

            result.Status.Should().Be(MagnetHandlerRegistrationStatus.Success);
            result.Message.Should().BeNull();
            Mock.Get(_internalUrlProvider).Verify(provider => provider.GetAbsoluteUrl(null, "download=%s"), Times.Once);
            Mock.Get(_jsRuntime).Verify(runtime => runtime.InvokeAsync<MagnetRegistrationResult>(
                "qbt.registerMagnetHandler",
                It.Is<object?[]>(arguments => MatchesRegisterArguments(arguments, "http://localhost/#/?download=%s", "HandlerName"))), Times.Once);
        }

        [Theory]
        [InlineData("success", MagnetHandlerRegistrationStatus.Success)]
        [InlineData("insecure", MagnetHandlerRegistrationStatus.Insecure)]
        [InlineData("unsupported", MagnetHandlerRegistrationStatus.Unsupported)]
        [InlineData("unknown", MagnetHandlerRegistrationStatus.Unknown)]
        [InlineData(null, MagnetHandlerRegistrationStatus.Unknown)]
        public async Task GIVEN_JsStatus_WHEN_RegisterHandler_THEN_ShouldMapToEnum(string? status, MagnetHandlerRegistrationStatus expectedStatus)
        {
            Mock.Get(_internalUrlProvider)
                .Setup(provider => provider.GetAbsoluteUrl(null, "download=%s"))
                .Returns("http://localhost/?download=%s");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<MagnetRegistrationResult>(
                    "qbt.registerMagnetHandler",
                    It.IsAny<object?[]>()))
                .Returns(ValueTask.FromResult(new MagnetRegistrationResult
                {
                    Status = status,
                    Message = "Message",
                }));

            var result = await _target.RegisterHandler("HandlerName");

            result.Status.Should().Be(expectedStatus);
            result.Message.Should().Be("Message");
        }

        [Fact]
        public async Task GIVEN_JsException_WHEN_RegisterHandler_THEN_ShouldReturnUnknownWithExceptionMessage()
        {
            Mock.Get(_internalUrlProvider)
                .Setup(provider => provider.GetAbsoluteUrl(null, "download=%s"))
                .Returns("http://localhost/?download=%s");
            Mock.Get(_jsRuntime)
                .Setup(runtime => runtime.InvokeAsync<MagnetRegistrationResult>(
                    "qbt.registerMagnetHandler",
                    It.IsAny<object?[]>()))
                .ThrowsAsync(new JSException("Failure"));

            var result = await _target.RegisterHandler("HandlerName");

            result.Status.Should().Be(MagnetHandlerRegistrationStatus.Unknown);
            result.Message.Should().Be("Failure");
        }

        [Fact]
        public void GIVEN_FragmentDownload_WHEN_ExtractDownloadLink_THEN_ShouldReturnDecodedMagnet()
        {
            var result = _target.ExtractDownloadLink("http://localhost/#download=magnet%3A%3Fxt%3Durn%3Abtih%3AABC");

            result.Should().Be("magnet:?xt=urn:btih:ABC");
        }

        [Fact]
        public void GIVEN_HashRouteDownload_WHEN_ExtractDownloadLink_THEN_ShouldReturnDecodedMagnet()
        {
            var result = _target.ExtractDownloadLink("http://localhost/#/?download=magnet%3A%3Fxt%3Durn%3Abtih%3AABC");

            result.Should().Be("magnet:?xt=urn:btih:ABC");
        }

        [Fact]
        public void GIVEN_QueryDownload_WHEN_ExtractDownloadLink_THEN_ShouldReturnTorrentUrl()
        {
            var result = _target.ExtractDownloadLink("http://localhost/?download=https%3A%2F%2Fexample.com%2Ffile.torrent");

            result.Should().Be("https://example.com/file.torrent");
        }

        [Fact]
        public void GIVEN_QueryContainsSegmentWithoutSeparator_WHEN_ExtractDownloadLink_THEN_ShouldSkipSegment()
        {
            var result = _target.ExtractDownloadLink("http://localhost/?view&download=https%3A%2F%2Fexample.com%2Ffile.torrent");

            result.Should().Be("https://example.com/file.torrent");
        }

        [Fact]
        public void GIVEN_UnsupportedOrInvalidUrls_WHEN_ExtractDownloadLink_THEN_ShouldReturnNull()
        {
            _target.ExtractDownloadLink(null).Should().BeNull();
            _target.ExtractDownloadLink(" ").Should().BeNull();
            _target.ExtractDownloadLink("not-a-uri").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/#").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/#/torrents").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/#/?").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/#download").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/#download=").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/?view=all").Should().BeNull();
            _target.ExtractDownloadLink("http://localhost/?download=https://example.com/file.txt").Should().BeNull();
        }

        [Fact]
        public void GIVEN_SupportedLinks_WHEN_IsSupportedDownloadLink_THEN_ShouldReturnTrue()
        {
            _target.IsSupportedDownloadLink("magnet:?xt=urn:btih:ABC").Should().BeTrue();
            _target.IsSupportedDownloadLink("https://example.com/file.torrent").Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnsupportedLinks_WHEN_IsSupportedDownloadLink_THEN_ShouldReturnFalse()
        {
            _target.IsSupportedDownloadLink(null).Should().BeFalse();
            _target.IsSupportedDownloadLink(" ").Should().BeFalse();
            _target.IsSupportedDownloadLink("magnet:?dn=NameOnly").Should().BeFalse();
            _target.IsSupportedDownloadLink("ftp://example.com/file.torrent").Should().BeFalse();
            _target.IsSupportedDownloadLink("https://example.com/file.txt").Should().BeFalse();
            _target.IsSupportedDownloadLink("https://example.com/file.torrent\r\nInjected").Should().BeFalse();
            _target.IsSupportedDownloadLink(new string('a', (8 * 1024) + 1)).Should().BeFalse();
        }

        private static bool MatchesRegisterArguments(object?[]? arguments, string expectedTemplateUrl, string expectedHandlerName)
        {
            if (arguments is null || arguments.Length != 2)
            {
                return false;
            }

            return (string?)arguments[0] == expectedTemplateUrl &&
                (string?)arguments[1] == expectedHandlerName;
        }
    }
}
