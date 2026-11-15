using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.UI
{
    public sealed class TorrentSplitPanelTests : RazorComponentTestBase<TorrentSplitPanel>
    {
        private readonly ILocalStorageService _storage = Mock.Of<ILocalStorageService>();
        private readonly BunitJSModuleInterop _module;

        public TorrentSplitPanelTests()
        {
            TestContext.Services.AddSingleton(_storage);
            _module = TestContext.JSInterop.SetupModule("./_content/Lantean.QBTMud.Presentation/torrent-split-panel.module.js");
            _module.SetupVoid("initialize", _ => true).SetVoidResult();
            _module.SetupVoid("setRatio", _ => true).SetVoidResult();
        }

        [Theory]
        [InlineData(false, true, "Hash", false)]
        [InlineData(true, false, "Hash", false)]
        [InlineData(true, true, null, false)]
        [InlineData(true, true, "Hash", true)]
        public async Task GIVEN_ConfigurationAndSelection_WHEN_AvailabilityChanges_THEN_ShowsOnlyEligibleDetails(bool enabled, bool available, string? hash, bool expected)
        {
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, enabled)
                .Add(component => component.Hash, hash));

            await target.InvokeAsync(() => target.Instance.SetAvailability(available));

            target.Instance.IsOpen.Should().Be(expected);
            target.FindComponents<Details>().Count.Should().Be(expected ? 1 : 0);
        }

        [Fact]
        public async Task GIVEN_OpenPanel_WHEN_Rendered_THEN_SizeControlIsInDetailsHeader()
        {
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, true)
                .Add(component => component.Hash, "Hash"));

            await target.InvokeAsync(() => target.Instance.SetAvailability(true));

            var details = target.FindComponent<Details>();
            details.Instance.HeaderContent.Should().NotBeNull();
            FindComponentByTestId<MudSelect<int>>(details, "TorrentSplitSize").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_OpenPanel_WHEN_SelectionCleared_THEN_DetailsAreRemovedAndRestoreOnReselection()
        {
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, true)
                .Add(component => component.Hash, "Hash"));
            await target.InvokeAsync(() => target.Instance.SetAvailability(true));

            target.Render(parameters => parameters.Add(component => component.Hash, (string?)null));

            target.Instance.IsOpen.Should().BeFalse();
            target.FindComponents<Details>().Should().BeEmpty();

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            var details = target.FindComponent<Details>();
            details.Instance.Hash.Should().Be("OtherHash");
            details.Instance.Embedded.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_OpenPanel_WHEN_HiddenAndRestored_THEN_SelectionAndSizeArePreserved()
        {
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, true)
                .Add(component => component.Hash, "Hash"));
            await target.InvokeAsync(() => target.Instance.SetAvailability(true));
            var size = FindComponentByTestId<MudSelect<int>>(target, "TorrentSplitSize");
            await target.InvokeAsync(() => size.Instance.ValueChanged.InvokeAsync(55));

            var details = target.FindComponent<Details>();
            await target.InvokeAsync(() => details.Instance.OnHide.InvokeAsync());

            target.Instance.IsOpen.Should().BeFalse();

            await target.InvokeAsync(target.Instance.ToggleDetails);

            target.Instance.IsOpen.Should().BeTrue();
            FindComponentByTestId<MudSelect<int>>(target, "TorrentSplitSize").Instance.GetState(component => component.Value).Should().Be(55);
            target.FindComponent<Details>().Instance.Hash.Should().Be("Hash");
            Mock.Get(_storage).Verify(service => service.SetItemAsync("TorrentList.DetailsRatio", 55, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0, 30)]
        [InlineData(45, 45)]
        [InlineData(100, 60)]
        public async Task GIVEN_ResizeRequest_WHEN_RatioSaved_THEN_ClampsAndPersistsRatio(int ratio, int expected)
        {
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, true)
                .Add(component => component.Hash, "Hash"));
            await target.InvokeAsync(() => target.Instance.SetAvailability(true));

            await target.InvokeAsync(() => target.Instance.ChangeRatio(ratio));

            FindComponentByTestId<MudSelect<int>>(target, "TorrentSplitSize").Instance.GetState(component => component.Value).Should().Be(expected);
            Mock.Get(_storage).Verify(service => service.SetItemAsync("TorrentList.DetailsRatio", expected, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SavedRatio_WHEN_ViewportShrinksAndGrows_THEN_RestoresPanelSize()
        {
            Mock.Get(_storage).Setup(service => service.GetItemAsync<int?>("TorrentList.DetailsRatio", It.IsAny<CancellationToken>())).ReturnsAsync(55);
            var target = TestContext.Render<TorrentSplitPanel>(parameters => parameters
                .Add(component => component.Enabled, true)
                .Add(component => component.Hash, "Hash"));
            await target.InvokeAsync(() => target.Instance.SetAvailability(true));
            await target.InvokeAsync(() => target.Instance.SetAvailability(false));

            target.FindComponents<Details>().Should().BeEmpty();

            await target.InvokeAsync(() => target.Instance.SetAvailability(true));
            await target.InvokeAsync(() => target.Instance.SetAvailability(true));

            FindComponentByTestId<MudSelect<int>>(target, "TorrentSplitSize").Instance.GetState(component => component.Value).Should().Be(55);
        }

        [Fact]
        public async Task GIVEN_DisposedPanel_WHEN_LateAvailabilityNotificationArrives_THEN_DoesNotRender()
        {
            var target = TestContext.Render<TorrentSplitPanel>();

            await target.Instance.DisposeAsync();
            await target.Instance.SetAvailability(true);

            target.Instance.IsOpen.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DisabledPanel_WHEN_SizeChangedBeforeInitialization_THEN_PersistsWithoutCallingJavaScript()
        {
            var target = TestContext.Render<TorrentSplitPanel>();

            await target.InvokeAsync(() => target.Instance.ChangeRatio(55));

            Mock.Get(_storage).Verify(service => service.SetItemAsync("TorrentList.DetailsRatio", 55, It.IsAny<CancellationToken>()), Times.Once);
            _module.Invocations.Should().BeEmpty();
        }
    }
}
