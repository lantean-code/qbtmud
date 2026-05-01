using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides a centralized workflow for showing snackbar messages.
    /// </summary>
    public interface ISnackbarWorkflow
    {
        /// <summary>
        /// Shows a localized snackbar message.
        /// </summary>
        /// <param name="context">The translation context.</param>
        /// <param name="source">The translation source string.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="arguments">Optional translation arguments.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        Snackbar? ShowLocalizedMessage(string context, string source, Severity severity = Severity.Normal, params object[] arguments);

        /// <summary>
        /// Shows a localized snackbar message with custom snackbar options.
        /// </summary>
        /// <param name="context">The translation context.</param>
        /// <param name="source">The translation source string.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="configure">An optional snackbar options configurator.</param>
        /// <param name="key">An optional snackbar key used to deduplicate messages.</param>
        /// <param name="arguments">Optional translation arguments.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        Snackbar? ShowLocalizedMessage(string context, string source, Severity severity, Action<SnackbarOptions>? configure, string? key, params object[] arguments);

        /// <summary>
        /// Shows a pre-formatted snackbar message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="configure">An optional snackbar options configurator.</param>
        /// <param name="key">An optional snackbar key used to deduplicate messages.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        Snackbar? ShowMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null);

        /// <summary>
        /// Shows a snackbar rendered from a component.
        /// </summary>
        /// <typeparam name="TComponent">The component type rendered in the snackbar body.</typeparam>
        /// <param name="componentParameters">Optional component parameters.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="configure">An optional snackbar options configurator.</param>
        /// <param name="key">An optional snackbar key used to identify and replace an existing snackbar.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        Snackbar? ShowComponent<TComponent>(Dictionary<string, object>? componentParameters = null, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null)
            where TComponent : IComponent;

        /// <summary>
        /// Hides a snackbar by key.
        /// </summary>
        /// <param name="key">The snackbar key.</param>
        void Hide(string key);
    }
}
