using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;
using System.Reflection;
using System.Reflection.Emit;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class AppBuildInfoServiceTests
    {
        [Fact]
        public void GIVEN_AssemblyWithBuildMetadata_WHEN_GetCurrentBuildInfoInvoked_THEN_UsesStampedAssemblyMetadata()
        {
            var assemblyName = new AssemblyName("QbtMudAppBuildInfoServiceTests.MetadataAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var metadataCtor = typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) })!;
            var metadataAttribute = new CustomAttributeBuilder(metadataCtor, new object[] { "QbtMudBuildVersion", "9.9.9" });
            assemblyBuilder.SetCustomAttribute(metadataAttribute);

            var target = new AppBuildInfoService(assemblyBuilder);

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().Be("AssemblyMetadata");
            result.Version.Should().Be("9.9.9");
        }

        [Fact]
        public void GIVEN_AssemblyWithoutBuildMetadata_WHEN_GetCurrentBuildInfoInvoked_THEN_FallsBackToInformationalOrAssemblyVersion()
        {
            var target = new AppBuildInfoService(typeof(AppBuildInfoServiceTests).Assembly);

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().BeOneOf("InformationalVersion", "AssemblyVersion");
            result.Version.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GIVEN_AssemblyWithWhitespaceMetadataAndInformationalVersion_WHEN_GetCurrentBuildInfoInvoked_THEN_UsesInformationalVersion()
        {
            var assemblyName = new AssemblyName("QbtMudAppBuildInfoServiceTests.InformationalAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var metadataCtor = typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) })!;
            var metadataAttribute = new CustomAttributeBuilder(metadataCtor, new object[] { "QbtMudBuildVersion", "   " });
            assemblyBuilder.SetCustomAttribute(metadataAttribute);

            var informationalCtor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) })!;
            var informationalAttribute = new CustomAttributeBuilder(informationalCtor, new object[] { "  3.4.5-informational  " });
            assemblyBuilder.SetCustomAttribute(informationalAttribute);

            var target = new AppBuildInfoService(assemblyBuilder);

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().Be("InformationalVersion");
            result.Version.Should().Be("3.4.5-informational");
        }

        [Fact]
        public void GIVEN_AssemblyWithoutMetadataAndInformationalVersion_WHEN_GetCurrentBuildInfoInvoked_THEN_UsesAssemblyVersion()
        {
            var assemblyName = new AssemblyName("QbtMudAppBuildInfoServiceTests.VersionAssembly")
            {
                Version = new Version(4, 3, 2, 1)
            };
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var target = new AppBuildInfoService(assemblyBuilder);

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().Be("AssemblyVersion");
            result.Version.Should().Be("4.3.2.1");
        }

        [Fact]
        public void GIVEN_DefaultConstructor_WHEN_GetCurrentBuildInfoInvoked_THEN_ReturnsKnownSource()
        {
            var target = new AppBuildInfoService();

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().BeOneOf("AssemblyMetadata", "InformationalVersion", "AssemblyVersion", "Unavailable");
            result.Version.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GIVEN_AssemblyWithoutMetadataInformationalOrVersion_WHEN_GetCurrentBuildInfoInvoked_THEN_ReturnsUnavailable()
        {
            var assembly = new Mock<Assembly>(MockBehavior.Strict);
            assembly
                .Setup(value => value.GetCustomAttributes(typeof(AssemblyMetadataAttribute), It.IsAny<bool>()))
                .Returns(Array.Empty<AssemblyMetadataAttribute>());
            assembly
                .Setup(value => value.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), It.IsAny<bool>()))
                .Returns(Array.Empty<AssemblyInformationalVersionAttribute>());
            assembly
                .Setup(value => value.GetName())
                .Returns(new AssemblyName("NoVersion"));

            var target = new AppBuildInfoService(assembly.Object);

            var result = target.GetCurrentBuildInfo();

            result.Source.Should().Be("Unavailable");
            result.Version.Should().Be("unknown");
        }
    }
}
