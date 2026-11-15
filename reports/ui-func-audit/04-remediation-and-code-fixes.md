# Chuyên Đề 04: Phương Án Xử Lý & Bản Vá Mã Nguồn Cụ Thể (Remediation & Code Fixes)
**Dự án:** Perfume Luxe E-Commerce Platform  
**Công nghệ:** ASP.NET Core 8.0 MVC & Web API, Entity Framework Core 8, Vanilla JavaScript  
**Mục tiêu:** Cung cấp giải pháp sửa lỗi triệt để, an toàn và sẵn sàng triển khai (Production-ready) cho toàn bộ khiếm khuyết từ DEF-01 đến DEF-10.  

---

## 1. Bản Vá Khẩn Cấp DEF-01 (P0): Mâu Thuẫn Enum PaymentMethod Khi Đặt Hàng

### Nguyên nhân gốc rễ (Root Cause Analysis):
Trong `src/Perfume.Domain/Enums/PerfumeEnums.cs`, enum `PaymentMethod` được định nghĩa:
```csharp
public enum PaymentMethod
{
    COD = 1,          // Thanh toán khi nhận hàng
    VNPAY = 2,        // Cổng VNPAY QR
    MoMo = 3,         // Ví điện tử MoMo
    BankTransfer = 4  // Chuyển khoản ngân hàng trực tiếp (VietQR)
}
```
Tuy nhiên, trong `src/Perfume.Web/Views/Checkout/Index.cshtml` (dòng 293), client JavaScript lại truyền:
```javascript
const paymentMethod = document.getElementById('radioVietQR')?.checked ? 1 : 0; // 0=COD, 1=BankTransfer -> SAI NGUYÊN TẮC
```
- Khi chọn COD, client gửi `paymentMethod: 0`. Tầng backend `OrderService.cs` kiểm tra `if (request.PaymentMethod != PaymentMethod.COD && request.PaymentMethod != PaymentMethod.BankTransfer)` thấy giá trị `0` không thuộc tập giá trị hỗ trợ nên lập tức từ chối với thông báo: `"Phương thức thanh toán chưa được hỗ trợ."`
- Khi chọn VietQR, client gửi `paymentMethod: 1`. Backend nhận nhầm thành `COD` (vì COD = 1), khiến luồng điều hướng và xử lý giao dịch bị sai lệch.

### Mã nguồn cần chỉnh sửa:
**File ảnh hưởng:** `src/Perfume.Web/Views/Checkout/Index.cshtml` (dòng 293)

```diff
--- a/src/Perfume.Web/Views/Checkout/Index.cshtml
+++ b/src/Perfume.Web/Views/Checkout/Index.cshtml
@@ -290,7 +290,7 @@
             const isGift = document.getElementById('chkGiftWrapping')?.checked || false;
             const giftMsg = document.getElementById('txtGiftMessage')?.value || '';
-            const paymentMethod = document.getElementById('radioVietQR')?.checked ? 1 : 0; // 0=COD, 1=BankTransfer
+            // Chuẩn hóa đúng Enum: COD = 1, BankTransfer = 4 theo PerfumeEnums.cs
+            const paymentMethod = document.getElementById('radioVietQR')?.checked ? 4 : 1;
 
             try {
                 const result = await CustomerApi.post('/api/v1/orders', {
```

Đồng thời, tăng cường bẫy lỗi chi tiết hơn tại `src/Perfume.Infrastructure/Services/OrderService.cs`:
```csharp
// Thay vì trả về câu thông báo chung chung "Tồn kho có thể vừa thay đổi", ghi log chi tiết và phản hồi rõ ràng:
catch (Exception ex)
{
    _logger?.LogError(ex, "Lỗi xảy ra trong quá trình PlaceOrderAsync cho khách hàng {CustomerEmail}: {Message}", request.CustomerEmail, ex.Message);
    if (transaction != null) await transaction.RollbackAsync();
    return (false, "Không thể hoàn tất đơn hàng do gián đoạn xử lý giao dịch. Quý khách vui lòng thử lại hoặc chọn hình thức chuyển khoản VietQR.", null);
}
```

