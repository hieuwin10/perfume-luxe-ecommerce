# 🌟 PERFUME LUXE - HAUTE PARFUMERIE & NICHE E-COMMERCE

> **Nền tảng thương mại điện tử nước hoa cao cấp xây dựng bằng 100% C# (.NET 8.0 LTS)**  
> Áp dụng kiến trúc chuẩn **Clean Architecture (4 Tầng)**, chuẩn hóa kỹ thuật theo tiêu chuẩn **BMAD Framework**, hỗ trợ môi trường kép **Docker song song Localhost**, phân hệ **Admin Database Console trực quan phong cách AWS Cloud**, thanh toán **COD & VietQR tự động**, và bộ kiểm thử **411 Tests (100% Green)**.

---

## 🏛️ 1. Cấu Trúc Dự Án (Clean Architecture)

```text
perfume-luxe-ecommerce/
├── src/
│   ├── Perfume.Domain/           # Thực thể cốt lõi: 3 Tầng hương (Top/Heart/Base), Nồng độ, Thương hiệu, Đơn hàng, FlashSale, Loyalty
│   ├── Perfume.Application/      # Use-cases, DTOs, Service Contracts, Fragrance Finder Quiz logic, Discovery Box, Loyalty
│   ├── Perfume.Infrastructure/   # EF Core 8 (SQL Server), Schema Upgrader V1-V6, Identity, In-Memory/Redis Cache, Database Console
│   └── Perfume.Web/              # ASP.NET Core 8 MVC & Web API, Luxury Dark/Light Storefront, Dual Auth & AWS-style Admin
├── docs/                         # Toàn bộ hồ sơ tài liệu kỹ thuật, nghiệp vụ & BMAD Framework
│   ├── BMAD.md                   # Tài liệu chuẩn hóa BMAD 4 trụ cột (Business, Model, Architecture, Database)
│   ├── bmad/                     # Chi tiết các phân hệ BMAD (BUSINESS, MODEL, ARCHITECTURE, DATABASE, EPICS...)
│   ├── ARCHITECTURE.md           # Kiến trúc Clean Architecture, Design Patterns, Middleware Pipeline & Dual Auth
│   ├── DATABASE_SCHEMA.md        # Từ điển CSDL, sơ đồ ERD Mermaid, Migration V1-V6 & B-Tree Indexing
│   ├── BUSINESS_FEATURES.md      # Đặc tả nghiệp vụ: Tháp hương, Quiz AI, Box, Flash Sale, VIP Loyalty, VietQR
│   ├── RBAC_AND_SECURITY.md      # Ma trận phân quyền 4 cấp (Admin, StoreManager, Staff, Customer), Dual Auth
│   ├── STORE_MANAGER_AND_ADMIN_GUIDE.md # Sổ tay vận hành Dashboard: Phân tích tài chính P&L, quản trị tồn kho
│   ├── API_REFERENCE.md          # Đặc tả RESTful Web API cho Mobile App/Frontend SPA, JWT Bearer Auth
│   └── DEPLOYMENT_AND_DEVOPS.md  # Triển khai Docker, biến môi trường .env, Migration tự động & Backup
├── scripts/                      # Kịch bản SQL Server Seeding tinh gọn
│   ├── seed_perfume_data.sql     # Script SQL nạp 15+ chai nước hoa kinh điển (Dùng cho nút 1-Click Seed trên Web)
│   └── seed_extended_data.sql    # Bộ dữ liệu mẫu mở rộng hoàn chỉnh (sản phẩm, đơn hàng, đánh giá, banner)
├── tests/
│   └── Perfume.Tests/            # 411 Automated Unit & Integration Tests (100% Passed)
├── docker-compose.infra.yml      # SQL Server 2022 + Redis + Adminer cho Localhost dev
├── docker-compose.yml            # Đóng gói Full Container cho toàn bộ hệ thống
└── Dockerfile                    # Multi-stage Docker build .NET 8 LTS
```

---

## 📚 2. Hồ Sơ Kỹ Thuật Chuẩn BMAD (`docs/bmad/`)

