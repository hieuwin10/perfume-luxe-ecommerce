## 2026-10-12T08:46:06Z
You are worker_m1_r2, an implementation specialist.
Your working directory is: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2
The workspace directory is: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main
Parent orchestrator conversation ID is: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f

MANDATORY FIRST STEP: Read the full user request at:
C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\ORIGINAL_REQUEST.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Reference Explorer Blueprints:
- Explorer 1 (Razor Scripts Consolidation): C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_1\handoff.md
- Explorer 2 (Service Stock Limit Check): C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_2\handoff.md
- Explorer 3 (Storefront Sold-Out Card Alignment): C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_3\handoff.md

Tasks:
1. `src/Perfume.Web/Views/Catalog/Detail.cshtml`: Consolidate the two separate `@section Scripts` blocks (lines 288 and 342) into a single unified `@section Scripts` block at the bottom of `Detail.cshtml`, ensuring JSON-LD structured data remains in the document body.
2. `src/Perfume.Infrastructure/Services/FlashSaleService.cs`: In `GetActiveFlashSaleItemForProductAsync` (lines 121-128), add `&& i.SoldQuantity < i.StockLimit` so exhausted flash promotions are excluded.
3. `src/Perfume.Web/Views/Home/Index.cshtml`:
   - For sold-out cards (`RemainingQuantity <= 0`), display regular `BasePrice` without strike-through, render badge as "HẾT SUẤT FLASH SALE".
   - In `handleFlashSaleExpired()`, guard button reset so sold-out cards remain disabled.
4. Verify by running `dotnet build` (ensuring 0 warnings, 0 errors) and `dotnet test`.
5. Write your handoff report to:
   C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1_r2\handoff.md
6. Send completion message to orchestrator `b6612c94-7adb-45ee-ad42-f5cfd31ddb1f`.
