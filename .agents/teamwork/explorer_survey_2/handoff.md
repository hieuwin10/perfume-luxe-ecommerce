# Handoff Report: Web Presentation Layer & UI Architecture Survey

**Agent**: `explorer_survey_2`  
**Working Directory**: `C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_survey_2`  
**Handoff Type**: Hard (Investigation complete, deliverables ready)

---

## 1. Observation

1. **Project Directory & Architecture**:
   - `src/Perfume.Web` chứa toàn bộ tầng Web Presentation (.NET 8 MVC kết hợp RESTful API Controller).
   - `src/Perfume.Web/Controllers`: Chứa 9 controllers (`AccountController.cs`, `BlogController.cs`, `CartController.cs`, `CatalogController.cs`, `CheckoutController.cs`, `CustomerApiController.cs`, `HomeController.cs`, `ReviewsController.cs`, `SeoController.cs`).
   - `src/Perfume.Web/Areas/Admin/Controllers`: Chứa 4 admin controllers (`AdminDashboardController.cs`, `DashboardController.cs`, `DatabaseConsoleController.cs`, `StoreManagerController.cs`).

2. **Master Layouts & Navigation**:
   - `src/Perfume.Web/Views/Shared/_Layout.cshtml`:
     - Lines 48-65: Header navigation links (`/`, `/bo-suu-tap`, `/catalog/quiz`, `/blog`, `/admin/admindashboard`).
     - Lines 76-79 & 131-153: Mini Cart Drawer (`#cartDrawer`, `#cartDrawerItems`, `#cartDrawerSubtotal`).
     - Lines 223-370: Tích hợp `CustomerApi.get('/api/v1/cart')`, `CustomerApi.post('/api/v1/cart/items', ...)` và Dark/Light theme toggle.
   - `src/Perfume.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`:
     - Lines 28-65: Navigation bar của Admin: Tổng Quan (`/admin/admindashboard`), Thống Kê (`/admin/storemanager`), Sản Phẩm (`/admin/admindashboard/products`), Đơn Hàng (`/admin/admindashboard/orders`), Phân Quyền (`/admin/admindashboard/users`), Voucher (`/admin/storemanager/vouchers`), Đánh Giá (`/admin/storemanager/reviews`), Sơ Đồ ERD (`/admin/admindashboard/erd`), DB Console (`/admin/databaseconsole`).
     - Hiện tại **chưa có** mục menu Flash Sale tại `/admin/admindashboard/flashsales`.

3. **Homepage & Catalog Views**:
   - `src/Perfume.Web/Views/Home/Index.cshtml`:
     - Lines 10-31: Hero Banner giới thiệu bộ sưu tập 2026.
     - Lines 47-108: Lưới sản phẩm tiêu biểu (`.perfume-card`), hiển thị badge `Best Seller`, `Mới Về`, giá từ `BasePrice` và nút `addToCart()`.
     - Hiện tại **chưa có** Flash Sale Banner với đồng hồ đếm ngược theo giây.
   - `src/Perfume.Web/Views/Catalog/Detail.cshtml`:
     - Lines 63-75: Ô hiển thị giá (`#activePrice`, `#activeOriginalPrice`).
     - Lines 77-98: Bộ chọn dung tích phiên bản (`.variant-btn`).
     - Lines 314-355: Script `selectVariant` và `handleAddToCart`.
     - Hiện tại **chưa có** Flash Sale Badge và cơ chế kiểm tra giá Flash Sale thời gian thực.

4. **Account & Profile Views**:
   - `src/Perfume.Web/Views/Account/Profile.cshtml`:
     - Lines 51-57: Chỉ hiển thị một card tĩnh đơn giản về hạng thành viên:
       ```html
       <div style="font-size: 1.4rem; font-weight: 700; color: var(--gold-primary); margin-bottom: 6px;">@Model.Tier</div>
       <p style="color: var(--text-muted); font-size: 0.85rem; margin: 0;">Điểm thưởng tích lũy: <strong style="color: var(--text-primary);">@Model.LoyaltyPoints</strong> pts</p>
       ```
     - Hiện tại **chưa có** Thẻ thành viên VIP kỹ thuật số (Digital VIP Card) và thanh tiến trình chi tiêu (Progress Bar) đến bậc kế tiếp.
   - `src/Perfume.Domain/Enums/PerfumeEnums.cs`:
     - Lines 63-69: Enum `UserTier` đã có 4 bậc: Bronze (1), Silver (2, giảm 3%), Gold (3, giảm 7%), Diamond (4, giảm 12% + quà độc quyền).

