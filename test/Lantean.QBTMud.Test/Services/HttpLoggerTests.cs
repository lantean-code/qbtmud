using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net.Http;

namespace Lantean.QBTMud.Test.Services
{
    public class HttpLoggerTests
    {
        private readonly ILogger<HttpLogger> _logger;
        private readonly HttpLogger _target;

        public HttpLoggerTests()
        {
            _logger = Mock.Of<ILogger<HttpLogger>>();
            _target = new HttpLogger(_logger);
        }

        [Fact]
        public void GIVEN_Request_WHEN_LogRequestStart_THEN_ShouldReturnNull()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            var result = _target.LogRequestStart(request);

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullLogger_WHEN_Constructing_THEN_ShouldThrow()
        {
            var act = () => new HttpLogger(null!);

            act.Should().Throw<ArgumentNullException>().Where(e => e.ParamName == "logger");
        }

        [Fact]
        public void GIVEN_RequestAndResponse_WHEN_LogRequestStop_THEN_ShouldNotThrow()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            _target.LogRequestStop(null, request, response, TimeSpan.FromMilliseconds(5));
        }

        [Fact]
        public void GIVEN_RequestUri_WHEN_LogRequestFailed_THEN_ShouldLogHostAndPath()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/path?q=1");
            var exception = new InvalidOperationException("failure");

            _target.LogRequestFailed(null, request, null, exception, TimeSpan.FromMilliseconds(12.3));

            Mock.Get(_logger).Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() == "Request towards 'https://example.com/path?q=1' failed after 12.3ms"),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_MissingRequestUri_WHEN_LogRequestFailed_THEN_ShouldLogWithEmptyPath()
        {
            var request = new HttpRequestMessage();
            var exception = new InvalidOperationException("failure");

            _target.LogRequestFailed(null, request, null, exception, TimeSpan.FromMilliseconds(0));

            Mock.Get(_logger).Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() == "Request towards '' failed after 0.0ms"),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
