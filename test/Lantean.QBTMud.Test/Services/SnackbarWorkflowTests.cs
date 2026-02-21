using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class SnackbarWorkflowTests
    {
        private readonly ISnackbar _snackbar;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly SnackbarWorkflow _target;

        public SnackbarWorkflowTests()
        {
            _snackbar = Mock.Of<ISnackbar>();
            _languageLocalizer = Mock.Of<ILanguageLocalizer>();

            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => Format(source, arguments));

            _target = new SnackbarWorkflow(_languageLocalizer, _snackbar);
        }

        [Fact]
        public void GIVEN_LocalizedSource_WHEN_ShowInvoked_THEN_TranslatesAndShowsSnackbar()
        {
            _target.ShowLocalizedMessage("AppContext", "Message %1", Severity.Warning, "Value");

            Mock.Get(_languageLocalizer).Verify(
                localizer => localizer.Translate("AppContext", "Message %1", It.Is<object[]>(args => args.Length == 1 && Equals(args[0], "Value"))),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Message Value", Severity.Warning, null, null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_ShowMessageOptions_WHEN_ShowMessageInvoked_THEN_ForwardsToSnackbar()
        {
            Action<SnackbarOptions> configure = options =>
            {
                options.RequireInteraction = true;
            };

            _target.ShowMessage("Message", Severity.Info, configure, "message-key");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Message", Severity.Info, configure, "message-key"),
                Times.Once);
        }

        [Fact]
        public void GIVEN_TransientExtension_WHEN_ShowTransientMessageInvoked_THEN_UsesNonInteractiveOptions()
        {
            _target.ShowTransientMessage("Message", Severity.Success);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "Message",
                    Severity.Success,
                    null,
                    null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_TransientLocalizedExtension_WHEN_ShowTransientInvoked_THEN_TranslatesAndShowsSnackbar()
        {
            _target.ShowTransient("AppContext", "Value: %1", Severity.Info, "A");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Value: A", Severity.Info, null, null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_DismissableExtension_WHEN_ShowDismissableMessageInvoked_THEN_UsesInteractiveOptions()
        {
            _target.ShowDismissableMessage("Message", Severity.Warning);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "Message",
                    Severity.Warning,
                    It.Is<Action<SnackbarOptions>>(configure => ConfigureRequireInteraction(configure, true)),
                    null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_DismissableLocalizedExtension_WHEN_ShowDismissableInvoked_THEN_UsesInteractiveOptions()
        {
            _target.ShowDismissable("AppContext", "Dismiss %1", Severity.Warning, "Now");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "Dismiss Now",
                    Severity.Warning,
                    It.Is<Action<SnackbarOptions>>(configure => ConfigureRequireInteraction(configure, true)),
                    null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_ActionExtension_WHEN_ShowActionMessageInvoked_THEN_ConfiguresActionAndInteraction()
        {
            _target.ShowActionMessage(
                "Message",
                Severity.Info,
                "Dismiss",
                _ => Task.CompletedTask,
                "action-key",
                options => options.CloseAfterNavigation = true);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    "Message",
                    Severity.Info,
                    It.Is<Action<SnackbarOptions>>(configure => ConfigureAction(configure)),
                    "action-key"),
                Times.Once);
        }

        [Fact]
        public void GIVEN_Exception_WHEN_ShowErrorInvoked_THEN_UsesErrorSeverityAndExceptionMessage()
        {
            _target.ShowError("AppContext", "Failure: %1", new InvalidOperationException("boom"));

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Failure: boom", Severity.Error, null, null),
                Times.Once);
        }

        [Fact]
        public void GIVEN_ErrorMessageExtension_WHEN_ShowErrorMessageInvoked_THEN_UsesErrorSeverity()
        {
            _target.ShowErrorMessage("ErrorMessage");

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("ErrorMessage", Severity.Error, null, null),
                Times.Once);
        }

        private static bool ConfigureRequireInteraction(Action<SnackbarOptions> configure, bool expected)
        {
            var options = new SnackbarOptions(Severity.Normal, Mock.Of<CommonSnackbarOptions>());
            configure(options);
            return options.RequireInteraction == expected;
        }

        private static bool ConfigureAction(Action<SnackbarOptions> configure)
        {
            var options = new SnackbarOptions(Severity.Normal, Mock.Of<CommonSnackbarOptions>());
            configure(options);

            options.RequireInteraction.Should().BeTrue();
            options.Action.Should().Be("Dismiss");
            options.OnClick.Should().NotBeNull();
            options.CloseAfterNavigation.Should().BeTrue();

            return true;
        }

        private static string Format(string source, object[] arguments)
        {
            var result = source;
            for (var i = 0; i < arguments.Length; i++)
            {
                var value = arguments[i]?.ToString() ?? string.Empty;
                result = result.Replace($"%{i + 1}", value, StringComparison.Ordinal);
            }

            return result;
        }
    }
}
