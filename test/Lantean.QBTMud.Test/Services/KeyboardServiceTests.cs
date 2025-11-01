using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class KeyboardServiceTests : IAsyncLifetime
    {
        private readonly Mock<IJSRuntime> _jsRuntime;

        private readonly KeyboardService _target;

        public KeyboardServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            _target = new KeyboardService(_jsRuntime.Object);
        }

        [Fact]
        public async Task GIVEN_Keypress_WHEN_RegisterKeypressEvent_THEN_ShouldInvokeJsWithReference()
        {
            _jsRuntime.Invocations.Clear();
            var criteria = new KeyboardEvent("Key");
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.registerKeypressEvent",
                    It.Is<object?[]>(args => MatchesRegisterArgs(args, criteria, _target))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.unregisterKeypressEvent",
                    It.Is<object?[]>(args => MatchesRegisterArgs(args, criteria, _target))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));

            await _target.RegisterKeypressEvent(criteria, _ => Task.CompletedTask);

            _jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.registerKeypressEvent",
                It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))), Times.Once);
            await _target.UnregisterKeypressEvent(criteria);
            _jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.unregisterKeypressEvent",
                It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_KeypressRegistered_WHEN_HandleKeyPressEventInvoked_THEN_ShouldCallHandler()
        {
            _jsRuntime.Invocations.Clear();
            var criteria = new KeyboardEvent("Enter");
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.registerKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.unregisterKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            var invoked = false;

            await _target.RegisterKeypressEvent(criteria, _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });
            await _target.HandleKeyPressEvent(new KeyboardEvent("Enter"));

            invoked.Should().BeTrue();
            await _target.UnregisterKeypressEvent(criteria);
        }

        [Fact]
        public async Task GIVEN_KeypressNotRegistered_WHEN_HandleKeyPressEventInvoked_THEN_ShouldNotCallHandler()
        {
            _jsRuntime.Invocations.Clear();
            var invoked = false;

            await _target.HandleKeyPressEvent(new KeyboardEvent("Escape"));

            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_KeypressRegistered_WHEN_UnregisterKeypressEvent_THEN_ShouldInvokeJsAndRemoveHandler()
        {
            _jsRuntime.Invocations.Clear();
            var criteria = new KeyboardEvent("Space");
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.registerKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.unregisterKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            var invoked = false;

            await _target.RegisterKeypressEvent(criteria, _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });
            await _target.UnregisterKeypressEvent(criteria);

            _jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.unregisterKeypressEvent",
                It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))), Times.Once);
            await _target.HandleKeyPressEvent(new KeyboardEvent("Space"));
            invoked.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_KeyboardService_WHEN_Focus_THEN_ShouldInvokeJs()
        {
            _jsRuntime.Invocations.Clear();
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.keyPressFocusInstance",
                    It.Is<object?[]>(args => MatchesFocusArgs(args, _target))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));

            await _target.Focus();

            _jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.keyPressFocusInstance",
                It.Is<object?[]>(args => MatchesSingleArg(args))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_KeyboardService_WHEN_UnFocus_THEN_ShouldInvokeJs()
        {
            _jsRuntime.Invocations.Clear();
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.keyPressUnFocusInstance",
                    It.Is<object?[]>(args => MatchesFocusArgs(args, _target))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));

            await _target.UnFocus();

            _jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.keyPressUnFocusInstance",
                It.Is<object?[]>(args => MatchesSingleArg(args))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_KeypressRegistered_WHEN_DisposeAsync_THEN_ShouldUnregisterHandlers()
        {
            var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            var criteria = new KeyboardEvent("Tab");
            jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.registerKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.keyPressUnFocusInstance",
                    It.Is<object?[]>(args => MatchesFocusArgs(args, null))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.unregisterKeypressEvent",
                    It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));

            await using (var target = new KeyboardService(jsRuntime.Object))
            {
                await target.RegisterKeypressEvent(criteria, _ => Task.CompletedTask);
            }

            jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.registerKeypressEvent",
                It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))), Times.Once);
            jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.keyPressUnFocusInstance",
                It.Is<object?[]>(args => MatchesSingleArg(args))), Times.Once);
            jsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "qbt.unregisterKeypressEvent",
                It.Is<object?[]>(args => MatchesCriteriaArgs(args, criteria))), Times.Once);
        }

        private static bool MatchesRegisterArgs(object?[]? args, KeyboardEvent criteria, KeyboardService target)
        {
            if (args is null || args.Length != 2)
            {
                return false;
            }

            if (!ReferenceEquals(args[0], criteria))
            {
                return false;
            }

            if (args[1] is not DotNetObjectReference<KeyboardService> reference)
            {
                return false;
            }

            return ReferenceEquals(reference.Value, target);
        }

        private static bool MatchesCriteriaArgs(object?[]? args, KeyboardEvent criteria)
        {
            if (args is null || args.Length != 2)
            {
                return false;
            }

            if (ReferenceEquals(args[0], criteria))
            {
                return true;
            }

            if (args[0] is string key)
            {
                return string.Equals(key, criteria.ToString(), StringComparison.Ordinal);
            }

            return false;
        }

        private static bool MatchesFocusArgs(object?[]? args, KeyboardService? target)
        {
            if (args is null || args.Length != 1)
            {
                return false;
            }

            if (args[0] is not DotNetObjectReference<KeyboardService> reference)
            {
                return false;
            }

            return target is null ? reference.Value is not null : ReferenceEquals(reference.Value, target);
        }

        private static bool MatchesSingleArg(object?[]? args)
        {
            return args is not null && args.Length == 1;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.keyPressUnFocusInstance",
                    It.IsAny<object?[]>()))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            _jsRuntime
                .Setup(js => js.InvokeAsync<IJSVoidResult>(
                    "qbt.unregisterKeypressEvent",
                    It.IsAny<object?[]>()))
                .Returns(() => ValueTask.FromResult<IJSVoidResult>(default!));
            await _target.DisposeAsync();
        }
    }
}
