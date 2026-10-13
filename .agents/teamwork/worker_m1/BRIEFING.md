# BRIEFING — 2026-10-12T08:24:00Z

## Mission
Implement Flash Sale Campaign Engine & Additive Database Schema (Milestone 1) including domain entities, DbContext mapping, DatabaseSchemaUpgrader migration script, application service, admin UI, storefront UI (home banner, catalog badges, product detail), and build verification.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\worker_m1
- Original parent: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Milestone: Milestone 1 - Flash Sale Campaign Engine & Additive Database Schema

## 🔒 Key Constraints
- Non-destructive additive database migrations only (use DatabaseSchemaUpgrader, no dropping tables or existing columns).
- BackupPath must handle null/empty gracefully without crash.
- Clean compilation (dotnet build: 0 errors).
- Clean code following existing luxury theme and architecture standards.
- Honest, genuine implementation; no facade or hardcoded values.

## Current Parent
- Conversation ID: b6612c94-7adb-45ee-ad42-f5cfd31ddb1f
- Updated: 2026-10-12T08:08:30Z

## Task Summary
- **What to build**: Domain entities (FlashSale, FlashSaleItem, Order/OrderItem/ShoppingCartItem additive properties), DbContext configuration, DatabaseSchemaUpgrader v2 migration, IFlashSaleService & FlashSaleService, Admin flash sale management actions & view, Storefront flash sale countdown & banner on Home/Catalog/Detail.
- **Success criteria**: dotnet build succeeds with 0 errors; end-to-end flash sale functionality operational.
- **Interface contracts**: PROJECT.md, survey_report.md
- **Code layout**: src/Perfume.Domain, src/Perfume.Infrastructure, src/Perfume.Application, src/Perfume.Web

## Key Decisions Made
- Implemented additive properties on Order (`LoyaltyDiscountAmount`, `AppliedLoyaltyTier`), OrderItem (`IsCustomBox`, `CustomBoxConfigJson`), and ShoppingCartItem (`IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`).
- Enhanced DatabaseSchemaUpgrader with migration sequence handling and safe try/catch for backup path to prevent crashes in dev/CI environments without backup directory access.
- Seeded default sample active flash sale in migration version 2 (`20261012_02_flashsales_loyalty_discoverybox`) so flash sale runs out-of-the-box.
- Implemented IFlashSaleService with full lifecycle methods and auto-enrichment in PerfumeCatalogService for product cards and detail.
- Added client-side real-time second-accurate countdown timers on Home and Detail with automatic price reversion when expired.

## Artifact Index
- DISPATCH.md — Task dispatch record
- BRIEFING.md — Persistent context & situational awareness
- progress.md — Real-time heartbeat and milestone tracking
- handoff.md — Final completion handoff report

## Change Tracker
- **Files modified**:
  - `src/Perfume.Domain/Entities/EcommerceEntities.cs`: Added FlashSale, FlashSaleItem, Order & OrderItem additive fields.
  - `src/Perfume.Domain/Entities/CustomerEntities.cs`: Added ShoppingCartItem additive fields.
  - `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`: Added FlashSales, FlashSaleItems DbSets and model configurations.
  - `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`: Added Version 2 migration & resilient backup logic.
  - `src/Perfume.Application/DTOs/FlashSaleDtos.cs`: Created DTOs for FlashSale and request models.
  - `src/Perfume.Application/Interfaces/IFlashSaleService.cs`: Created interface for FlashSale service.
  - `src/Perfume.Application/DTOs/PerfumeDtos.cs`: Added flash sale fields to ProductCardDto and ProductDetailDto.
  - `src/Perfume.Infrastructure/Services/FlashSaleService.cs`: Implemented IFlashSaleService.
  - `src/Perfume.Infrastructure/DependencyInjection.cs`: Registered IFlashSaleService.
  - `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`: Enriched products with active flash sale pricing.
  - `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`: Added FlashSale admin actions.
  - `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`: Created FlashSale admin management view.
  - `src/Perfume.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`: Added Flash Sale nav link.
  - `src/Perfume.Web/Controllers/HomeController.cs`: Injected IFlashSaleService and passed active campaign to ViewBag.
  - `src/Perfume.Web/Views/Home/Index.cshtml`: Rendered Flash Sale banner with real-time countdown timer & cards.
  - `src/Perfume.Web/Views/Catalog/Index.cshtml`: Displayed Flash Sale badges and price adjustments.
  - `src/Perfume.Web/Views/Catalog/Detail.cshtml`: Displayed Flash Sale banner, timer, and price adjustment.
- **Build status**: Pass (0 Warnings, 0 Errors)
- **Pending issues**: None

## Quality Status
- **Build/test result**: Pass (dotnet build: 0 Warning, 0 Error)
- **Lint status**: Clean
- **Tests added/modified**: Ready for Milestone 5 / QA suite

## Loaded Skills
- None
