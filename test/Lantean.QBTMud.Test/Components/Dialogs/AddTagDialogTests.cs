using AwesomeAssertions;
using Bunit;
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
    public sealed class AddTagDialogTests : RazorComponentTestBase<AddTagDialogTestHarness>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddTagDialogTestDriver _target;

        public AddTagDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddTagDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_EmptyInput_WHEN_AddTagInvoked_THEN_TagsRemainEmpty()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeAddTag();

            dialog.Component.Instance.GetTags().Should().BeEmpty();
            dialog.Component.Instance.TagValue.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TagSet_WHEN_AddTagInvoked_THEN_TagAddedAndFieldCleared()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetTag("Tag");
            dialog.Component.Instance.InvokeAddTag();
            dialog.Component.Render();

            dialog.Component.Instance.GetTags().Should().ContainSingle().Which.Should().Be("Tag");
            dialog.Component.Instance.TagValue.Should().BeNull();

            FindComponentByTestId<MudIconButton>(dialog.Component, "DeleteTag-Tag").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_DeleteInvoked_THEN_TagRemoved()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetTag("Tag");
            dialog.Component.Instance.InvokeAddTag();
            dialog.Component.Render();

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "DeleteTag-Tag");
            await deleteButton.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());

            dialog.Component.Instance.GetTags().Should().BeEmpty();
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
        public async Task GIVEN_TagSetAndNoExistingTags_WHEN_SubmitInvoked_THEN_ResultContainsTag()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetTag("Tag");

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmit());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_SubmitInvoked_THEN_ResultContainsTags()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetTag("Tag");
            dialog.Component.Instance.InvokeAddTag();

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmit());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsTags()
        {
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.Instance.InvokeSetTag("Tag");
            dialog.Component.Instance.InvokeAddTag();

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.InvokeSubmitWithKeyboardAsync(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
        }
    }

    public sealed class AddTagDialogTestHarness : AddTagDialog
    {
        public HashSet<string> GetTags()
        {
            return Tags;
        }

        public string? TagValue
        {
            get
            {
                return Tag;
            }
        }

        public void InvokeSetTag(string tag)
        {
            SetTag(tag);
        }

        public void InvokeAddTag()
        {
            AddTag();
        }

        public void InvokeDeleteTag(string tag)
        {
            DeleteTag(tag);
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

    internal sealed class AddTagDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddTagDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<AddTagDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<AddTagDialogTestHarness>("Add Tag");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTagDialogTestHarness>();

            return new AddTagDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTagDialogRenderContext
    {
        public AddTagDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTagDialogTestHarness> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTagDialogTestHarness> Component { get; }

        public IDialogReference Reference { get; }
    }
}
