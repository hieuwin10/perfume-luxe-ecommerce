# Chuyên Đề 02: Bảng Tổng Hợp Khiếm Khuyết & Ma Trận Phân Loại Lỗi
**Dự án:** Perfume Luxe E-Commerce Platform  
**Phương pháp phân loại:** Thang đo chuẩn ISTQB / IEEE Standard for Software Defect Classification kết hợp CMMI  

---

## 1. Bảng Ma Trận Tổng Hợp Toàn Bộ Lỗi Đã Xác Thực

| Mã Lỗi | Tên Khiếm Khuyết | Mức Độ | Phân Loại Lỗi | Phân Hệ | URL Ảnh Hưởng | Trạng Thái Thẩm Định |
| :---: | :--- | :---: | :---: | :---: | :--- | :---: |
| **DEF-01** | Lỗi Gián Đoạn Đặt Hàng Do Mâu Thuẫn Enum PaymentMethod | **P0 - Blocker** | Lỗi Logic & API Contract | Commerce | `/checkout`<br>`/api/v1/orders` | **Đã xác thực 100% trên Web Live** |
| **DEF-02** | Sập Máy Chủ HTTP 500 Trang Database Console Quản Trị | **P1 - Critical** | Lỗi Routing / Server Crash | Admin | `/Admin/AdminDashboard/DatabaseConsole` | **Đã xác thực 100% trên Web Live** |
| **DEF-03** | Lỗi 404 Not Found Tại Trang Quản Lý Danh Mục Gốc | **P2 - High** | Broken Link / Missing Route | Admin | `/Admin/CatalogManager` | **Đã xác thực 100% trên Web Live** |
| **DEF-04** | Mã Ưu Đãi Gợi Ý Trên Form Thanh Toán Bị Vô Hiệu Hóa | **P2 - High** | Lỗi Logic Dữ Liệu & UI | Commerce | `/checkout`<br>`/api/v1/vouchers/validate` | **Đã xác thực 100% trên Web Live** |
| **DEF-05** | Vòng Lặp Chuyển Hướng Im Lặng Khi Thiếu Quyền Quản Trị | **P2 - High** | Lỗi Access Control & UX | Admin / Auth | `/Admin/AdminDashboard/*` | **Đã xác thực 100% trên Web Live** |
| **DEF-06** | Tiêu Đề Báo Sai Ngữ Nghĩa Khi Tra Cứu Đơn Thiếu SĐT | **P3 - Medium** | Lỗi Giao Diện & UX Copy | Commerce | `/tra-cuu-don-hang` | **Đã xác thực 100% trên Web Live** |
| **DEF-07** | Lặp Lồng Thẻ `<main>` Vi Phạm Chuẩn HTML5 & WCAG 2.1 | **P3 - Medium** | Tiêu Chuẩn HTML / Accessibility | Storefront | `/discovery-box`<br>`/dat-lich-trai-nghiem`<br>`/blog/*` | **Đã xác thực 100% trên Web Live** |
| **DEF-08** | Khối Banner Flash Sale Để Trống Trên Trang Chủ | **P3 - Medium** | Lỗi Giao Diện & UI State | Storefront | `/` (Homepage) | **Đã xác thực 100% trên Web Live** |
| **DEF-09** | Thông Báo Lỗi Form Hiển Thị Tiếng Anh Kỹ Thuật ProblemDetails | **P3 - Medium** | Lỗi I18n & Client Parsing | Auth / User | `/Account/Register`<br>`/Account/EditProfile` | **Đã xác thực 100% trên Web Live** |
| **DEF-10** | Thiếu Thuộc Tính `rel="noopener noreferrer"` Tại Link Checkout | **P4 - Low** | An Toàn HTML Tab Mới | Commerce | `/checkout` | **Đã xác thực 100% trên Web Live** |

---

## 2. Thống Kê Phân Loại Khiếm Khuyết

