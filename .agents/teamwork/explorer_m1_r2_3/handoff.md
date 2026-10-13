# Investigation & Analysis Report: Sold-Out Product Card Presentation & Alignment

**Agent**: `explorer_m1_r2_3` (Storefront UI & Presentation Specialist)  
**Date**: 2026-10-12  
**Target Views**: `src/Perfume.Web/Views/Home/Index.cshtml`, `src/Perfume.Web/Views/Catalog/Index.cshtml`  
**Related Services & DTOs**: `PerfumeCatalogService.cs`, `FlashSaleService.cs`, `PerfumeDtos.cs`, `FlashSaleDtos.cs`  
**Reference Issue**: Reviewer `reviewer_m1_2` Finding 5 & Iteration 1 Gate Action Item 3: *Homepage Banner Card Sold-Out Price Alignment*

---

## 1. Observation

### 1.1 Homepage Flash Sale Banner Cards (`src/Perfume.Web/Views/Home/Index.cshtml`)

1. **Sold-Out Calculation (lines 101-102)**:
   ```razor
   var soldPercent = item.StockLimit > 0 ? (int)Math.Min(100, Math.Round((double)item.SoldQuantity / item.StockLimit * 100)) : 0;
   bool isSoldOut = item.RemainingQuantity <= 0;
   ```
   `isSoldOut` is accurately evaluated based on `item.RemainingQuantity <= 0` (where `RemainingQuantity => Math.Max(0, StockLimit - SoldQuantity)` in `FlashSaleItemDto.cs`).

2. **Price Display Inconsistency (lines 128-135)**:
   ```razor
   <div class="pricing-container" style="margin-bottom: 14px; display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap;">
       <span class="current-price" style="font-size: 1.35rem; font-weight: 800; color: #f59e0b;">
           @item.FlashSalePrice.ToString("N0")₫
       </span>
       <span class="original-price" style="font-size: 0.85rem; text-decoration: line-through; color: var(--text-muted);">
           @item.OriginalPrice.ToString("N0")₫
       </span>
   </div>
   ```
   **Verbatim Observation**: Even when `isSoldOut == true`, the template unconditionally renders `@item.FlashSalePrice` in bold gold (`#f59e0b`) as `.current-price`, while `@item.OriginalPrice` is rendered with `text-decoration: line-through`.  
   **Requirement Non-Conformance**: R1 explicitly states: *"Tự động chuyển về giá gốc khi hết giờ hoặc hết số lượng khuyến mãi."* When the promotional quota is depleted (`isSoldOut == true`), the card still advertises the promotional flash sale price instead of reverting to the base price.

3. **Badge Presentation Inconsistency (lines 111-115)**:
   ```razor
   <div class="flash-badge-container" style="position: absolute; top: 12px; left: 12px; z-index: 2;">
       <span class="badge-tag flash-badge" style="background: linear-gradient(135deg, #ef4444, #f59e0b); color: #FFF; font-weight: 700; font-size: 0.75rem; padding: 4px 10px; border-radius: 20px; box-shadow: 0 4px 10px rgba(239, 68, 68, 0.4);">
           <i class="fa-solid fa-bolt"></i> GIẢM @item.DiscountPercent%
       </span>
   </div>
   ```
   **Verbatim Observation**: When `isSoldOut == true`, the card still displays an active red-amber gradient badge proclaiming `GIẢM @item.DiscountPercent%`. While the button states "Đã Hết Suất Flash Sale", the top badge creates visual cognitive dissonance by promoting an unavailable discount.

4. **Button Disablement (lines 148-160)**:
   ```razor
   <div style="margin-top: auto;">
       @if (!isSoldOut)
       {
           <button type="button" class="btn-luxe buy-btn" onclick="addToCart('@item.ProductSlug', @(item.VariantId.HasValue ? item.VariantId.Value.ToString() : "null"), 1)" style="width: 100%; padding: 10px; font-size: 0.85rem; font-weight: 700;">
               <i class="fa-solid fa-bolt" style="margin-right: 6px;"></i> Sở Hữu Ngay
           </button>
       }
       else
       {
           <button type="button" class="btn-luxe-outline buy-btn" disabled style="width: 100%; padding: 10px; font-size: 0.85rem; opacity: 0.6; cursor: not-allowed;">
               Đã Hết Suất Flash Sale
           </button>
       }
   </div>
   ```
   **Verbatim Observation**: Initial server-side rendering properly disables the button (`disabled`, `opacity: 0.6`, `cursor: not-allowed`), omits the `onclick` handler, and labels it `"Đã Hết Suất Flash Sale"`.

