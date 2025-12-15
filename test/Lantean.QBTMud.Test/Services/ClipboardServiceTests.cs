using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public class ClipboardServiceTests
    {
        private readonly IJSRuntime _jsRuntime;

        private readonly ClipboardService _target;

        public ClipboardServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict).Object;
            _target = new ClipboardService(_jsRuntime);
        }

        [Fact]
        public async Task GIVEN_TextProvided_WHEN_WriteToClipboard_THEN_ShouldInvokeNavigatorClipboard()
        {
            const string text = "text";
            Mock.Get(_jsRuntime)
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "navigator.clipboard.writeText",
                    It.Is<object?[]>(args => MatchesClipboardArgs(args, text))))
                .Returns(ValueTask.FromResult<IJSVoidResult>(default!));

            await _target.WriteToClipboard(text);

            Mock.Get(_jsRuntime).Verify(js => js.InvokeAsync<IJSVoidResult>(
                "navigator.clipboard.writeText",
                It.Is<object?[]>(args => MatchesClipboardArgs(args, text))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClipboardUnavailable_WHEN_WriteToClipboard_THEN_ShouldSwallowJsException()
        {
            const string text = "text";
            Mock.Get(_jsRuntime)
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "navigator.clipboard.writeText",
                    It.Is<object?[]>(args => MatchesClipboardArgs(args, text))))
                .Returns(new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(new JSException("denied"))));

            await _target.WriteToClipboard(text);

            Mock.Get(_jsRuntime).Verify(js => js.InvokeAsync<IJSVoidResult>(
                "navigator.clipboard.writeText",
                It.Is<object?[]>(args => MatchesClipboardArgs(args, text))), Times.Once);
        }

        private static bool MatchesClipboardArgs(object?[]? args, string expectedText)
        {
            if (args is null || args.Length != 1)
            {
                return false;
            }

            return (string?)args[0] == expectedText;
        }
    }
}
