# Review & Adversarial Critic Handoff Report - Milestone 1 Storefront & Admin Presentation

**Agent**: `reviewer_m1_2` (Objective Code Reviewer & Adversarial Critic)  
**Date**: 2026-10-12  
**Milestone**: Milestone 1 (Storefront & Admin Presentation, Flash Sale Campaign Engine)  
**Verdict**: **REQUEST_CHANGES**  

---

## 1. Observation

1. **Build Health**:
   - Executed: `dotnet build`
   - Verbatim console output:
     ```text
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:03.12
     ```
   - All projects (`Perfume.Domain`, `Perfume.Application`, `Perfume.Infrastructure`, `Perfume.Web`, and `Perfume.Tests`) compile without syntax errors.

2. **CRITICAL FINDING: Duplicate `@section Scripts` in `Views/Catalog/Detail.cshtml`**:
   - Location 1: `src/Perfume.Web/Views/Catalog/Detail.cshtml` line 288:
     ```razor
     @section Scripts {
     <script>
     document.getElementById('customerReviewForm')?.addEventListener('submit', async event => {
         event.preventDefault();
         ...
     ```
   - Location 2: `src/Perfume.Web/Views/Catalog/Detail.cshtml` line 342:
     ```razor
     @section Scripts {
         <script>
             let selectedVariantId = @(defaultVariant?.Id.ToString() ?? "null");
             ...
     ```
   - In ASP.NET Core Razor MVC, defining multiple `@section` blocks with the identical name in a view violates the Razor page specification. When rendered by `_Layout.cshtml` via `@await RenderSectionAsync("Scripts", required: false)`, Razor executes `RazorPage.DefineSection("Scripts", ...)` which throws:
     `System.InvalidOperationException: The section 'Scripts' has already been defined for the page '/Views/Catalog/Detail.cshtml'.`
   - Impact: Every request to `/nuoc-hoa/{slug}` (Product Detail page) crashes at runtime with an unhandled HTTP 500 error.

3. **MAJOR FINDING: Missing Stock Depletion Guard in `FlashSaleService.GetActiveFlashSaleItemForProductAsync`**:
   - In `src/Perfume.Infrastructure/Services/FlashSaleService.cs` lines 121-126:
     ```csharp
     .Where(i => i.ProductId == productId
         && (!variantId.HasValue || i.VariantId == variantId.Value || i.VariantId == null)
         && i.FlashSale != null
         && i.FlashSale.IsActive
         && i.FlashSale.StartTime <= now
         && i.FlashSale.EndTime > now)
     ```
   - Missing check: `&& i.SoldQuantity < i.StockLimit`.
   - While `PerfumeCatalogService.cs` (lines 176 and 279) independently includes `&& i.SoldQuantity < i.StockLimit`, any caller invoking `IFlashSaleService.GetActiveFlashSaleItemForProductAsync` (e.g. cart/checkout pricing verification or future APIs in M2/M4) will return exhausted items as active promotional items.

4. **Countdown Timer & Automated Reversion Inspection**:
   - **Homepage Banner (`Views/Home/Index.cshtml`)**:
     - Lines 170-171: Reads ISO 8601 UTC end timestamp via `@activeFlashSale.EndTime.ToString("o")`.
     - Lines 173-197: Runs second-accurate interval `setInterval(updateFlashTimer, 1000)` computing `days`, `hours`, `minutes`, and `seconds`.
     - Lines 177-181 & 199-225: When `distance <= 0`, clears interval and executes `handleFlashSaleExpired()`:
       - Displays `"Chiến Dịch Đã Kết Thúc"` in `#flashCountdownContainer`.
       - Iterates over `.flash-sale-product-card`: reads `data-base-price`, updates `.current-price` to BasePrice formatted in `vi-VN` currency, hides strike-through `.original-price` and `.flash-badge`, and re-labels the action button to `"Thêm Vào Giỏ"`.
   - **Product Detail (`Views/Catalog/Detail.cshtml`)**:
     - Lines 384-387: Serializes ISO 8601 UTC timestamp `flashEndTimeStr`.
     - Lines 394-419: Runs second-accurate timer `setInterval(updateDetailTimer, 1000)` displaying formatted `HH:mm:ss`.
     - Lines 397-406: When `diff <= 0`, hides `#detailFlashSaleBanner`, hides `#activeOriginalPrice`, and reverts `#activePrice` to `basePrice.toLocaleString('vi-VN') + '₫'` in gold color.
     - Note: While the script logic is correct, it fails to execute at runtime due to the crash in Observation 2.

5. **MINOR FINDING: Homepage Banner Sold-Out Card Display Alignment**:
   - In `src/Perfume.Web/Views/Home/Index.cshtml` lines 102 & 148-160:
     - When `isSoldOut` (`RemainingQuantity <= 0`) is true, the button is correctly disabled with `"Đã Hết Suất Flash Sale"`.
     - However, lines 128-135 still render `item.FlashSalePrice` instead of switching the displayed card price to `item.OriginalPrice` (`BasePrice`).
     - Requirement R1 states: *"Tự động chuyển về giá gốc khi hết giờ hoặc hết số lượng khuyến mãi."*

