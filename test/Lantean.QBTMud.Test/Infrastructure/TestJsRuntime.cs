using Microsoft.JSInterop;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class TestJsRuntime : IJSRuntime
    {
        private readonly Queue<object?> _results = new();

        public int CallCount { get; private set; }

        public string? LastIdentifier { get; private set; }

        public object?[]? LastArguments { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public void EnqueueResult(object? result)
        {
            _results.Enqueue(result);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            LastIdentifier = identifier;
            LastArguments = args;
            LastCancellationToken = cancellationToken;
            CallCount++;

            if (_results.Count == 0)
            {
                return new ValueTask<TValue>(default(TValue)!);
            }

            var result = _results.Dequeue();
            if (result is null)
            {
                return new ValueTask<TValue>(default(TValue)!);
            }

            return new ValueTask<TValue>((TValue)result);
        }
    }
}