5. **Countdown Expiry Dynamic Reversion Interaction (lines 199-225)**:
   ```javascript
   function handleFlashSaleExpired() {
       const container = document.getElementById('flashCountdownContainer');
       if (container) {
           container.innerHTML = '<span style="color: #ef4444; font-weight: 700; font-size: 0.9rem;"><i class="fa-solid fa-circle-exclamation"></i> Chiến Dịch Đã Kết Thúc</span>';
       }

       const cards = document.querySelectorAll('.flash-sale-product-card');
       cards.forEach(card => {
           const basePrice = Number(card.getAttribute('data-base-price')) || 0;
           const priceEl = card.querySelector('.current-price');
           const origEl = card.querySelector('.original-price');
           const badgeEl = card.querySelector('.flash-badge');
           const buyBtn = card.querySelector('.buy-btn');

           if (priceEl && basePrice > 0) {
               priceEl.innerText = basePrice.toLocaleString('vi-VN') + '₫';
               priceEl.style.color = 'var(--text-primary)';
           }
           if (origEl) origEl.style.display = 'none';
           if (badgeEl) badgeEl.style.display = 'none';
           if (buyBtn) {
               buyBtn.innerHTML = '<i class="fa-solid fa-bag-shopping"></i> Thêm Vào Giỏ';
               buyBtn.classList.remove('btn-luxe');
               buyBtn.classList.add('btn-luxe-outline');
           }
       });
   }
   ```
   **Verbatim Observation**: 
   - Dynamically reverts `.current-price` to `basePrice` formatted in `vi-VN` currency, sets text color to `var(--text-primary)`, and hides `.original-price` and `.flash-badge`.
   - **Flaw on Sold-Out Cards**: `buyBtn.innerHTML` is overwritten with `'Thêm Vào Giỏ'` for *all* cards, including those where `isSoldOut == true`. However, because the sold-out card had the `disabled` attribute and no `onclick` event listener, it ends up as a confusing disabled button labeled "Thêm Vào Giỏ".

---

### 1.2 Catalog View & Homepage Catalog Grids (`Catalog/Index.cshtml` & `Home/Index.cshtml`)

1. **Server-Side Filtering in `PerfumeCatalogService.cs` (lines 272-280)**:
   ```csharp
   var activeItems = await _context.FlashSaleItems
       .Include(i => i.FlashSale)
       .Where(i => productIds.Contains(i.ProductId)
           && i.FlashSale != null
           && i.FlashSale.IsActive
           && i.FlashSale.StartTime <= now
           && i.FlashSale.EndTime > now
           && i.SoldQuantity < i.StockLimit) // <--- Crucial Filter
       .ToListAsync();
   ```
2. **Catalog Product Card Rendering (`Catalog/Index.cshtml` lines 96-99 & 138-154)**:
   ```razor
   @if (p.IsFlashSale)
   {
       <span class="badge-tag" style="background: linear-gradient(135deg, #ef4444, #f59e0b); color: #FFF; font-weight: 700;"><i class="fa-solid fa-bolt"></i> Flash Sale -@p.FlashSaleDiscountPercent%</span>
   }
   ...
   <div class="card-footer">
       <div>
           @if (p.IsFlashSale && p.FlashSalePrice.HasValue)
           {
               <span style="font-size: 0.7rem; color: #f59e0b; display: block; font-weight: 600;"><i class="fa-solid fa-bolt"></i> Flash Sale</span>
               <span class="card-price" style="color: #f59e0b;">@p.FlashSalePrice.Value.ToString("N0")₫</span>
               <span style="font-size: 0.7rem; text-decoration: line-through; color: var(--text-muted); display: block;">@p.BasePrice.ToString("N0")₫</span>
           }
           else
           {
               <span style="font-size: 0.7rem; color: var(--text-muted); display: block;">Từ</span>
               <span class="card-price">@p.BasePrice.ToString("N0")₫</span>
           }
       </div>
       <button type="button" class="btn-luxe" onclick="addToCart('@p.Slug', null, 1)" style="padding: 8px 16px; font-size: 0.78rem;">
           <i class="fa-solid fa-bag-shopping"></i> Thêm
       </button>
   </div>
   ```
3. **Homepage Featured & Best Seller Cards (`Home/Index.cshtml` lines 252-255 & 286-304)**:
   Follows the identical structure to `Catalog/Index.cshtml`.
