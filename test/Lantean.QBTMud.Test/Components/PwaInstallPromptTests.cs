using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PwaInstallPromptTests : RazorComponentTestBase<PwaInstallPrompt>
    {
        private const string _dismissedStorageKey = "PwaInstallPrompt.Dismissed.v1";
        private const string _promptSnackbarKey = "pwa-install-prompt";
        private const string _promptSnackbarClass = "pwa-install-snackbar";

        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly Mock<IPwaInstallPromptService> _pwaInstallPromptServiceMock;
        private readonly List<SnackbarAddCall> _snackbarAddCalls;
        private readonly List<string> _removedSnackbarKeys;

        public PwaInstallPromptTests()
        {
            _snackbarAddCalls = new List<SnackbarAddCall>();
            _removedSnackbarKeys = new List<string>();

            _snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Strict);
            _snackbarMock
                .Setup(snackbar => snackbar.Add<PwaInstallPromptSnackbarContent>(It.IsAny<Dictionary<string, object>?>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()))
                .Callback((Dictionary<string, object>? componentParameters, Severity severity, Action<SnackbarOptions>? configure, string? key) =>
                {
                    _snackbarAddCalls.Add(new SnackbarAddCall(componentParameters, severity, configure, key));
                })
                .Returns((Snackbar?)null);
            _snackbarMock
                .Setup(snackbar => snackbar.RemoveByKey(It.IsAny<string>()))
                .Callback((string key) =>
                {
                    _removedSnackbarKeys.Add(key);
                });
            _snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(Array.Empty<Snackbar>());
            _snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            _snackbarMock.SetupAdd(snackbar => snackbar.OnSnackbarsUpdated += It.IsAny<Action>());
            _snackbarMock.SetupRemove(snackbar => snackbar.OnSnackbarsUpdated -= It.IsAny<Action>());

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
        public async Task GIVEN_StateCanPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsCenteredInstallSnackbar()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptSnackbar(target);

            var call = _snackbarAddCalls.Single();
            var componentParameters = GetComponentParameters(call);
            var options = BuildSnackbarOptions(call);

            componentParameters[nameof(PwaInstallPromptSnackbarContent.CanPromptInstall)].Should().Be(true);
            componentParameters[nameof(PwaInstallPromptSnackbarContent.ShowIosInstructions)].Should().Be(false);
            call.Severity.Should().Be(Severity.Normal);
            call.Key.Should().Be(_promptSnackbarKey);

            options.RequireInteraction.Should().BeTrue();
            options.ShowCloseIcon.Should().BeFalse();
            options.HideIcon.Should().BeTrue();
            options.SnackbarVariant.Should().Be(Variant.Outlined);
            options.Action.Should().BeNull();
            options.OnClick.Should().BeNull();
            options.CloseButtonClickFunc.Should().BeNull();
            options.SnackbarTypeClass.Should().Be(_promptSnackbarClass);
        }

        [Fact]
        public async Task GIVEN_StateIsIosWithoutPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsInstructionSnackbarWithoutInstallAction()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));
            WaitForPromptSnackbar(target);

            var call = _snackbarAddCalls.Single();
            var componentParameters = GetComponentParameters(call);
            var options = BuildSnackbarOptions(call);

            componentParameters[nameof(PwaInstallPromptSnackbarContent.CanPromptInstall)].Should().Be(false);
            componentParameters[nameof(PwaInstallPromptSnackbarContent.ShowIosInstructions)].Should().Be(true);
            call.Severity.Should().Be(Severity.Normal);
            options.SnackbarVariant.Should().Be(Variant.Outlined);
            options.Action.Should().BeNull();
            options.OnClick.Should().BeNull();
            options.CloseButtonClickFunc.Should().BeNull();
            options.SnackbarTypeClass.Should().Be(_promptSnackbarClass);
        }

        [Fact]
        public async Task GIVEN_StateChangeReceivesNull_WHEN_OnInstallPromptStateChanged_THEN_RemovesPromptSnackbar()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(null!));

            _snackbarAddCalls.Should().BeEmpty();
            _removedSnackbarKeys.Should().Contain(_promptSnackbarKey);
        }

        [Fact]
        public async Task GIVEN_SnackbarAlreadyVisibleInSameMode_WHEN_StateUpdates_THEN_DoesNotDuplicateSnackbar()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptSnackbar(target);
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_SnackbarVisible_WHEN_StateChangesMode_THEN_RecreatesSnackbar()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptSnackbar(target);
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));

            target.WaitForAssertion(() => _snackbarAddCalls.Count.Should().Be(2), timeout: TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task GIVEN_InitialPromptableStateClearsBeforeDelay_WHEN_OnInstallPromptStateChanged_THEN_DoesNotShowSnackbar()
        {
            var target = RenderTarget();

            var firstStateChangeTask = target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            await Task.Yield();
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState()));
            await firstStateChangeTask;

            _snackbarAddCalls.Should().BeEmpty();
            _removedSnackbarKeys.Should().Contain(_promptSnackbarKey);
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
            WaitForPromptSnackbar(target);

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            await target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            _snackbarAddCalls.Should().ContainSingle();
            _removedSnackbarKeys.Should().Contain(_promptSnackbarKey);
        }

        [Fact]
        public async Task GIVEN_InstallActionClicked_WHEN_PromptDismissedAndStateStillPromptable_THEN_KeepsPromptVisibleWithoutRecreatingSnackbar()
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
            WaitForPromptSnackbar(target);
            var removeCountBeforeClick = _removedSnackbarKeys.Count;

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            await target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            _pwaInstallPromptServiceMock.Verify(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()), Times.Once);
            _snackbarAddCalls.Should().ContainSingle();
            _removedSnackbarKeys.Count.Should().Be(removeCountBeforeClick);
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
            WaitForPromptSnackbar(target);

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            var firstClickTask = target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            var secondClickTask = target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            requestInstallPromptTaskSource.SetResult("dismissed");
            await Task.WhenAll(firstClickTask, secondClickTask);
        }

        [Fact]
        public async Task GIVEN_InstallRequestInProgress_WHEN_StateChanges_THEN_DoesNotHideOrRecreateSnackbarUntilRequestCompletes()
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
            WaitForPromptSnackbar(target);
            var removeCountBeforeInstall = _removedSnackbarKeys.Count;

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            var clickTask = target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));

            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState()));

            _snackbarAddCalls.Should().ContainSingle();
            _removedSnackbarKeys.Count.Should().Be(removeCountBeforeInstall);

            requestInstallPromptTaskSource.SetResult("dismissed");
            await clickTask;

            _snackbarAddCalls.Should().ContainSingle();
            _removedSnackbarKeys.Count.Should().Be(removeCountBeforeInstall);
        }

        [Fact]
        public async Task GIVEN_DismissButtonClicked_WHEN_DismissHandlerInvoked_THEN_DismissesForever()
        {
            var target = RenderTarget();

            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            WaitForPromptSnackbar(target);

            var onDismissClicked = GetOnDismissClicked(_snackbarAddCalls.Single());
            await target.InvokeAsync(() => onDismissClicked.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
            TestContext.LocalStorage.Snapshot().Should().ContainKey(_dismissedStorageKey);
            TestContext.LocalStorage.Snapshot()[_dismissedStorageKey].Should().Be(true);
        }

        [Fact]
        public async Task GIVEN_DismissalPersistedBeforeRender_WHEN_ComponentRenders_THEN_SubscriptionIsSkipped()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();

            TestContext.Render<PwaInstallPrompt>();

            _pwaInstallPromptServiceMock.Verify(
                service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
        public async Task GIVEN_NoSubscription_WHEN_Disposed_THEN_UnsubscribeNotInvoked()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();
            var target = TestContext.Render<PwaInstallPrompt>();

            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            _pwaInstallPromptServiceMock.Verify(
                service => service.UnsubscribeInstallPromptStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private IRenderedComponent<PwaInstallPrompt> RenderTarget()
        {
            return TestContext.Render<PwaInstallPrompt>();
        }

        private void WaitForPromptSnackbar(IRenderedComponent<PwaInstallPrompt> target)
        {
            target.WaitForAssertion(() => _snackbarAddCalls.Should().ContainSingle(), timeout: TimeSpan.FromSeconds(2));
        }

        private static SnackbarOptions BuildSnackbarOptions(SnackbarAddCall call)
        {
            var options = new SnackbarOptions(call.Severity, new SnackbarConfiguration());
            call.Configure?.Invoke(options);
            return options;
        }

        private static Dictionary<string, object> GetComponentParameters(SnackbarAddCall call)
        {
            call.ComponentParameters.Should().NotBeNull();
            return call.ComponentParameters!;
        }

        private static EventCallback<MouseEventArgs> GetOnInstallClicked(SnackbarAddCall call)
        {
            var componentParameters = GetComponentParameters(call);
            componentParameters[nameof(PwaInstallPromptSnackbarContent.OnInstallClicked)].Should().BeOfType<EventCallback<MouseEventArgs>>();
            return (EventCallback<MouseEventArgs>)componentParameters[nameof(PwaInstallPromptSnackbarContent.OnInstallClicked)];
        }

        private static EventCallback<MouseEventArgs> GetOnDismissClicked(SnackbarAddCall call)
        {
            var componentParameters = GetComponentParameters(call);
            componentParameters[nameof(PwaInstallPromptSnackbarContent.OnDismissClicked)].Should().BeOfType<EventCallback<MouseEventArgs>>();
            return (EventCallback<MouseEventArgs>)componentParameters[nameof(PwaInstallPromptSnackbarContent.OnDismissClicked)];
        }

        private sealed record SnackbarAddCall(Dictionary<string, object>? ComponentParameters, Severity Severity, Action<SnackbarOptions>? Configure, string? Key);
    }
}
