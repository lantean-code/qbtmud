using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
{
    public sealed class SliderFieldDialogTests : RazorComponentTestBase<SliderFieldDialog<int>>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly SliderFieldDialogTestDriver _target;

        public SliderFieldDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new SliderFieldDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_LabelNull_WHEN_Rendered_THEN_UsesValueOnly()
        {
            var dialog = await _target.RenderDialogAsync(null, 5, value => value.ToString());

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "SliderFieldInput");
            input.Instance.GetState(x => x.Value).Should().Be("5");
        }

        [Fact]
        public async Task GIVEN_LabelAndEmptyDisplay_WHEN_Rendered_THEN_UsesLabelForValue()
        {
            await _target.RenderDialogAsync("RateLabel", 0, _ => " ");
        }

        [Fact]
        public async Task GIVEN_LabelAndDisplay_WHEN_Rendered_THEN_UsesLabelAndValue()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 10, _ => "10");

            GetChildContentText(FindComponentByTestId<MudText>(dialog.Component, "SliderValueLabel").Instance.ChildContent).Should().Be("Rate: 10");
        }

        [Fact]
        public async Task GIVEN_LabelWhitespaceAndNullDisplay_WHEN_Rendered_THEN_UsesValueOnly()
        {
            await _target.RenderDialogAsync(" ", 10, _ => null!);
        }

        [Fact]
        public async Task GIVEN_ValueGetFuncProvided_WHEN_Changed_THEN_UsesValueGetFunc()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 1, null, value => 42);

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "SliderFieldInput");
            input.Find("input").Input("99");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "SliderFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_ValidValue_WHEN_Changed_THEN_ParsesValue()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 1);

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "SliderFieldInput");
            input.Find("input").Input("12");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "SliderFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(12);
        }

        [Fact]
        public async Task GIVEN_InvalidValue_WHEN_Changed_THEN_UsesMin()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 1, null, null, 5);

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "SliderFieldInput");
            input.Find("input").Input("invalid");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "SliderFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(5);
        }

        [Fact]
        public async Task GIVEN_SliderChanged_WHEN_Saved_THEN_UsesSliderValue()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 1);

            var slider = FindComponentByTestId<MudSlider<int>>(dialog.Component, "SliderFieldSlider");
            await dialog.Component.InvokeAsync(() => slider.Instance.ValueChanged.InvokeAsync(15));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "SliderFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(15);
        }

        [Theory]
        [InlineData(null, false, "8", 8192L)]
        [InlineData("", false, "8", 8192L)]
        [InlineData("Invalid", false, "∞", 0L)]
        [InlineData("9223372036854775808", false, "8", 8192L)]
        [InlineData("9223372036854775807", false, "8", 8192L)]
        [InlineData("-1", false, "8", 8192L)]
        [InlineData("Invalid", true, "8", 6144L)]
        public async Task GIVEN_ProductionRateConverter_WHEN_InvalidTextEntered_THEN_ShouldValidateAndAllowCorrection(string? invalidValue, bool correctWithSlider, string correction, long expectedRate)
        {
            var apiClient = new Mock<IApiClient>();
            apiClient.Setup(client => client.SetGlobalDownloadLimitAsync(expectedRate)).ReturnsSuccess(Task.CompletedTask);
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(apiClient.Object);
            Func<KeyboardEvent, Task>? submitHandler = null;
            Mock.Get(_keyboardService)
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(key => key.Key == "Enter" && !key.CtrlKey), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);
            var provider = TestContext.Render<MudDialogProvider>();
            var workflow = TestContext.Services.GetRequiredService<IDialogWorkflow>();
            var rateChange = workflow.InvokeGlobalDownloadRateDialog(1024);
            provider.WaitForAssertion(() => provider.FindComponent<SliderFieldDialog<long>>().Should().NotBeNull());
            var target = provider.FindComponent<SliderFieldDialog<long>>();
            var input = FindComponentByTestId<MudTextField<string>>(target, "SliderFieldInput");

            await target.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(invalidValue));

            input.Instance.GetState(component => component.Error).Should().BeTrue();
            input.Instance.GetState(component => component.ErrorText).Should().Be("Enter a valid rate limit.");
            input.Instance.GetState(component => component.Value).Should().Be(invalidValue ?? string.Empty);
            var save = FindComponentByTestId<MudButton>(target, "SliderFieldSave");
            save.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => save.Instance.OnClick.InvokeAsync());
            target.WaitForAssertion(() => submitHandler.Should().NotBeNull());
            await target.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));
            rateChange.IsCompleted.Should().BeFalse();
            apiClient.Verify(client => client.SetGlobalDownloadLimitAsync(It.IsAny<long>()), Times.Never);

            if (correctWithSlider)
            {
                var slider = FindComponentByTestId<MudSlider<long>>(target, "SliderFieldSlider");
                await target.InvokeAsync(() => slider.Instance.ValueChanged.InvokeAsync(6L));
            }
            else
            {
                await target.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(correction));
            }

            input.Instance.GetState(component => component.Error).Should().BeFalse();
            save.Instance.Disabled.Should().BeFalse();
            await target.InvokeAsync(() => save.Instance.OnClick.InvokeAsync());
            (await rateChange).Should().Be(expectedRate);
            apiClient.Verify(client => client.SetGlobalDownloadLimitAsync(expectedRate), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Rate", 1);

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "SliderFieldCancel");
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

            var dialog = await _target.RenderDialogAsync("Rate", 1);

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }
    }

    internal sealed class SliderFieldDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public SliderFieldDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<SliderFieldDialogRenderContext> RenderDialogAsync(
            string? label,
            int value,
            Func<int, string>? valueDisplayFunc = null,
            Func<string, int>? valueGetFunc = null,
            int? min = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<>.Label), label },
                { nameof(SliderFieldDialog<>.Value), value },
            };

            if (valueDisplayFunc is not null)
            {
                parameters.Add(nameof(SliderFieldDialog<>.ValueDisplayFunc), valueDisplayFunc);
            }

            if (valueGetFunc is not null)
            {
                parameters.Add(nameof(SliderFieldDialog<>.ValueGetFunc), valueGetFunc);
            }

            if (min.HasValue)
            {
                parameters.Add(nameof(SliderFieldDialog<>.Min), min.Value);
            }

            var reference = await dialogService.ShowAsync<SliderFieldDialog<int>>("Slider Field", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<SliderFieldDialog<int>>();

            return new SliderFieldDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class SliderFieldDialogRenderContext
    {
        public SliderFieldDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<SliderFieldDialog<int>> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<SliderFieldDialog<int>> Component { get; }

        public IDialogReference Reference { get; }
    }
}
