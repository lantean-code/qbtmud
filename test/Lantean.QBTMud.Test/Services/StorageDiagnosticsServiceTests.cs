using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class StorageDiagnosticsServiceTests
    {
        private readonly Mock<IJSRuntime> _jsRuntime;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly StorageDiagnosticsService _target;

        public StorageDiagnosticsServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            _clientDataStorageAdapter = Mock.Of<IClientDataStorageAdapter>();
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            _target = new StorageDiagnosticsService(_jsRuntime.Object, _clientDataStorageAdapter, _webApiCapabilityService);
        }

        [Fact]
        public async Task GIVEN_LocalAndClientEntries_WHEN_GetEntriesInvoked_THEN_ShouldReturnMergedEntriesWithStorageType()
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

            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.ClientOnly"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone()
                    }));

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().HaveCount(3);
            result.Should().Contain(entry => entry.StorageType == StorageType.LocalStorage && entry.DisplayKey == "Alpha");
            result.Should().Contain(entry => entry.StorageType == StorageType.LocalStorage && entry.DisplayKey == "Beta");
            result.Should().Contain(entry => entry.StorageType == StorageType.ClientData && entry.DisplayKey == "ClientOnly");
            result.Single(entry => entry.DisplayKey == "Alpha").Preview.Length.Should().Be(163);
        }

        [Fact]
        public async Task GIVEN_LocalStorageTypeEntry_WHEN_RemoveEntryInvoked_THEN_ShouldCallRuntimeRemove()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));

            await _target.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.Key", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.removeLocalStorageEntry",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeEntryWithSupport_WHEN_RemoveEntryInvoked_THEN_ShouldCallClientDataAdapter()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.RemoveEntryAsync(StorageType.ClientData, "QbtMud.Key", TestContext.Current.CancellationToken);

            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.RemovePrefixedEntriesAsync(
                    It.Is<IEnumerable<string>>(keys => keys.Contains("QbtMud.Key", StringComparer.Ordinal)),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearRequested_WHEN_ClearEntriesInvoked_THEN_ShouldClearBothStorageTypes()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<int>(
                    "qbt.clearLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(3);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.A"] = JsonDocument.Parse("1").RootElement.Clone(),
                        ["QbtMud.B"] = JsonDocument.Parse("2").RootElement.Clone()
                    }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            var removed = await _target.ClearEntriesAsync(cancellationToken: TestContext.Current.CancellationToken);

            removed.Should().Be(5);
        }

        [Fact]
        public async Task GIVEN_ClientDataUnsupported_WHEN_GetEntriesInvoked_THEN_ShouldReturnLocalOnly()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync([new BrowserStorageEntry("QbtMud.LocalOnly", "{}")]);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().HaveCount(1);
            result[0].StorageType.Should().Be(StorageType.LocalStorage);
            result[0].DisplayKey.Should().Be("LocalOnly");
        }

        [Fact]
        public async Task GIVEN_ClientDataLoadFails_WHEN_GetEntriesInvoked_THEN_ShouldReturnLocalOnly()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync([new BrowserStorageEntry("QbtMud.LocalOnly", "{}")]);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.Failure);

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().ContainSingle();
            result[0].StorageType.Should().Be(StorageType.LocalStorage);
            result[0].DisplayKey.Should().Be("LocalOnly");
        }

        [Fact]
        public async Task GIVEN_WhitespaceKey_WHEN_RemoveEntryInvoked_THEN_ShouldDoNothing()
        {
            await _target.RemoveEntryAsync(StorageType.LocalStorage, " ", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotPrefixedKey_WHEN_RemoveEntryInvoked_THEN_ShouldDoNothing()
        {
            await _target.RemoveEntryAsync(StorageType.LocalStorage, "Other.Key", TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeWithoutSupport_WHEN_RemoveEntryInvoked_THEN_ShouldNotCallAdapter()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            await _target.RemoveEntryAsync(StorageType.ClientData, "QbtMud.Key", TestContext.Current.CancellationToken);

            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_LocalStorageTypeRequested_WHEN_ClearEntriesInvoked_THEN_ShouldOnlyClearLocalEntries()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<int>(
                    "qbt.clearLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(4);

            var removed = await _target.ClearEntriesAsync(StorageType.LocalStorage, TestContext.Current.CancellationToken);

            removed.Should().Be(4);
            Mock.Get(_webApiCapabilityService).Verify(
                service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeUnsupported_WHEN_ClearEntriesInvoked_THEN_ShouldReturnZero()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var removed = await _target.ClearEntriesAsync(StorageType.ClientData, TestContext.Current.CancellationToken);

            removed.Should().Be(0);
            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<int>(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeHasNoEntries_WHEN_ClearEntriesInvoked_THEN_ShouldNotCallRemove()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)));

            var removed = await _target.ClearEntriesAsync(StorageType.ClientData, TestContext.Current.CancellationToken);

            removed.Should().Be(0);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeLoadFails_WHEN_ClearEntriesInvoked_THEN_ShouldReturnZero()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.Failure);

            var removed = await _target.ClearEntriesAsync(StorageType.ClientData, TestContext.Current.CancellationToken);

            removed.Should().Be(0);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientStorageTypeRemoveFails_WHEN_ClearEntriesInvoked_THEN_ShouldReturnZero()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.A"] = JsonDocument.Parse("1").RootElement.Clone()
                    }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure);

            var removed = await _target.ClearEntriesAsync(StorageType.ClientData, TestContext.Current.CancellationToken);

            removed.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_EmptyAndStringClientValues_WHEN_GetEntriesInvoked_THEN_ShouldConvertValuesAndPreviews()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync([new BrowserStorageEntry("QbtMud.Local", null)]);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.Null"] = JsonDocument.Parse("null").RootElement.Clone(),
                        ["QbtMud.String"] = JsonDocument.Parse("\"text\"").RootElement.Clone()
                    }));

            var result = await _target.GetEntriesAsync(TestContext.Current.CancellationToken);

            result.Single(entry => entry.DisplayKey == "Local").Preview.Should().BeEmpty();
            result.Single(entry => entry.DisplayKey == "Null").Value.Should().BeNull();
            result.Single(entry => entry.DisplayKey == "String").Value.Should().Be("text");
        }
    }
}
