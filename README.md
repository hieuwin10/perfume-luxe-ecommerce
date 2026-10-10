# 🌟 PERFUME LUXE - HAUTE PARFUMERIE E-COMMERCE

> **Nền tảng thương mại điện tử nước hoa cao cấp xây dựng bằng 100% C# (.NET 8.0 LTS)**  
> Áp dụng kiến trúc chuẩn **Clean Architecture (4 Tầng)**, hỗ trợ môi trường kép **Docker song song Localhost**, phân hệ **Admin Database Console trực quan phong cách AWS Cloud**, và chuẩn hóa **SEO Google**.

---

## 🏛️ 1. Cấu Trúc Kiến Trúc (Clean Architecture)

```
dotnet/
├── src/
│   ├── Perfume.Domain/           # Thực thể cốt lõi: 3 Tầng hương (Top/Heart/Base), Nồng độ, Thương hiệu, Đơn hàng
│   ├── Perfume.Application/      # Use-cases, DTOs, Service Contracts, Fragrance Finder Quiz logic
│   ├── Perfume.Infrastructure/   # EF Core 8 (SQL Server), Dapper, Identity, Redis Cache, Database Console Service
│   └── Perfume.Web/              # ASP.NET Core 8 MVC & Web API, Luxury Dark/Light Storefront & AWS-style Admin
├── scripts/
│   └── seed_perfume_data.sql     # Script SQL nạp 15+ chai nước hoa kinh điển, vouchers, nốt hương (Độc lập, không hardcode vào C#)
├── docker-compose.infra.yml      # SQL Server 2022 + Redis + Adminer cho Localhost dev
├── docker-compose.yml            # Đóng gói Full Container cho toàn bộ hệ thống
└── Dockerfile                    # Multi-stage Docker build .NET 8 LTS
```

---

## 🚀 2. Hướng Dẫn Chạy Dự Án

### Yêu Cầu Cài Đặt:
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Cách 1: Chạy Dịch Vụ Nền Bằng Docker + Chạy Code Bằng Localhost (Dev Nhanh Nhất)
1. **Khởi động SQL Server và Redis**:
   ```bash
   docker compose -f docker-compose.infra.yml up -d
   ```
2. **Khởi động Web App với Hot Reload**:
   ```bash
   dotnet watch --project src/Perfume.Web run
   ```
3. **Truy cập các cổng dịch vụ**:
   * **Cửa hàng Storefront**: `https://localhost:5001` (hoặc `http://localhost:5000`)
   * **Bảng Điều Khiển CSDL (AWS-Style DB Console)**: `https://localhost:5001/admin/databaseconsole`
   * **Quản trị CSDL Web (Adminer)**: `http://localhost:8088`

### 🔑 Thông Số Đăng Nhập Quản Trị CSDL (Adminer & SQL Server)

Khi truy cập vào `http://localhost:8088`, điền thông tin đăng nhập như sau:

| Trường thông tin | Giá trị điền | Ghi chú |
| :--- | :--- | :--- |
| **Hệ thống (System)** | `MS SQL` | Chọn trong danh sách thả xuống |
| **Máy chủ (Server)** | `sqlserver` | Tên container SQL Server trong mạng Docker |
| **Tên người dùng (User)** | `sa` | Tài khoản quản trị cấp cao của SQL Server |
| **Mật khẩu (Password)** | `PerfumePass@2026!` | Mật khẩu thiết lập trong `docker-compose.infra.yml` |
| **Cơ sở dữ liệu (Database)** | `PerfumeDb` | *(Tùy chọn)* Điền để vào thẳng database của dự án |

---

### Cách 2: Chạy Toàn Bộ Hệ Thống Bằng Docker (Full Container)
```bash
docker compose up --build -d
```
* Storefront & Admin: `http://localhost:5000`

---

## 💎 3. Hướng Dẫn Chi Tiết Các Phân Hệ & Tính Năng Nổi Bật

