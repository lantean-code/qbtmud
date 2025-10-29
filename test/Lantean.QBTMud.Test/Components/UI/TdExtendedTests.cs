using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TdExtendedTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public TdExtendedTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public async Task GIVEN_LongPressHandler_WHEN_LongPressRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var cut = _target.RenderComponent<TdExtended>(parameters =>
            {
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<LongPressEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await cut.Find("td").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ContextMenuHandler_WHEN_ContextMenuRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var cut = _target.RenderComponent<TdExtended>(parameters =>
            {
                parameters.Add(p => p.OnContextMenu, EventCallback.Factory.Create<MouseEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await cut.Find("td").TriggerEventAsync("oncontextmenu", new MouseEventArgs());

            invoked.Should().BeTrue();
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