### 2.1. Phân loại theo Mức độ nghiêm trọng (Severity Level)
- **P0 - Blocker (Chặn quy trình cốt lõi): 1 lỗi (10%)**  
  *DEF-01: Toàn bộ khách hàng không thể gửi đơn hàng và thanh toán trên toàn hệ thống.*
- **P1 - Critical (Sập hệ thống / Crash server): 1 lỗi (10%)**  
  *DEF-02: HTTP 500 AmbiguousMatchException gây sập màn hình Database Console.*
- **P2 - High (Ảnh hưởng trải nghiệm & điều hướng): 3 lỗi (30%)**  
  *DEF-03 (404 Admin), DEF-04 (Mã giảm giá hỏng), DEF-05 (Chuyển hướng im lặng).*
- **P3 - Medium (Trải nghiệm người dùng, ngữ nghĩa, bản địa hóa): 4 lỗi (40%)**  
  *DEF-06 (Copywriting tra cứu), DEF-07 (Lặp thẻ main), DEF-08 (Flash sale empty), DEF-09 (I18n tiếng Anh).*
- **P4 - Low (Khuyến nghị nâng cao tiêu chuẩn): 1 lỗi (10%)**  
  *DEF-10 (Noopener noreferrer).*

### 2.2. Phân loại theo Bản chất Kỹ thuật (Technical Nature)
```
┌──────────────────────────────────────────────────────────┬───────┬────────────┐
│ Loại Khiếm Khuyết                                        │ Số SL │ Tỷ Lệ %    │
├──────────────────────────────────────────────────────────┼───────┼────────────┤
│ 1. Lỗi Hợp Đồng API & Logic Nghiệp Vụ (API / Logic Error)│ 3     │ 30.0%      │
│ 2. Lỗi Hạ Tầng Định Tuyến & Sập Máy Chủ (Routing / Crash)│ 2     │ 20.0%      │
│ 3. Lỗi Trải Nghiệm & Giao Diện Người Dùng (UI/UX Glitch) │ 2     │ 20.0%      │
│ 4. Tiêu Chuẩn Ngữ Nghĩa W3C & Tiếp Cận (HTML5 / WCAG)    │ 1     │ 10.0%      │
│ 5. Đa Ngôn Ngữ & Bản Địa Hóa (I18n / Localization)       │ 1     │ 10.0%      │
│ 6. An Toàn Điều Hướng Trình Duyệt (HTML Security)        │ 1     │ 10.0%      │
└──────────────────────────────────────────────────────────┴───────┴────────────┘
```

---

## 3. Ma Trận Ảnh Hưởng Theo Từng Màn Hình & Luồng Nghiệp Vụ

```
[Storefront Khách Hàng]
 ├── / (Trang chủ) ──────────────────────────► DEF-08 (Flash Sale rỗng)
 ├── /discovery-box ─────────────────────────► DEF-07 (Lồng thẻ <main>)
 ├── /dat-lich-trai-nghiem ──────────────────► DEF-07 (Lồng thẻ <main>)
 └── /blog/{slug} ───────────────────────────► DEF-07 (Lồng thẻ <main>)

[Thương Mại & Đặt Hàng]
 ├── /checkout (COD Order) ──────────────────► DEF-01 (Blocker - Enum 0)
 ├── /checkout (Voucher Suggestion) ─────────► DEF-04 (Mã giảm giá không hợp lệ)
 ├── /checkout (Privacy Link) ───────────────► DEF-10 (Thiếu noopener)
 └── /tra-cuu-don-hang ──────────────────────► DEF-06 (Báo sai thông tin thiếu phone)

[Quản Trị & Bảo Mật]
 ├── /Admin/AdminDashboard/DatabaseConsole ──► DEF-02 (Critical - Crash 500)
 ├── /Admin/CatalogManager ──────────────────► DEF-03 (404 Missing Index)
 ├── /Admin/AdminDashboard/* (Customer Role) ► DEF-05 (Silent Redirect Loop)
 └── /Account/Register, /Account/EditProfile ► DEF-09 (Lỗi tiếng Anh mặc định)
```
