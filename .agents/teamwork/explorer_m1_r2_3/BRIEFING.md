# BRIEFING — 2026-10-12T08:45:00Z

## Mission
Investigate sold-out handling on product cards in Home/Index.cshtml and Catalog/Index.cshtml, focusing on price alignment, badge presentation, button disablement, and countdown event handling.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_3
- Original parent: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Milestone: m1

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Analyze how sold-out status (SoldQuantity >= StockLimit) is handled visually on product cards in both Home and Catalog views (price display, badge presentation, button disablement, countdown completion event handling)

## Current Parent
- Conversation ID: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `src/Perfume.Web/Views/Home/Index.cshtml`
  - `src/Perfume.Web/Views/Catalog/Index.cshtml`
  - `src/Perfume.Web/Views/Catalog/Detail.cshtml`
  - `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`
  - `src/Perfume.Infrastructure/Services/CustomerCartService.cs`
  - `src/Perfume.Application/DTOs/FlashSaleDtos.cs` & `PerfumeDtos.cs`
  - `tests/Perfume.Tests/FlashSaleBoundaryTests.cs`
- **Key findings**:
  - `Home/Index.cshtml` Flash Sale Banner card does not revert displayed price to `OriginalPrice` when `isSoldOut == true` (renders `FlashSalePrice` with strike-through original price).
  - `Home/Index.cshtml` Flash Sale Banner card keeps promotional `GIẢM X%` badge when sold out.
  - `Home/Index.cshtml` `handleFlashSaleExpired()` overwrites `buyBtn.innerHTML` to "Thêm Vào Giỏ" on sold-out cards without clearing `disabled` or binding `onclick`.
  - `Catalog/Index.cshtml` and Home Featured grids automatically revert server-side to base price because `PerfumeCatalogService` queries only `SoldQuantity < StockLimit`.
- **Unexplored areas**: none (full investigation complete)

## Key Decisions Made
- Formulated concrete fix specifications and comparison matrix in handoff.md.

## Artifact Index
- DISPATCH.md — Initial dispatch message
- BRIEFING.md — Persistent context & state
- progress.md — Liveness heartbeat & task progress
- handoff.md — 5-component handoff report with observations, logic chain, caveats, conclusion, and verification method
