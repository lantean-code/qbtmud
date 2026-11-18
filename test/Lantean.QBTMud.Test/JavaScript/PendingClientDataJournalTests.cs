using AwesomeAssertions;
using Microsoft.Playwright;

namespace Lantean.QBTMud.Test.JavaScript
{
    public sealed class PendingClientDataJournalTests
    {
        private const string _storageKey = "SettingsStorage.PendingClientData.v1";

        [Fact]
        public async Task GIVEN_TwoTabs_WHEN_JournalUpdatesOverlap_THEN_ShouldPreserveEveryPendingKey()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            var sibling = await OpenSiblingAsync(target.Page);

            await Task.WhenAll(target.Page.EvaluateAsync("""
                storageKey => Promise.all(Array.from({ length: 20 }, (_, index) =>
                    qbt.updatePendingClientDataJournal(storageKey, "set", { ["QbtMud.First" + index]: index })))
                """, _storageKey), sibling.EvaluateAsync("""
                storageKey => Promise.all(Array.from({ length: 20 }, (_, index) =>
                    qbt.updatePendingClientDataJournal(storageKey, "set", { ["QbtMud.Second" + index]: index })))
                """, _storageKey));

            var result = await target.Page.EvaluateAsync<Dictionary<string, int>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);
            result.Should().HaveCount(40);
            foreach (var index in Enumerable.Range(0, 20))
            {
                result["QbtMud.First" + index].Should().Be(index);
                result["QbtMud.Second" + index].Should().Be(index);
            }
        }

        [Fact]
        public async Task GIVEN_ReplayedSnapshot_WHEN_AnotherTabQueuesNewerChanges_THEN_ShouldAcknowledgeOnlyMatchingValues()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            var sibling = await OpenSiblingAsync(target.Page);
            var snapshot = await target.Page.EvaluateAsync<Dictionary<string, string?>>("""
                storageKey => qbt.updatePendingClientDataJournal(storageKey, "set", {
                    "QbtMud.Changed": "Original", "QbtMud.Completed": "Completed", "QbtMud.Removed": null
                })
                """, _storageKey);
            await sibling.EvaluateAsync("""
                storageKey => qbt.updatePendingClientDataJournal(storageKey, "set", {
                    "QbtMud.Changed": "Newer", "QbtMud.Added": "Added"
                })
                """, _storageKey);

            var remaining = await target.Page.EvaluateAsync<Dictionary<string, string>>("""
                args => qbt.updatePendingClientDataJournal(args.storageKey, "complete", {
                    ...args.snapshot, "QbtMud.Absent": "Absent"
                })
                """, new { storageKey = _storageKey, snapshot });

            remaining.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["QbtMud.Changed"] = "Newer",
                ["QbtMud.Added"] = "Added"
            });
        }

        [Theory]
        [InlineData("Source")]
        [InlineData("QbtMud.Source")]
        public async Task GIVEN_NewerPendingSave_WHEN_MigrationCleansSource_THEN_ShouldKeepPendingValueAndFallback(string sourceKey)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            var sibling = await OpenSiblingAsync(target.Page);
            await target.Page.EvaluateAsync("""
                () => {
                    localStorage.setItem("Source", JSON.stringify("Legacy"));
                    localStorage.setItem("QbtMud.Source", JSON.stringify("Newer"));
                }
                """);
            await sibling.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'set', { 'QbtMud.Source': 'Newer' })", _storageKey);

            var pending = await target.Page.EvaluateAsync<Dictionary<string, string>>("""
                args => qbt.updatePendingClientDataJournal(args.storageKey, "removeLocalSource", args.sourceKey)
                """, new { storageKey = _storageKey, sourceKey });

            pending.Should().ContainKey("QbtMud.Source").WhoseValue.Should().Be("Newer");
            (await target.Page.EvaluateAsync<string>("() => JSON.parse(localStorage.getItem('QbtMud.Source'))"))
                .Should().Be("Newer");
            await sibling.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'complete', { 'QbtMud.Source': 'Newer' })", _storageKey);
            await target.Page.EvaluateAsync("""
                args => qbt.updatePendingClientDataJournal(args.storageKey, "removeLocalSource", args.sourceKey)
                """, new { storageKey = _storageKey, sourceKey });
            (await target.Page.EvaluateAsync<bool>("() => localStorage.getItem('Source') === null && localStorage.getItem('QbtMud.Source') === null"))
                .Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_LegacyJournal_WHEN_ReadThenRemovedAndCleared_THEN_ShouldMigrateAndRetainUnrelatedEntries()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("""
                storageKey => localStorage.setItem(storageKey, JSON.stringify({ "QbtMud.First": "First", "QbtMud.Second": null }))
                """, _storageKey);

            var migrated = await target.Page.EvaluateAsync<Dictionary<string, string?>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);
            var remaining = await target.Page.EvaluateAsync<Dictionary<string, string?>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'remove', 'QbtMud.First')", _storageKey);

            migrated.Should().BeEquivalentTo(new Dictionary<string, string?>
            {
                ["QbtMud.First"] = "First",
                ["QbtMud.Second"] = null
            });
            remaining.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, string?>("QbtMud.Second", null));
            (await target.Page.EvaluateAsync<string?>("storageKey => localStorage.getItem(storageKey)", _storageKey)).Should().BeNull();
            await target.Page.EvaluateAsync("storageKey => qbt.updatePendingClientDataJournal(storageKey, 'clear')", _storageKey);
            (await target.Page.EvaluateAsync<string?>("storageKey => localStorage.getItem('QbtMud.' + storageKey)", _storageKey)).Should().BeNull();
            (await target.Page.EvaluateAsync<Dictionary<string, string?>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey)).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_PrefixedAndLegacyJournals_WHEN_Read_THEN_ShouldUseCurrentPrefixedJournal()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("""
                storageKey => {
                    localStorage.setItem(storageKey, JSON.stringify({ "QbtMud.Source": "Legacy" }));
                    localStorage.setItem("QbtMud." + storageKey, JSON.stringify({ "QbtMud.Source": "Current" }));
                }
                """, _storageKey);

            var result = await target.Page.EvaluateAsync<Dictionary<string, string>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);

            result.Should().ContainKey("QbtMud.Source").WhoseValue.Should().Be("Current");
        }

        [Theory]
        [InlineData("Corrupt")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("\"Corrupt\"")]
        [InlineData("42")]
        public async Task GIVEN_CorruptJournal_WHEN_Read_THEN_ShouldRejectAndPreserveStoredData(string storedData)
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("args => localStorage.setItem('QbtMud.' + args.storageKey, args.storedData)",
                new { storageKey = _storageKey, storedData });

            var operation = async () => await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);

            await operation.Should().ThrowAsync<PlaywrightException>();
            (await target.Page.EvaluateAsync<string>("storageKey => localStorage.getItem('QbtMud.' + storageKey)", _storageKey))
                .Should().Be(storedData);
        }

        [Fact]
        public async Task GIVEN_UnknownOperation_WHEN_JournalUpdated_THEN_ShouldRejectWithoutLosingPendingChanges()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'set', { 'QbtMud.Source': 'Source' })", _storageKey);

            var operation = async () => await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'unknown')", _storageKey);

            await operation.Should().ThrowAsync<PlaywrightException>().WithMessage("*Unknown pending-change journal operation.*");
            (await target.Page.EvaluateAsync<Dictionary<string, string>>(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey))
                .Should().ContainKey("QbtMud.Source").WhoseValue.Should().Be("Source");
        }

        [Fact]
        public async Task GIVEN_NewerCoordinationDatabase_WHEN_JournalRead_THEN_ShouldRejectOpeningFailure()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("""
                () => new Promise((resolve, reject) => {
                    const opening = indexedDB.open("QbtMud.StorageCoordination", 2);
                    opening.onsuccess = () => { opening.result.close(); resolve(); };
                    opening.onerror = () => reject(opening.error);
                })
                """);

            var operation = async () => await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);

            await operation.Should().ThrowAsync<PlaywrightException>().WithMessage("*VersionError*");
        }

        [Fact]
        public async Task GIVEN_MissingCoordinationStore_WHEN_JournalRead_THEN_ShouldRejectWithoutHanging()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("""
                () => new Promise((resolve, reject) => {
                    const opening = indexedDB.open("QbtMud.StorageCoordination", 1);
                    opening.onsuccess = () => { opening.result.close(); resolve(); };
                    opening.onerror = () => reject(opening.error);
                })
                """);

            var operation = async () => await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey).WaitAsync(TimeSpan.FromSeconds(5));

            await operation.Should().ThrowAsync<PlaywrightException>().WithMessage("*NotFoundError*");
        }

        [Fact]
        public async Task GIVEN_AbortedBrowserTransaction_WHEN_JournalRead_THEN_ShouldRejectWithoutHanging()
        {
            await using var target = await JavaScriptTestPage.CreateAsync("interop.js");
            await target.Page.EvaluateAsync("""
                () => {
                    const originalGet = IDBObjectStore.prototype.get;
                    IDBObjectStore.prototype.get = function (key) {
                        const request = originalGet.call(this, key);
                        this.transaction.abort();
                        return request;
                    };
                }
                """);

            var operation = async () => await target.Page.EvaluateAsync(
                "storageKey => qbt.updatePendingClientDataJournal(storageKey, 'read')", _storageKey);

            await operation.Should().ThrowAsync<PlaywrightException>();
        }

        private static async Task<IPage> OpenSiblingAsync(IPage page)
        {
            var sibling = await page.Context.NewPageAsync();
            await sibling.GotoAsync(page.Url);
            await sibling.AddScriptTagAsync(new PageAddScriptTagOptions { Url = "https://qbtmud.test/js/interop.js" });
            return sibling;
        }
    }
}
