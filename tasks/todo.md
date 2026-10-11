# TODO: Website Content & Data Completion

- [x] Giai Đoạn 1: Mở rộng Dữ liệu SQL Seed (`scripts/seed_perfume_data.sql` & `scripts/seed_extended_data.sql`)
  - [x] Bổ sung 9 sản phẩm kinh điển mới (lên 15+ chai) kèm Variants và nốt hương kim tự tháp (Le Labo, YSL, Chanel No.5, Tom Ford Lost Cherry, Dior J'adore...).
  - [x] Bổ sung danh mục blog mới và 11 bài viết chuyên sâu mới (tổng 12 bài viết chuẩn Haute Parfumerie).
  - [x] Nạp 18 đánh giá 5 sao mẫu kèm hình ảnh và độ lưu hương thực tế.
  - [x] Nạp 8-10 đơn hàng mẫu kích hoạt biểu đồ thống kê Admin.
  - [x] Thực thi file SQL vào SQL Server qua Docker.

- [x] Giai Đoạn 2: Backend Services & Controllers
  - [x] Nâng cấp `IBlogService.cs` và `BlogService.cs` (hỗ trợ phân trang, lọc category, CRUD).
  - [x] Cập nhật `BlogController.cs` (hỗ trợ lọc theo tab danh mục).
  - [x] Cập nhật `HomeController.cs` (thêm actions cho About, Boutique, ReturnPolicy, BuyingGuide, SubscribeNewsletter).

- [x] Giai Đoạn 3: Giao Diện Storefront
  - [x] Tái thiết kế `Views/Blog/Index.cshtml` thành Tạp chí Haute Parfumerie (Hero article + Category tabs).
  - [x] Tạo mới `Views/Home/About.cshtml` (Câu chuyện thương hiệu).
  - [x] Tạo mới `Views/Home/Boutique.cshtml` (Showroom & Form hẹn lịch VIP Concierge).
  - [x] Tạo mới `Views/Home/ReturnPolicy.cshtml` (Chính sách đổi trả 7 ngày).
  - [x] Tạo mới `Views/Home/BuyingGuide.cshtml` (Hướng dẫn mua hàng & COD).
  - [x] Hoàn thiện `Views/Home/Privacy.cshtml` (Chính sách bảo mật dữ liệu).
  - [x] Nâng cấp `_Layout.cshtml` (Header/Footer links & AJAX Newsletter toast).

- [x] Giai Đoạn 4: Phân Hệ Quản Trị Admin Blog
  - [x] Cập nhật `AdminDashboardController.cs` (Actions: Blogs, CreateBlog, EditBlog, DeleteBlog).
  - [x] Tạo mới `Areas/Admin/Views/AdminDashboard/Blogs.cshtml` (Danh sách bài viết).
  - [x] Tạo mới `Areas/Admin/Views/AdminDashboard/CreateBlog.cshtml` (Form soạn thảo bài viết).

- [x] Giai Đoạn 5: Kiểm Thử Toàn Diện & Hoàn Thiện
  - [x] Build dự án (`dotnet build`) xác nhận 0 Warning, 0 Error.
  - [x] Kiểm tra toàn bộ trang trên Google Chrome bằng Chrome DevTools MCP.
