using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

using ClientModels = QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
{
    public sealed class AddTorrentLinkDialogTests : RazorComponentTestBase<AddTorrentLinkDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddTorrentLinkDialogTestDriver _target;
        private QBittorrentPreferences? _preferences;

        public AddTorrentLinkDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddTorrentLinkDialogTestDriver(TestContext, () => _preferences);
        }

        [Fact]
        public async Task GIVEN_NoUrls_WHEN_SubmitInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkClose");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_UrlParameter_WHEN_Rendered_THEN_UrlsSet()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync("Url");

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Instance.GetState(x => x.Value).Should().Be("Url");
        }

        [Fact]
        public async Task GIVEN_UrlsEntered_WHEN_SubmitInvoked_THEN_ResultContainsUrls()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Find("textarea").Change("Url\nSecondUrl");

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentLinkOptions)result.Data!;
            options.Urls.Should().BeEquivalentTo(new[] { "Url", "SecondUrl" });
        }

        [Fact]
        public async Task GIVEN_UrlsEntered_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsUrls()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter"), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Find("textarea").Change("Url");

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentLinkOptions)result.Data!;
            options.Urls.Should().ContainSingle().Which.Should().Be("Url");
        }

        private Mock<IApiClient> UseApiClientMock()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetAllCategoriesAsync()).ReturnsSuccessAsync(new Dictionary<string, ClientModels.Category>());
            apiClientMock.Setup(c => c.GetAllTagsAsync()).ReturnsSuccessAsync(Array.Empty<string>());
            apiClientMock.Setup(c => c.GetBuildInfoAsync()).ReturnsAsync(CreateBuildInfo());

            _preferences = CreatePreferences();

            return apiClientMock;
        }

        private static ApiResult<BuildInfo> CreateBuildInfo()
        {
            return ApiResult.CreateSuccess(new BuildInfo("QTVersion", "LibTorrentVersion", "BoostVersion", "OpenSSLVersion", "ZLibVersion", 64, BuildPlatform.Linux));
        }

        private static QBittorrentPreferences CreatePreferences()
        {
            return PreferencesFactory.CreateQBittorrentPreferences(spec =>
            {
                spec.AddStoppedEnabled = false;
                spec.AddToTopOfQueue = true;
                spec.AutoTmmEnabled = false;
                spec.MaxInactiveSeedingTime = 0;
                spec.MaxInactiveSeedingTimeEnabled = false;
                spec.MaxRatio = 1.0f;
                spec.MaxRatioAct = MaxRatioAction.StopTorrent;
                spec.MaxRatioEnabled = false;
                spec.MaxSeedingTime = 0;
                spec.MaxSeedingTimeEnabled = false;
                spec.SavePath = string.Empty;
                spec.TempPath = string.Empty;
                spec.TempPathEnabled = false;
                spec.TorrentContentLayout = TorrentContentLayout.Original;
                spec.TorrentStopCondition = StopCondition.None;
            });
        }
    }

    internal sealed class AddTorrentLinkDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;
        private readonly Func<QBittorrentPreferences?> _getPreferences;

        public AddTorrentLinkDialogTestDriver(ComponentTestContext testContext, Func<QBittorrentPreferences?> getPreferences)
        {
            _testContext = testContext;
            _getPreferences = getPreferences;
        }

        public async Task<AddTorrentLinkDialogRenderContext> RenderDialogAsync(string? url = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (url is not null)
            {
                parameters.Add(nameof(AddTorrentLinkDialog.Url), url);
            }

            var preferences = _getPreferences();
            if (preferences is not null)
            {
                parameters.Add(nameof(AddTorrentLinkDialog.Preferences), preferences);
            }

            var reference = await dialogService.ShowAsync<AddTorrentLinkDialog>("Upload torrent file", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTorrentLinkDialog>();

            return new AddTorrentLinkDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTorrentLinkDialogRenderContext
    {
        public AddTorrentLinkDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTorrentLinkDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTorrentLinkDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
