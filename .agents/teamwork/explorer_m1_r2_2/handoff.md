# Handoff Report: Stock Depletion Filter Investigation across FlashSaleService and PerfumeCatalogService

**Agent**: `explorer_m1_r2_2`  
**Date**: 2026-10-12  
**Mission**: Investigate `src/Perfume.Infrastructure/Services/FlashSaleService.cs` and `PerfumeCatalogService.cs` to verify uniform application of stock depletion check (`SoldQuantity < StockLimit`) across all `FlashSaleItem` and active campaign queries.  

---

## 1. Observation

1. **`FlashSaleService.cs` - `GetActiveFlashSaleItemForProductAsync` (lines 113–129)**:
   - File path: `src/Perfume.Infrastructure/Services/FlashSaleService.cs`
   - Verbatim code lines 121–128:
     ```csharp
     .Where(i => i.ProductId == productId
         && (!variantId.HasValue || i.VariantId == variantId.Value || i.VariantId == null)
         && i.FlashSale != null
         && i.FlashSale.IsActive
         && i.FlashSale.StartTime <= now
         && i.FlashSale.EndTime > now)
     .OrderBy(i => i.FlashSalePrice)
     .FirstOrDefaultAsync(cancellationToken);
     ```
   - **Defect observed**: The LINQ filter is missing `&& i.SoldQuantity < i.StockLimit`. When an item's promotion stock is exhausted (`SoldQuantity >= StockLimit`), this method returns the depleted item instead of returning `null`.

2. **`PerfumeCatalogService.cs` - `GetProductBySlugAsync` (lines 168–179)**:
   - File path: `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`
   - Verbatim code lines 169–178:
     ```csharp
     var activeFlashItem = await _context.FlashSaleItems
         .Include(i => i.FlashSale)
         .Where(i => i.ProductId == product.Id
             && i.FlashSale != null
             && i.FlashSale.IsActive
             && i.FlashSale.StartTime <= now
             && i.FlashSale.EndTime > now
             && i.SoldQuantity < i.StockLimit)
         .OrderBy(i => i.FlashSalePrice)
         .FirstOrDefaultAsync();
     ```
   - **Observation**: `&& i.SoldQuantity < i.StockLimit` is **already correctly present**. When stock is exhausted, `activeFlashItem` is `null` and the product detail view automatically reverts to `BasePrice`.

3. **`PerfumeCatalogService.cs` - `QueryProductCards` (lines 270–280)**:
   - File path: `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs`
   - Verbatim code lines 272–280:
     ```csharp
     var activeItems = await _context.FlashSaleItems
         .Include(i => i.FlashSale)
         .Where(i => productIds.Contains(i.ProductId)
             && i.FlashSale != null
             && i.FlashSale.IsActive
             && i.FlashSale.StartTime <= now
             && i.FlashSale.EndTime > now
             && i.SoldQuantity < i.StockLimit)
         .ToListAsync();
     ```
   - **Observation**: `&& i.SoldQuantity < i.StockLimit` is **already correctly present**. When stock is exhausted, cards in catalog and recommendation listings do not flag `IsFlashSale`.
   - **Improvement note**: While line 177 in `GetProductBySlugAsync` orders by `.OrderBy(i => i.FlashSalePrice)`, `QueryProductCards` does not order by price before matching `activeItems.FirstOrDefault(i => i.ProductId == card.Id)`. Adding `.OrderBy(i => i.FlashSalePrice)` to the SQL query ensures deterministic lowest price selection when multiple active items exist for a product.

