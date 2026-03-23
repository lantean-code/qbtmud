using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PwaInstallPromptTests : RazorComponentTestBase<PwaInstallPrompt>
    {
        private const string _dismissedStorageKey = "PwaInstallPrompt.Dismissed.v1";

        private readonly Mock<IPwaInstallPromptService> _pwaInstallPromptServiceMock;
        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        private IRenderedComponent<MudPopoverProvider> PopoverProvider =>
            _popoverProvider ?? throw new InvalidOperationException("MudPopoverProvider has not been rendered.");

        public PwaInstallPromptTests()
        {
            _pwaInstallPromptServiceMock = TestContext.AddSingletonMock<IPwaInstallPromptService>();
            _pwaInstallPromptServiceMock
                .Setup(service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(17);
            _pwaInstallPromptServiceMock
                .Setup(service => service.UnsubscribeInstallPromptStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("dismissed");
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState());
        }

        [Fact]
        public async Task GIVEN_StateCanPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsConfiguredInstallPopover()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var popover = target.FindComponent<MudPopover>();
            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();

            popover.Instance.Open.Should().BeTrue();
            popover.Instance.Fixed.Should().BeTrue();
            popover.Instance.Elevation.Should().Be(12);
            popover.Instance.OverflowBehavior.Should().Be(OverflowBehavior.FlipNever);
            popover.Instance.TransformOrigin.Should().Be(Origin.TopCenter);
            popover.Instance.AnchorOrigin.Should().Be(Origin.BottomCenter);
            popover.Instance.Class.Should().Be("pwa-install-prompt-popover");
            content.Instance.CanPromptInstall.Should().BeTrue();
            content.Instance.ShowIosInstructions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StateIsIosWithoutPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsPopoverWithoutInstallAction()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();

            content.Instance.CanPromptInstall.Should().BeFalse();
            content.Instance.ShowIosInstructions.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_StateChangeReceivesNull_WHEN_OnInstallPromptStateChanged_THEN_ClosesPopover()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(null!));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeFalse();
            PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_PopoverAlreadyVisibleInSameMode_WHEN_StateUpdates_THEN_StaysOpenWithSingleContentInstance()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
            PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_PopoverVisible_WHEN_StateChangesMode_THEN_UpdatesPopoverContent()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));

            target.WaitForAssertion(() =>
            {
                var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
                content.Instance.CanPromptInstall.Should().BeFalse();
                content.Instance.ShowIosInstructions.Should().BeTrue();
            }, timeout: TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task GIVEN_InitialPromptableStateClearsBeforeDelay_WHEN_OnInstallPromptStateChanged_THEN_DoesNotOpenPopover()
        {
            var target = RenderTarget();

            var firstStateChangeTask = target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            await Task.Yield();
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState()));
            await firstStateChangeTask;

            target.FindComponent<MudPopover>().Instance.Open.Should().BeFalse();
            PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ExternalInstallPromptRequestInProgress_WHEN_StateChanges_THEN_DoesNotHidePopoverUntilBusyStateClears()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsPromptInProgress = true
            }));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
            PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_InstallActionClicked_WHEN_PromptAccepted_THEN_HidesForSession()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("accepted");

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
            await target.InvokeAsync(() => content.Instance.OnInstallClicked.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            target.FindComponent<MudPopover>().Instance.Open.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_InstallActionClicked_WHEN_PromptDismissedAndStateStillPromptable_THEN_KeepsPopoverVisibleWithoutClosing()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("dismissed");
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
            await target.InvokeAsync(() => content.Instance.OnInstallClicked.InvokeAsync(new MouseEventArgs()));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            _pwaInstallPromptServiceMock.Verify(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
            PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_InstallRequestInProgress_WHEN_ActionClickedTwice_THEN_SecondPromptRequestIsIgnored()
        {
            var requestInstallPromptTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .Returns(requestInstallPromptTaskSource.Task);

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
            var firstClickTask = target.InvokeAsync(() => content.Instance.OnInstallClicked.InvokeAsync(new MouseEventArgs()));
            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            var secondClickTask = target.InvokeAsync(() => content.Instance.OnInstallClicked.InvokeAsync(new MouseEventArgs()));
            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            requestInstallPromptTaskSource.SetResult("dismissed");
            await Task.WhenAll(firstClickTask, secondClickTask);
        }

        [Fact]
        public async Task GIVEN_InstallRequestInProgress_WHEN_StateChanges_THEN_DoesNotHidePopoverUntilRequestCompletes()
        {
            var requestInstallPromptTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .Returns(requestInstallPromptTaskSource.Task);
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
            var clickTask = target.InvokeAsync(() => content.Instance.OnInstallClicked.InvokeAsync(new MouseEventArgs()));

            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState()));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();

            requestInstallPromptTaskSource.SetResult("dismissed");
            await clickTask;

            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DismissButtonClicked_WHEN_DismissHandlerInvoked_THEN_DismissesForever()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptPopover(target);

            var content = PopoverProvider.FindComponent<PwaInstallPromptSnackbarContent>();
            await target.InvokeAsync(() => content.Instance.OnDismissClicked.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            target.FindComponent<MudPopover>().Instance.Open.Should().BeFalse();
            TestContext.LocalStorage.Snapshot().Should().ContainKey(_dismissedStorageKey);
            TestContext.LocalStorage.Snapshot()[_dismissedStorageKey].Should().Be(true);
        }

        [Fact]
        public async Task GIVEN_DismissalPersistedBeforeRender_WHEN_ComponentRenders_THEN_SubscriptionStillStarts()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();

            TestContext.Render<PwaInstallPrompt>();

            _pwaInstallPromptServiceMock.Verify(
                service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DismissalPersistedBeforeRender_WHEN_DismissalClearedAndStateChanges_THEN_PopoverCanShowAgain()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            var target = RenderTarget();

            await TestContext.LocalStorage.RemoveItemAsync(_dismissedStorageKey, Xunit.TestContext.Current.CancellationToken);
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            WaitForPromptPopover(target);
            target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ComponentRerendered_WHEN_NotFirstRender_THEN_SubscribeIsNotRepeated()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();
            target.Render();

            _pwaInstallPromptServiceMock.Verify(
                service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SubscriptionExists_WHEN_Disposed_THEN_UnsubscribeInvoked()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();

            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            _pwaInstallPromptServiceMock.Verify(
                service => service.UnsubscribeInstallPromptStateAsync(17, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnsubscribeThrowsJsException_WHEN_Disposed_THEN_DisposeContinues()
        {
            _pwaInstallPromptServiceMock
                .Setup(service => service.UnsubscribeInstallPromptStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("Message"));
            var target = TestContext.Render<PwaInstallPrompt>();

            var action = async () => await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            await action.Should().NotThrowAsync();
            _pwaInstallPromptServiceMock.Verify(
                service => service.UnsubscribeInstallPromptStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_DismissalPersistedBeforeRender_WHEN_Disposed_THEN_UnsubscribeStillInvoked()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();
            var target = TestContext.Render<PwaInstallPrompt>();

            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            _pwaInstallPromptServiceMock.Verify(
                service => service.UnsubscribeInstallPromptStateAsync(17, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private IRenderedComponent<PwaInstallPrompt> RenderTarget()
        {
            _popoverProvider ??= TestContext.Render<MudPopoverProvider>();
            return TestContext.Render<PwaInstallPrompt>();
        }

        private void WaitForPromptPopover(IRenderedComponent<PwaInstallPrompt> target)
        {
            target.WaitForAssertion(() =>
            {
                target.FindComponent<MudPopover>().Instance.Open.Should().BeTrue();
                PopoverProvider.FindComponents<PwaInstallPromptSnackbarContent>().Should().ContainSingle();
            }, timeout: TimeSpan.FromSeconds(2));
        }
    }
}
