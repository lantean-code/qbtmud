using Microsoft.Playwright;

namespace ReadmeScreenshots
{
    internal static class ScreenshotLogin
    {
        /// <summary>
        /// Logs in through the production login form when authentication is required.
        /// </summary>
        /// <param name="page">The capture page.</param>
        /// <param name="appBaseUrl">The application base URL.</param>
        /// <param name="username">The disposable capture username.</param>
        /// <param name="password">The disposable capture password.</param>
        /// <returns>A task that completes after the authenticated home page is ready.</returns>
        public static async Task EnsureLoggedInAsync(IPage page, string appBaseUrl, string username, string password)
        {
            var usernameField = page.Locator("#username");
            if (!IsLoginRoute(page.Url) && !await IsVisibleAsync(usernameField))
            {
                return;
            }

            await usernameField.FillAsync(username);
            await page.Locator("#password").FillAsync(password);
            await page.Locator("form button[type='submit']").ClickAsync();
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
    }
}