Dự án được tài liệu hóa đồng bộ theo khung tiêu chuẩn **BMAD Framework** (**B**usiness — **M**odel — **A**rchitecture — **D**atabase) đặt trọn vẹn trong thư mục [`docs/`](docs/README.md):

* 📘 [**`docs/BMAD.md`**](docs/BMAD.md): Bản tổng hợp điều hướng 4 trụ cột BMAD của toàn bộ hệ thống.
* 💎 [**`docs/bmad/BUSINESS.md`**](docs/bmad/BUSINESS.md): Đặc tả phân khúc Haute Parfumerie, Customer Journeys và 6 tính năng độc bản.
* 🗄️ [**`docs/bmad/MODEL.md`**](docs/bmad/MODEL.md): Sơ đồ quan hệ thực thể Mermaid ERD, danh mục Domain Entities, Enums và DTOs.
* 🏛️ [**`docs/bmad/ARCHITECTURE.md`**](docs/bmad/ARCHITECTURE.md): Kiến trúc Clean Architecture 4 tầng, Dual Auth, Caching In-Memory và luồng checkout Serializable.
* ⚡ [**`docs/bmad/DATABASE.md`**](docs/bmad/DATABASE.md): Động cơ Schema Migration V1 - V6, chiến lược đánh chỉ mục B-Tree Indexing.
* 📋 [**`docs/bmad/EPICS_AND_STORIES.md`**](docs/bmad/EPICS_AND_STORIES.md): Danh mục 8 Epics (EPC-01 đến EPC-08) và tiêu chuẩn nghiệm thu (DoD).
* 📝 [**`docs/bmad/CODING_STANDARDS.md`**](docs/bmad/CODING_STANDARDS.md): Nguyên tắc Clean Code, SOLID, chống N+1 và quy định comment tiếng Việt không dấu.
* 🛠️ [**`docs/bmad/TECH_STACK.md`**](docs/bmad/TECH_STACK.md): Ngăn xếp công nghệ .NET 8 LTS, EF Core 8, SQL Server 2022, xUnit, Moq.

---

## 🚀 3. Hướng Dẫn Chạy Dự Án

### Yêu Cầu Cài Đặt:
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Cách 1: Chạy SQL Server, Redis, Mailpit bằng Docker và ứng dụng bằng .NET
1. Sao chép `.env.example` thành `.env`, đổi giá trị mẫu `MSSQL_SA_PASSWORD` thành mật khẩu mạnh. Nếu dùng lại SQL volume cũ, giữ đúng mật khẩu SA đã khởi tạo volume.
2. Khởi động dịch vụ nền:
   ```bash
   docker compose -f docker-compose.infra.yml up -d
   ```
3. Cấu hình các biến môi trường cho ứng dụng trước khi chạy: `ConnectionStrings__DefaultConnection` trỏ tới SQL Server tại `localhost,1433`; đặt `Database__BackupPath=/var/opt/mssql/backup`, `Storage__RootPath=src/Perfume.Web/App_Data`, `InitialAdmin__Email` và `InitialAdmin__Password` để tạo Admin lần đầu; đặt `Smtp__Enabled=true`, `Smtp__Host=localhost`, `Smtp__Port=1025`. Khi deploy, đặt `Seo__SiteBaseUrl` (hoặc `SEO_SITE_BASE_URL` trong `.env` Docker) thành domain HTTPS chính để canonical, Open Graph và sitemap dùng thống nhất một host.
4. Chạy ứng dụng:
   ```bash
   dotnet watch --project src/Perfume.Web run
   ```
5. Truy cập:
   - Cửa hàng (Storefront): `http://localhost:5000`
   - Bảng điều khiển Quản trị CSDL: `http://localhost:5000/admin/databaseconsole`
   - Bảng điều khiển Quản lý Cửa hàng: `http://localhost:5000/admin/storemanager`
   - Báo cáo tài chính P&L: `http://localhost:5000/admin/financialreport`
   - Adminer quản lý DB: `http://localhost:8088`
   - Mailpit xem email thử nghiệm: `http://localhost:8025`

