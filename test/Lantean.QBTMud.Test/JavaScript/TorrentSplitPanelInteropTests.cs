using AwesomeAssertions;
using Microsoft.Playwright;

namespace Lantean.QBTMud.Test.JavaScript
{
    public sealed class TorrentSplitPanelInteropTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(90)]
        [InlineData(100)]
        public async Task GIVEN_CustomRatio_WHEN_Applied_THEN_UsesFullResizableRangeAndKeepsHeaders(int ratio)
        {
            await using var target = await CreateTargetAsync();

            await target.Page.EvaluateAsync("ratio => window.splitModule.setRatio(window.panel, ratio)", ratio);

            var sizes = await target.Page.EvaluateAsync<double[]>("""
                () => [
                    window.panel.querySelector('.torrent-split__details').getBoundingClientRect().height,
                    window.panel.querySelector('.torrent-split__list').getBoundingClientRect().height
                ]
                """);
            sizes[0].Should().BeApproximately(56 + (682 * ratio / 100.0), 1);
            sizes[0].Should().BeGreaterThanOrEqualTo(56);
            sizes[1].Should().BeGreaterThanOrEqualTo(56);
            (await target.Page.GetByRole(AriaRole.Separator).GetAttributeAsync("aria-valuenow")).Should().Be(ratio.ToString());
        }

        [Theory]
        [InlineData(true, 100)]
        [InlineData(false, 0)]
        public async Task GIVEN_MouseResize_WHEN_DraggedBeyondOldLimits_THEN_PersistsExtremeRatio(bool increase, int expected)
        {
            await using var target = await CreateTargetAsync();
            var grip = target.Page.GetByRole(AriaRole.Separator);
            var bounds = await grip.BoundingBoxAsync();
            bounds.Should().NotBeNull();
            var x = bounds!.X + bounds.Width / 2;
            var y = bounds.Y + bounds.Height / 2;
            await target.Page.Mouse.MoveAsync(x, y);
            await target.Page.Mouse.DownAsync();
            await target.Page.Mouse.MoveAsync(x, y + (increase ? -420 : 285));
            await target.Page.Mouse.UpAsync();
            await target.Page.EvaluateAsync("() => window.panel.pending");

            (await grip.GetAttributeAsync("aria-valuenow")).Should().Be(expected.ToString());
            (await target.Page.EvaluateAsync<int[]>("() => window.calls.filter(call => call.method === 'ChangeRatio').map(call => call.value)")).Should().Equal(expected);
        }

        [Fact]
        public async Task GIVEN_ResizeKeyboard_WHEN_ExtremesAndArrowsUsed_THEN_SupportsEntireRange()
        {
            await using var target = await CreateTargetAsync();
            var grip = target.Page.GetByRole(AriaRole.Separator);
            await grip.FocusAsync();

            await target.Page.Keyboard.PressAsync("End");
            await target.Page.Keyboard.PressAsync("ArrowUp");
            await target.Page.Keyboard.PressAsync("ArrowDown");
            await target.Page.Keyboard.PressAsync("Home");
            await target.Page.Keyboard.PressAsync("ArrowDown");
            await target.Page.Keyboard.PressAsync("ArrowUp");
            await target.Page.EvaluateAsync("() => window.panel.pending");

            (await target.Page.EvaluateAsync<int[]>("() => window.calls.filter(call => call.method === 'ChangeRatio').map(call => call.value)"))
                .Should().Equal(100, 100, 95, 0, 0, 5);
        }

        private static async Task<JavaScriptTestPage> CreateTargetAsync()
        {
            var target = await JavaScriptTestPage.CreateAsync("torrent-split-panel.module.js");
            try
            {
                await target.Page.SetViewportSizeAsync(1440, 1000);
                await target.Page.EvaluateAsync("""
                    async () => {
                        window.splitModule = await import('/js/torrent-split-panel.module.js');
                        window.calls = [];
                        const panel = document.createElement('qbt-torrent-split');
                        panel.style.cssText = 'display:flex;flex-direction:column;width:900px;height:800px';
                        panel.innerHTML = `
                            <div class="torrent-split__list" style="flex:1;min-height:56px;overflow:hidden">List header</div>
                            <div class="torrent-split__divider" style="flex:0 0 6px;display:flex;min-height:0">
                                <button class="torrent-split__grip" role="separator" aria-label="Resize" style="width:100%;height:100%;min-height:0;padding:0;border:0;touch-action:none" tabindex="0"><span></span></button>
                            </div>
                            <div class="torrent-split__details" style="flex:0 0 var(--torrent-details-height);min-height:56px;overflow:hidden">Details header</div>`;
                        document.body.append(panel);
                        window.panel = panel;
                        window.splitModule.initialize(panel, {
                            invokeMethodAsync(method, value) {
                                window.calls.push({method, value});
                                return Promise.resolve();
                            }
                        }, 40);
                        await panel.pending;
                    }
                    """);
                return target;
            }
            catch
            {
                await target.DisposeAsync();
                throw;
            }
        }
    }
}
