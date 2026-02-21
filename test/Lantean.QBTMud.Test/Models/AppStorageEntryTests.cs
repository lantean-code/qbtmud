using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class AppStorageEntryTests
    {
        [Fact]
        public void GIVEN_EntryWithNullValue_WHEN_Constructed_THEN_ExposesExpectedProperties()
        {
            var result = new AppStorageEntry(
                key: "QbtMud.AppSettings.State.v1",
                displayKey: "AppSettings.State.v1",
                value: null,
                preview: "{}",
                length: 2);

            result.Key.Should().Be("QbtMud.AppSettings.State.v1");
            result.DisplayKey.Should().Be("AppSettings.State.v1");
            result.Value.Should().BeNull();
            result.Preview.Should().Be("{}");
            result.Length.Should().Be(2);
        }

        [Fact]
        public void GIVEN_Entry_WHEN_ToStringInvoked_THEN_IncludesAllPrimaryValues()
        {
            var result = new AppStorageEntry(
                key: "QbtMud.AppSettings.State.v1",
                displayKey: "AppSettings.State.v1",
                value: "{\"value\":true}",
                preview: "{\"value\":true}",
                length: 14);

            result.ToString().Should().Contain("AppStorageEntry");
            result.ToString().Should().Contain("QbtMud.AppSettings.State.v1");
            result.ToString().Should().Contain("AppSettings.State.v1");
            result.ToString().Should().Contain("{\"value\":true}");
            result.ToString().Should().Contain("14");
        }
    }
}
