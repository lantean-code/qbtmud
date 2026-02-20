using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;
using System.Runtime.CompilerServices;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class WizardStepTests : RazorComponentTestBase<WizardStep>
    {
        private readonly ILogger<WizardStep> _logger;

        public WizardStepTests()
        {
            _logger = Mock.Of<ILogger<WizardStep>>();
            TestContext.AddSingleton(_logger);
        }

        [Fact]
        public void GIVEN_StepRendered_WHEN_Initialized_THEN_RendersIconTitleSubtitleAndChildContent()
        {
            var target = RenderStep(WizardAccentColor.FromPalette(Color.Info), null, Severity.Info);

            var icon = FindComponentByTestId<MudIcon>(target, "WizardStepIcon");
            var title = FindComponentByTestId<MudText>(target, "WizardStepTitle");
            var subtitle = FindComponentByTestId<MudText>(target, "WizardStepSubtitle");
            var childSelect = FindComponentByTestId<MudSelect<string>>(target, "WizardStepChildSelect");

            icon.Instance.Icon.Should().Be(Icons.Material.Filled.Language);
            GetChildContentText(title.Instance.ChildContent).Should().Be("Language");
            GetChildContentText(subtitle.Instance.ChildContent).Should().Be("Thanks for using qbtmud. Choose your language to get started.");
            HasTestId(childSelect, "WizardStepChildSelect").Should().BeTrue();
        }

        [Fact]
        public void GIVEN_AlertTextProvided_WHEN_Rendered_THEN_RendersAlertAndBodyWrappers()
        {
            var target = RenderStep(WizardAccentColor.FromPalette(Color.Info), "AlertText", Severity.Warning);

            var alert = FindComponentByTestId<MudAlert>(target, "WizardStepAlert");
            var mainWrappers = target.FindAll("[data-test-id='WizardStepBodyMain']");
            var alertWrappers = target.FindAll("[data-test-id='WizardStepBodyAlert']");
            var spacers = target.FindComponents<MudSpacer>();

            alert.Instance.Severity.Should().Be(Severity.Warning);
            GetChildContentText(alert.Instance.ChildContent).Should().Be("AlertText");
            mainWrappers.Should().HaveCount(1);
            alertWrappers.Should().HaveCount(1);
            spacers.Should().HaveCount(1);
        }

        [Fact]
        public void GIVEN_AlertTextMissing_WHEN_Rendered_THEN_HidesAlert()
        {
            var target = RenderStep(WizardAccentColor.FromPalette(Color.Info), null, Severity.Info);

            target.FindComponents<MudAlert>().Should().BeEmpty();
            target.FindAll("[data-test-id='WizardStepBodyAlert']").Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_AlertTextWhitespace_WHEN_Rendered_THEN_HidesAlert()
        {
            var target = RenderStep(WizardAccentColor.FromPalette(Color.Info), "   ", Severity.Info);

            target.FindComponents<MudAlert>().Should().BeEmpty();
            target.FindAll("[data-test-id='WizardStepBodyAlert']").Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_PaletteAccent_WHEN_Rendered_THEN_UsesMappedPaletteCssColorForRootAndAvatar()
        {
            var target = RenderStep(WizardAccentColor.FromPalette(Color.Info), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");
            var avatar = FindComponentByTestId<MudAvatar>(target, "WizardStepAvatar");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-info);");
            avatar.Instance.Style.Should().Contain("background-color: var(--wizard-step-accent);");
        }

        [Fact]
        public void GIVEN_CssAccent_WHEN_Rendered_THEN_UsesProvidedCssColorForRootAndAvatar()
        {
            var target = RenderStep(WizardAccentColor.FromCss("#112233"), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");
            var avatar = FindComponentByTestId<MudAvatar>(target, "WizardStepAvatar");

            root.Instance.Style.Should().Contain("--wizard-step-accent: #112233;");
            avatar.Instance.Style.Should().Contain("background-color: var(--wizard-step-accent);");
        }

        [Fact]
        public void GIVEN_InvalidCssAccent_WHEN_Rendered_THEN_FallsBackToPrimaryAndLogsWarning()
        {
            var target = RenderStep(WizardAccentColor.FromCss("bad;color"), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");

            Mock.Get(_logger).Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() != null && state.ToString()!.Contains("invalid", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(Color.Primary, "var(--mud-palette-primary)")]
        [InlineData(Color.Secondary, "var(--mud-palette-secondary)")]
        [InlineData(Color.Tertiary, "var(--mud-palette-tertiary)")]
        [InlineData(Color.Info, "var(--mud-palette-info)")]
        [InlineData(Color.Success, "var(--mud-palette-success)")]
        [InlineData(Color.Warning, "var(--mud-palette-warning)")]
        [InlineData(Color.Error, "var(--mud-palette-error)")]
        [InlineData(Color.Dark, "var(--mud-palette-dark)")]
        [InlineData(Color.Surface, "var(--mud-palette-surface)")]
        [InlineData(Color.Transparent, "transparent")]
        [InlineData(Color.Inherit, "inherit")]
        [InlineData(Color.Default, "var(--mud-palette-primary)")]
        public void GIVEN_PaletteAccentVariant_WHEN_Rendered_THEN_UsesMappedCssValue(Color accentColor, string expectedCssValue)
        {
            var target = RenderStep(WizardAccentColor.FromPalette(accentColor), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain(FormattableString.Invariant($"--wizard-step-accent: {expectedCssValue};"));
        }

        [Fact]
        public void GIVEN_NullAccent_WHEN_Rendered_THEN_FallsBackToPrimaryAndLogsWarning()
        {
            var target = RenderStep(null!, "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");

            Mock.Get(_logger).Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() != null && state.ToString()!.Contains("missing", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_PaletteAccentMissingPaletteValue_WHEN_Rendered_THEN_FallsBackToPrimaryAndLogsWarning()
        {
            var invalidAccent = CreateWizardAccentColor(WizardAccentColorKind.Palette, null, null);
            var target = RenderStep(invalidAccent, "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");

            Mock.Get(_logger).Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() != null && state.ToString()!.Contains("requires a palette color", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_UnsupportedAccentKind_WHEN_Rendered_THEN_FallsBackToPrimaryAndLogsWarning()
        {
            var invalidAccent = CreateWizardAccentColor((WizardAccentColorKind)999, Color.Info, "#112233");
            var target = RenderStep(invalidAccent, "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");

            Mock.Get(_logger).Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString() != null && state.ToString()!.Contains("not supported", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("var(--mud-palette-info)")]
        [InlineData("rgb(17, 34, 51)")]
        [InlineData("rebeccapurple")]
        public void GIVEN_ValidCssAccentVariant_WHEN_Rendered_THEN_UsesProvidedCssColor(string cssColor)
        {
            var target = RenderStep(WizardAccentColor.FromCss(cssColor), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain(FormattableString.Invariant($"--wizard-step-accent: {cssColor};"));
        }

        [Fact]
        public void GIVEN_CssAccentWithWhitespaceValue_WHEN_Rendered_THEN_FallsBackToPrimary()
        {
            var invalidAccent = CreateWizardAccentColor(WizardAccentColorKind.Css, null, "   ");
            var target = RenderStep(invalidAccent, "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");
        }

        [Theory]
        [InlineData("var(--mud-palette-info")]
        [InlineData("rgb(17, 34, 51")]
        [InlineData("??invalid??")]
        public void GIVEN_InvalidCssAccentVariant_WHEN_Rendered_THEN_FallsBackToPrimary(string cssColor)
        {
            var target = RenderStep(WizardAccentColor.FromCss(cssColor), "AlertText", Severity.Info);
            var root = FindComponentByTestId<MudPaper>(target, "WizardStepRoot");

            root.Instance.Style.Should().Contain("--wizard-step-accent: var(--mud-palette-primary);");
        }

        private IRenderedComponent<WizardStep> RenderStep(WizardAccentColor accent, string? alertText, Severity alertSeverity)
        {
            return TestContext.Render<WizardStep>(parameters =>
            {
                parameters.Add(step => step.Accent, accent);
                parameters.Add(step => step.Icon, Icons.Material.Filled.Language);
                parameters.Add(step => step.Title, "Language");
                parameters.Add(step => step.Subtitle, "Thanks for using qbtmud. Choose your language to get started.");
                parameters.Add(step => step.AlertText, alertText);
                parameters.Add(step => step.AlertSeverity, alertSeverity);
                parameters.Add(step => step.ChildContent, CreateStepControls());
            });
        }

        private static RenderFragment CreateStepControls()
        {
            return builder =>
            {
                builder.OpenComponent<MudSelect<string>>(0);
                builder.AddAttribute(1, nameof(MudSelect<string>.Label), "User interface language:");
                builder.AddAttribute(2, nameof(MudSelect<string>.Value), "English");
                builder.AddAttribute(3, nameof(MudSelect<string>.Variant), Variant.Outlined);
                builder.AddAttribute(4, nameof(MudSelect<string>.ShrinkLabel), true);
                builder.AddAttribute(5, "data-test-id", TestIdHelper.For("WizardStepChildSelect"));
                builder.CloseComponent();
            };
        }

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        private static extern WizardAccentColor CreateWizardAccentColor(WizardAccentColorKind kind, Color? paletteColor, string? cssColor);
    }
}
