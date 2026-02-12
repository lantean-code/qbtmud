using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class BreakpointOrientationProviderTests : RazorComponentTestBase
    {
        [Fact]
        public async Task GIVEN_DefaultCascades_WHEN_ViewportChanges_THEN_BreakpointCascadedAndEventRaised()
        {
            var breakpointChanges = new List<Breakpoint>();
            var orientationChanges = new List<Orientation>();
            var setup = SetupViewportService();

            var target = TestContext.Render<BreakpointOrientationProvider>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<CascadingProbe>(0);
                    builder.CloseComponent();
                });
                parameters.Add(p => p.OnBreakpointChanged, EventCallback.Factory.Create<Breakpoint>(this, value => breakpointChanges.Add(value)));
                parameters.Add(p => p.OnOrientationChanged, EventCallback.Factory.Create<Orientation>(this, value => orientationChanges.Add(value)));
            });

            var observer = setup.GetObserver();
            observer.Should().NotBeNull();
            await observer!.NotifyBrowserViewportChangeAsync(CreateArgs(1200, 800, Breakpoint.Lg));

            target.FindComponents<CascadingValue<Breakpoint>>().Should().HaveCount(1);
            target.FindComponents<CascadingValue<Orientation>>().Should().BeEmpty();
            target.Instance.CurrentBreakpoint.Should().Be(Breakpoint.Lg);
            ((IBrowserViewportObserver)target.Instance).ResizeOptions.Should().BeSameAs(setup.DefaultOptions);
            breakpointChanges.Should().ContainSingle().And.Contain(Breakpoint.Lg);
            orientationChanges.Should().BeEmpty();

            await observer.NotifyBrowserViewportChangeAsync(CreateArgs(1200, 800, Breakpoint.Lg));
            breakpointChanges.Should().ContainSingle();
            await ((IAsyncDisposable)target.Instance).DisposeAsync();
            setup.Mock.Verify(v => v.UnsubscribeAsync(observer), Times.Once);
        }

        [Fact]
        public async Task GIVEN_OrientationOnly_WHEN_ViewportChanges_THEN_OrientationCascadedAndBreakpointIgnored()
        {
            var breakpointChanges = new List<Breakpoint>();
            var orientationChanges = new List<Orientation>();
            var setup = SetupViewportService();

            var target = TestContext.Render<BreakpointOrientationProvider>(parameters =>
            {
                parameters.Add(p => p.Cascades, BreakpointOrientationProviderCascades.Orientation);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<CascadingProbe>(0);
                    builder.CloseComponent();
                });
                parameters.Add(p => p.OnBreakpointChanged, EventCallback.Factory.Create<Breakpoint>(this, value => breakpointChanges.Add(value)));
                parameters.Add(p => p.OnOrientationChanged, EventCallback.Factory.Create<Orientation>(this, value => orientationChanges.Add(value)));
            });

            var observer = setup.GetObserver();
            observer.Should().NotBeNull();
            await observer!.NotifyBrowserViewportChangeAsync(CreateArgs(600, 900, Breakpoint.Md));

            target.FindComponents<CascadingValue<Breakpoint>>().Should().BeEmpty();
            target.FindComponents<CascadingValue<Orientation>>().Should().HaveCount(1);
            target.Instance.CurrentOrientation.Should().Be(Orientation.Portrait);
            breakpointChanges.Should().BeEmpty();
            orientationChanges.Should().ContainSingle().And.Contain(Orientation.Portrait);

            await observer.NotifyBrowserViewportChangeAsync(CreateArgs(600, 900, Breakpoint.Md));
            orientationChanges.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_BothCascades_WHEN_ViewportChanges_THEN_CallbacksRaisedForBreakpointAndOrientation()
        {
            var breakpointChanges = new List<Breakpoint>();
            var orientationChanges = new List<Orientation>();
            var setup = SetupViewportService();

            var target = TestContext.Render<BreakpointOrientationProvider>(parameters =>
            {
                parameters.Add(p => p.Cascades, BreakpointOrientationProviderCascades.Both);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<CascadingProbe>(0);
                    builder.CloseComponent();
                });
                parameters.Add(p => p.OnBreakpointChanged, EventCallback.Factory.Create<Breakpoint>(this, value => breakpointChanges.Add(value)));
                parameters.Add(p => p.OnOrientationChanged, EventCallback.Factory.Create<Orientation>(this, value => orientationChanges.Add(value)));
            });

            var observer = setup.GetObserver();
            observer.Should().NotBeNull();
            await observer!.NotifyBrowserViewportChangeAsync(CreateArgs(1400, 600, Breakpoint.Xl));

            target.FindComponents<CascadingValue<Breakpoint>>().Should().HaveCount(1);
            target.FindComponents<CascadingValue<Orientation>>().Should().HaveCount(1);
            target.Instance.CurrentBreakpoint.Should().Be(Breakpoint.Xl);
            target.Instance.CurrentOrientation.Should().Be(Orientation.Landscape);
            breakpointChanges.Should().ContainSingle().And.Contain(Breakpoint.Xl);
            orientationChanges.Should().ContainSingle().And.Contain(Orientation.Landscape);

            await observer!.NotifyBrowserViewportChangeAsync(CreateArgs(1400, 600, Breakpoint.Xl));
            breakpointChanges.Should().ContainSingle();
            orientationChanges.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_CustomResizeOptions_WHEN_Observed_THEN_UsesProvidedOptions()
        {
            var setup = SetupViewportService();
            var customOptions = new ResizeOptions
            {
                ReportRate = 42
            };

            var target = TestContext.Render<BreakpointOrientationProvider>(parameters =>
            {
                parameters.Add(p => p.Options, customOptions);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<CascadingProbe>(0);
                    builder.CloseComponent();
                });
            });

            var resizeOptions = ((IBrowserViewportObserver)target.Instance).ResizeOptions;
            resizeOptions.Should().BeSameAs(customOptions);
            ((IBrowserViewportObserver)target.Instance).Id.Should().NotBe(Guid.Empty);

            await ((IAsyncDisposable)target.Instance).DisposeAsync();
            var observer = setup.GetObserver();
            observer.Should().NotBeNull();
            setup.Mock.Verify(v => v.UnsubscribeAsync(observer), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoCascades_WHEN_ViewportChanges_THEN_NoCallbacksOrCascades()
        {
            var breakpointChanges = new List<Breakpoint>();
            var orientationChanges = new List<Orientation>();
            var setup = SetupViewportService();

            var target = TestContext.Render<BreakpointOrientationProvider>(parameters =>
            {
                parameters.Add(p => p.Cascades, (BreakpointOrientationProviderCascades)0);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<CascadingProbe>(0);
                    builder.CloseComponent();
                });
                parameters.Add(p => p.OnBreakpointChanged, EventCallback.Factory.Create<Breakpoint>(this, value => breakpointChanges.Add(value)));
                parameters.Add(p => p.OnOrientationChanged, EventCallback.Factory.Create<Orientation>(this, value => orientationChanges.Add(value)));
            });

            var observer = setup.GetObserver();
            observer.Should().NotBeNull();
            await observer!.NotifyBrowserViewportChangeAsync(CreateArgs(500, 300, Breakpoint.Sm));

            target.FindComponents<CascadingValue<Breakpoint>>().Should().BeEmpty();
            target.FindComponents<CascadingValue<Orientation>>().Should().BeEmpty();
            breakpointChanges.Should().BeEmpty();
            orientationChanges.Should().BeEmpty();
        }

        private (Mock<IBrowserViewportService> Mock, ResizeOptions DefaultOptions, Func<IBrowserViewportObserver?> GetObserver) SetupViewportService()
        {
            var viewportMock = new Mock<IBrowserViewportService>(MockBehavior.Strict);
            var defaultOptions = new ResizeOptions();
            IBrowserViewportObserver? observer = null;

            viewportMock.SetupGet(v => v.ResizeOptions).Returns(defaultOptions);
            viewportMock.Setup(v => v.SubscribeAsync(It.IsAny<IBrowserViewportObserver>(), It.IsAny<bool>()))
                .Callback<IBrowserViewportObserver, bool>((obs, _) => observer = obs)
                .Returns(Task.CompletedTask);
            viewportMock.Setup(v => v.UnsubscribeAsync(It.IsAny<IBrowserViewportObserver>()))
                .Returns(Task.CompletedTask);

            TestContext.Services.RemoveAll<IBrowserViewportService>();
            TestContext.Services.AddSingleton(viewportMock.Object);

            return (viewportMock, defaultOptions, () => observer);
        }

        private static BrowserViewportEventArgs CreateArgs(double width, double height, Breakpoint breakpoint)
        {
            var size = new BrowserWindowSize
            {
                Width = (int)width,
                Height = (int)height
            };
            return new BrowserViewportEventArgs(Guid.NewGuid(), size, breakpoint, false);
        }

        private sealed class CascadingProbe : ComponentBase
        {
            [CascadingParameter]
            public Breakpoint Breakpoint { get; set; }

            [CascadingParameter]
            public Orientation Orientation { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, $"{Breakpoint}-{Orientation}");
                builder.CloseElement();
            }
        }
    }
}
