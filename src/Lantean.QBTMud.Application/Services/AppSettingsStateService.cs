using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAppSettingsStateService"/>.
    /// </summary>
    public sealed class AppSettingsStateService : IAppSettingsStateService
    {
        private AppSettings? _current;

        /// <inheritdoc />
        public event EventHandler<AppSettingsChangedEventArgs>? Changed;

        /// <inheritdoc />
        public AppSettings? Current => _current?.Clone();

        /// <inheritdoc />
        public bool SetSettings(AppSettings? settings)
        {
            var normalizedSettings = settings?.Clone();
            if (AppSettings.AreEquivalent(_current, normalizedSettings))
            {
                return false;
            }

            var previousSettings = _current?.Clone();
            _current = normalizedSettings;
            Changed?.Invoke(this, new AppSettingsChangedEventArgs(previousSettings, _current?.Clone()));
            return true;
        }
    }
}
