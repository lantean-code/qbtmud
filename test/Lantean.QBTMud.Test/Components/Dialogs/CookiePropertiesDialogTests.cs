using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class CookiePropertiesDialogTests : RazorComponentTestBase<CookiePropertiesDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly CookiePropertiesDialogTestDriver _target;

        public CookiePropertiesDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new CookiePropertiesDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_CookieProvided_WHEN_Rendered_THEN_InitialValuesAreShown()
        {
            var expiration = new DateTimeOffset(2020, 1, 2, 3, 4, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            var cookie = new ApplicationCookie("Name", "Domain", "/Path", "Value", expiration);

            var dialog = await _target.RenderDialogAsync(cookie);

            var domain = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesDomain");
            var path = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesPath");
            var name = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesName");
            var value = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesValue");
            var expirationField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesExpiration");

            domain.Instance.Value.Should().Be("Domain");
            path.Instance.Value.Should().Be("/Path");
            name.Instance.Value.Should().Be("Name");
            value.Instance.Value.Should().Be("Value");
            expirationField.Instance.Value.Should().Be(DateTimeOffset.FromUnixTimeSeconds(expiration).LocalDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task GIVEN_ValidValues_WHEN_SaveClicked_THEN_ReturnsNormalizedCookie()
        {
            var dialog = await _target.RenderDialogAsync();

            var domain = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesDomain");
            var path = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesPath");
            var name = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesName");
            var value = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesValue");
            var expirationField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesExpiration");

            await dialog.Component.InvokeAsync(() => domain.Instance.ValueChanged.InvokeAsync(" Domain "));
            await dialog.Component.InvokeAsync(() => path.Instance.ValueChanged.InvokeAsync(" /Path "));
            await dialog.Component.InvokeAsync(() => name.Instance.ValueChanged.InvokeAsync(" Name "));
            await dialog.Component.InvokeAsync(() => value.Instance.ValueChanged.InvokeAsync("Value"));
            await dialog.Component.InvokeAsync(() => expirationField.Instance.ValueChanged.InvokeAsync("2020-01-02T03:04"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CookiePropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var returnedCookie = (ApplicationCookie)result.Data!;

            returnedCookie.Name.Should().Be("Name");
            returnedCookie.Domain.Should().Be("Domain");
            returnedCookie.Path.Should().Be("/Path");
            returnedCookie.Value.Should().Be("Value");

            var expectedDate = DateTime.ParseExact("2020-01-02T03:04", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            var expectedExpiration = new DateTimeOffset(expectedDate, DateTimeOffset.Now.Offset).ToUnixTimeSeconds();
            returnedCookie.ExpirationDate.Should().Be(expectedExpiration);
        }

        [Fact]
        public async Task GIVEN_NameMissing_WHEN_SaveClicked_THEN_ResultDoesNotClose()
        {
            var dialog = await _target.RenderDialogAsync();

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CookiePropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Reference.Result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_InvalidExpiration_WHEN_SaveClicked_THEN_ResultDoesNotCloseAndValidationFails()
        {
            var dialog = await _target.RenderDialogAsync();

            var name = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesName");
            await dialog.Component.InvokeAsync(() => name.Instance.ValueChanged.InvokeAsync("Name"));

            var expirationField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "CookiePropertiesExpiration");
            await dialog.Component.InvokeAsync(() => expirationField.Instance.ValueChanged.InvokeAsync("Invalid"));

            var validation = expirationField.Instance.Validation as Func<string, IEnumerable<string>>;
            validation.Should().NotBeNull();
            validation!("Invalid").Should().ContainSingle("Expiration date must be a valid date and time.");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "CookiePropertiesSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            dialog.Reference.Result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelClicked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "CookiePropertiesCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultOk()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter" && !e.CtrlKey), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(new ApplicationCookie("Name", null, null, null, null));

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().BeOfType<ApplicationCookie>();
        }
    }

    internal sealed class CookiePropertiesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public CookiePropertiesDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<CookiePropertiesDialogRenderContext> RenderDialogAsync(ApplicationCookie? cookie = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (cookie is not null)
            {
                parameters.Add(nameof(CookiePropertiesDialog.Cookie), cookie);
            }

            var reference = await dialogService.ShowAsync<CookiePropertiesDialog>("Cookie properties", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<CookiePropertiesDialog>();

            return new CookiePropertiesDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class CookiePropertiesDialogRenderContext
    {
        public CookiePropertiesDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<CookiePropertiesDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<CookiePropertiesDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
