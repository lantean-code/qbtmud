using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Presentation.Test.Components
{
    public sealed class Version53WorkflowsTests : RazorComponentTestBase
    {
        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_ApiVersion_WHEN_UsingSessionControls_THEN_OnlySupportedServersReceiveRequests(string version, bool supported)
        {
            SetVersion(version);
            var api = TestContext.UseApiClientMock();
            api.Setup(client => client.PauseSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ApiResult.CreateSuccess());
            api.Setup(client => client.ResumeSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ApiResult.CreateSuccess());
            var target = TestContext.Render<ApplicationActions>(parameters => parameters.Add(component => component.IsMenu, true));
            if (!supported)
            {
                target.FindComponents<MudMenuItem>().Should().NotContain(component => HasTestId(component, "PauseSession"));
                return;
            }

            var pause = FindComponentByTestId<MudMenuItem>(target, "PauseSession");
            await target.InvokeAsync(() => pause.Instance.OnClick.InvokeAsync());
            var resume = FindComponentByTestId<MudMenuItem>(target, "ResumeSession");
            await target.InvokeAsync(() => resume.Instance.OnClick.InvokeAsync());
            api.Verify(client => client.PauseSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
            api.Verify(client => client.ResumeSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("2.13.1", false)]
        [InlineData("2.15.1", false)]
        [InlineData("2.16.2", true)]
        public async Task GIVEN_DirectoryPath_WHEN_Changed_THEN_QueriesFreeSpaceOnlyWhenSupported(string version, bool supported)
        {
            SetVersion(version);
            var api = TestContext.UseApiClientMock();
            api.Setup(client => client.GetFreeSpaceAtPathAsync("SavePath", It.IsAny<CancellationToken>())).ReturnsSuccessAsync(1024L);
            TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<PathAutocomplete>(parameters => parameters
                .Add(component => component.Value, "SavePath")
                .Add(component => component.ShowFreeSpace, true));
            api.Verify(client => client.GetFreeSpaceAtPathAsync("SavePath", It.IsAny<CancellationToken>()), supported ? Times.Once() : Times.Never());
            var input = target.FindComponent<MudAutocomplete<string>>();
            if (supported)
            {
                input.Instance.HelperText.Should().Be("Free space: 1 KiB");
            }

            await target.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(string.Empty));
            input.Instance.HelperText.Should().BeNull();
        }

        [Theory]
        [InlineData(false, 1, true)]
        [InlineData(false, 0.5, false)]
        [InlineData(true, 1, false)]
        public async Task GIVEN_SelectedContent_WHEN_Downloading_THEN_OnlyCompletedFilesCanBeDownloaded(bool folder, double progress, bool downloadable)
        {
            SetVersion("2.16.2");
            var target = TestContext.Render<FilesTab>(parameters => parameters.Add(component => component.Hash, "Hash").AddCascadingValue("RefreshInterval", 10000));
            var item = new ContentItem("Folder/File name", "File name", 1, Priority.Normal, progress, 100, 1, folder);
            var table = target.FindComponent<DynamicTable<ContentItem>>();
            await target.InvokeAsync(() => table.Instance.SelectedItemChanged.InvokeAsync(item));
            var button = FindComponentByTestId<MudIconButton>(target, "DownloadFileToolbar");
            button.Instance.Disabled.Should().Be(!downloadable);
            if (downloadable)
            {
                var download = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
                download.SetVoidResult();
                await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());
                download.Invocations.Should().ContainSingle().Which.Arguments.Should().Contain("File name");
            }
        }

        private void SetVersion(string version)
        {
            var capabilities = new Mock<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(capabilities.Object);
            capabilities.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(Version.Parse(version), true));
        }
    }
}
