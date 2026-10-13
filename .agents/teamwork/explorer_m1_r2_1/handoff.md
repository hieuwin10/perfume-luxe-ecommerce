# Handoff Report: Investigation of Duplicate `@section Scripts` in `Detail.cshtml`

- **Author**: `explorer_m1_r2_1`
- **Working Directory**: `C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_m1_r2_1`
- **Target File**: `src/Perfume.Web/Views/Catalog/Detail.cshtml`
- **Date**: 2026-10-12T08:44:00Z
- **Reference**: Reviewer `reviewer_m1_2` Issue #1 (CRITICAL - Duplicate `@section Scripts`)

---

## 1. Observation

### 1.1 Duplicate `@section Scripts` in `Detail.cshtml`
Inspection of `src/Perfume.Web/Views/Catalog/Detail.cshtml` (total 422 lines) revealed two distinct `@section Scripts` declarations:

#### Block 1: Lines 288 – 308 (Customer Reviews AJAX Submission)
```razor
@section Scripts {
<script>
document.getElementById('customerReviewForm')?.addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    const button = form.querySelector('button[type="submit"]');
    const message = document.getElementById('reviewApiMessage');
    button.disabled = true;
    message.hidden = true;
    try {
        const result = await CustomerApi.post(`/api/v1/products/${form.dataset.productId}/reviews`, new FormData(form));
        message.textContent = result.message;
        message.style.cssText = 'margin:12px 0;padding:12px;border-radius:8px;color:#52b788;background:rgba(82,183,136,.12)';
        form.reset();
    } catch (error) {
        message.textContent = error.message || 'Chưa gửi được đánh giá.';
        message.style.cssText = 'margin:12px 0;padding:12px;border-radius:8px;color:#f87171;background:rgba(239,68,68,.12)';
    } finally { message.hidden = false; button.disabled = false; }
});
</script>
}
```

#### Intermediate Block: Lines 310 – 340 (SEO Structured Data)
Between the two `@section Scripts` blocks lies standard JSON-LD SEO metadata:
```html
<!-- GOOGLE STRUCTURED DATA (JSON-LD) FOR SEO -->
<script type="application/ld+json">
{
  "@@context": "https://schema.org/",
  "@@type": "Product",
  "name": "@Model.Name",
  ...
}
</script>
```

