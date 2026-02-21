using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class AddTorrentLinkDialogTests : RazorComponentTestBase<AddTorrentLinkDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly AddTorrentLinkDialogTestDriver _target;

        public AddTorrentLinkDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new AddTorrentLinkDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoUrls_WHEN_SubmitInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkClose");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_UrlParameter_WHEN_Rendered_THEN_UrlsSet()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync("Url");

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Instance.GetState(x => x.Value).Should().Be("Url");
        }

        [Fact]
        public async Task GIVEN_UrlsEntered_WHEN_SubmitInvoked_THEN_ResultContainsUrls()
        {
            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Find("textarea").Change("Url\nSecondUrl");

            var submitButton = FindComponentByTestId<MudButton>(dialog.Component, "AddTorrentLinkSubmit");
            await submitButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentLinkOptions)result.Data!;
            options.Urls.Should().BeEquivalentTo(new[] { "Url", "SecondUrl" });
        }

        [Fact]
        public async Task GIVEN_UrlsEntered_WHEN_KeyboardSubmitInvoked_THEN_ResultContainsUrls()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter"), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            UseApiClientMock();
            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            var urlsField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "AddTorrentLinkUrls");
            urlsField.Find("textarea").Change("Url");

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var options = (AddTorrentLinkOptions)result.Data!;
            options.Urls.Should().ContainSingle().Which.Should().Be("Url");
        }

        private Mock<IApiClient> UseApiClientMock()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            apiClientMock.Setup(c => c.GetAllCategories()).ReturnsAsync(new Dictionary<string, ClientModels.Category>());
            apiClientMock.Setup(c => c.GetAllTags()).ReturnsAsync(Array.Empty<string>());
            apiClientMock.Setup(c => c.GetApplicationPreferences()).ReturnsAsync(CreatePreferences());
            return apiClientMock;
        }

        private static ClientModels.Preferences CreatePreferences()
        {
            var json = "{\"auto_tmm_enabled\":false,\"save_path\":\"\",\"temp_path\":\"\",\"temp_path_enabled\":false,\"add_stopped_enabled\":false,\"add_to_top_of_queue\":true,\"torrent_stop_condition\":\"None\",\"torrent_content_layout\":\"Original\",\"max_ratio_enabled\":false,\"max_ratio\":1.0,\"max_seeding_time_enabled\":false,\"max_seeding_time\":0,\"max_inactive_seeding_time_enabled\":false,\"max_inactive_seeding_time\":0,\"max_ratio_act\":0}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }

    internal sealed class AddTorrentLinkDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public AddTorrentLinkDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<AddTorrentLinkDialogRenderContext> RenderDialogAsync(string? url = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();
            if (url is not null)
            {
                parameters.Add(nameof(AddTorrentLinkDialog.Url), url);
            }

            var reference = await dialogService.ShowAsync<AddTorrentLinkDialog>("Upload torrent file", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<AddTorrentLinkDialog>();

            return new AddTorrentLinkDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class AddTorrentLinkDialogRenderContext
    {
        public AddTorrentLinkDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<AddTorrentLinkDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<AddTorrentLinkDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
