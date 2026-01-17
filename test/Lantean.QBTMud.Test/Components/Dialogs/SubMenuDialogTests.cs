using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
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
    public sealed class SubMenuDialogTests : RazorComponentTestBase<SubMenuDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly SubMenuDialogTestDriver _target;

        public SubMenuDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new SubMenuDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_SubMenuRendered_WHEN_CloseClicked_THEN_ResultOk()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponent<TorrentActions>();

            var closeButton = FindComponentByTestId<MudButton>(dialog.Component, "SubMenuClose");
            await closeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SubMenuRendered_WHEN_CancelClicked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "SubMenuCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }
    }

    internal sealed class SubMenuDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public SubMenuDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<SubMenuDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(SubMenuDialog.ParentAction), CreateParentAction() },
                { nameof(SubMenuDialog.Hashes), Array.Empty<string>() },
                { nameof(SubMenuDialog.Torrents), new Dictionary<string, Torrent>() },
                { nameof(SubMenuDialog.Tags), new HashSet<string>() },
                { nameof(SubMenuDialog.Categories), new Dictionary<string, Category>() },
            };

            var reference = await dialogService.ShowAsync<SubMenuDialog>("SubMenu", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<SubMenuDialog>();

            return new SubMenuDialogRenderContext(provider, dialog, component, reference);
        }

        private static UIAction CreateParentAction()
        {
            return new UIAction("queue", "Queue", Icons.Material.Filled.Queue, Color.Info, Array.Empty<UIAction>());
        }
    }

    internal sealed class SubMenuDialogRenderContext
    {
        public SubMenuDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<SubMenuDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<SubMenuDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
