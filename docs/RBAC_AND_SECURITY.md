# 🛡️ MA TRẬN PHÂN QUYỀN & BẢO MẬT (RBAC & SECURITY MATRIX)

> **Cơ Chế Xác Thực**: Song song **ASP.NET Core Identity Cookie Authentication** (Web Storefront & Admin Portal) và **JWT Bearer Authentication** (RESTful API Mobile/Customer).  
> **Cơ Chế Phân Quyền**: Role-Based Access Control (RBAC) kết hợp Policy-Based Claims.

---

## 1. Ba Vai Trò Cốt Lõi Trong Hệ Thống (User Roles)

```
                       +-----------------------------------+
                       |         Quản Trị Viên             |
                       |         Role: [Admin]             |
                       | - Toàn quyền cấu hình hệ thống    |
                       | - Quản trị CSDL & Sơ đồ ERD       |
                       | - Phân quyền người dùng & Role    |
                       +-----------------+-----------------+
                                         | Thừa kế / Bao hàm
                                         v
                       +-----------------+-----------------+
                       |       Cửa Hàng Trưởng             |
                       |     Role: [StoreManager]          |
                       | - Thống kê vận hành bán lẻ độc quyền
                       | - Quản lý kho, nhập hàng, đổi giá |
                       | - Xử lý đơn hàng, đối soát COD    |
                       | - Duyệt đánh giá, tạo Voucher     |
                       +-----------------+-----------------+
                                         |
                                         v
                       +-----------------+-----------------+
                       |          Khách Hàng               |
                       |       Role: [Customer]            |
                       | - Khảo sát trắc nghiệm AI Quiz    |
                       | - Đặt hàng, theo dõi đơn của mình |
                       | - Viết review sau khi mua hàng    |
                       +-----------------------------------+
```

---

## 2. Ma Trận Phân Quyền Chi Tiết (Access Control Matrix)

| Chức Năng / Điểm Truy Cập (Route) | Khách Hàng (Customer) | Cửa Hàng Trưởng (StoreManager) | Quản Trị Viên (Admin) | Ghi Chú Bảo Mật |
| :--- | :---: | :---: | :---: | :--- |
| **Xem Catalog, Tìm Kiếm, AI Scent Quiz** | ✅ Xem | ✅ Xem | ✅ Xem | Công khai không cần đăng nhập |
| **Thêm Giỏ Hàng, Áp Mã Voucher, Đặt Hàng** | ✅ Thực hiện | ✅ Thực hiện | ✅ Thực hiện | Lưu Session / Cookie an toàn |
| **Xem Đơn Hàng Của Bản Thân & Đổi Profile** | ✅ Chỉ đơn của mình | ✅ Đơn của mình | ✅ Tất cả | Lọc chặt theo `UserId` hiện tại |
| **Viết Nhận Xét Đánh Giá Sản Phẩm** | ✅ Viết | ✅ Viết | ✅ Viết | Yêu cầu kiểm duyệt `IsApproved = false` |
| **Bảng Thống Kê Vận Hành Kho (`/admin/storemanager`)** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Phân tích dòng tiền ca & Tồn kho nguy cấp |
| **Cập Nhật Tồn Kho, Giá Bán Phiên Bản** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Kiểm soát biến động kho hàng |
| **Xử Lý Trạng Thái Đơn Hàng (Xác nhận, Giao hàng)** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Chặn chuyển trạng thái nhảy cóc |
| **Xác Nhận Thu Tiền Mặt COD Shipper** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Chỉ cho phép đơn `Delivered` |
| **Quản Lý & Ban Hành Mã Voucher** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Kiểm tra trùng mã, ngân sách tối đa |
| **Phê Duyệt / Ẩn Đánh Giá Khách Hàng** | ❌ Cấm (403) | ✅ **Toàn Quyền** | ✅ **Toàn Quyền** | Tự động tính lại điểm sao trung bình |
| **Quản Lý Bài Viết Tạp Chí / Blog** | ❌ Cấm (403) | ✅ Đọc & Viết bài | ✅ **Toàn Quyền** | Biên soạn kiến thức mùi hương |
| **Tổng Quan Doanh Nghiệp (`/admin/admindashboard`)** | ❌ Cấm (403) | 🔄 Auto Redirect | ✅ **Toàn Quyền** | Nếu StoreManager vào -> Chuyển hướng sang Store |
| **Phân Quyền Vai Trò User (`ChangeUserRole`)** | ❌ Cấm (403) | ❌ Cấm (403) | ✅ **Độc Quyền Admin** | Chặn Admin tự hạ quyền chính mình |
| **Khóa / Mở Khóa Tài Khoản Người Dùng** | ❌ Cấm (403) | ❌ Cấm (403) | ✅ **Độc Quyền Admin** | Chặn khóa tài khoản Admin cuối cùng |
| **AWS Database Console (`/admin/databaseconsole`)** | ❌ Cấm (403) | ❌ Cấm (403) | ✅ **Độc Quyền Admin** | Quản trị bảng, chạy SQL an toàn |
| **Sơ Đồ Thực Thể ERD Interactive** | ❌ Cấm (403) | ❌ Cấm (403) | ✅ **Độc Quyền Admin** | Xem toàn bộ cấu trúc CSDL trực quan |
| **Cài Đặt Hệ Thống (`/admin/settings`)** | ❌ Cấm (403) | ❌ Cấm (403) | ✅ **Độc Quyền Admin** | Điều chỉnh cấu hình chạy runtime |

