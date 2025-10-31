using AwesomeAssertions;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Moq;
using MudBlazor;
using MudCategory = Lantean.QBTMud.Models.Category;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class DialogWorkflowTests
    {
        private readonly Mock<IDialogService> _dialogService;
        private readonly Mock<IApiClient> _apiClient;
        private readonly Mock<ISnackbar> _snackbar;
        private readonly IDialogWorkflow _target;

        public DialogWorkflowTests()
        {
            _dialogService = new Mock<IDialogService>(MockBehavior.Strict);
            _apiClient = new Mock<IApiClient>(MockBehavior.Strict);
            _snackbar = new Mock<ISnackbar>();

            _target = new DialogWorkflow(_dialogService.Object, _apiClient.Object, _snackbar.Object);
        }

        [Fact]
        public async Task GIVEN_CategoryCreated_WHEN_InvokeAddCategoryDialog_THEN_ShouldCallApi()
        {
            var reference = CreateReference(DialogResult.Ok(new MudCategory("CategoryName", "SavePath")));
            _dialogService
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Add Category", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.AddCategory("CategoryName", "SavePath"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeAddCategoryDialog();

            result.Should().Be("CategoryName");
            _apiClient.Verify();
        }

        [Fact]
        public async Task GIVEN_LinkOptions_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldAddTorrent()
        {
            var options = new AddTorrentLinkOptions("http://one", CreateTorrentOptions());
            var reference = CreateReference(DialogResult.Ok(options));
            _dialogService
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            _apiClient
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(1, 0))
                .Verifiable();

            await _target.InvokeAddTorrentLinkDialog();

            _apiClient.Verify();
            _snackbar.Invocations.Count(invocation => invocation.Method.Name == nameof(ISnackbar.Add)).Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_DeleteWithoutConfirmation_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldDelete()
        {
            _apiClient
                .Setup(a => a.DeleteTorrents(It.IsAny<bool?>(), false, It.Is<string[]>(hashes => hashes.Length == 1 && hashes[0] == "hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(false, "hash");

            result.Should().BeTrue();
            _apiClient.Verify();
        }

        private static IDialogReference CreateReference(DialogResult result)
        {
            var reference = new Mock<IDialogReference>();
            reference.Setup(r => r.Result).ReturnsAsync(result);
            return reference.Object;
        }

        private static TorrentOptions CreateTorrentOptions()
        {
            return new TorrentOptions(
                torrentManagementMode: false,
                savePath: "SavePath",
                cookie: null,
                renameTorrent: "Rename",
                category: "Category",
                startTorrent: true,
                addToTopOfQueue: true,
                stopCondition: "None",
                skipHashCheck: false,
                contentLayout: "Original",
                downloadInSequentialOrder: false,
                downloadFirstAndLastPiecesFirst: false,
                downloadLimit: 0,
                uploadLimit: 0);
        }
    }
}