# Deeper review and assessment of the original review

Review date: 15 November 2026. Findings are recorded as actionable checklists in [REVIEW_FINDINGS.md](REVIEW_FINDINGS.md#deeper-review-findings-and-resolutions).

**Remediation status:** R56–R62 have subsequently been fixed with regression tests. The review observations below describe the earlier reviewed commit; the resolution and validation record is in the findings checklist.

## Findings and branch assessment

| Priority | ID | Area | Finding | Provenance |
| --- | --- | --- | --- | --- |
| P2 | R56 | Storage migration | Cleanup can overwrite another operation's pending-journal changes. | Introduced on develop; previously reported and revalidated here. |
| P2 | R57 | Split panel | Initialisation can finish after disposal or run more than once concurrently. | Introduced on develop; previously reported and revalidated here. |
| P2 | R58 | Storage diagnostics | A pending write recreates an entry after the user deletes it. | Introduced by journal replay on develop; newly identified here. |
| P2 | R59 | File-filter editor | Controls operate on originals while submission uses unequal cloned records. | Pre-existing in master; missed by the original whole-codebase review. |
| P2 | R60 | Filter expressions | Boxed operands cause exceptions or incorrect comparisons for advertised operators. | Pre-existing in master; missed by the original whole-codebase review. |
| P2 | R61 | Magnet handler | Valid v2-only magnets are rejected before reaching the add dialog. | Pre-existing in master; missed by the original whole-codebase review. |
| P2 | R62 | Options saving | Successful saves discard edits made while the request is in flight. | Pre-existing in master; missed by the original whole-codebase review. |

At the reviewed commit, the branch needed changes for R56–R58. R59–R62 are additional repository defects, not regressions to attribute to this branch. At that reviewed commit, their relevant production files and the contradictory filter tests had no changes from the comparison base. The subsequent fixes intentionally change those files and assertions. No P0 or P1 issue was established in this follow-up.

## Exact scope and method

- Reviewed state: `develop` / HEAD `21a377ee288fe9eabfca24e0f12b8b42bf5b2475`.
- Comparison: local `master` and `origin/master` both resolve to `1bb741a1dd7e2cdf3a7ecf0cd52f534825e9b380`; that commit is also the merge base. The branch change set contains 166 files, 7,406 insertions and 599 deletions.
- Extended the comparison into unchanged first-party services, components and tests where the changed workflows depend on them. The external `QBittorrent.ApiClient` NuGet package was not treated as repository-owned source.
- Read the review instructions and repository standards, inspected changes and surrounding implementations, traced callers and shared state, and inspected relevant test bodies rather than relying on test names or passing-suite counts.
- Checked asynchronous sequences explicitly: failed mutation → journal → migration/diagnostics → recovery; pending import → render/disposal; filter dialog → definitions → expression compilation; save request → later edit → completion; handler URI → validation → pending-download workflow.
- Used static source and test inspection only. No application code, builds, tests, scripts, browser automation or live qBittorrent operations were run. Only these Markdown reports were edited. Historical test results in the original report were not rerun or independently reproduced.
- Roslyn's existing workspace was stale. It was not reopened because loading it can execute build evaluation. Navigation therefore used source inspection and searches; no fresh semantic analysis is claimed.

## What the original review missed

The assertions described below were captured at 21a377ee and have since been corrected. They are retained as historical evidence of the review gap, rather than descriptions of the fixed tests.

The concern about the earlier whole-codebase review is supported by concrete defects, especially R59 and R60. These are not merely hypothetical edge cases: ordinary edit/remove operations and advertised filter comparisons have incorrect implementations, and tests preserve the incorrect outcomes.

| Evidence | Actual assertion or missing boundary | Consequence for confidence |
| --- | --- | --- |
| [FilterOptionsDialogTests.cs:115](test/Lantean.QBTMud.Presentation.Test/Components/Dialogs/FilterOptionsDialogTests.cs#L115) | The test named `DefinitionReflectsChanges` expects the original operator and original value after editing. | A passing test does not establish that edits work. |
| [FilterOptionsDialogTests.cs:138](test/Lantean.QBTMud.Presentation.Test/Components/Dialogs/FilterOptionsDialogTests.cs#L138) | The test named `DefinitionListEmpty` expects one definition after removal. | The asserted outcome directly contradicts the intended workflow. |
| [FilterExpressionGeneratorTests.cs:34](test/Lantean.QBTMud.Core.Test/Filter/FilterExpressionGeneratorTests.cs#L34) | Empty and NotEmpty are expected to throw. | Tests exercise the broken branches while treating failure as success. |
| [FilterExpressionGeneratorTests.cs:107](test/Lantean.QBTMud.Core.Test/Filter/FilterExpressionGeneratorTests.cs#L107) | Size 10 equal to 10 is expected to be false; ordering is expected to throw. | Execution/branch coverage cannot establish semantic correctness. Similar false equality assertions exist for Boolean, enum and GUID values. |
| Original R48 filter-cancellation coverage | The workflow supplies preconstructed filter definitions through a mocked dialog outcome. | It protects cancellation without validating the actual editor or expression type handling. |
| Original R03 journal coverage | Settings mutations and recovery use one semaphore, but migration and diagnostics mutate related state outside that complete protocol. | Tests of one service do not establish consistency across all owners. |
| Split-panel disposal coverage | A disabled panel receives a late callback after disposal; storage/import are not suspended during disposal. | Late callbacks and late resource acquisition are different cases. |

These observations identify gaps in the review and in its supporting test evidence. They do not establish exactly what each earlier reviewer considered. The earlier fixes remain useful, but “55 completed” must not be read as “the repository has been cleared of defects”.

## Original findings: coverage assessment by area

This table accounts for all original IDs R01–R55. It relates the existing fixes and regression evidence to the workflows inspected during the follow-up. It is a static coverage assessment, not a fresh pass result for every test or proof that each area is defect-free. Historical completion marks are retained; new defects have separate IDs so the original record is not silently rewritten.

| Area and original IDs | Fix and regression evidence considered | Remaining limits or result of the deeper trace |
| --- | --- | --- |
| Storage/history: R01, R03, R04, R05, R49 | Copy-before-routing-commit/source-cleanup ordering; pending typed/raw writes and tombstones; newer mutations overlapping a loading read; path-filter invalidation; populated subcategory rebuilds; resumed weighted speed buckets. Relevant evidence is in StorageRoutingServiceTests, SettingsStorageServiceTests, TorrentDataManagerMainDataTests and SpeedHistoryServiceTests. | R56 and R58 expose missing migration/diagnostics coordination around the journal. Sequential preservation of unrelated entries does not cover overlapping read/modify/write operations. |
| Core membership/completion: R02, R06, R07, R08 | Distinct synthetic category/tag keys and saved-selection decoding; complete tracker-to-hash filtering and bulk-action callers; progress-based completion; timed regex matching and timeout handling. Tests cover named collisions, secondary trackers, selective/resumed downloads and backtracking. | Separate property-filter expression generation was not protected by these tests; R60 remains. Regex timeout is per match, not a measured whole-query performance budget. |
| Themes: R09, R10, R14, R18, R50 | Clearing active selection before deletion, preserving selection after repository failures, keeping same-theme edits across cascades, assigning a null-import fallback, and multiword font validation. Theme tests inspect persisted selection, reload/recovery, edited fields and submitted definitions; browser tests exercise the actual font JavaScript. | Repository/network responses are controlled in tests. Live catalogue availability and complete rendered typography were not checked. No additional confirmed defect was established in these paths. |
| Pages/search/RSS: R12, R13, R15, R16, R17, R19 | Search result draining, feed/article composite identity, displayed log criteria, severity generations/cursor reset, live snapshots after mutations, and retaining active creator tasks after transient failure. Page tests cover batches, duplicate article IDs, rendered rows, late responses and recovery. | These checks address the stated scenarios; they do not exercise every API failure or simultaneous user action. No additional confirmed defect was established in these fixes. |
| Options/share limits: R24, R25, R26, R27, R28, R29, R39 | Retained option panels, switch-only persistence, schedule reversion, independent seeding fields, legacy export disabling, duplicate scan-directory guards and inherited-limit sentinels. Component and page tests check emitted values and representative submitted requests. | R62 exposes edits during an unresolved save, beyond the existing edit-before-save cases. Retaining panels makes correct dirty-state handling particularly important. |
| Files/renaming/categories: R23, R32, R33, R34, R38, R44, R45, R46, R48, R53 | Single-pass replacement expansion; deepest-first folder rename and completed-row retry exclusion; uncategorised membership; cookie URL construction; rename parent paths and endpoint selection; slash-delimited subcategories; cancellation preservation; remembered rename settings. | R59/R60 show that file-filter editing and compiled predicates were not established by the cancellation test. Live qBittorrent filesystem changes were not performed. |
| Async dialogs: R35, R36, R37 | RSS selected-rule snapshots/generations, comma-separated tags, path/load generations and captured request paths. Relevant tests suspend responses and check which rule/path receives the result. | These safeguards are local to those dialogs; they do not imply that every component's initialisation/disposal is safe. R57 is a separate uncovered lifetime path. |
| Browser/UI: R11, R20, R21, R22, R30, R47, R51, R54 | Local CDN fallback, add-link outcomes retained for retry, bounded rate parsing, focus-owner ordering, table focus gating, native pieces activation, orientation state and presentation invalidation. JavaScript regressions use real production scripts on a small Chromium test page; component tests cover the corresponding C# workflows. | The small page is not a full Blazor application runner. Split-panel custom-element initialisation is not covered by those JavaScript tests. R61 exposes a validation gap before the pending-download outcome handling. |
| Translation/build/tooling/docs: R31, R40, R41, R42, R43, R52, R55 | Translation retry/token protection and restoration, quoted conversion, bootstrap asset provenance, screenshot cleanup/login selectors and corrected repository inventory. Build/test configuration and release/container helpers were inspected as text. | No external provider, Linux container, published screenshot capture, historical release asset or fresh build was exercised here. Earlier recorded execution results remain historical evidence only. |

The subsequent three-finding fix at `21a377ee` was also checked in context: serialising successful settings mutations against replay, preserving native button activation, and restoring protected translation-token boundaries. R56/R58 concern additional journal owners; they are not a claim that the newer-mutation test itself asserts the wrong result.

## Additional inspection coverage

The deeper pass extended beyond the original changed methods into the following implementations and caller relationships:

| Area | Paths and relationships inspected |
| --- | --- |
| Storage and application state | SettingsStorageService, StorageRoutingService, StorageDiagnosticsService, StorageCatalogService, AppSettingsService, ClientDataCacheState/invalidation, BrowserStorageService, ClientDataStorageAdapter, SpeedHistoryService, shell sampling and the storage settings tab. |
| Domain/state processing | TorrentDataManager, FilterHelper, completion extensions and notification transitions, RSS/peer data managers, property-filter records, expression generation and expression visitors. |
| Component state and lifetime | TorrentSplitPanel and its module, TorrentList selection/rendering, Details and keyed child lifetime, DynamicTable focus, file-filter/rename/path/RSS/share-limit dialogs, option panels, theme editor and PWA prompt initialisation. |
| Navigation and downloads | PendingDownloadWorkflow, DialogWorkflow add outcomes, MagnetLinkService capture/restore, API URL resolution/configuration, login, shell/startup workflows and authenticated layouts. |
| Other pages/infrastructure | Cookies, statistics/speed, theme loading, update service, localisation initialisation/resources/localiser, HTTP cookie/log handlers, capability service, startup and dependency registrations. |
| Tooling and runtime assets | Translation protection/synchronisation/conversion and associated regression changes; JavaScript bootstrap/keyboard code and small-page tests/coverage configuration; screenshot cleanup/login changes; container image/archive/index/export helpers and startup/configuration shell text; project configuration changes and repository inventory documentation. |

This is a substantially deeper workflow review, with explicit source and assertion checks. It is not an independent line-by-line second audit of every repository file, every test body, generated asset or translation catalogue. A list of inspected areas must not be presented as an exhaustive correctness certificate.

## Remaining review and validation work

- [x] Address R56–R62 and add regression coverage for their failure scenarios; see the resolution record in REVIEW_FINDINGS.md.
- [ ] Inspect remaining test expectations against intended behaviour, especially tests named for successful outcomes which assert exceptions or unchanged state. The filter examples establish a real problem; they do not prove that all such cases have been found.
- [ ] Exercise full-page Blazor workflows separately from the small JavaScript fixture: real filter editing, options saving during network delay, split-panel navigation/disposal and v2 handler capture.
- [x] Validate storage races with shared real service instances and controlled asynchronous boundaries, including final remote/read-back state and preservation of unrelated pending changes.
- [ ] If an independent exhaustive repository audit is required, maintain a file-level coverage ledger as it proceeds. This follow-up's area ledger records its actual reach without claiming that work has already been completed.

The review itself was static. Its subsequent remediation includes the seven fixes, their regression tests and the validation recorded in REVIEW_FINDINGS.md. Broader review and full-page browser work listed above remain separate follow-ups.
