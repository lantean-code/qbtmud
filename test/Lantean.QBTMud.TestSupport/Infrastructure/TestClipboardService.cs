using System.Collections.Concurrent;
using Lantean.QBTMud.Application.Services;

namespace Lantean.QBTMud.TestSupport.Infrastructure
{
    internal sealed class TestClipboardService : IClipboardService
    {
        private readonly ConcurrentQueue<string> _writes = new();

        public Task WriteToClipboard(string text)
        {
            _writes.Enqueue(text);
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<string> Entries => _writes.ToArray();

        public string? PeekLast()
        {
            return _writes.LastOrDefault();
        }

        public void Clear()
        {
            while (_writes.TryDequeue(out _))
            {
            }
        }
    }
}