#### Block 2: Lines 342 – 421 (Variant Selection, Quantity, Add To Cart, Countdown Timer)
```razor
@section Scripts {
    <script>
        let selectedVariantId = @(defaultVariant?.Id.ToString() ?? "null");

        function selectVariant(btn) {
            document.querySelectorAll('.variant-btn').forEach(b => {
                b.style.borderColor = 'var(--border-card)';
                b.style.background = 'var(--bg-surface)';
            });

            btn.style.borderColor = 'var(--gold-primary)';
            btn.style.background = 'rgba(212, 175, 55, 0.15)';

            selectedVariantId = btn.getAttribute('data-id');
            const price = Number(btn.getAttribute('data-price'));
            const orig = Number(btn.getAttribute('data-orig'));

            document.getElementById('activePrice').innerText = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
            const origEl = document.getElementById('activeOriginalPrice');
            if (origEl) {
                if (orig > price) {
                    origEl.innerText = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(orig);
                    origEl.style.display = 'inline';
                } else {
                    origEl.style.display = 'none';
                }
            }
        }

        function changeQty(delta) {
            const el = document.getElementById('buyQty');
            let val = parseInt(el.value) + delta;
            if (val < 1) val = 1;
            if (val > 10) val = 10;
            el.value = val;
        }

        function handleAddToCart() {
            const qty = parseInt(document.getElementById('buyQty').value) || 1;
            addToCart('@Model.Slug', selectedVariantId, qty);
        }

        const flashEndTimeStr = '@(Model.IsFlashSale && Model.FlashSaleEndTime.HasValue ? Model.FlashSaleEndTime.Value.ToString("o") : "")';
        if (flashEndTimeStr) {
            (function() {
                const endTime = new Date(flashEndTimeStr).getTime();
                const timerEl = document.getElementById('detailCountdownDisplay');
                const bannerEl = document.getElementById('detailFlashSaleBanner');
                const priceEl = document.getElementById('activePrice');
                const origEl = document.getElementById('activeOriginalPrice');
                const basePrice = @Model.BasePrice;

                function updateDetailTimer() {
                    const now = new Date().getTime();
                    const diff = endTime - now;
                    if (diff <= 0) {
                        clearInterval(interval);
                        if (bannerEl) bannerEl.style.display = 'none';
                        if (priceEl) {
                            priceEl.innerText = basePrice.toLocaleString('vi-VN') + '₫';
                            priceEl.style.color = 'var(--gold-primary)';
                        }
                        if (origEl) origEl.style.display = 'none';
                        return;
                    }

                    const hours = Math.floor(diff / (1000 * 60 * 60));
                    const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                    const secs = Math.floor((diff % (1000 * 60)) / 1000);
                    if (timerEl) {
                        timerEl.innerText = `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
                    }
                }

                updateDetailTimer();
                const interval = setInterval(updateDetailTimer, 1000);
            })();
        }
    </script>
}
```

### 1.2 Image Zoom Investigation
- At line 23 of `Detail.cshtml`:
  `<img id="mainProductImg" src="@Model.MainImageUrl" alt="@Model.Name" style="max-width: 100%; height: 480px; object-fit: contain; border-radius: var(--radius-md); transition: transform 0.4s ease;" />`
- Git commit log inspection (`eddeb79`, `069b762`, `5c1f381`) shows there was **never any custom JS image zoom function**. The zoom effect is achieved via CSS transition styling (`transition: transform 0.4s ease;`).
- Therefore, no JavaScript image zoom code was lost or broken; the 4 functional script features in `Detail.cshtml` are:
  1. Variant selection & dynamic price formatting (`selectVariant`)
  2. Quantity increment/decrement (`changeQty`)
  3. Add to cart invocation (`handleAddToCart` calling `addToCart` from `_Layout.cshtml`)
  4. Flash sale countdown timer with auto-revert to base price upon expiry (`updateDetailTimer`)
  5. Customer review AJAX form submission (`customerReviewForm` submit listener)

### 1.3 Full Codebase Scan of All Razor Views
A recursive scan of all 37 `.cshtml` files across `src/Perfume.Web` was executed:
```powershell
Get-ChildItem -Path "src\Perfume.Web" -Filter "*.cshtml" -Recurse | ForEach-Object {
    $filePath = $_.FullName
    $relPath = Resolve-Path -Relative $filePath
    $lines = Get-Content $filePath
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '^\s*@section\s+([A-Za-z0-9_]+)') {
            [PSCustomObject]@{ File = $relPath; Line = ($i + 1); Section = $matches[1] }
        }
    }
}
```

Result Table of all `@section` declarations across the application:
| File | Line | Section Name | Status |
|------|------|--------------|--------|
| `src\Perfume.Web\Areas\Admin\Views\AdminDashboard\Erd.cshtml` | 276 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Areas\Admin\Views\DatabaseConsole\Index.cshtml` | 348 | Styles | OK (1 of 1 Styles) |
| `src\Perfume.Web\Areas\Admin\Views\DatabaseConsole\Index.cshtml` | 371 | Scripts | OK (1 of 1 Scripts) |
| `src\Perfume.Web\Views\Account\EditProfile.cshtml` | 22 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Account\ForgotPassword.cshtml` | 20 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Account\Login.cshtml` | 91 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Account\MyOrders.cshtml` | 13 | Styles | OK (1 of 1 Styles) |
| `src\Perfume.Web\Views\Account\MyOrders.cshtml` | 21 | Scripts | OK (1 of 1 Scripts) |
| `src\Perfume.Web\Views\Account\OrderDetails.cshtml` | 11 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Account\Register.cshtml` | 113 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Account\ResetPassword.cshtml` | 24 | Scripts | OK (1 of 1) |
| `src\Perfume.Web\Views\Cart\Index.cshtml` | 99 | Scripts | OK (1 of 1 Scripts) |
| `src\Perfume.Web\Views\Cart\Index.cshtml` | 109 | Styles | OK (1 of 1 Styles) |
| **`src\Perfume.Web\Views\Catalog\Detail.cshtml`** | **288** | **Scripts** | **DUPLICATE #1 (VIOLATION)** |
| **`src\Perfume.Web\Views\Catalog\Detail.cshtml`** | **342** | **Scripts** | **DUPLICATE #2 (VIOLATION)** |
| `src\Perfume.Web\Views\Catalog\Quiz.cshtml` | 118 | Styles | OK (1 of 1 Styles) |
| `src\Perfume.Web\Views\Catalog\Quiz.cshtml` | 138 | Scripts | OK (1 of 1 Scripts) |
| `src\Perfume.Web\Views\Checkout\Index.cshtml` | 123 | Scripts | OK (1 of 1 Scripts) |
| `src\Perfume.Web\Views\Checkout\Index.cshtml` | 189 | Styles | OK (1 of 1 Styles) |

**Conclusion from scan:** `src/Perfume.Web/Views/Catalog/Detail.cshtml` is the **ONLY** file in the entire project that contains duplicate `@section` declarations. All other views have zero duplicate sections.

---

## 2. Logic Chain

1. **Origin of Bug**: In commit `5c1f381` ("feat(customer): complete customer API and purchase flows"), the customer reviews async submit logic was added to `Detail.cshtml` right above the JSON-LD script, enclosed inside `@section Scripts { ... }` (lines 288-308). The developer did not notice that `Detail.cshtml` already had an `@section Scripts { ... }` block at the bottom of the file (lines 342-421) handling variant selection and flash sale timers.
2. **Runtime Failure Mechanism in ASP.NET Core Razor**:
   - In ASP.NET Core Razor Pages/MVC, the page execution engine calls `DefineSection("Scripts", ...)`.
   - If `DefineSection` is invoked more than once with the same section name in a single view, Razor throws:
     `InvalidOperationException: The section 'Scripts' has already been defined for the page '/Views/Catalog/Detail.cshtml'`.
   - Because `dotnet build` only checks compilation syntax of C# expressions in Razor files and does not evaluate runtime section dictionary collisions, the build succeeded, masking the runtime 500 server error when visiting any product detail page.
3. **Consolidation Architecture**:
   - The first `@section Scripts { ... }` block (lines 288-308) must be eliminated completely.
   - The JSON-LD script (lines 310-340) must remain directly in the HTML document body after the reviews `<section>` element. JSON-LD is semantic markup intended for web crawlers, not executable client script, so it belongs in HTML body/head, not inside `@section Scripts`.
   - The single `@section Scripts { ... }` block must be placed at the very bottom of `Detail.cshtml`.
   - Inside `<script>`, all 5 capabilities must be preserved in clean, modular sections:
     - Section 1: Variant Selection State & Handler (`selectedVariantId`, `selectVariant(btn)`) — must remain top-level/global for inline `onclick` handler.
     - Section 2: Quantity Adjustment (`changeQty(delta)`) — global for inline `onclick`.
     - Section 3: Add to Cart Handler (`handleAddToCart()`) — global for inline `onclick`.
     - Section 4: Flash Sale Countdown Timer (IIFE with interval) — scoped to avoid namespace pollution.
     - Section 5: Customer Reviews Form Submit Listener (`document.getElementById('customerReviewForm')?.addEventListener(...)`) — DOM event listener.

---

## 3. Caveats

1. **Image Zoom**: No separate JS image zoom function exists in the repository. The image utilizes CSS transition styling (`transition: transform 0.4s ease;`). Do not attempt to inject unrequested third-party zoom libraries that could conflict with layout CSS.
2. **Inline Event Handlers**: Because `Detail.cshtml` uses inline attributes (`onclick="selectVariant(this)"`, `onclick="changeQty(-1)"`, `onclick="handleAddToCart()"`), those functions must be exposed in the window/global scope inside the `<script>` tag and NOT wrapped inside a self-invoking function closure `(function() { ... })()`.
3. **Compiler Limitation**: `dotnet build` does not validate duplicate Razor `@section` blocks. Automated PowerShell/grep checks or runtime integration tests are required to guard against regressions.

---

## 4. Conclusion & Concrete Recommended Fix

### 4.1 Recommended Blueprint for Implementer Agent
In `src/Perfume.Web/Views/Catalog/Detail.cshtml`:
- **Delete** lines 288–308 (the first `@section Scripts` block).
- **Keep** lines 310–340 (the JSON-LD script).
- **Replace** lines 342–421 with the consolidated single `@section Scripts` block containing all 5 features.

### 4.2 Exact Replacement Snippet (From line 286 to end of file)

```razor
    }
