using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class ErrorDisplayTests : RazorComponentTestBase<ErrorDisplay>
    {
        private readonly IDialogService _dialogService;
        private readonly Mock<ILanguageLocalizer> _languageLocalizerMock;
        private readonly IRenderedComponent<ErrorDisplay> _target;

        public ErrorDisplayTests()
        {
            _dialogService = Mock.Of<IDialogService>();
            _languageLocalizerMock = new Mock<ILanguageLocalizer>();
            _languageLocalizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] __) => source);

            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<ExceptionDialog>(
                    It.IsAny<string>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(Mock.Of<IDialogReference>());

            TestContext.Services.RemoveAll<IDialogService>();
            TestContext.Services.RemoveAll<ILanguageLocalizer>();
            TestContext.Services.AddSingleton(_dialogService);
            TestContext.Services.AddSingleton(_languageLocalizerMock.Object);

            _target = RenderErrorDisplay(CreateErrorBoundary());
        }

        [Fact]
        public void GIVEN_ErrorBoundaryWithoutErrors_WHEN_Rendered_THEN_RendersOnlyActionItems()
        {
            var listItems = _target.FindComponents<MudListItem<string>>();

            listItems.Should().HaveCount(2);
            GetChildContentText(listItems[0].Instance.ChildContent).Should().Be("Clear Errors");
            GetChildContentText(listItems[1].Instance.ChildContent).Should().Be("Clear Errors and Resume");
        }

        [Fact]
        public async Task GIVEN_ErrorItemClicked_WHEN_ShowExceptionInvoked_THEN_OpensDialogWithExpectedParameters()
        {
            var exception = new InvalidOperationException("Boom");
            var boundary = CreateErroredBoundary(exception);
            var target = RenderErrorDisplay(boundary);
            var listItems = target.FindComponents<MudListItem<string>>();

            listItems.Should().HaveCount(3);
            await target.InvokeAsync(() => listItems[2].Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogService).Verify(service => service.ShowAsync<ExceptionDialog>(
                "Error Details",
                It.Is<DialogParameters>(parameters => HasExceptionDialogParameters(parameters, exception)),
                Lantean.QBTMud.Services.DialogWorkflow.FormDialogOptions), Times.Once);
            _languageLocalizerMock.Verify(
                localizer => localizer.Translate("AppErrorDisplay", "Error Details", It.IsAny<object[]>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearErrorsClicked_WHEN_ActionInvoked_THEN_ClearsErrorsWithoutRecoveringBoundary()
        {
            var exception = new InvalidOperationException("Boom");
            var boundary = CreateErroredBoundary(exception);
            var target = RenderErrorDisplay(boundary);
            var listItems = target.FindComponents<MudListItem<string>>();

            await target.InvokeAsync(() => listItems[0].Instance.OnClick.InvokeAsync());

            boundary.Errors.Should().BeEmpty();
            boundary.HasErrored.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ClearErrorsAndResumeClicked_WHEN_ActionInvoked_THEN_ClearsErrorsAndRecoversBoundary()
        {
            var exception = new InvalidOperationException("Boom");
            var boundary = CreateErroredBoundary(exception);
            var target = RenderErrorDisplay(boundary);
            var listItems = target.FindComponents<MudListItem<string>>();

            await target.InvokeAsync(() => listItems[1].Instance.OnClick.InvokeAsync());

            boundary.Errors.Should().BeEmpty();
            boundary.HasErrored.Should().BeFalse();
        }

        private IRenderedComponent<ErrorDisplay> RenderErrorDisplay(EnhancedErrorBoundary errorBoundary)
        {
            return TestContext.Render<ErrorDisplay>(parameters =>
            {
                parameters.Add(parameter => parameter.ErrorBoundary, errorBoundary);
            });
        }

        private EnhancedErrorBoundary CreateErrorBoundary()
        {
            var boundary = TestContext.Render<EnhancedErrorBoundary>(parameters =>
            {
                parameters.AddChildContent(_ => { });
            });

            return boundary.Instance;
        }

        private EnhancedErrorBoundary CreateErroredBoundary(Exception exception)
        {
            var boundary = TestContext.Render<EnhancedErrorBoundary>(parameters =>
            {
                parameters.AddChildContent<ThrowOnClick>(child =>
                {
                    child.Add(parameter => parameter.Exception, exception);
                });
            });

            boundary.Find("#throw-button").Click();
            boundary.Render();

            boundary.Instance.Errors.Should().ContainSingle();
            return boundary.Instance;
        }

        private static bool HasExceptionDialogParameters(DialogParameters parameters, Exception exception)
        {
            return parameters.Any(parameter => parameter.Key == nameof(ExceptionDialog.Exception))
                   && ReferenceEquals(parameters[nameof(ExceptionDialog.Exception)], exception);
        }

        private sealed class ThrowOnClick : ComponentBase
        {
            [Parameter]
            [EditorRequired]
            public Exception Exception { get; set; } = default!;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "id", "throw-button");
                builder.AddAttribute(2, "type", "button");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
                builder.AddContent(4, "Throw");
                builder.CloseElement();
            }

            private Task OnClick(MouseEventArgs args)
            {
                throw Exception;
            }
        }
    }
}
