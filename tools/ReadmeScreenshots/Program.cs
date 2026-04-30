using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace ReadmeScreenshots
{
    internal static class Program
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        private static readonly DateTimeOffset _torrentCreationDate = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);

        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                WriteUsage();
                return 1;
            }

            try
            {
                return args[0] switch
                {
                    "generate-fixtures" => await GenerateFixturesAsync(args),
                    "hash-password" => await HashPasswordAsync(args),
                    "seed" => await SeedAsync(args),
                    "capture" => await CaptureAsync(args),
                    _ => Fail($"Unknown command '{args[0]}'.")
                };
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static Task<int> HashPasswordAsync(string[] args)
        {
            var password = GetRequiredOption(args, "--password");
            Console.WriteLine(GenerateQBitTorrentPasswordHash(password));
            return Task.FromResult(0);
        }

        private static async Task<int> GenerateFixturesAsync(string[] args)
        {
            var repoRoot = GetRequiredOption(args, "--repo-root");
            var manifest = await LoadManifestAsync(repoRoot);
            var payloadRoot = Path.Combine(repoRoot, "tools", "ReadmeScreenshots", "readme-fixtures", "payloads");
            var torrentRoot = Path.Combine(repoRoot, "tools", "ReadmeScreenshots", "readme-fixtures", "torrents");

            Directory.CreateDirectory(torrentRoot);

            foreach (var fixture in manifest.Fixtures)
            {
                var payloadPath = Path.Combine(payloadRoot, fixture.PayloadFile);
                var torrentPath = Path.Combine(torrentRoot, $"{fixture.Id}.torrent");
                var torrentBytes = CreateSingleFileTorrent(payloadPath, fixture.DisplayName, fixture.Comment);
                await File.WriteAllBytesAsync(torrentPath, torrentBytes);
                Console.WriteLine($"Generated {torrentPath}");
            }

            return 0;
        }

        private static async Task<int> SeedAsync(string[] args)
        {
            var repoRoot = GetRequiredOption(args, "--repo-root");
            var apiBaseUrl = GetRequiredOption(args, "--api-base-url");
            var downloadRoot = GetRequiredOption(args, "--download-root");
            var captureStatePath = GetRequiredOption(args, "--capture-state");
            var username = GetOptionalOption(args, "--username");
            var password = GetOptionalOption(args, "--password");

            var manifest = await LoadManifestAsync(repoRoot);
            var payloadRoot = Path.Combine(repoRoot, "tools", "ReadmeScreenshots", "readme-fixtures", "payloads");
            var torrentRoot = Path.Combine(repoRoot, "tools", "ReadmeScreenshots", "readme-fixtures", "torrents");

            Directory.CreateDirectory(downloadRoot);

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var services = new ServiceCollection();
            services.AddQBittorrentApiClient(httpClient);

            var sp = services.BuildServiceProvider();
            var apiClient = sp.GetRequiredService<IApiClient>();

            await WaitForApiAsync(apiClient, username, password);

            await ResetEnvironmentAsync(apiClient);

            var categories = manifest.Fixtures
                .Select(fixture => fixture.Category)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var category in categories)
            {
                var categoryPath = Path.Combine(downloadRoot, category);
                Directory.CreateDirectory(categoryPath);
                await apiClient.AddCategoryAsync(category, categoryPath);
            }

            var tags = manifest.Fixtures
                .SelectMany(fixture => fixture.Tags)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (tags.Length > 0)
            {
                await apiClient.CreateTagsAsync(tags);
            }

            var seededFixtures = new List<SeededFixture>();

            foreach (var fixture in manifest.Fixtures)
            {
                var fixtureDirectory = Path.Combine(downloadRoot, fixture.Category);
                Directory.CreateDirectory(fixtureDirectory);

                var sourcePayloadPath = Path.Combine(payloadRoot, fixture.PayloadFile);
                var workingPayloadPath = Path.Combine(fixtureDirectory, fixture.DisplayName);
                if (fixture.State is FixtureState.Complete or FixtureState.Missing)
                {
                    File.Copy(sourcePayloadPath, workingPayloadPath, overwrite: true);
                }

                var currentMainData = await GetRequiredValueAsync(apiClient.GetMainDataAsync(0), "load current main data");
                var knownHashes = (currentMainData.Torrents ?? new Dictionary<string, Torrent>(StringComparer.Ordinal)).Keys.ToHashSet(StringComparer.Ordinal);
                var torrentPath = Path.Combine(torrentRoot, $"{fixture.Id}.torrent");
                await using var torrentStream = File.OpenRead(torrentPath);
                var addTorrentResult = await GetRequiredValueAsync(apiClient.AddTorrentAsync(new AddTorrentParams
                {
                    SavePath = fixtureDirectory,
                    Category = fixture.Category,
                    Tags = fixture.Tags,
                    Stopped = true,
                    Torrents = new Dictionary<string, Stream>(StringComparer.Ordinal)
                    {
                        [Path.GetFileName(torrentPath)] = torrentStream
                    }
                }), "add torrent");

                var hash = await WaitForTorrentHashAsync(apiClient, knownHashes, addTorrentResult.AddedTorrentIds);

                if (fixture.State is FixtureState.Complete or FixtureState.Missing)
                {
                    await WaitForTorrentAsync(
                        apiClient,
                        hash,
                        torrent => (torrent.Progress ?? 0) >= 0.999d && IsCompletedState(torrent.State));
                }

                if (fixture.State == FixtureState.Missing)
                {
                    if (File.Exists(workingPayloadPath))
                    {
                        File.Delete(workingPayloadPath);
                    }

                    await apiClient.RecheckTorrentsAsync(TorrentSelector.FromHash(hash));
                    await WaitForTorrentAsync(
                        apiClient,
                        hash,
                        torrent => IsMissingLikeState(torrent.State));
                }

                if (fixture.State == FixtureState.Stopped)
                {
                    await WaitForTorrentAsync(
                        apiClient,
                        hash,
                        torrent => IsIncompleteState(torrent.State));
                }

                seededFixtures.Add(new SeededFixture
                {
                    Id = fixture.Id,
                    DisplayName = fixture.DisplayName,
                    Hash = hash,
                    State = fixture.State
                });
            }

            var detailFixture = seededFixtures.Single(fixture => string.Equals(fixture.Id, manifest.DetailFixtureId, StringComparison.Ordinal));
            var captureState = new ReadmeCaptureState
            {
                DetailHash = detailFixture.Hash,
                Fixtures = seededFixtures
            };

            Directory.CreateDirectory(Path.GetDirectoryName(captureStatePath)!);
            await File.WriteAllTextAsync(captureStatePath, JsonSerializer.Serialize(captureState, _jsonOptions));
            Console.WriteLine($"Wrote {captureStatePath}");
            return 0;
        }

        private static async Task<int> CaptureAsync(string[] args)
        {
            var appBaseUrl = GetRequiredOption(args, "--app-base-url").TrimEnd('/');
            var outputDir = GetRequiredOption(args, "--output-dir");
            var captureStatePath = GetRequiredOption(args, "--capture-state");
            var username = GetRequiredOption(args, "--username");
            var password = GetRequiredOption(args, "--password");
            var edgeExecutablePath = GetOptionalOption(args, "--edge-executable");
            var browserChannel = GetOptionalOption(args, "--browser-channel");

            var captureState = JsonSerializer.Deserialize<ReadmeCaptureState>(await File.ReadAllTextAsync(captureStatePath), _jsonOptions)
                ?? throw new InvalidOperationException("Could not read capture state.");

            Directory.CreateDirectory(outputDir);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Channel = string.IsNullOrWhiteSpace(edgeExecutablePath) ? browserChannel : null,
                ExecutablePath = edgeExecutablePath
            });

            await CaptureWelcomeWizardAsync(browser, appBaseUrl, outputDir, username, password);
            await CaptureDashboardAsync(browser, appBaseUrl, outputDir, username, password);
            await CaptureTorrentDetailsAsync(browser, appBaseUrl, outputDir, captureState.DetailHash, username, password);
            await CaptureThemeManagerAsync(browser, appBaseUrl, outputDir, username, password);
            await CaptureAppSettingsAsync(browser, appBaseUrl, outputDir, username, password);

            return 0;
        }

        private static async Task CaptureWelcomeWizardAsync(IBrowser browser, string appBaseUrl, string outputDir, string username, string password)
        {
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1680, Height = 1180 },
                ColorScheme = ColorScheme.Dark
            });

            var page = await context.NewPageAsync();
            var consoleMessages = new List<string>();
            page.Console += (_, message) => consoleMessages.Add($"console.{message.Type}: {message.Text}");
            page.PageError += (_, message) => consoleMessages.Add($"pageerror: {message}");
            await page.GotoAsync($"{appBaseUrl}/#/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await EnsureLoggedInAsync(page, appBaseUrl, username, password);
            await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");
            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
            try
            {
                await page.GetByText("Thanks for using qbtmud. Choose your language to get started.").WaitForAsync();
            }
            catch
            {
                var debugPath = Path.Combine(outputDir, "welcome-wizard-debug.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = debugPath,
                    FullPage = true
                });

                var storageState = await page.EvaluateAsync<string>(
                    @"() => JSON.stringify({
                        href: window.location.href,
                        localStorage: Object.fromEntries(Object.entries(localStorage)),
                        sessionStorage: Object.fromEntries(Object.entries(sessionStorage))
                    }, null, 2)");
                var bodyText = await page.EvaluateAsync<string>("() => document.body?.innerText ?? ''");
                var bodyHtml = await page.EvaluateAsync<string>("() => document.body?.innerHTML ?? ''");

                Console.Error.WriteLine(storageState);
                Console.Error.WriteLine(bodyText);
                Console.Error.WriteLine(bodyHtml[..Math.Min(bodyHtml.Length, 2_000)]);
                foreach (var message in consoleMessages)
                {
                    Console.Error.WriteLine(message);
                }
                throw;
            }

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outputDir, "welcome-wizard.png"),
                FullPage = true
            });
        }

        private static async Task CaptureDashboardAsync(IBrowser browser, string appBaseUrl, string outputDir, string username, string password)
        {
            await using var context = await CreateConfiguredContextAsync(browser, appBaseUrl, username, password);
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{appBaseUrl}/#/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByText("Alpine Release Notes Mirror").First.WaitForAsync();
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outputDir, "dashboard.png"),
                FullPage = true
            });
        }

        private static async Task CaptureTorrentDetailsAsync(IBrowser browser, string appBaseUrl, string outputDir, string hash, string username, string password)
        {
            await using var context = await CreateConfiguredContextAsync(browser, appBaseUrl, username, password);
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{appBaseUrl}/#/details/{hash}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByText("Alpine Release Notes Mirror").First.WaitForAsync();
            await page.WaitForTimeoutAsync(1_000);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outputDir, "torrent-details.png"),
                FullPage = true
            });
        }

        private static async Task CaptureThemeManagerAsync(IBrowser browser, string appBaseUrl, string outputDir, string username, string password)
        {
            await using var context = await CreateConfiguredContextAsync(browser, appBaseUrl, username, password);
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{appBaseUrl}/#/themes", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GetByText("qbtmud Default").First.WaitForAsync();
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outputDir, "theme-manager.png"),
                FullPage = true
            });
        }

        private static async Task CaptureAppSettingsAsync(IBrowser browser, string appBaseUrl, string outputDir, string username, string password)
        {
            await using var context = await CreateConfiguredContextAsync(browser, appBaseUrl, username, password);
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{appBaseUrl}/#/app-settings", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            var dialog = page.Locator(".mud-dialog-container");
            if (await IsVisibleAsync(dialog))
            {
                var dialogText = await dialog.InnerTextAsync();
                throw new InvalidOperationException($"Unexpected modal dialog blocked the app settings screenshot: {dialogText}");
            }

            await page.GetByText("App Settings").First.WaitForAsync();
            var visualTab = page.GetByRole(AriaRole.Tab, new() { Name = "Visual" });
            await visualTab.WaitForAsync();
            await visualTab.ClickAsync();
            await page.WaitForTimeoutAsync(1_000);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(outputDir, "app-settings.png"),
                FullPage = true
            });
        }

        private static async Task<IBrowserContext> CreateConfiguredContextAsync(IBrowser browser, string appBaseUrl, string username, string password)
        {
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1680, Height = 1180 },
                ColorScheme = ColorScheme.Dark
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync($"{appBaseUrl}/#/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await EnsureLoggedInAsync(page, appBaseUrl, username, password);
            await page.EvaluateAsync(
                @"() => {
                    const prefix = 'QbtMud.';
                    const welcomeWizardState = JSON.stringify({
                        acknowledgedStepIds: [
                            'welcome.language.v1',
                            'welcome.theme.v1',
                            'welcome.notifications.v1',
                            'welcome.storage.v1'
                        ],
                        lastShownUtc: '2026-01-03T12:00:00Z',
                        lastCompletedUtc: '2026-01-03T12:00:00Z'
                    });
                    const appSettingsState = JSON.stringify({
                        updateChecksEnabled: false,
                        notificationsEnabled: false,
                        themeModePreference: 0,
                        downloadFinishedNotificationsEnabled: true,
                        torrentAddedNotificationsEnabled: false,
                        torrentAddedSnackbarsEnabledWithNotifications: false,
                        dismissedReleaseTag: null,
                        themeRepositoryIndexUrl: 'https://lantean-code.github.io/qbtmud-themes/index.json'
                    });

                    localStorage.setItem(prefix + 'WelcomeWizard.State.v2', welcomeWizardState);
                    localStorage.setItem(prefix + 'PwaInstallPrompt.Dismissed.v1', 'true');
                    localStorage.setItem(prefix + 'AppSettings.State.v1', appSettingsState);
                }");
            await page.CloseAsync();
            return context;
        }

        private static async Task EnsureLoggedInAsync(IPage page, string appBaseUrl, string username, string password)
        {
            var usernameField = page.Locator("[data-test-id='Username']");
            if (!IsLoginRoute(page.Url) && !await IsVisibleAsync(usernameField))
            {
                return;
            }

            await usernameField.FillAsync(username);
            await page.Locator("[data-test-id='Password']").FillAsync(password);
            await page.Locator("[data-test-id='Login']").ClickAsync();
            await page.WaitForFunctionAsync("() => !window.location.hash.includes('/login')");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (IsLoginRoute(page.Url))
            {
                throw new InvalidOperationException("Playwright login did not complete.");
            }

            await page.GotoAsync($"{appBaseUrl}/#/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }

        private static bool IsLoginRoute(string url)
        {
            return url.Contains("#/login", StringComparison.Ordinal);
        }

        private static async Task<bool> IsVisibleAsync(ILocator locator)
        {
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2_000
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (PlaywrightException)
            {
                return false;
            }
        }

        private static async Task WaitForApiAsync(IApiClient apiClient, string? username, string? password)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!timeout.IsCancellationRequested)
            {
                try
                {
                    var authState = await apiClient.CheckAuthStateAsync();
                    if (authState.TryGetValue(out var isAuthenticated) && isAuthenticated)
                    {
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        await apiClient.LoginAsync(username, password);
                        authState = await apiClient.CheckAuthStateAsync();
                        if (authState.TryGetValue(out isAuthenticated) && isAuthenticated)
                        {
                            return;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (InvalidOperationException)
                {
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
            }

            throw new TimeoutException("Timed out waiting for qBittorrent Web API.");
        }

        private static string GenerateQBitTorrentPasswordHash(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            Span<byte> salt = stackalloc byte[16];
            RandomNumberGenerator.Fill(salt);

            Span<byte> key = stackalloc byte[64];
            Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                key,
                100000,
                HashAlgorithmName.SHA512);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        private static async Task ResetEnvironmentAsync(IApiClient apiClient)
        {
            await EnsureSuccessAsync(apiClient.DeleteTorrentsAsync(TorrentSelector.AllTorrents(), deleteFiles: true), "delete all torrents");

            var categories = await GetRequiredValueAsync(apiClient.GetAllCategoriesAsync(), "load categories");
            if (categories.Count > 0)
            {
                await EnsureSuccessAsync(apiClient.RemoveCategoriesAsync(categories.Keys.ToArray()), "remove categories");
            }

            var tags = await GetRequiredValueAsync(apiClient.GetAllTagsAsync(), "load tags");
            if (tags.Count > 0)
            {
                await EnsureSuccessAsync(apiClient.DeleteTagsAsync(tags.ToArray()), "delete tags");
            }
        }

        private static async Task<string> WaitForTorrentHashAsync(IApiClient apiClient, HashSet<string> knownHashes, IReadOnlyList<string> addedTorrentIds)
        {
            if (addedTorrentIds.Count == 1)
            {
                return addedTorrentIds[0];
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!timeout.IsCancellationRequested)
            {
                var mainData = await GetRequiredValueAsync(apiClient.GetMainDataAsync(0), "load main data");
                var torrents = mainData.Torrents ?? new Dictionary<string, Torrent>(StringComparer.Ordinal);
                var newHash = torrents.Keys.FirstOrDefault(hash => !knownHashes.Contains(hash));
                if (!string.IsNullOrWhiteSpace(newHash))
                {
                    return newHash;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
            }

            throw new TimeoutException("Timed out waiting for added torrent hash.");
        }

        private static async Task WaitForTorrentAsync(IApiClient apiClient, string hash, Func<Torrent, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            TorrentState? lastState = null;
            double? lastProgress = null;
            long? lastAmountLeft = null;
            string? lastName = null;

            try
            {
                while (!timeout.IsCancellationRequested)
                {
                    var torrent = await GetRequiredValueAsync(apiClient.GetTorrentAsync(hash), "load torrent");
                    if (torrent is not null)
                    {
                        lastState = torrent.State;
                        lastProgress = torrent.Progress;
                        lastAmountLeft = torrent.AmountLeft;
                        lastName = torrent.Name;
                    }

                    if (torrent is not null && predicate(torrent))
                    {
                        return;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
                }
            }
            catch (TaskCanceledException)
            {
            }

            throw new TimeoutException($"Timed out waiting for torrent '{hash}' to reach the expected state. Last seen name='{lastName}', state='{lastState}', progress='{lastProgress}', amountLeft='{lastAmountLeft}'.");
        }

        private static async Task EnsureSuccessAsync(Task<ApiResult> resultTask, string operation)
        {
            var result = await resultTask;
            if (result.IsSuccess)
            {
                return;
            }

            throw new InvalidOperationException($"Failed to {operation}: {result.Failure?.UserMessage ?? "Unknown error"}");
        }

        private static async Task<T> GetRequiredValueAsync<T>(Task<ApiResult<T>> resultTask, string operation) where T : notnull
        {
            var result = await resultTask;
            if (result.TryGetValue(out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"Failed to {operation}: {result.Failure?.UserMessage ?? "Unknown error"}");
        }

        private static async Task<TValue> GetRequiredValueAsync<TValue, TPending>(Task<ApiResult<TValue, TPending>> resultTask, string operation) where TValue : notnull where TPending : notnull
        {
            var result = await resultTask;
            if (result.TryGetSuccessValue(out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"Failed to {operation}: {result.Failure?.UserMessage ?? "Unknown error"}");
        }

        private static bool IsCheckingState(TorrentState? state)
        {
            return state is TorrentState.CheckingUploading
                or TorrentState.CheckingDownloading
                or TorrentState.CheckingResumeData;
        }

        private static bool IsCompletedState(TorrentState? state)
        {
            return state is TorrentState.Uploading
                or TorrentState.StalledUploading
                or TorrentState.QueuedUploading
                or TorrentState.ForcedUploading
                or TorrentState.StoppedUploading;
        }

        private static bool IsIncompleteState(TorrentState? state)
        {
            return state is TorrentState.StoppedDownloading
                or TorrentState.StalledDownloading
                or TorrentState.QueuedDownloading
                or TorrentState.ForcedDownloading
                or TorrentState.Downloading;
        }

        private static bool IsMissingLikeState(TorrentState? state)
        {
            return state is TorrentState.MissingFiles
                || IsIncompleteState(state);
        }

        private static async Task<FixtureManifest> LoadManifestAsync(string repoRoot)
        {
            var manifestPath = Path.Combine(repoRoot, "tools", "ReadmeScreenshots", "readme-fixtures", "manifest.json");
            var manifest = JsonSerializer.Deserialize<FixtureManifest>(await File.ReadAllTextAsync(manifestPath), _jsonOptions);
            return manifest ?? throw new InvalidOperationException($"Could not load fixture manifest from '{manifestPath}'.");
        }

        private static byte[] CreateSingleFileTorrent(string payloadPath, string contentName, string comment)
        {
            if (!File.Exists(payloadPath))
            {
                throw new FileNotFoundException("Fixture payload not found.", payloadPath);
            }

            const int pieceLength = 16 * 1024;
            var fileInfo = new FileInfo(payloadPath);
            var pieces = BuildPieces(payloadPath, pieceLength);

            var info = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["length"] = fileInfo.Length,
                ["name"] = contentName,
                ["piece length"] = pieceLength,
                ["pieces"] = pieces
            };

            var torrent = new SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                ["comment"] = comment,
                ["created by"] = "qbtmud README screenshot fixtures",
                ["creation date"] = _torrentCreationDate.ToUnixTimeSeconds(),
                ["info"] = info
            };

            using var stream = new MemoryStream();
            WriteBencode(stream, torrent);
            return stream.ToArray();
        }

        private static byte[] BuildPieces(string payloadPath, int pieceLength)
        {
            using var input = File.OpenRead(payloadPath);
            using var pieces = new MemoryStream();
            var buffer = new byte[pieceLength];

            while (true)
            {
                var bytesRead = input.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                var hash = SHA1.HashData(buffer.AsSpan(0, bytesRead));
                pieces.Write(hash, 0, hash.Length);
            }

            return pieces.ToArray();
        }

        private static void WriteBencode(Stream stream, object value)
        {
            switch (value)
            {
                case string stringValue:
                    WriteBytes(stream, Encoding.UTF8.GetBytes(stringValue));
                    return;

                case byte[] bytesValue:
                    WriteBytes(stream, bytesValue);
                    return;

                case int intValue:
                    WriteInteger(stream, intValue);
                    return;

                case long longValue:
                    WriteInteger(stream, longValue);
                    return;

                case IReadOnlyDictionary<string, object> dictionaryValue:
                    stream.WriteByte((byte)'d');
                    foreach (var pair in dictionaryValue)
                    {
                        WriteBencode(stream, pair.Key);
                        WriteBencode(stream, pair.Value);
                    }
                    stream.WriteByte((byte)'e');
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported bencode value type '{value.GetType().FullName}'.");
            }
        }

        private static void WriteInteger(Stream stream, long value)
        {
            var bytes = Encoding.ASCII.GetBytes($"i{value}e");
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteBytes(Stream stream, byte[] bytes)
        {
            var prefix = Encoding.ASCII.GetBytes($"{bytes.Length}:");
            stream.Write(prefix, 0, prefix.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string GetRequiredOption(string[] args, string name)
        {
            var value = GetOptionalOption(args, name);
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Missing required option '{name}'.")
                : value;
        }

        private static string? GetOptionalOption(string[] args, string name)
        {
            for (var index = 1; index < args.Length; index++)
            {
                var arg = args[index];
                if (string.Equals(arg, name, StringComparison.Ordinal))
                {
                    return index + 1 < args.Length ? args[index + 1] : null;
                }

                if (arg.StartsWith($"{name}=", StringComparison.Ordinal))
                {
                    return arg[(name.Length + 1)..];
                }
            }

            return null;
        }

        private static int Fail(string message)
        {
            Console.Error.WriteLine(message);
            WriteUsage();
            return 1;
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ReadmeScreenshots generate-fixtures --repo-root <path>");
            Console.WriteLine("  ReadmeScreenshots hash-password --password <value>");
            Console.WriteLine("  ReadmeScreenshots seed --repo-root <path> --api-base-url <url> --download-root <path> --capture-state <path> [--username <value>] [--password <value>]");
            Console.WriteLine("  ReadmeScreenshots capture --app-base-url <url> --output-dir <path> --capture-state <path> --username <value> --password <value> [--edge-executable <path>] [--browser-channel <channel>]");
        }

        private sealed class FixtureManifest
        {
            [JsonPropertyName("detailFixtureId")]
            public string DetailFixtureId { get; set; } = string.Empty;

            [JsonPropertyName("fixtures")]
            public List<FixtureDefinition> Fixtures { get; set; } = [];
        }

        private sealed class FixtureDefinition
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("payloadFile")]
            public string PayloadFile { get; set; } = string.Empty;

            [JsonPropertyName("category")]
            public string Category { get; set; } = string.Empty;

            [JsonPropertyName("tags")]
            public List<string> Tags { get; set; } = [];

            [JsonPropertyName("state")]
            [JsonConverter(typeof(JsonStringEnumConverter<FixtureState>))]
            public FixtureState State { get; set; }

            [JsonPropertyName("comment")]
            public string Comment { get; set; } = string.Empty;
        }

        private sealed class ReadmeCaptureState
        {
            [JsonPropertyName("detailHash")]
            public string DetailHash { get; set; } = string.Empty;

            [JsonPropertyName("fixtures")]
            public List<SeededFixture> Fixtures { get; set; } = [];
        }

        private sealed class SeededFixture
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("hash")]
            public string Hash { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            [JsonConverter(typeof(JsonStringEnumConverter<FixtureState>))]
            public FixtureState State { get; set; }
        }

        private enum FixtureState
        {
            [JsonStringEnumMemberName("complete")]
            Complete,

            [JsonStringEnumMemberName("stopped")]
            Stopped,

            [JsonStringEnumMemberName("missing")]
            Missing
        }
    }
}
