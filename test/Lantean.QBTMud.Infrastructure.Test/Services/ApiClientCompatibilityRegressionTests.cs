using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Infrastructure.Test.Services
{
    public sealed class ApiClientCompatibilityRegressionTests
    {
        [Fact]
        public async Task GIVEN_StartupInitializedNamedApiClient_WHEN_FreshClientDataStorageAdapterStoresEntries_THEN_ShouldSucceedWithoutAdditionalInitialization()
        {
            var apiVersionRequestCount = 0;
            var storeRequestCount = 0;
            using var serviceProvider = CreateServiceProvider(request =>
            {
                if (request.RequestUri?.AbsolutePath == "/app/webapiVersion")
                {
                    apiVersionRequestCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("2.13.1")
                    };
                }

                if (request.RequestUri?.AbsolutePath == "/clientdata/store")
                {
                    storeRequestCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
            });
            using var scope = serviceProvider.CreateScope();
            var startupClient = scope.ServiceProvider.GetRequiredService<IApiClient>();
            var adapter = new ClientDataStorageAdapter(scope.ServiceProvider.GetRequiredService<IApiClient>());

            var initializationResult = await startupClient.InitializeAsync(TestContext.Current.CancellationToken);
            var storeResult = await adapter.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"theme\":\"dark\"}").RootElement.Clone()
                },
                TestContext.Current.CancellationToken);

            initializationResult.IsFailure.Should().BeFalse();
            storeResult.Succeeded.Should().BeTrue();
            storeResult.FailureResult.Should().BeNull();
            apiVersionRequestCount.Should().Be(1);
            storeRequestCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_StartupInitializedNamedApiClient_WHEN_StorageRoutingServiceMigratesSettingsToClientData_THEN_ShouldPersistRoutingWithoutAdditionalInitialization()
        {
            var storeRequestCount = 0;
            using var serviceProvider = CreateServiceProvider(request =>
            {
                if (request.RequestUri?.AbsolutePath == "/app/webapiVersion")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("2.13.1")
                    };
                }

                if (request.RequestUri?.AbsolutePath == "/clientdata/store")
                {
                    storeRequestCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
            });
            using var scope = serviceProvider.CreateScope();
            var startupClient = scope.ServiceProvider.GetRequiredService<IApiClient>();
            var localStorageService = new TestLocalStorageService();
            var clientDataStorageAdapter = new ClientDataStorageAdapter(scope.ServiceProvider.GetRequiredService<IApiClient>());
            var webApiCapabilityService = new WebApiCapabilityService(scope.ServiceProvider.GetRequiredService<IApiClient>());
            var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(Array.Empty<BrowserStorageEntry>());
            var apiFeedbackWorkflow = new Mock<IApiFeedbackWorkflow>(MockBehavior.Strict);
            var target = new StorageRoutingService(
                localStorageService,
                clientDataStorageAdapter,
                webApiCapabilityService,
                new StorageCatalogService(),
                new LocalStorageEntryAdapter(jsRuntime.Object),
                apiFeedbackWorkflow.Object);

            await localStorageService.SetItemAsStringAsync(
                "AppSettings.State.v1",
                "{\"theme\":\"dark\"}",
                TestContext.Current.CancellationToken);

            var initializationResult = await startupClient.InitializeAsync(TestContext.Current.CancellationToken);
            var updated = await target.SaveSettingsAsync(
                new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                },
                TestContext.Current.CancellationToken);

            initializationResult.IsFailure.Should().BeFalse();
            updated.MasterStorageType.Should().Be(StorageType.ClientData);
            updated.GroupStorageTypes.Should().BeEmpty();
            updated.ItemStorageTypes.Should().BeEmpty();
            storeRequestCount.Should().Be(1);

            var migratedLocalValue = await localStorageService.GetItemAsStringAsync("AppSettings.State.v1", TestContext.Current.CancellationToken);
            migratedLocalValue.Should().BeNull();

            var persistedSettings = await localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, TestContext.Current.CancellationToken);
            persistedSettings.Should().NotBeNull();
            persistedSettings!.MasterStorageType.Should().Be(StorageType.ClientData);
        }

        private static ServiceProvider CreateServiceProvider(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            var services = new ServiceCollection();

            services.AddHttpClient(
                "qbt",
                client =>
                {
                    client.BaseAddress = new Uri("http://localhost/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new DelegateMessageHandler(responder));
            services.AddQBittorrentApiClient("qbt");

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }

        private sealed class DelegateMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public DelegateMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }
    }
}
