using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageEmbeddedResourceLoaderTests
    {
        private readonly IAssemblyResourceAccessor _resourceAccessor;
        private readonly ILogger<LanguageEmbeddedResourceLoader> _logger;
        private readonly LanguageEmbeddedResourceLoader _target;

        public LanguageEmbeddedResourceLoaderTests()
        {
            _resourceAccessor = Mock.Of<IAssemblyResourceAccessor>();
            _logger = Mock.Of<ILogger<LanguageEmbeddedResourceLoader>>();
            _target = new LanguageEmbeddedResourceLoader(_resourceAccessor, _logger);
        }

        [Fact]
        public async Task GIVEN_CanceledToken_WHEN_LoadDictionaryAsync_THEN_ShouldThrowOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Func<Task> action = async () =>
            {
                await _target.LoadDictionaryAsync("webui_en.json", cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_MissingEmbeddedResource_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            Mock.Get(_resourceAccessor)
                .Setup(accessor => accessor.GetManifestResourceNames())
                .Returns([]);

            var result = await _target.LoadDictionaryAsync("missing_resource.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_EmbeddedEnglishResource_WHEN_LoadDictionaryAsync_THEN_ShouldReturnDeserializedDictionary()
        {
            var resourceAccessor = new AssemblyResourceAccessor();
            var target = new LanguageEmbeddedResourceLoader(resourceAccessor, _logger);

            var result = await target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().ContainKey("misc|Unknown").WhoseValue.Should().Be("Unknown");
        }

        [Fact]
        public async Task GIVEN_UppercaseResourceName_WHEN_LoadDictionaryAsync_THEN_ShouldResolveCaseInsensitively()
        {
            var resourceAccessor = new AssemblyResourceAccessor();
            var target = new LanguageEmbeddedResourceLoader(resourceAccessor, _logger);

            var result = await target.LoadDictionaryAsync("WEBUI_EN.JSON", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().ContainKey("misc|Unknown");
        }

        [Fact]
        public async Task GIVEN_NullResourceStream_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            Mock.Get(_resourceAccessor)
                .Setup(accessor => accessor.GetManifestResourceNames())
                .Returns(["Lantean.QBTMud.wwwroot.i18n.webui_en.json"]);
            Mock.Get(_resourceAccessor)
                .Setup(accessor => accessor.GetManifestResourceStream("Lantean.QBTMud.wwwroot.i18n.webui_en.json"))
                .Returns((Stream?)null);

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_InvalidEmbeddedJson_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            using var invalidJson = new MemoryStream(Encoding.UTF8.GetBytes("{"));

            Mock.Get(_resourceAccessor)
                .Setup(accessor => accessor.GetManifestResourceNames())
                .Returns(["Lantean.QBTMud.wwwroot.i18n.webui_en.json"]);
            Mock.Get(_resourceAccessor)
                .Setup(accessor => accessor.GetManifestResourceStream("Lantean.QBTMud.wwwroot.i18n.webui_en.json"))
                .Returns(invalidJson);

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }
    }
}
