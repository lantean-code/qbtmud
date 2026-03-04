using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class StorageRoutingSettingsTests
    {
        private readonly StorageRoutingSettings _target;

        public StorageRoutingSettingsTests()
        {
            _target = new StorageRoutingSettings();
        }

        [Fact]
        public void GIVEN_NullSettings_WHEN_NormalizeInvoked_THEN_ThrowsArgumentNullException()
        {
            var act = () => StorageRoutingSettings.Normalize(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GIVEN_InvalidMasterAndOverrideValues_WHEN_NormalizeInvoked_THEN_FiltersToSupportedValues()
        {
            _target.MasterStorageType = (StorageType)987;
            _target.GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
            {
                [" themes "] = StorageType.ClientData,
                [" "] = StorageType.ClientData,
                ["invalid"] = (StorageType)777
            };
            _target.ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
            {
                [" themes.selected-theme "] = StorageType.LocalStorage,
                ["  "] = StorageType.LocalStorage,
                ["invalid"] = (StorageType)666
            };

            var result = StorageRoutingSettings.Normalize(_target);

            result.MasterStorageType.Should().Be(StorageType.LocalStorage);
            result.GroupStorageTypes.Should().ContainKey("themes");
            result.GroupStorageTypes["themes"].Should().Be(StorageType.ClientData);
            result.GroupStorageTypes.Should().NotContainKey(" ");
            result.GroupStorageTypes.Should().NotContainKey("invalid");
            result.ItemStorageTypes.Should().ContainKey("themes.selected-theme");
            result.ItemStorageTypes["themes.selected-theme"].Should().Be(StorageType.LocalStorage);
            result.ItemStorageTypes.Should().NotContainKey("  ");
            result.ItemStorageTypes.Should().NotContainKey("invalid");
        }

        [Fact]
        public void GIVEN_NullOverrideDictionaries_WHEN_NormalizeInvoked_THEN_ThrowsArgumentNullException()
        {
            _target.GroupStorageTypes = null!;
            _target.ItemStorageTypes = null!;

            var act = () => StorageRoutingSettings.Normalize(_target);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
