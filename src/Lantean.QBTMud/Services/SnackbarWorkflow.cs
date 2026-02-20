using Lantean.QBTMud.Services.Localization;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="ISnackbarWorkflow"/>.
    /// </summary>
    public sealed class SnackbarWorkflow : ISnackbarWorkflow
    {
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly ISnackbar _snackbar;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnackbarWorkflow"/> class.
        /// </summary>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="snackbar">The snackbar service.</param>
        public SnackbarWorkflow(ILanguageLocalizer languageLocalizer, ISnackbar snackbar)
        {
            _languageLocalizer = languageLocalizer;
            _snackbar = snackbar;
        }

        /// <inheritdoc />
        public Snackbar? ShowLocalizedMessage(string context, string source, Severity severity = Severity.Normal, params object[] arguments)
        {
            return ShowLocalizedMessage(context, source, severity, null, null, arguments);
        }

        /// <inheritdoc />
        public Snackbar? ShowLocalizedMessage(string context, string source, Severity severity, Action<SnackbarOptions>? configure, string? key, params object[] arguments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(source);

            var message = _languageLocalizer.Translate(context, source, arguments);
            return ShowMessage(message, severity, configure, key);
        }

        /// <inheritdoc />
        public Snackbar? ShowMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            return _snackbar.Add(message, severity, configure, key);
        }
    }
}
