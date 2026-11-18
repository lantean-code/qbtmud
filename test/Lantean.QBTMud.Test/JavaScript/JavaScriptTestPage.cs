using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Playwright;

namespace Lantean.QBTMud.Test.JavaScript
{
    internal sealed class JavaScriptTestPage : IAsyncDisposable
    {
        private const string _origin = "https://qbtmud.test";
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly ICDPSession _session;

        public IPage Page { get; }

        private JavaScriptTestPage(IPlaywright playwright, IBrowser browser, IPage page, ICDPSession session)
        {
            _playwright = playwright;
            _browser = browser;
            Page = page;
            _session = session;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            try
            {
                var coverage = await _session.SendAsync("Profiler.takePreciseCoverage");
                var scripts = new List<object>();
                foreach (var entry in coverage!.Value.GetProperty("result").EnumerateArray())
                {
                    var url = entry.GetProperty("url").GetString()!;
                    if (url != $"{_origin}/js/boot.resources.js" && url != $"{_origin}/js/interop.js" && url != $"{_origin}/js/torrent-split-panel.module.js")
                    {
                        continue;
                    }

                    var scriptSource = await _session.SendAsync("Debugger.getScriptSource", new Dictionary<string, object>
                    {
                        ["scriptId"] = entry.GetProperty("scriptId").GetString()!
                    });
                    scripts.Add(new
                    {
                        url,
                        source = scriptSource!.Value.GetProperty("scriptSource").GetString(),
                        functions = entry.GetProperty("functions").Clone()
                    });
                }

                var outputDirectory = Environment.GetEnvironmentVariable("QBTMUD_JS_COVERAGE_DIR")
                    ?? Path.GetFullPath("/tmp/artifacts/qbtmud/javascript-coverage");
                Directory.CreateDirectory(outputDirectory);
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, $"coverage-{Guid.NewGuid():N}.json"),
                    JsonSerializer.Serialize(new { scripts }), Xunit.TestContext.Current.CancellationToken);
            }
            finally
            {
                await _browser.DisposeAsync();
                _playwright.Dispose();
            }
        }

        /// <summary>
        /// Opens an isolated Chromium page and loads the actual repository JavaScript under coverage.
        /// </summary>
        /// <param name="scriptName">The production script to load.</param>
        /// <param name="initializationScript">External dependency and storage setup to run before page scripts.</param>
        /// <returns>The page and its browser resources.</returns>
        public static async Task<JavaScriptTestPage> CreateAsync(string scriptName, string? initializationScript = null)
        {
            if (scriptName is not "boot.resources.js" and not "interop.js" and not "torrent-split-panel.module.js")
            {
                throw new ArgumentOutOfRangeException(nameof(scriptName));
            }

            var repositoryRoot = FindRepositoryRoot();
            var playwright = await Playwright.CreateAsync();
            IBrowser? browser = null;
            try
            {
                browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                var context = await browser.NewContextAsync();
                if (initializationScript is not null)
                {
                    await context.AddInitScriptAsync(initializationScript);
                }

                var page = await context.NewPageAsync();
                var session = await context.NewCDPSessionAsync(page);
                await session.SendAsync("Debugger.enable");
                await session.SendAsync("Profiler.enable");
                await session.SendAsync("Profiler.startPreciseCoverage", new Dictionary<string, object>
                {
                    ["callCount"] = true,
                    ["detailed"] = true
                });
                await context.RouteAsync("**/*", async route =>
                {
                    if (route.Request.Url == $"{_origin}/")
                    {
                        await route.FulfillAsync(new RouteFulfillOptions
                        {
                            ContentType = "text/html",
                            Path = Path.Combine(repositoryRoot, "test", "Lantean.QBTMud.Test", "JavaScript", "Fixtures", "test-page.html")
                        });
                    }
                    else if (route.Request.Url == $"{_origin}/js/{scriptName}")
                    {
                        await route.FulfillAsync(new RouteFulfillOptions
                        {
                            ContentType = "application/javascript",
                            Path = scriptName == "torrent-split-panel.module.js"
                                ? Path.Combine(repositoryRoot, "src", "Lantean.QBTMud.Presentation", "wwwroot", scriptName)
                                : Path.Combine(repositoryRoot, "src", "Lantean.QBTMud", "wwwroot", "js", scriptName)
                        });
                    }
                    else
                    {
                        await route.AbortAsync();
                    }
                });
                await page.GotoAsync($"{_origin}/");
                await page.AddScriptTagAsync(new PageAddScriptTagOptions
                {
                    Url = $"{_origin}/js/{scriptName}",
                    Type = scriptName == "torrent-split-panel.module.js" ? "module" : "text/javascript"
                });
                await page.WaitForFunctionAsync(scriptName == "boot.resources.js"
                    ? "typeof window.bootResource === 'function'"
                    : scriptName == "interop.js"
                        ? "typeof window.qbt?.registerKeypressEvent === 'function'"
                        : "!!customElements.get('qbt-torrent-split')");
                return new JavaScriptTestPage(playwright, browser, page, session);
            }
            catch
            {
                if (browser is not null)
                {
                    await browser.DisposeAsync();
                }

                playwright.Dispose();
                throw;
            }
        }

        private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
        {
            var directory = Path.GetDirectoryName(sourcePath);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory, "Lantean.QBTMud.slnx")))
                {
                    return directory;
                }

                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new DirectoryNotFoundException("The repository root could not be located.");
        }
    }
}
