using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;

namespace Lantean.QBTMud.Presentation.Test.Services
{
    public sealed class PendingDownloadWorkflowTests
    {
        private const string _pendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string _lastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";

        [Fact]
        public async Task GIVEN_RestoredPendingDownload_WHEN_Processed_THEN_ShouldShowDialogPersistLastProcessedAndNavigateHome()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            await sessionStorage.SetItemAsync(_pendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", cancellationToken);
            await sessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, "magnet:?xt=urn:btih:OLD", cancellationToken);
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.IsSupportedDownloadLink("magnet:?xt=urn:btih:ABC")).Returns(true);
            var dialogWorkflow = new Mock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflow.Setup(workflow => workflow.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC")).Returns(Task.CompletedTask);
            var navigationManager = new TestNavigationManager();
            var target = CreateTarget(sessionStorage, magnetLinkService.Object, dialogWorkflow.Object, navigationManager);

            await target.RestoreAsync(cancellationToken);
            await target.ProcessAsync(cancellationToken);

            var pending = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            var lastProcessed = await sessionStorage.GetItemAsync<string>(_lastProcessedDownloadStorageKey, cancellationToken);
            pending.Should().BeNull();
            lastProcessed.Should().Be("magnet:?xt=urn:btih:ABC");
            navigationManager.LastNavigationUri.Should().Be("./");
            navigationManager.ForceLoad.Should().BeTrue();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("not-a-link")]
        public async Task GIVEN_InvalidStoredPendingDownload_WHEN_Restored_THEN_ShouldRemovePersistedValue(string pendingDownload)
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            await sessionStorage.SetItemAsync(_pendingDownloadStorageKey, pendingDownload, cancellationToken);
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.IsSupportedDownloadLink(pendingDownload)).Returns(false);
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.RestoreAsync(cancellationToken);

            var stored = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_WhitespaceUri_WHEN_CapturingFromUri_THEN_ShouldIgnoreIt()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.CaptureFromUriAsync(" ", cancellationToken);

            magnetLinkService.Verify(service => service.ExtractDownloadLink(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_UnsupportedUri_WHEN_CapturingFromUri_THEN_ShouldNotPersistPendingDownload()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.ExtractDownloadLink("http://localhost/?download=file.txt")).Returns((string?)null);
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.CaptureFromUriAsync("http://localhost/?download=file.txt", cancellationToken);

            var stored = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ExtractedDownloadWhitespace_WHEN_CapturingFromUri_THEN_ShouldNotPersistPendingDownload()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.ExtractDownloadLink("http://localhost/?download=blank")).Returns(" ");
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.CaptureFromUriAsync("http://localhost/?download=blank", cancellationToken);

            var stored = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_AlreadyProcessedDownload_WHEN_CapturingFromUri_THEN_ShouldNotPersistPendingDownload()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            await sessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, "magnet:?xt=urn:btih:ABC", cancellationToken);
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.IsSupportedDownloadLink(null)).Returns(false);
            magnetLinkService.Setup(service => service.ExtractDownloadLink("http://localhost/?download=magnet")).Returns("magnet:?xt=urn:btih:ABC");
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.RestoreAsync(cancellationToken);
            await target.CaptureFromUriAsync("http://localhost/?download=magnet", cancellationToken);

            var stored = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValidUri_WHEN_CapturingFromUri_THEN_ShouldPersistPendingDownload()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.ExtractDownloadLink("http://localhost/?download=magnet")).Returns("magnet:?xt=urn:btih:ABC");
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.CaptureFromUriAsync("http://localhost/?download=magnet", cancellationToken);

            var stored = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            stored.Should().Be("magnet:?xt=urn:btih:ABC");
        }

        [Fact]
        public async Task GIVEN_NoPendingDownload_WHEN_Processing_THEN_ShouldDoNothing()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            var dialogWorkflow = new Mock<IDialogWorkflow>(MockBehavior.Strict);
            var navigationManager = new TestNavigationManager();
            var target = CreateTarget(sessionStorage, magnetLinkService.Object, dialogWorkflow.Object, navigationManager);

            await target.ProcessAsync(cancellationToken);

            navigationManager.LastNavigationUri.Should().BeNull();
            dialogWorkflow.Verify(workflow => workflow.InvokeAddTorrentLinkDialog(It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_PendingDownloadAlreadyProcessed_WHEN_Processing_THEN_ShouldClearAndNavigateHome()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            await sessionStorage.SetItemAsync(_pendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", cancellationToken);
            await sessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, "magnet:?xt=urn:btih:ABC", cancellationToken);
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.IsSupportedDownloadLink("magnet:?xt=urn:btih:ABC")).Returns(true);
            var dialogWorkflow = new Mock<IDialogWorkflow>(MockBehavior.Strict);
            var navigationManager = new TestNavigationManager();
            var target = CreateTarget(sessionStorage, magnetLinkService.Object, dialogWorkflow.Object, navigationManager);

            await target.RestoreAsync(cancellationToken);
            await target.ProcessAsync(cancellationToken);

            var pending = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            pending.Should().BeNull();
            navigationManager.LastNavigationUri.Should().Be("./");
            navigationManager.ForceLoad.Should().BeTrue();
            dialogWorkflow.Verify(workflow => workflow.InvokeAddTorrentLinkDialog(It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DialogWorkflowThrows_WHEN_ProcessingPendingDownload_THEN_ShouldPersistPendingDownloadAndRethrow()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.ExtractDownloadLink("http://localhost/?download=magnet")).Returns("magnet:?xt=urn:btih:ABC");
            var dialogWorkflow = new Mock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflow
                .Setup(workflow => workflow.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC"))
                .ThrowsAsync(new InvalidOperationException("Failure"));
            var target = CreateTarget(sessionStorage, magnetLinkService.Object, dialogWorkflow.Object, new TestNavigationManager());

            await target.CaptureFromUriAsync("http://localhost/?download=magnet", cancellationToken);
            var action = async () => await target.ProcessAsync(cancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Failure");
            var pending = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            pending.Should().Be("magnet:?xt=urn:btih:ABC");
        }

        [Fact]
        public async Task GIVEN_PendingDownloadExists_WHEN_Clearing_THEN_ShouldRemovePersistedValue()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var sessionStorage = new TestSessionStorageService();
            await sessionStorage.SetItemAsync(_pendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", cancellationToken);
            var magnetLinkService = new Mock<IMagnetLinkService>(MockBehavior.Strict);
            magnetLinkService.Setup(service => service.IsSupportedDownloadLink("magnet:?xt=urn:btih:ABC")).Returns(true);
            var target = CreateTarget(sessionStorage, magnetLinkService.Object);

            await target.RestoreAsync(cancellationToken);
            await target.ClearAsync(cancellationToken);

            var pending = await sessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey, cancellationToken);
            pending.Should().BeNull();
        }

        private static PendingDownloadWorkflow CreateTarget(
            ISessionStorageService sessionStorageService,
            IMagnetLinkService magnetLinkService,
            IDialogWorkflow? dialogWorkflow = null,
            NavigationManager? navigationManager = null)
        {
            return new PendingDownloadWorkflow(
                sessionStorageService,
                magnetLinkService,
                dialogWorkflow ?? Mock.Of<IDialogWorkflow>(MockBehavior.Strict),
                navigationManager ?? new TestNavigationManager());
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public string? LastNavigationUri { get; private set; }

            public bool ForceLoad { get; private set; }

            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigationUri = uri;
                ForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
