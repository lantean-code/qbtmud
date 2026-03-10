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
        private const string DismissedStorageKey = "PwaInstallPrompt.Dismissed.v1";
        private const string PromptSnackbarKey = "pwa-install-prompt";
        private const string PromptSnackbarClass = "pwa-install-snackbar";

        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly Mock<IPwaInstallPromptService> _pwaInstallPromptServiceMock;
        private readonly List<SnackbarAddCall> _snackbarAddCalls;
        private readonly List<string> _removedSnackbarKeys;
        private readonly IRenderedComponent<PwaInstallPrompt> _target;

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

            _target = TestContext.Render<PwaInstallPrompt>();
        }

        [Fact]
        public async Task GIVEN_StateCanPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsCenteredInstallSnackbar()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
            var call = _snackbarAddCalls.Single();
            var componentParameters = GetComponentParameters(call);
            var options = BuildSnackbarOptions(call);

            componentParameters[nameof(PwaInstallPromptSnackbarContent.CanPromptInstall)].Should().Be(true);
            componentParameters[nameof(PwaInstallPromptSnackbarContent.ShowIosInstructions)].Should().Be(false);
            call.Severity.Should().Be(Severity.Normal);
            call.Key.Should().Be(PromptSnackbarKey);

            options.RequireInteraction.Should().BeTrue();
            options.ShowCloseIcon.Should().BeFalse();
            options.HideIcon.Should().BeTrue();
            options.SnackbarVariant.Should().Be(Variant.Outlined);
            options.Action.Should().BeNull();
            options.OnClick.Should().BeNull();
            options.CloseButtonClickFunc.Should().BeNull();
            options.SnackbarTypeClass.Should().Be(PromptSnackbarClass);
        }

        [Fact]
        public async Task GIVEN_StateIsIosWithoutPrompt_WHEN_OnInstallPromptStateChanged_THEN_ShowsInstructionSnackbarWithoutInstallAction()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
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
            options.SnackbarTypeClass.Should().Be(PromptSnackbarClass);
        }

        [Fact]
        public async Task GIVEN_StateChangeReceivesNull_WHEN_OnInstallPromptStateChanged_THEN_RemovesPromptSnackbar()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(null!));

            _snackbarAddCalls.Should().BeEmpty();
            _removedSnackbarKeys.Should().Contain(PromptSnackbarKey);
        }

        [Fact]
        public async Task GIVEN_SnackbarAlreadyVisibleInSameMode_WHEN_StateUpdates_THEN_DoesNotDuplicateSnackbar()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_SnackbarVisible_WHEN_StateChangesMode_THEN_RecreatesSnackbar()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                IsIos = true
            }));

            _snackbarAddCalls.Count.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_InstallActionClicked_WHEN_PromptAccepted_THEN_HidesForSession()
        {
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("accepted");

            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            await _target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            _snackbarAddCalls.Should().ContainSingle();
            _removedSnackbarKeys.Should().Contain(PromptSnackbarKey);
        }

        [Fact]
        public async Task GIVEN_InstallActionClicked_WHEN_PromptDismissed_THEN_ShowsPromptAgain()
        {
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("dismissed");

            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            await _target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            _snackbarAddCalls.Count.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_InstallRequestInProgress_WHEN_ActionClickedTwice_THEN_SecondPromptRequestIsIgnored()
        {
            var requestInstallPromptTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pwaInstallPromptServiceMock.ClearInvocations();
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .Returns(requestInstallPromptTaskSource.Task);

            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            var onInstallClicked = GetOnInstallClicked(_snackbarAddCalls.Single());
            var firstClickTask = _target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            _target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            var secondClickTask = _target.InvokeAsync(() => onInstallClicked.InvokeAsync(new MouseEventArgs()));
            _target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
            });

            requestInstallPromptTaskSource.SetResult("dismissed");
            await Task.WhenAll(firstClickTask, secondClickTask);
        }

        [Fact]
        public async Task GIVEN_DismissButtonClicked_WHEN_DismissHandlerInvoked_THEN_DismissesForever()
        {
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            var onDismissClicked = GetOnDismissClicked(_snackbarAddCalls.Single());
            await _target.InvokeAsync(() => onDismissClicked.InvokeAsync(new MouseEventArgs()));
            await _target.InvokeAsync(() => _target.Instance.OnInstallPromptStateChanged(new PwaInstallPromptState
            {
                CanPrompt = true
            }));

            _snackbarAddCalls.Should().ContainSingle();
            TestContext.LocalStorage.Snapshot().Should().ContainKey(DismissedStorageKey);
            TestContext.LocalStorage.Snapshot()[DismissedStorageKey].Should().Be(true);
        }

        [Fact]
        public async Task GIVEN_DismissalPersistedBeforeRender_WHEN_ComponentRenders_THEN_SubscriptionIsSkipped()
        {
            await TestContext.LocalStorage.SetItemAsync(DismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();

            TestContext.Render<PwaInstallPrompt>();

            _pwaInstallPromptServiceMock.Verify(
                service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_ComponentRerendered_WHEN_NotFirstRender_THEN_SubscribeIsNotRepeated()
        {
            _pwaInstallPromptServiceMock.ClearInvocations();
            _target.Render();

            _pwaInstallPromptServiceMock.Verify(
                service => service.SubscribeInstallPromptStateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SubscriptionExists_WHEN_Disposed_THEN_UnsubscribeInvoked()
        {
            _pwaInstallPromptServiceMock.ClearInvocations();

            await _target.InvokeAsync(() => _target.Instance.DisposeAsync().AsTask());

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
            await TestContext.LocalStorage.SetItemAsync(DismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            _pwaInstallPromptServiceMock.ClearInvocations();
            var target = TestContext.Render<PwaInstallPrompt>();

            await target.InvokeAsync(() => target.Instance.DisposeAsync().AsTask());

            _pwaInstallPromptServiceMock.Verify(
                service => service.UnsubscribeInstallPromptStateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Never);
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