---

## 3. Cơ Chế Xác Thực Kép (Dual Authentication Pipeline)

### 3.1. Cookie Authentication (Web Browser)
- Thiết lập cờ `HttpOnly = true`: Chống tấn công đánh cắp Cookie qua XSS.
- Cờ `SecurePolicy = SameAsRequest` / `Always`: Chỉ gửi Cookie qua HTTPS.
- Cờ `SameSite = SameSiteMode.Lax`: Phòng chống triệt để tấn công Cross-Site Request Forgery (CSRF).
- Hạn sử dụng: 14 ngày (Persistent Cookie), tự động gia hạn khi có tương tác (Sliding Expiration).

### 3.2. JWT Bearer Authentication (Mobile / API)
- Sử dụng thuật toán ký điện tử **HMAC-SHA256**.
- Payload chứa Claims tiêu chuẩn: `sub` (User ID), `email`, `role`, `jti` (Token GUID), `exp` (Thời gian hết hạn).
- Hạn dùng Access Token: 60 phút.

---

## 4. Tiêu Chuẩn Phòng Vệ OWASP Top 10

1. **Chống SQL Injection (SQLi)**:
   - 100% truy vấn dữ liệu thông qua Entity Framework Core 8 với cú pháp tham số hóa (Parameterized Queries).
   - Công cụ Visual Query Runner trong Database Console chủ động lọc các từ khóa phá hoại nguy hiểm (`DROP DATABASE`, `ALTER SYSTEM`).
2. **Chống Cross-Site Scripting (XSS)**:
   - Razor Engine tự động mã hóa HTML (HTML Encoding) toàn bộ dữ liệu người dùng nhập xuất ra màn hình.
3. **Chống Cross-Site Request Forgery (CSRF)**:
   - Tất cả Form POST (Cập nhật đơn hàng, đổi tồn kho, tạo voucher, đổi vai trò) đều bắt buộc thuộc tính `[ValidateAntiForgeryToken]` và thẻ `@Html.AntiForgeryToken()`.
4. **Bảo vệ Trạng Thái Đơn Hàng (State Machine Validation)**:
   - Chặn việc gian lận nhảy cóc trạng thái đơn hàng (ví dụ: không thể chuyển từ `Pending` thẳng sang `Delivered` mà phải qua `Confirmed` và `Shipping`).
   - Chặn xác nhận thu COD cho các đơn hàng chưa giao thành công.
