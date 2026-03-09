using Lantean.QBTMud.Interop;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IPwaInstallPromptService"/>.
    /// </summary>
    public sealed class PwaInstallPromptService : IPwaInstallPromptService
    {
        private readonly IJSRuntime _jSRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="PwaInstallPromptService"/> class.
        /// </summary>
        /// <param name="jSRuntime">The JavaScript runtime.</param>
        public PwaInstallPromptService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        /// <inheritdoc />
        public async Task<PwaInstallPromptState> GetInstallPromptStateAsync(CancellationToken cancellationToken = default)
        {
            var state = await _jSRuntime.InvokeAsync<PwaInstallPromptState>("qbt.getInstallPromptState", cancellationToken, Array.Empty<object?>());
            return state ?? new PwaInstallPromptState();
        }

        /// <inheritdoc />
        public Task<long> SubscribeInstallPromptStateAsync(object dotNetObjectReference, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dotNetObjectReference);
            return _jSRuntime.InvokeAsync<long>("qbt.subscribeInstallPromptState", cancellationToken, dotNetObjectReference).AsTask();
        }

        /// <inheritdoc />
        public Task UnsubscribeInstallPromptStateAsync(long subscriptionId, CancellationToken cancellationToken = default)
        {
            if (subscriptionId <= 0)
            {
                return Task.CompletedTask;
            }

            return _jSRuntime.InvokeAsync<object?>("qbt.unsubscribeInstallPromptState", cancellationToken, subscriptionId).AsTask();
        }

        /// <inheritdoc />
        public async Task<string> RequestInstallPromptAsync(CancellationToken cancellationToken = default)
        {
            var outcome = await _jSRuntime.InvokeAsync<string>("qbt.requestInstallPrompt", cancellationToken, Array.Empty<object?>());
            return outcome ?? "unknown";
        }
    }
}
