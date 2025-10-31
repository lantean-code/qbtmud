using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Net;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class ComponentTestContext : TestContext
    {
        private const string ApiClientName = "API";

        private static readonly Uri _baseAddress = new Uri("http://localhost:8080");
        private readonly TestHttpMessageHandler _httpHandler;

        public ComponentTestContext()
        {
            _httpHandler = new TestHttpMessageHandler();

            // Keep JS interop permissive while bootstrapping tests.
            JSInterop.Mode = JSRuntimeMode.Loose;

            // Logging/options
            Services.AddLogging(b =>
            {
                b.ClearProviders();
                b.SetMinimumLevel(LogLevel.Debug);
            });
            Services.AddOptions();

            // Core UI services
            Services.AddMudServices();
            MudGlobal.InputDefaults.ShrinkLabel = true;

            // Local storage (uses JSRuntime; Loose mode avoids brittle setup)
            Services.AddBlazoredLocalStorage();

            // Message handlers used by your HttpClient pipeline
            Services.AddTransient<CookieHandler>();
            Services.AddScoped<HttpLogger>();

            // Named HttpClient "API" like in Program.cs, but with an in-memory handler
            Services
                .AddScoped(sp => sp
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(ApiClientName))
                .AddHttpClient(ApiClientName, client =>
                {
                    client.BaseAddress = new Uri(_baseAddress, "/api/v2/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => _httpHandler)
                .AddHttpMessageHandler<CookieHandler>()
                .RemoveAllLoggers()
                .AddLogger<HttpLogger>(wrapHandlersPipeline: true);

            // App services
            Services.AddScoped<ApiClient>();
            Services.AddScoped<IApiClient, ApiClient>();
            Services.AddScoped<IDialogWorkflow, DialogWorkflow>();

            Services.AddSingleton<ITorrentDataManager, TorrentDataManager>();
            Services.AddSingleton<IPeerDataManager, PeerDataManager>();
            Services.AddSingleton<IPreferencesDataManager, PreferencesDataManager>();
            Services.AddSingleton<IRssDataManager, RssDataManager>();

            Services.AddSingleton<IClipboardService, ClipboardService>();
            Services.AddTransient<IKeyboardService, KeyboardService>();
        }

        public FakeNavigationManager Nav
        {
            get
            {
                return Services.GetRequiredService<FakeNavigationManager>();
            }
        }

        /// <summary>
        /// Replace IApiClient with a Moq mock (Strict by default). Returns the mock so you can set expectations.
        /// </summary>
        public Mock<IApiClient> UseApiClientMock(MockBehavior behavior = MockBehavior.Strict)
        {
            RemoveServiceDescriptor<IApiClient>();
            var mock = new Mock<IApiClient>(behavior);
            Services.AddSingleton(mock.Object);
            return mock;
        }

        /// <summary>
        /// Add any singleton instance (helpful for substituting stubs).
        /// </summary>
        public T AddSingleton<T>(T instance) where T : class
        {
            RemoveServiceDescriptor<T>();
            Services.AddSingleton(instance);
            return instance;
        }

        /// <summary>
        /// Add a Moq singleton for any service type (loose/strict).
        /// </summary>
        public Mock<T> AddSingletonMock<T>(MockBehavior behavior = MockBehavior.Strict) where T : class
        {
            RemoveServiceDescriptor<T>();
            var mock = new Mock<T>(behavior);
            Services.AddSingleton(mock.Object);
            return mock;
        }

        /// <summary>
        /// Configure the in-memory HTTP pipeline used by the named "API" HttpClient.
        /// Use this if you want to exercise ApiClient instead of mocking IApiClient.
        /// </summary>
        public void WhenHttp(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _httpHandler.SetResponder(responder);
        }

        /// <summary>
        /// Convenience overload for simple synchronous responders.
        /// </summary>
        public void WhenHttp(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _httpHandler.SetResponder((req, _) => Task.FromResult(responder(req)));
        }

        private void RemoveServiceDescriptor<T>()
        {
            for (var i = Services.Count - 1; i >= 0; i--)
            {
                if (Services[i].ServiceType == typeof(T))
                {
                    Services.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// In-memory primary handler for the named HttpClient ("API").
        /// </summary>
        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _responder;

            public void SetResponder(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_responder != null)
                {
                    return _responder(request, cancellationToken);
                }

                var notConfigured = new HttpResponseMessage(HttpStatusCode.NotImplemented)
                {
                    Content = new StringContent("{\"error\":\"Test handler not configured\"}")
                };
                return Task.FromResult(notConfigured);
            }
        }
    }
}