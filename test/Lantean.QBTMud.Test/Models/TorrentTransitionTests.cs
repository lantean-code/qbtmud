using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class TorrentTransitionTests
    {
        [Fact]
        public void GIVEN_Transition_WHEN_Constructed_THEN_ExposesExpectedProperties()
        {
            var result = new TorrentTransition(
                hash: "Hash",
                name: "Name",
                isAdded: true,
                previousIsFinished: false,
                currentIsFinished: true);

            result.Hash.Should().Be("Hash");
            result.Name.Should().Be("Name");
            result.IsAdded.Should().BeTrue();
            result.PreviousIsFinished.Should().BeFalse();
            result.CurrentIsFinished.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_Transition_WHEN_ToStringInvoked_THEN_IncludesKeyValues()
        {
            var result = new TorrentTransition(
                hash: "Hash",
                name: "Name",
                isAdded: false,
                previousIsFinished: true,
                currentIsFinished: true);

            result.ToString().Should().Contain("TorrentTransition");
            result.ToString().Should().Contain("Hash = Hash");
            result.ToString().Should().Contain("Name = Name");
            result.ToString().Should().Contain("IsAdded = False");
            result.ToString().Should().Contain("PreviousIsFinished = True");
            result.ToString().Should().Contain("CurrentIsFinished = True");
        }
    }
}