5. **Cart & Checkout Flow**:
   - `src/Perfume.Web/Views/Cart/Index.cshtml`: Hiển thị danh sách Cart Items dựa trên `CartStateDto`.
   - `src/Perfume.Web/Views/Checkout/Index.cshtml`:
     - Lines 83-91: Nhập mã Voucher và gọi `CustomerApi.post('/api/v1/vouchers/validate')`.
     - Lines 94-113: Bảng tính tiền chỉ gồm Tạm tính, Giảm giá (Voucher), Phí vận chuyển, Tổng thanh toán.
     - Lines 131-156: Submit gọi `CustomerApi.post('/api/v1/orders')`.
     - Hiện tại **chưa có** dòng tự động chiết khấu VIP Tier cho khách hàng đăng nhập.
   - `src/Perfume.Infrastructure/Services/OrderService.cs`:
     - Lines 106-125: Đơn hàng chỉ tính giảm giá từ Voucher:
       ```csharp
       order.SubTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
       if (!string.IsNullOrWhiteSpace(request.VoucherCode)) { ... order.DiscountAmount = Math.Min(order.SubTotal, discount); }
       order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount + order.ShippingFee);
       ```
     - Chưa trừ thêm chiết khấu VIP Tier và chưa có logic tự động nâng hạng thành viên khi đơn hàng chuyển sang `Delivered` hoặc `PaymentStatus.Completed`.

6. **Order Details in Admin**:
   - `src/Perfume.Web/Areas/Admin/Views/AdminDashboard/Orders.cshtml`: Chỉ có bảng danh sách đơn hàng và nút đổi trạng thái đơn hàng (`updateorderstatus`), chưa có màn hình xem chi tiết đơn hàng quản trị (`OrderDetails`).

---

## 2. Logic Chain

1. **Khảo sát yêu cầu R1 (Flash Sale Engine)**:
   - Từ Quan sát 1 & 2: Quản trị Flash Sale yêu cầu giao diện tại `/Admin/AdminDashboard/FlashSales`. Vì `AdminDashboardController` đã quản lý `Orders`, `Products`, `Users`, nên tạo thêm Action `FlashSales()` tại đây và bổ sung View `Areas/Admin/Views/AdminDashboard/FlashSales.cshtml`, đồng thời thêm nav link trong `_AdminLayout.cshtml`.
   - Từ Quan sát 3: Storefront cần Banner trên `Views/Home/Index.cshtml` và Badge trên `Views/Catalog/Index.cshtml` / `Views/Catalog/Detail.cshtml`. Script đếm ngược theo giây cần dùng `setInterval` đếm ngược đến `EndTime`. Khi `now >= EndTime` hoặc số lượng hết, JS tự động gỡ bỏ badge và hoàn giá về `BasePrice`.

2. **Khảo sát yêu cầu R2 (Loyalty VIP Tier)**:
   - Từ Quan sát 4: `UserTier` đã có sẵn trong Domain Enums (Bronze 0đ, Silver >= 5M - 3%, Gold >= 15M - 7%, Diamond >= 30M - 12%). `Profile.cshtml` hiện chỉ hiển thị text tĩnh. Cần tái thiết kế thành Thẻ VIP kỹ thuật số kim loại hoàng gia sang trọng kèm Progress Bar tính từ tổng chi tiêu các đơn hoàn tất (`OrderStatus.Delivered` hoặc `PaymentStatus.Completed`).
   - Từ Quan sát 5: Tại `Views/Checkout/Index.cshtml` và `OrderService.cs`, cần nhận diện tài khoản đăng nhập để tự động tính chiết khấu VIP Tier (`Silver: 3%`, `Gold: 7%`, `Diamond: 12%`) kết hợp cùng Voucher (không bị xung đột hay ghi đè).