---

## 2. Bản Vá DEF-02 (P1): Triệt Tiêu Sập Máy Chủ HTTP 500 Tại DatabaseConsole

### Nguyên nhân gốc rễ (Root Cause Analysis):
Trong file `src/Perfume.Web/Areas/Admin/Controllers/DatabaseConsoleController.cs`:
- Tại dòng 15 khai báo route cấp Controller: `[Route("Admin/AdminDashboard/DatabaseConsole")]`
- Tại dòng 28 của Action `Index()` lại khai báo: `[HttpGet("/Admin/AdminDashboard/DatabaseConsole")]`
Cơ chế ASP.NET Core Endpoint Routing khi khởi tạo map hai route template trùng khít cho cùng một action, dẫn tới ném ra ngoại lệ `AmbiguousMatchException` ngay khi request vừa tới middleware routing.

### Mã nguồn cần chỉnh sửa:
**File ảnh hưởng:** `src/Perfume.Web/Areas/Admin/Controllers/DatabaseConsoleController.cs`

```diff
--- a/src/Perfume.Web/Areas/Admin/Controllers/DatabaseConsoleController.cs
+++ b/src/Perfume.Web/Areas/Admin/Controllers/DatabaseConsoleController.cs
@@ -12,7 +12,6 @@ namespace Perfume.Web.Areas.Admin.Controllers;
 [Area("Admin")]
 [Authorize(Roles = "Admin")]
 [Route("Admin/[controller]/[action]")]
-[Route("Admin/AdminDashboard/DatabaseConsole")]
 public class DatabaseConsoleController : Controller
 {
     private readonly IDatabaseConsoleService _dbConsole;
@@ -25,7 +24,6 @@ public class DatabaseConsoleController : Controller
 
     [HttpGet]
     [HttpGet("/Admin/DatabaseConsole")]
     [HttpGet("/Admin/AdminDashboard/DatabaseConsole")]
     public async Task<IActionResult> Index(string? table, int page = 1, int pageSize = 15, string? sortBy = null, bool asc = true)
```

---

## 3. Bản Vá DEF-03 (P2): Bổ Sung Action Index Cho CatalogManagerController

### Nguyên nhân gốc rễ:
`CatalogManagerController.cs` quản lý các thực thể thương hiệu, nhóm hương và nốt hương nhưng thiếu action mặc định `Index()`, khiến URL `/Admin/CatalogManager` trả về HTTP 404 Not Found.

### Mã nguồn cần chỉnh sửa:
**File ảnh hưởng:** `src/Perfume.Web/Areas/Admin/Controllers/CatalogManagerController.cs`

```csharp
// Thêm action Index điều hướng về trang Brands làm trang danh mục mặc định:
[HttpGet]
public IActionResult Index()
{
    return RedirectToAction(nameof(Brands));
}
```

---

## 4. Bản Vá DEF-04 (P2): Đồng Bộ Mã Giảm Giá Khuyến Mãi Gợi Ý (LUXE2026, WELCOME)

### Nguyên nhân gốc rễ:
Trang checkout gợi ý người dùng nhập `LUXE2026` hoặc `WELCOME`, nhưng trong cơ sở dữ liệu `Vouchers` chưa có dữ liệu của 2 mã này hoặc chưa được kích hoạt với giá trị `MinOrderAmount = 0`.

