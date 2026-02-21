using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class StorageDiagnosticsServiceTests
    {
        private readonly Mock<IJSRuntime> _jsRuntime;
        private readonly StorageDiagnosticsService _target;

        public StorageDiagnosticsServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            _target = new StorageDiagnosticsService(_jsRuntime.Object);
        }

        [Fact]
        public async Task GIVEN_PrefixedEntries_WHEN_GetEntriesInvoked_THEN_ReturnsNormalizedEntries()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(
                [
                    new BrowserStorageEntry("QbtMud.Beta", "b"),
                    new BrowserStorageEntry("QbtMud.Alpha", new string('x', 200))
                ]);

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().HaveCount(2);
            result[0].Key.Should().Be("QbtMud.Alpha");
            result[0].DisplayKey.Should().Be("Alpha");
            result[0].Length.Should().Be(200);
            result[0].Preview.Length.Should().Be(163);
            result[1].Key.Should().Be("QbtMud.Beta");
        }

        [Fact]
        public async Task GIVEN_NonPrefixedKey_WHEN_RemoveEntryInvoked_THEN_DoesNotCallRuntime()
        {
            await _target.RemoveEntryAsync("Other.Key", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_PrefixedKey_WHEN_RemoveEntryInvoked_THEN_CallsRuntime()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

            await _target.RemoveEntryAsync("QbtMud.Key", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearRequested_WHEN_ClearEntriesInvoked_THEN_ReturnsRemovedCount()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<int>(
                    "qbt.clearLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(3);

            var removed = await _target.ClearEntriesAsync(TestContext.Current.CancellationToken);

            removed.Should().Be(3);
            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<int>(
                    "qbt.clearLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_EntriesWithEmptyValuesAndInvalidKeys_WHEN_GetEntriesInvoked_THEN_UsesEmptyPreviewAndFiltersInvalidKeys()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(
                [
                    new BrowserStorageEntry("QbtMud.Empty", string.Empty),
                    new BrowserStorageEntry("QbtMud.Null", null),
                    new BrowserStorageEntry("Other.Key", "value"),
                    new BrowserStorageEntry(" ", "value"),
                    null!
                ]);

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().HaveCount(3);
            result[0].DisplayKey.Should().Be("Other.Key");
            result[0].Preview.Should().Be("value");
            result[0].Length.Should().Be(5);
            result[1].DisplayKey.Should().Be("Empty");
            result[1].Preview.Should().BeEmpty();
            result[1].Length.Should().Be(0);
            result[2].DisplayKey.Should().Be("Null");
            result[2].Preview.Should().BeEmpty();
            result[2].Length.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_WhitespaceKey_WHEN_RemoveEntryInvoked_THEN_DoesNotCallRuntime()
        {
            await _target.RemoveEntryAsync("   ", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }
    }
}