3. **Khảo sát yêu cầu R3 (Custom Discovery Gift Box Builder)**:
   - Cần giao diện tương tác 3 bước (Chọn vỏ hộp: Gỗ sơn mài 250k / Nhung đen 200k / Bao da 180k -> Chọn 3-5 ống vial 2ml-5ml từ danh mục -> Nhập thông điệp thiệp mạ vàng & chọn font viết tay).
   - Tổng giá trọn gói = Giá hộp + Tổng giá các vial.
   - Thêm trọn gói vào Giỏ hàng cần hiển thị rõ cấu hình hộp quà tại Mini Cart Drawer (`_Layout.cshtml`), trang Giỏ hàng (`Views/Cart/Index.cshtml`), trang Thanh toán (`Views/Checkout/Index.cshtml`), chi tiết đơn hàng khách hàng (`Views/Account/OrderDetails.cshtml`) và chi tiết đơn hàng quản trị (`Areas/Admin/Views/AdminDashboard/Orders.cshtml` hoặc view `OrderDetails.cshtml`).

---

## 3. Caveats

- **Phần Backend Service & Schema DB**: Nhiệm vụ của `explorer_survey_2` tập trung khảo sát tầng Web Presentation (Controllers, Views, ViewModels, CSS/JS). Việc thiết kế Entity `FlashSale`, cơ chế Database Migration / Schema Upgrader và TDD Unit Tests thuộc phạm vi khảo sát của các kỹ sư chuyên trách Backend/Architecture.
- **Dữ liệu Seed ban đầu**: Các vial mẫu thử 2ml - 5ml cần đảm bảo các sản phẩm mẫu trong hệ thống có sẵn variant dung tích 2ml và 5ml (hoặc được hệ thống tự động sinh ra phục vụ Discovery Box).

---

## 4. Conclusion

Tầng trình diễn web của PERFUME LUXE có nền tảng kiến trúc rất vững chắc:
- Hệ thống Style `luxury-perfume.css` đồng bộ cao cấp với đầy đủ biến màu vàng ánh kim, Dark/Light theme, các class tái sử dụng (`.btn-luxe`, `.luxury-card`).
- Thư viện `customer-api.js` hoạt động tin cậy với cơ chế Antiforgery CSRF tự động.
- Điểm chạm UI cho cả 3 tính năng R1, R2, R3 đã được định vị chính xác tới từng dòng code, file Razor View và Controller liên quan.
- Báo cáo khảo sát chi tiết đã được xuất ra tại:
  `src/.agents/teamwork/explorer_survey_2/survey_report.md`.

---

## 5. Verification Method

1. **Kiểm tra báo cáo khảo sát và artifact**:
   - Xem tệp: `C:\Users\Sieu Chu nhiem\.gemini\antigravity\worktrees\dotnet\merge_all_prs_to_main\.agents\teamwork\explorer_survey_2\survey_report.md`
   - Xác nhận báo cáo bao quát đầy đủ 4 yêu cầu được giao trong Dispatch Message.
2. **Kiểm tra các tệp mã nguồn và giao diện tham chiếu**:
   - `src/Perfume.Web/Views/Shared/_Layout.cshtml` (dòng 48-153)
   - `src/Perfume.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml` (dòng 28-65)
   - `src/Perfume.Web/Views/Home/Index.cshtml` (dòng 10-120)
   - `src/Perfume.Web/Views/Catalog/Detail.cshtml` (dòng 63-98)
   - `src/Perfume.Web/Views/Account/Profile.cshtml` (dòng 51-57)
   - `src/Perfume.Web/Views/Checkout/Index.cshtml` (dòng 83-115)
   - `src/Perfume.Web/Controllers/CustomerApiController.cs` (toàn bộ các route `api/v1/...`)
3. **Điều kiện vô hiệu hóa (Invalidation conditions)**:
   - Nếu cấu trúc thư mục của `Perfume.Web` bị thay đổi (ví dụ: tách Views sang Single Page App React/Vue) thì các đường dẫn Razor Views cần được khảo sát lại. Hiện tại 100% là Razor Views SSR kết hợp Fetch API.
