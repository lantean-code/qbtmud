using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public abstract class RazorComponentTestBase<T> : RazorComponentTestBase where T : IComponent
    {
    }

    public abstract class RazorComponentTestBase : IAsyncDisposable
    {
        private bool _disposedValue;

        internal ComponentTestContext TestContext { get; private set; }

        protected RazorComponentTestBase()
        {
            TestContext = new ComponentTestContext();
            var webUiLocalizer = Mock.Of<IWebUiLocalizer>();
            var localizerMock = Mock.Get(webUiLocalizer);
            localizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) => FormatLocalizerString(source, arguments));
            TestContext.Services.AddSingleton(webUiLocalizer);

            var languageCatalog = Mock.Of<IWebUiLanguageCatalog>();
            var languageCatalogMock = Mock.Get(languageCatalog);
            languageCatalogMock
                .Setup(catalog => catalog.Languages)
                .Returns(new List<WebUiLanguageCatalogItem> { new("en", "English") });
            languageCatalogMock
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            TestContext.Services.AddSingleton(languageCatalog);
        }

        protected string? GetChildContentText(RenderFragment? fragment)
        {
            if (fragment is null)
            {
                return null;
            }

            var rendered = TestContext.Render(fragment);
            return string.Concat(rendered.Nodes.Select(node => node.TextContent)).Trim();
        }

        protected static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<IComponent> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(component =>
            {
                if (HasTestId(component, testId))
                {
                    return true;
                }

                var selector = $"[data-test-id='{EscapeSelectorValue(testId)}']";
                return component.FindAll(selector).Count > 0;
            });
        }

        protected static IRenderedComponent<MudIconButton> FindIconButton(IRenderedComponent<IComponent> target, string icon)
        {
            return target.FindComponents<MudIconButton>().Single(button => button.Instance.Icon == icon);
        }

        protected static IRenderedComponent<MudButton> FindButton(IRenderedComponent<IComponent> target, string testId)
        {
            return FindComponentByTestId<MudButton>(target, testId);
        }

        protected static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<IComponent> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
        }

        protected static IRenderedComponent<MudSelect<TValue>> FindSelect<TValue>(IRenderedComponent<IComponent> target, string testId)
        {
            return FindComponentByTestId<MudSelect<TValue>>(target, testId);
        }

        protected static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<IComponent> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        protected static IRenderedComponent<MudNumericField<int>> FindNumericField(IRenderedComponent<IComponent> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        protected static bool HasTestId<TComponent>(IRenderedComponent<TComponent> component, string testId)
            where TComponent : IComponent
        {
            var selector = $"[data-test-id='{EscapeSelectorValue(testId)}']";
            return component.FindAll(selector).Count > 0;
        }

        private static string EscapeSelectorValue(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        private static string FormatLocalizerString(string source, object[] arguments)
        {
            if (arguments is null || arguments.Length == 0)
            {
                return source;
            }

            var result = source;
            for (var i = 0; i < arguments.Length; i++)
            {
                var token = $"%{i + 1}";
                var value = arguments[i]?.ToString() ?? string.Empty;
                result = result.Replace(token, value);
            }

            return result;
        }

        protected virtual ValueTask Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    TestContext.Dispose();
                }

                _disposedValue = true;
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
