# BRIEFING — 2026-10-12T08:46:20Z

## Mission
Implement Milestone 1 Round 2 fixes: Razor Scripts consolidation in Detail.cshtml, service stock limit check in FlashSaleService.cs, and storefront sold-out card alignment in Index.cshtml.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2
- Original parent: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Milestone: milestone_1_round_2

## 🔒 Key Constraints
- DO NOT CHEAT: Genuine implementation, no hardcoded test outputs or dummy facades.
- Minimal change principle.
- Verify `dotnet build` with 0 warnings, 0 errors, and `dotnet test` passes.
- Ensure JSON-LD structured data remains in document body in Detail.cshtml.
- In Index.cshtml, sold-out flash cards display regular BasePrice without strike-through, badge "HẾT SUẤT FLASH SALE", button remains disabled on expired.

## Current Parent
- Conversation ID: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Updated: not yet

## Task Summary
- **What to build**:
  1. Consolidate two `@section Scripts` in `src/Perfume.Web/Views/Catalog/Detail.cshtml` into one at bottom, keeping JSON-LD in body.
  2. Add `&& i.SoldQuantity < i.StockLimit` in `GetActiveFlashSaleItemForProductAsync` in `src/Perfume.Infrastructure/Services/FlashSaleService.cs`.
  3. Update `src/Perfume.Web/Views/Home/Index.cshtml`: sold-out cards render BasePrice without strike-through, badge "HẾT SUẤT FLASH SALE", handleFlashSaleExpired guards sold-out cards.
- **Success criteria**: Clean compilation (0 warnings, 0 errors), all tests pass, correct UI and service behavior.
- **Interface contracts**: See explorer handoffs and original request.
- **Code layout**: ASP.NET Core MVC project structure.

## Key Decisions Made
- [TBD]

## Change Tracker
- **Files modified**: None yet
- **Build status**: Untested
- **Pending issues**: None

## Quality Status
- **Build/test result**: Untested
- **Lint status**: Clean
- **Tests added/modified**: TBD

## Loaded Skills
- None explicitly loaded

## Artifact Index
- C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2\DISPATCH.md
- C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2\BRIEFING.md
- C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2\progress.md
- C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2\handoff.md
