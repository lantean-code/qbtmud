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
    public sealed class AddTrackerDialogTests : RazorComponentTestBase<AddTrackerDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddTrackerDialogTestDriver _target;

        public AddTrackerDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddTrackerDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_EmptyInput_WHEN_AddTrackerInvoked_THEN_TrackersRemainEmpty()
        {
            var dialog = await _target.RenderDialogAsync();

            var trackerField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTrackerInput");
            trackerField.Find("input").Change(string.Empty);

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
            trackerField.Instance.Value.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_ValidInput_WHEN_AddTrackerInvoked_THEN_TrackerAddedAndFieldCleared()
        {
            var dialog = await _target.RenderDialogAsync();

            var trackerField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTrackerInput");
            trackerField.Find("input").Change("Tracker");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerDelete-Tracker").Should().NotBeNull();
            trackerField.Instance.Value.Should().BeNull();
            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(2);
        }

        [Fact]
        public async Task GIVEN_TrackerAdded_WHEN_DeleteInvoked_THEN_TrackerRemoved()
        {
            var dialog = await _target.RenderDialogAsync();

            var trackerField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTrackerInput");
            trackerField.Find("input").Change("Tracker");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerDelete-Tracker");
            await deleteButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Component.FindComponents<MudIconButton>().Should().HaveCount(1);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTrackerCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TrackerAdded_WHEN_SubmitInvoked_THEN_ResultContainsTracker()
        {
            var dialog = await _target.RenderDialogAsync();

            var trackerField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTrackerInput");
            trackerField.Find("input").Change("Tracker");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTrackerSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var trackers = (HashSet<string>)result.Data!;
            trackers.Should().ContainSingle(item => item == "Tracker");
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultContainsTrackers()
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

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            var trackerField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTrackerInput");
            trackerField.Find("input").Change("Tracker");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "AddTrackerAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var trackers = (HashSet<string>)result.Data!;
            trackers.Should().ContainSingle(item => item == "Tracker");
        }
    }

    internal sealed class AddTrackerDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddTrackerDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<AddTrackerDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<AddTrackerDialog>("Add Trackers");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTrackerDialog>();

            return new AddTrackerDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTrackerDialogRenderContext
    {
        public AddTrackerDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTrackerDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTrackerDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
