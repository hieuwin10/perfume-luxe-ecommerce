# Handoff Report - Milestone 1: Flash Sale Campaign Engine & Additive Database Schema

**Agent**: `worker_m1` (Implementation Specialist)  
**Date**: 2026-10-12  
**Milestone**: Milestone 1 (R1 + Additive Schema Upgrader Foundation)  
**Status**: Completed  

---

## 1. Observation

1. **Domain Entities**:
   - In `src/Perfume.Domain/Entities/EcommerceEntities.cs`:
     - Added `FlashSale` (lines 115-126) and `FlashSaleItem` (lines 128-144).
     - Added additive properties to `Order` (`LoyaltyDiscountAmount`, `AppliedLoyaltyTier`, lines 19-20).
     - Added additive properties to `OrderItem` (`IsCustomBox`, `CustomBoxConfigJson`, lines 51-52).
   - In `src/Perfume.Domain/Entities/CustomerEntities.cs`:
     - Added additive properties to `ShoppingCartItem` (`IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`, lines 19-21).

2. **DbContext & Fluent API**:
   - In `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`:
     - Registered `DbSet<FlashSale> FlashSales` and `DbSet<FlashSaleItem> FlashSaleItems` (lines 36-37).
     - Configured relationships, precision (`18, 2`), delete behaviors, and global query filters `!IsDeleted` for `FlashSale`, `FlashSaleItem`, `UserLoyalty`, `Order.LoyaltyDiscountAmount`, and `ShoppingCartItem.CustomUnitPrice` (lines 157, 234, 248-278).

3. **Additive Schema Upgrader**:
   - In `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`:
     - Migrated versioning architecture to multi-step sequential array `Migrations`.
     - Added Version 2: `"20261012_02_flashsales_loyalty_discoverybox"`.
     - Script safely uses `IF OBJECT_ID('dbo.FlashSales') IS NULL CREATE TABLE...`, `IF OBJECT_ID('dbo.FlashSaleItems') IS NULL CREATE TABLE...`, `IF OBJECT_ID('dbo.UserLoyalties') IS NULL CREATE TABLE...`, and `IF COL_LENGTH(...) IS NULL ALTER TABLE... ADD...`.
     - Script includes seed logic to create an active out-of-the-box Flash Sale campaign referencing existing products (`SELECT TOP 1 Id FROM dbo.Products`) with discounted prices and stock limits if none exists.
     - Database backup logic checks `configuration["Database:BackupPath"]` safely: if null or whitespace, or if SQL Server throws permissions error, it logs warning/info and proceeds without crashing application startup.

4. **Service Architecture & Dependency Injection**:
   - Defined `IFlashSaleService` in `src/Perfume.Application/Interfaces/IFlashSaleService.cs`.
   - Defined `FlashSaleDto`, `FlashSaleItemDto`, `CreateFlashSaleRequest`, `CreateFlashSaleItemRequest` in `src/Perfume.Application/DTOs/FlashSaleDtos.cs`.
   - Added flash sale attributes (`IsFlashSale`, `FlashSalePrice`, `FlashSaleDiscountPercent`, `FlashSaleEndTime`, `FlashSaleStockLimit`, `FlashSaleSoldQuantity`) to `ProductCardDto` and `ProductDetailDto` in `src/Perfume.Application/DTOs/PerfumeDtos.cs`.
   - Implemented `FlashSaleService` in `src/Perfume.Infrastructure/Services/FlashSaleService.cs` (`GetActiveFlashSaleAsync`, `GetAllFlashSalesAsync`, `GetFlashSaleByIdAsync`, `CreateFlashSaleAsync`, `ToggleStatusAsync`, `DeleteAsync`, `GetActiveFlashSaleItemForProductAsync`).
   - Registered `services.AddScoped<IFlashSaleService, FlashSaleService>()` in `src/Perfume.Infrastructure/DependencyInjection.cs` (line 43).
   - Enriched `PerfumeCatalogService.cs` (`QueryProductCards` and `GetProductBySlugAsync`) to check active flash sale items and bind discount pricing, percentages, stock quantities, and end times to catalog products in real-time.

5. **Admin Management & Presentation**:
   - In `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`:
     - Injected `IFlashSaleService`.
     - Added actions: `FlashSales` (GET), `CreateFlashSale` (POST with anti-forgery), `ToggleFlashSaleStatus` (POST with anti-forgery), `DeleteFlashSale` (POST with anti-forgery).
   - Created `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`:
     - Built luxury dashboard interface adhering to `.luxury-card`, `.btn-luxe`, `.btn-luxe-outline`, dark theme.
     - Allows modal-based creation with dynamic multi-product rows, date-time pickers, prices, limits, and status toggles.
   - Added navigation item in `src/Perfume.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml` with bolt icon and gold accent.