4. **`FlashSaleService.cs` - `GetActiveFlashSaleAsync` (lines 18–33)**:
   - File path: `src/Perfume.Infrastructure/Services/FlashSaleService.cs`
   - Verbatim code lines 21–29:
     ```csharp
     var flashSale = await _context.FlashSales
         .Include(f => f.Items)
             .ThenInclude(i => i.Product)
                 .ThenInclude(p => p!.Brand)
         .Include(f => f.Items)
             .ThenInclude(i => i.Variant)
         .Where(f => f.IsActive && f.StartTime <= now && f.EndTime > now)
         .OrderBy(f => f.EndTime)
         .FirstOrDefaultAsync(cancellationToken);
     ```
   - Mapped via `MapToDto` (lines 154–190), which maps all `f.Items.Where(i => !i.IsDeleted)`.
   - **Storefront Consumer**: In `src/Perfume.Web/Views/Home/Index.cshtml` lines 80–87 & 148–160:
     ```razor
     var soldPercent = item.StockLimit > 0 ? (int)Math.Min(100, Math.Round((double)item.SoldQuantity / item.StockLimit * 100)) : 0;
     bool isSoldOut = item.RemainingQuantity <= 0;
     ...
     @if (!isSoldOut) { <button class="btn-luxe buy-btn">Sở Hữu Ngay</button> }
     else { <button class="btn-luxe-outline buy-btn" disabled>Đã Hết Suất Flash Sale</button> }
     ```
   - **Observation**: `GetActiveFlashSaleAsync` deliberately returns all non-deleted items so the Homepage banner can display promotional sales progress (`Đã bán @item.SoldQuantity/@item.StockLimit`, progress bar at 100%) and render the disabled state (`Đã Hết Suất Flash Sale`). Filtering `SoldQuantity < StockLimit` inside `GetActiveFlashSaleAsync` would erase sold-out products from the promotional campaign grid entirely, preventing the user from seeing the sold-out state.

5. **`tests/Perfume.Tests/FlashSaleBoundaryTests.cs` (lines 183–252)**:
   - File path: `tests/Perfume.Tests/FlashSaleBoundaryTests.cs`
   - In `Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion`:
     - Tested `catalogService.GetProductBySlugAsync("mfk-br540")` -> verified `IsFlashSale == false`.
     - Tested `catalogService.GetProductsAsync(...)` -> verified `IsFlashSale == false`.
     - Tested `itemDto.RemainingQuantity == 0`.
     - **GAP**: The test initialized `var flashSaleService = new FlashSaleService(db);` at line 226, but **never invoked or asserted** `flashSaleService.GetActiveFlashSaleItemForProductAsync(3)`. Because of this missing assertion, the test suite was green (8 passed) despite the bug in `FlashSaleService.cs`.

---

## 2. Logic Chain

1. **Contract Intent**: `IFlashSaleService.GetActiveFlashSaleItemForProductAsync(productId, variantId)` is designed to answer: *"Is there an active, purchasable flash sale item for this product/variant right now?"* Downstream consumers (e.g. cart item pricing verification, checkout total calculation in M2, or REST APIs in M4) rely on this method to determine whether to apply the promotional price.
2. **Defect Mechanism**: Omitting `&& i.SoldQuantity < i.StockLimit` in `FlashSaleService.cs` line 126 means that once all promotional units are sold (`SoldQuantity >= StockLimit`), `GetActiveFlashSaleItemForProductAsync` continues returning `FlashSaleItemDto` with `FlashSalePrice`, allowing customers to purchase at the discounted price indefinitely during the campaign duration.
3. **Contrast with `PerfumeCatalogService`**: `PerfumeCatalogService` already enforces `&& i.SoldQuantity < i.StockLimit` in both product detail (line 176) and product card queries (line 279). Thus, storefront browsing displays normal base price, but any caller directly using `IFlashSaleService` would obtain the stale promotional price.
4. **Contrast with `GetActiveFlashSaleAsync`**: Campaign-level queries (`GetActiveFlashSaleAsync`) provide presentation data for the campaign banner and admin views. The UI logic explicitly depends on receiving exhausted items so it can render the 100% progress bar and disable the purchase button. Hence, stock depletion filtering must NOT be applied to `GetActiveFlashSaleAsync`, but MUST be applied to `GetActiveFlashSaleItemForProductAsync`.
5. **Testing Blind Spot**: In `FlashSaleBoundaryTests.cs`, `Boundary3` verified that `PerfumeCatalogService` reverted to BasePrice upon stock exhaustion, but failed to assert `flashSaleService.GetActiveFlashSaleItemForProductAsync(3) == null`. Adding this assertion will provide regression testing.

---

## 3. Caveats

