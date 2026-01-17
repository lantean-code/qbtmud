using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class AddTorrentFileDialogTests : RazorComponentTestBase<AddTorrentFileDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddTorrentFileDialogTestDriver _target;

        public AddTorrentFileDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddTorrentFileDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoFiles_WHEN_SubmitInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentFileSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentFileClose");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FilesUploaded_WHEN_Rendered_THEN_FileListDisplayed()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var file = CreateBrowserFile("Name");
            await UploadFilesAsync(dialog.Component, new[] { file });

            FindComponentByTestId<MudCard>(dialog.Component, "AddTorrentFileList").Should().NotBeNull();
            dialog.Component.FindComponents<MudListItem<string>>().Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_FilesUploaded_WHEN_RemoveClicked_THEN_FileRemoved()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var fileOne = CreateBrowserFile("Name");
            var fileTwo = CreateBrowserFile("SecondName");
            await UploadFilesAsync(dialog.Component, new[] { fileOne, fileTwo });

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "RemoveTorrentFile-Name");
            await deleteButton.Find("button").ClickAsync(new MouseEventArgs());

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentFileSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentFileOptions)result.Data!;
            options.Files.Should().ContainSingle().Which.Should().Be(fileTwo);
        }

        [Fact]
        public async Task GIVEN_FileUploaded_WHEN_SubmitInvoked_THEN_ResultContainsFileOptions()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var file = CreateBrowserFile("Name");
            await UploadFilesAsync(dialog.Component, new[] { file });

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentFileSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentFileOptions)result.Data!;
            options.Files.Should().ContainSingle().Which.Should().Be(file);
        }

        [Fact]
        public async Task GIVEN_FileUploaded_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsFileOptions()
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

            var file = CreateBrowserFile("Name");
            await UploadFilesAsync(dialog.Component, new[] { file });

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentFileOptions)result.Data!;
            options.Files.Should().ContainSingle().Which.Should().Be(file);
        }

        private async Task UploadFilesAsync(IRenderedComponent<AddTorrentFileDialog> component, IReadOnlyList<IBrowserFile> files)
        {
            var upload = FindComponentByTestId<MudFileUpload<IReadOnlyList<IBrowserFile>>>(component, "AddTorrentFileUpload");
            await component.InvokeAsync(() => upload.Instance.FilesChanged.InvokeAsync(files));
        }

        private Mock<IApiClient> UseApiClientMock()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetAllCategories()).ReturnsAsync(new Dictionary<string, ClientModels.Category>());
            apiClientMock.Setup(c => c.GetAllTags()).ReturnsAsync(Array.Empty<string>());
            apiClientMock.Setup(c => c.GetApplicationPreferences()).ReturnsAsync(CreatePreferences());
            return apiClientMock;
        }

        private static ClientModels.Preferences CreatePreferences()
        {
            var json = "{\"auto_tmm_enabled\":false,\"save_path\":\"\",\"temp_path\":\"\",\"temp_path_enabled\":false,\"add_stopped_enabled\":false,\"add_to_top_of_queue\":true,\"torrent_stop_condition\":\"None\",\"torrent_content_layout\":\"Original\",\"max_ratio_enabled\":false,\"max_ratio\":1.0,\"max_seeding_time_enabled\":false,\"max_seeding_time\":0,\"max_inactive_seeding_time_enabled\":false,\"max_inactive_seeding_time\":0,\"max_ratio_act\":0}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static IBrowserFile CreateBrowserFile(string name)
        {
            var file = new Mock<IBrowserFile>(MockBehavior.Strict);
            file.Setup(f => f.Name).Returns(name);
            return file.Object;
        }
    }

    internal sealed class AddTorrentFileDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddTorrentFileDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<AddTorrentFileDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<AddTorrentFileDialog>("Upload local torrent");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTorrentFileDialog>();

            return new AddTorrentFileDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTorrentFileDialogRenderContext
    {
        public AddTorrentFileDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTorrentFileDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTorrentFileDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
