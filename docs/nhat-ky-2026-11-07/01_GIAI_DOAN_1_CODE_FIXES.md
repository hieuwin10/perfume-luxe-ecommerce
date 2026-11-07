# BÁO CÁO KỸ THUẬT & CODE FIXES GIAI ĐOẠN 1 (07/11/2026)
## Phân hệ: Storefront Core Shopping & Catalog & Admin Routing

---

### 1. Tổng Quan Các Lỗi Đã Được Sửa & Triển Khai (Deploy) Lên Somee

Trong ca làm việc hôm nay (07/11/2026), toàn bộ các lỗi chặn luồng mua sắm chính và quản trị của Giai đoạn 1 đã được sửa đổi, kiểm thử tự động 419/419 tests pass và đẩy lên Somee qua GitHub Actions:

- **Commit `5b15336`**: `fix(routing): map /admin va /admin/admindashboard toi AdminAnalyticsController.Index de sua loi 404`
- **Commit `7ad672b`**: `fix(admin): cho phep catalog perfumeluxe2026 trong DatabaseConsoleService va sua xung dot route Index`
- **Commit `0c116be`**: `fix(phase-1): hoan thien storefront cart, customer-api formatVnd va bo sung GET admin actions`

---

### 2. Chi Tiết Các Thay Đổi Mã Nguồn (Code Changes)

#### 2.1. Thêm hàm `formatVnd` vào Client API (`src/Perfume.Web/wwwroot/js/customer-api.js`)
- **Vấn đề**: `CustomerApi` thiếu phương thức `formatVnd`, dẫn đến khi mở hoặc tải Mini Cart Drawer thì trình duyệt ném ngoại lệ `TypeError: CustomerApi.formatVnd is not a function`.
- **Mã nguồn đã sửa**:
```javascript
window.CustomerApi = Object.freeze({
    get: (url) => request(url),
    post: (url, data) => request(url, { method: 'POST', body: data instanceof FormData ? data : JSON.stringify(data) }),
    put: (url, data) => request(url, { method: 'PUT', body: data instanceof FormData ? data : JSON.stringify(data) }),
    delete: (url) => request(url, { method: 'DELETE' }),
    request,
    escapeHtml: (value) => String(value ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch])),
    formatVnd: (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(Number(value) || 0)
});
```

#### 2.2. Khắc phục hàm `addToCart` & Chuẩn hóa `refreshCartState` (`src/Perfume.Web/Views/Shared/_Layout.cshtml`)
- **Vấn đề**:
  - `addToCart` gửi `{ slug: slug }` trong khi backend API `POST /api/v1/cart/items` yêu cầu `ProductSlug` -> sinh lỗi `400 Bad Request`.
  - `refreshCartState` đọc `res.data` trong khi `CustomerApi.get` trả về trực tiếp đối tượng giỏ hàng.
