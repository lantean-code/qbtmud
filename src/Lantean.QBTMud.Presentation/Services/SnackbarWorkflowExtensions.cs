using MudBlazor;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides semantic helpers for snackbar presentation behaviors.
    /// </summary>
    public static class SnackbarWorkflowExtensions
    {
        /// <summary>
        /// Shows a localized transient snackbar message.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="context">The translation context.</param>
        /// <param name="source">The translation source string.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="arguments">Optional translation arguments.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowTransient(this ISnackbarWorkflow workflow, string context, string source, Severity severity = Severity.Normal, params object[] arguments)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            return workflow.ShowLocalizedMessage(context, source, severity, arguments);
        }

        /// <summary>
        /// Shows a transient pre-formatted snackbar message.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowTransientMessage(this ISnackbarWorkflow workflow, string message, Severity severity = Severity.Normal)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            return workflow.ShowMessage(message, severity);
        }

        /// <summary>
        /// Shows a localized dismissable snackbar message.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="context">The translation context.</param>
        /// <param name="source">The translation source string.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="arguments">Optional translation arguments.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowDismissable(this ISnackbarWorkflow workflow, string context, string source, Severity severity = Severity.Normal, params object[] arguments)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            return workflow.ShowLocalizedMessage(
                context,
                source,
                severity,
                options =>
                {
                    options.RequireInteraction = true;
                },
                null,
                arguments);
        }

        /// <summary>
        /// Shows a dismissable pre-formatted snackbar message.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowDismissableMessage(this ISnackbarWorkflow workflow, string message, Severity severity = Severity.Normal)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            return workflow.ShowMessage(
                message,
                severity,
                options =>
                {
                    options.RequireInteraction = true;
                });
        }

        /// <summary>
        /// Shows a dismissable pre-formatted snackbar message with an action button.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The snackbar severity.</param>
        /// <param name="actionLabel">The action button label.</param>
        /// <param name="onClick">The action callback.</param>
        /// <param name="key">An optional snackbar key used to deduplicate messages.</param>
        /// <param name="configure">An optional snackbar options configurator.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowActionMessage(
            this ISnackbarWorkflow workflow,
            string message,
            Severity severity,
            string actionLabel,
            Func<Snackbar, Task> onClick,
            string? key = null,
            Action<SnackbarOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(workflow);
            ArgumentException.ThrowIfNullOrWhiteSpace(actionLabel);
            ArgumentNullException.ThrowIfNull(onClick);

            return workflow.ShowMessage(message, severity, CombineOptions(
                options =>
                {
                    options.RequireInteraction = true;
                    options.Action = actionLabel;
                    options.OnClick = onClick;
                },
                configure), key);
        }

        /// <summary>
        /// Shows a localized error snackbar using <paramref name="exception"/> message as argument.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="context">The translation context.</param>
        /// <param name="source">The translation source string that supports a single argument.</param>
        /// <param name="exception">The error to display.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowError(this ISnackbarWorkflow workflow, string context, string source, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(workflow);
            ArgumentNullException.ThrowIfNull(exception);

            return workflow.ShowLocalizedMessage(context, source, Severity.Error, exception.Message);
        }

        /// <summary>
        /// Shows a transient pre-formatted error snackbar.
        /// </summary>
        /// <param name="workflow">The snackbar workflow.</param>
        /// <param name="message">The message to display.</param>
        /// <returns>The created snackbar instance, if any.</returns>
        public static Snackbar? ShowErrorMessage(this ISnackbarWorkflow workflow, string message)
        {
            ArgumentNullException.ThrowIfNull(workflow);

            return workflow.ShowTransientMessage(message, Severity.Error);
        }

        private static Action<SnackbarOptions> CombineOptions(Action<SnackbarOptions> primary, Action<SnackbarOptions>? secondary)
        {
            return options =>
            {
                primary(options);
                secondary?.Invoke(options);
            };
        }
    }
}