6. **Storefront Presentation**:
   - Updated `src/Perfume.Web/Controllers/HomeController.cs`: Injected `IFlashSaleService`, passed `ViewBag.ActiveFlashSale`.
   - Updated `src/Perfume.Web/Views/Home/Index.cshtml`:
     - Added royal gold Flash Sale banner section with campaign title, description, and live real-time countdown timer (`Ngày`, `Giờ`, `Phút`, `Giây`).
     - Product cards show flash discount badge (`GIẢM X%`), flash price, original price, stock progress bar (`Đã bán X/Y chai`), and "Sở Hữu Ngay" button.
     - Included second-accurate `setInterval` countdown script that handles automatic price reversion to `BasePrice` and updates button states when time expires.
     - Updated Featured Perfumes cards to display Flash Sale badges and adjusted prices when active.
   - Updated `src/Perfume.Web/Views/Catalog/Index.cshtml`: Product cards render Flash Sale badge and struck-through original price with active flash price.
   - Updated `src/Perfume.Web/Views/Catalog/Detail.cshtml`: Added Flash Sale alert box, stock sold counter, live countdown timer (`--:--:--`), and automatic price reversion script.

7. **Build Command Verification**:
   - Command: `dotnet build`
   - Output:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     ```

---

## 2. Logic Chain

1. **Requirement R1 & Schema Foundation**: R1 demands a Flash Sale engine with admin management, real-time storefront countdown, product badges, and automated reversion. Additionally, R2 (Loyalty) and R3 (Custom Discovery Box) require additive columns on `Orders`, `OrderItems`, and `ShoppingCartItems`, plus `UserLoyalties`.
2. **Layer Separation**:
   - Domain layer defines entities (`FlashSale`, `FlashSaleItem`) and additive fields without external dependencies.
   - Infrastructure layer provides EF Core mappings in `PerfumeDbContext` and additive schema DDL in `DatabaseSchemaUpgrader`.
   - Application layer defines DTOs and contracts in `Perfume.Application` to decouple presentation from storage.
   - Web layer coordinates admin actions and storefront rendering via Razor SSR + client-side timers.
3. **Additive Safety & Zero Downtime**:
   - All DDL statements check `IF OBJECT_ID(...) IS NULL` or `IF COL_LENGTH(...) IS NULL`.
   - Existing table schemas and data remain untouched.
   - Database backup failure or unconfigured path in local dev does not block the application from starting or applying schema upgrades.
4. **End-to-End Storefront Reactivity**:
   - When a flash sale campaign is active, `PerfumeCatalogService` automatically matches products with active campaign items.
   - Product cards on Home, Catalog, and Product Detail show promotional badges and prices without manual per-page overrides.
   - When the campaign expires (or timer reaches 0), client scripts immediately revert prices to base prices and disable promotional styling.

---

## 3. Caveats

- **Timezone**: Campaign `StartTime` and `EndTime` are persisted and compared in UTC (`DateTime.UtcNow`). Client views format to local time via `.ToLocalTime()` or browser JavaScript `Date.getTime()`.
- **Payment & Order Items in Milestone 1**: Milestone 1 creates the schema foundation (`LoyaltyDiscountAmount`, `AppliedLoyaltyTier`, `IsCustomBox`, `CustomBoxConfigJson`, `CustomUnitPrice`). Order checkout processing for VIP discounts and Custom Box assembly will be implemented in Milestones 2 and 3 respectively.

---

## 4. Conclusion

Milestone 1 is complete. The Flash Sale Campaign Engine, additive database schema, EF Core DbContext mappings, DatabaseSchemaUpgrader Version 2, application service, Admin UI at `/Admin/AdminDashboard/FlashSales`, and Storefront UI (Homepage banner with real-time countdown, Catalog badges, and Detail view) are implemented, integrated, and compiling cleanly with 0 warnings and 0 errors.

---

## 5. Verification Method

To independently verify the implementation:

1. **Compilation Check**:
   ```powershell
   dotnet build
   ```
   *Expected result*: Succeeded with 0 Warnings and 0 Errors across all projects (`Perfume.Domain`, `Perfume.Application`, `Perfume.Infrastructure`, `Perfume.Web`).

2. **Schema & Code Inspection**:
   - Inspect entities in `src/Perfume.Domain/Entities/EcommerceEntities.cs` and `CustomerEntities.cs`.
   - Inspect migration script in `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs` (Version `20261012_02_flashsales_loyalty_discoverybox`).
   - Inspect service in `src/Perfume.Infrastructure/Services/FlashSaleService.cs`.
   - Inspect admin view in `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`.
   - Inspect storefront views in `src/Perfume.Web/Views/Home/Index.cshtml`, `src/Perfume.Web/Views/Catalog/Index.cshtml`, and `src/Perfume.Web/Views/Catalog/Detail.cshtml`.

3. **Invalidation Conditions**:
   - `dotnet build` produces any compiler errors or warnings.
   - Flash sale entities or migrations alter existing non-nullable columns without defaults.
