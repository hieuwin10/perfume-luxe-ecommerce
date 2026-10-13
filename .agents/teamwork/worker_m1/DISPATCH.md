## 2026-10-12T08:08:07Z
You are worker_m1, an implementation specialist.
Your working directory is: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1
The workspace directory is: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main
Parent orchestrator conversation ID is: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f

MANDATORY FIRST STEP: Read the full user request at:
C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\ORIGINAL_REQUEST.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Reference Documents:
- PROJECT.md: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\PROJECT.md
- Architecture & Schema Survey: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_survey_1\survey_report.md
- UI & Presentation Survey: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_survey_2\survey_report.md

Milestone 1 Scope:
Implement Flash Sale Campaign Engine & Additive Database Schema (R1 + Additive Schema Upgrader Foundation).
1. Domain Entities (`src/Perfume.Domain/Entities/EcommerceEntities.cs`):
   - Add `FlashSale` and `FlashSaleItem` entities as specified in explorer_survey_1 report.
   - Add additive properties to `Order` (`LoyaltyDiscountAmount`, `AppliedLoyaltyTier`), `OrderItem` (`IsCustomBox`, `CustomBoxConfigJson`), and `ShoppingCartItem` (`IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`).
2. DbContext (`src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`):
   - Add `DbSet<FlashSale> FlashSales`, `DbSet<FlashSaleItem> FlashSaleItems`.
   - Configure Fluent API, relationships, precision, query filters (`!IsDeleted`).
3. Additive Schema Upgrader (`src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`):
   - Add Version 2 migration (`20261012_02_flashsales_loyalty_discoverybox`):
     - `IF OBJECT_ID('dbo.FlashSales') IS NULL CREATE TABLE...`
     - `IF OBJECT_ID('dbo.FlashSaleItems') IS NULL CREATE TABLE...`
     - `IF OBJECT_ID('dbo.UserLoyalties') IS NULL CREATE TABLE...`
     - Additive columns on `Orders`, `OrderItems`, `ShoppingCartItems` via `IF COL_LENGTH(...) IS NULL ALTER TABLE... ADD...`
     - Ensure `Database:BackupPath` handles null or empty safely without throwing fatal crash during development runs.
     - Include seed sample active flash sale campaign if none exists so that flash sale works out-of-the-box.
4. Services (`Perfume.Application` & `Perfume.Infrastructure`):
   - Define and implement `IFlashSaleService` (GetActiveFlashSaleAsync, GetAllFlashSalesAsync, CreateFlashSaleAsync, ToggleStatusAsync, DeleteAsync).
   - Register service in Dependency Injection (`InfrastructureServiceRegistration.cs` or `Program.cs`).
5. Admin Management (`src/Perfume.Web`):
   - In `Areas/Admin/Controllers/AdminDashboardController.cs`, add actions: `FlashSales`, `CreateFlashSale`, `ToggleFlashSaleStatus`, `DeleteFlashSale`.
   - Create `Areas/Admin/Views/AdminDashboard/FlashSales.cshtml` adhering to the luxury aesthetic (`.luxury-card`, `.btn-luxe`, dark theme).
   - Add navigation link to `Areas/Admin/Views/Shared/_AdminLayout.cshtml`.
6. Storefront Presentation (`src/Perfume.Web`):
   - Update `HomeController.cs` to fetch active flash sale and pass to `Views/Home/Index.cshtml`.
   - Update `Views/Home/Index.cshtml` to render Flash Sale Banner with a second-accurate real-time countdown timer, flash sale product cards, stock progress bar, and automated price reversion to BasePrice when countdown reaches 0 or stock exhausted.
   - Update `Views/Catalog/Index.cshtml` and `Views/Catalog/Detail.cshtml` to show Flash Sale badges and price adjustments.
7. Verification:
   - Run `dotnet build` to ensure 0 errors and 0 warnings.
   - Test that models, upgrader, and views compile cleanly.
   - Write full completion and handoff report to:
     `C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1\handoff.md`
   - Send completion message to orchestrator `b6612c94-7adb-45ee-ad42-f5cfd31ddb1f`.
