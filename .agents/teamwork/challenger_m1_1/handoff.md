# Challenger Handoff Report - Milestone 1 Verification

**Agent**: `challenger_m1_1` (Empirical Verifier & Adversarial Critic)  
**Date**: 2026-10-12  
**Milestone**: Milestone 1 (Flash Sale Campaign Engine & Additive Database Schema)  
**Verdict**: **APPROVE**  

---

## 1. Observation

1. **Build Execution**:
   - Command: `dotnet build`
   - Result:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:03.32
     ```
   - Compiled projects: `Perfume.Domain`, `Perfume.Application`, `Perfume.Infrastructure`, `Perfume.Web`, and `Perfume.Tests`.

2. **Empirical Boundary Test Execution**:
   - Test harness created in `tests/Perfume.Tests/FlashSaleBoundaryTests.cs` and registered in `PerfumeEcommerce.sln`.
   - Command: `dotnet test --verbosity normal`
   - Result:
     ```
     Test run for ...\tests\Perfume.Tests\bin\Debug\net8.0\Perfume.Tests.dll (.NETCoreApp,Version=v8.0)
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary2_CampaignToggledIsActiveFalse_ExcludesFromStorefront [1 s]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary4_NoActiveFlashSaleCampaignExists_GracefulHandlingWithoutNRE [209 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary7_CampaignStartTimeInTheFuture_IsNotYetActive [14 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary6_SoftDeletedFlashSale_IsExcludedByGlobalQueryFilter [3 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary5_EdgeCaseCalculations_ZeroPricesAndStockLimits_NoDivideByZero [1 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary8_MultipleConcurrentCampaigns_PicksEarliestEnding [3 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion [4 ms]
       Passed Perfume.Tests.FlashSaleBoundaryTests.Boundary1_CampaignEndTimeInThePast_ReturnsNullAndRevertsPrice [43 ms]

     Test Run Successful.
     Total tests: 8
          Passed: 8
      Total time: 2,2841 Seconds
     ```

3. **Code Inspection - Boundary Condition 1 (Campaign end time in the past)**:
   - In `src/Perfume.Infrastructure/Services/FlashSaleService.cs` line 27:
     `.Where(f => f.IsActive && f.StartTime <= now && f.EndTime > now)`
   - In `src/Perfume.Application/DTOs/FlashSaleDtos.cs` line 13:
     `public bool IsOngoing => IsActive && StartTime <= DateTime.UtcNow && EndTime > DateTime.UtcNow;`
   - In `src/Perfume.Web/Views/Home/Index.cshtml` lines 175-181 & 199-225:
     Client-side JavaScript detects `distance <= 0`, terminates countdown interval, hides promotional badges, reverts `.current-price` to `data-base-price`, and displays `"Chiến Dịch Đã Kết Thúc"`.
   - In `src/Perfume.Web/Views/Catalog/Detail.cshtml` lines 397-405:
     JS detects `diff <= 0`, hides `#detailFlashSaleBanner`, and reverts `#activePrice` to standard `basePrice`.

4. **Code Inspection - Boundary Condition 2 (Campaign toggled IsActive = false)**:
   - In `FlashSaleService.cs` line 98:
     `fs.IsActive = !fs.IsActive; await _context.SaveChangesAsync(cancellationToken);`
   - In `FlashSaleService.cs` line 27:
     `f.IsActive` is a mandatory filter.
   - In `PerfumeCatalogService.cs` lines 173 and 276:
     `i.FlashSale.IsActive` is strictly evaluated. When deactivated, active items are not bound to cards or detail pages.

5. **Code Inspection - Boundary Condition 3 (SoldQuantity >= StockLimit)**:
   - In `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs` lines 176 and 279:
     Queries explicitly enforce `&& i.SoldQuantity < i.StockLimit`. When `SoldQuantity >= StockLimit`, the item is omitted from active promotion, reverting catalog cards and product detail views to the regular base price.
   - In `src/Perfume.Application/DTOs/FlashSaleDtos.cs` line 34:
     `public int RemainingQuantity => Math.Max(0, StockLimit - SoldQuantity);` (prevents negative values).
   - In `src/Perfume.Web/Views/Home/Index.cshtml` lines 102 & 154-159:
     Card checks `bool isSoldOut = item.RemainingQuantity <= 0;`. If sold out, button is disabled with label `"Đã Hết Suất Flash Sale"`.

6. **Code Inspection - Boundary Condition 4 (No active flash sale campaign exists)**:
   - In `FlashSaleService.cs` line 31: Returns `null`.
   - In `src/Perfume.Web/Controllers/HomeController.cs` line 23:
     `ViewBag.ActiveFlashSale = await _flashSaleService.GetActiveFlashSaleAsync();` (evaluates to `null`).
   - In `src/Perfume.Web/Views/Home/Index.cshtml` line 49:
     `@if (activeFlashSale != null && activeFlashSale.IsOngoing && activeFlashSale.Items.Any())`
     When `null`, entire section is omitted; zero NullReferenceException.
   - In `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml` line 52:
     `@if (Model == null || !Model.Any())` renders luxury empty state message.

7. **Code Inspection - Mathematical Robustness & Division Guards**:
   - `FlashSaleItemDto.DiscountPercent` (line 35):
     `OriginalPrice > 0 ? (int)Math.Round((1m - (FlashSalePrice / OriginalPrice)) * 100m) : 0`
   - `Home/Index.cshtml` sold progress (line 101):
     `item.StockLimit > 0 ? (int)Math.Min(100, Math.Round((double)item.SoldQuantity / item.StockLimit * 100)) : 0`
   - All division operations are protected against zero or negative denominators.

---

## 2. Logic Chain

1. **Build Integrity**: Clean build of all solution components (`Perfume.Domain`, `Perfume.Application`, `Perfume.Infrastructure`, `Perfume.Web`, `Perfume.Tests`) with 0 errors and 0 warnings (Observation 1) proves syntactic and type conformance with .NET 8.
2. **Temporal Correctness**: `EndTime > now` in EF queries and `DateTime.UtcNow` comparisons in DTO `IsOngoing` ensure expired campaigns are never served as active (Observation 3). Storefront client scripts handle in-session expiration reactively (Observations 3 & 4), satisfying R1's requirement to automatically revert to base prices when time expires. Tested and verified in `Boundary1_CampaignEndTimeInThePast_ReturnsNullAndRevertsPrice`.
3. **Administrative State Management**: The toggle action flips `IsActive` and updates SQL Server (Observation 4). Active queries strictly check `IsActive == true`. Deactivation immediately clears the flash sale state without orphaned references. Tested and verified in `Boundary2_CampaignToggledIsActiveFalse_ExcludesFromStorefront`.
4. **Stock Limit Enforcement**: By filtering `SoldQuantity < StockLimit` at both the catalog service layer and the view rendering layer (Observation 5), products whose promotional stock has run out are automatically restored to regular catalog pricing, and the storefront disables purchase actions. Tested and verified in `Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion`.
5. **Absence Safety**: Full null-conditional checking and safe defaulting across controllers, Razor templates, and services ensure that an empty or missing flash sale campaign produces zero runtime crashes or NullReferenceExceptions (Observation 6). Tested and verified in `Boundary4_NoActiveFlashSaleCampaignExists_GracefulHandlingWithoutNRE`.
6. **Edge Case Resilience**: Zero or negative prices/limits are gracefully handled via division guards, soft deletion is filtered by EF Core global query filters, future campaigns remain dormant, and concurrent campaigns are resolved deterministically by earliest end time (Observations 2 & 7). Tested and verified in Boundaries 5, 6, 7, and 8.

---

## 3. Caveats

- **Order Decrement Concurrency**: Milestone 1 implements the catalog presentation, campaign administration, and schema upgrader. Concurrency control during high-velocity checkout writes (`SoldQuantity` incrementing under concurrent lock/transaction) is handled during Milestone 2 Order & Checkout integration.
- **Client vs Server Time Sync**: Storefront timers rely on client browser clock compared to ISO-8601 UTC timestamps (`EndTime.ToString("o")`). Minor local clock skew will not affect server price validation during cart/checkout.

---

## 4. Conclusion

**Verdict: APPROVE**

Milestone 1 Flash Sale business logic, additive database schema, EF Core mappings, and storefront/admin presentations are verified to be robust, secure, and resilient under all required boundary conditions:
- Past campaign end times safely return `null` and revert prices to `BasePrice`.
- Deactivated campaigns (`IsActive = false`) are immediately excluded from active storefront views.
- Depleted stock items (`SoldQuantity >= StockLimit`) revert to base price and display sold-out states.
- The absence of any active campaigns renders cleanly with zero NullReferenceExceptions.
- All 8 empirical boundary tests pass with 100% green status, and the entire solution compiles cleanly with 0 warnings and 0 errors.

---

## 5. Verification Method

To independently reproduce and verify this assessment:

1. **Compile Solution**:
   ```powershell
   dotnet build
   ```
   *Expected outcome*: Succeeded with 0 Warning(s), 0 Error(s).

2. **Execute Empirical Test Suite**:
   ```powershell
   dotnet test --verbosity normal
   ```
   *Expected outcome*: All 8 tests in `tests/Perfume.Tests/FlashSaleBoundaryTests.cs` pass (100% green).

3. **Inspect Implementation Files**:
   - `src/Perfume.Infrastructure/Services/FlashSaleService.cs` (lines 18-33, 113-152)
   - `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs` (lines 168-191, 268-294)
   - `src/Perfume.Web/Views/Home/Index.cshtml` (lines 48-231)
   - `src/Perfume.Web/Views/Catalog/Detail.cshtml` (lines 64-103, 384-420)
   - `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs` (lines 200-300)

4. **Invalidation Conditions**:
   - Any compiler error or warning from `dotnet build`.
   - Any test failure in `dotnet test`.
   - An expired, inactive, or sold-out flash sale item showing promotional discount on catalog or product detail views.
