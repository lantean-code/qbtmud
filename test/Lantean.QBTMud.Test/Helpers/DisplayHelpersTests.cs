using AwesomeAssertions;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Test.Helpers
{
    public sealed class DisplayHelpersTests
    {
        [Fact]
        public void GIVEN_Duration_WHEN_NullOrSpecialValues_THEN_ShouldReturnExpectedTokens()
        {
            DisplayHelpers.Duration(null).Should().Be(string.Empty);
            DisplayHelpers.Duration(-1).Should().Be("< 1m");
            DisplayHelpers.Duration(0).Should().Be("< 1m");
            DisplayHelpers.Duration(59).Should().Be("< 1m");
            DisplayHelpers.Duration(8_640_000).Should().Be("∞");
            DisplayHelpers.Duration(long.MaxValue).Should().Be("∞");
        }

        [Fact]
        public void GIVEN_Duration_WHEN_RegularValues_THEN_ShouldFormatWithUnitsPrefixAndSuffix()
        {
            DisplayHelpers.Duration(60).Should().Be("1m");
            DisplayHelpers.Duration(3_900).Should().Be("1h 5m");
            DisplayHelpers.Duration(90_000).Should().Be("1d 1h");
            DisplayHelpers.Duration(90_000, "ETA ", "left").Should().Be("ETA 1d 1h left");
        }

        [Fact]
        public void GIVEN_Size_WHEN_NullNegativeOrObjectType_THEN_ShouldNormalize()
        {
            DisplayHelpers.Size((long?)null).Should().Be(string.Empty);
            DisplayHelpers.Size(-10).Should().StartWith("0");
            DisplayHelpers.Size("not-a-size").Should().Be(string.Empty);
        }

        [Fact]
        public void GIVEN_SizeAndSpeed_WHEN_Formatted_THEN_ShouldIncludeExpectedSuffixes()
        {
            DisplayHelpers.Size(1024, "pre-", "-post").Should().Contain("pre-").And.Contain("-post");
            DisplayHelpers.Speed(null).Should().Be(string.Empty);
            DisplayHelpers.Speed(-1).Should().Be("∞");
            DisplayHelpers.Speed(1024, "pre-", "-post").Should().Contain("/s").And.Contain("pre-").And.Contain("-post");
        }

        [Fact]
        public void GIVEN_EmptyIfNullGeneric_WHEN_FormattingDifferentNumericTypes_THEN_ShouldUseFormatWhenSupported()
        {
            DisplayHelpers.EmptyIfNull<int>(null).Should().Be(string.Empty);
            DisplayHelpers.EmptyIfNull<long>(12L, format: "000").Should().Be("012");
            DisplayHelpers.EmptyIfNull<int>(5, format: "00").Should().Be("05");
            DisplayHelpers.EmptyIfNull<float>(1.5f, format: "0.0").Should().Be("1.5");
            DisplayHelpers.EmptyIfNull<double>(1.5d, format: "0.0").Should().Be("1.5");
            DisplayHelpers.EmptyIfNull<decimal>(1.5m, format: "0.0").Should().Be("1.5");
            DisplayHelpers.EmptyIfNull<short>((short)7, format: "00").Should().Be("07");
            DisplayHelpers.EmptyIfNull<byte>((byte)3, format: "00").Should().Be("3");
            DisplayHelpers.EmptyIfNull<int>(42, prefix: "(", suffix: ")").Should().Be("(42)");
        }

        [Fact]
        public void GIVEN_EmptyIfNullString_WHEN_NullOrValue_THEN_ShouldReturnExpected()
        {
            DisplayHelpers.EmptyIfNull((string?)null).Should().Be(string.Empty);
            DisplayHelpers.EmptyIfNull("value", "pre-", "-post").Should().Be("pre-value-post");
        }

        [Fact]
        public void GIVEN_DateTimePercentageBoolAndRatio_WHEN_Formatted_THEN_ShouldMatchRules()
        {
            DisplayHelpers.DateTime(null).Should().Be(string.Empty);
            DisplayHelpers.DateTime(-1, "Never").Should().Be("Never");
            DisplayHelpers.DateTime(946684800).Should().NotBeNullOrWhiteSpace();

            DisplayHelpers.Percentage(null).Should().Be(string.Empty);
            DisplayHelpers.Percentage(-1).Should().Be("0%");
            DisplayHelpers.Percentage(0).Should().Be("0%");
            DisplayHelpers.Percentage(0.155f).Should().Be("15.5%");

            DisplayHelpers.Bool(true).Should().Be("Yes");
            DisplayHelpers.Bool(false, "On", "Off").Should().Be("Off");

            DisplayHelpers.RatioLimit(Limits.GlobalLimit).Should().Be("Global");
            DisplayHelpers.RatioLimit(Limits.NoLimit).Should().Be("∞");
            DisplayHelpers.RatioLimit(1.234f).Should().Be("1.23");
        }

        [Theory]
        [InlineData("downloading", "Downloading")]
        [InlineData("stalledDL", "Stalled")]
        [InlineData("metaDL", "Downloading metadata")]
        [InlineData("forcedMetaDL", "[F] Downloading metadata")]
        [InlineData("forcedDL", "[F] Downloading")]
        [InlineData("uploading", "Seeding")]
        [InlineData("stalledUP", "Seeding")]
        [InlineData("forcedUP", "[F] Seeding")]
        [InlineData("queuedDL", "Queued")]
        [InlineData("queuedUP", "Queued")]
        [InlineData("checkingDL", "Checking")]
        [InlineData("checkingUP", "Checking")]
        [InlineData("queuedForChecking", "Queued for checking")]
        [InlineData("checkingResumeData", "Checking resume data")]
        [InlineData("pausedDL", "Paused")]
        [InlineData("pausedUP", "Completed")]
        [InlineData("stoppedDL", "Stopped")]
        [InlineData("stoppedUP", "Completed")]
        [InlineData("moving", "Moving")]
        [InlineData("missingFiles", "Missing Files")]
        [InlineData("error", "Errored")]
        [InlineData("unknown", "Unknown")]
        public void GIVEN_State_WHEN_Mapped_THEN_ShouldReturnExpectedLabel(string state, string expected)
        {
            DisplayHelpers.State(state).Should().Be(expected);
        }

        [Theory]
        [InlineData("forcedDL", Icons.Material.Filled.Downloading, Color.Success)]
        [InlineData("uploading", Icons.Material.Filled.Upload, Color.Info)]
        [InlineData("stalledUP", Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info)]
        [InlineData("stalledDL", Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success)]
        [InlineData("pausedDL", Icons.Material.Filled.Pause, Color.Success)]
        [InlineData("pausedUP", Icons.Material.Filled.Pause, Color.Info)]
        [InlineData("stoppedDL", Icons.Material.Filled.Stop, Color.Success)]
        [InlineData("stoppedUP", Icons.Material.Filled.Stop, Color.Info)]
        [InlineData("queuedDL", Icons.Material.Filled.Queue, Color.Default)]
        [InlineData("checkingDL", Icons.Material.Filled.Loop, Color.Info)]
        [InlineData("queuedForChecking", Icons.Material.Filled.Loop, Color.Warning)]
        [InlineData("moving", Icons.Material.Filled.Moving, Color.Info)]
        [InlineData("error", Icons.Material.Filled.Error, Color.Error)]
        [InlineData("unknown-state", Icons.Material.Filled.QuestionMark, Color.Warning)]
        public void GIVEN_GetStateIcon_WHEN_StateProvided_THEN_ShouldReturnExpectedIconAndColor(string state, string expectedIcon, Color expectedColor)
        {
            var (icon, color) = DisplayHelpers.GetStateIcon(state);
            icon.Should().Be(expectedIcon);
            color.Should().Be(expectedColor);
        }

        [Theory]
        [InlineData(Status.All, Icons.Material.Filled.AllOut, Color.Warning)]
        [InlineData(Status.Downloading, Icons.Material.Filled.Downloading, Color.Success)]
        [InlineData(Status.Seeding, Icons.Material.Filled.Upload, Color.Info)]
        [InlineData(Status.Completed, Icons.Material.Filled.Check, Color.Default)]
        [InlineData(Status.Stopped, Icons.Material.Filled.Stop, Color.Default)]
        [InlineData(Status.Active, Icons.Material.Filled.Sort, Color.Success)]
        [InlineData(Status.Inactive, Icons.Material.Filled.Sort, Color.Error)]
        [InlineData(Status.Stalled, Icons.Material.Filled.Sort, Color.Info)]
        [InlineData(Status.StalledUploading, Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info)]
        [InlineData(Status.StalledDownloading, Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success)]
        [InlineData(Status.Checking, Icons.Material.Filled.Loop, Color.Info)]
        [InlineData(Status.Errored, Icons.Material.Filled.Error, Color.Error)]
        public void GIVEN_GetStatusIcon_WHEN_KnownStatusProvided_THEN_ShouldReturnExpected(Status status, string expectedIcon, Color expectedColor)
        {
            var (icon, color) = DisplayHelpers.GetStatusIcon(status.ToString());
            icon.Should().Be(expectedIcon);
            color.Should().Be(expectedColor);
        }

        [Fact]
        public void GIVEN_GetStatusIcon_WHEN_UnknownNumericStatusProvided_THEN_ShouldUseFallback()
        {
            var (icon, color) = DisplayHelpers.GetStatusIcon("999");
            icon.Should().Be(Icons.Material.Filled.QuestionMark);
            color.Should().Be(Color.Inherit);
        }
    }
}
