# 🔌 TÀI LIỆU ĐẶC TẢ WEB API (API SPECIFICATION & REFERENCE)

> **Base URL**: `http://localhost:5000/api`  
> **Phiên bản API**: `v1`  
> **Định dạng dữ liệu**: `application/json` (Unicode UTF-8)  
> **Cơ chế xác thực**: `Authorization: Bearer <JWT_ACCESS_TOKEN>`

---

## 1. Cơ Chế Xác Thực API (Authentication)

### 1.1. Đăng Nhập Lấy Token (Customer Login)
- **Endpoint**: `POST /api/v1/customer/login`
- **Quyền hạn**: Công khai (Anonymous)
- **Request Body**:
```json
{
  "email": "customer@perfumeluxe.com",
  "password": "Customer@Password123"
}
```
- **Response 200 OK**:
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-10-11T18:00:00Z",
  "user": {
    "id": "usr-8823-uuid",
    "email": "customer@perfumeluxe.com",
    "fullName": "Nguyễn Hoàng Nam",
    "roles": ["Customer"]
  }
}
```

---

## 2. Các Đầu Mút API Dành Cho Khách Hàng (Customer API)

### 2.1. Lấy Thông Tin Cá Nhân
- **Endpoint**: `GET /api/v1/customer/profile`
- **Headers**: `Authorization: Bearer <token>`
- **Response 200 OK**:
```json
{
  "id": "usr-8823-uuid",
  "fullName": "Nguyễn Hoàng Nam",
  "email": "customer@perfumeluxe.com",
  "phoneNumber": "0901234567",
  "loyaltyTier": "Gold",
  "totalOrders": 5,
  "totalSpent": 18500000.00
}
```

### 2.2. Danh Sách Đơn Hàng Của Bản Thân
- **Endpoint**: `GET /api/v1/customer/orders`
- **Headers**: `Authorization: Bearer <token>`
- **Response 200 OK**:
```json
[
  {
    "orderCode": "LUXE-20261011-001",
    "createdAt": "2026-10-11T09:15:00Z",
    "orderStatus": "Delivered",
    "paymentStatus": "Completed",
    "paymentMethod": "COD",
    "totalAmount": 3850000.00,
    "itemsCount": 1
  }
]
```

### 2.3. Tạo Đánh Giá Sản Phẩm
- **Endpoint**: `POST /api/v1/customer/products/{productId}/reviews`
- **Headers**: `Authorization: Bearer <token>`
- **Request Body**:
```json
{
  "rating": 5,
  "title": "Mùi hương gỗ trầm cực kỳ quý phái và lưu hương trên 12 tiếng",
  "comment": "Chai nước hoa Tom Ford Oud Wood này xứng đáng là kiệt tác. Đóng gói rất sang trọng!"
}
```
- **Response 201 Created**:
```json
{
  "success": true,
  "message": "Đánh giá của bạn đã được gửi và đang chờ kiểm duyệt từ cửa hàng.",
  "reviewId": 42
}
```

---

## 3. Các Đầu Mút Tra Cứu Catalog & Quiz (Public Endpoints)

### 3.1. Danh Sách Sản Phẩm (Lọc & Phân Trang)
- **Endpoint**: `GET /api/v1/catalog/products`
- **Query Params**:
  - `page`: số trang (mặc định 1)
  - `pageSize`: số mục trên trang (mặc định 12)
  - `brandId`: mã thương hiệu
  - `familyId`: mã nhóm hương
  - `search`: từ khóa tìm kiếm
  - `sortBy`: `price-asc`, `price-desc`, `newest`, `rating`
- **Response 200 OK**:
```json
{
  "totalItems": 15,
  "page": 1,
  "pageSize": 12,
  "items": [
    {
      "id": 1,
      "name": "Bleu de Chanel Parfum",
      "slug": "bleu-de-chanel-parfum",
      "brandName": "Chanel",
      "fragranceFamily": "Woody Aromatic",
      "basePrice": 4200000.00,
      "ratingAverage": 4.9,
      "ratingCount": 18,
      "thumbnailUrl": "/images/products/bleu-de-chanel.webp"
    }
  ]
}
```

### 3.2. Thuật Toán Gợi Ý AI Scent Finder Quiz
- **Endpoint**: `POST /api/v1/catalog/quiz/recommend`
- **Request Body**:
```json
{
  "occasion": "Evening Gala",
  "season": "Autumn Winter",
  "scentPreference": "Woody Amber",
  "sillage": "Intense",
  "longevity": "12h+"
}
```
- **Response 200 OK**:
```json
{
  "recommendations": [
    {
      "productId": 3,
      "productName": "Tom Ford Tobacco Vanille",
      "matchScore": 98.5,
      "matchReason": "Hoàn toàn phù hợp với buổi tiệc đêm sang trọng cùng nốt hương thuốc lá ngọt ngào và vani Bourbon quý phái.",
      "slug": "tom-ford-tobacco-vanille",
      "price": 7500000.00
    }
  ]
}
```

---

## 4. Chuẩn Mã Lỗi HTTP & Định Dạng Lỗi (Error Handling)

Khi xảy ra lỗi, API luôn trả về cấu trúc lỗi chuẩn RFC 7807 (Problem Details):
```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "Mã voucher đã hết hạn hoặc không đủ điều kiện áp dụng.",
  "errors": {
    "VoucherCode": ["Mã voucher 'EXPIRED20' đã kết thúc vào ngày 01/10/2026."]
  }
}
```

- `200 OK`: Truy vấn dữ liệu thành công.
- `201 Created`: Tạo mới tài nguyên thành công (Đơn hàng, Đánh giá).
- `400 Bad Request`: Dữ liệu đầu vào không hợp lệ.
- `401 Unauthorized`: Chưa truyền Token hoặc Token đã hết hạn.
- `403 Forbidden`: Người dùng không có quyền truy cập vào chức năng (ví dụ: Customer cố gọi API quản lý kho).
- `404 Not Found`: Không tìm thấy tài nguyên.
- `500 Internal Server Error`: Lỗi hệ thống nội bộ (được ghi log vào Serilog).
