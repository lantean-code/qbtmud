using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class PathAutocompleteTests : RazorComponentTestBase<PathAutocomplete>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly PathAutocompleteTestDriver _target;

        public PathAutocompleteTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.AddSingleton(_apiClient);
            TestContext.AddSingleton(_dialogWorkflow);

            _target = new PathAutocompleteTestDriver(TestContext);
        }

        [Fact]
        public void GIVEN_RequiredNotTouched_WHEN_Rendered_THEN_NoErrorAndAdornmentVisible()
        {
            var component = _target.RenderComponent(required: true);

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            autocomplete.Instance.Error.Should().BeFalse();
            autocomplete.Instance.Adornment.Should().Be(Adornment.End);
            autocomplete.Instance.AdornmentIcon.Should().Be(Icons.Material.Filled.FolderOpen);
        }

        [Fact]
        public async Task GIVEN_BlurredRequiredEmpty_WHEN_BlurInvoked_THEN_ErrorShownAndBlurEventRaised()
        {
            var wasBlurred = false;
            var component = _target.RenderComponent(
                required: true,
                onBlur: EventCallback.Factory.Create<FocusEventArgs>(this, _ => wasBlurred = true));

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            await component.InvokeAsync(() => autocomplete.Instance.OnBlur.InvokeAsync(new FocusEventArgs()));

            autocomplete.Instance.Error.Should().BeTrue();
            wasBlurred.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_BrowseButtonHidden_WHEN_Rendered_THEN_AdornmentNone()
        {
            var component = _target.RenderComponent(showBrowseButton: false);

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            autocomplete.Instance.Adornment.Should().Be(Adornment.None);
            autocomplete.Instance.AdornmentIcon.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_Disabled_WHEN_AdornmentClicked_THEN_DialogNotOpened()
        {
            var component = _target.RenderComponent(disabled: true);

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            await component.InvokeAsync(() => autocomplete.Instance.OnAdornmentClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowPathBrowserDialog(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<DirectoryContentMode>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_DialogReturnsPath_WHEN_AdornmentClicked_THEN_ValueChangedRaised()
        {
            string? received = null;
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true))
                .ReturnsAsync("Selected");

            var component = _target.RenderComponent(
                browseDialogTitle: "Pick",
                mode: DirectoryContentMode.Directories,
                valueChanged: EventCallback.Factory.Create<string?>(this, value => received = value));

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            await component.InvokeAsync(() => autocomplete.Instance.OnAdornmentClick.InvokeAsync());

            received.Should().Be("Selected");
        }

        [Fact]
        public async Task GIVEN_DialogReturnsEmpty_WHEN_AdornmentClicked_THEN_ValueNotChanged()
        {
            string? received = null;
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowPathBrowserDialog("Pick", null, DirectoryContentMode.Directories, true))
                .ReturnsAsync(string.Empty);

            var component = _target.RenderComponent(
                browseDialogTitle: "Pick",
                mode: DirectoryContentMode.Directories,
                valueChanged: EventCallback.Factory.Create<string?>(this, value => received = value));

            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            await component.InvokeAsync(() => autocomplete.Instance.OnAdornmentClick.InvokeAsync());

            received.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CanceledToken_WHEN_SearchInvoked_THEN_ReturnsEmpty()
        {
            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("C:/", cts.Token);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_EmptyInput_WHEN_SearchInvoked_THEN_ReturnsEmpty()
        {
            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke(string.Empty, CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NoParentPath_WHEN_SearchInvoked_THEN_ReturnsEmpty()
        {
            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("Folder", CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ApiThrows_WHEN_SearchInvoked_THEN_ReturnsEmpty()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.All))
                .ThrowsAsync(new HttpRequestException("Failed"));

            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("C:/Test", CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_TrailingSlash_WHEN_SearchInvoked_THEN_ReturnsAllCandidates()
        {
            var candidates = new[] { "C:/Alpha", "C:/Beta" };
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.All))
                .ReturnsAsync(candidates);

            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("C:/", CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEquivalentTo(candidates);
        }

        [Fact]
        public async Task GIVEN_Prefix_WHEN_SearchInvoked_THEN_FiltersByTailSegment()
        {
            var candidates = new[] { "C:/Alpha", "C:/beta", "C:/other", "C:/Alpha/" };
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.All))
                .ReturnsAsync(candidates);

            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("C:/Al", CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEquivalentTo(new[] { "C:/Alpha", "C:/Alpha/" });
        }

        [Fact]
        public async Task GIVEN_CandidateWhitespace_WHEN_SearchInvoked_THEN_HandlesEmptyTailSegment()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.All))
                .ReturnsAsync(new[] { " ", "C:/Alpha" });

            var component = _target.RenderComponent();
            var autocomplete = component.FindComponent<MudAutocomplete<string>>();

            Func<string?, CancellationToken, Task<IEnumerable<string>>?> searchFunc = autocomplete.Instance.SearchFunc ?? throw new InvalidOperationException();
            var task = searchFunc.Invoke("C:/A", CancellationToken.None);
            task.Should().NotBeNull();
            var results = await task!;
            var items = results ?? Array.Empty<string>();

            items.Should().BeEquivalentTo(new[] { "C:/Alpha" });
        }

        private sealed class PathAutocompleteTestDriver
        {
            private readonly ComponentTestContext _testContext;

            public PathAutocompleteTestDriver(ComponentTestContext testContext)
            {
                _testContext = testContext;
            }

            public IRenderedComponent<PathAutocomplete> RenderComponent(
                bool required = false,
                bool disabled = false,
                bool showBrowseButton = true,
                string? browseDialogTitle = null,
                DirectoryContentMode mode = DirectoryContentMode.All,
                EventCallback<string?> valueChanged = default,
                EventCallback<FocusEventArgs> onBlur = default)
            {
                return _testContext.Render<PathAutocomplete>(parameters =>
                {
                    parameters.Add(p => p.Required, required);
                    parameters.Add(p => p.Disabled, disabled);
                    parameters.Add(p => p.ShowBrowseButton, showBrowseButton);
                    parameters.Add(p => p.Mode, mode);

                    if (valueChanged.HasDelegate)
                    {
                        parameters.Add(p => p.ValueChanged, valueChanged);
                    }

                    if (!string.IsNullOrWhiteSpace(browseDialogTitle))
                    {
                        parameters.Add(p => p.BrowseDialogTitle, browseDialogTitle);
                    }

                    if (onBlur.HasDelegate)
                    {
                        parameters.Add(p => p.OnBlur, onBlur);
                    }
                });
            }
        }
    }
}
