# Review & Adversarial Critic Report: Milestone 1 (Flash Sale Engine & Additive Schema)

**Agent**: `reviewer_m1_1`  
**Role**: Objective Code Reviewer & Adversarial Critic  
**Date**: 2026-10-12  
**Target Milestone**: Milestone 1 (Flash Sale Campaign Engine & Additive Database Schema)  
**Verdict**: **APPROVE**  

---

## 1. Observation

1. **Compilation & Build Health**:
   - Executed clean verification:
     ```powershell
     dotnet build --no-incremental
     ```
   - Verbatim compiler output:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:12.54
     ```
   - Target assemblies built cleanly: `Perfume.Domain.dll`, `Perfume.Application.dll`, `Perfume.Infrastructure.dll`, `Perfume.Web.dll`.

2. **Domain Entities & Additive Schema Models**:
   - `src/Perfume.Domain/Entities/EcommerceEntities.cs`:
     - Lines 122-132: Entity `FlashSale` defined with `Name` (string), `Description` (string?), `StartTime` (DateTime), `EndTime` (DateTime), `IsActive` (bool), `BannerUrl` (string?), and navigation collection `Items` (`ICollection<FlashSaleItem>`). Inherits from `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`).
     - Lines 134-148: Entity `FlashSaleItem` defined with foreign keys `FlashSaleId`, `ProductId`, `VariantId`, alongside `FlashSalePrice` (decimal), `StockLimit` (int), and `SoldQuantity` (int).
     - Lines 19-20: Additive properties in `Order`: `LoyaltyDiscountAmount` (decimal = 0), `AppliedLoyaltyTier` (UserTier?).
     - Lines 53-54: Additive properties in `OrderItem`: `IsCustomBox` (bool = false), `CustomBoxConfigJson` (string?).
     - Lines 114-120: Entity `UserLoyalty` (`UserId`, `Points`, `Tier`, `TotalSpent`).
   - `src/Perfume.Domain/Entities/CustomerEntities.cs`:
     - Lines 19-21: Additive properties in `ShoppingCartItem`: `IsCustomBox` (bool = false), `CustomBoxConfigJson` (string?), `CustomUnitPrice` (decimal?).

3. **EF Core Configuration & DbContext**:
   - `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`:
     - Lines 36-37: Registered `DbSet<FlashSale> FlashSales` and `DbSet<FlashSaleItem> FlashSaleItems`.
     - Lines 258-267: `FlashSale` configured with `MaxLength(200)` on `Name`, `MaxLength(500)` on `BannerUrl`, `Cascade` deletion to `Items`, and soft-delete global filter `!IsDeleted`.
     - Lines 269-281: `FlashSaleItem` configured with `Precision(18, 2)` on `FlashSalePrice`, foreign key to `Product` with `DeleteBehavior.Restrict`, foreign key to `Variant` with `DeleteBehavior.SetNull`, and soft-delete global filter `!IsDeleted`.
     - Lines 157, 234, 253: Configured precision `(18, 2)` for `Order.LoyaltyDiscountAmount`, `ShoppingCartItem.CustomUnitPrice`, and `UserLoyalty.TotalSpent`.

4. **Additive Schema Upgrader**:
   - `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`:
     - Lines 96-211: Added migration `"20261012_02_flashsales_loyalty_discoverybox"`.
     - Guarded with DDL checks: `IF OBJECT_ID(N'dbo.FlashSales', N'U') IS NULL`, `IF OBJECT_ID(N'dbo.FlashSaleItems', N'U') IS NULL`, `IF OBJECT_ID(N'dbo.UserLoyalties', N'U') IS NULL`, `IF COL_LENGTH(N'dbo.Orders', N'LoyaltyDiscountAmount') IS NULL ALTER TABLE...`, `IF COL_LENGTH(N'dbo.ShoppingCartItems', N'CustomUnitPrice') IS NULL ALTER TABLE...`.
     - Lines 280-299: Migration execution is wrapped in a dedicated `SqlTransaction` with commit and rollback handling.
     - Lines 254-278: Database pre-migration backup handles missing configuration or permission restrictions gracefully without crashing application startup.
     - Lines 177-210: Out-of-the-box seed campaign inserts a live Flash Sale referencing existing catalog products if no active campaign currently exists.

5. **Service Layer & Dependency Injection**:
   - `src/Perfume.Application/Interfaces/IFlashSaleService.cs`: Interface contract defines `GetActiveFlashSaleAsync`, `GetAllFlashSalesAsync`, `GetFlashSaleByIdAsync`, `CreateFlashSaleAsync`, `ToggleStatusAsync`, `DeleteAsync`, and `GetActiveFlashSaleItemForProductAsync`.
   - `src/Perfume.Infrastructure/Services/FlashSaleService.cs`: Implementation checks UTC time bounds (`f.StartTime <= now && f.EndTime > now`), maps DTOs, and handles null safety.
   - `src/Perfume.Infrastructure/DependencyInjection.cs`: Registered `services.AddScoped<IFlashSaleService, FlashSaleService>()` at line 43.
   - `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`: Lines 168-191 & 268-294 enrich product cards and details in real-time by querying active flash sale items where `SoldQuantity < StockLimit`.

6. **Admin UI & Storefront UI**:
   - `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`:
     - Lines 201-299: Actions `FlashSales` (GET), `CreateFlashSale` (POST), `ToggleFlashSaleStatus` (POST), and `DeleteFlashSale` (POST) implemented with `[ValidateAntiForgeryToken]` and model validation.
   - `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`: Luxury UI with campaign status indicators, item breakdown tables, progress bars, and modal creation form.
   - `src/Perfume.Web/Views/Home/Index.cshtml`:
     - Lines 49-165: Flash sale banner section with campaign heading, real-time countdown timer down to seconds (`Ngày`, `Giờ`, `Phút`, `Giây`), flash badge (`GIẢM X%`), stock sold progress bar (`Đã bán X/Y chai`), and automatic price reversion JavaScript upon expiration.
   - `src/Perfume.Web/Views/Catalog/Index.cshtml` & `Detail.cshtml`: Flash sale badges and price tags rendered on catalog cards and product detail page.

7. **Integrity Verification**:
   - Scanned all new and modified source code for hardcoded test results, facade logic, or bypassed operations.
   - Confirmed all operations query and persist to EF Core and SQL Server schema with genuine logic.

---

## 2. Logic Chain

1. **Requirement Fulfillment**:
   - R1 explicitly requires a Flash Sale campaign engine (name, start/end time, active status, product/variant items, stock limit, exclusive price), Admin UI at `/Admin/AdminDashboard/FlashSales`, and Storefront UI with live countdown timer, badges, and automated reversion to base price upon expiry.
   - The observed implementation in `EcommerceEntities.cs`, `PerfumeDbContext.cs`, `FlashSaleService.cs`, `AdminDashboardController.cs`, and `Home/Index.cshtml` covers 100% of these requirements.

2. **Additive Safety & Zero Data Loss**:
   - DatabaseSchemaUpgrader follows strict additive DDL (`ADD ... NULL` or `ADD ... NOT NULL CONSTRAINT ... DEFAULT`). No existing columns or tables are deleted or altered.
   - Version tracking in `__PerfumeSchemaVersions` ensures idempotency across multiple runs.
   - Migrations are atomic per step via `SqlTransaction`.

3. **Time Zone & Expiry Safety**:
   - All server-side filtering is grounded in `DateTime.UtcNow`.
   - Storefront countdown scripts receive ISO 8601 UTC strings (`EndTime.ToString("o")`), which browsers parse into accurate local epoch timestamps via `new Date(...).getTime()`.
   - Client scripts handle expiration gracefully by reverting displayed prices to `BasePrice` and replacing countdown timers with an expiration notice without requiring an immediate page refresh.

4. **Catalog Decoupling & Performance**:
   - Instead of altering core `Product` pricing columns destructively, `PerfumeCatalogService` dynamically overlays promotional pricing during queries when an active flash campaign is present and stock is available (`SoldQuantity < StockLimit`).
   - Global query filters (`!IsDeleted`) prevent soft-deleted campaigns or items from leaking into catalog queries.

---

## 3. Caveats

1. **Order Processing in Milestone 1**:
   - Milestone 1 lays the schema foundation for order items and checkout (`LoyaltyDiscountAmount`, `AppliedLoyaltyTier`, `IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`). The transactional decrement of `FlashSaleItem.SoldQuantity` during order checkout and validation of stock limits at checkout will be integrated during the checkout flow enhancements in subsequent milestones.
2. **Database Engine**:
   - The migration script utilizes SQL Server specific DDL functions (`SYSUTCDATETIME()`, `OBJECT_ID(...)`, `COL_LENGTH(...)`). This matches the project's production database configuration (`UseSqlServer`).

---

## 4. Conclusion

**Verdict**: **APPROVE**

Milestone 1 successfully delivers the Flash Sale Campaign Engine, additive schema migration, EF Core configuration, Admin UI, and Storefront real-time countdown presentation. The solution compiles cleanly with 0 warnings and 0 errors across all projects. No integrity violations or shortcut facades were identified. The implementation is robust, adheres to Clean Architecture conventions, and is ready to serve as the foundation for Milestone 2.

---

## 5. Verification Method

To independently verify this evaluation:

1. **Build Verification**:
   ```powershell
   dotnet build --no-incremental
   ```
   *Expected result*: `Build succeeded. 0 Warning(s) 0 Error(s)`.

2. **Schema & Code Inspection**:
   - Review entities: `src/Perfume.Domain/Entities/EcommerceEntities.cs` lines 122-148.
   - Review DbContext: `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs` lines 258-281.
   - Review DDL script: `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs` lines 96-211.
   - Review Service: `src/Perfume.Infrastructure/Services/FlashSaleService.cs`.
   - Review Admin UI: `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`.
   - Review Storefront: `src/Perfume.Web/Views/Home/Index.cshtml` lines 49-165.

3. **Invalidation Conditions**:
   - Any compiler error or warning produced by `dotnet build`.
   - Breaking or non-additive schema modifications to existing database tables.
   - Client-side countdown logic failing to handle ISO UTC timestamps or price reversion upon expiration.
