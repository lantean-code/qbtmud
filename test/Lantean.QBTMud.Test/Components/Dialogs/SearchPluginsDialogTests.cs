using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class SearchPluginsDialogTests : RazorComponentTestBase<SearchPluginsDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly SearchPluginsDialogTestDriver _target;

        public SearchPluginsDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_snackbar);

            _target = new SearchPluginsDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_LoadFails_WHEN_Rendered_THEN_ShowsSnackbarError()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ThrowsAsync(new HttpRequestException("Failed"));

            var snackbarMock = Mock.Get(_snackbar);

            await _target.RenderDialogAsync();

            snackbarMock.Verify(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("Failed to load search plugins: Failed")), Severity.Error), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Loading_WHEN_Rendered_THEN_ShowsProgress()
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<SearchPlugin>>();
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .Returns(tcs.Task);

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponent<MudProgressLinear>();

            tcs.SetResult(new List<SearchPlugin>());
        }

        [Fact]
        public async Task GIVEN_NullPluginResponse_WHEN_Rendered_THEN_ShowsEmptyList()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .Returns(Task.FromResult<IReadOnlyList<SearchPlugin>>(null!));

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.FindComponents<MudTable<SearchPlugin>>().Should().ContainSingle();
            dialog.Component.FindComponents<MudTr>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NoInstallSources_WHEN_InstallClicked_THEN_DoesNotCallApi()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);

            var dialog = await _target.RenderDialogAsync();

            var urlButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginInstallUrlButton");
            await dialog.Component.InvokeAsync(() => urlButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var pathButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginInstallPathButton");
            await dialog.Component.InvokeAsync(() => pathButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            apiClientMock.Verify(client => client.InstallSearchPlugins(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_ActionButtonsClicked_THEN_DoesNotCallApi()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);

            var dialog = await _target.RenderDialogAsync();

            var enableButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginEnable");
            await dialog.Component.InvokeAsync(() => enableButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var disableButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginDisable");
            await dialog.Component.InvokeAsync(() => disableButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var uninstallButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginUninstall");
            await dialog.Component.InvokeAsync(() => uninstallButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            apiClientMock.Verify(client => client.EnableSearchPlugins(It.IsAny<string[]>()), Times.Never);
            apiClientMock.Verify(client => client.DisableSearchPlugins(It.IsAny<string[]>()), Times.Never);
            apiClientMock.Verify(client => client.UninstallSearchPlugins(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoPlugins_WHEN_UpdateAllClicked_THEN_DoesNotCallApi()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(new List<SearchPlugin>());

            var dialog = await _target.RenderDialogAsync();

            var updateButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginUpdateAll");
            await dialog.Component.InvokeAsync(() => updateButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            apiClientMock.Verify(client => client.UpdateSearchPlugins(), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshClicked_WHEN_Clicked_THEN_ReloadsPlugins()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins);

            var dialog = await _target.RenderDialogAsync();

            var refreshButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginRefresh");
            await refreshButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.GetSearchPlugins(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ToggleDisableRequested_WHEN_Clicked_THEN_DisablesPlugin()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.DisableSearchPlugins(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var toggleButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginToggle-Plugin");
            await toggleButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.DisableSearchPlugins(It.Is<string[]>(names => names.SequenceEqual(new[] { "Plugin" }))), Times.Once);
            toggleButton.Instance.Icon.Should().Be(Icons.Material.Outlined.ToggleOff);
        }

        [Fact]
        public async Task GIVEN_SelectionToggledTwice_WHEN_Clicked_THEN_RemovesSelection()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);

            var dialog = await _target.RenderDialogAsync();

            var selectButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginSelect-Plugin");
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());

            selectButton.Instance.Icon.Should().Be(Icons.Material.Outlined.CheckBoxOutlineBlank);
        }

        [Fact]
        public async Task GIVEN_PluginSelected_WHEN_ActionButtonsClicked_THEN_CallsApiAndReturnsChanges()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, "http://example.com") };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.EnableSearchPlugins(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            apiClientMock
                .Setup(client => client.DisableSearchPlugins(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);
            apiClientMock
                .Setup(client => client.UninstallSearchPlugins(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var selectButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginSelect-Plugin");
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());

            var enableButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginEnable");
            enableButton.Instance.Disabled.Should().BeFalse();
            await enableButton.Find("button").ClickAsync(new MouseEventArgs());

            selectButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginSelect-Plugin");
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());

            var disableButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginDisable");
            disableButton.Instance.Disabled.Should().BeFalse();
            await disableButton.Find("button").ClickAsync(new MouseEventArgs());

            selectButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginSelect-Plugin");
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());

            var uninstallButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginUninstall");
            uninstallButton.Instance.Disabled.Should().BeFalse();
            await uninstallButton.Find("button").ClickAsync(new MouseEventArgs());

            var closeButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginClose");
            await closeButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.EnableSearchPlugins(It.Is<string[]>(names => names.SequenceEqual(new[] { "Plugin" }))), Times.Once);
            apiClientMock.Verify(client => client.DisableSearchPlugins(It.Is<string[]>(names => names.SequenceEqual(new[] { "Plugin" }))), Times.Once);
            apiClientMock.Verify(client => client.UninstallSearchPlugins(It.Is<string[]>(names => names.SequenceEqual(new[] { "Plugin" }))), Times.Once);

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);
        }

        [Fact]
        public async Task GIVEN_InstallUrlProvided_WHEN_Clicked_THEN_InstallsAndClearsInput()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.InstallSearchPlugins(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var urlField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "SearchPluginInstallUrl");
            urlField.Find("input").Input(" https://example.com/plugin.zip ");

            var installButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginInstallUrlButton");
            await installButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.InstallSearchPlugins("https://example.com/plugin.zip"), Times.Once);
            urlField.Instance.GetState(x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_InstallPathProvided_WHEN_Clicked_THEN_InstallsAndClearsInput()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.InstallSearchPlugins(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var pathField = FindComponentByTestId<PathAutocomplete>(dialog.Component, "SearchPluginInstallPath");
            await dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("/path/plugin.py"));

            var installButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginInstallPathButton");
            await installButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.InstallSearchPlugins("/path/plugin.py"), Times.Once);
            pathField.Instance.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_InstallPathFails_WHEN_Clicked_THEN_ShowsErrorAndKeepsInput()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.InstallSearchPlugins(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Failed"));

            var snackbarMock = Mock.Get(_snackbar);

            var dialog = await _target.RenderDialogAsync();

            var pathField = FindComponentByTestId<PathAutocomplete>(dialog.Component, "SearchPluginInstallPath");
            await dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("/path/plugin.py"));

            var installButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginInstallPathButton");
            await installButton.Find("button").ClickAsync(new MouseEventArgs());

            snackbarMock.Verify(snackbar => snackbar.Add(It.Is<string>(message => message.Contains("Search plugin operation failed: Failed")), Severity.Error), Times.Once);
            pathField.Instance.Value.Should().Be("/path/plugin.py");
        }

        [Fact]
        public async Task GIVEN_UpdateAllRequested_WHEN_Clicked_THEN_CallsApi()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, "http://example.com") };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins)
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.UpdateSearchPlugins())
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var updateButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginUpdateAll");
            await updateButton.Find("button").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.UpdateSearchPlugins(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_OperationInProgress_WHEN_ToggleClicked_THEN_DoesNotCallApi()
        {
            var tcs = new TaskCompletionSource();
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.EnableSearchPlugins(It.IsAny<string[]>()))
                .Returns(tcs.Task);
            apiClientMock
                .Setup(client => client.DisableSearchPlugins(It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync();

            var selectButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginSelect-Plugin");
            await selectButton.Find("button").ClickAsync(new MouseEventArgs());

            var enableButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginEnable");
            var enableTask = enableButton.Find("button").ClickAsync(new MouseEventArgs());

            await Task.Yield();

            var toggleButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginToggle-Plugin");
            await dialog.Component.InvokeAsync(() => toggleButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            apiClientMock.Verify(client => client.DisableSearchPlugins(It.IsAny<string[]>()), Times.Never);

            tcs.SetResult();
            await enableTask;
        }

        [Fact]
        public async Task GIVEN_ToggleFails_WHEN_Clicked_THEN_RevertsState()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", false, string.Empty) };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);
            apiClientMock
                .Setup(client => client.EnableSearchPlugins(It.IsAny<string[]>()))
                .ThrowsAsync(new HttpRequestException("Failed"));

            var dialog = await _target.RenderDialogAsync();

            var toggleButton = FindComponentByTestId<MudIconButton>(dialog.Component, "SearchPluginToggle-Plugin");
            await toggleButton.Find("button").ClickAsync(new MouseEventArgs());

            toggleButton.Instance.Icon.Should().Be(Icons.Material.Outlined.ToggleOff);
        }

        [Fact]
        public async Task GIVEN_NoChanges_WHEN_Closed_THEN_ResultFalse()
        {
            var plugins = new List<SearchPlugin> { CreatePlugin("Plugin", true, "http://example.com") };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetSearchPlugins())
                .ReturnsAsync(plugins);

            var dialog = await _target.RenderDialogAsync();

            var closeButton = FindComponentByTestId<MudButton>(dialog.Component, "SearchPluginClose");
            closeButton.Instance.Variant.Should().Be(Variant.Filled);
            closeButton.Instance.Color.Should().Be(Color.Success);
            await closeButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(false);
        }

        private static SearchPlugin CreatePlugin(string name, bool enabled, string url)
        {
            return new SearchPlugin(
                enabled,
                $"Full{name}",
                name,
                Array.Empty<SearchCategory>(),
                url,
                "1.0.0");
        }
    }

    internal sealed class SearchPluginsDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public SearchPluginsDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<SearchPluginsDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var reference = await dialogService.ShowAsync<SearchPluginsDialog>("Search Plugins");

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<SearchPluginsDialog>();

            return new SearchPluginsDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class SearchPluginsDialogRenderContext
    {
        public SearchPluginsDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<SearchPluginsDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<SearchPluginsDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
