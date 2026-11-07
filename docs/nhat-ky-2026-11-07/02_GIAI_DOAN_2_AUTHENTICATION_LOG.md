# BÁO CÁO KIỂM THỬ GIAI ĐOẠN 2 & VẤN ĐỀ TỒN ĐỌNG (07/11/2026)
## Phân hệ: Authentication, Profile & Customer Journey

---

### 1. Kết Quả Kiểm Thử Thực Tế Bằng Chrome DevTools

Đã tiến hành kiểm thử tương tác thực tế 100% qua Chrome DevTools trên `https://perfumeluxe.somee.com/`:

| Chức Năng | URL Thử Nghiệm | Thao Tác Trực Tiếp | Kết Quả Network | Trạng Thái UI | Đánh Giá |
|:---|:---|:---|:---:|:---|:---:|
| **Đăng Ký Tài Khoản** | `/account/register` | Nhập Họ tên, Email `testvip2026@perfumeluxe.vn`, Mật khẩu `TestVip2026!@#` | `POST /api/v1/auth/register` (200 OK) | Đăng ký thành công, tự động đăng nhập và chuyển hướng sang `/account/profile` | **PASSED** |
| **Quên Mật Khẩu** | `/account/forgotpassword` | Nhập email đã đăng ký và nhấn "Gửi hướng dẫn" | `POST /api/v1/auth/password/forgot` (200 OK) | Hiển thị thông báo màu xanh gửi hướng dẫn thành công | **PASSED** |
| **Đăng Nhập** | `/account/login` | Điền thông tin tài khoản và nhấn "Đăng Nhập" | `POST /api/v1/auth/login` (200 OK) | Đăng nhập thành công, chuyển hướng hồ sơ cá nhân | **PASSED** |
| **Hồ Sơ Cá Nhân VIP** | `/account/profile` | Xem thông tin, thẻ thành viên VIP Club và bấm nút kiểm tra API | `GET /api/v1/loyalty/status` (200 OK) | Hiển thị chuẩn hạng Đồng (Bronze), chiết khấu 0%, điểm thưởng 0 pts | **PASSED** |
| **Chỉnh Sửa Hồ Sơ** | `/account/editprofile` | Cập nhật Họ tên, SĐT `0988777666`, Địa chỉ tại TP.HCM | `PUT /api/v1/customer/profile` (200 OK) | Báo thành công, tên người dùng cập nhật ngay trên navbar | **PASSED** |
| **Tra Cứu Đơn Hàng** | `/tra-cuu-don-hang` | Tra cứu theo mã đơn và SĐT qua cả Live API và Form GET | `GET /api/v1/orders/track` (404) | Hiển thị thông báo chuẩn khi đơn hàng chưa tồn tại | **PASSED** |
| **Đơn Hàng Của Tôi** | `/account/myorders` | Tải trang danh sách đơn hàng của tài khoản đang đăng nhập | `GET /account/myorders` (200 OK) | Hiển thị thông báo đỏ: `CustomerApi is not defined` | **FAILED (GHI NHẬN TỒN ĐỌNG)** |

---

### 2. Chi Tiết Lỗi Tồn Đọng Cần Sửa Vào Tuần Tới

- **Mã lỗi**: `ISSUE-2026-WK45-01`
- **Các trang bị ảnh hưởng**:
  1. `https://perfumeluxe.somee.com/account/myorders`
  2. `https://perfumeluxe.somee.com/account/orderdetails/{id}`
- **Nguyên nhân kỹ thuật**:
  - Trong `_Layout.cshtml`:
    ```html
    <script src="/js/customer-api.js?v=..." defer></script>
    ```
    Thuộc tính `defer` hoãn thực thi file `customer-api.js` cho đến khi toàn bộ HTML parse xong.
  - Trong `MyOrders.cshtml` và `OrderDetails.cshtml`:
    Đoạn script tại `@section Scripts` là một hàm IIFE chạy ngay lập tức khi trình duyệt render đến:
    ```javascript
    (async function loadOrders(){
        const orders = await CustomerApi.get('/api/v1/customer/orders');
        ...
    })();
    ```
    Do inline script không có `defer`, nó thực thi **trước** file `customer-api.js`, làm phát sinh lỗi `CustomerApi is not defined`.

---

### 3. Giải Pháp Khắc Phục Vào Tuần Tới

Sửa trong `MyOrders.cshtml` và `OrderDetails.cshtml`: Bọc lời gọi hàm `loadOrders()` vào sự kiện `DOMContentLoaded` hoặc bỏ thuộc tính `defer` ở thẻ script layout chính:
```javascript
document.addEventListener('DOMContentLoaded', () => {
    loadOrders();
});
```