### 1. Phân Hệ Quản Trị CSDL Trực Quan (AWS-Style DB Console)
* **Đường dẫn truy cập**: `/admin/databaseconsole`
* **Các tính năng cốt lõi**:
  - **1-Click Seeder**: Nạp toàn bộ dữ liệu mẫu (15+ chai nước hoa kinh điển, 8 thương hiệu lớn, kim tự tháp hương và mã giảm giá) chỉ bằng 1 nút bấm trực tiếp từ file [scripts/seed_perfume_data.sql](file:///scripts/seed_perfume_data.sql) mà không cần cấu hình SSMS phức tạp.
  - **Table Explorer & Health Metrics**: Xem trực quan danh sách bảng, tổng số bản ghi (rows), kích thước dữ liệu (Data & Index size) theo phong cách AWS RDS/DynamoDB Console.
  - **Visual CRUD**:
    + Thêm mới bản ghi bằng popup form tự động ánh xạ kiểu dữ liệu cột.
    + Chỉnh sửa trực tiếp từng trường dữ liệu với nút *Edit*.
    + Xóa an toàn bản ghi với xác nhận cảnh báo.
  - **SQL Query Runner**: 
    + Khung soạn thảo lệnh T-SQL tự do (`SELECT`, `JOIN`, `UPDATE`, `GROUP BY`).
    + Tự động render bảng kết quả linh hoạt và hiển thị thời gian phản hồi thực thi tính bằng mili-giây (ms benchmark).

### 2. Trắc Nghiệm Mùi Hương Thông Minh (AI Fragrance Finder Quiz)
* **Đường dẫn truy cập**: `/catalog/quiz`
* **Nguyên lý hoạt động**:
  - Trải nghiệm 3 bước trắc nghiệm trực quan (Không gian sử dụng, Cá tính/Phong cách yêu thích, và Khung cảnh/Thời điểm).
  - Hệ thống tự động phân tích ma trận nốt hương và nhóm hương (Fragrance Families) tương thích.
  - Trả về danh sách nước hoa đề xuất hoàn hảo nhất kèm điểm số tương thích và nút mua hàng ngay.

### 3. Kim Tự Tháp 3 Tầng Hương (Scent Pyramid)
* **Vị trí hiển thị**: Trang chi tiết sản phẩm (`/catalog/detail/{slug}`)
* **Thiết kế phân tầng trực quan**:
  - **Hương Đầu (Top Notes)**: Tươi sáng, lan tỏa ngay trong 15 phút đầu (Cam Bergamot, Quả mọng, Tiêu hồng...).
  - **Hương Giữa (Heart Notes)**: Linh hồn của chai nước hoa kéo dài 3-5 giờ (Hoa hồng Damask, Nhài Sambac, Diên vĩ...).
  - **Hương Cuối (Base Notes)**: Tầng hương lưu lại bền bỉ nhất trên da 8-12 giờ (Gỗ đàn hương, Hổ phách, Xạ hương trắng, Vani...).

### 4. Hệ Thống Giỏ Hàng & Thanh Toán Toàn Diện (Cart & Checkout)
* **Mini-Cart trượt (Slide-in Drawer)**: Thêm vào giỏ mượt mà không reload trang, cập nhật số lượng và tổng tiền realtime.
* **Mã Khuyến Mãi (Vouchers)**:
  - Nhập mã `LUXE10` để được giảm ngay **10%** tổng đơn hàng.
  - Nhập mã `LUXE20` để được giảm ngay **20%** tổng đơn hàng.
* **Đa Dạng Phương Thức Thanh Toán**:
  - Thanh toán khi nhận hàng (COD).
  - Chuyển khoản ngân hàng / Cổng VNPAY QR Code.
  - Ví điện tử MoMo.

### 5. Giao Diện Chuẩn Haute Parfumerie (Dark & Light Theme)
* Bảng màu chủ đạo: **Champagne Gold (`#D4AF37`)** phối nền **Velvet Black / Pure White**.
* Nút chuyển đổi giao diện Dark/Light mượt mà ở góc trên thanh điều hướng, tự động lưu tuỳ chọn vào LocalStorage.
* Typography cao cấp chuẩn phong cách tạp chí thời trang xa xỉ (Vogue / Harper's Bazaar).

### 6. Chuẩn Hóa Kỹ Thuật & SEO Google Tối Ưu
* Tự động sinh `sitemap.xml` và `robots.txt` cho toàn bộ danh mục và sản phẩm.
* Tích hợp dữ liệu cấu trúc chuẩn Google Rich Results: `Schema.org/Product` và `Schema.org/BlogPosting`.
* Tối ưu LCP (Largest Contentful Paint) với lazy-loading ảnh và hệ thống cache Redis phân tán.

---

## 📌 4. Hướng Dẫn Dành Cho AI / Dev Tiếp Tục Phát Triển (Next Tasks)
1. Tích hợp thanh toán thật qua SDK VNPAY / MoMo API thực tế.
2. Thêm hệ thống gửi Email xác nhận đơn hàng qua SMTP (SendGrid / Gmail).
3. Mở rộng tính năng đánh giá có upload ảnh feedback từ khách hàng.
4. Triển khai CI/CD qua GitHub Actions lên Cloud (Azure / AWS / VPS).
