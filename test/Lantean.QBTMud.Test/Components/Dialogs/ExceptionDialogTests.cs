using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class ExceptionDialogTests : RazorComponentTestBase<ExceptionDialog>
    {
        private readonly ExceptionDialogTestDriver _target;

        public ExceptionDialogTests()
        {
            _target = new ExceptionDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_ExceptionNull_WHEN_Rendered_THEN_ShowsMissingAlert()
        {
            var dialog = await _target.RenderDialogAsync(null);

            GetChildContentText(FindComponentByTestId<MudAlert>(dialog.Component, "ExceptionMissingMessage").Instance.ChildContent)
                .Should()
                .Be("Missing error information.");
            dialog.Component.FindComponents<MudField>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ExceptionProvided_WHEN_Rendered_THEN_ShowsFields()
        {
            var exception = new InvalidOperationException("Message")
            {
                Source = "Source",
            };

            var dialog = await _target.RenderDialogAsync(exception);

            dialog.Component.FindComponents<MudField>().Should().HaveCount(3);
            GetChildContentText(FindComponentByTestId<MudField>(dialog.Component, "ExceptionMessage").Instance.ChildContent).Should().Be("Message");
            GetChildContentText(FindComponentByTestId<MudField>(dialog.Component, "ExceptionSource").Instance.ChildContent).Should().Be("Source");
            GetChildContentText(FindComponentByTestId<MudField>(dialog.Component, "ExceptionStackTrace").Instance.ChildContent)
                .Should()
                .Be(exception.StackTrace ?? string.Empty);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CloseInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync(null);

            var closeButton = FindComponentByTestId<MudButton>(dialog.Component, "ExceptionClose");
            closeButton.Instance.Variant.Should().Be(Variant.Filled);
            closeButton.Instance.Color.Should().Be(Color.Success);
            await closeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }
    }

    internal sealed class ExceptionDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ExceptionDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ExceptionDialogRenderContext> RenderDialogAsync(Exception? exception)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();

            if (exception is not null)
            {
                parameters.Add(nameof(ExceptionDialog.Exception), exception);
            }

            var reference = await dialogService.ShowAsync<ExceptionDialog>("Exception", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ExceptionDialog>();

            return new ExceptionDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ExceptionDialogRenderContext
    {
        public ExceptionDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ExceptionDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ExceptionDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