4. **Verbatim Findings for Catalog & General Product Cards**:
   - Because `PerfumeCatalogService` explicitly filters with `&& i.SoldQuantity < i.StockLimit`, any product whose flash sale quota is exhausted is excluded from `activeItems`.
   - As a result, `p.IsFlashSale` is `false`, `p.FlashSalePrice` is `null`.
   - The card automatically falls back to rendering `p.BasePrice` ("Từ @p.BasePrice₫") and omits the Flash Sale badge.
   - The action button remains an active "Thêm" button allowing purchase at standard base price.
   - **Countdown Timer**: Neither `Catalog/Index.cshtml` nor the Home featured grids contain a countdown timer script. Countdown timer is required by R1 specifically for the Homepage Flash Sale Banner and the Product Detail page.

---

## 2. Logic Chain

1. **Root Cause Analysis (Banner vs. Catalog Data Models)**:
   - On the Homepage Flash Sale Banner, the data source is `activeFlashSale.Items` (`List<FlashSaleItemDto>`). The banner must display all items enrolled in the active campaign to communicate quota progress (e.g. `Đã bán 10/10 (100%)`). Thus, sold-out items are deliberately retained in the collection.
   - In contrast, the Catalog and Featured grids display general inventory through `ProductCardDto`, where `PerfumeCatalogService` queries only unexhausted promotions.
2. **Defect Mechanism**:
   - Because `activeFlashSale.Items` includes items with `RemainingQuantity <= 0`, `Home/Index.cshtml` recognized this condition for button disablement (`isSoldOut = item.RemainingQuantity <= 0`), but omitted conditional branching in the pricing container and badge container.
3. **Requirement Impact (R1 Acceptance Criteria)**:
   - R1: *"Tự động chuyển về giá gốc khi hết giờ hoặc hết số lượng khuyến mãi."*
   - By rendering `item.FlashSalePrice` with strike-through `item.OriginalPrice` on sold-out banner items, the UI promised a discounted price that customers cannot claim, failing both R1 and UX expectations.
4. **Consistency Across Views**:
   - On `Catalog/Index.cshtml`, automatic reversion to base price upon sold-out status is already achieved server-side by `PerfumeCatalogService`.
   - On `Home/Index.cshtml`, the Flash Sale Banner requires explicit Razor template branching for `isSoldOut` so that its pricing, badge, and expiry event handler are fully synchronized.

---

## 3. Caveats

1. **Campaign Sold-Out vs. Store Inventory Sold-Out**:
   - `FlashSaleItemDto.RemainingQuantity <= 0` signifies that the *promotional campaign allocation* is exhausted. The perfume itself may still have stock in the general store warehouse (`Product.IsInStock == true`, `ProductVariant.StockQuantity > 0`).
   - Reverting the banner card's displayed price to `item.OriginalPrice` accurately reflects that the perfume can only be purchased at standard price, while keeping the promotional "Sở Hữu Ngay" flash button disabled.
2. **Catalog Real-Time Depletion Without Page Reload**:
   - Since `Catalog/Index.cshtml` does not implement client-side polling or WebSocket push, a product whose flash sale quota is depleted mid-session by another customer will reflect base price upon next navigation/filter. Cart addition is protected at API level (`CustomerCartService`/`OrderService`).
3. **No Database Schema or DTO Modification Required**:
   - `FlashSaleItemDto` already provides `OriginalPrice`, `FlashSalePrice`, `StockLimit`, `SoldQuantity`, and `RemainingQuantity`. No backend schema changes are necessary to fix this issue.

---

## 4. Conclusion

The sold-out status (`SoldQuantity >= StockLimit`) issue flagged by `reviewer_m1_2` is localized to **`src/Perfume.Web/Views/Home/Index.cshtml`** within the Flash Sale Banner card component (`lines 104-162` and `lines 199-225`). `Catalog/Index.cshtml` already conforms to R1 server-side reversion logic.

### Comparison Matrix