- **Mã nguồn đã sửa**:
```javascript
async function refreshCartState() {
    try {
        const res = await CustomerApi.get('/api/v1/cart');
        const cart = res && res.data ? res.data : res;
        const totalCount = cart?.totalCount ?? cart?.totalItems ?? (cart?.items ? cart.items.length : 0);

        const badge = document.getElementById('headerCartCount');
        if (badge) badge.textContent = totalCount;

        const mobileBadge = document.getElementById('mobileBottomCartBadge');
        if (mobileBadge) {
            mobileBadge.textContent = totalCount;
            mobileBadge.style.display = totalCount > 0 ? 'flex' : 'none';
        }

        const subtotalEl = document.getElementById('cartDrawerSubtotal');
        if (subtotalEl) {
            subtotalEl.textContent = CustomerApi.formatVnd(cart?.subTotal ?? cart?.finalTotal ?? 0);
        }

        const itemsContainer = document.getElementById('cartDrawerItems');
        if (!itemsContainer) return;

        if (!cart || !cart.items || cart.items.length === 0) {
            itemsContainer.innerHTML = `
                <div style="text-align: center; padding: 40px 0; color: var(--text-muted);">
                    <i class="fa-solid fa-bag-shopping" style="font-size: 3rem; margin-bottom: 12px; opacity: 0.3;"></i>
                    <p>Giỏ hàng đang trống.</p>
                </div>`;
            return;
        }

        itemsContainer.innerHTML = cart.items.map(item => `
            <div class="cart-item-row" style="display: flex; gap: 14px; padding: 14px 0; border-bottom: 1px solid var(--border-card);">
                <img src="${CustomerApi.escapeHtml(item.productImageUrl || '/images/placeholder-perfume.jpg')}" alt="${CustomerApi.escapeHtml(item.productName)}" style="width: 60px; height: 60px; object-fit: contain; background: var(--bg-surface-elevated); border-radius: var(--radius-sm); padding: 4px;" />
                <div style="flex-grow: 1;">
                    <div style="font-weight: 600; font-size: 0.88rem; color: var(--text-primary); margin-bottom: 2px;">${CustomerApi.escapeHtml(item.productName)}</div>
                    <div style="font-size: 0.75rem; color: var(--gold-primary); margin-bottom: 4px;">${CustomerApi.escapeHtml(item.variantLabel || item.variantName || 'Fullbox')} &bull; x${item.quantity}</div>
                    <div style="font-weight: 700; font-size: 0.85rem; color: var(--text-primary);">${CustomerApi.formatVnd(item.totalPrice ?? item.subTotal ?? (item.unitPrice * item.quantity))}</div>
                </div>
                <button type="button" onclick="removeCartItem(${item.productId}, ${item.variantId ?? 'null'})" class="icon-btn" style="width: 28px; height: 28px; color: #ef4444;" aria-label="Xóa"><i class="fa-solid fa-trash-can" style="font-size: 0.75rem;"></i></button>
            </div>
        `).join('');
    } catch (err) {
        console.warn('Could not refresh cart state:', err);
    }
}

async function addToCart(slug, variantId, qty = 1) {
    try {
        await CustomerApi.post('/api/v1/cart/items', {
            productSlug: slug,
            slug: slug,
            variantId: variantId,
            quantity: qty
        });
        await refreshCartState();
        openCart();
        showToast('Đã thêm nước hoa vào giỏ hàng!', 'success');
    } catch (err) {
        showToast(err.message || 'Không thể thêm vào giỏ hàng.', 'error');
    }
}
```

#### 2.3. Bổ sung fallback Slug trên DTO & Backend Service
- **File**: `src/Perfume.Application/DTOs/CustomerDtos.cs` & `src/Perfume.Infrastructure/Services/CustomerCartService.cs`.
- **Cơ chế**: Cho phép nhận cả `ProductSlug` và `Slug`. Nếu `ProductSlug` trống nhưng có `Slug`, backend tự động gán `ProductSlug = Slug` để tương thích 100% với các client cũ.

#### 2.4. Cấp quyền CSP cho thư viện CDN (`src/Perfume.Web/Program.cs`)
- **Vấn đề**: Content-Security-Policy chặn `cdn.jsdelivr.net` khiến Chart.js và Mermaid.js không thể nạp trên các trang quản trị.
- **Mã nguồn đã sửa**:
```csharp
"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com https://www.google-analytics.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
"style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
```

#### 2.5. Bổ sung các Action HTTP GET còn thiếu trong Admin Controllers
- **`AdminSystemController.cs`**:
  - `[HttpGet] Settings()`: Nạp cài đặt hệ thống và hiển thị View `Settings.cshtml`.
  - `[HttpGet] CreateBlog()`: Nạp danh mục blog và hiển thị form tạo bài viết mới.
  - `[HttpGet] EditBlog(int id)`: Nạp bài viết theo Id và hiển thị form chỉnh sửa.
- **`AdminInventoryController.cs`**:
  - `[HttpGet] CreateProduct()`: Nạp thương hiệu, danh mục, nhóm hương và hiển thị form tạo sản phẩm.
  - `[HttpGet] EditProduct(int id)`: Nạp sản phẩm cùng ảnh và biến thể, mapping vào `EditProductDto` để hiển thị form chỉnh sửa.

---

### 3. Kết Quả Kiểm Thử Sau Khi Sửa (Verification)
- **Tự động**: 419/419 unit & integration tests PASSED.
- **Live Chrome DevTools**:
  - `GET /` -> `200 OK`, `0 console errors`.
  - Thêm sản phẩm vào giỏ hàng -> `200 OK`, Mini Cart Drawer hiển thị mượt mà.
  - `GET /bo-suu-tap` -> `200 OK`, bộ lọc từ khóa hoạt động chuẩn.
  - `GET /cart` -> `200 OK`, bảng giỏ hàng tính tiền chuẩn.
  - `GET /checkout` -> `200 OK` (khi giỏ có hàng).

