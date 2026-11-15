using System.Text.Json;
using AwesomeAssertions;
using Microsoft.JSInterop;
using Moq;

namespace Lantean.QBTMud.Infrastructure.Test.Services
{
    public sealed class PendingClientDataJournalTests
    {
        private const string _journalKey = "SettingsStorage.PendingClientData.v1";
        private readonly IJSRuntime _jsRuntime;
        private readonly PendingClientDataJournal _target;

        public PendingClientDataJournalTests()
        {
            _jsRuntime = Mock.Of<IJSRuntime>();
            _target = new PendingClientDataJournal(_jsRuntime);
        }

        [Fact]
        public async Task GIVEN_PendingChanges_WHEN_Reading_THEN_ShouldReturnBrowserSnapshot()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var snapshot = new Dictionary<string, JsonElement?> { ["Key"] = JsonSerializer.SerializeToElement("Value") };
            Mock.Get(_jsRuntime).Setup(mock => mock.InvokeAsync<Dictionary<string, JsonElement?>>("qbt.updatePendingClientDataJournal", cancellationToken,
                    It.Is<object?[]>(arguments => arguments.SequenceEqual(new object?[] { _journalKey, "read", null }))))
                .Returns(new ValueTask<Dictionary<string, JsonElement?>>(snapshot));

            var result = await _target.ReadAsync(cancellationToken);

            result.Should().BeSameAs(snapshot);
            Mock.Get(_jsRuntime).VerifyAll();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_ValueOrDeletion_WHEN_Setting_THEN_ShouldSubmitOnlyThatKey(bool deletion)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var value = deletion ? (JsonElement?)null : JsonSerializer.SerializeToElement("Value");
            Mock.Get(_jsRuntime).Setup(mock => mock.InvokeAsync<Dictionary<string, JsonElement?>>("qbt.updatePendingClientDataJournal", cancellationToken,
                    It.Is<object?[]>(arguments => IsSetPayload(arguments, value))))
                .Returns(new ValueTask<Dictionary<string, JsonElement?>>([]));

            await _target.SetAsync("Key", value, cancellationToken);

            Mock.Get(_jsRuntime).VerifyAll();
        }

        [Fact]
        public async Task GIVEN_ReplayedSnapshot_WHEN_Completing_THEN_ShouldReturnRemainingBrowserChanges()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var snapshot = new Dictionary<string, JsonElement?> { ["Key"] = null };
            var remaining = new Dictionary<string, JsonElement?> { ["OtherKey"] = JsonSerializer.SerializeToElement("Value") };
            Mock.Get(_jsRuntime).Setup(mock => mock.InvokeAsync<Dictionary<string, JsonElement?>>("qbt.updatePendingClientDataJournal", cancellationToken,
                    It.Is<object?[]>(arguments => arguments.SequenceEqual(new object?[] { _journalKey, "complete", snapshot }))))
                .Returns(new ValueTask<Dictionary<string, JsonElement?>>(remaining));

            var result = await _target.CompleteAsync(snapshot, cancellationToken);

            result.Should().BeSameAs(remaining);
            Mock.Get(_jsRuntime).VerifyAll();
        }

        [Theory]
        [InlineData("remove", "Key")]
        [InlineData("clear", null)]
        [InlineData("removeLocalSource", "Key")]
        public async Task GIVEN_CleanupRequest_WHEN_Mutating_THEN_ShouldForwardOperationAndCancellation(string action, string? key)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            Mock.Get(_jsRuntime).Setup(mock => mock.InvokeAsync<Dictionary<string, JsonElement?>>("qbt.updatePendingClientDataJournal", cancellationToken,
                    It.Is<object?[]>(arguments => arguments.SequenceEqual(new object?[] { _journalKey, action, key }))))
                .Returns(new ValueTask<Dictionary<string, JsonElement?>>([]));

            switch (action)
            {
                case "remove":
                    await _target.RemoveAsync(key!, cancellationToken);
                    break;
                case "clear":
                    await _target.ClearAsync(cancellationToken);
                    break;
                case "removeLocalSource":
                    await _target.RemoveLocalSourceAsync(key!, cancellationToken);
                    break;
            }

            Mock.Get(_jsRuntime).VerifyAll();
        }

        [Fact]
        public async Task GIVEN_BrowserFailure_WHEN_UpdatingJournal_THEN_ShouldPropagateFailure()
        {
            var exception = new JSException("Storage unavailable.");
            Mock.Get(_jsRuntime).Setup(mock => mock.InvokeAsync<Dictionary<string, JsonElement?>>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object?[]>()))
                .Throws(exception);

            var action = async () => await _target.ClearAsync(TestContext.Current.CancellationToken);

            (await action.Should().ThrowAsync<JSException>()).Which.Should().BeSameAs(exception);
        }

        private static bool IsSetPayload(object?[] arguments, JsonElement? expected)
        {
            return arguments.Length == 3 && Equals(arguments[0], _journalKey) && Equals(arguments[1], "set")
                && arguments[2] is Dictionary<string, JsonElement?> changes && changes.Count == 1
                && changes.TryGetValue("Key", out var actual) && actual?.GetRawText() == expected?.GetRawText();
        }
    }
}
