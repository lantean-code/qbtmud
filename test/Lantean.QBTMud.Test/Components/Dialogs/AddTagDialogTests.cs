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
    public sealed class AddTagDialogTests : RazorComponentTestBase<AddTagDialog>
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

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTagAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
            tagField.Instance.Value.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TagSet_WHEN_AddTagInvoked_THEN_TagAddedAndFieldCleared()
        {
            var dialog = await _target.RenderDialogAsync();

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            tagField.Find("input").Change("Tag");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTagAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            tagField.Instance.Value.Should().BeNull();

            FindComponentByTestId<MudIconButton>(dialog.Component, "DeleteTag-Tag").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_DeleteInvoked_THEN_TagRemoved()
        {
            var dialog = await _target.RenderDialogAsync();

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            tagField.Find("input").Change("Tag");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTagAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "DeleteTag-Tag");
            await deleteButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTagCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TagSetAndNoExistingTags_WHEN_SubmitInvoked_THEN_ResultContainsTag()
        {
            var dialog = await _target.RenderDialogAsync();

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            tagField.Find("input").Change("Tag");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTagSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
        }

        [Fact]
        public async Task GIVEN_NoTagsAndNoInput_WHEN_SubmitInvoked_THEN_ResultContainsNoTags()
        {
            var dialog = await _target.RenderDialogAsync();

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTagSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_SubmitInvoked_THEN_ResultContainsTags()
        {
            var dialog = await _target.RenderDialogAsync();

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            tagField.Find("input").Change("Tag");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTagAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTagSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
        }

        [Fact]
        public async Task GIVEN_TagAdded_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsTags()
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

            var tagField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTagInput");
            tagField.Find("input").Change("Tag");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTagAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var tags = (HashSet<string>)result.Data!;
            tags.Should().ContainSingle().Which.Should().Be("Tag");
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

            var reference = await dialogService.ShowAsync<AddTagDialog>("Add Tag");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTagDialog>();

            return new AddTagDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTagDialogRenderContext
    {
        public AddTagDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTagDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTagDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