| Visual / Functional Aspect | `Catalog/Index.cshtml` | `Home/Index.cshtml` (Featured Cards) | `Home/Index.cshtml` (Flash Sale Banner Cards) | Conformance Status |
| :--- | :--- | :--- | :--- | :--- |
| **Price Display (when Sold Out)** | Reverts to Base Price (`@p.BasePrice₫`) | Reverts to Base Price (`@p.BasePrice₫`) | **BUG**: Displays discounted `FlashSalePrice` with struck-out `OriginalPrice` | ❌ Banner Non-compliant |
| **Badge Presentation (when Sold Out)** | Flash Sale badge hidden | Flash Sale badge hidden | **INCONSISTENCY**: Still shows active `GIẢM X%` promotional badge | ⚠️ Misleading |
| **Button Disablement (when Sold Out)** | Active "Thêm" at base price | Active "Thêm" at base price | Disabled with "Đã Hết Suất Flash Sale" | ✅ Correct |
| **Countdown Completion Event** | N/A (no timer per spec) | N/A (no timer per spec) | Clears timer, updates container, reverts price/badge. **EDGE BUG**: Mutates sold-out button text without removing `disabled` | ⚠️ Needs guard |

---

### Recommended Fix Strategy (Patch Specification)

In `src/Perfume.Web/Views/Home/Index.cshtml`:

#### 1. Badge Presentation Fix (around line 111):
```razor
<div class="flash-badge-container" style="position: absolute; top: 12px; left: 12px; z-index: 2;">
    @if (!isSoldOut)
    {
        <span class="badge-tag flash-badge" style="background: linear-gradient(135deg, #ef4444, #f59e0b); color: #FFF; font-weight: 700; font-size: 0.75rem; padding: 4px 10px; border-radius: 20px; box-shadow: 0 4px 10px rgba(239, 68, 68, 0.4);">
            <i class="fa-solid fa-bolt"></i> GIẢM @item.DiscountPercent%
        </span>
    }
    else
    {
        <span class="badge-tag flash-badge" style="background: rgba(107, 114, 128, 0.85); color: #FFF; font-weight: 700; font-size: 0.75rem; padding: 4px 10px; border-radius: 20px;">
            <i class="fa-solid fa-circle-xmark"></i> HẾT SUẤT FLASH SALE
        </span>
    }
</div>
```

#### 2. Price Alignment Fix (around line 128):
```razor
<div class="pricing-container" style="margin-bottom: 14px; display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap;">
    @if (!isSoldOut)
    {
        <span class="current-price" style="font-size: 1.35rem; font-weight: 800; color: #f59e0b;">
            @item.FlashSalePrice.ToString("N0")₫
        </span>
        <span class="original-price" style="font-size: 0.85rem; text-decoration: line-through; color: var(--text-muted);">
            @item.OriginalPrice.ToString("N0")₫
        </span>
    }
    else
    {
        <span class="current-price" style="font-size: 1.35rem; font-weight: 800; color: var(--text-primary);">
            @item.OriginalPrice.ToString("N0")₫
        </span>
        <span style="font-size: 0.78rem; color: #ef4444; font-weight: 600;">(Giá gốc - Hết suất ưu đãi)</span>
    }
</div>
```

#### 3. Expiry Script Guard Fix (around line 219):
```javascript
const isCardSoldOut = card.getAttribute('data-sold-out') === 'true';
...
if (buyBtn && !isCardSoldOut) {
    buyBtn.innerHTML = '<i class="fa-solid fa-bag-shopping"></i> Thêm Vào Giỏ';
    buyBtn.classList.remove('btn-luxe');
    buyBtn.classList.add('btn-luxe-outline');
}
```

---

## 5. Verification Method

1. **Static Analysis & Build**:
   ```powershell
   dotnet build
   ```
   *Expected*: Succeeded with 0 Warnings, 0 Errors.

2. **Automated Unit & Boundary Tests**:
   ```powershell
   dotnet test
   ```
   *Expected*: 100% of tests pass, verifying:
   - `FlashSaleBoundaryTests.Boundary3_SoldQuantityEqualsOrExceedsStockLimit_AutomaticallyExcludesFromPromotions` (lines 200-250) verifies `PerfumeCatalogService` correctly excludes exhausted items and returns base price.

3. **Storefront Visual Inspection**:
   - Render Homepage when an active Flash Sale contains an item with `SoldQuantity >= StockLimit`:
     - Primary price shows `@item.OriginalPrice.ToString("N0")₫` in standard color.
     - No strike-through original price.
     - Badge displays "HẾT SUẤT FLASH SALE".
     - Button remains disabled with "Đã Hết Suất Flash Sale".
     - Countdown timer expiring does not overwrite the button to an unclickable "Thêm Vào Giỏ".

4. **Invalidation Conditions**:
   - Banner card with `RemainingQuantity <= 0` displaying `@item.FlashSalePrice` as discounted price.
   - Banner card with `RemainingQuantity <= 0` displaying promotional `GIẢM @item.DiscountPercent%` badge.
