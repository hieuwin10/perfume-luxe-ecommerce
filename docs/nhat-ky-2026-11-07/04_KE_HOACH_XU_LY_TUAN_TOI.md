# KẾ HOẠCH HÀNH ĐỘNG & ROADMAP SỬA LỖI TUẦN TỚI (WEEK 46 / 2026)
## Dự án: PERFUME LUXE E-COMMERCE

---

### 1. Tổng Hợp Danh Mục Lỗi Cần Xử Lý (11 Lỗi)

Tổng hợp từ Nhật ký kiểm thử Giai đoạn 2 (`ISSUE-2026-WK45-01`) và Sổ bộ lỗi Giai đoạn 3 & 4 (10 lỗi):

| Thứ Tự | Mã Lỗi | Phân Hệ | File Dự Kiến Sửa | Mức Độ | Nội Dung Cần Xử Lý |
|:---:|:---|:---|:---|:---:|:---|
| **1** | `ISSUE-2026-WK45-01` | Customer Orders | `Views/Account/MyOrders.cshtml`<br>`Views/Account/OrderDetails.cshtml` | **Major** | Bọc `loadOrders()` vào `DOMContentLoaded` để triệt tiêu lỗi `CustomerApi is not defined`. |
| **2** | `BUG-P4-01` | Admin Products | `Areas/Admin/Views/AdminDashboard/Products.cshtml`<br>`Areas/Admin/Controllers/AdminInventoryController.cs` | **Major** | Bổ sung route template `[HttpGet("Admin/AdminDashboard/EditProduct/{id:int}")]` và sửa href của nút "Sửa" trên bảng sản phẩm thành `?id=@item.Id`. |
| **3** | `BUG-P3-01` & `BUG-P3-02` | Product Reviews | `Controllers/CustomerOrdersApiController.cs`<br>`Views/Catalog/Detail.cshtml` | **Major** | Bổ sung JSON message giải thích rõ khi khách chưa mua hàng (`"Bạn cần mua sản phẩm trước khi đánh giá"`), giao diện hiển thị hộp thông báo thân thiện thay vì `Yêu cầu thất bại (403)`. |
| **4** | `BUG-P4-02` | System Health | `Services/SystemHealthService.cs` | **Major** | Tinh chỉnh phương thức ping SQL database, nâng timeout hoặc dùng query nhẹ `SELECT 1;` thay vì check EF migration để triệt tiêu cảnh báo giả "Ngoại tuyến". |
| **5** | `BUG-P3-03` | Product Reviews Anchor | `Views/Catalog/Detail.cshtml` | **Minor** | Bổ sung `id="reviews"` vào thẻ `<section class="luxury-card">` của khối đánh giá để hashtag `#reviews` tự động cuộn trang. |
| **6** | `BUG-P4-03` | Admin Routing Alias | `Areas/Admin/Controllers/DatabaseConsoleController.cs` | **Minor** | Thêm attribute `[Route("Admin/AdminDashboard/DatabaseConsole")]` làm alias chuyển hướng vào `/admin/databaseconsole`. |
| **7** | `BUG-P4-04` | Blog Editor | `Areas/Admin/Views/AdminDashboard/CreateBlog.cshtml` | **Minor** | Tích hợp bộ soạn thảo trực quan Rich Text (TinyMCE hoặc Quill qua CDN jsdelivr đã được cấp phép CSP). |
| **8** | `BUG-P3-04` | Comparison Semantic | `Views/Catalog/Compare.cshtml` | **UI/UX** | Loại bỏ thẻ `<main>` dư thừa, chỉ giữ lại một thẻ `<main>` duy nhất chuẩn W3C HTML5. |
| **9** | `BUG-P4-05` | Dashboard Responsive | `Areas/Admin/Views/AdminDashboard/Index.cshtml` | **UI/UX** | Bổ sung CSS `overflow-x: auto` cho container bảng và thẻ KPI để triệt tiêu thanh cuộn ngang trên màn hình mobile <= 375px. |
| **10** | `BUG-P4-06` | Admin Navbar Logout | `Areas/Admin/Views/Shared/_AdminLayout.cshtml` | **UI/UX** | Thêm hộp thoại xác nhận `confirm('Bạn có chắc muốn đăng xuất?')` trước khi submit logout. |

---

### 2. Kế Hoạch Phân Bổ Sprint Tuần Tới

- **Ngày 1 (Thứ Hai)**:
  - Khắc phục `ISSUE-2026-WK45-01` (Đơn hàng của tôi `CustomerApi is not defined`).
  - Khắc phục `BUG-P4-01` (Nút sửa sản phẩm Admin).
- **Ngày 2 (Thứ Ba)**:
  - Khắc phục `BUG-P3-01`, `BUG-P3-02`, `BUG-P3-03` (Trải nghiệm Review & thông điệp lỗi thân thiện).
  - Khắc phục `BUG-P4-02` (Sửa lỗi báo động giả CSDL trong System Health).
- **Ngày 3 (Thứ Tư)**:
  - Bổ sung Route alias `BUG-P4-03` và tích hợp Rich Text Editor `BUG-P4-04`.
  - Tối ưu CSS responsive `BUG-P4-05`, sửa ngữ nghĩa DOM `BUG-P3-04`, thêm xác nhận đăng xuất `BUG-P4-06`.
- **Ngày 4 (Thứ Năm)**:
  - Chạy toàn bộ test suite `dotnet test` (419 tests).
  - Triển khai lên Somee qua GitHub Actions và kiểm thử E2E lại bằng Chrome DevTools để đóng toàn bộ sổ bộ lỗi.

