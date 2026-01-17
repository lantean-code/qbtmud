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
    public sealed class AddPeerDialogTests : RazorComponentTestBase<AddPeerDialog>
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

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddPeerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var ipField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddPeerIp");
            var portField = FindComponentByTestId<MudNumericField<int?>>(dialog.Component, "AddPeerPort");

            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
            ipField.Instance.Value.Should().BeNull();
            portField.Instance.Value.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValidInput_WHEN_AddTrackerInvoked_THEN_PeerAddedAndFieldsCleared()
        {
            var dialog = await _target.RenderDialogAsync();

            var ipField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddPeerIp");
            ipField.Find("input").Change("IP");

            var portField = FindComponentByTestId<MudNumericField<int?>>(dialog.Component, "AddPeerPort");
            portField.Find("input").Change("6881");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddPeerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            FindComponentByTestId<MudIconButton>(dialog.Component, "DeletePeer-IP-6881").Should().NotBeNull();
            ipField.Instance.Value.Should().BeNull();
            portField.Instance.Value.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_DeletePeerInvoked_THEN_PeerRemoved()
        {
            var dialog = await _target.RenderDialogAsync();

            var ipField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddPeerIp");
            ipField.Find("input").Change("IP");

            var portField = FindComponentByTestId<MudNumericField<int?>>(dialog.Component, "AddPeerPort");
            portField.Find("input").Change("6881");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddPeerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "DeletePeer-IP-6881");
            await deleteButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddPeerCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_SubmitInvoked_THEN_ResultContainsPeers()
        {
            var dialog = await _target.RenderDialogAsync();

            var ipField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddPeerIp");
            ipField.Find("input").Change("IP");

            var portField = FindComponentByTestId<MudNumericField<int?>>(dialog.Component, "AddPeerPort");
            portField.Find("input").Change("6881");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddPeerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "AddPeerSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var peers = (HashSet<PeerId>)result.Data!;
            peers.Should().ContainSingle(peer => peer.Host == "IP" && peer.Port == 6881);
        }

        [Fact]
        public async Task GIVEN_PeerAdded_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsPeers()
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

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            var ipField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddPeerIp");
            ipField.Find("input").Change("IP");

            var portField = FindComponentByTestId<MudNumericField<int?>>(dialog.Component, "AddPeerPort");
            portField.Find("input").Change("6881");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddPeerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var peers = (HashSet<PeerId>)result.Data!;
            peers.Should().ContainSingle(peer => peer.Host == "IP" && peer.Port == 6881);
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

            var reference = await dialogService.ShowAsync<AddPeerDialog>("Add Peer");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddPeerDialog>();

            return new AddPeerDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddPeerDialogRenderContext
    {
        public AddPeerDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddPeerDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddPeerDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
