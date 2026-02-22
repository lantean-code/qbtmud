# Coverage Report

Generated from a full solution run:

- `dotnet test --artifacts-path=/tmp/artifacts/qbtmud --collect:"XPlat Code Coverage"`

## Test Execution Summary

- `Blazor.BrowserCapabilities.Test`: 10 passed
- `Lantean.QBitTorrentClient.Test`: 358 passed
- `Lantean.QBTMud.Test`: 2037 passed
- Total: 2405 passed, 0 failed

## Coverage Summary (Whole Solution)

| Scope | Line coverage | Branch coverage |
|---|---:|---:|
| `Blazor.BrowserCapabilities` | 128/138 (92.75%) | 11/12 (91.67%) |
| `Lantean.QBitTorrentClient` | 2574/3383 (76.09%) | 306/366 (83.61%) |
| `Lantean.QBTMud` | 27297/30481 (89.55%) | 7473/8551 (87.39%) |
| **Whole solution** | **29999/34002 (88.23%)** | **7790/8929 (87.24%)** |

## Least-Covered Components (Top 20)

Sorted by line coverage, then branch coverage (files with at least 10 executable lines).

| Rank | Component | Line coverage | Branch coverage |
|---:|---|---:|---:|
| 1 | `src/Lantean.QBTMud/Components/PieceProgress.razor.cs` | 0.00% (0/54) | 0.00% (0/8) |
| 2 | `src/Lantean.QBTMud/Program.cs` | 0.00% (0/67) | N/A (0/0) |
| 3 | `src/Lantean.QBTMud/Components/ErrorDisplay.razor.cs` | 20.00% (4/20) | N/A (0/0) |
| 4 | `src/Lantean.QBTMud/Components/Options/SpeedOptions.razor` | 46.15% (6/13) | N/A (0/0) |
| 5 | `src/Lantean.QBTMud/Components/Options/Options.cs` | 55.17% (16/29) | 33.33% (4/12) |
| 6 | `src/Lantean.QBTMud/Extensions.cs` | 55.77% (29/52) | 33.33% (4/12) |
| 7 | `src/Lantean.QBTMud/Components/TorrentsListNav.razor` | 61.54% (8/13) | 50.00% (3/6) |
| 8 | `src/Lantean.QBTMud/EventHandlers/LongPressEventArgs.cs` | 64.29% (9/14) | N/A (0/0) |
| 9 | `src/Lantean.QBTMud/Filter/ExpressionModifier.cs` | 64.37% (56/87) | 50.00% (6/12) |
| 10 | `src/Lantean.QBTMud/Pages/ThemeDetail.razor` | 66.67% (32/48) | 84.09% (37/44) |
| 11 | `src/Lantean.QBitTorrentClient/Models/SaveLocation.cs` | 67.19% (43/64) | 45.45% (10/22) |
| 12 | `src/Lantean.QBTMud/Components/EnhancedErrorBoundary.cs` | 69.23% (18/26) | 50.00% (1/2) |
| 13 | `src/Lantean.QBTMud/Components/PiecesProgressNew.razor.cs` | 72.47% (279/385) | 49.04% (51/104) |
| 14 | `src/Lantean.QBitTorrentClient/Converters/NullableStringFloatJsonConverter.cs` | 75.00% (18/24) | 37.50% (6/16) |
| 15 | `src/Lantean.QBTMud/Models/FileRow.cs` | 76.47% (13/17) | 50.00% (1/2) |
| 16 | `src/Lantean.QBTMud/Components/UI/Tooltip.razor.cs` | 76.92% (110/143) | 68.75% (33/48) |
| 17 | `src/Lantean.QBTMud/Components/WebSeedsTab.razor.cs` | 77.33% (58/75) | 77.27% (17/22) |
| 18 | `src/Lantean.QBTMud/Components/TrackersTab.razor` | 77.78% (14/18) | 100.00% (2/2) |
| 19 | `src/Lantean.QBTMud/Components/Dialogs/ShareRatioDialog.razor` | 80.00% (8/10) | N/A (0/0) |
| 20 | `src/Lantean.QBTMud/Components/Options/AdvancedOptions.razor` | 80.65% (25/31) | 100.00% (4/4) |

## Recently Targeted Components

These files were explicitly targeted and are now fully covered in the latest run.

| Component | Line coverage | Branch coverage |
|---|---:|---:|
| `src/Lantean.QBTMud/Services/Localization/LanguageCatalog.cs` | 100.00% (123/123) | 100.00% (30/30) |
| `src/Lantean.QBTMud/Components/TorrentInfo.razor.cs` | 100.00% (13/13) | 100.00% (4/4) |
| `src/Lantean.QBitTorrentClient/ApiClientExtensions.cs` | 100.00% (92/92) | 100.00% (2/2) |
| `src/Lantean.QBTMud/Filter/FilterOperator.cs` | 100.00% (66/66) | 100.00% (12/12) |
| `src/Lantean.QBTMud/Components/PiecesProgressCanvas.razor.cs` | 100.00% (223/223) | 100.00% (64/64) |
| `src/Lantean.QBTMud/Layout/OtherLayout.razor.cs` | 100.00% (10/10) | 100.00% (2/2) |
| `src/Lantean.QBTMud/Layout/ListLayout.razor.cs` | 100.00% (14/14) | 100.00% (2/2) |
| `src/Lantean.QBitTorrentClient/Models/TrackerEndpoint.cs` | 100.00% (12/12) | N/A (0/0) |
| `src/Lantean.QBitTorrentClient/Models/TorrentTracker.cs` | 100.00% (36/36) | 100.00% (2/2) |
| `src/Lantean.QBTMud/Models/KeyboardEvent.cs` | 100.00% (83/83) | 100.00% (46/46) |

## Coverage Artifacts

- `test/Blazor.BrowserCapabilities.Test/TestResults/8aeccd94-d715-478d-9506-f394c6e0df7f/coverage.cobertura.xml`
- `test/Lantean.QBitTorrentClient.Test/TestResults/51748c68-d671-43b2-a693-9178ea3d9dbb/coverage.cobertura.xml`
- `test/Lantean.QBTMud.Test/TestResults/4235d9ed-3316-4207-971a-2553852335e7/coverage.cobertura.xml`
