using AwesomeAssertions;
using Blazor.BrowserCapabilities;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TooltipTests : RazorComponentTestBase<Tooltip>
    {
        private readonly IBrowserCapabilitiesService _browserCapabilitiesService;

        public TooltipTests()
        {
            _browserCapabilitiesService = Mock.Of<IBrowserCapabilitiesService>();
            TestContext.Services.RemoveAll<IBrowserCapabilitiesService>();
            TestContext.Services.AddSingleton(_browserCapabilitiesService);
        }

        [Fact]
        public void GIVEN_DefaultSettings_WHEN_HoverPointerUnavailable_THEN_UsesTouchClickTrigger()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DefaultSettings_WHEN_HoverPointerAvailable_THEN_UsesDesktopTriggers()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: true);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            target.WaitForAssertion(() =>
            {
                var tooltip = target.FindComponent<MudTooltip>();
                tooltip.Instance.ShowOnHover.Should().BeTrue();
                tooltip.Instance.ShowOnFocus.Should().BeTrue();
                tooltip.Instance.ShowOnClick.Should().BeFalse();
            });
        }

        [Fact]
        public void GIVEN_ConfiguredDesktopTriggers_WHEN_HoverPointerUnavailable_THEN_UsesInternalTouchTriggers()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.ShowOnHover, true);
                parameters.Add(p => p.ShowOnFocus, false);
                parameters.Add(p => p.ShowOnClick, false);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DesktopBehaviorConfigured_WHEN_HoverPointerAvailable_THEN_UsesConfiguredDesktopTriggers()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: true);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.ShowOnHover, false);
                parameters.Add(p => p.ShowOnFocus, false);
                parameters.Add(p => p.ShowOnClick, true);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_CapabilitiesNotInitialized_WHEN_Rendered_THEN_FallsBackToTouchClickBehavior()
        {
            ConfigureBrowserCapabilities(isInitialized: false, supportsHoverPointer: false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TouchClickTrigger_WHEN_TooltipShown_THEN_AutoHidesAfterDelay()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();
            var root = tooltip.Find(".mud-tooltip-root");

            await root.TriggerEventAsync("onpointerup", new PointerEventArgs());

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeTrue();
            });

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeFalse();
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GIVEN_VisibleChangesWithoutDelegate_WHEN_ParametersUpdated_THEN_VisibleStateTracksParameter()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: true);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.Visible, false);
            });

            var tooltip = target.FindComponent<MudTooltip>();
            tooltip.Instance.GetState(x => x.Visible).Should().BeFalse();

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(Tooltip.Visible), true },
            })));

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeTrue();
            });

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(Tooltip.Visible), true },
            })));

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_VisibleChangedDelegate_WHEN_VisibilityToggled_THEN_DelegateReceivesUpdates()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: true);
            var values = new List<bool>();
            var callback = EventCallback.Factory.Create<bool>(this, (bool value) => values.Add(value));

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.VisibleChanged, callback);
                parameters.Add(p => p.Visible, false);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(true));
            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(false));

            values.Should().BeEquivalentTo(new[] { true, false });
        }

        [Fact]
        public async Task GIVEN_TouchAutoHideScheduled_WHEN_HiddenImmediately_THEN_RemainsHidden()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(true));
            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(false));

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_TouchAutoHideWithDelegate_WHEN_DelayElapses_THEN_InvokesDelegateWithFalse()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);
            var values = new List<bool>();
            var callback = EventCallback.Factory.Create<bool>(this, (bool value) => values.Add(value));

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.VisibleChanged, callback);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(true));

            target.WaitForAssertion(() =>
            {
                values.Should().ContainInOrder(true, false);
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GIVEN_TouchAutoHideScheduled_WHEN_VisibleParameterSetFalse_THEN_AutoHideReturnsEarly()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: false);
            var values = new List<bool>();
            var callback = EventCallback.Factory.Create<bool>(this, (bool value) => values.Add(value));

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.Visible, false);
                parameters.Add(p => p.VisibleChanged, callback);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(true));
            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(Tooltip.Visible), false },
                { nameof(Tooltip.VisibleChanged), callback },
            })));

            target.WaitForAssertion(() =>
            {
                tooltip.Instance.GetState(x => x.Visible).Should().BeFalse();
            }, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GIVEN_DisposedComponent_WHEN_VisibleChangedInvoked_THEN_DoesNotInvokeParentDelegate()
        {
            ConfigureBrowserCapabilities(isInitialized: true, supportsHoverPointer: true);
            var invokedValues = new List<bool>();
            var callback = EventCallback.Factory.Create<bool>(this, (bool value) => invokedValues.Add(value));

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.VisibleChanged, callback);
            });

            var tooltip = target.FindComponent<MudTooltip>();
            target.Instance.Dispose();
            target.Instance.Dispose();

            await target.InvokeAsync(() => tooltip.Instance.VisibleChanged.InvokeAsync(true));

            invokedValues.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_UnmatchedAttributes_WHEN_Rendered_THEN_ForwardsAttributesToMudTooltip()
        {
            var target = RenderTooltip(parameters =>
            {
                parameters.AddUnmatched("data-test-id", "TooltipComponent");
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            HasTestId(tooltip, "TooltipComponent").Should().BeTrue();
        }

        private IRenderedComponent<Tooltip> RenderTooltip(Action<ComponentParameterCollectionBuilder<Tooltip>> configure)
        {
            return TestContext.Render<Tooltip>(parameters =>
            {
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "button");
                    builder.AddContent(1, "Action");
                    builder.CloseElement();
                }));

                configure(parameters);
            });
        }

        private void ConfigureBrowserCapabilities(bool isInitialized, bool supportsHoverPointer)
        {
            var browserCapabilitiesServiceMock = Mock.Get(_browserCapabilitiesService);
            browserCapabilitiesServiceMock.Setup(service => service.IsInitialized).Returns(isInitialized);
            browserCapabilitiesServiceMock.Setup(service => service.Capabilities).Returns(new BrowserCapabilities(
                SupportsHoverPointer: supportsHoverPointer,
                SupportsHover: supportsHoverPointer,
                SupportsFinePointer: supportsHoverPointer,
                SupportsCoarsePointer: !supportsHoverPointer,
                SupportsPointerEvents: true,
                HasTouchInput: !supportsHoverPointer,
                MaxTouchPoints: supportsHoverPointer ? 0 : 5,
                PrefersReducedMotion: false,
                PrefersReducedData: false,
                PrefersDarkColorScheme: false,
                ForcedColorsActive: false,
                PrefersHighContrast: false,
                SupportsClipboardRead: true,
                SupportsClipboardWrite: true,
                SupportsShareApi: true,
                SupportsInstallPrompt: false,
                IsAppleMobilePlatform: !supportsHoverPointer,
                IsStandaloneDisplayMode: false));
        }
    }
}
