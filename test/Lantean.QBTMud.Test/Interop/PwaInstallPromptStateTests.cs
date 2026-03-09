using AwesomeAssertions;
using Lantean.QBTMud.Interop;

namespace Lantean.QBTMud.Test.Interop
{
    public sealed class PwaInstallPromptStateTests
    {
        [Fact]
        public void GIVEN_StateValues_WHEN_Assigned_THEN_PropertiesMatch()
        {
            var result = new PwaInstallPromptState
            {
                IsInstalled = true,
                CanPrompt = true,
                IsIos = true,
                IsSafari = true
            };

            result.IsInstalled.Should().BeTrue();
            result.CanPrompt.Should().BeTrue();
            result.IsIos.Should().BeTrue();
            result.IsSafari.Should().BeTrue();
        }
    }
}
