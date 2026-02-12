using AwesomeAssertions;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components.Forms;
using Moq;
using MudBlazor;
using MudCategory = Lantean.QBTMud.Models.Category;
using MudTorrent = Lantean.QBTMud.Models.Torrent;
using QbtCategory = Lantean.QBitTorrentClient.Models.Category;
using QbtTorrent = Lantean.QBitTorrentClient.Models.Torrent;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class DialogWorkflowTests
    {
        private readonly IDialogService _dialogService = Mock.Of<IDialogService>();
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly ISnackbar _snackbar = Mock.Of<ISnackbar>();
        private readonly IWebUiLocalizer _webUiLocalizer;

        private readonly DialogWorkflow _target;

        public DialogWorkflowTests()
        {
            _webUiLocalizer = Mock.Of<IWebUiLocalizer>();
            var localizerMock = Mock.Get(_webUiLocalizer);
            localizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => FormatLocalizerString(source, arguments));

            _target = new DialogWorkflow(_dialogService, _apiClient, _snackbar, _webUiLocalizer);
        }

        [Fact]
        public async Task GIVEN_CategoryCreated_WHEN_InvokeAddCategoryDialog_THEN_ShouldCallApi()
        {
            var reference = CreateReference(DialogResult.Ok(new MudCategory("Name", "SavePath")));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("New Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddCategory("Name", "SavePath"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeAddCategoryDialog();

            result.Should().Be("Name");
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_InitialValues_WHEN_InvokeAddCategoryDialog_THEN_ShouldPopulateParameters()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("New Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeAddCategoryDialog("Category", "SavePath");

            result.Should().BeNull();
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<CategoryPropertiesDialog>(
                    "New Category",
                    It.Is<DialogParameters>(parameters => HasCategoryPropertiesDialogParameters(parameters, "Category", "SavePath")),
                    DialogWorkflow.NonBlurFormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddCategoryDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("New Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeAddCategoryDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FileOptions_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldUploadFilesAndDisposeStreams()
        {
            var streamOne = new TrackingStream();
            var streamTwo = new TrackingStream();
            var fileOne = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileOne.Setup(f => f.Name).Returns("Name");
            fileOne.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamOne);
            var fileTwo = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileTwo.Setup(f => f.Name).Returns("SecondName");
            fileTwo.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamTwo);

            var options = CreateTorrentOptions(false, false);
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            var fileOptions = new AddTorrentFileOptions(new[] { fileOne.Object, fileTwo.Object }, options)
            {
                DownloadPath = "DownloadPath",
                InactiveSeedingTimeLimit = 4,
                RatioLimit = 5F,
                SeedingTimeLimit = 6,
                ShareLimitAction = ShareLimitAction.Remove.ToString(),
                UseDownloadPath = true,
                Tags = new[] { "Tags" }
            };

            var reference = CreateReference(DialogResult.Ok(fileOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(1, 1));

            await _target.InvokeAddTorrentFileDialog();

            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.Is<AddTorrentParams>(parameters => MatchesAddTorrentFileParameters(parameters, streamOne, streamTwo))),
                Times.Once);

            streamOne.DisposeAsyncCalled.Should().BeTrue();
            streamTwo.DisposeAsyncCalled.Should().BeTrue();
            fileOne.VerifyAll();
            fileTwo.VerifyAll();

            VerifySnackbar("Added torrent(s) and failed to add torrent(s).", Severity.Warning);
        }

        [Fact]
        public async Task GIVEN_FileOpenFails_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldDisposeOpenedStreamsAndShowError()
        {
            var stream = new TrackingStream();
            var fileOne = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileOne.Setup(f => f.Name).Returns("First.torrent");
            fileOne.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(stream);
            var fileTwo = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileTwo.Setup(f => f.Name).Returns("Second.torrent");
            fileTwo.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Throws(new InvalidOperationException("fail"));

            var options = CreateTorrentOptions(false, false);
            var fileOptions = new AddTorrentFileOptions(new[] { fileOne.Object, fileTwo.Object }, options);
            var reference = CreateReference(DialogResult.Ok(fileOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeAddTorrentFileDialog();

            stream.DisposeAsyncCalled.Should().BeTrue();
            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.IsAny<AddTorrentParams>()), Times.Never);
            VerifySnackbar("Unable to read \"Second.torrent\": fail", Severity.Error);
            fileOne.VerifyAll();
            fileTwo.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_AddTorrentFails_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldDisposeStreamsAndShowError()
        {
            var streamOne = new TrackingStream();
            var streamTwo = new TrackingStream();
            var fileOne = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileOne.Setup(f => f.Name).Returns("One.torrent");
            fileOne.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamOne);
            var fileTwo = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileTwo.Setup(f => f.Name).Returns("Two.torrent");
            fileTwo.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamTwo);

            var options = CreateTorrentOptions(false, false);
            var fileOptions = new AddTorrentFileOptions(new[] { fileOne.Object, fileTwo.Object }, options);
            var reference = CreateReference(DialogResult.Ok(fileOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ThrowsAsync(new HttpRequestException());

            await _target.InvokeAddTorrentFileDialog();

            streamOne.DisposeAsyncCalled.Should().BeTrue();
            streamTwo.DisposeAsyncCalled.Should().BeTrue();
            VerifySnackbar("Unable to add torrent. Please try again.", Severity.Error);
            fileOne.VerifyAll();
            fileTwo.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_DuplicateFileNames_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldEnsureUniqueNames()
        {
            var streamOne = new TrackingStream();
            var streamTwo = new TrackingStream();
            var streamThree = new TrackingStream();
            var fileOne = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileOne.Setup(f => f.Name).Returns("Same.torrent");
            fileOne.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamOne);
            var fileTwo = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileTwo.Setup(f => f.Name).Returns("Same.torrent");
            fileTwo.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamTwo);
            var fileThree = new Mock<IBrowserFile>(MockBehavior.Strict);
            fileThree.Setup(f => f.Name).Returns("Same.torrent");
            fileThree.Setup(f => f.OpenReadStream(4194304, It.IsAny<CancellationToken>())).Returns(streamThree);

            var options = CreateTorrentOptions(false, false);
            var fileOptions = new AddTorrentFileOptions(new[] { fileOne.Object, fileTwo.Object, fileThree.Object }, options);
            var reference = CreateReference(DialogResult.Ok(fileOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(0, 0));

            await _target.InvokeAddTorrentFileDialog();

            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.Is<AddTorrentParams>(parameters => MatchesDuplicateNameAddTorrentFileParameters(parameters))),
                Times.Once);
            streamOne.DisposeAsyncCalled.Should().BeTrue();
            streamTwo.DisposeAsyncCalled.Should().BeTrue();
            streamThree.DisposeAsyncCalled.Should().BeTrue();
            VerifySnackbar("No torrents processed.", Severity.Success);
            fileOne.VerifyAll();
            fileTwo.VerifyAll();
            fileThree.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_CookieProvided_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldSendCookie()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(1, 0, 0, null));

            await _target.InvokeAddTorrentLinkDialog();

            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.Is<AddTorrentParams>(parameters => parameters.Cookie == "Cookie")), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldNotUpload()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeAddTorrentFileDialog();

            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.IsAny<AddTorrentParams>()), Times.Never);
            VerifyNoSnackbar();
        }

        [Fact]
        public async Task GIVEN_NoFiles_WHEN_InvokeAddTorrentFileDialog_THEN_ShouldHandleGracefully()
        {
            var options = CreateTorrentOptions(false, false);
            var fileOptions = new AddTorrentFileOptions(Array.Empty<IBrowserFile>(), options);
            var reference = CreateReference(DialogResult.Ok(fileOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentFileDialog>("Add torrent", DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(0, 0));

            await _target.InvokeAddTorrentFileDialog();

            VerifySnackbar("No torrents processed.", Severity.Success);
        }

        [Fact]
        public async Task GIVEN_LinkOptions_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldAddTorrent()
        {
            var options = CreateTorrentOptions(true, true);
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            var linkOptions = new AddTorrentLinkOptions("http://one\nhttp://two", options)
            {
                DownloadPath = "DownloadPath",
                InactiveSeedingTimeLimit = 4,
                RatioLimit = 5F,
                SeedingTimeLimit = 6,
                ShareLimitAction = ShareLimitAction.Remove.ToString(),
                UseDownloadPath = true,
                Tags = new[] { "Tags" }
            };

            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(1, 0, 0, new[] { "Hash" }));

            await _target.InvokeAddTorrentLinkDialog();

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<AddTorrentLinkDialog>(
                    "Download from URLs",
                    It.Is<DialogParameters>(parameters => HasAddTorrentLinkDialogUrlUnsetParameter(parameters)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.Is<AddTorrentParams>(parameters => MatchesAddTorrentLinkParameters(parameters))),
                Times.Once);

            VerifySnackbar("Added 1 torrent.", Severity.Success);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldNotAddTorrent()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeAddTorrentLinkDialog();

            Mock.Get(_apiClient).Verify(a => a.AddTorrent(It.IsAny<AddTorrentParams>()), Times.Never);
            VerifyNoSnackbar();
        }

        [Fact]
        public async Task GIVEN_AddTorrentFails_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldReportFailure()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(0, 1, 0, new[] { "Hash" }));

            await _target.InvokeAddTorrentLinkDialog();

            VerifySnackbar("Failed to add 1 torrent.", Severity.Error);
        }

        [Fact]
        public async Task GIVEN_AddTorrentLinkThrows_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldShowError()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ThrowsAsync(new HttpRequestException());

            await _target.InvokeAddTorrentLinkDialog();

            VerifySnackbar("Unable to add torrent. Please try again.", Severity.Error);
        }

        [Fact]
        public async Task GIVEN_MixedAddResults_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldShowAggregatedWarning()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(2, 2, 1, null));

            await _target.InvokeAddTorrentLinkDialog();

            VerifySnackbar("Added 2 torrents and failed to add 2 torrents and Pending 1 torrent.", Severity.Warning);
        }

        [Fact]
        public async Task GIVEN_PendingAddRequests_WHEN_InvokeAddTorrentLinkDialog_THEN_ShouldShowPendingInfo()
        {
            var options = CreateTorrentOptions(true, true);
            var linkOptions = new AddTorrentLinkOptions("http://one", options);
            var reference = CreateReference(DialogResult.Ok(linkOptions));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTorrentLinkDialog>("Download from URLs", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.AddTorrent(It.IsAny<AddTorrentParams>()))
                .ReturnsAsync(new AddTorrentResult(0, 0, 2, null));

            await _target.InvokeAddTorrentLinkDialog();

            VerifySnackbar("Pending 2 torrents.", Severity.Info);
        }

        [Fact]
        public async Task GIVEN_DeleteWithoutConfirmation_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldDelete()
        {
            Mock.Get(_apiClient)
                .Setup(a => a.DeleteTorrents(null, false, It.Is<string[]>(hashes => hashes.Length == 1 && hashes[0] == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(false, "Hash");

            result.Should().BeTrue();
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldReturnFalse()
        {
            var result = await _target.InvokeDeleteTorrentDialog(true);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationDeclined_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldNotDelete()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<DeleteDialog>("Remove torrent(s)", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.GetTorrentList(null, null, null, null, null, null, null, null, null, null, "Hash"))
                .ReturnsAsync(new List<QbtTorrent> { new() { Name = "Name" } });

            var result = await _target.InvokeDeleteTorrentDialog(true, "Hash");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldDeleteWithFilesOption()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<DeleteDialog>("Remove torrent(s)", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.GetTorrentList(null, null, null, null, null, null, null, null, null, null, "Hash"))
                .ReturnsAsync(new List<QbtTorrent> { new() { Name = "Name" } });
            Mock.Get(_apiClient)
                .Setup(a => a.DeleteTorrents(null, true, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(true, "Hash");

            result.Should().BeTrue();
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<DeleteDialog>(
                    "Remove torrent(s)",
                    It.Is<DialogParameters>(parameters => HasDeleteDialogParameters(parameters, 1, "Name")),
                    DialogWorkflow.ConfirmDialogOptions),
                Times.Once);
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_MultipleHashes_WHEN_InvokeDeleteTorrentDialog_THEN_ShouldUsePluralTitle()
        {
            var reference = CreateReference(DialogResult.Ok(false));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<DeleteDialog>("Remove torrent(s)", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.DeleteTorrents(null, false, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "Other" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeDeleteTorrentDialog(true, "Hash", "Other");

            result.Should().BeTrue();
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<DeleteDialog>(
                    "Remove torrent(s)",
                    It.Is<DialogParameters>(parameters => HasDeleteDialogParameters(parameters, 2, null)),
                    DialogWorkflow.ConfirmDialogOptions),
                Times.Once);
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_ForceRecheckAsync_THEN_ShouldNotInvokeApi()
        {
            await _target.ForceRecheckAsync(Array.Empty<string>(), false);

            Mock.Get(_apiClient).Verify(a => a.RecheckTorrents(It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NullHashes_WHEN_ForceRecheckAsync_THEN_ShouldNotInvokeApi()
        {
            await _target.ForceRecheckAsync(null!, false);

            Mock.Get(_apiClient).Verify(a => a.RecheckTorrents(It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RecheckWithoutConfirmation_WHEN_ForceRecheckAsync_THEN_ShouldCallApi()
        {
            Mock.Get(_apiClient)
                .Setup(a => a.RecheckTorrents(null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.ForceRecheckAsync(new[] { "Hash" }, false);

            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_RecheckConfirmationDeclined_WHEN_ForceRecheckAsync_THEN_ShouldNotCallApi()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Recheck confirmation", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            await _target.ForceRecheckAsync(new[] { "Hash" }, true);

            Mock.Get(_apiClient).Verify(a => a.RecheckTorrents(It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RecheckConfirmationAccepted_WHEN_ForceRecheckAsync_THEN_ShouldCallApi()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Recheck confirmation", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.RecheckTorrents(null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.ForceRecheckAsync(new[] { "Hash" }, true);

            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_MultipleHashes_WHEN_ForceRecheckAsync_THEN_ShouldPluralizeConfirmation()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Recheck confirmation", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.RecheckTorrents(null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "Other" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.ForceRecheckAsync(new[] { "Hash", "Other" }, true);

            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_InvokeDownloadRateDialog_THEN_ShouldUpdateRate()
        {
            var reference = CreateReference(DialogResult.Ok(3L));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Download Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.SetTorrentDownloadLimit(3072, null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeDownloadRateDialog(2048, new[] { "Hash" });

            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeDownloadRateDialog_THEN_ShouldNotUpdateRate()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Download Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeDownloadRateDialog(2048, new[] { "Hash" });

            Mock.Get(_apiClient).Verify(a => a.SetTorrentDownloadLimit(It.IsAny<long>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DownloadRateDialog_WHEN_BuildingParameters_THEN_ValueFuncsCoverBranches()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Download Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeDownloadRateDialog(1024, new[] { "Hash" });

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<SliderFieldDialog<long>>(
                    "Torrent Download Speed Limiting",
                    It.Is<DialogParameters>(parameters => HasDownloadRateDialogValueFunctions(parameters)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_InvokeUploadRateDialog_THEN_ShouldUpdateRate()
        {
            var reference = CreateReference(DialogResult.Ok(4L));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Upload Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.SetTorrentUploadLimit(4096, null, It.Is<string[]>(hashes => hashes.Single() == "Hash")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeUploadRateDialog(1024, new[] { "Hash" });

            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeUploadRateDialog_THEN_ShouldNotUpdateRate()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Upload Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeUploadRateDialog(1024, new[] { "Hash" });

            Mock.Get(_apiClient).Verify(a => a.SetTorrentUploadLimit(It.IsAny<long>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_UploadRateDialog_WHEN_BuildingParameters_THEN_ValueFuncsCoverBranches()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SliderFieldDialog<long>>("Torrent Upload Speed Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeUploadRateDialog(1024, new[] { "Hash" });

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<SliderFieldDialog<long>>(
                    "Torrent Upload Speed Limiting",
                    It.Is<DialogParameters>(parameters => HasUploadRateDialogValueFunctions(parameters)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryFound_WHEN_InvokeEditCategoryDialog_THEN_ShouldCallApi()
        {
            var categories = new Dictionary<string, QbtCategory>
            {
                { "Category", new QbtCategory("Category", "SavePath", new DownloadPathOption(true, "DownloadPath")) }
            };
            Mock.Get(_apiClient)
                .Setup(a => a.GetAllCategories())
                .ReturnsAsync(categories);
            var reference = CreateReference(DialogResult.Ok(new MudCategory("Name", "SavePath")));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Edit Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.EditCategory("Name", "SavePath"))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await _target.InvokeEditCategoryDialog("Category");

            result.Should().Be("Name");
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<CategoryPropertiesDialog>(
                    "Edit Category",
                    It.Is<DialogParameters>(parameters => HasCategoryPropertiesDialogParameters(parameters, "Category", "SavePath")),
                    DialogWorkflow.NonBlurFormDialogOptions),
                Times.Once);
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeEditCategoryDialog_THEN_ShouldReturnNull()
        {
            var categories = new Dictionary<string, QbtCategory>();
            Mock.Get(_apiClient)
                .Setup(a => a.GetAllCategories())
                .ReturnsAsync(categories);
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CategoryPropertiesDialog>("Edit Category", It.IsAny<DialogParameters>(), DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.InvokeEditCategoryDialog("Category");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_InvokeRenameFilesDialog_THEN_ShouldForwardParameters()
        {
            var reference = new Mock<IDialogReference>();
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<RenameFilesDialog>("Renaming", It.IsAny<DialogParameters>(), DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference.Object);

            await _target.InvokeRenameFilesDialog("Hash");

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<RenameFilesDialog>(
                    "Renaming",
                    It.Is<DialogParameters>(parameters => HasStringParameter(parameters, nameof(RenameFilesDialog.Hash), "Hash")),
                    DialogWorkflow.FullScreenDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InvokeRssRulesDialog_WHEN_Executed_THEN_ShouldOpenDialog()
        {
            var reference = new Mock<IDialogReference>();
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<RssRulesDialog>("Rss Downloader", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference.Object)
                .Verifiable();

            await _target.InvokeRssRulesDialog();

            Mock.Get(_dialogService).Verify();
        }

        [Fact]
        public async Task GIVEN_NoTorrents_WHEN_InvokeShareRatioDialog_THEN_ShouldReturnImmediately()
        {
            await _target.InvokeShareRatioDialog(Enumerable.Empty<MudTorrent>());

            Mock.Get(_dialogService).Invocations.Should().BeEmpty();
            Mock.Get(_apiClient).Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_TorrentsWithDistinctShareRatios_WHEN_InvokeShareRatioDialogConfirmed_THEN_ShouldUpdateShareLimits()
        {
            var torrents = new[]
            {
                CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop),
                CreateTorrent("SecondHash", 3F, 3, 4F, ShareLimitAction.Remove)
            };

            var reference = CreateReference(DialogResult.Ok(new ShareRatio
            {
                RatioLimit = 5F,
                SeedingTimeLimit = 6F,
                InactiveSeedingTimeLimit = 7F,
                ShareLimitAction = ShareLimitAction.Remove
            }));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Torrent Upload/Download Ratio Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.SetTorrentShareLimit(5F, 6F, 7F, ShareLimitAction.Remove, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "SecondHash" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeShareRatioDialog(torrents);

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<ShareRatioDialog>(
                    "Torrent Upload/Download Ratio Limiting",
                    It.Is<DialogParameters>(parameters => HasDistinctShareRatioDialogParameters(parameters)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
            Mock.Get(_apiClient).Verify();
        }

        [Fact]
        public async Task GIVEN_TorrentsWithMatchingShareRatios_WHEN_InvokeShareRatioDialogConfirmed_THEN_ShouldProvideExistingValue()
        {
            var torrents = new[]
            {
                CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop),
                CreateTorrent("SecondHash", 2F, 3, 4F, ShareLimitAction.Stop)
            };

            var reference = CreateReference(DialogResult.Ok(new ShareRatio
            {
                RatioLimit = 5F,
                SeedingTimeLimit = 6F,
                InactiveSeedingTimeLimit = 7F,
                ShareLimitAction = ShareLimitAction.Remove
            }));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Torrent Upload/Download Ratio Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            Mock.Get(_apiClient)
                .Setup(a => a.SetTorrentShareLimit(5F, 6F, 7F, ShareLimitAction.Remove, null, It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash", "SecondHash" }))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await _target.InvokeShareRatioDialog(torrents);

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<ShareRatioDialog>(
                    "Torrent Upload/Download Ratio Limiting",
                    It.Is<DialogParameters>(parameters => HasMatchingShareRatioDialogParameters(parameters)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_InvokeShareRatioDialog_THEN_ShouldNotUpdateLimits()
        {
            var torrents = new[] { CreateTorrent("Hash", 2F, 3, 4F, ShareLimitAction.Stop) };
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ShareRatioDialog>("Torrent Upload/Download Ratio Limiting", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            await _target.InvokeShareRatioDialog(torrents);

            Mock.Get(_apiClient).Verify(a => a.SetTorrentShareLimit(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<ShareLimitAction?>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ValueReturned_WHEN_InvokeStringFieldDialog_THEN_ShouldInvokeSuccess()
        {
            var reference = CreateReference(DialogResult.Ok("Value"));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.InvokeStringFieldDialog("Title", "Label", "Value", value =>
            {
                invoked = value == "Value";
                return Task.CompletedTask;
            });

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ValueMissing_WHEN_InvokeStringFieldDialog_THEN_ShouldNotInvokeSuccess()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.InvokeStringFieldDialog("Title", "Label", "Value", _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_PeersAdded_WHEN_ShowAddPeersDialog_THEN_ShouldReturnPeers()
        {
            var peers = new HashSet<PeerId> { new PeerId("Host", 1) };
            var reference = CreateReference(DialogResult.Ok(peers));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddPeerDialog>("Add Peers", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddPeersDialog();

            result.Should().BeEquivalentTo(peers);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddPeersDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddPeerDialog>("Add Peers", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddPeersDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TagsAdded_WHEN_ShowAddTagsDialog_THEN_ShouldReturnTags()
        {
            var tags = new HashSet<string> { "Tag" };
            var reference = CreateReference(DialogResult.Ok(tags));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTagDialog>("Add tags", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTagsDialog();

            result.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddTagsDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTagDialog>("Add tags", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTagsDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TrackersAdded_WHEN_ShowAddTrackersDialog_THEN_ShouldReturnTrackers()
        {
            var trackers = new HashSet<string> { "Tracker" };
            var reference = CreateReference(DialogResult.Ok(trackers));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTrackerDialog>("Add trackers", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTrackersDialog();

            result.Should().BeEquivalentTo(trackers);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowAddTrackersDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<AddTrackerDialog>("Add trackers", DialogWorkflow.NonBlurFormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowAddTrackersDialog();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ColumnOptionsReturned_WHEN_ShowColumnsOptionsDialog_THEN_ShouldReturnValues()
        {
            var columns = new[] { new ColumnDefinition<string>("Header", value => value) };
            var selected = new HashSet<string> { "Header" };
            var widths = new Dictionary<string, int?> { { "Header", 10 } };
            var order = new Dictionary<string, int> { { "Header", 0 } };
            var reference = CreateReference(DialogResult.Ok((selected, widths, order)));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ColumnOptionsDialog<string>>("Choose Columns", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowColumnsOptionsDialog(columns.ToList(), selected, widths, order);

            result.SelectedColumns.Should().BeEquivalentTo(selected);
            result.ColumnWidths.Should().BeEquivalentTo(widths);
            result.ColumnOrder.Should().BeEquivalentTo(order);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowColumnsOptionsDialog_THEN_ShouldReturnDefault()
        {
            var columns = new[] { new ColumnDefinition<string>("Header", value => value) };
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ColumnOptionsDialog<string>>("Choose Columns", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowColumnsOptionsDialog(columns.ToList(), new HashSet<string>(), new Dictionary<string, int?>(), new Dictionary<string, int>());

            result.SelectedColumns.Should().BeNull();
            result.ColumnWidths.Should().BeNull();
            result.ColumnOrder.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_DialogConfirmed_WHEN_ShowConfirmDialog_THEN_ShouldReturnTrue()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowConfirmDialog("Title", "Content");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowConfirmDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowConfirmDialog("Title", "Content");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NullResult_WHEN_ShowConfirmDialog_THEN_ShouldReturnFalse()
        {
            var reference = new Mock<IDialogReference>();
            reference.Setup(r => r.Result).Returns(Task.FromResult<DialogResult?>(null));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference.Object);

            var result = await _target.ShowConfirmDialog("Title", "Content");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_ShowConfirmDialogWithTask_THEN_ShouldInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ConfirmationRejected_WHEN_ShowConfirmDialogWithTask_THEN_ShouldNotInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConfirmationAccepted_WHEN_ShowConfirmDialogWithAction_THEN_ShouldInvokeCallback()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ConfirmDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.ConfirmDialogOptions))
                .ReturnsAsync(reference);
            var invoked = false;

            await _target.ShowConfirmDialog("Title", "Content", () => invoked = true);

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_FiltersReturned_WHEN_ShowFilterOptionsDialog_THEN_ShouldReturnFilters()
        {
            var filters = new List<PropertyFilterDefinition<FilterSample>>
            {
                new PropertyFilterDefinition<FilterSample>(nameof(FilterSample.Value), "Equals", "Value")
            };
            var reference = CreateReference(DialogResult.Ok(filters));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<FilterOptionsDialog<FilterSample>>("Filters", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowFilterOptionsDialog(filters);

            result.Should().BeEquivalentTo(filters);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowFilterOptionsDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<FilterOptionsDialog<FilterSample>>("Filters", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowFilterOptionsDialog<FilterSample>(null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValueReturned_WHEN_ShowStringFieldDialog_THEN_ShouldReturnValue()
        {
            var reference = CreateReference(DialogResult.Ok("Value"));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowStringFieldDialog("Title", "Label", "Value");

            result.Should().Be("Value");
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<StringFieldDialog>(
                    "Title",
                    It.Is<DialogParameters>(parameters => HasStringFieldDialogParameters(parameters, "Label", "Value")),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowStringFieldDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<StringFieldDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowStringFieldDialog("Title", "Label", "Value");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CookieReturned_WHEN_ShowCookiePropertiesDialog_THEN_ShouldReturnCookieAndForwardParameters()
        {
            var cookie = new ApplicationCookie("Name", "Domain", "/Path", "Value", 1);
            var reference = CreateReference(DialogResult.Ok(cookie));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CookiePropertiesDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowCookiePropertiesDialog("Title", cookie);

            result.Should().Be(cookie);
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<CookiePropertiesDialog>(
                    "Title",
                    It.Is<DialogParameters>(parameters => HasCookieDialogParameters(parameters, cookie)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowCookiePropertiesDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CookiePropertiesDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowCookiePropertiesDialog("Title", null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NonCookieResult_WHEN_ShowCookiePropertiesDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Ok("Value"));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<CookiePropertiesDialog>("Title", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowCookiePropertiesDialog("Title", null);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Data_WHEN_ShowSubMenu_THEN_ShouldForwardParameters()
        {
            var reference = new Mock<IDialogReference>();
            var parent = new UIAction("Name", "Parent", null, Color.Primary, "Href");
            var hashes = new[] { "Hash" };
            var torrents = new Dictionary<string, MudTorrent> { { "Hash", CreateTorrent("Hash", 0F, 0, 0F, ShareLimitAction.Default) } };
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SubMenuDialog>("Parent", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference.Object);

            await _target.ShowSubMenu(hashes, parent, torrents, null, [], []);

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<SubMenuDialog>(
                    "Parent",
                    It.Is<DialogParameters>(parameters => HasSubMenuDialogParameters(parameters, parent, hashes, torrents)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PluginChanges_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnTrue()
        {
            var reference = CreateReference(DialogResult.Ok(true));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoPluginChanges_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Ok(false));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NonBooleanResult_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Ok("ignore"));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowSearchPluginsDialog_THEN_ShouldReturnFalse()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<SearchPluginsDialog>("Search plugins", DialogWorkflow.FullScreenDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowSearchPluginsDialog();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NullTheme_WHEN_ShowThemePreviewDialog_THEN_Throws()
        {
            Func<Task> act = async () => await _target.ShowThemePreviewDialog(null!, true);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_Theme_WHEN_ShowThemePreviewDialog_THEN_ShowsDialog()
        {
            var reference = new Mock<IDialogReference>();
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<ThemePreviewDialog>("Theme Preview", It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
                .ReturnsAsync(reference.Object);

            var theme = new MudTheme();

            await _target.ShowThemePreviewDialog(theme, true);

            Mock.Get(_dialogService).Verify(s => s.ShowAsync<ThemePreviewDialog>(
                    "Theme Preview",
                    It.Is<DialogParameters>(parameters => HasThemePreviewDialogParameters(parameters, theme, true)),
                    It.Is<DialogOptions>(options => HasThemePreviewDialogOptions(options))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PathBrowserSelection_WHEN_ShowPathBrowserDialog_THEN_ShouldReturnSelectedPath()
        {
            var reference = CreateReference(DialogResult.Ok("C:/Folder"));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<PathBrowserDialog>("Pick", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowPathBrowserDialog("Pick", "C:/", DirectoryContentMode.Files, false);

            result.Should().Be("C:/Folder");
            Mock.Get(_dialogService).Verify(s => s.ShowAsync<PathBrowserDialog>(
                    "Pick",
                    It.Is<DialogParameters>(parameters => HasPathBrowserDialogParameters(parameters, "C:/", DirectoryContentMode.Files, false)),
                    DialogWorkflow.FormDialogOptions),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogCanceled_WHEN_ShowPathBrowserDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Cancel());
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<PathBrowserDialog>("Pick", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NullDialogResult_WHEN_ShowPathBrowserDialog_THEN_ShouldReturnNull()
        {
            var reference = new Mock<IDialogReference>();
            reference.Setup(r => r.Result).ReturnsAsync((DialogResult?)null);
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<PathBrowserDialog>("Pick", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference.Object);

            var result = await _target.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NonStringResult_WHEN_ShowPathBrowserDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Ok(12));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<PathBrowserDialog>("Pick", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_WhitespaceResult_WHEN_ShowPathBrowserDialog_THEN_ShouldReturnNull()
        {
            var reference = CreateReference(DialogResult.Ok(" "));
            Mock.Get(_dialogService)
                .Setup(s => s.ShowAsync<PathBrowserDialog>("Pick", It.IsAny<DialogParameters>(), DialogWorkflow.FormDialogOptions))
                .ReturnsAsync(reference);

            var result = await _target.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true);

            result.Should().BeNull();
        }

        private void VerifySnackbar(string message, Severity severity)
        {
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(message, severity, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        private void VerifyNoSnackbar()
        {
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>()),
                Times.Never);
        }

        private static bool HasCategoryPropertiesDialogParameters(DialogParameters parameters, string category, string savePath)
        {
            return HasStringParameter(parameters, nameof(CategoryPropertiesDialog.Category), category)
                   && HasStringParameter(parameters, nameof(CategoryPropertiesDialog.SavePath), savePath);
        }

        private static bool MatchesAddTorrentFileParameters(AddTorrentParams parameters, Stream firstStream, Stream secondStream)
        {
            if (parameters.Torrents == null)
            {
                return false;
            }

            if (parameters.Torrents.Count != 2)
            {
                return false;
            }

            if (!parameters.Torrents.TryGetValue("Name", out var firstTorrentStream) || !ReferenceEquals(firstTorrentStream, firstStream))
            {
                return false;
            }

            if (!parameters.Torrents.TryGetValue("SecondName", out var secondTorrentStream) || !ReferenceEquals(secondTorrentStream, secondStream))
            {
                return false;
            }

            return parameters.AutoTorrentManagement == false
                   && parameters.Category == "Category"
                   && parameters.DownloadLimit == 2
                   && parameters.UploadLimit == 3
                   && parameters.DownloadPath == "DownloadPath"
                   && parameters.UseDownloadPath == true
                   && parameters.SavePath == "SavePath"
                   && parameters.Tags != null
                   && parameters.Tags.SequenceEqual(new[] { "Tags" })
                   && parameters.RatioLimit == 5F
                   && parameters.SeedingTimeLimit == 6
                   && parameters.InactiveSeedingTimeLimit == 4
                   && parameters.ShareLimitAction == ShareLimitAction.Remove
                   && parameters.ContentLayout == TorrentContentLayout.Original
                   && parameters.StopCondition == StopCondition.MetadataReceived
                   && parameters.Stopped == true
                   && parameters.AddToTopOfQueue == true;
        }

        private static bool MatchesDuplicateNameAddTorrentFileParameters(AddTorrentParams parameters)
        {
            return parameters.Torrents != null
                   && parameters.Torrents.Count == 3
                   && parameters.Torrents.ContainsKey("Same.torrent")
                   && parameters.Torrents.ContainsKey("Same (1).torrent")
                   && parameters.Torrents.ContainsKey("Same (2).torrent");
        }

        private static bool HasAddTorrentLinkDialogUrlUnsetParameter(DialogParameters parameters)
        {
            return HasParameter(parameters, nameof(AddTorrentLinkDialog.Url))
                   && parameters[nameof(AddTorrentLinkDialog.Url)] == null;
        }

        private static bool MatchesAddTorrentLinkParameters(AddTorrentParams parameters)
        {
            return parameters.Urls != null
                   && parameters.Urls.SequenceEqual(new[] { "http://one", "http://two" })
                   && parameters.Torrents == null
                   && parameters.AutoTorrentManagement == true
                   && parameters.SavePath == null
                   && parameters.DownloadPath == "DownloadPath"
                   && parameters.UseDownloadPath == true
                   && parameters.Tags != null
                   && parameters.Tags.SequenceEqual(new[] { "Tags" })
                   && parameters.RatioLimit == 5F
                   && parameters.SeedingTimeLimit == 6
                   && parameters.InactiveSeedingTimeLimit == 4
                   && parameters.ShareLimitAction == ShareLimitAction.Remove
                   && parameters.StopCondition == StopCondition.MetadataReceived
                   && parameters.Stopped == false;
        }

        private static bool HasDeleteDialogParameters(DialogParameters parameters, int count, string? torrentName)
        {
            if (!HasParameter(parameters, nameof(DeleteDialog.Count)))
            {
                return false;
            }

            if (!Equals(parameters[nameof(DeleteDialog.Count)], count))
            {
                return false;
            }

            var hasTorrentName = HasParameter(parameters, nameof(DeleteDialog.TorrentName));
            if (torrentName == null)
            {
                return hasTorrentName == false;
            }

            return hasTorrentName
                   && string.Equals(parameters[nameof(DeleteDialog.TorrentName)]?.ToString(), torrentName, StringComparison.Ordinal);
        }

        private static bool HasDownloadRateDialogValueFunctions(DialogParameters parameters)
        {
            if (!HasParameter(parameters, nameof(SliderFieldDialog<long>.ValueDisplayFunc)))
            {
                return false;
            }

            var display = parameters[nameof(SliderFieldDialog<long>.ValueDisplayFunc)] as Func<long, string>;
            if (display == null || display(Limits.NoLimit) != "∞" || display(2048) != "2048")
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(SliderFieldDialog<long>.ValueGetFunc)))
            {
                return false;
            }

            var getter = parameters[nameof(SliderFieldDialog<long>.ValueGetFunc)] as Func<string, long>;
            return getter != null
                   && getter("∞") == Limits.NoLimit
                   && getter("5") == 5;
        }

        private static bool HasUploadRateDialogValueFunctions(DialogParameters parameters)
        {
            if (!HasParameter(parameters, nameof(SliderFieldDialog<long>.ValueDisplayFunc)))
            {
                return false;
            }

            var display = parameters[nameof(SliderFieldDialog<long>.ValueDisplayFunc)] as Func<long, string>;
            if (display == null || display(Limits.NoLimit) != "∞" || display(1024) != "1024")
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(SliderFieldDialog<long>.ValueGetFunc)))
            {
                return false;
            }

            var getter = parameters[nameof(SliderFieldDialog<long>.ValueGetFunc)] as Func<string, long>;
            return getter != null
                   && getter("∞") == Limits.NoLimit
                   && getter("7") == 7;
        }

        private static bool HasDistinctShareRatioDialogParameters(DialogParameters parameters)
        {
            if (!HasParameter(parameters, nameof(ShareRatioDialog.Value)))
            {
                return false;
            }

            if (parameters[nameof(ShareRatioDialog.Value)] != null)
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(ShareRatioDialog.CurrentValue)))
            {
                return false;
            }

            var currentValue = parameters[nameof(ShareRatioDialog.CurrentValue)] as ShareRatioMax;
            return currentValue != null
                   && currentValue.RatioLimit == 2F
                   && currentValue.SeedingTimeLimit == 3
                   && currentValue.InactiveSeedingTimeLimit == 4F
                   && currentValue.ShareLimitAction == ShareLimitAction.Stop;
        }

        private static bool HasMatchingShareRatioDialogParameters(DialogParameters parameters)
        {
            return HasParameter(parameters, nameof(ShareRatioDialog.Value))
                   && parameters[nameof(ShareRatioDialog.Value)] is ShareRatioMax;
        }

        private static bool HasStringFieldDialogParameters(DialogParameters parameters, string label, string value)
        {
            return HasStringParameter(parameters, nameof(StringFieldDialog.Label), label)
                   && HasStringParameter(parameters, nameof(StringFieldDialog.Value), value);
        }

        private static bool HasCookieDialogParameters(DialogParameters parameters, ApplicationCookie cookie)
        {
            return HasParameter(parameters, nameof(CookiePropertiesDialog.Cookie))
                   && ReferenceEquals(parameters[nameof(CookiePropertiesDialog.Cookie)], cookie);
        }

        private static bool HasSubMenuDialogParameters(DialogParameters parameters, UIAction parent, IEnumerable<string> hashes, IReadOnlyDictionary<string, MudTorrent> torrents)
        {
            if (!HasParameter(parameters, nameof(SubMenuDialog.ParentAction))
                || !ReferenceEquals(parameters[nameof(SubMenuDialog.ParentAction)], parent))
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(SubMenuDialog.Hashes)))
            {
                return false;
            }

            var forwardedHashes = parameters[nameof(SubMenuDialog.Hashes)] as IEnumerable<string>;
            if (forwardedHashes == null || !forwardedHashes.SequenceEqual(hashes))
            {
                return false;
            }

            return HasParameter(parameters, nameof(SubMenuDialog.Torrents))
                   && ReferenceEquals(parameters[nameof(SubMenuDialog.Torrents)], torrents);
        }

        private static bool HasThemePreviewDialogParameters(DialogParameters parameters, MudTheme theme, bool isDarkMode)
        {
            if (!HasParameter(parameters, nameof(ThemePreviewDialog.Theme))
                || !ReferenceEquals(parameters[nameof(ThemePreviewDialog.Theme)], theme))
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(ThemePreviewDialog.IsDarkMode)))
            {
                return false;
            }

            var isDarkModeValue = parameters[nameof(ThemePreviewDialog.IsDarkMode)] as bool?;
            return isDarkModeValue.HasValue && isDarkModeValue.Value == isDarkMode;
        }

        private static bool HasThemePreviewDialogOptions(DialogOptions options)
        {
            return options.FullScreen == false && options.NoHeader == true && options.FullWidth == false;
        }

        private static bool HasPathBrowserDialogParameters(DialogParameters parameters, string initialPath, DirectoryContentMode mode, bool allowFolderSelection)
        {
            if (!HasStringParameter(parameters, nameof(PathBrowserDialog.InitialPath), initialPath))
            {
                return false;
            }

            if (!HasParameter(parameters, nameof(PathBrowserDialog.Mode))
                || !Equals(parameters[nameof(PathBrowserDialog.Mode)], mode))
            {
                return false;
            }

            return HasParameter(parameters, nameof(PathBrowserDialog.AllowFolderSelection))
                   && Equals(parameters[nameof(PathBrowserDialog.AllowFolderSelection)], allowFolderSelection);
        }

        private static bool HasStringParameter(DialogParameters parameters, string key, string value)
        {
            return HasParameter(parameters, key)
                   && string.Equals(parameters[key]?.ToString(), value, StringComparison.Ordinal);
        }

        private static bool HasParameter(DialogParameters parameters, string key)
        {
            return parameters.Any(parameter => parameter.Key == key);
        }

        private static IDialogReference CreateReference(DialogResult result)
        {
            var reference = new Mock<IDialogReference>();
            reference.Setup(r => r.Result).ReturnsAsync(result);
            return reference.Object;
        }

        private static TorrentOptions CreateTorrentOptions(bool torrentManagementMode, bool startTorrent)
        {
            var options = new TorrentOptions(
                torrentManagementMode,
                "SavePath",
                "Cookie",
                "RenameTorrent",
                "Category",
                startTorrent,
                true,
                StopCondition.MetadataReceived.ToString(),
                false,
                "Original",
                true,
                true,
                2,
                3);
            options.DownloadPath = "DownloadPath";
            options.InactiveSeedingTimeLimit = 4;
            options.RatioLimit = 5F;
            options.SeedingTimeLimit = 6;
            options.ShareLimitAction = ShareLimitAction.Remove.ToString();
            options.UseDownloadPath = true;
            options.Tags = new[] { "Tags" };
            return options;
        }

        private static MudTorrent CreateTorrent(string hash, float ratioLimit, int seedingTimeLimit, float inactiveSeedingTimeLimit, ShareLimitAction shareLimitAction)
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 1,
                category: string.Empty,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: ratioLimit + 1,
                maxSeedingTime: seedingTimeLimit + 1,
                name: hash,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: string.Empty,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker: string.Empty,
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit,
                maxInactiveSeedingTime: inactiveSeedingTimeLimit + 1,
                popularity: 0,
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                shareLimitAction,
                comment: string.Empty);
        }

        private static string FormatLocalizerString(string source, object[] arguments)
        {
            if (arguments is null || arguments.Length == 0)
            {
                return source;
            }

            var result = source;
            for (var i = 0; i < arguments.Length; i++)
            {
                var token = $"%{i + 1}";
                var value = arguments[i]?.ToString() ?? string.Empty;
                result = result.Replace(token, value);
            }

            return result;
        }

        private sealed class TrackingStream : MemoryStream
        {
            public bool DisposeAsyncCalled { get; private set; }

            public override ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                return base.DisposeAsync();
            }
        }

        private sealed class FilterSample
        {
            public string Value { get; set; } = "Value";
        }
    }
}
