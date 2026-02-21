using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class CreateTorrentDialogTests : RazorComponentTestBase<CreateTorrentDialog>
    {
        private const string StorageKey = "TorrentCreator.FormState";
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly CreateTorrentDialogTestDriver _target;

        public CreateTorrentDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();
            TestContext.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_snackbar);
            _target = new CreateTorrentDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoSourcePath_WHEN_SubmitClicked_THEN_ShowsWarningAndKeepsDialogOpen()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            var dialog = await _target.RenderDialogAsync();
            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");

            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Source path is required.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
            dialog.Reference.Result.IsCompleted.Should().BeFalse();

            var sourcePath = dialog.Component.FindComponents<PathAutocomplete>().First();
            sourcePath.Instance.ForceValidation.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FormatSupported_WHEN_SubmitClicked_THEN_ReturnsRequestWithFormat()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.1.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetTorrentFilePathAsync(dialog.Component, "C:/Out");
            await SetPieceSizeAsync(dialog.Component, 65536);
            await SetFormatAsync(dialog.Component, "v1");
            await SetFieldSwitchAsync(dialog.Component, "PrivateTorrent", true);
            await SetFieldSwitchAsync(dialog.Component, "StartSeeding", false);
            await SetTextFieldAsync(dialog.Component, "TrackerUrls", "http://a\n\n http://b ");
            await SetTextFieldAsync(dialog.Component, "WebSeedUrls", "http://c\r\nhttp://d");
            await SetTextFieldAsync(dialog.Component, "Comments", " Comment ");
            await SetTextFieldAsync(dialog.Component, "Source", " Source ");

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            var request = result!.Data.Should().BeOfType<TorrentCreationTaskRequest>().Subject;

            request.SourcePath.Should().Be("C:/Source");
            request.TorrentFilePath.Should().Be("C:/Out");
            request.PieceSize.Should().Be(65536);
            request.Private.Should().BeTrue();
            request.StartSeeding.Should().BeFalse();
            request.Comment.Should().Be("Comment");
            request.Source.Should().Be("Source");
            request.Format.Should().Be("v1");
            request.OptimizeAlignment.Should().BeNull();
            request.PaddedFileSizeLimit.Should().BeNull();
            request.Trackers.Should().BeEquivalentTo(new[] { "http://a", "http://b" });
            request.UrlSeeds.Should().BeEquivalentTo(new[] { "http://c", "http://d" });
        }

        [Fact]
        public async Task GIVEN_FormatUnsupported_WHEN_SubmitClickedWithPaddingDisabled_THEN_RequestHasNullPadding()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("1.2.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetFieldSwitchAsync(dialog.Component, "OptimizeAlignment", false);
            await SetNumericFieldAsync(dialog.Component, "PaddedFileSizeLimit", 16);

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            var request = result!.Data.Should().BeOfType<TorrentCreationTaskRequest>().Subject;

            request.OptimizeAlignment.Should().BeFalse();
            request.PaddedFileSizeLimit.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FormatUnsupported_WHEN_SubmitClickedWithPaddingEnabled_THEN_RequestHasConvertedPadding()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("1.9.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetFieldSwitchAsync(dialog.Component, "OptimizeAlignment", true);
            await SetNumericFieldAsync(dialog.Component, "PaddedFileSizeLimit", -1);

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            var request = result!.Data.Should().BeOfType<TorrentCreationTaskRequest>().Subject;

            request.OptimizeAlignment.Should().BeTrue();
            request.PaddedFileSizeLimit.Should().Be(-1);
        }

        [Fact]
        public async Task GIVEN_PaddedLimitOverflow_WHEN_SubmitClicked_THEN_RequestUsesMaxValue()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("1.9.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetFieldSwitchAsync(dialog.Component, "OptimizeAlignment", true);
            await SetNumericFieldAsync(dialog.Component, "PaddedFileSizeLimit", 2097152);

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            var request = result!.Data.Should().BeOfType<TorrentCreationTaskRequest>().Subject;

            request.PaddedFileSizeLimit.Should().Be(int.MaxValue);
        }

        [Fact]
        public async Task GIVEN_CloseClicked_WHEN_Invoked_THEN_CancelsDialog()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            var dialog = await _target.RenderDialogAsync();
            var closeButton = FindButton(dialog.Component, "CreateTorrentClose");

            await dialog.Component.InvokeAsync(() => closeButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_SubmitsDialog()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = new Mock<IKeyboardService>(MockBehavior.Strict);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((criteria, handler) =>
                {
                    if (criteria.Key == "Enter" && !criteria.CtrlKey)
                    {
                        submitHandler = handler;
                    }
                })
                .Returns(Task.CompletedTask);
            keyboardMock
                .Setup(service => service.Focus())
                .Returns(Task.CompletedTask);
            keyboardMock
                .Setup(service => service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()))
                .Returns(Task.CompletedTask);
            keyboardMock
                .Setup(service => service.UnFocus())
                .Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(keyboardMock.Object);

            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StoredFormState_WHEN_Rendered_THEN_FieldsHydrated()
        {
            var state = new TorrentCreationFormState
            {
                SourcePath = "SourcePath",
                TorrentFilePath = "TorrentFilePath",
                PieceSize = 65536,
                Private = true,
                StartSeeding = false,
                Trackers = "Trackers",
                UrlSeeds = "UrlSeeds",
                Comment = "Comment",
                Source = "Source",
                Format = "v2",
                OptimizeAlignment = false,
                PaddedFileSizeLimit = 2048
            };

            await TestContext.LocalStorage.SetItemAsync(StorageKey, state, Xunit.TestContext.Current.CancellationToken);
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            var dialog = await _target.RenderDialogAsync();

            var pathFields = dialog.Component.FindComponents<PathAutocomplete>();
            pathFields[0].Instance.Value.Should().Be("SourcePath");
            pathFields[1].Instance.Value.Should().Be("TorrentFilePath");

            var pieceSelect = dialog.Component.FindComponent<MudSelect<int?>>();
            pieceSelect.Instance.Value.Should().Be(65536);

            var privateSwitch = FindFieldSwitch(dialog.Component, "PrivateTorrent");
            privateSwitch.Instance.Value.Should().BeTrue();

            var seedSwitch = FindFieldSwitch(dialog.Component, "StartSeeding");
            seedSwitch.Instance.Value.Should().BeFalse();

            var formatSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "TorrentFormat");
            formatSelect.Instance.Value.Should().Be("v2");

            var trackers = FindTextField(dialog.Component, "TrackerUrls");
            trackers.Instance.Value.Should().Be("Trackers");

            var urlSeeds = FindTextField(dialog.Component, "WebSeedUrls");
            urlSeeds.Instance.Value.Should().Be("UrlSeeds");

            var comment = FindTextField(dialog.Component, "Comments");
            comment.Instance.Value.Should().Be("Comment");

            var source = FindTextField(dialog.Component, "Source");
            source.Instance.Value.Should().Be("Source");
        }

        [Fact]
        public async Task GIVEN_StoredFormStateWithWhitespaceFormatAndNegativePadding_WHEN_Rendered_THEN_UsesDefaults()
        {
            var state = new TorrentCreationFormState
            {
                SourcePath = "SourcePath",
                Format = " ",
                OptimizeAlignment = true,
                PaddedFileSizeLimit = -1
            };

            await TestContext.LocalStorage.SetItemAsync(StorageKey, state, Xunit.TestContext.Current.CancellationToken);
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("1.2.0"));

            var dialog = await _target.RenderDialogAsync();

            var formatSelect = dialog.Component.FindComponents<MudSelect<string>>()
                .Any(select => HasTestId(select, "TorrentFormat"));
            formatSelect.Should().BeFalse();

            var paddedLimit = FindNumericField(dialog.Component, "PaddedFileSizeLimit");
            paddedLimit.Instance.Value.Should().Be(-1);
        }

        [Fact]
        public async Task GIVEN_WhitespaceOnlyTrackersAndWebSeeds_WHEN_SubmitClicked_THEN_RequestListsAreNull()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.1.0"));

            var dialog = await _target.RenderDialogAsync();
            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetTextFieldAsync(dialog.Component, "TrackerUrls", " \n \r\n ");
            await SetTextFieldAsync(dialog.Component, "WebSeedUrls", " \n \r\n ");

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            var request = result!.Data.Should().BeOfType<TorrentCreationTaskRequest>().Subject;
            request.Trackers.Should().BeNull();
            request.UrlSeeds.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_InvalidVersion_WHEN_Rendered_THEN_FormatSelectHidden()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo(" "));

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponents<MudSelect<string>>()
                .Any(select => HasTestId(select, "TorrentFormat"))
                .Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_VersionStartsWithText_WHEN_Rendered_THEN_FormatSelectHidden()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("x2.0"));

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponents<MudSelect<string>>()
                .Any(select => HasTestId(select, "TorrentFormat"))
                .Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_VersionTooLong_WHEN_Rendered_THEN_FormatSelectShown()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0.0.0"));

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponents<MudSelect<string>>()
                .Any(select => HasTestId(select, "TorrentFormat"))
                .Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_LoadFormStateThrows_WHEN_Rendered_THEN_ShowsWarning()
        {
            var localStorage = new Mock<ILocalStorageService>(MockBehavior.Strict);
            localStorage
                .Setup(storage => storage.GetItemAsync<TorrentCreationFormState>(StorageKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failed"));
            localStorage
                .Setup(storage => storage.SetItemAsync(StorageKey, It.IsAny<TorrentCreationFormState>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            TestContext.Services.RemoveAll<ILocalStorageService>();
            TestContext.Services.AddSingleton(localStorage.Object);

            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            await _target.RenderDialogAsync();

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to load saved torrent creator settings: Failed", Severity.Warning, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SubmitValid_WHEN_Clicked_THEN_PersistsFormState()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(CreateBuildInfo("2.0.0"));

            var dialog = await _target.RenderDialogAsync();

            await SetSourcePathAsync(dialog.Component, "C:/Source");
            await SetTorrentFilePathAsync(dialog.Component, "C:/Out");
            await SetTextFieldAsync(dialog.Component, "TrackerUrls", "Tracker");
            await SetTextFieldAsync(dialog.Component, "WebSeedUrls", "Seed");
            await SetTextFieldAsync(dialog.Component, "Comments", "Comment");
            await SetTextFieldAsync(dialog.Component, "Source", "Source");
            await SetFormatAsync(dialog.Component, "hybrid");

            var createButton = FindButton(dialog.Component, "CreateTorrentSubmit");
            await dialog.Component.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            var stored = await TestContext.LocalStorage.GetItemAsync<TorrentCreationFormState>(StorageKey, Xunit.TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.SourcePath.Should().Be("C:/Source");
            stored.TorrentFilePath.Should().Be("C:/Out");
            stored.Trackers.Should().Be("Tracker");
            stored.UrlSeeds.Should().Be("Seed");
            stored.Comment.Should().Be("Comment");
            stored.Source.Should().Be("Source");
            stored.Format.Should().Be("hybrid");
        }

        [Fact]
        public async Task GIVEN_BuildInfoThrows_WHEN_Rendered_THEN_UsesAlignmentFields()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ThrowsAsync(new InvalidOperationException());

            var dialog = await _target.RenderDialogAsync();

            var alignmentSwitch = FindFieldSwitch(dialog.Component, "OptimizeAlignment");
            alignmentSwitch.Should().NotBeNull();

            var numericField = FindNumericField(dialog.Component, "PaddedFileSizeLimit");
            numericField.Should().NotBeNull();
        }

        private static BuildInfo CreateBuildInfo(string libTorrentVersion)
        {
            return new BuildInfo("QT", libTorrentVersion, "Boost", "OpenSSL", "ZLib", 64);
        }

        private static IRenderedComponent<FieldSwitch> FindFieldSwitch(IRenderedComponent<CreateTorrentDialog> component, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(component, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<CreateTorrentDialog> component, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(component, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumericField(IRenderedComponent<CreateTorrentDialog> component, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(component, testId);
        }

        private static async Task SetSourcePathAsync(IRenderedComponent<CreateTorrentDialog> component, string value)
        {
            var pathField = FindComponentByTestId<PathAutocomplete>(component, "SourcePath");
            await component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetTorrentFilePathAsync(IRenderedComponent<CreateTorrentDialog> component, string value)
        {
            var pathField = FindComponentByTestId<PathAutocomplete>(component, "TorrentFilePath");
            await component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetPieceSizeAsync(IRenderedComponent<CreateTorrentDialog> component, int value)
        {
            var select = FindComponentByTestId<MudSelect<int?>>(component, "PieceSize");
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetFormatAsync(IRenderedComponent<CreateTorrentDialog> component, string value)
        {
            var select = FindComponentByTestId<MudSelect<string>>(component, "TorrentFormat");
            await component.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetFieldSwitchAsync(IRenderedComponent<CreateTorrentDialog> component, string testId, bool value)
        {
            var fieldSwitch = FindFieldSwitch(component, testId);
            await component.InvokeAsync(() => fieldSwitch.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetTextFieldAsync(IRenderedComponent<CreateTorrentDialog> component, string testId, string value)
        {
            var field = FindTextField(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private static async Task SetNumericFieldAsync(IRenderedComponent<CreateTorrentDialog> component, string testId, int value)
        {
            var field = FindNumericField(component, testId);
            await component.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync(value));
        }

        private sealed class CreateTorrentDialogTestDriver
        {
            private readonly ComponentTestContext _testContext;

            public CreateTorrentDialogTestDriver(ComponentTestContext testContext)
            {
                _testContext = testContext;
            }

            public async Task<CreateTorrentDialogRenderContext> RenderDialogAsync()
            {
                var provider = _testContext.Render<MudDialogProvider>();
                var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

                var reference = await dialogService.ShowAsync<CreateTorrentDialog>("Create Torrent", DialogWorkflow.FormDialogOptions);

                var dialog = provider.FindComponent<MudDialog>();
                var component = provider.FindComponent<CreateTorrentDialog>();

                return new CreateTorrentDialogRenderContext(provider, dialog, component, reference);
            }
        }

        private sealed class CreateTorrentDialogRenderContext
        {
            public CreateTorrentDialogRenderContext(
                IRenderedComponent<MudDialogProvider> provider,
                IRenderedComponent<MudDialog> dialog,
                IRenderedComponent<CreateTorrentDialog> component,
                IDialogReference reference)
            {
                Provider = provider;
                Dialog = dialog;
                Component = component;
                Reference = reference;
            }

            public IRenderedComponent<MudDialogProvider> Provider { get; }

            public IRenderedComponent<MudDialog> Dialog { get; }

            public IRenderedComponent<CreateTorrentDialog> Component { get; }

            public IDialogReference Reference { get; }
        }
    }
}
