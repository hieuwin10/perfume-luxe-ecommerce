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

### Cách 1: Chạy SQL Server, Redis, Mailpit bằng Docker và ứng dụng bằng .NET
1. Sao chép `.env.example` thành `.env`, đổi giá trị mẫu `MSSQL_SA_PASSWORD` thành mật khẩu mạnh. Nếu dùng lại SQL volume cũ, giữ đúng mật khẩu SA đã khởi tạo volume.
2. Khởi động dịch vụ nền:
   ```bash
   docker compose -f docker-compose.infra.yml up -d
   ```
3. Cấu hình các biến môi trường cho ứng dụng trước khi chạy: `ConnectionStrings__DefaultConnection` trỏ tới SQL Server tại `localhost,1433`; đặt `Database__BackupPath=/var/opt/mssql/backup`, `Storage__RootPath=src/Perfume.Web/App_Data`, `InitialAdmin__Email` và `InitialAdmin__Password` để tạo Admin lần đầu; đặt `Smtp__Enabled=true`, `Smtp__Host=localhost`, `Smtp__Port=1025`.
4. Chạy `dotnet watch --project src/Perfume.Web run`.
5. Truy cập cửa hàng ở cổng ứng dụng, Adminer tại `http://localhost:8088`, Mailpit tại `http://localhost:8025`.

Adminer dùng máy chủ `localhost` khi truy cập từ máy host, user `sa`, mật khẩu lấy từ file `.env`, database `PerfumeDb`. Không có mật khẩu quản trị mặc định trong source.

### Cách 2: Chạy toàn bộ hệ thống bằng Docker
Sao chép và cấu hình `.env.example` thành `.env`, sau đó chạy:
```bash
docker compose up --build -d
```
Cửa hàng: `http://localhost:5000`; Mailpit: `http://localhost:8025`. Mật khẩu SQL và Admin lần đầu phải được đặt trong `.env`; Docker Compose giữ nguyên volume dữ liệu SQL hiện có.

---

## 💎 3. Hướng Dẫn Chi Tiết Các Phân Hệ & Tính Năng Nổi Bật

### 1. Phân Hệ Quản Trị CSDL Trực Quan (AWS-Style DB Console)
* **Đường dẫn truy cập**: `/admin/databaseconsole` (chỉ role Admin)
* **Các tính năng cốt lõi**:
  - **1-Click Seeder**: Nạp toàn bộ dữ liệu mẫu (15+ chai nước hoa kinh điển, 8 thương hiệu lớn, kim tự tháp hương và mã giảm giá) chỉ bằng 1 nút bấm trực tiếp từ file [scripts/seed_perfume_data.sql](scripts/seed_perfume_data.sql) mà không cần cấu hình SSMS phức tạp.
  - **Table Explorer & Health Metrics**: Xem trực quan danh sách bảng, tổng số bản ghi (rows), kích thước dữ liệu (Data & Index size) theo phong cách AWS RDS/DynamoDB Console.
  - **Visual CRUD**:
    + Thêm mới bản ghi bằng popup form tự động ánh xạ kiểu dữ liệu cột.
    + Chỉnh sửa trực tiếp từng trường dữ liệu với nút *Edit*.
    + Xóa an toàn bản ghi với xác nhận cảnh báo.
  - **SQL Query Runner**:
    + Chỉ kết nối database ứng dụng `PerfumeDb`; các lệnh đổi database hoặc tham chiếu database khác bị chặn.
    + Lệnh có thể ghi dữ liệu yêu cầu xác nhận; audit lưu người thao tác, loại hành động, kết quả và mã băm câu lệnh, không lưu nội dung nhạy cảm.
    + Nâng cấp schema dùng thay đổi cộng thêm và sao lưu trước khi sửa database hiện có.

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

### 4. Giỏ hàng và thanh toán COD (Cart & Checkout)
* **Mini-Cart trượt (Slide-in Drawer)**: Thêm vào giỏ mượt mà không reload trang, cập nhật số lượng và tổng tiền realtime.
* **Mã Khuyến Mãi (Vouchers)**:
  - Nhập mã `LUXE10` để được giảm ngay **10%** tổng đơn hàng.
  - Nhập mã `LUXE20` để được giảm ngay **20%** tổng đơn hàng.
* Hiện chỉ bật COD. Đơn chỉ chuyển sang đã thanh toán khi cửa hàng xác nhận đã thu tiền; VNPAY và MoMo chưa bật.

### 5. Giao Diện Chuẩn Haute Parfumerie (Dark & Light Theme)
* Bảng màu chủ đạo: **Champagne Gold (`#D4AF37`)** phối nền **Velvet Black / Pure White**.
* Nút chuyển đổi giao diện Dark/Light mượt mà ở góc trên thanh điều hướng, tự động lưu tuỳ chọn vào LocalStorage.
* Typography cao cấp chuẩn phong cách tạp chí thời trang xa xỉ (Vogue / Harper's Bazaar).

### 6. Chuẩn Hóa Kỹ Thuật & SEO Google Tối Ưu
* Tự động sinh `sitemap.xml` và `robots.txt` cho toàn bộ danh mục và sản phẩm.
* Tích hợp dữ liệu cấu trúc chuẩn Google Rich Results: `Schema.org/Product` và `Schema.org/BlogPosting`.
* Tối ưu LCP (Largest Contentful Paint) với lazy-loading ảnh và hệ thống cache Redis phân tán.

---

## 🛍️ Vai trò và vận hành cửa hàng
- Admin quản lý hệ thống, vai trò, Database Console và ERD.
- StoreManager xem thống kê theo ngày, xuất CSV, xử lý đơn hàng, sản phẩm/phiên bản/tồn kho, voucher và duyệt đánh giá.
- Customer sửa hồ sơ, xem đơn của mình và đánh giá sản phẩm đã mua, giao thành công. Đánh giá và ảnh chỉ công khai sau khi duyệt.
- Khách vãng lai được duyệt sản phẩm, dùng giỏ hàng, đặt COD và nhận liên kết theo dõi riêng qua email.
- Email cục bộ được gửi vào Mailpit. Lỗi SMTP không làm mất đơn hàng.

## 📝 Bài viết và SEO
Trang Blog và các trường SEO hiện có được giữ nguyên. Màn quản lý bài viết, quy trình nháp/xuất bản và phần nâng cấp SEO được để cho giai đoạn sau..