### Mã nguồn cần chỉnh sửa:
**Thêm seed dữ liệu Voucher vào cơ sở dữ liệu:**
```csharp
// Thêm vào DbInitializer hoặc script seeding:
if (!await _db.Vouchers.AnyAsync(v => v.Code == "LUXE2026"))
{
    _db.Vouchers.Add(new Voucher
    {
        Code = "LUXE2026",
        Description = "Ưu đãi 10% đặc quyền thành viên Haute Parfumerie Club",
        DiscountPercent = 10,
        MinOrderAmount = 0,
        MaxDiscountAmount = 500_000,
        UsageLimit = 10_000,
        UsedCount = 0,
        ValidFrom = DateTime.UtcNow.AddDays(-30),
        ValidTo = DateTime.UtcNow.AddYears(2),
        IsActive = true
    });
    await _db.SaveChangesAsync();
}

if (!await _db.Vouchers.AnyAsync(v => v.Code == "WELCOME"))
{
    _db.Vouchers.Add(new Voucher
    {
        Code = "WELCOME",
        Description = "Ưu đãi chào đón khách hàng mới giảm ngay 100.000₫",
        DiscountAmount = 100_000,
        MinOrderAmount = 500_000,
        UsageLimit = 10_000,
        UsedCount = 0,
        ValidFrom = DateTime.UtcNow.AddDays(-30),
        ValidTo = DateTime.UtcNow.AddYears(2),
        IsActive = true
    });
    await _db.SaveChangesAsync();
}
```

---

## 5. Bản Vá DEF-05 (P2): Cấu Hình Rõ Ràng AccessDeniedPath Cho Cookie Auth

### Nguyên nhân gốc rễ:
Khi người dùng chưa đủ quyền truy cập khu vực Admin (`Roles = "Admin"`), mặc định ASP.NET Core chuyển hướng về `LoginPath`, và tại login người dùng đã xác thực lại bị đẩy về `/` gây vòng lặp im lặng.

### Mã nguồn cần chỉnh sửa:
**File ảnh hưởng:** `src/Perfume.Web/Program.cs`

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Thêm cấu hình tường minh
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

---

## 6. Bản Vá DEF-06 (P3): Tách Biệt Tiêu Đề Báo Lỗi Tra Cứu Khi Thiếu SĐT

### Nguyên nhân gốc rễ:
View `OrderTracking/Index.cshtml` hiển thị cùng tiêu đề "Không Tìm Thấy Thông Tin Đơn Hàng" cho cả 2 trường hợp: không tìm thấy đơn và thiếu số điện thoại xác thực.

### Mã nguồn cần chỉnh sửa:
**File ảnh hưởng:** `src/Perfume.Web/Views/OrderTracking/Index.cshtml`

```diff
--- a/src/Perfume.Web/Views/OrderTracking/Index.cshtml
+++ b/src/Perfume.Web/Views/OrderTracking/Index.cshtml
@@ -45,7 +45,11 @@
               <i class="fa-solid fa-circle-exclamation" style="font-size: 2.2rem; color: #f87171; margin-bottom: 12px; display: block;"></i>
-              <h3 style="margin: 0 0 8px; font-size: 1.15rem; color: var(--text-primary);">Không Tìm Thấy Thông Tin Đơn Hàng</h3>
+              @if (string.IsNullOrWhiteSpace(ViewBag.Phone))
+              {
+                  <h3 style="margin: 0 0 8px; font-size: 1.15rem; color: var(--text-primary);">Vui Lòng Nhập Thêm Số Điện Thoại Để Xác Thực</h3>
+              }
+              else
+              {
+                  <h3 style="margin: 0 0 8px; font-size: 1.15rem; color: var(--text-primary);">Không Tìm Thấy Thông Tin Đơn Hàng</h3>
+              }
               <p style="color: var(--text-muted); font-size: 0.9rem; margin: 0 0 16px;">@ViewBag.ErrorMessage</p>
```

---

## 7. Bản Vá DEF-07 (P3): Loại Bỏ Lồng Thẻ `<main>` Vi Phạm HTML5 / WCAG

### Mã nguồn cần chỉnh sửa:
1. **File `src/Perfume.Web/Views/Catalog/DiscoveryBox.cshtml`:**
   Đổi thẻ bọc `<main style="max-width: 1360px; ...">` thành `<div class="discovery-box-container" style="max-width: 1360px; ...">`.
