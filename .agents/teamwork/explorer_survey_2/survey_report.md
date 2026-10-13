# BÁO CÁO KHẢO SÁT CHI TIẾT TẦNG TRÌNH DIỄN WEB (PRESENTATION LAYER SURVEY)
**PERFUME LUXE - HAUTE PARFUMERIE ENTERPRISE SYSTEM**

- **Người thực hiện**: `explorer_survey_2` (Exploration Specialist)
- **Thời gian hoàn thành**: 2026-10-12
- **Mục tiêu**: Khảo sát toàn diện Controllers, Razor Views, ViewModels, Client Scripts, Theme CSS và xác định các điểm tác động UI cho 3 tính năng trọng tâm: R1 (Flash Sale), R2 (Loyalty VIP Tier), R3 (Custom Discovery Gift Box Builder).

---

## MỤC LỤC
1. [Tổng Quan Cấu Trúc Tầng Trình Diễn Web](#1-tổng-quan-cấu-trúc-tầng-trình-diễn-web)
2. [Hệ Thống Layout, Phong Cách Thiết Kế & Client-Side Assets](#2-hệ-thống-layout-phong-cách-thiết-kế--client-side-assets)
3. [Khảo Sát Chi Tiết Các Controllers & Razor Views Hiện Hữu](#3-khảo-sát-chi-tiết-các-controllers--razor-views-hiện-hữu)
   - 3.1. Trang Chủ (`HomeController` & `Views/Home/Index.cshtml`)
   - 3.2. Danh Mục & Chi Tiết Sản Phẩm (`CatalogController` & `Views/Catalog/`)
   - 3.3. Giỏ Hàng & Thanh Toán (`CartController`, `CheckoutController` & `Views/Cart/`, `Views/Checkout/`)
   - 3.4. Quản Lý Tài Khoản Khách Hàng (`AccountController` & `Views/Account/`)
   - 3.5. Bảng Điều Khiển Quản Trị Admin (`AdminDashboardController`, `StoreManagerController` & `Areas/Admin/Views/`)
4. [Kế Hoạch & Điểm Tác Động Giao Diện Cho Tính Năng R1: Flash Sale & Real-time Countdown](#4-kế-hoạch--điểm-tác-động-giao-diện-cho-tính-năng-r1-flash-sale--real-time-countdown)
5. [Kế Hoạch & Điểm Tác Động Giao Diện Cho Tính Năng R2: Thẻ VIP Kỹ Thuật Số & Chiết Khấu Tự Động](#5-kế-hoạch--điểm-tác-động-giao-diện-cho-tính-năng-r2-thẻ-vip-kỹ-thuật-số--chiết-khấu-tự-động)
6. [Kế Hoạch & Điểm Tác Động Giao Diện Cho Tính Năng R3: Trình Thiết Kế Hộp Quà Discovery](#6-kế-hoạch--điểm-tác-động-giao-diện-cho-tính-năng-r3-trình-thiết-kế-hộp-quà-discovery)
7. [Tổng Hợp Ma Trận File & Điểm Chạm Giao Diện (Touchpoint Matrix)](#7-tổng-hợp-ma-trận-file--điểm-chạm-giao-diện-touchpoint-matrix)

---

## 1. TỔNG QUAN CẤU TRÚC TẦNG TRÌNH DIỄN WEB

Dự án áp dụng mô hình kiến trúc phân tầng chuẩn .NET 8 (Clean/Layered Architecture) với 4 projects:
- `Perfume.Domain`: Định nghĩa Entities (`Order`, `OrderItem`, `Product`, `ProductVariant`, `UserLoyalty`, `Voucher`...), Enums (`UserTier`, `OrderStatus`, `PaymentStatus`...).
- `Perfume.Application`: Chứa Interfaces (`ICustomerCartService`, `IOrderService`, `IPerfumeCatalogService`...) và DTOs (`CartStateDto`, `CartItemDto`, `ProductDetailDto`, `CustomerOrderDto`...).
- `Perfume.Infrastructure`: EF Core `PerfumeDbContext`, Entity Configurations, `CustomerCartService`, `OrderService`, `DatabaseSchemaUpgrader`.
- `Perfume.Web`: Tầng trình diễn chính:
  - Controllers MVC truyền thống phục vụ Razor Views SSR (Server-Side Rendering).
  - API Controller đồng bộ (`CustomerApiController.cs` với tiền tố route `api/v1/...`).
  - Phân vùng quản trị độc lập: `Areas/Admin/Controllers` (`AdminDashboardController.cs`, `StoreManagerController.cs`, `DatabaseConsoleController.cs`).
  - Thư mục Razor Views: `Views/Home`, `Views/Catalog`, `Views/Cart`, `Views/Checkout`, `Views/Account`, `Views/Shared`.
  - Static Assets: `wwwroot/css/luxury-perfume.css`, `wwwroot/js/customer-api.js`, `wwwroot/js/site.js`.

---

## 2. HỆ THỐNG LAYOUT, PHONG CÁCH THIẾT KẾ & CLIENT-SIDE ASSETS

### 2.1. Ngôn Ngữ Thiết Kế (Luxury Design System)
File stylesheet chính: `src/Perfume.Web/wwwroot/css/luxury-perfume.css` (752 dòng).
- **Typography**:
  - Tiêu đề (Headings): `'Cormorant Garamond', Georgia, serif` (phong cách quý tộc Haute Parfumerie).
  - Nội dung (Body): `'Plus Jakarta Sans', sans-serif` (hiện đại, tinh tế, dễ đọc).
- **Màu sắc & Tokens**:
  - Vàng Champagne Hoàng gia: `--gold-primary: #D4AF37`, `--gold-light: #F3E5AB`, `--gold-dark: #997A15`.
  - Gradient: `--gold-gradient: linear-gradient(135deg, #ECC86E 0%, #D4AF37 50%, #997A15 100%)`.
  - Nền Dark Theme (Mặc định): `--bg-main: #0B0B0E`, `--bg-surface: #141419`, `--bg-surface-elevated: #1D1D24`.
  - Nền Light Theme: `--bg-main: #FAF8F5`, `--bg-surface: #FFFFFF`.
  - Viền & Hiệu ứng đổ bóng: `--border-subtle: rgba(212, 175, 55, 0.15)`, `--shadow-luxury: 0 12px 40px rgba(0, 0, 0, 0.6), 0 0 15px rgba(212, 175, 55, 0.15)`, `--glow-gold: 0 0 25px rgba(212, 175, 55, 0.25)`.
- **Thành phần UI cốt lõi (Reusable Components)**:
  - `.btn-luxe`: Nút viền vàng bo tròn hoàn toàn, nền vàng gradient, chữ đen sang trọng, đổ bóng nổi bật.
  - `.btn-luxe-outline`: Nút viền vàng trong suốt, tương tác hover chuyển vàng nhạt.
  - `.luxury-card`: Card nền kính mờ (`var(--bg-surface)`), viền mảnh thẻ sang trọng, bo góc `var(--radius-md)` (12px).
  - `.badge-tag`: Nhãn nổi bật (`tag-hot`, `tag-new`), bo góc tròn.
  - `.toast-container-luxe` & `.toast-luxe`: Thông báo đẩy nổi ở góc màn hình.

### 2.2. Master Layouts
- **Storefront Layout** (`src/Perfume.Web/Views/Shared/_Layout.cshtml`):
  - Top Announcement Bar: Cam kết chính hãng, miễn phí giao hàng đơn từ 2.000.000₫.
  - Header Navigation (`.luxe-header`): Logo thương hiệu vàng ánh kim `PERFUME LUXE`, menu điều hướng, nút đổi Theme (Dark/Light), Mini Cart Trigger (`#cartDrawerBtn` kèm huy hiệu `#headerCartCount`), Dropdown Avatar người dùng (`#userDropdown`).
  - Mini Cart Drawer (`.mini-cart-drawer`): Trượt từ cạnh phải khi nhấn icon giỏ hàng hoặc sau khi thêm sản phẩm, hiển thị danh sách sản phẩm, tạm tính và 2 nút hành động (Tiến hành thanh toán `/checkout`, Xem giỏ hàng `/cart`).
  - Global JS Helpers: `showToast(msg, type)`, `openCart()`, `closeCart()`, `refreshCartState()`, `addToCart(slug, variantId, qty)`, `removeCartItem(productId, variantId)`.
- **Admin Layout** (`src/Perfume.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`):
  - Thanh Admin Console Navigation (`#111116`, viền vàng mờ `rgba(212, 175, 55, 0.2)`).
  - Menu các mục quản trị: Tổng Quan (`/admin/admindashboard`), Thống Kê (`/admin/storemanager`), Sản Phẩm (`/admin/admindashboard/products`), Đơn Hàng (`/admin/admindashboard/orders`), Phân Quyền (`/admin/admindashboard/users`), Voucher (`/admin/storemanager/vouchers`), Đánh Giá (`/admin/storemanager/reviews`), Sơ Đồ ERD (`/admin/admindashboard/erd`), DB Console (`/admin/databaseconsole`).
  - Đổi Theme Admin Dark/Light độc lập, nút truy cập Cửa hàng (`/`), nút Đăng xuất.

### 2.3. Khách Thể Giao Tiếp API (`customer-api.js`)
File `src/Perfume.Web/wwwroot/js/customer-api.js`:
- Tự động đính kèm Antiforgery CSRF Token từ thẻ `input[name="__RequestVerificationToken"]` vào request headers (`RequestVerificationToken`).
- Cung cấp:
  - `CustomerApi.get(url)`
  - `CustomerApi.post(url, data)`
  - `CustomerApi.put(url, data)`
  - `CustomerApi.delete(url)`
  - `CustomerApi.escapeHtml(str)`: Khử ký tự độc hại XSS trước khi chèn chuỗi HTML.

---

## 3. KHẢO SÁT CHI TIẾT CÁC CONTROLLERS & RAZOR VIEWS HIỆN HỮU

### 3.1. Trang Chủ (`HomeController` & `Views/Home/Index.cshtml`)
- **Controller** (`HomeController.cs`):
  - Inject `IPerfumeCatalogService` và `IBlogService`.
  - Action `Index()`: Nạp `ViewBag.FeaturedProducts`, `ViewBag.BestSellers`, `ViewBag.NewArrivals`, `ViewBag.Brands`, `ViewBag.Families`, `ViewBag.RecentBlogs`.
- **View** (`Views/Home/Index.cshtml`):
  - Hero Banner (dòng 10-31): Nền radial-gradient hoàng gia, slogan *"Bản Giao Hưởng Mùi Hương Bất Hủ"*, 2 nút CTA "Khám Phá Bộ Sưu Tập" (`/bo-suu-tap`) và "Trắc Nghiệm Tìm Mùi" (`/catalog/quiz`).
  - Brand Logo Strip (dòng 34-44): DIOR, CHANEL, CREED, TOM FORD, LE LABO, KILIAN, MAISON MARGIELA.
  - Featured Perfumes Grid (dòng 47-122): Lưới sản phẩm dạng thẻ `.perfume-card`, hiển thị nhãn `Best Seller`, `Mới Về`, ảnh đại diện, thương hiệu, tên, các nốt hương đầu (`card-notes`), giá từ `BasePrice`, và nút "Thêm vào giỏ" nhanh (`addToCart(slug, null, 1)`).
  - Fragrance Finder Quiz Teaser (dòng 124-156).
  - Recent Blog Posts (dòng 158-190).

### 3.2. Danh Mục & Chi Tiết Sản Phẩm (`CatalogController` & `Views/Catalog/`)
- **Controller** (`CatalogController.cs`):
  - `Index(ProductFilterParams filter)`: Hỗ trợ lọc theo Keyword, Gender, BrandSlug, FamilySlug, SortBy (`price_asc`, `price_desc`, `rating`).
  - `Detail(string slug)`: Trả về `ProductDetailDto`, đồng thời nạp 4 sản phẩm liên quan (`ViewBag.RelatedProducts`) và danh sách đánh giá đã duyệt (`ViewBag.ApprovedReviews`).
- **View Danh Mục** (`Views/Catalog/Index.cshtml`):
  - Sidebar bộ lọc cố định (`<aside>` sticky): Tìm kiếm theo từ khóa, giới tính, thương hiệu, nhóm hương, sắp xếp giá.
  - Lưới sản phẩm 3 cột (`.perfume-card`), phân trang hoặc hiển thị tổng số kết quả.
- **View Chi Tiết Sản Phẩm** (`Views/Catalog/Detail.cshtml`):
  - Trình bày 2 cột chính:
    - Cột trái: Ảnh sản phẩm lớn zoom mềm mại (`#mainProductImg`), huy hiệu cam kết (100% Chính hãng, Hỏa tốc 2H, Đổi trả 7 ngày).
    - Cột phải: Thương hiệu, quốc gia, tên, đánh giá sao, nồng độ (EDP/Parfum/EDT), giới tính.
    - Khung giá: `#activePrice` và giá gốc gạch ngang `#activeOriginalPrice`.
    - Bộ chọn phiên bản/dung tích (`.variant-btn`): 2ml, 10ml, 50ml, 100ml. Click kích hoạt hàm JS `selectVariant(this)` cập nhật giá và lưu `selectedVariantId`.
    - Bộ tăng giảm số lượng (`#buyQty`) và nút "Thêm Vào Giỏ Hàng" (`handleAddToCart()`).
    - Bảng thông số: Độ lưu hương (`Longevity`), Độ tỏa hương (`Sillage`), Mùa khuyên dùng (`SeasonRecommendation`).
  - Khối trực quan kim tự tháp 3 tầng hương (Fragrance Pyramid Visualizer: Hương Đầu, Hương Giữa, Hương Cuối).
  - Khu vực đánh giá khách hàng (Review form có upload ảnh và danh sách review đã duyệt).

### 3.3. Giỏ Hàng & Thanh Toán (`CartController`, `CheckoutController` & `Views/Cart/`, `Views/Checkout/`)
- **Giỏ Hàng** (`CartController.cs` & `Views/Cart/Index.cshtml`):
  - Quản lý qua `ICustomerCartService` dựa trên Cookie `plx_guest_cart` hoặc `CurrentUserId()`.
  - `Views/Cart/Index.cshtml`: Bảng liệt kê sản phẩm với ảnh, tên, dung tích (`VariantLabel`), đơn giá, bộ cộng trừ số lượng (`updateItemQty()`), thành tiền, nút xóa (`removeCartItem()`). Thẻ tóm tắt đơn hàng bên phải tính tạm tính (`subTotal`), phí giao hàng (đơn >= 2.000.000₫ miễn phí, ngược lại 35.000₫), tổng cộng và nút "Tiến Hành Thanh Toán".
- **Thanh Toán** (`CheckoutController.cs` & `Views/Checkout/Index.cshtml`):
  - Giao diện 2 cột:
    - Cột trái: Form thông tin người nhận (Họ tên, SĐT, Email, Tỉnh/TP, Địa chỉ chi tiết, Ghi chú đơn hàng). Phương thức thanh toán COD độc quyền.
    - Cột phải: Danh sách sản phẩm thu nhỏ, ô nhập mã Voucher (`#voucherCodeInput`) kèm nút "Áp Dụng" gọi `CustomerApi.post('/api/v1/vouchers/validate')`.
    - Bảng tính tiền: Tạm tính (`#displaySubTotal`), Giảm giá (`#displayDiscount`), Phí vận chuyển, Tổng thanh toán (`#displayTotal`).
  - JavaScript submit: Bắt sự kiện submit form, gửi POST payload tới `CustomerApi.post('/api/v1/orders')`, sau đó chuyển hướng tới `SuccessUrl` (hoặc `/account/myorders`).

### 3.4. Quản Lý Tài Khoản Khách Hàng (`AccountController` & `Views/Account/`)
- **Controller** (`AccountController.cs`):
  - `Profile()`: Lấy `AppUser` qua `_userManager.GetUserAsync(User)` và danh sách Roles qua `ViewBag.Roles`.
  - `MyOrders()` & `OrderDetails(int id)`: Trả về view để client tải dữ liệu qua `CustomerApi.get('/api/v1/customer/orders')`.
- **View Hồ Sơ** (`Views/Account/Profile.cshtml`):
  - Header: Avatar chữ cái đầu, họ tên, email, role badge (`role-badge-customer` / `role-badge-admin`), nút Sửa hồ sơ, nút Đăng xuất.
  - Hàng thẻ thông tin:
    - Thẻ 1: Hạng thành viên (`@Model.Tier`) và Điểm thưởng (`@Model.LoyaltyPoints`).
    - Thẻ 2: Ngày gia nhập (`@Model.CreatedAt`).
    - Thẻ 3: Lịch sử mua hàng kèm nút xem đơn hàng (`/account/myorders`).
- **View Chi Tiết Đơn Hàng** (`Views/Account/OrderDetails.cshtml`):
  - Tải động chi tiết đơn qua `/api/v1/customer/orders/{id}`: Mã đơn, ngày tạo, trạng thái (`OrderStatus`), thanh toán (`PaymentStatus`), danh sách sản phẩm, thông tin giao nhận, mã vận đơn (`trackingNumber`), tạm tính, giảm giá, phí ship, tổng tiền.

### 3.5. Bảng Điều Khiển Quản Trị Admin (`AdminDashboardController`, `StoreManagerController` & `Areas/Admin/Views/`)
- **Quản trị chung** (`AdminDashboardController.cs`):
  - `Index()`: Thống kê tổng số Sản phẩm, Đơn hàng, Người dùng, Thương hiệu, Danh mục, Bài viết, Đánh giá, Voucher, các đơn hàng gần nhất.
  - `Products()`: Danh sách sản phẩm, phân trang, lọc tìm kiếm, xóa mềm (`IsDeleted = true`).
  - `Orders()`: Danh sách đơn hàng, khách hàng, SĐT, địa chỉ, trạng thái, tổng tiền, thao tác cập nhật trạng thái đơn hàng (`updateorderstatus`) và xác nhận thu tiền COD (`confirmcodpayment`).
  - `Users()`: Đổi vai trò tài khoản (`ChangeUserRole`), xóa người dùng.
  - `Erd()`: Hiển thị cấu trúc lược đồ các bảng CSDL.
- **Quản lý bán hàng** (`StoreManagerController.cs`):
  - `Vouchers()` & `SaveVoucher()`: Giao diện tạo và sửa voucher (mã, % giảm, số tiền giảm, đơn tối thiểu, giảm tối đa, hạn mức, ngày bắt đầu/kết thúc, trạng thái kích hoạt).
  - `Reviews()` & `ModerateReview()`: Duyệt hoặc từ chối đánh giá từ khách hàng.

---

## 4. KẾ HOẠCH & ĐIỂM TÁC ĐỘNG GIAO DIỆN CHO TÍNH NĂNG R1: FLASH SALE & REAL-TIME COUNTDOWN

### 4.1. Giao Diện Quản Trị Flash Sale tại `/Admin/AdminDashboard/FlashSales`
- **Controller Target**: `src/Perfume.Web/Areas/Admin/Controllers/AdminDashboardController.cs`
  - Thêm Action `[HttpGet] FlashSales()`: Lấy danh sách chiến dịch Flash Sale (bao gồm chiến dịch đang hoạt động, sắp diễn ra, đã kết thúc) kèm danh sách sản phẩm trong chiến dịch.
  - Thêm Action `[HttpPost] CreateFlashSale(...)`: Tạo chiến dịch mới với Tên chiến dịch, Thời gian bắt đầu (UTC), Thời gian kết thúc (UTC), Trạng thái kích hoạt (`IsActive`), và cấu hình sản phẩm tham gia (ProductId, VariantId, FlashSalePrice, LimitQuantity).
  - Thêm Action `[HttpPost] ToggleFlashSaleStatus(int id)`: Bật/tắt nhanh kích hoạt chiến dịch.
  - Thêm Action `[HttpPost] DeleteFlashSale(int id)`: Xóa chiến dịch.
- **View Target**: `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml` (Tạo mới)
  - Layout: `_AdminLayout.cshtml`.
  - Form tạo chiến dịch với phong cách `.luxury-card`: Tên chiến dịch, Date-time pickers (`datetime-local`) bắt đầu & kết thúc, bộ chọn sản phẩm tham gia Flash Sale, ô nhập giá Flash Sale độc quyền và số lượng giới hạn.
  - Bảng danh sách chiến dịch: Tên, Thời gian, Số sản phẩm, Tổng giới hạn, Đã bán, Trạng thái (Đang diễn ra [Xanh], Sắp diễn ra [Vàng], Đã kết thúc [Mờ]), Nút Bật/Tắt, Nút Xóa.
- **Menu Admin**: Bổ sung liên kết trong `_AdminLayout.cshtml`:
  ```html
  <a href="/admin/admindashboard/flashsales" class="nav-link" style="padding: 6px 12px; border-radius: 6px; font-size: 0.85rem;">
      <i class="fa-solid fa-bolt" style="margin-right: 4px;"></i> Flash Sale
  </a>
  ```

### 4.2. Banner Flash Sale Trên Trang Chủ (`Views/Home/Index.cshtml`)
- **Vị trí**: Đặt ngay bên dưới Hero Banner và Logo Strip (hoặc trên khối Bộ Sưu Tập Tiêu Biểu).
- **Thiết kế UI Banner Hoàng Gia**:
  - Khung bao: Nền gradient đen huyền bí viền vàng kim (`border: 1px solid var(--gold-primary)`), hiệu ứng hào quang vàng (`box-shadow: var(--glow-gold)`).
  - Header Banner:
    - Tiêu đề: `<i class="fa-solid fa-bolt text-gold-gradient"></i> FLASH SALE ĐẶC QUYỀN HAUTE PARFUMERIE`
    - Đồng hồ đếm ngược thời gian thực theo giây (Real-time Countdown Timer):
      - 4 khối số đếm: `Ngày (DD)` : `Giờ (HH)` : `Phút (MM)` : `Giây (SS)`.
      - Phong cách ô số: Nền đen nổi (`--bg-surface-elevated`), viền vàng, chữ vàng gradient kích thước lớn, font chữ số đậm, nhãn chữ nhỏ bên dưới.
  - Lưới sản phẩm Flash Sale dạng Carousel hoặc Grid:
    - Mỗi thẻ sản phẩm hiển thị:
      - Nhãn góc: `<span class="badge-flash-sale"><i class="fa-solid fa-bolt"></i> GIẢM 35%</span>`
      - Ảnh sản phẩm, Thương hiệu, Tên nước hoa.
      - Giá Flash Sale nổi bật bằng vàng kim kích thước lớn, bên cạnh giá gốc gạch ngang.
      - Thanh tiến trình số lượng (Stock Progress Bar): Đồ họa thanh vàng thể hiện tỷ lệ đã bán (ví dụ: *"Đã bán 18/25 chai"*).
      - Nút "Sở Hữu Ngay" (`addToCart(slug, variantId, 1)`).
- **Hành vi Script Đồng Hồ Đếm Ngược & Giá Tự Động Reversion**:
  - Script tính khoảng cách từ thời điểm hiện tại `new Date()` đến `EndTime` của chiến dịch Flash Sale.
  - `setInterval(updateTimer, 1000)`: Cập nhật từng giây cho các ô Giờ, Phút, Giây.
  - **Price Reversion Behavior (Tự động hoàn về giá gốc)**:
    - Khi thời gian về `00:00:00` hoặc khi số lượng bán đạt mức tối đa (`SoldQuantity >= LimitQuantity`):
      - Đồng hồ hiển thị *"Chiến dịch đã kết thúc"*.
      - Thẻ sản phẩm tự động gỡ bỏ nhãn Flash Sale, cập nhật mức giá hiển thị trở về giá gốc (`BasePrice` hoặc `Variant.SalePrice`).
      - Nút mua hàng chuyển sang giá thường.

### 4.3. Huy Hiệu Flash Sale Trên Product Cards & Product Details
- **Product Card** (`Views/Catalog/Index.cshtml` & `Views/Home/Index.cshtml`):
  - Nếu sản phẩm nằm trong chiến dịch Flash Sale đang kích hoạt và còn hạn:
    - Hiển thị badge đỏ-vàng nhấp nháy tinh tế: `<span class="badge-tag tag-flash-sale"><i class="fa-solid fa-bolt"></i> FLASH SALE</span>`
    - Cột giá hiển thị giá Flash Sale thay vì `BasePrice`.
- **Product Details** (`Views/Catalog/Detail.cshtml`):
  - Khối thông báo Flash Sale ngay trên ô giá:
    - Thanh banner nhỏ: Nền đỏ đen viền vàng, icon sấm sét nhấp nháy, đồng hồ đếm ngược thu nhỏ cho riêng sản phẩm này.
    - Giá Flash Sale độc quyền hiển thị nổi bật tại `#activePrice`, gạch ngang giá gốc tại `#activeOriginalPrice`.
    - Tự động hoàn giá khi hết giờ mà không cần tải lại trang.

---

## 5. KẾ HOẠCH & ĐIỂM TÁC ĐỘNG GIAO DIỆN CHO TÍNH NĂNG R2: THẺ VIP KỸ THUẬT SỐ & CHIẾT KHẤU TỰ ĐỘNG

### 5.1. Thẻ Thành Viên VIP Kỹ Thuật Số Trên Trang Hồ Sơ (`/Account/Profile`)
- **Tập tin tác động**: `src/Perfume.Web/Views/Account/Profile.cshtml` và `src/Perfume.Web/Controllers/AccountController.cs`.
- **Thiết kế Thẻ VIP Hoàng Gia (Digital VIP Membership Card)**:
  - Tỷ lệ chuẩn thẻ tín dụng quốc tế (aspect ratio 1.586:1 hoặc min-height 230px), bo góc tròn mềm mại (`border-radius: 18px`).
  - Nền thẻ (Metallic Sheen / Gold Foil Texture):
    - Bronze: Gradient kim loại đồng đồng ánh kim (`linear-gradient(135deg, #4a2810, #8B5A2B, #2e1708)`).
    - Silver: Gradient bạc bạch kim xà cừ (`linear-gradient(135deg, #71717a, #e4e4e7, #52525b)`).
    - Gold: Gradient vàng hoàng gia mạ 24K (`linear-gradient(135deg, #78590f, #d4af37, #ecc86e, #573e07)`).
    - Diamond: Gradient đen huyền bí viền xanh kim cương óng ánh (`linear-gradient(135deg, #090d16, #1e293b, #0f172a)` kèm viền óng ánh `#38bdf8`).
  - Các thành phần trên mặt thẻ:
    - Góc trên trái: Logo vương miện mạ vàng `PERFUME LUXE - VIP PRIVILEGE CLUB`.
    - Góc trên phải: Huy hiệu bậc thành viên dập nổi viền sáng: `BRONZE`, `SILVER`, `GOLD`, `DIAMOND`.
    - Giữa thẻ: Chip vi mạch kim loại vàng và mã định danh thẻ thành viên kỹ thuật số: `PLX-8899-****-2026`.
    - Góc dưới trái: Họ tên chủ thẻ (`@Model.FullName.ToUpper()`).
    - Góc dưới phải: Ngày phát hành (`Member Since: @Model.CreatedAt.ToString("MM/yy")`).
- **Thanh Tiến Trình Tích Lũy Chi Tiêu (Progress Bar)**:
  - Hiển thị ngay bên dưới thẻ VIP:
    - Tổng chi tiêu tích lũy qua các đơn hàng hoàn tất (`Delivered` hoặc `PaymentStatus.Completed`): ví dụ `12.500.000₫`.
    - Bậc hiện tại: `Silver (Đang giảm 3%)`.
    - Mục tiêu bậc kế tiếp: `Gold (15.000.000₫ - Giảm 7%)`.
    - Dòng thông điệp động: *"Còn tích lũy thêm 2.500.000₫ để tự động thăng hạng Gold và hưởng đặc quyền giảm 7%!"*.
    - Thanh Progress Bar: Nền xám mờ, dải tiến trình gradient vàng phát sáng rực rỡ, kèm các mốc đánh dấu (Milestones: 0đ Bronze, 5M Silver, 15M Gold, 30M Diamond).
- **Bảng Danh Sách Đặc Quyền VIP (Privileges Matrix)**:
  - Liệt kê trực quan 4 bậc thành viên và quyền lợi tương ứng:
    - **Bronze (0đ)**: Tích lũy điểm đổi quà, tư vấn mùi hương cơ bản.
    - **Silver (≥ 5.000.000đ)**: Giảm trực tiếp **3%** trên mọi đơn hàng, quà sinh nhật.
    - **Gold (≥ 15.000.000đ)**: Giảm trực tiếp **7%** trên mọi đơn hàng, miễn phí vận chuyển trọn đời, đóng gói thiệp sáp niêm phong hoàng gia.
    - **Diamond (≥ 30.000.000đ)**: Giảm trực tiếp **12%** trên mọi đơn hàng, bộ quà tặng mùi hương độc bản hàng năm, chuyên gia mùi hương phục vụ riêng 1:1.

### 5.2. Tự Động Nhận Diện & Áp Dụng Chiết Khấu VIP Tại Checkout (`/Checkout`)
- **Tập tin tác động**:
  - `src/Perfume.Web/Views/Checkout/Index.cshtml`
  - `src/Perfume.Web/Controllers/CheckoutController.cs`
  - `src/Perfume.Infrastructure/Services/OrderService.cs`
  - `src/Perfume.Web/Controllers/CustomerApiController.cs`
- **Logic Tính Toán & Trình Diễn UI**:
  - Nếu khách hàng đã đăng nhập:
    - Hệ thống lấy thông tin `UserTier` (hoặc truy vấn từ `UserLoyalty` / `AppUser.Tier`).
    - Tính tỷ lệ chiết khấu VIP: Bronze = 0%, Silver = 3%, Gold = 7%, Diamond = 12%.
  - Trình bày tại cột tính tiền trong `Views/Checkout/Index.cshtml`:
    - `Tạm tính:` `subTotal` (ví dụ 4.000.000₫)
    - **Dòng chiết khấu VIP Tier độc lập**:
      ```html
      <div style="display: flex; justify-content: space-between; margin-bottom: 8px; color: var(--gold-primary);">
          <span><i class="fa-solid fa-crown" style="margin-right: 4px;"></i> Chiết khấu VIP (@userTier - @tierDiscountPercent%):</span>
          <span id="displayVipDiscount" style="font-weight: 700;">-@vipDiscountAmount.ToString("N0")₫</span>
      </div>
      ```
    - **Dòng chiết khấu Voucher**: Áp dụng **đồng thời** với Voucher mà không bị triệt tiêu!
      ```
      Tổng giảm giá = Chiết khấu VIP + Giảm giá Voucher
      ```
    - `Phí vận chuyển:` 0₫ hoặc 35.000₫.
    - `Tổng thanh toán:` `subTotal - (vipDiscount + voucherDiscount) + shippingFee`.

---

## 6. KẾ HOẠCH & ĐIỂM TÁC ĐỘNG GIAO DIỆN CHO TÍNH NĂNG R3: TRÌNH THIẾT KẾ HỘP QUÀ DISCOVERY

### 6.1. Kiến Trúc Giao Diện Tương Tác 3 Bước (Interactive 3-Step Builder)
- **Tuyến đường truy cập**:
  - URL trang chủ storefront: `/discovery-box` hoặc `/hop-qua-discovery`.
  - Có thể đưa vào Header navigation (`_Layout.cshtml`) bên cạnh mục *"Trắc Nghiệm Mùi"*:
    ```html
    <li><a href="/discovery-box" class="nav-link" style="color: var(--gold-primary);"><i class="fa-solid fa-gift" style="margin-right: 4px;"></i> Thiết Kế Hộp Quà</a></li>
    ```
- **Thiết Kế Trải Nghiệm Khách Hàng (User Experience Flow)**:
  - **Thanh chỉ dẫn 3 bước (Stepper Header)**:
    - `Bước 1: Chọn Hộp Hoàng Gia` &rarr; `Bước 2: Chọn 3-5 Mẫu Thử Vial` &rarr; `Bước 3: Thiệp & Lời Chúc Mạ Vàng`.

#### Bước 1: Chọn Mẫu Hộp Hoàng Gia (Box Selection)
- 3 tùy chọn phong cách xa xỉ được thiết kế dạng thẻ tương tác có radio chọn viền vàng sáng:
  1. **Hộp Gỗ Sơn Mài Cổ Điển (Classic Lacquer Wooden Box)**:
     - Giá: 250.000₫.
     - Mô tả: Chế tác từ gỗ sồi bọc sơn mài bóng mờ, hoa văn khắc laser mạ vàng, lót nhung đỏ Bordeaux.
     - Hình ảnh minh họa hộp gỗ sang trọng.
  2. **Hộp Nhung Đen Hoàng Gia (Royal Black Velvet Box)**:
     - Giá: 200.000₫.
     - Mô tả: Vải nhung nhung tuyết đen cao cấp, khay giữ vial mút xốp đúc khuôn vừa vặn, dải ruy băng lụa champagne.
     - Hình ảnh minh họa hộp nhung quý phái.
  3. **Bao Da Thủ Công (Handcrafted Leather Pouch)**:
     - Giá: 180.000₫.
     - Mô tả: Da microfiber vân Togo nhập khẩu Ý, nút bấm kim loại đồng cổ điển, nhỏ gọn cho quý ông/quý cô di chuyển.
     - Hình ảnh minh họa bao da phong cách lãng tử.

#### Bước 2: Chọn 3-5 Ống Vial Mẫu Thử (3-5 Vial Samples Selection)
- **Khay chứa Vial trực quan (Vial Rack Visualizer)**:
  - Đồ họa hiển thị 5 vị trí ống nghiệm thủy tinh sang trọng:
    - `[Vial 1: Đã chọn Sauvage 5ml]` `[Vial 2: Đã chọn Baccarat 2ml]` `[Vial 3: Đã chọn Aventus 5ml]` `[Vial 4: + Thêm]` `[Vial 5: + Thêm]`
    - Bộ đếm: *"Đã chọn 3/5 ống vial (Tối thiểu 3 ống, tối đa 5 ống)"*.
    - Cảnh báo trực quan: Nút tiếp tục chỉ mở khi đã chọn từ 3 đến 5 ống.
- **Danh mục mẫu thử để khách hàng lựa chọn**:
  - Tải từ cơ sở dữ liệu các dòng nước hoa hàng đầu (Dior Sauvage, Chanel Bleu, Creed Aventus, Tom Ford Oud Wood, Baccarat Rouge 540, Le Labo Santal 33...).
  - Mỗi mẫu thử hiển thị 2 nút dung tích:
    - **2ml Mini Vial**: ví dụ 120.000₫ – 160.000₫.
    - **5ml Luxury Vial**: ví dụ 240.000₫ – 320.000₫.
  - Khách hàng bấm chọn -> Ống vial tự động bay vào khay chứa kèm âm thanh/hiệu ứng động thị giác.

#### Bước 3: Cá Nhân Hóa Lời Chúc & Thiệp Mạ Vàng (Custom Greeting Card)
- **Khu vực nhập liệu**:
  - Người nhận (To): e.g. *"Nàng thơ Thanh Hằng"*.
  - Người gửi (From): e.g. *"Hoàng Nam"*.
  - Thông điệp chúc mừng (Message): Textarea tối đa 200 ký tự (e.g. *"Chúc em luôn rạng rỡ và quyến rũ như từng nốt hương nồng nàn này..."*).
  - Chọn phông chữ viết tay nghệ thuật (Handwriting Font):
    - *Cormorant Garamond Italic* (Cổ điển Pháp)
    - *Dancing Script* (Thư pháp Hoàng gia lãng mạn)
    - *Playfair Italic* (Hiện đại thanh tao)
- **Bản xem trước thiệp mạ vàng thời gian thực (Live Gold-Foil Card Preview)**:
  - Render thẻ thiệp phong cách hoàng gia trên màn hình: Nền giấy mỹ thuật vân mây đen nhám, viền dập chìm mạ vàng kim, nội dung chữ hiển thị theo phông chữ viết tay đã chọn với hiệu ứng chữ vàng óng ánh ánh kim (`text-gold-gradient` hoặc ánh vàng kim loại sáng bóng). Khách hàng gõ chữ đến đâu, thiệp cập nhật trực tiếp tức thì đến đó.

### 6.2. Tính Giá & Tích Hợp Vào Giỏ Hàng (`/Cart`), Checkout (`/Checkout`)
- **Bộ tính giá trọn gói**:
  ```
  Tổng giá Hộp Quà Discovery = Giá Vỏ Hộp + Tổng Giá Các Ống Vial Đã Chọn
  ```
- **Thao Tác Thêm Vào Giỏ Hàng**:
  - Nút "Thêm Hộp Quà Vào Giỏ Hàng": Gọi API `POST /api/v1/discovery-box/validate-and-add`.
  - Payload gửi lên:
    ```json
    {
      "boxType": "LacquerWood", // LacquerWood | BlackVelvet | LeatherPouch
      "boxName": "Hộp Gỗ Sơn Mài Cổ Điển",
      "boxPrice": 250000,
      "items": [
        { "productId": 1, "productName": "Dior Sauvage", "volumeMl": 5, "price": 280000 },
        { "productId": 2, "productName": "Creed Aventus", "volumeMl": 2, "price": 180000 },
        { "productId": 3, "productName": "Bleu de Chanel", "volumeMl": 5, "price": 260000 }
      ],
      "cardMessage": "Chúc em luôn rạng rỡ...",
      "cardFont": "Dancing Script",
      "recipientName": "Thanh Hằng",
      "senderName": "Hoàng Nam"
    }
    ```
- **Trình Diễn Tại Giỏ Hàng (`Views/Cart/Index.cshtml`) & Mini Cart Drawer**:
  - Dòng sản phẩm Hộp Quà Discovery hiển thị phân biệt rõ ràng so với chai nước hoa thông thường:
    - Ảnh đại diện: Ảnh mẫu hộp đã chọn (Hộp gỗ/Hộp nhung/Bao da).
    - Tên sản phẩm: `Custom Discovery Gift Box - [Tên Hộp]`.
    - Danh sách chi tiết các ống vial bên dưới:
      - `• Dior Sauvage (5ml) - 280.000₫`
      - `• Creed Aventus (2ml) - 180.000₫`
      - `• Bleu de Chanel (5ml) - 260.000₫`
    - Nhãn ghi chú thiệp: `<i class="fa-solid fa-envelope-open-text text-gold-gradient"></i> Thiệp mạ vàng kèm lời chúc: "Chúc em luôn rạng rỡ..." (Font: Dancing Script)`.
    - Thành tiền: Tổng trọn gói chính xác.

### 6.3. Trình Diễn Tại Trang Thanh Toán (`/Checkout`) & Chi Tiết Đơn Hàng Admin (`/Admin/AdminDashboard/OrderDetails`)
- **Tại `/Checkout`**:
  - Khách hàng xem lại đầy đủ cấu hình hộp quà (loại hộp, từng ống vial đã chọn, nội dung thiệp) ngay trong danh sách tóm tắt đơn hàng trước khi bấm Xác nhận đặt hàng.
- **Tại Quản Trị Đơn Hàng Admin (`/Admin/AdminDashboard/OrderDetails`)**:
  - Cung cấp giao diện chi tiết đơn hàng quản trị tại `/Admin/AdminDashboard/OrderDetails/{id}` hoặc modal chi tiết tại `/Admin/AdminDashboard/Orders`:
    - Nhân viên đóng gói (Packer) thấy toàn bộ chi tiết:
      - Loại vỏ hộp cần lấy trong kho (Hộp gỗ sơn mài / Hộp nhung / Bao da).
      - Danh sách chính xác 3-5 ống vial (tên sản phẩm, dung tích 2ml/5ml) để chiết và đóng vào khay.
      - Nội dung lời chúc cần in hoặc viết tay lên thiệp mạ vàng kèm phông chữ yêu cầu.
    - Đảm bảo 100% không bị sót bất kỳ thông tin cá nhân hóa nào của khách hàng.

---

## 7. TỔNG HỢP MA TRẬN FILE & ĐIỂM CHẠM GIAO DIỆN (TOUCHPOINT MATRIX)

| Mã Tính Năng | Mô Tả Yêu Cầu | Tệp Cần Tạo / Sửa Đổi | Vai Trò & Điểm Chạm |
|---|---|---|---|
| **R1** | Admin Flash Sale View | `Areas/Admin/Controllers/AdminDashboardController.cs` | Thêm Actions `FlashSales`, `CreateFlashSale`, `ToggleFlashSaleStatus`, `DeleteFlashSale` |
| **R1** | Admin Flash Sale View | `Areas/Admin/Views/AdminDashboard/FlashSales.cshtml` | View quản trị chiến dịch Flash Sale, danh sách sản phẩm, giá flash sale, số lượng giới hạn |
| **R1** | Admin Flash Sale Nav | `Areas/Admin/Views/Shared/_AdminLayout.cshtml` | Thêm nav link Flash Sale vào thanh điều hướng Admin |
| **R1** | Homepage Banner & Timer | `Views/Home/Index.cshtml` | Banner Flash Sale với đồng hồ đếm ngược thời gian thực theo giây, thẻ sản phẩm giá khuyến mãi |
| **R1** | Product Badges & Details | `Views/Catalog/Index.cshtml`, `Views/Catalog/Detail.cshtml` | Gắn nhãn Flash Sale Badge, hiển thị giá ưu đãi và cơ chế tự động chuyển về giá gốc khi hết giờ/hết hàng |
| **R2** | Digital VIP Card & Progress Bar | `Views/Account/Profile.cshtml` | Thẻ thành viên VIP kỹ thuật số kim loại hoàng gia, thanh tiến trình chi tiêu tới bậc tiếp theo, bảng đặc quyền |
| **R2** | Loyalty Tier Logic Profile | `Controllers/AccountController.cs` | Tính toán tổng chi tiêu của user qua các đơn hàng hoàn tất, xác định Tier hiện tại và số tiền còn thiếu |
| **R2** | Checkout VIP Discount | `Views/Checkout/Index.cshtml` | Hiển thị dòng giảm giá VIP Tier độc lập, tính tổng thanh toán kết hợp cùng mã Voucher |
| **R2** | Checkout Controller & Order Service | `Controllers/CheckoutController.cs`, `OrderService.cs` | Tự động nhận diện VIP tier, tính chiết khấu VIP Tier và trừ vào tổng đơn hàng |
| **R3** | Discovery Box Builder Page | `Controllers/HomeController.cs` (hoặc `CatalogController.cs`) | Thêm Action `DiscoveryBox()` trả về view builder |
| **R3** | Discovery Box Builder View | `Views/Home/DiscoveryBox.cshtml` (hoặc `Views/Catalog/DiscoveryBox.cshtml`) | Giao diện tương tác 3 bước: Chọn hộp hoàng gia, Chọn 3-5 vial (2ml-5ml), Viết thiệp mạ vàng và chọn font |
| **R3** | Cart Drawer & Cart View | `Views/Shared/_Layout.cshtml`, `Views/Cart/Index.cshtml` | Trình diễn hộp quà trọn gói, danh sách các vial con kèm ml và xem trước lời chúc |
| **R3** | Checkout & Order Details | `Views/Checkout/Index.cshtml`, `Views/Account/OrderDetails.cshtml` | Hiển thị hộp quà và thiệp chúc mừng trong đơn hàng của khách hàng |
| **R3** | Admin Order Details | `Areas/Admin/Views/AdminDashboard/Orders.cshtml` / `OrderDetails.cshtml` | Hiển thị đầy đủ chi tiết cấu hình hộp quà và nội dung thiệp để nhân viên đóng gói thực hiện |
| **R1, R2, R3** | API Endpoints | `Controllers/CustomerApiController.cs` | `GET /api/v1/flashsales/active`, `GET /api/v1/loyalty/status`, `POST /api/v1/discovery-box/validate-and-add` |

---
*Báo cáo khảo sát được lập bởi `explorer_survey_2`, phục vụ làm cơ sở vững chắc cho giai đoạn triển khai (Implementation).*
