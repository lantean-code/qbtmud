using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public class ComponentTestContextTests
    {
        [Fact]
        public async Task LocalStorageStub_RoundTripsValues()
        {
            using var context = new ComponentTestContext();

            await context.LocalStorage.SetItemAsync("Number", 42);

            var value = await context.LocalStorage.GetItemAsync<int>("Number");
            value.Should().Be(42);
        }

        [Fact]
        public async Task ClipboardStub_RecordsWrites()
        {
            using var context = new ComponentTestContext();

            await context.Clipboard.WriteToClipboard("hello");
            await context.Clipboard.WriteToClipboard("world");

            context.Clipboard.Entries.Should().ContainInOrder("hello", "world");
            context.Clipboard.PeekLast().Should().Be("world");
        }

        [Fact]
        public void SnackbarMock_ReplacesRegisteredService()
        {
            using var context = new ComponentTestContext();

            var mock = context.UseSnackbarMock(MockBehavior.Loose);
            var resolved = context.Services.GetRequiredService<ISnackbar>();

            ReferenceEquals(resolved, mock.Object).Should().BeTrue();
        }
    }
}
