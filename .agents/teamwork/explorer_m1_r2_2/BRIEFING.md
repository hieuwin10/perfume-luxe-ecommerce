# BRIEFING — 2026-10-12T08:44:45Z

## Mission
Investigate FlashSaleService.cs and PerfumeCatalogService.cs for FlashSaleItem queries and active campaign handling to ensure stock depletion filter (SoldQuantity < StockLimit) is uniformly applied.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_2
- Original parent: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Milestone: milestone-1

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Inspect all queries touching FlashSaleItem and active campaigns across both services
- Check if stock depletion check (SoldQuantity < StockLimit) is uniformly applied wherever active flash items are queried or returned
- Recommend the exact fix

## Current Parent
- Conversation ID: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `ORIGINAL_REQUEST.md` (R1 Flash Sale requirements)
  - `src/Perfume.Infrastructure/Services/FlashSaleService.cs`
  - `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`
  - `src/Perfume.Application/Interfaces/IFlashSaleService.cs`
  - `src/Perfume.Application/DTOs/FlashSaleDtos.cs`
  - `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`
  - `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`
  - `src/Perfume.Web/Views/Home/Index.cshtml`
  - `src/Perfume.Web/Views/Catalog/Detail.cshtml`
  - `tests/Perfume.Tests/FlashSaleBoundaryTests.cs`
  - `.agents/teamwork/reviewer_m1_2/handoff.md`
- **Key findings**:
  - In `FlashSaleService.GetActiveFlashSaleItemForProductAsync`: LINQ `.Where` lacks `&& i.SoldQuantity < i.StockLimit`. Returning exhausted items when `SoldQuantity >= StockLimit`.
  - In `PerfumeCatalogService.cs`: Both `GetProductBySlugAsync` (line 176) and `QueryProductCards` (line 279) already include `&& i.SoldQuantity < i.StockLimit`.
  - In `FlashSaleService.GetActiveFlashSaleAsync`: Correctly returns all campaign items with `StockLimit` and `SoldQuantity` so `Home/Index.cshtml` can display stock progress bars and disabled "Đã Hết Suất Flash Sale" buttons.
  - In `tests/Perfume.Tests/FlashSaleBoundaryTests.cs`: `Boundary3` only tested `catalogService`, never asserted `flashSaleService.GetActiveFlashSaleItemForProductAsync`, which allowed this defect to evade unit testing.
- **Unexplored areas**: None within scope.

## Key Decisions Made
- Confirmed exact fix location in `FlashSaleService.cs` line 126.
- Confirmed that `GetActiveFlashSaleAsync` should NOT filter out depleted items because UI needs them for progress bar / sold-out state rendering.
- Recommended adding assertion in `Boundary3` of `FlashSaleBoundaryTests.cs`.

## Artifact Index
- `DISPATCH.md` — incoming dispatch instructions
- `BRIEFING.md` — persistent state memory
- `progress.md` — liveness heartbeat
- `handoff.md` — final 5-component handoff analysis and recommended fix
