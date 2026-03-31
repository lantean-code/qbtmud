namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IConnectivityStateService"/>.
    /// </summary>
    public sealed class ConnectivityStateService : IConnectivityStateService
    {
        private bool _isLostConnection;

        /// <inheritdoc />
        public bool IsLostConnection
        {
            get { return _isLostConnection; }
        }

        /// <inheritdoc />
        public event Action<bool>? ConnectivityChanged;

        /// <inheritdoc />
        public void MarkLostConnection()
        {
            SetState(isLostConnection: true);
        }

        /// <inheritdoc />
        public void MarkConnected()
        {
            SetState(isLostConnection: false);
        }

        private void SetState(bool isLostConnection)
        {
            if (_isLostConnection == isLostConnection)
            {
                return;
            }

            _isLostConnection = isLostConnection;
            ConnectivityChanged?.Invoke(_isLostConnection);
        }
    }
}
