using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
{
    public sealed class CategoryPropertiesDialogTests : RazorComponentTestBase<CategoryPropertiesDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly CategoryPropertiesDialogTestDriver _target;
        private QBittorrentPreferences? _preferences;

        public CategoryPropertiesDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new CategoryPropertiesDialogTestDriver(TestContext, () => _preferences);
        }

        [Fact]
        public async Task GIVEN_SavePathNull_WHEN_Rendered_THEN_DefaultSavePathApplied()
        {
            UsePreferences("SavePath");

            var dialog = await _target.RenderDialogAsync(category: "Category");

            var categoryField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CategoryPropertiesCategory");
            var savePathField = FindComponentByTestId<PathAutocomplete>(dialog.Component, "CategoryPropertiesSavePath");

            categoryField.Instance.GetState(x => x.Value).Should().Be("Category");
            savePathField.Instance.Value.Should().Be("SavePath");
        }

        [Fact]
        public async Task GIVEN_EmptyCategory_WHEN_SubmitInvoked_THEN_DoesNotClose()
        {
            UsePreferences("SavePath");

            var dialog = await _target.RenderDialogAsync();

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CategoryPropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Reference.Result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_EmptySavePath_WHEN_Submitted_THEN_UsesDefaultSavePath()
        {
            UsePreferences("SavePath");

            var dialog = await _target.RenderDialogAsync(category: "Category", savePath: string.Empty);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CategoryPropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var category = (Category)result.Data!;
            category.Name.Should().Be("Category");
            category.SavePath.Should().Be("SavePath");
        }

        [Fact]
        public async Task GIVEN_ValidInputs_WHEN_Submitted_THEN_ResultContainsCategory()
        {
            UsePreferences("DefaultSavePath");

            var dialog = await _target.RenderDialogAsync();

            var categoryField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CategoryPropertiesCategory");
            categoryField.Find("input").Change("Category");

            var savePathField = FindComponentByTestId<PathAutocomplete>(dialog.Component, "CategoryPropertiesSavePath");
            await dialog.Component.InvokeAsync(() => savePathField.Instance.ValueChanged.InvokeAsync("SavePath"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CategoryPropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var category = (Category)result.Data!;
            category.Name.Should().Be("Category");
            category.SavePath.Should().Be("SavePath");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            UsePreferences("SavePath");

            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "CategoryPropertiesCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultContainsCategory()
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

            UsePreferences("SavePath");

            var dialog = await _target.RenderDialogAsync(category: "Category", savePath: "SavePath");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var category = (Category)result.Data!;
            category.Name.Should().Be("Category");
            category.SavePath.Should().Be("SavePath");
        }

        private void UsePreferences(string savePath)
        {
            var preferences = PreferencesFactory.CreateQBittorrentPreferences(spec =>
            {
                spec.SavePath = savePath;
            });

            _preferences = preferences;
        }
    }

    internal sealed class CategoryPropertiesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;
        private readonly Func<QBittorrentPreferences?> _getPreferences;

        public CategoryPropertiesDialogTestDriver(ComponentTestContext testContext, Func<QBittorrentPreferences?> getPreferences)
        {
            _testContext = testContext;
            _getPreferences = getPreferences;
        }

        public async Task<CategoryPropertiesDialogRenderContext> RenderDialogAsync(string? category = null, string? savePath = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (category is not null)
            {
                parameters.Add(nameof(CategoryPropertiesDialog.Category), category);
            }

            if (savePath is not null)
            {
                parameters.Add(nameof(CategoryPropertiesDialog.SavePath), savePath);
            }

            var preferences = _getPreferences();
            if (preferences is not null)
            {
                parameters.Add(nameof(CategoryPropertiesDialog.Preferences), preferences);
            }

            var reference = await dialogService.ShowAsync<CategoryPropertiesDialog>("Category properties", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<CategoryPropertiesDialog>();

            return new CategoryPropertiesDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class CategoryPropertiesDialogRenderContext
    {
        public CategoryPropertiesDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<CategoryPropertiesDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<CategoryPropertiesDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
