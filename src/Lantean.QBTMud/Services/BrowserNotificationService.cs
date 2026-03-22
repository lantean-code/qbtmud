using Lantean.QBTMud.Interop;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IBrowserNotificationService"/>.
    /// </summary>
    public sealed class BrowserNotificationService : IBrowserNotificationService
    {
        private static readonly TimeSpan _notificationInteropTimeout = TimeSpan.FromMilliseconds(250);

        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserNotificationService"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public BrowserNotificationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
        {
            return InvokeWithFallbackAsync(
                () => _jsRuntime.InvokeAsync<bool>("qbt.isNotificationSupported", cancellationToken).AsTask(),
                fallback: false,
                cancellationToken,
                _notificationInteropTimeout);
        }

        /// <inheritdoc />
        public Task<BrowserNotificationPermission> GetPermissionAsync(CancellationToken cancellationToken = default)
        {
            return InvokeWithFallbackAsync(
                async () => ParseNotificationPermission(await _jsRuntime.InvokeAsync<string?>("qbt.getNotificationPermission", cancellationToken)),
                BrowserNotificationPermission.Unsupported,
                cancellationToken,
                _notificationInteropTimeout);
        }

        /// <inheritdoc />
        public Task<BrowserNotificationPermission> RequestPermissionAsync(CancellationToken cancellationToken = default)
        {
            return RequestPermissionCoreAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ShowNotificationAsync(string title, string body, CancellationToken cancellationToken = default)
        {
            await InvokeWithFallbackAsync(
                async () =>
                {
                    await _jsRuntime.InvokeAsync<IJSVoidResult>("qbt.showNotification", cancellationToken, title, body);
                    return true;
                },
                fallback: false,
                cancellationToken,
                _notificationInteropTimeout);
        }

        /// <inheritdoc />
        public Task<long> SubscribePermissionChangesAsync(object dotNetObjectReference, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dotNetObjectReference);

            return InvokeWithFallbackAsync(
                () => _jsRuntime.InvokeAsync<long>("qbt.subscribeNotificationPermission", cancellationToken, dotNetObjectReference).AsTask(),
                fallback: 0L,
                cancellationToken,
                _notificationInteropTimeout);
        }

        /// <inheritdoc />
        public async Task UnsubscribePermissionChangesAsync(long subscriptionId, CancellationToken cancellationToken = default)
        {
            if (subscriptionId <= 0)
            {
                return;
            }

            await InvokeWithFallbackAsync(
                () => _jsRuntime.InvokeAsync<object?>("qbt.unsubscribeNotificationPermission", cancellationToken, subscriptionId).AsTask(),
                fallback: (object?)null,
                cancellationToken,
                _notificationInteropTimeout);
        }

        private static async Task<T> InvokeWithFallbackAsync<T>(Func<Task<T>> callback, T fallback, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            try
            {
                var task = callback();

                if (timeout is null)
                {
                    return await task.WaitAsync(cancellationToken);
                }

                return await task.WaitAsync(timeout.Value, cancellationToken);
            }
            catch (TimeoutException)
            {
                return fallback;
            }
            catch (JSException)
            {
                return fallback;
            }
            catch (InvalidOperationException)
            {
                return fallback;
            }
            catch (HttpRequestException)
            {
                return fallback;
            }
        }

        private async Task<BrowserNotificationPermission> RequestPermissionCoreAsync(CancellationToken cancellationToken)
        {
            var currentPermission = await GetPermissionAsync(cancellationToken);
            if (currentPermission != BrowserNotificationPermission.Default)
            {
                return currentPermission;
            }

            return await InvokeWithFallbackAsync(
                async () => ParseNotificationPermission(await _jsRuntime.InvokeAsync<string?>("qbt.requestNotificationPermission", cancellationToken)),
                BrowserNotificationPermission.Unsupported,
                cancellationToken);
        }

        private static BrowserNotificationPermission ParseNotificationPermission(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return BrowserNotificationPermission.Default;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "granted" => BrowserNotificationPermission.Granted,
                "denied" => BrowserNotificationPermission.Denied,
                "default" => BrowserNotificationPermission.Default,
                "unsupported" => BrowserNotificationPermission.Unsupported,
                "insecure" => BrowserNotificationPermission.Insecure,
                _ => BrowserNotificationPermission.Default
            };
        }
    }
}
