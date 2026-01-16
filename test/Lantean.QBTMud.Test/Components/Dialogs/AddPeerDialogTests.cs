using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class AddPeerDialogTests : RazorComponentTestBase<AddPeerDialogTestHarness>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddPeerDialogTestDriver _target;

        public AddPeerDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddPeerDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_EmptyInput_WHEN_AddTrackerInvoked_THEN_PeersRemainEmpty()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeAddTracker();

            dialog.Component.Instance.GetPeers().Should().BeEmpty();
            dialog.Component.Instance.IPValue.Should().BeNull();
            dialog.Component.Instance.PortValue.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValidInput_WHEN_AddTrackerInvoked_THEN_PeerAddedAndFieldsCleared()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetIP("IP");
            dialog.Component.Instance.InvokeSetPort(6881);
            dialog.Component.Instance.InvokeAddTracker();
            dialog.Component.Render();

            var peers = dialog.Component.Instance.GetPeers();
            peers.Should().ContainSingle();
            var peer = peers.Single();
            peer.Host.Should().Be("IP");
            peer.Port.Should().Be(6881);
            dialog.Component.Instance.IPValue.Should().BeNull();
            dialog.Component.Instance.PortValue.Should().BeNull();

            FindComponentByTestId<MudIconButton>(dialog.Component, "DeletePeer-IP-6881").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_DeletePeerInvoked_THEN_PeerRemoved()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetIP("IP");
            dialog.Component.Instance.InvokeSetPort(6881);
            dialog.Component.Instance.InvokeAddTracker();
            dialog.Component.Render();

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "DeletePeer-IP-6881");
            await deleteButton.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());

            dialog.Component.Instance.GetPeers().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeCancel());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_SubmitInvoked_THEN_ResultContainsPeers()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetIP("IP");
            dialog.Component.Instance.InvokeSetPort(6881);
            dialog.Component.Instance.InvokeAddTracker();

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmit());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var peers = (HashSet<PeerId>)result.Data!;
            peers.Should().ContainSingle();
            peers.Single().Host.Should().Be("IP");
            peers.Single().Port.Should().Be(6881);
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsPeers()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetIP("IP");
            dialog.Component.Instance.InvokeSetPort(6881);
            dialog.Component.Instance.InvokeAddTracker();

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitWithKeyboardAsync(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var peers = (HashSet<PeerId>)result.Data!;
            peers.Should().ContainSingle();
            peers.Single().Host.Should().Be("IP");
            peers.Single().Port.Should().Be(6881);
        }
    }

    public sealed class AddPeerDialogTestHarness : AddPeerDialog
    {
        public HashSet<PeerId> GetPeers()
        {
            return Peers;
        }

        public string? IPValue
        {
            get
            {
                return IP;
            }
        }

        public int? PortValue
        {
            get
            {
                return Port;
            }
        }

        public void InvokeSetIP(string value)
        {
            SetIP(value);
        }

        public void InvokeSetPort(int? value)
        {
            SetPort(value);
        }

        public void InvokeAddTracker()
        {
            AddTracker();
        }

        public void InvokeDeletePeer(PeerId peer)
        {
            DeletePeer(peer);
        }

        public void InvokeCancel()
        {
            Cancel();
        }

        public void InvokeSubmit()
        {
            Submit();
        }

        public Task InvokeSubmitWithKeyboardAsync(KeyboardEvent keyboardEvent)
        {
            return Submit(keyboardEvent);
        }
    }

    internal sealed class AddPeerDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddPeerDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<AddPeerDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<AddPeerDialogTestHarness>("Add Peer");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddPeerDialogTestHarness>();

            return new AddPeerDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddPeerDialogRenderContext
    {
        public AddPeerDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddPeerDialogTestHarness> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddPeerDialogTestHarness> Component { get; }

        public IDialogReference Reference { get; }
    }
}
