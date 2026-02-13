using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient.Test
{
    public sealed class UpdatePreferencesTests
    {
        [Fact]
        public void GIVEN_MaxRatioAndMaxRatioEnabledBothSet_WHEN_Validate_THEN_ShouldThrow()
        {
            var target = new UpdatePreferences
            {
                MaxRatio = 1.5f,
                MaxRatioEnabled = true
            };

            var action = () => target.Validate();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Specify either max_ratio or max_ratio_enabled, not both.");
        }

        [Fact]
        public void GIVEN_MaxSeedingTimeAndMaxSeedingTimeEnabledBothSet_WHEN_Validate_THEN_ShouldThrow()
        {
            var target = new UpdatePreferences
            {
                MaxSeedingTime = 10,
                MaxSeedingTimeEnabled = true
            };

            var action = () => target.Validate();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Specify either max_seeding_time or max_seeding_time_enabled, not both.");
        }

        [Fact]
        public void GIVEN_MaxInactiveSeedingTimeAndMaxInactiveSeedingTimeEnabledBothSet_WHEN_Validate_THEN_ShouldThrow()
        {
            var target = new UpdatePreferences
            {
                MaxInactiveSeedingTime = 10,
                MaxInactiveSeedingTimeEnabled = true
            };

            var action = () => target.Validate();

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Specify either max_inactive_seeding_time or max_inactive_seeding_time_enabled, not both.");
        }

        [Fact]
        public void GIVEN_MutuallyExclusiveFieldsNotOverlapping_WHEN_Validate_THEN_ShouldNotThrow()
        {
            var target = new UpdatePreferences
            {
                MaxRatioEnabled = true,
                MaxSeedingTime = 1440,
                MaxInactiveSeedingTimeEnabled = false
            };

            var action = () => target.Validate();

            action.Should().NotThrow();
        }
    }
}