2. **File `src/Perfume.Web/Views/Experience/TastingBooking.cshtml`:**
   Đổi thẻ `<main style="max-width: 1100px; ...">` thành `<div class="tasting-booking-container" style="max-width: 1100px; ...">`.
3. **File `src/Perfume.Web/Views/Blog/Detail.cshtml`:**
   Đổi `<main class="blog-content-col">` thành `<article class="blog-content-col">` (chuẩn ngữ nghĩa cho bài viết chi tiết).

---

## 8. Bản Vá DEF-08 (P3): Bổ Sung Banner Placeholder Cho Flash Sale

### Mã nguồn cần chỉnh sửa:
**File `src/Perfume.Web/Views/Home/Index.cshtml`:**
```html
<div id="flashSaleContainer" style="display: none;">
    <!-- Khối Flash Sale động khi API trả về 200 -->
</div>
<div id="flashSalePlaceholder" class="luxury-card" style="margin: 40px auto; max-width: 1280px; padding: 24px; text-align: center; border: 1px dashed var(--gold-primary); background: rgba(212,175,55,0.03);">
    <p style="color: var(--gold-primary); font-size: 1.1rem; margin: 0; font-weight: 600;">
        <i class="fa-solid fa-clock" style="margin-right: 8px;"></i> Phiên Flash Sale Haute Parfumerie tiếp theo đang được chuẩn bị.
    </p>
    <p style="color: var(--text-muted); font-size: 0.88rem; margin: 8px 0 0;">
        Đăng ký thành viên bên dưới để nhận thông báo sớm nhất cùng đặc quyền giảm 10%!
    </p>
</div>
```

---

## 9. Bản Vá DEF-09 (P3): Việt Hóa Trích Xuất Lỗi ProblemDetails Client-side

### Mã nguồn cần chỉnh sửa:
**File `src/Perfume.Web/wwwroot/js/customer-api.js`:**
```javascript
// Cải tiến hàm parse lỗi trong CustomerApi:
function extractErrorMessage(payload) {
    if (!payload) return 'Đã xảy ra lỗi không xác định. Vui lòng thử lại.';
    if (payload.message) return payload.message;
    if (payload.errors && typeof payload.errors === 'object') {
        const errorList = Object.values(payload.errors).flat();
        if (errorList.length > 0) return errorList.join(' ');
    }
    if (payload.title && payload.title !== 'One or more validation errors occurred.') {
        return payload.title;
    }
    return 'Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại các trường thông tin.';
}
```

---

## 10. Bản Vá DEF-10 (P4): Thêm `rel="noopener noreferrer"` Cho Link Tab Mới

### Mã nguồn cần chỉnh sửa:
**File `src/Perfume.Web/Views/Checkout/Index.cshtml` (dòng 221-224):**

```diff
--- a/src/Perfume.Web/Views/Checkout/Index.cshtml
+++ b/src/Perfume.Web/Views/Checkout/Index.cshtml
@@ -221,4 +221,4 @@
                         <span>
                             Tôi đã đọc, hiểu rõ và tự nguyện đồng ý với 
-                            <a href="/huong-dan-mua-hang" target="_blank" style="color: var(--gold-primary); text-decoration: underline;">Điều khoản mua hàng</a> 
-                            và <a href="/privacy" target="_blank" style="color: var(--gold-primary); text-decoration: underline;">Chính sách bảo vệ dữ liệu cá nhân</a> 
+                            <a href="/huong-dan-mua-hang" target="_blank" rel="noopener noreferrer" style="color: var(--gold-primary); text-decoration: underline;">Điều khoản mua hàng</a> 
+                            và <a href="/privacy" target="_blank" rel="noopener noreferrer" style="color: var(--gold-primary); text-decoration: underline;">Chính sách bảo vệ dữ liệu cá nhân</a> 
                             của PERFUME LUXE theo Luật 91/2025/QH15.
                         </span>
```
