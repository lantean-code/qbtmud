using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class DeleteDialogTests : RazorComponentTestBase<DeleteDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly IWebUiLocalizer _webUiLocalizer;
        private readonly DeleteDialogTestDriver _target;

        public DeleteDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _webUiLocalizer = Mock.Of<IWebUiLocalizer>();
            var localizerMock = Mock.Get(_webUiLocalizer);
            localizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] args) => FormatLocalizedString(source, args));

            TestContext.Services.RemoveAll<IWebUiLocalizer>();
            TestContext.Services.AddSingleton(_webUiLocalizer);

            _target = new DeleteDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_SingleTorrent_WHEN_Rendered_THEN_RendersSingularMessage()
        {
            var dialog = await _target.RenderDialogAsync(1, "TorrentName");

            GetChildContentText(FindComponentByTestId<MudText>(dialog.Component, "DeleteDialogMessage").Instance.ChildContent)
                .Should()
                .Be("Are you sure you want to remove \"TorrentName\" from the transfer list?");
        }

        [Fact]
        public async Task GIVEN_MultipleTorrents_WHEN_Rendered_THEN_RendersPluralMessage()
        {
            var dialog = await _target.RenderDialogAsync(2);

            GetChildContentText(FindComponentByTestId<MudText>(dialog.Component, "DeleteDialogMessage").Instance.ChildContent)
                .Should()
                .Be("Are you sure you want to remove these 2 torrents from the transfer list?");
        }

        [Fact]
        public async Task GIVEN_DeleteUnchecked_WHEN_SubmitInvoked_THEN_ResultFalse()
        {
            var dialog = await _target.RenderDialogAsync(1, "TorrentName");

            var removeButton = FindComponentByTestId<MudButton>(dialog.Component, "DeleteDialogRemove");
            await removeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(false);
        }

        [Fact]
        public async Task GIVEN_DeleteChecked_WHEN_SubmitInvoked_THEN_ResultTrue()
        {
            var dialog = await _target.RenderDialogAsync(1, "TorrentName");

            var deleteCheckBox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "DeleteFiles");
            deleteCheckBox.Find("input").Change(true);

            var removeButton = FindComponentByTestId<MudButton>(dialog.Component, "DeleteDialogRemove");
            await removeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync(1, "TorrentName");

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "DeleteDialogCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultFalse()
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

            var dialog = await _target.RenderDialogAsync(1, "TorrentName");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(false);
        }

        private static string FormatLocalizedString(string source, object[] args)
        {
            if (args.Length == 0)
            {
                return source;
            }

            var result = source;
            for (var i = 0; i < args.Length; i++)
            {
                var replacement = args[i]?.ToString() ?? string.Empty;
                result = result.Replace($"%{i + 1}", replacement, StringComparison.Ordinal);
            }

            return result;
        }
    }

    internal sealed class DeleteDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public DeleteDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<DeleteDialogRenderContext> RenderDialogAsync(int count, string? torrentName = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(DeleteDialog.Count), count },
            };
            if (!string.IsNullOrWhiteSpace(torrentName))
            {
                parameters.Add(nameof(DeleteDialog.TorrentName), torrentName);
            }

            var reference = await dialogService.ShowAsync<DeleteDialog>("Delete", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<DeleteDialog>();

            return new DeleteDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class DeleteDialogRenderContext
    {
        public DeleteDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<DeleteDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<DeleteDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