> **Lưu ý Adminer**: Dùng máy chủ `localhost` khi truy cập từ máy host, user `sa`, mật khẩu lấy từ file `.env`, database `PerfumeDb`. Không có mật khẩu quản trị mặc định trong source code.

### Cách 2: Chạy toàn bộ hệ thống bằng Docker
Sao chép và cấu hình `.env.example` thành `.env`, sau đó chạy:
```bash
docker compose up --build -d
```
Cửa hàng: `http://localhost:5000`; Mailpit: `http://localhost:8025`. Mật khẩu SQL và Admin lần đầu phải được đặt trong `.env`; Docker Compose giữ nguyên volume dữ liệu SQL hiện có.

---

## 💎 4. Hướng Dẫn Chi Tiết Các Phân Hệ & Tính Năng Nổi Bật

### 1. Phân Hệ Quản Trị CSDL Trực Quan (AWS-Style DB Console)
* **Đường dẫn truy cập**: `/admin/databaseconsole` (chỉ role `Admin`)
* **Các tính năng cốt lõi**:
  - **1-Click Seeder**: Nạp toàn bộ dữ liệu mẫu (15+ chai nước hoa kinh điển, 8 thương hiệu lớn, kim tự tháp hương và mã giảm giá) chỉ bằng 1 nút bấm trực tiếp từ file [`scripts/seed_perfume_data.sql`](scripts/seed_perfume_data.sql) mà không cần cấu hình SSMS phức tạp.
  - **Table Explorer & Health Metrics**: Xem trực quan danh sách bảng, tổng số bản ghi (rows), kích thước dữ liệu (Data & Index size) theo phong cách AWS RDS/DynamoDB Console.
  - **Visual CRUD**:
    + Thêm mới bản ghi bằng popup form tự động ánh xạ kiểu dữ liệu cột.
    + Chỉnh sửa trực tiếp từng trường dữ liệu với nút *Edit*.
    + Xóa an toàn bản ghi với xác nhận cảnh báo.
  - **SQL Query Runner**:
    + Chỉ kết nối database ứng dụng `PerfumeDb`; các lệnh đổi database hoặc tham chiếu database khác bị chặn.
    + Lệnh có thể ghi dữ liệu yêu cầu xác nhận; audit lưu người thao tác, loại hành động, kết quả và mã băm câu lệnh, không lưu nội dung nhạy cảm.
    + Nâng cấp schema dùng thay đổi cộng thêm và sao lưu trước khi sửa database hiện có.

### 2. Trắc Nghiệm Mùi Hương Thông Minh Bằng AI (AI Fragrance Finder Quiz)
* **Đường dẫn truy cập**: `/catalog/quiz`
* **Nguyên lý hoạt động**:
  - Trải nghiệm trắc nghiệm ma trận 4 chiều (Không gian sử dụng, Cá tính/Phong cách yêu thích, Cường độ tỏa hương và Nhóm nốt hương mong muốn).
  - Hệ thống tính toán điểm Match Score chuẩn hóa từ 85% đến 99% và gán danh hiệu cá tính mùi hương độc bản (*The Sovereign Aristocrat*, *The Velvet Enchantress*, *The Ethereal Luminary*...).
  - Trả về danh sách Top 4 nước hoa đề xuất hoàn hảo nhất kèm lý do chi tiết và nút "Thêm vào giỏ" trực tiếp.

### 3. Tùy Biến Hộp Quà Khám Phá (Discovery Box Builder)
* **Đường dẫn truy cập**: `/discovery-box`
* **Trải nghiệm cá nhân hóa**:
  - Tự chọn 3 đến 5 ống chiết VIP (dung tích 2ml hoặc 5ml).
  - Chọn 3 kiểu đóng gói hoàng gia: *Hộp Gỗ Sơn Mài Hoàng Gia* (ClassicWood), *Hộp Nhung Đen Huyền Bí* (RoyalVelvet), *Túi Da Thủ Công Viền Vàng* (ArtisanLeather).
  - Nhập lời chúc in thiệp ép kim với 3 font chữ thanh lịch (*Playfair Display*, *Dancing Script*, *Cormorant Garamond*).
  - Đơn giá ống mẫu được tự động tính theo tỷ lệ dung tích so với giá gốc chai fullbox.

