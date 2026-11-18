using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Playwright;

namespace Lantean.QBTMud.Test.JavaScript
{
    public sealed class BootResourcesTests
    {
        private const string _cdnInitialization = "window.__useCdnAot = true; window.__cdnBase = 'https://cdn.qbtmud.test/release/'; window.__assetVersion = 'Build 1';";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_CdnFailure_WHEN_BootResourceRequested_THEN_ShouldReturnLocalResponseAndDisableFurtherCdnRequests(bool networkFailure)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("boot.resources.js", _cdnInitialization);
            var requests = new ConcurrentQueue<string>();
            const string payload = "Payload";
            var integrity = "sha256-" + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
            await target.Page.RouteAsync("https://cdn.qbtmud.test/**", async route =>
            {
                requests.Enqueue(route.Request.Url);
                if (networkFailure)
                {
                    await route.AbortAsync();
                }
                else
                {
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 503,
                        Body = payload,
                        Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" }
                    });
                }
            });
            await target.Page.RouteAsync("https://qbtmud.test/_framework/**", async route =>
            {
                requests.Enqueue(route.Request.Url);
                await route.FulfillAsync(new RouteFulfillOptions { Body = payload });
            });

            var result = await target.Page.EvaluateAsync<JsonElement>("""
                async integrity => {
                    const response = await window.bootResource('assembly', 'Client.wasm', '/_framework/Client.wasm?existing=true', integrity);
                    return {
                        isResponse: response instanceof Response,
                        status: response.status,
                        body: await response.text(),
                        nextUri: window.bootResource('assembly', 'Next.wasm', '/_framework/Next.wasm', '')
                    };
                }
                """, integrity);

            result.GetProperty("isResponse").GetBoolean().Should().BeTrue();
            result.GetProperty("status").GetInt32().Should().Be(200);
            result.GetProperty("body").GetString().Should().Be(payload);
            result.GetProperty("nextUri").GetString().Should().Be("/_framework/Next.wasm?v=Build%201");
            requests.Should().Equal("https://cdn.qbtmud.test/release/_framework/Client.wasm?v=Build%201",
                "https://qbtmud.test/_framework/Client.wasm?existing=true&v=Build%201");
        }

        [Fact]
        public async Task GIVEN_SuccessfulCdn_WHEN_BootResourceRequested_THEN_ShouldReturnCdnResponseWithoutLocalFetch()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("boot.resources.js", _cdnInitialization);
            var requests = new ConcurrentQueue<string>();
            await target.Page.RouteAsync("**/_framework/**", async route =>
            {
                requests.Enqueue(route.Request.Url);
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Body = "Payload",
                    Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" }
                });
            });

            var result = await target.Page.EvaluateAsync<string>("async () => (await window.bootResource('assembly', '_framework/Client.wasm', '/_framework/Client.wasm', '')).text()");

            result.Should().Be("Payload");
            requests.Should().Equal("https://cdn.qbtmud.test/release/_framework/Client.wasm?v=Build%201");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_UnusableLocalFallback_WHEN_CdnFails_THEN_ShouldRejectResourceAndPreserveIntegrity(bool corruptPayload)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("boot.resources.js", _cdnInitialization);
            await target.Page.RouteAsync("https://cdn.qbtmud.test/**", route => route.AbortAsync());
            await target.Page.RouteAsync("https://qbtmud.test/_framework/**", async route =>
            {
                if (corruptPayload)
                {
                    await route.FulfillAsync(new RouteFulfillOptions { Body = "Corrupt payload" });
                }
                else
                {
                    await route.AbortAsync();
                }
            });
            var integrity = "sha256-" + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("Expected payload")));

            var rejected = await target.Page.EvaluateAsync<bool>("""
                async integrity => {
                    try {
                        await window.bootResource('assembly', 'Client.wasm', '/_framework/Client.wasm', integrity);
                        return false;
                    } catch {
                        return true;
                    }
                }
                """, integrity);

            rejected.Should().BeTrue();
        }

        [Theory]
        [InlineData("Open Sans", false, "Open+Sans")]
        [InlineData("Open Sans", true, "Open+Sans")]
        [InlineData("  Open Sans  ", false, "Open+Sans")]
        [InlineData("Roboto-Slab", false, "Roboto-Slab")]
        [InlineData("Name\\Family", false, null)]
        [InlineData("Name;Color", false, null)]
        [InlineData("<script>", false, null)]
        [InlineData("", false, null)]
        public async Task GIVEN_PersistedFont_WHEN_BootstrapRuns_THEN_ShouldLoadOnlyValidFontNames(string font, bool jsonEncoded, string? expectedFamily)
        {
            var storedValue = jsonEncoded ? JsonSerializer.Serialize(font) : font;
            var initialization = "localStorage.setItem('QbtMud.ThemeManager.BootstrapFontFamily', " + JsonSerializer.Serialize(storedValue) + ");";
            await using var target = await JavaScriptTestPage.CreateAsync("boot.resources.js", initialization);

            var fonts = await target.Page.EvaluateAsync<string[]>("Array.from(document.getElementsByTagName('link')).map(link => link.href)");

            if (expectedFamily is null)
            {
                fonts.Should().BeEmpty();
            }
            else
            {
                fonts.Should().Equal($"https://fonts.googleapis.com/css2?family={expectedFamily}&display=swap");
            }
        }

        [Fact]
        public async Task GIVEN_ExistingFontStylesheet_WHEN_BootstrapRuns_THEN_ShouldPreserveSingleStylesheet()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("boot.resources.js", """
                localStorage.setItem('QbtMud.ThemeManager.BootstrapFontFamily', 'Open Sans');
                document.addEventListener('DOMContentLoaded', () => {
                    const link = document.createElement('link');
                    link.id = 'qbt-font-open-sans';
                    link.rel = 'stylesheet';
                    link.href = 'https://qbtmud.test/fonts/existing.css';
                    document.head.appendChild(link);
                });
                """);

            var fonts = await target.Page.EvaluateAsync<string[]>("Array.from(document.getElementsByTagName('link')).map(link => link.href)");

            fonts.Should().Equal("https://qbtmud.test/fonts/existing.css");
        }
    }
}