6. **Admin Management Inspection (`AdminDashboardController.cs` & `FlashSales.cshtml`)**:
   - Controller actions:
     - `FlashSales` (GET): Loads campaigns and product catalog for dropdown selection.
     - `CreateFlashSale` (POST): Validates name, ensures `endTime > startTime`, converts to UTC (`.ToUniversalTime()`), and parses dynamic product rows, fallback discount prices, and stock limits. Guarded with `[ValidateAntiForgeryToken]`.
     - `ToggleFlashSaleStatus` (POST): Toggles active status with anti-forgery protection.
     - `DeleteFlashSale` (POST): Soft-deletes campaign with anti-forgery protection.
   - Admin View (`Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`):
     - Luxury card styling conforming to `.luxury-card`, `.btn-luxe`, `.btn-luxe-outline`.
     - Campaign status badges ("Đang Diễn Ra", "Sắp Diễn Ra", "Đã Kết Thúc", "Tạm Dừng").
     - Dynamic multi-product modal row insertion (`addProductRow()`).
     - Division-by-zero guards on sales progress percentage:
       `var pct = item.StockLimit > 0 ? (int)Math.Min(100, Math.Round((double)item.SoldQuantity / item.StockLimit * 100)) : 0;`

---

## 2. Logic Chain

1. **Build Success vs Runtime Failure**: Although `dotnet build` completes with 0 errors and 0 warnings (Observation 1), ASP.NET Core Razor standard compilation does not block on duplicate `@section` declarations at compile time in this configuration. At runtime during request processing, `RazorPage.DefineSection` strictly checks dictionary key existence and throws `InvalidOperationException` upon encountering the second `@section Scripts` (Observation 2).
2. **Blast Radius of Finding 1**: Because `Detail.cshtml` is the sole template responsible for rendering all individual product pages (`/nuoc-hoa/{slug}`), this defect completely breaks storefront access to every fragrance detail page, rendering the product detail countdown timer and purchase flow unreachable.
3. **Data Integrity in Service Layer**: In Observation 3, while `PerfumeCatalogService` implements stock depletion filtering locally, `IFlashSaleService` is the central domain service contract. Omitting `SoldQuantity < StockLimit` in `FlashSaleService.GetActiveFlashSaleItemForProductAsync` leaves an integrity loophole for any caller relying on the interface.
4. **Storefront Presentation Conformance**: Storefront countdown scripts on Homepage and Detail page accurately track seconds and handle expiry price reversion (Observation 4). However, resolving the duplicate section tag in `Detail.cshtml` and synchronizing the sold-out price representation in `Home/Index.cshtml` (Observation 5) are necessary to fully satisfy R1 acceptance criteria.

---

## 3. Caveats

- **Order Decrement Concurrency**: Concurrency locking during `SoldQuantity` incrementing under simultaneous high-volume checkout requests is scheduled for integration in Milestone 2 (`CheckoutController` and `OrderService`).
- **Client Clock Skew**: Timers calculate remaining milliseconds against ISO 8601 UTC timestamps on the client browser. Normal clock skew of a few seconds does not compromise server-side cart validation.

---

## 4. Conclusion

**Verdict**: **REQUEST_CHANGES**

Milestone 1 presents high-quality architecture and UI styling, but cannot be approved in its current state due to a blocking runtime crash and a service-level query gap:

### Required Action Items:
1. **[CRITICAL] Fix duplicate `@section Scripts` in `src/Perfume.Web/Views/Catalog/Detail.cshtml`**:
   - Merge the customer review script (line 288) and the variant/countdown script (line 342) into a single `@section Scripts { ... }` block at the bottom of the file.
2. **[MAJOR] Add stock depletion guard to `FlashSaleService.cs`**:
   - Update `GetActiveFlashSaleItemForProductAsync` (line 126) to include `&& i.SoldQuantity < i.StockLimit`.
3. **[MINOR] Align sold-out card price display on `Views/Home/Index.cshtml`**:
   - Update the banner card pricing container so that when `isSoldOut` is true, the primary price reverts to `item.OriginalPrice` (`BasePrice`).

---

## 5. Verification Method

To verify these findings and confirm resolution:

1. **Verify Runtime Rendering of Product Detail Page**:
   - After merging the `@section Scripts` blocks, run:
     ```powershell
     dotnet build
     ```
     *Expected*: 0 Warnings, 0 Errors.
   - Navigate to `/nuoc-hoa/{slug}` (or execute integration test rendering `Catalog/Detail`) to verify HTTP 200 OK without `InvalidOperationException`.

2. **Verify Service Stock Depletion Guard**:
   - In `src/Perfume.Infrastructure/Services/FlashSaleService.cs`:
     Confirm line 126 includes `&& i.SoldQuantity < i.StockLimit`.
   - Run:
     ```powershell
     dotnet test
     ```
     *Expected*: All 8 tests in `Perfume.Tests` pass.

3. **Invalidation Conditions**:
   - Any HTTP 500 error on `/nuoc-hoa/{slug}`.
   - `FlashSaleService.GetActiveFlashSaleItemForProductAsync` returning an item whose `SoldQuantity >= StockLimit`.