### 4. Chương Trình Khách Hàng Thân Thiết VIP (Loyalty Engine)
* Tự động thăng hạng theo tổng chi tiêu thực tế tích lũy từ các đơn giao thành công hoặc đã thanh toán:
  - **Bronze**: Mặc định (< 5.000.000₫)
  - **Silver**: ≥ 5.000.000₫ (Giảm tự động 3%)
  - **Gold**: ≥ 15.000.000₫ (Giảm tự động 7%)
  - **Diamond**: ≥ 30.000.000₫ (Giảm tự động 12%)
* Tích lũy điểm thưởng: 10.000₫ = 1 điểm. Hỗ trợ hiển thị thanh tiến trình thăng hạng và số tiền còn thiếu.

### 5. Sự Kiện Flash Sale Giờ Vàng Độc Bản
* Đếm ngược thời gian thực theo giây UTC (`StartTime <= now < EndTime`).
* Quản lý hạn mức tồn kho riêng biệt (`StockLimit` và `SoldQuantity`), ngăn chặn tuyệt đối tình trạng bán vượt số lượng.

### 6. Kim Tự Tháp 3 Tầng Hương (Scent Pyramid)
* **Vị trí hiển thị**: Trang chi tiết sản phẩm (`/catalog/detail/{slug}`)
* **Thiết kế phân tầng trực quan**:
  - **Hương Đầu (Top Notes)**: Tươi sáng, lan tỏa ngay trong 15 phút đầu (Cam Bergamot, Quả mọng, Tiêu hồng...).
  - **Hương Giữa (Heart Notes)**: Linh hồn của chai nước hoa kéo dài 3-5 giờ (Hoa hồng Damask, Nhài Sambac, Diên vĩ...).
  - **Hương Cuối (Base Notes)**: Tầng hương lưu lại bền bỉ nhất trên da 8-12 giờ (Gỗ đàn hương, Hổ phách, Xạ hương trắng, Vani...).

### 7. Giỏ Hàng Kép & Thanh Toán Linh Hoạt (Cart & Checkout)
* **Giỏ Hàng Kép (Dual Cart)**: Hỗ trợ khách vãng lai qua token SHA-256 an toàn trong cookie (30 ngày); tự động gộp giỏ hàng vào tài khoản thành viên khi đăng nhập (`MergeGuestCartAsync`).
* **Phương Thức Thanh Toán**:
  - **COD**: Thanh toán tiền mặt khi nhận hàng.
  - **VietQR**: Tự động sinh mã Vietcombank QR Code động khớp với số tiền và mã đơn hàng.
* **Mã Khuyến Mãi (Vouchers)**: Kiểm tra tính hợp lệ, thời hạn, ngân sách và giới hạn mức giảm giá realtime qua AJAX.

### 8. Tra Cứu Đơn Hàng Realtime Không Cần Đăng Nhập
* **Đường dẫn**: `/tra-cuu-don-hang`
* Khách hàng chỉ cần nhập mã đơn (`LUXE-YYYYMMDD-...`) và số điện thoại đặt hàng để kiểm tra tiến trình 5 giai đoạn: *Chờ Xác Nhận → Đã Xác Nhận → Đang Vận Chuyển → Đã Giao → Hoàn Tất*.

### 9. Giao Diện Chuẩn Haute Parfumerie (Dark & Light Theme)
* Bảng màu chủ đạo: **Champagne Gold (`#D4AF37`)** phối nền **Velvet Black / Pure White**.
* Nút chuyển đổi giao diện Dark/Light mượt mà ở thanh điều hướng, tự động lưu tuỳ chọn vào LocalStorage.
* Typography cao cấp chuẩn phong cách tạp chí thời trang xa xỉ (Playfair Display, Inter).

