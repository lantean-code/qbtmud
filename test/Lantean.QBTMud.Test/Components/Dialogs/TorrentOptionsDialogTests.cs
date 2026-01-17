using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class TorrentOptionsDialogTests : RazorComponentTestBase<TorrentOptionsDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly TorrentOptionsDialogTestDriver _target;

        public TorrentOptionsDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new TorrentOptionsDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_TorrentMissing_WHEN_SaveClicked_THEN_ResultOk()
        {
            var mainData = CreateMainData(new Dictionary<string, Torrent>());
            var preferences = CreatePreferences("TempPath");

            var dialog = await _target.RenderDialogAsync(mainData, preferences, "Hash");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "TorrentOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_TorrentPresent_WHEN_CancelClicked_THEN_ResultCanceled()
        {
            var torrent = CreateTorrent("Hash", true, "SavePath");
            var mainData = CreateMainData(new Dictionary<string, Torrent>
            {
                { "Hash", torrent },
            });
            var preferences = CreatePreferences("TempPath");

            var dialog = await _target.RenderDialogAsync(mainData, preferences, "Hash");

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "TorrentOptionsCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultOk()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter" && !e.CtrlKey), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            var mainData = CreateMainData(new Dictionary<string, Torrent>());
            var preferences = CreatePreferences("TempPath");

            var dialog = await _target.RenderDialogAsync(mainData, preferences, "Hash");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        private static MainData CreateMainData(Dictionary<string, Torrent> torrents)
        {
            return new MainData(
                torrents,
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static ClientModels.Preferences CreatePreferences(string tempPath)
        {
            var json = $"{{\"temp_path\":\"{tempPath}\"}}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, SerializerOptions.Options)!;
        }

        private static Torrent CreateTorrent(string hash, bool automaticTorrentManagement, string savePath)
        {
            return new Torrent(
                hash,
                1,
                2,
                automaticTorrentManagement,
                0.5f,
                "Category",
                3,
                4,
                "ContentPath",
                5,
                6,
                7,
                8,
                9,
                true,
                false,
                "InfoHashV1",
                "InfoHashV2",
                10,
                "MagnetUri",
                1.5f,
                11,
                "Name",
                12,
                13,
                14,
                15,
                1,
                0.5f,
                1.0f,
                2.0f,
                savePath,
                16,
                17,
                18,
                false,
                19,
                "State",
                false,
                new[] { "Tag" },
                20,
                21,
                "Tracker",
                1,
                false,
                false,
                false,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                1.0f,
                "DownloadPath",
                "RootPath",
                false,
                ClientModels.ShareLimitAction.Default,
                "Comment");
        }
    }

    internal sealed class TorrentOptionsDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public TorrentOptionsDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<TorrentOptionsDialogRenderContext> RenderDialogAsync(MainData mainData, ClientModels.Preferences preferences, string hash)
        {
            var provider = _testContext.Render<TorrentOptionsDialogHost>(parameters =>
            {
                parameters.Add(p => p.MainData, mainData);
                parameters.Add(p => p.Preferences, preferences);
            });

            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var dialogParameters = new DialogParameters
            {
                { nameof(TorrentOptionsDialog.Hash), hash },
            };

            var reference = await dialogService.ShowAsync<TorrentOptionsDialog>("Torrent Options", dialogParameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<TorrentOptionsDialog>();

            return new TorrentOptionsDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class TorrentOptionsDialogHost : ComponentBase
    {
        [Parameter]
        public MainData MainData { get; set; } = default!;

        [Parameter]
        public ClientModels.Preferences Preferences { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<MainData>>(0);
            builder.AddAttribute(1, "Value", MainData);
            builder.AddAttribute(2, "IsFixed", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<CascadingValue<ClientModels.Preferences>>(0);
                childBuilder.AddAttribute(1, "Value", Preferences);
                childBuilder.AddAttribute(2, "IsFixed", true);
                childBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(dialogBuilder =>
                {
                    dialogBuilder.OpenComponent<MudDialogProvider>(0);
                    dialogBuilder.CloseComponent();
                }));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    internal sealed class TorrentOptionsDialogRenderContext
    {
        public TorrentOptionsDialogRenderContext(
            IRenderedComponent<TorrentOptionsDialogHost> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<TorrentOptionsDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<TorrentOptionsDialogHost> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<TorrentOptionsDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
