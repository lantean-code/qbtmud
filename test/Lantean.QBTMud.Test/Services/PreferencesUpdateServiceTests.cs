using AwesomeAssertions;
using Lantean.QBTMud.Services;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class PreferencesUpdateServiceTests
    {
        private readonly PreferencesUpdateService _target;

        public PreferencesUpdateServiceTests()
        {
            _target = new PreferencesUpdateService();
        }

        [Fact]
        public async Task GIVEN_NoSubscribers_WHEN_PublishAsync_THEN_Completes()
        {
            var preferences = CreatePreferences("en");

            var action = async () => await _target.PublishAsync(preferences);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_NullPreferences_WHEN_PublishAsync_THEN_ThrowsArgumentNullException()
        {
            var action = async () => await _target.PublishAsync(null!);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_Subscribers_WHEN_PublishAsync_THEN_InvokesAllInRegistrationOrder()
        {
            var invocationOrder = new List<string>();
            _target.PreferencesUpdated += preferences =>
            {
                invocationOrder.Add($"1:{preferences.Locale}");
                return ValueTask.CompletedTask;
            };
            _target.PreferencesUpdated += preferences =>
            {
                invocationOrder.Add($"2:{preferences.Locale}");
                return ValueTask.CompletedTask;
            };

            var preferences = CreatePreferences("fr");
            await _target.PublishAsync(preferences);

            invocationOrder.Should().Equal("1:fr", "2:fr");
        }

        private static Preferences CreatePreferences(string locale)
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.Locale = locale;
            });
        }
    }
}