### 10. Chuẩn Hóa Kỹ Thuật & SEO Google Tối Ưu
* Tự động sinh `sitemap.xml`, `robots.txt`, `llms.txt` cho toàn bộ danh mục và sản phẩm.
* Tích hợp dữ liệu cấu trúc chuẩn Google Rich Results: `Schema.org/Product` và `Schema.org/BlogPosting`.
* Tối ưu LCP (Largest Contentful Paint) với lazy-loading ảnh và hệ thống cache phân tán.

---

## ⚡ 5. Tối Ưu Hóa Hiệu Năng & Cơ Sở Dữ Liệu

- **Động cơ Schema Migration V1 - V6**: Tự động nâng cấp cấu trúc bảng qua `DatabaseSchemaUpgrader`, áp dụng hệ thống chỉ mục B-Tree Index chuyên biệt (`IX_Products_CreatedAt`, `IX_Products_BasePrice`, `IX_Orders_UserId_CreatedAt`, `IX_Orders_OrderStatus_CreatedAt`...).
- **Triệt tiêu N+1 Query**: Toàn bộ giỏ hàng và danh mục được nạp batch dạng Dictionary trong 1 truy vấn duy nhất.
- **In-Memory Caching (`IMemoryCache`)**: Cache trong RAM các danh mục phân loại tĩnh (`Brands`, `Categories`, `FragranceFamilies`, `FragranceNotes`) với sliding expiration 10 phút.
- **Nén dữ liệu truyền tải**: Kích hoạt Brotli và Gzip nén HTTP Response ở tầng middleware.

---

## 🛍️ 6. Vai Trò & Phân Hệ Vận Hành Cửa Hàng

- **`Admin`**: Toàn quyền quản trị hệ thống, vai trò RBAC, bảng điều khiển CSDL AWS-style, kiểm toán bảo mật và báo cáo tài chính P&L.
- **`StoreManager`**: Quản lý danh mục sản phẩm, biến thể dung tích, kho hàng, tạo chiến dịch Flash Sale, duyệt bài đánh giá và quản lý Voucher.
- **`WarehouseStaff` / `Concierge`**: Cập nhật trạng thái xử lý đơn hàng, theo dõi mã vận đơn và tư vấn mùi hương.
- **`Customer`**: Chỉnh sửa hồ sơ, xem lịch sử đơn hàng, tích điểm VIP Loyalty và đánh giá sản phẩm.
- **Khách vãng lai (Guest)**: Xem catalog, dùng giỏ hàng cookie, đặt COD/VietQR và nhận liên kết theo dõi an toàn qua email (bảo vệ bởi `IDataProtector`).
- **Hệ thống Email**: Tích hợp gửi email xác nhận đơn hàng tự động vào Mailpit khi test local; lỗi SMTP không làm gián đoạn đơn hàng.

---

## 🧪 7. Bộ Kiểm Thử Tự Động (Quality Assurance)

Dự án sở hữu bộ kiểm thử tự động toàn diện trong `tests/Perfume.Tests/`:
```bash
dotnet test
```
* **Kết quả kiểm thử**: **`Passed! - Failed: 0, Passed: 411, Skipped: 0, Total: 411`** (100% Passed).
* **Phạm vi bao phủ**:
  - Giao dịch đặt hàng an toàn mức Serializable.
  - Giới hạn biên sự kiện Flash Sale & đếm ngược thời gian thực.
  - Công thức thăng hạng và chiết khấu VIP Loyalty (Bronze → Silver → Gold → Diamond).
  - Thuật toán trắc nghiệm mùi hương AI Scent Quiz & gợi ý sản phẩm.
  - Kiểm tra tính hợp lệ cấu hình hộp quà Discovery Box và in thiệp.
  - Báo cáo tài chính P&L, tính toán doanh thu thuần, COGS và biên lợi nhuận gộp.
  - Kiểm toán CSDL và chặn các câu lệnh SQL nguy hiểm trong Database Console.
