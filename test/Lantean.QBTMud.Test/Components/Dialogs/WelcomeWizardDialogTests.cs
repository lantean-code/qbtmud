using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class WelcomeWizardDialogTests : RazorComponentTestBase<WelcomeWizardDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly IThemeManagerService _themeManagerService;
        private readonly Mock<IThemeManagerService> _themeManagerServiceMock;
        private readonly ISnackbar _snackbar;
        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly IWebUiLanguageCatalog _languageCatalog;
        private readonly Mock<IWebUiLanguageCatalog> _languageCatalogMock;
        private readonly TestNavigationManager _navigationManager;
        private readonly WelcomeWizardDialogTestDriver _target;

        public WelcomeWizardDialogTests()
        {
            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll<Microsoft.AspNetCore.Components.NavigationManager>();
            TestContext.Services.AddSingleton<Microsoft.AspNetCore.Components.NavigationManager>(_navigationManager);

            _apiClient = Mock.Of<IApiClient>();
            _apiClientMock = Mock.Get(_apiClient);
            _apiClientMock
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .Returns(Task.CompletedTask);

            _themeManagerService = Mock.Of<IThemeManagerService>();
            _themeManagerServiceMock = Mock.Get(_themeManagerService);
            _themeManagerServiceMock
                .Setup(service => service.EnsureInitialized())
                .Returns(Task.CompletedTask);
            _themeManagerServiceMock
                .SetupGet(service => service.Themes)
                .Returns(new List<ThemeCatalogItem>
                {
                    CreateTheme("theme1", "Theme1"),
                    CreateTheme("theme2", "Theme2"),
                });
            _themeManagerServiceMock
                .SetupGet(service => service.CurrentThemeId)
                .Returns("theme1");
            _themeManagerServiceMock
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _snackbar = Mock.Of<ISnackbar>();
            _snackbarMock = Mock.Get(_snackbar);

            _languageCatalog = Mock.Of<IWebUiLanguageCatalog>();
            _languageCatalogMock = Mock.Get(_languageCatalog);
            _languageCatalogMock
                .SetupGet(catalog => catalog.Languages)
                .Returns(new List<WebUiLanguageCatalogItem>
                {
                    new("en", "English"),
                    new("fr", "Francais"),
                });
            _languageCatalogMock
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.RemoveAll<IWebUiLanguageCatalog>();
            TestContext.Services.RemoveAll<IKeyboardService>();

            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.AddSingleton(_languageCatalog);

            _target = new WelcomeWizardDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_InitialLocaleProvided_WHEN_Rendered_THEN_SelectsResolvedLocale()
        {
            var dialog = await _target.RenderDialogAsync("fr-FR");

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            languageSelect.Instance.Value.Should().Be("fr");
        }

        [Fact]
        public async Task GIVEN_FirstStepActive_WHEN_Rendered_THEN_BackDisabledAndLanguageSelectVisible()
        {
            var dialog = await _target.RenderDialogAsync();

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            backButton.Instance.Disabled.Should().BeTrue();

            FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_LanguageStep_WHEN_NextClicked_THEN_ShowsThemeStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
            {
                FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_LanguageStep_WHEN_BackInvoked_THEN_StaysOnLanguageStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            await dialog.Component.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_BackClicked_THEN_ShowsLanguageStep()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var backButton = FindButton(dialog.Component, "WelcomeWizardBack");
            await backButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
            {
                FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_LanguageSelected_WHEN_ValueChanged_THEN_UpdatesPreferences()
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");

            var previousCurrentCulture = CultureInfo.CurrentCulture;
            var previousCurrentUiCulture = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
            var previousUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

            try
            {
                await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCurrentCulture;
                CultureInfo.CurrentUICulture = previousCurrentUiCulture;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUiCulture;
            }

            _apiClientMock.Verify(client => client.SetApplicationPreferences(It.Is<UpdatePreferences>(preferences =>
                string.Equals(preferences.Locale, "fr", StringComparison.Ordinal))), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GIVEN_LocaleEmpty_WHEN_LocaleChanged_THEN_DoesNotUpdatePreferences(string? locale)
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync(locale));

            _apiClientMock.Verify(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()), Times.Never);
        }

        [Theory]
        [InlineData("@")]
        [InlineData("en@")]
        [InlineData("en@latin")]
        [InlineData("en@cyrillic")]
        [InlineData("en@Abcd")]
        [InlineData("en@foo")]
        [InlineData("invalid$$")]
        public async Task GIVEN_LocaleVariant_WHEN_LocaleChanged_THEN_UpdatesPreferences(string locale)
        {
            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");

            var previousCurrentCulture = CultureInfo.CurrentCulture;
            var previousCurrentUiCulture = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
            var previousUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

            try
            {
                await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync(locale));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCurrentCulture;
                CultureInfo.CurrentUICulture = previousCurrentUiCulture;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUiCulture;
            }

            _apiClientMock.Verify(client => client.SetApplicationPreferences(It.Is<UpdatePreferences>(preferences =>
                string.Equals(preferences.Locale, locale, StringComparison.Ordinal))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeSelected_THEN_AppliesTheme()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            _themeManagerServiceMock.Verify(service => service.ApplyTheme("theme2"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeSelectionIsEmpty_THEN_DoesNotApplyTheme()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync(" "));

            _themeManagerServiceMock.Verify(service => service.ApplyTheme(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ApplyThemeThrows_THEN_ShowsSnackbarError()
        {
            _themeManagerServiceMock
                .Setup(service => service.ApplyTheme(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("theme2"));

            _snackbarMock.Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeStep_WHEN_ThemeIdMissing_THEN_DoneStepRenders()
        {
            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            await dialog.Component.InvokeAsync(() => themeSelect.Instance.ValueChanged.InvokeAsync("missing"));

            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            FindButton(dialog.Component, "WelcomeWizardFinish").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_LastStep_WHEN_FinishClicked_THEN_StoresCompletionAndClosesDialog()
        {
            await TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);

            var stored = await TestContext.LocalStorage.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_LastStep_WHEN_OpenOptionsClicked_THEN_NavigatesAndClosesDialog()
        {
            await TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var openOptionsButton = FindButton(dialog.Component, "WelcomeWizardOpenOptions");
            await openOptionsButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();

            _navigationManager.LastUri.Should().EndWith("/settings");

            var stored = await TestContext.LocalStorage.GetItemAsync<bool?>(WelcomeWizardStorageKeys.Completed, Xunit.TestContext.Current.CancellationToken);
            stored.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FinishWriteFails_WHEN_FinishClicked_THEN_ShowsSnackbarError()
        {
            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorage
                .Setup(service => service.SetItemAsync<bool>(WelcomeWizardStorageKeys.Completed, true, It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Message"));

            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton<ILocalStorageService>(localStorage.Object);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var finishButton = FindButton(dialog.Component, "WelcomeWizardFinish");
            await finishButton.Find("button").ClickAsync(new MouseEventArgs());

            _snackbarMock.Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageUpdateFails_WHEN_LocaleSelected_THEN_ShowsSnackbarError()
        {
            _apiClientMock
                .Setup(client => client.SetApplicationPreferences(It.IsAny<UpdatePreferences>()))
                .ThrowsAsync(new HttpRequestException("Message"));

            var dialog = await _target.RenderDialogAsync();

            var languageSelect = FindSelect<string>(dialog.Component, "WelcomeWizardLanguageSelect");
            await dialog.Component.InvokeAsync(() => languageSelect.Instance.ValueChanged.InvokeAsync("fr"));

            _snackbarMock.Verify(snackbar => snackbar.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeIdUnsetAndLanguageUnset_WHEN_DoneStepRendered_THEN_RendersSuccessfully()
        {
            _themeManagerServiceMock
                .SetupGet(service => service.Themes)
                .Returns(new List<ThemeCatalogItem>());
            _themeManagerServiceMock
                .SetupGet(service => service.CurrentThemeId)
                .Returns((string?)null);

            _languageCatalogMock
                .SetupGet(catalog => catalog.Languages)
                .Returns(new List<WebUiLanguageCatalogItem>());

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            FindButton(dialog.Component, "WelcomeWizardFinish").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_CurrentThemeIdMissing_WHEN_Rendered_THEN_SelectsFirstTheme()
        {
            _themeManagerServiceMock
                .SetupGet(service => service.CurrentThemeId)
                .Returns((string?)null);

            var dialog = await _target.RenderDialogAsync();

            var nextButton = FindButton(dialog.Component, "WelcomeWizardNext");
            await nextButton.Find("button").ClickAsync(new MouseEventArgs());

            var themeSelect = FindSelect<string>(dialog.Component, "WelcomeWizardThemeSelect");
            themeSelect.Instance.Value.Should().Be("theme1");
        }

        private static ThemeCatalogItem CreateTheme(string id, string name)
        {
            var definition = new ThemeDefinition
            {
                Id = id,
                Name = name
            };

            return new ThemeCatalogItem(id, name, definition, ThemeSource.Local, sourcePath: null);
        }
    }

    internal sealed class TestNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        public string LastUri { get; private set; } = "http://localhost/";

        protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options)
        {
            var absolute = ToAbsoluteUri(uri).ToString();
            LastUri = absolute;
            Uri = absolute;
        }
    }

    internal sealed class WelcomeWizardDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public WelcomeWizardDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<WelcomeWizardDialogRenderContext> RenderDialogAsync(string? initialLocale = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (!string.IsNullOrWhiteSpace(initialLocale))
            {
                parameters.Add(nameof(WelcomeWizardDialog.InitialLocale), initialLocale);
            }

            var reference = await dialogService.ShowAsync<WelcomeWizardDialog>(title: null, parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<WelcomeWizardDialog>();

            return new WelcomeWizardDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class WelcomeWizardDialogRenderContext
    {
        public WelcomeWizardDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<WelcomeWizardDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<WelcomeWizardDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
