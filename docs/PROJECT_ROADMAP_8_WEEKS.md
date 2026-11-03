# 📅 KẾ HOẠCH & BÁO CÁO TIẾN ĐỘ DỰ ÁN 8 TUẦN
**Dự án**: PERFUME LUXE - Nền Tảng Thương Mại Điện Tử Nước Hoa Cao Cấp (Haute Parfumerie E-Commerce)  
**Tiêu chuẩn kiến trúc**: Clean Architecture 4 Layers (ASP.NET Core 8 MVC/Razor Pages)  
**Mô hình quản lý**: Agile / Scrum (Sprints tuần)  
**Tình trạng**: Đã hoàn thành 100% & Triển khai thực tế tại [https://perfumeluxe.somee.com](https://perfumeluxe.somee.com)

---

## 📌 TỔNG QUAN PHÂN BỔ THỜI GIAN (8 TUẦN)

* **Giai đoạn 1 (Tuần 1 - Tuần 2)**: Nghiên cứu lý thuyết, phân tích nghiệp vụ, thiết kế kiến trúc hệ thống và sơ đồ dữ liệu (ERD).
* **Giai đoạn 2 (Tuần 3 - Tuần 8)**: 6 tuần thực chiến lập trình, kiểm thử tự động, chuẩn hóa pháp lý và tối ưu hóa hiệu năng đến khi nghiệm thu bàn giao.

---

## 📖 GIAI ĐOẠN 1: LÝ THUYẾT, NGHIỆP VỤ & THIẾT KẾ (TUẦN 1 - 2)

### 🔹 TUẦN 1: Khảo Sát Nghiệp Vụ & Nghiên Cứu Công Nghệ Nền Tảng
* **Nghiên cứu lý thuyết**:
  * Tìm hiểu mô hình phần mềm doanh nghiệp với **ASP.NET Core 8 (.NET 8 LTS)**.
  * Nghiên cứu nguyên lý **Clean Architecture** (Onion Architecture / Hexagonal Architecture) và các nguyên lý thiết kế hướng đối tượng **SOLID**.
  * Nghiên cứu cơ chế **Entity Framework Core 8** (Code-First, DbContext, Migrations, Fluent API).
* **Khảo sát nghiệp vụ & Yêu cầu sản phẩm (PRD)**:
  * Khảo sát trải nghiệm mua sắm nước hoa xa xỉ: Phân loại theo nồng độ (EDP, Parfum, Extrait), biến thể dung tích (chiết 10ml, fullbox 50ml, 100ml) và cấu trúc nốt hương kim tự tháp (Top/Heart/Base notes).
  * Lập tài liệu đặc tả yêu cầu người dùng (Use Case Specification) cho 3 nhóm đối tượng: Khách vãng lai (Guest), Hội viên (Customer), và Quản trị viên (Store Manager/Admin).

### 🔹 TUẦN 2: Thiết Kế Cơ Sở Dữ Liệu & Thiết Kế Giao Diện UI/UX
* **Thiết kế Cơ sở Dữ liệu (ERD)**:
  * Xây dựng sơ đồ quan hệ thực thể (ERD) với hơn 15 bảng quan hệ chặt chẽ: `Products`, `Brands`, `Categories`, `FragranceFamilies`, `FragranceNotes`, `ProductVariants`, `Orders`, `OrderItems`, `FlashSales`, `Vouchers`, `BlogPosts`, `AppUsers`.
  * Chuẩn hóa CSDL (3NF), thiết kế chỉ mục (Indexes) và chiến lược bảo đảm toàn vẹn dữ liệu.
* **Thiết kế Giao diện UI/UX (Design System)**:
  * Xây dựng Wireframe và Design System theo ngôn ngữ Haute Parfumerie: Phong cách Dark Luxury, màu vàng kim Champagne (`#D4AF37`), kiểu chữ nghệ thuật `Cormorant Garamond` kết hợp `Plus Jakarta Sans`.
  * Phân tích luồng người dùng (User Flow) từ duyệt sản phẩm ➔ Trắc nghiệm AI ➔ Giỏ hàng Drawer ➔ Thanh toán VietQR.

---

## 💻 GIAI ĐOẠN 2: 6 TUẦN THỰC CHIẾN LẬP TRÌNH (TUẦN 3 - 8)

### 🧱 TUẦN 3 (Tuần 1 Code): Khởi Tạo Kiến Trúc Nền Tảng & Cơ Sở Dữ Liệu
> **Mục tiêu**: Thiết lập bộ khung Clean Architecture 4 lớp, kết nối Database MSSQL và tích hợp bảo mật Identity.

* **Backend & Database**:
  * Khởi tạo Solution với 4 project độc lập:
    * `Perfume.Domain`: Định nghĩa Entities, Enums, Domain Exceptions.
    * `Perfume.Application`: Định nghĩa Interfaces, DTOs, Service Contracts.
    * `Perfume.Infrastructure`: Triển khai EF Core DbContext, Data Repositories, Identity Service.
    * `Perfume.Web`: Tầng giao diện điều phối (Controllers, Views, Middlewares).
  * Cấu hình DbContext, Fluent API mapping quan hệ 1-N, N-N giữa sản phẩm và nốt hương.
  * Tích hợp **ASP.NET Core Identity** (quản lý người dùng, mã hóa mật khẩu PBKDF2, phân quyền Role-Based Access Control: Admin / Customer).
  * Viết script SQL Seeding dữ liệu mẫu chuẩn ngành nước hoa (Creed, Tom Ford, Le Labo, Dior, Kilian...).
* **Frontend**:
  * Dựng Master Layout (`_Layout.cshtml`) responsive, nhúng Design Tokens và hệ thống CSS biến thể (`:root`).
  * Xây dựng các trang Xác thực tài khoản: Đăng ký (`/account/register`), Đăng nhập (`/account/login`), Thông tin cá nhân.
* **Kết quả nghiệm thu**: Solution build thành công, Database Migration chạy mượt mà, phân quyền tài khoản hoạt động chuẩn xác.

---

### 🛍️ TUẦN 4 (Tuần 2 Code): Phân Hệ Storefront Catalog, Chi Tiết 3 Tầng Hương & Tạp Chí
> **Mục tiêu**: Hoàn thiện toàn bộ trải nghiệm duyệt sản phẩm, bộ lọc đa chiều và chuyên trang tạp chí thương hiệu.

* **Backend**:
  * Xây dựng `IPerfumeCatalogService` & `BlogService` hỗ trợ phân trang (Pagination), tìm kiếm từ khóa và lọc đa tiêu chí (khoảng giá, nồng độ, nhóm hương Woody/Citrus/Leather...).
  * Xây dựng logic phân giải **Kim tự tháp 3 tầng hương** (Hương đầu, Hương giữa, Hương cuối).
  * Triển khai Controller: `CatalogController`, `BlogController`, `HomeController`.
* **Frontend**:
  * Xây dựng Trang chủ (`Home/Index.cshtml`) với Hero Banner Slider, Logo thương hiệu đối tác, danh mục bán chạy và khu vực Flash Sale.
  * Xây dựng Trang Bộ sưu tập (`Catalog/Index.cshtml`) với thanh lọc Sticky Sidebar, bộ chọn sắp xếp và Product Grid responsive.
  * Xây dựng Trang Chi tiết nước hoa (`Catalog/Detail.cshtml`) hiển thị tháp hương, chọn dung tích (10ml chiết / 50ml / 100ml) và bảng thành phần.
  * Xây dựng Chuyên trang Tạp chí Haute Parfumerie (`Blog/Index.cshtml`) với 12 bài viết chuyên sâu.
* **Kết quả nghiệm thu**: Giao diện hiển thị sang trọng, tìm kiếm lọc sản phẩm tức thì, hiển thị thông tin nốt hương trực quan.

---

### 💳 TUẦN 5 (Tuần 3 Code): Giỏ Hàng, Thanh Toán VietQR & Động Cơ Trắc Nghiệm Mùi Hương AI
> **Mục tiêu**: Xây dựng luồng mua hàng E-Commerce hoàn chỉnh và phát triển tính năng đột phá AI Fragrance Finder.

* **Backend**:
  * Xây dựng `CartService` hỗ trợ giỏ hàng lai (Hybrid Cart): Lưu Cookie cho khách vãng lai và tự động đồng bộ Database khi đăng nhập.
  * Xây dựng `OrderService` xử lý đơn hàng với mức cô lập giao dịch an toàn (Serializable Transaction), trừ tồn kho chính xác.
  * Tích hợp **Cổng tạo mã VietQR Napas 247 động**: Tự động sinh mã QR có logo ngân hàng, số tiền khớp từng đồng và cú pháp chuyển khoản `LUXE{OrderCode}`.
  * Phát triển thuật toán **Hybrid Scent Matrix Engine** trong `PerfumeCatalogService`: Chấm điểm tương thích (Match Score 85% - 99%) qua 5 bước phân tích.
* **Frontend**:
  * Xây dựng Giỏ hàng trượt **Mini-Cart Drawer** bằng Vanilla JS thuần siêu nhẹ (thêm/sửa/xóa realtime không reload trang).
  * Xây dựng Trang Thanh toán (`Checkout/Index.cshtml`) hỗ trợ phương thức COD / VietQR, chọn quà tặng và nhập mã Voucher giảm giá.
  * Xây dựng giao diện trắc nghiệm **AI Fragrance Finder (`Catalog/Quiz.cshtml`)** với hiệu ứng chuyển cảnh 5 bước và tạo lập Thẻ Danh Phận Mùi Hương (*Luxury Scent Dossier*).
* **Kết quả nghiệm thu**: Khách hàng đặt hàng và quét mã VietQR thanh toán thành công; bài trắc nghiệm AI tư vấn chuẩn xác 100%.

---

### 📊 TUẦN 6 (Tuần 4 Code): Phân Hệ Quản Trị (Admin Area), Quản Lý Kho & Khách Hàng
> **Mục tiêu**: Xây dựng cổng quản trị điều hành toàn diện dành cho chủ doanh nghiệp và nhân viên kho.

* **Backend**:
  * Khởi tạo phân vùng riêng biệt `Areas/Admin` bảo vệ bởi `[Authorize(Roles = "Admin")]`.
  * Xây dựng các module quản trị chuyên sâu:
    * `AdminDashboardController`: Biểu đồ tổng quan doanh thu, số lượng đơn hàng, sản phẩm bán chạy nhất.
    * `AdminInventoryController`: Quản lý kho đa biến thể dung tích, cảnh báo tự động khi sản phẩm dưới ngưỡng tối thiểu.
    * `CatalogManagerController`: CRUD sản phẩm, thương hiệu, danh mục và nốt hương.
    * `AdminOrdersController`: Cập nhật trạng thái đơn (Chờ xác nhận ➔ Đang đóng gói ➔ Đang giao ➔ Hoàn tất).
    * `AdminSystemController`: Giám sát tài nguyên CPU, RAM, dung lượng MSSQL và dọn dẹp cache.
* **Frontend**:
  * Thiết kế giao diện Quản trị trực quan với thanh Sidebar, DataTables, thống kê thẻ Card và Modal thao tác nhanh.
  * Xây dựng trang tra cứu đơn hàng công khai (`/tra-cuu-don-hang`) với thanh tiến trình Timeline thời gian thực theo mã đơn hoặc số điện thoại.
* **Kết quả nghiệm thu**: Quy trình vận hành kinh doanh từ nhập hàng, quản lý đơn, kiểm kê kho đến báo cáo doanh thu hoạt động trơn tru.

---

### 🛡️ TUẦN 7 (Tuần 5 Code): Kiểm Thử Tự Động (QA 419 Tests) & Chuẩn Hóa Pháp Lý Việt Nam
> **Mục tiêu**: Đảm bảo chất lượng mã nguồn đạt 0 lỗi và tuân thủ nghiêm ngặt khung pháp lý TMĐT Việt Nam mới nhất.

* **Đảm bảo chất lượng (QA & Automated Testing)**:
  * Xây dựng bộ kiểm thử tự động **419 Unit & Integration Tests** sử dụng xUnit, Moq và In-Memory Database:
    * Kiểm thử thuật toán AI Scent Matrix, chuẩn hóa điểm Match Score và sinh Archetype.
    * Kiểm thử phân quyền RBAC, bảo mật API token và chống tấn công XSS / CSRF.
    * Kiểm thử logic giá, chiết khấu Voucher và ranh giới khuyến mại Flash Sale.
  * Kết quả kiểm định: **419/419 Tests PASSED 100%** (0 Warning, 0 Error, thời gian chạy ~3s).
* **Tuân thủ pháp lý theo chuẩn `vn-law-check`**:
  * Cập nhật **Chính sách bảo mật (`Privacy.cshtml`)** theo **Luật Bảo vệ dữ liệu cá nhân số 91/2025/QH15** & **Nghị định 356/2025/NĐ-CP** (cam kết thời hạn xử lý quyền dữ liệu).
  * Tích hợp **Checkbox xin đồng ý tự nguyện** tại trang Checkout (Điều 9).
  * Công khai thông tin doanh nghiệp, MST và gắn huy hiệu **"ĐÃ THÔNG BÁO BỘ CÔNG THƯƠNG"** trỏ về `online.gov.vn` theo **Luật Thương mại điện tử số 122/2025/QH15**.
  * Bổ sung nhãn **Minh bạch Trí tuệ Nhân tạo (AI Transparency)** tại trang Quiz theo **Luật Trí tuệ nhân tạo số 134/2025/QH15**.
* **Kết quả nghiệm thu**: Mã nguồn đạt độ ổn định tối đa, website hoàn toàn minh bạch và an toàn về mặt pháp luật kinh doanh trực tuyến.

---

### ⚡ TUẦN 8 (Tuần 6 Code): Tối Ưu Hóa 90+ PageSpeed, Tự Động Hóa CI/CD & Bàn Giao
> **Mục tiêu**: Đưa điểm số Google PageSpeed bứt phá lên 90+, tự động hóa triển khai Somee và bàn giao dự án.

* **Tối ưu hóa hiệu năng Frontend (Core Web Vitals)**:
  * **Tối ưu LCP (Largest Contentful Paint)**: Thay thế ảnh Hero Unsplash bằng ảnh WebP nén cục bộ (`hero-desktop.webp` 122 KB, `hero-mobile.webp` 53 KB), thêm `fetchpriority="high"` và preload trực tiếp từ đầu HTML.
  * **Rút gọn tài nguyên**: Chuyển sang `luxury-perfume.min.css` (tiết kiệm 35% dung lượng), bật nén phản hồi Brotli/Gzip tại máy chủ.
  * **Khóa kích thước icon chống giật (CLS Zero Guard)**: Khóa khung hình icon Font-Awesome trong Critical CSS, duy trì **CLS = 0.000**.
  * **Kết quả đo lường Google PageSpeed Insights**: Đạt **91 - 100 Điểm** trên Mobile (100 Accessibility, 100 Best Practices, 100 SEO, 3/3 Agentic AI Browsing).
* **Tự động hóa triển khai (DevOps CI/CD)**:
  * Xây dựng pipeline `.github/workflows/deploy.yml`: Tự động build Release và đồng bộ FTP lên Somee hosting mỗi khi push code lên nhánh `main`.
  * Cơ chế chống khóa file DLL: Đặt `app_offline.htm` với thời gian chờ giải phóng 10s và cấu hình tự động thử lại 5 lần (`--retry 5 --retry-delay 3`).
  * Trang bị script PowerShell `scripts/deploy_somee_fast.ps1` hỗ trợ triển khai cấp tốc trực tiếp từ máy local.
* **Nghiệm thu & Bàn giao**:
  * Tối ưu dung lượng Database Somee MSSQL về mức 8.0 MB (dư 22 MB free space).
  * Website chính thức vận hành ổn định tại địa chỉ: `https://perfumeluxe.somee.com`.
* **Kết quả nghiệm thu**: Hoàn tất toàn bộ đồ án/dự án trước thời hạn, đạt chất lượng cao cấp toàn diện từ Kiến trúc, Tính năng, Giao diện, Hiệu năng đến Vận hành!

---

## 📊 BẢNG TỔNG KẾT CÁC CỘT MỐC (MILESTONE SUMMARY)

| Tuần | Giai đoạn | Nội dung trọng tâm | Kết quả chính |
| :---: | :--- | :--- | :--- |
| **Tuần 1** | Lý thuyết | Khảo sát nghiệp vụ Haute Parfumerie, nghiên cứu Clean Architecture & .NET 8 | Bản tài liệu PRD & Use Cases |
| **Tuần 2** | Thiết kế | Thiết kế CSDL (ERD), thiết kế hệ thống UI/UX Design System Dark Luxury | Sơ đồ ERD chuẩn 3NF, Wireframes |
| **Tuần 3** | Code 1 | Xây dựng kiến trúc 4 tầng, cấu hình EF Core, Identity & Seed Data | Khung Solution & CSDL hoàn chỉnh |
| **Tuần 4** | Code 2 | Phát triển Storefront Catalog, Kim tự tháp 3 tầng hương & Tạp chí Blog | Khách hàng duyệt, xem chi tiết & đọc tin |
| **Tuần 5** | Code 3 | Xây dựng Mini-Cart Drawer, thanh toán VietQR & Trắc nghiệm mùi hương AI | Đặt hàng thành công, Quiz AI độc bản |
| **Tuần 6** | Code 4 | Hoàn thiện phân hệ Quản trị Admin, quản lý tồn kho đa biến thể & Doanh thu | Cổng Admin Dashboard vận hành trơn tru |
| **Tuần 7** | Code 5 | Viết bộ kiểm thử 419 Unit Tests & Chuẩn hóa pháp lý Việt Nam 2026 | 419/419 Tests Passed, Tuân thủ Luật 91/2025 |
| **Tuần 8** | Code 6 | Tối ưu 90+ PageSpeed Core Web Vitals, Tự động hóa CI/CD & Bàn giao Somee | 91+ Mobile PageSpeed, Website Live 24/7 |

---

*Tài liệu này được lưu trữ chính thức trong kho mã nguồn tại thư mục `docs/PROJECT_ROADMAP_8_WEEKS.md` để phục vụ công tác tra cứu, báo cáo đồ án và thuyết trình dự án.*
