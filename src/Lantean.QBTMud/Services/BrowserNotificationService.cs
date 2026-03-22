using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IBrowserNotificationService"/>.
    /// </summary>
    public sealed class BrowserNotificationService : IBrowserNotificationService, IAsyncDisposable
    {
        private static readonly TimeSpan _notificationInteropTimeout = TimeSpan.FromMilliseconds(250);

        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly IJSRuntime _jsRuntime;
        private DotNetObjectReference<BrowserNotificationService>? _dotNetObjectReference;
        private long _notificationPermissionSubscriptionId;
        private BrowserNotificationPermission _cachedPermission = BrowserNotificationPermission.Unknown;
        private bool _hasCachedPermission;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserNotificationService"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime.</param>
        public BrowserNotificationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public event EventHandler<BrowserNotificationPermissionChangedEventArgs>? PermissionChanged;

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
        public async Task<BrowserNotificationPermission> GetPermissionAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);
            return _cachedPermission;
        }

        /// <inheritdoc />
        public Task<BrowserNotificationPermission> RequestPermissionAsync(CancellationToken cancellationToken = default)
        {
            return RequestPermissionCoreAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<long> SubscribePermissionChangesAsync(object dotNetObjectReference, CancellationToken cancellationToken = default)
        {
            return SubscribePermissionChangesCoreAsync(dotNetObjectReference, cancellationToken);
        }

        /// <inheritdoc />
        public Task UnsubscribePermissionChangesAsync(long subscriptionId, CancellationToken cancellationToken = default)
        {
            return UnsubscribePermissionChangesCoreAsync(subscriptionId, cancellationToken);
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
            await EnsureInitializedAsync(cancellationToken);

            // Notification permission can change outside qbtmud, so the request path must
            // always re-read the live browser state before deciding whether to request.
            var getPermissionResult = await TryGetPermissionCoreAsync(cancellationToken);
            if (!getPermissionResult.IsAuthoritative)
            {
                return BrowserNotificationPermission.Unknown;
            }

            var currentPermission = UpdateCachedPermission(getPermissionResult.Permission);
            if (currentPermission != BrowserNotificationPermission.Default)
            {
                return currentPermission;
            }

            try
            {
                var task = _jsRuntime.InvokeAsync<string?>("qbt.requestNotificationPermission", cancellationToken).AsTask();
                var permission = await task.WaitAsync(_notificationInteropTimeout, cancellationToken);
                return UpdateCachedPermission(ParseNotificationPermission(permission));
            }
            catch (TimeoutException)
            {
                return BrowserNotificationPermission.Default;
            }
            catch (JSException)
            {
                return BrowserNotificationPermission.Unknown;
            }
            catch (InvalidOperationException)
            {
                return BrowserNotificationPermission.Unknown;
            }
            catch (HttpRequestException)
            {
                return BrowserNotificationPermission.Unknown;
            }
        }

        private static BrowserNotificationPermission ParseNotificationPermission(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return BrowserNotificationPermission.Default;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "unknown" => BrowserNotificationPermission.Unknown,
                "granted" => BrowserNotificationPermission.Granted,
                "denied" => BrowserNotificationPermission.Denied,
                "default" => BrowserNotificationPermission.Default,
                "unsupported" => BrowserNotificationPermission.Unsupported,
                "insecure" => BrowserNotificationPermission.Insecure,
                _ => BrowserNotificationPermission.Default
            };
        }

        /// <summary>
        /// Updates cached browser notification permission after a JavaScript notification callback.
        /// </summary>
        /// <returns>A task representing the asynchronous callback handling.</returns>
        [JSInvokable]
        public async Task OnNotificationPermissionChanged()
        {
            var permissionResult = await TryGetPermissionCoreAsync();
            if (!permissionResult.IsAuthoritative)
            {
                return;
            }

            UpdateCachedPermission(permissionResult.Permission);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await UnsubscribePermissionChangesCoreAsync(_notificationPermissionSubscriptionId);
            _dotNetObjectReference?.Dispose();
            _dotNetObjectReference = null;
            _notificationPermissionSubscriptionId = 0;
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_hasCachedPermission && _notificationPermissionSubscriptionId > 0)
            {
                return;
            }

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_hasCachedPermission)
                {
                    var permissionResult = await TryGetPermissionCoreAsync(cancellationToken);
                    _cachedPermission = permissionResult.IsAuthoritative
                        ? permissionResult.Permission
                        : BrowserNotificationPermission.Unknown;
                    _hasCachedPermission = true;
                }

                if (_notificationPermissionSubscriptionId > 0)
                {
                    return;
                }

                _dotNetObjectReference ??= DotNetObjectReference.Create(this);

                for (var attempt = 0; attempt < 3 && _notificationPermissionSubscriptionId <= 0; attempt++)
                {
                    _notificationPermissionSubscriptionId = await SubscribePermissionChangesCoreAsync(_dotNetObjectReference, cancellationToken);
                    if (_notificationPermissionSubscriptionId > 0)
                    {
                        break;
                    }

                    await Task.Yield();
                }
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private async Task<GetPermissionResult> TryGetPermissionCoreAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var task = _jsRuntime.InvokeAsync<string?>("qbt.getNotificationPermission", cancellationToken).AsTask();
                var permission = await task.WaitAsync(_notificationInteropTimeout, cancellationToken);
                return new GetPermissionResult(ParseNotificationPermission(permission), true);
            }
            catch (TimeoutException)
            {
                return new GetPermissionResult(BrowserNotificationPermission.Unknown, false);
            }
            catch (JSException)
            {
                return new GetPermissionResult(BrowserNotificationPermission.Unknown, false);
            }
            catch (InvalidOperationException)
            {
                return new GetPermissionResult(BrowserNotificationPermission.Unknown, false);
            }
            catch (HttpRequestException)
            {
                return new GetPermissionResult(BrowserNotificationPermission.Unknown, false);
            }
        }

        private async Task<long> SubscribePermissionChangesCoreAsync(object dotNetObjectReference, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dotNetObjectReference);

            return await InvokeWithFallbackAsync(
                () => _jsRuntime.InvokeAsync<long>("qbt.subscribeNotificationPermission", cancellationToken, dotNetObjectReference).AsTask(),
                fallback: 0L,
                cancellationToken,
                _notificationInteropTimeout);
        }

        private async Task UnsubscribePermissionChangesCoreAsync(long subscriptionId, CancellationToken cancellationToken = default)
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

        private BrowserNotificationPermission UpdateCachedPermission(BrowserNotificationPermission permission)
        {
            var changed = !_hasCachedPermission || _cachedPermission != permission;
            _cachedPermission = permission;
            _hasCachedPermission = true;

            if (changed)
            {
                PermissionChanged?.Invoke(this, new BrowserNotificationPermissionChangedEventArgs(permission));
            }

            return _cachedPermission;
        }

        private readonly record struct GetPermissionResult(BrowserNotificationPermission Permission, bool IsAuthoritative);
    }
}
