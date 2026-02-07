using AwesomeAssertions;
using Lantean.QBTMud.Test.Infrastructure;
using Lantean.QBTMud.Theming;
using Moq;
using System.Net;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemeFontCatalogTests
    {
        private static readonly string[] _fonts = new[]
        {
            "Nunito Sans",
            "Open Sans",
            "open sans",
            "Roboto-Flex",
            "Invalid!",
            " "
        };

        [Fact]
        public async Task GIVEN_ValidCatalog_WHEN_Initialized_THEN_NormalizesFontsAndResolvesUrl()
        {
            var json = JsonSerializer.Serialize(_fonts, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var target = CreateCatalog(request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().Equal("Nunito Sans", "Open Sans", "Roboto-Flex");

            var success = target.TryGetFontUrl("open sans", out var url);

            success.Should().BeTrue();
            url.Should().Contain("Open+Sans");
        }

        [Fact]
        public async Task GIVEN_InitializedCatalog_WHEN_EnsureInitializedCalledAgain_THEN_DoesNotReload()
        {
            var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
            var target = CreateCatalog(handler);

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));
            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            handler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_ClientFactoryThrows_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateClient("Assets")).Throws<InvalidOperationException>();
            var target = new ThemeFontCatalog(factory.Object);

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NonSuccessResponse_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var target = CreateCatalog(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_InvalidJson_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var target = CreateCatalog(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json")
            });

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NullJson_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var target = CreateCatalog(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            });

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RequestThrows_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var target = CreateCatalog(_ => throw new HttpRequestException("Request"));

            await (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RequestCanceled_WHEN_Initialized_THEN_SuggestedFontsEmpty()
        {
            var cts = new CancellationTokenSource();
            var handler = new DelegatingHandlerWithCancellation(cts, _ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });
            var target = CreateCatalog(handler);

            await target.EnsureInitialized(cts.Token);

            target.SuggestedFonts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ConcurrentEnsureInitialized_WHEN_LockHeld_THEN_SkipsReload()
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new BlockingHandler(() => tcs.Task);
            var target = CreateCatalog(handler);

            var first = (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));
            var second = (target.EnsureInitialized(Xunit.TestContext.Current.CancellationToken));

            tcs.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            await Task.WhenAll(first, second);

            handler.CallCount.Should().Be(1);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("Invalid!", false)]
        [InlineData("Open Sans", true)]
        public void GIVEN_FontFamily_WHEN_TryGetFontUrl_THEN_ReturnsExpected(string fontFamily, bool expected)
        {
            var target = CreateCatalog(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            var success = target.TryGetFontUrl(fontFamily, out var url);

            success.Should().Be(expected);
            if (expected)
            {
                url.Should().Contain("Open+Sans");
            }
            else
            {
                url.Should().BeEmpty();
            }
        }

        [Theory]
        [InlineData(null, "qbt-font-default")]
        [InlineData(" ", "qbt-font-default")]
        [InlineData("Open Sans", "qbt-font-open-sans")]
        [InlineData("Roboto-Flex", "qbt-font-roboto-flex")]
        [InlineData("Roboto Flex 2", "qbt-font-roboto-flex-2")]
        public void GIVEN_FontFamily_WHEN_BuildFontId_THEN_ReturnsExpected(string? fontFamily, string expected)
        {
            var target = CreateCatalog(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            var result = target.BuildFontId(fontFamily!);

            result.Should().Be(expected);
        }

        private static ThemeFontCatalog CreateCatalog(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var handler = new CountingHandler(responder);
            return CreateCatalog(handler);
        }

        private static ThemeFontCatalog CreateCatalog(HttpMessageHandler handler)
        {
            var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateClient("Assets")).Returns(TestHttpClientFactory.CreateClient(handler));
            return new ThemeFontCatalog(factory.Object);
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public int CallCount { get; private set; }

            public CountingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(_responder(request));
            }
        }

        private sealed class DelegatingHandlerWithCancellation : HttpMessageHandler
        {
            private readonly CancellationTokenSource _cts;
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public DelegatingHandlerWithCancellation(CancellationTokenSource cts, Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _cts = cts;
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.Register(() => _cts.Cancel());
                return Task.FromResult(_responder(request));
            }
        }

        private sealed class BlockingHandler : HttpMessageHandler
        {
            private readonly Func<Task<HttpResponseMessage>> _responder;

            public int CallCount { get; private set; }

            public BlockingHandler(Func<Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return await _responder();
            }
        }
    }
}
