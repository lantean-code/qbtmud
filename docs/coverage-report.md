# Coverage Report

Generated from full solution test execution with coverage collection.

## Test Execution Summary

- `Lantean.QBitTorrentClient.Test`: 325 passed
- `Lantean.QBTMud.Test`: 1490 passed
- Total: 1815 passed, 0 failed

## Coverage Summary (Whole Solution)

Coverage was collected from `XPlat Code Coverage` Cobertura outputs and aggregated per target assembly.

| Scope | Line coverage | Branch coverage |
|---|---:|---:|
| `Lantean.QBitTorrentClient` | 2420/3383 (71.53%) | 302/366 (82.51%) |
| `Lantean.QBTMud` | 24368/28334 (86.00%) | 6572/8018 (81.97%) |
| **Whole solution** | **26788/31717 (84.46%)** | **6874/8384 (81.99%)** |

## Risk-Weighted Least-Covered Components (Highlighted Set)

Ranking is prioritized by user-impact and operational risk (core workflows, cross-cutting behavior, async/state complexity), not raw coverage alone.

| Rank | Priority | Component | Line % (delta) | Branch % (delta) | Risk factors |
|---:|---|---|---:|---:|---|
| 1 | P0 | `src/Lantean.QBTMud/Services/Localization/WebUiLocalizer.cs` | 75.86% (+75.86pp) | 72.58% (+72.58pp) | Cross-cutting localization behavior and fallback/error handling paths |
| 2 | P0 | `src/Lantean.QBTMud/Pages/TorrentList.razor.cs` | 77.81% (+28.38pp) | 73.21% (+25.43pp) | Primary list page with heavy interaction/state transitions |
| 3 | P0 | `src/Lantean.QBTMud/Pages/Rss.razor.cs` | 86.69% (+86.69pp) | 83.80% (+81.17pp) | Core RSS workflow; async/state-heavy paths substantially improved but still not fully saturated at file-level aggregation |
| 4 | P0 | `src/Lantean.QBTMud/Pages/Rss.razor` | 99.15% (+99.15pp) | 100.00% (+93.55pp) | Core RSS UI surface now effectively saturated |
| 5 | P0 | `src/Lantean.QBTMud/Filter/FilterExpressionGenerator.cs` | 100.00% (+77.56pp) | 100.00% (+92.69pp) | Filtering engine paths fully exercised |
| 6 | P1 | `src/Lantean.QBTMud/Components/UI/DynamicTable.razor.cs` | 82.31% (+1.21pp) | 74.03% (-1.12pp) | Shared table primitive with dense keyboard/selection state machine paths |
| 7 | P1 | `src/Lantean.QBTMud/Pages/Search.razor.cs` | 85.83% (+0.64pp) | 74.06% (-0.74pp) | Polling/job lifecycle logic; cancellation/error synchronization risk |
| 8 | P1 | `src/Lantean.QBTMud/Components/Options/WebUIOptions.razor.cs` | 92.52% (+19.91pp) | 71.05% (+35.07pp) | Settings validation and branch-heavy option mapping |
| 9 | P1 | `src/Lantean.QBitTorrentClient/ApiClient.cs` | 97.84% (+12.48pp) | 91.96% (+10.94pp) | API boundary methods and non-success/validation branches |
| 10 | P1 | `src/Lantean.QBTMud/Helpers/DisplayHelpers.cs` | 99.62% (+61.46pp) | 95.50% (+80.71pp) | Shared formatting helpers used across UI surfaces |
| 11 | P2 | `src/Lantean.QBTMud/Components/Options/BitTorrentOptions.razor.cs` | 98.36% (+32.29pp) | 96.67% (+59.89pp) | Configuration branch correctness and mapping rules |
| 12 | P2 | `src/Lantean.QBitTorrentClient/Models/UpdatePreferences.cs` | 100.00% (+2.57pp) | 100.00% (+50.00pp) | Validation guard model; fully exercised |

## Baseline Comparison Notes

- `FilterExpressionGenerator.cs` reached 100% line and 100% branch coverage.
- `Rss.razor` reached 100% branch coverage and 99.15% line coverage.
- `Rss.razor.cs` improved to 86.69% line and 83.80% branch coverage from a near-zero baseline in the original report.
- 10 of 12 highlighted components improved for both line and branch coverage.
- `DynamicTable.razor.cs` and `Search.razor.cs` gained line coverage but show slight branch percentage drops, indicating remaining unexercised branch paths despite added tests.
- Largest risk reduction occurred in previously near-zero areas: `Rss.razor.cs`, `Rss.razor`, `WebUiLocalizer.cs`, and `FilterExpressionGenerator.cs`.

## Coverage Artifacts

- `test/Lantean.QBTMud.Test/TestResults/e210117d-dd76-4e76-81a3-a521bd5b7bd2/coverage.cobertura.xml`
- `test/Lantean.QBitTorrentClient.Test/TestResults/6d9c43fa-ef5f-4e39-9458-ede658f5fc36/coverage.cobertura.xml`
