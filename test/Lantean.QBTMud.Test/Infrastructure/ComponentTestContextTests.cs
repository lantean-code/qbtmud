using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public class ComponentTestContextTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public ComponentTestContextTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public async Task GIVEN_LocalStorageStub_WHEN_RoundTrippingValues_THEN_ShouldReturnStoredValue()
        {
            await _target.LocalStorage.SetItemAsync("Number", 42);

            var value = await _target.LocalStorage.GetItemAsync<int>("Number");
            value.Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_ClipboardStub_WHEN_WritingEntries_THEN_ShouldRecordWritesInOrder()
        {
            await _target.Clipboard.WriteToClipboard("hello");
            await _target.Clipboard.WriteToClipboard("world");

            _target.Clipboard.Entries.Should().ContainInOrder("hello", "world");
            _target.Clipboard.PeekLast().Should().Be("world");
        }

        [Fact]
        public void GIVEN_SnackbarMock_WHEN_ReplacingRegisteredService_THEN_ShouldResolveMock()
        {
            var mock = _target.UseSnackbarMock(MockBehavior.Loose);
            var resolved = _target.Services.GetRequiredService<ISnackbar>();

            ReferenceEquals(resolved, mock.Object).Should().BeTrue();
        }

        public void Dispose()
        {
            _target.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
