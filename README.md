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
3. **Mở trình duyệt**:
   * Cửa hàng Storefront: `https://localhost:5001` (hoặc `http://localhost:5000`)
   * Bảng Điều Khiển CSDL (AWS Style): `https://localhost:5001/admin/databaseconsole`
   * Web Database Adminer: `http://localhost:8088`

### Cách 2: Chạy Toàn Bộ Hệ Thống Bằng Docker (Full Container)
```bash
docker compose up --build -d
```
Truy cập tại: `http://localhost:5000`

---

## 💎 3. Các Tính Năng Độc Đáo Nổi Bật

* **Phân Hệ Quản Trị CSDL Trực Quan (AWS-Style DB Console)**:
  - Xem danh sách bảng, số bản ghi, dung lượng lưu trữ trực quan.
  - **Visual CRUD**: Thêm, sửa, xóa trực tiếp từng dòng dữ liệu mà không cần cài đặt SSMS.
  - **SQL Query Runner**: Chạy lệnh SQL tùy chỉnh, hiển thị bảng kết quả và đo tốc độ thực thi (ms).
  - **1-Click Seeder**: Nạp dữ liệu 15+ chai nước hoa nổi tiếng (Dior Sauvage Elixir, Bleu de Chanel, Creed Aventus, Tom Ford, Kilian...) từ script SQL riêng mà không hardcode vào mã nguồn C#.
* **Giao Diện Luxury Dark & Light Mode**:
  - Tông màu hoàng gia Champagne Gold & Velvet Black.
  - Tự động nhận diện theme trình duyệt (Cốc Cốc, Chrome) và có nút chuyển đổi thủ công.
* **Kim Tự Tháp 3 Tầng Hương (Scent Pyramid)**: Hiển thị trực quan Hương Đầu (Top notes), Hương Giữa (Heart notes) và Hương Cuối (Base notes).
* **AI Fragrance Finder Quiz**: Bài trắc nghiệm 3 câu hỏi gợi ý nước hoa tương thích với cá tính người dùng.
* **Đầy Đủ Giỏ Hàng & Thanh Toán**: Mini-cart trượt mượt mà, áp mã giảm giá (Vouchers), cổng thanh toán COD, VNPAY, MoMo.
* **Chuẩn Hóa SEO Google**: Tự sinh `sitemap.xml`, `robots.txt`, dữ liệu cấu trúc `Schema.org/Product` và `Schema.org/BlogPosting`.

---

## 📌 4. Hướng Dẫn Dành Cho AI / Dev Tiếp Tục Phát Triển (Next Tasks)
1. Tích hợp thanh toán thật qua SDK VNPAY / MoMo API thực tế.
2. Thêm hệ thống gửi Email xác nhận đơn hàng qua SMTP (SendGrid / Gmail).
3. Mở rộng tính năng đánh giá có upload ảnh feedback từ khách hàng.
4. Triển khai CI/CD qua GitHub Actions lên Cloud (Azure / AWS / VPS).