</section>

<!-- GOOGLE STRUCTURED DATA (JSON-LD) FOR SEO -->
<script type="application/ld+json">
{
  "@@context": "https://schema.org/",
  "@@type": "Product",
  "name": "@Model.Name",
  "image": ["@Model.MainImageUrl"],
  "description": "@(Model.MetaDescription ?? Model.ShortDescription)",
  "sku": "@Model.Sku",
  "brand": {
    "@@type": "Brand",
    "name": "@Model.BrandName"
  },
  "offers": {
    "@@type": "Offer",
    "url": "@($"{Context.Request.Scheme}://{Context.Request.Host}{Context.Request.Path}")",
    "priceCurrency": "VND",
    "price": "@Model.BasePrice",
    "availability": "https://schema.org/InStock",
    "seller": {
      "@@type": "Organization",
      "name": "PERFUME LUXE"
    }
  },
  "aggregateRating": {
    "@@type": "AggregateRating",
    "ratingValue": "@Model.RatingAverage",
    "reviewCount": "@Math.Max(1, Model.RatingCount)"
  }
}
</script>

@section Scripts {
    <script>
        // --- 1. Variant Selection & Pricing ---
        let selectedVariantId = @(defaultVariant?.Id.ToString() ?? "null");

        function selectVariant(btn) {
            document.querySelectorAll('.variant-btn').forEach(b => {
                b.style.borderColor = 'var(--border-card)';
                b.style.background = 'var(--bg-surface)';
            });

            btn.style.borderColor = 'var(--gold-primary)';
            btn.style.background = 'rgba(212, 175, 55, 0.15)';

            selectedVariantId = btn.getAttribute('data-id');
            const price = Number(btn.getAttribute('data-price'));
            const orig = Number(btn.getAttribute('data-orig'));

            document.getElementById('activePrice').innerText = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
            const origEl = document.getElementById('activeOriginalPrice');
            if (origEl) {
                if (orig > price) {
                    origEl.innerText = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(orig);
                    origEl.style.display = 'inline';
                } else {
                    origEl.style.display = 'none';
                }
            }
        }

        // --- 2. Quantity Selector ---
        function changeQty(delta) {
            const el = document.getElementById('buyQty');
            let val = parseInt(el.value) + delta;
            if (val < 1) val = 1;
            if (val > 10) val = 10;
            el.value = val;
        }

        // --- 3. Add to Cart ---
        function handleAddToCart() {
            const qty = parseInt(document.getElementById('buyQty').value) || 1;
            addToCart('@Model.Slug', selectedVariantId, qty);
        }

        // --- 4. Flash Sale Real-time Countdown Timer ---
        const flashEndTimeStr = '@(Model.IsFlashSale && Model.FlashSaleEndTime.HasValue ? Model.FlashSaleEndTime.Value.ToString("o") : "")';
        if (flashEndTimeStr) {
            (function() {
                const endTime = new Date(flashEndTimeStr).getTime();
                const timerEl = document.getElementById('detailCountdownDisplay');
                const bannerEl = document.getElementById('detailFlashSaleBanner');
                const priceEl = document.getElementById('activePrice');
                const origEl = document.getElementById('activeOriginalPrice');
                const basePrice = @Model.BasePrice;

                function updateDetailTimer() {
                    const now = new Date().getTime();
                    const diff = endTime - now;
                    if (diff <= 0) {
                        clearInterval(interval);
                        if (bannerEl) bannerEl.style.display = 'none';
                        if (priceEl) {
                            priceEl.innerText = basePrice.toLocaleString('vi-VN') + '₫';
                            priceEl.style.color = 'var(--gold-primary)';
                        }
                        if (origEl) origEl.style.display = 'none';
                        return;
                    }

                    const hours = Math.floor(diff / (1000 * 60 * 60));
                    const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                    const secs = Math.floor((diff % (1000 * 60)) / 1000);
                    if (timerEl) {
                        timerEl.innerText = `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
                    }
                }

                updateDetailTimer();
                const interval = setInterval(updateDetailTimer, 1000);
            })();
        }

        // --- 5. Customer Reviews AJAX Submission ---
        document.getElementById('customerReviewForm')?.addEventListener('submit', async event => {
            event.preventDefault();
            const form = event.currentTarget;
            const button = form.querySelector('button[type="submit"]');
            const message = document.getElementById('reviewApiMessage');
            button.disabled = true;
            message.hidden = true;
            try {
                const result = await CustomerApi.post(`/api/v1/products/${form.dataset.productId}/reviews`, new FormData(form));
                message.textContent = result.message;
                message.style.cssText = 'margin:12px 0;padding:12px;border-radius:8px;color:#52b788;background:rgba(82,183,136,.12)';
                form.reset();
            } catch (error) {
                message.textContent = error.message || 'Chưa gửi được đánh giá.';
                message.style.cssText = 'margin:12px 0;padding:12px;border-radius:8px;color:#f87171;background:rgba(239,68,68,.12)';
            } finally { message.hidden = false; button.disabled = false; }
        });
    </script>
}
```

---

## 5. Verification Method

### 5.1 Duplicate Section Check (PowerShell Command)
To independently verify that zero `.cshtml` files have duplicate `@section` blocks:
```powershell
Get-ChildItem -Path "src\Perfume.Web" -Filter "*.cshtml" -Recurse | ForEach-Object {
    $file = $_.FullName
    $lines = Get-Content $file
    $sections = @()
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i] -match '^\s*@section\s+([A-Za-z0-9_]+)') {
            $sections += [PSCustomObject]@{ File = $_.FullName; Line = ($i + 1); Section = $matches[1] }
        }
    }
    $sections
} | Group-Object File, Section | Where-Object { $_.Count -gt 1 }
```
**Pass condition**: Output must be completely empty (0 results).

### 5.2 Build Verification
```powershell
dotnet build
```
**Pass condition**: 0 Warnings, 0 Errors.

### 5.3 Test Suite Execution
```powershell
dotnet test
```
**Pass condition**: 100% tests pass.

### 5.4 Live View Rendering Verification
Run Kestrel or integration test requesting product detail URL:
```powershell
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5000/catalog/detail?slug=tf-tobacco-vanille"
```
**Pass condition**: HTTP 200 OK without any `InvalidOperationException: The section 'Scripts' has already been defined`.