- **No Caveats on Query Logic**: The database schema enforces `StockLimit >= 0` and `SoldQuantity >= 0` via table constraints (`CK_FlashSaleItems_Limit` and `CK_FlashSaleItems_Sold`), so `i.SoldQuantity < i.StockLimit` is completely safe against negative values.
- **Concurrency Locking**: Incremental locking of `SoldQuantity` during checkout execution under simultaneous traffic will be handled in Milestone 2 within `OrderService` / `CheckoutController`.

---

## 4. Conclusion

The reviewer's finding is **confirmed 100% valid**. The stock depletion check is currently applied in `PerfumeCatalogService.cs` but missing in `FlashSaleService.cs`.

### Exact Fix Strategy

#### 1. Target: `src/Perfume.Infrastructure/Services/FlashSaleService.cs`
In method `GetActiveFlashSaleItemForProductAsync` (lines 121–128):

```csharp
// BEFORE
        var item = await _context.FlashSaleItems
            .Include(i => i.FlashSale)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Brand)
            .Include(i => i.Variant)
            .Where(i => i.ProductId == productId
                && (!variantId.HasValue || i.VariantId == variantId.Value || i.VariantId == null)
                && i.FlashSale != null
                && i.FlashSale.IsActive
                && i.FlashSale.StartTime <= now
                && i.FlashSale.EndTime > now)
            .OrderBy(i => i.FlashSalePrice)
            .FirstOrDefaultAsync(cancellationToken);

// AFTER
        var item = await _context.FlashSaleItems
            .Include(i => i.FlashSale)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Brand)
            .Include(i => i.Variant)
            .Where(i => i.ProductId == productId
                && (!variantId.HasValue || i.VariantId == variantId.Value || i.VariantId == null)
                && i.FlashSale != null
                && i.FlashSale.IsActive
                && i.FlashSale.StartTime <= now
                && i.FlashSale.EndTime > now
                && i.SoldQuantity < i.StockLimit)
            .OrderBy(i => i.FlashSalePrice)
            .FirstOrDefaultAsync(cancellationToken);
```

#### 2. Target: `src/Perfume.Infrastructure/Services/PerfumeCatalogService.cs` (Optimization)
In `QueryProductCards` (line 280), add `.OrderBy(i => i.FlashSalePrice)` to guarantee the lowest promotional price is consistently selected if a product has multiple active items:

```csharp
            var activeItems = await _context.FlashSaleItems
                .Include(i => i.FlashSale)
                .Where(i => productIds.Contains(i.ProductId)
                    && i.FlashSale != null
                    && i.FlashSale.IsActive
                    && i.FlashSale.StartTime <= now
                    && i.FlashSale.EndTime > now
                    && i.SoldQuantity < i.StockLimit)
                .OrderBy(i => i.FlashSalePrice)
                .ToListAsync();
```

#### 3. Target: `tests/Perfume.Tests/FlashSaleBoundaryTests.cs` (Regression Test)
In `Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion`:
Add explicit assertion verifying `GetActiveFlashSaleItemForProductAsync`:

```csharp
        // Act & Assert 4: FlashSaleService directly excludes sold-out item
        var activeItem = await flashSaleService.GetActiveFlashSaleItemForProductAsync(3);
        Assert.Null(activeItem);
```

---

## 5. Verification Method

1. **Compilation Check**:
   ```powershell
   dotnet build
   ```
   *Expectation*: Build succeeded with 0 Warnings, 0 Errors.

2. **Automated Unit & Boundary Test Execution**:
   ```powershell
   dotnet test --filter "FullyQualifiedName~Boundary3"
   ```
   *Expectation*: All assertions in `Boundary3` pass, confirming both `PerfumeCatalogService` and `FlashSaleService.GetActiveFlashSaleItemForProductAsync` return `null` for sold-out items.

3. **Invalidation Conditions**:
   - `flashSaleService.GetActiveFlashSaleItemForProductAsync(productId)` returning a non-null `FlashSaleItemDto` when `SoldQuantity >= StockLimit`.
   - `catalogService.GetProductBySlugAsync(slug)` setting `IsFlashSale = true` when all flash promotion units are sold out.
