# Coverage Report

Generated from full solution test execution with coverage collection.

## Test execution summary

- `Lantean.QBitTorrentClient.Test`: 294 passed
- `Lantean.QBTMud.Test`: 1298 passed
- Total: 1592 passed, 0 failed

## Coverage summary (whole solution)

Coverage was collected per target assembly and aggregated to avoid double-counting shared assemblies.

| Scope | Line coverage | Branch coverage |
|---|---:|---:|
| `Lantean.QBitTorrentClient` | 2345/3383 (69.32%) | 255/366 (69.67%) |
| `Lantean.QBTMud` | 21060/24703 (85.25%) | 5623/7642 (73.58%) |
| **Whole solution** | **23405/28086 (83.33%)** | **5878/8008 (73.40%)** |

## Least-covered components ranked by priority

Priority combines coverage gap, complexity, and criticality. Core pages/services/filter/API paths are weighted higher due to user and system impact.

| Rank | Priority | Component | Line % | Branch % | Risk factors |
|---:|---|---|---:|---:|---|
| 1 | P0 | `src/Lantean.QBTMud/Pages/Rss.razor.cs` | 0.00 | 2.63 | Core flow page, very low coverage, very high complexity (380) |
| 2 | P0 | `src/Lantean.QBTMud/Filter/FilterExpressionGenerator.cs` | 22.44 | 7.31 | Core filtering logic, both line/branch very low, medium-high complexity (164) |
| 3 | P0 | `src/Lantean.QBTMud/Pages/Rss.razor` | 0.00 | 6.45 | Core UI surface with near-zero coverage |
| 4 | P0 | `src/Lantean.QBTMud/Services/Localization/WebUiLocalizer.cs` | 0.00 | 0.00 | Cross-cutting service, completely untested |
| 5 | P0 | `src/Lantean.QBTMud/Pages/TorrentList.razor.cs` | 49.43 | 47.78 | Primary workflow page, both line/branch under 50% |
| 6 | P1 | `src/Lantean.QBTMud/Helpers/DisplayHelpers.cs` | 38.16 | 14.79 | Broad formatting/helper impact, high complexity (312) |
| 7 | P1 | `src/Lantean.QBitTorrentClient/ApiClient.cs` | 85.36 | 81.02 | API boundary and error-path risk due very high complexity (332) |
| 8 | P1 | `src/Lantean.QBTMud/Components/UI/DynamicTable.razor.cs` | 81.10 | 75.15 | Shared table component, very high complexity (374), medium branch gap |
| 9 | P1 | `src/Lantean.QBTMud/Pages/Search.razor.cs` | 85.19 | 74.80 | Core user flow, high complexity (330), branch-path risk |
| 10 | P1 | `src/Lantean.QBTMud/Components/Options/WebUIOptions.razor.cs` | 72.61 | 35.98 | Configuration surface, low branch coverage, moderate complexity |
| 11 | P2 | `src/Lantean.QBTMud/Components/Options/BitTorrentOptions.razor.cs` | 66.07 | 36.78 | Settings logic with branch gaps |
| 12 | P2 | `src/Lantean.QBitTorrentClient/Models/UpdatePreferences.cs` | 97.43 | 50.00 | High-complexity model mapping, branch behavior only partially covered |

## Notes

- Full solution tests passed during the same coverage run.
- Branch coverage is the better signal for behavioral-path risk in this codebase.
