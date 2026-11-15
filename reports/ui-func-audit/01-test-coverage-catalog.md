# Chuyên Đề 01: Danh Mục Tất Cả Chức Năng & Màn Hình Đã Kiểm Thử
**Phạm vi thẩm định:** Hệ thống Perfume Luxe E-Commerce trên máy chủ Live [https://perfumeluxe.somee.com/](https://perfumeluxe.somee.com/)  
**Phương thức kiểm thử:** Black-box User Interaction, HTTP Contract Analysis & DOM Visual Inspection  

---

## 1. Phân Hệ 1: Storefront Khách Hàng (Public Pages & Discovery)

| STT | Màn hình / Tính năng | URL / Endpoint kiểm thử | Phương thức | Kịch bản kiểm thử | Trạng thái | Ghi chú & Đánh giá |
| :---: | :--- | :--- | :---: | :--- | :---: | :--- |
| **1.1** | **Trang Chủ (Homepage)** | `/` | GET | Tải trang chủ, hiển thị Hero Banner, kiểm tra tải font/CSS/ảnh, tải khối Best Sellers, New Arrivals, Nhóm hương, Blog gần đây. | <span style="color:#22c55e">PASS</span> | Tải mượt mà, Dark Theme đồng nhất, không lỗi console font/CSS. |
| **1.2** | **Hero LCP Preload** | `/` | GET | Kiểm tra tối ưu tải ảnh LCP Hero Slider định dạng WebP với responsive `imagesrcset`. | <span style="color:#22c55e">PASS</span> | Tối ưu Core Web Vitals chuẩn, preload hoạt động tốt. |
| **1.3** | **Đăng ký Newsletter Club** | `/api/newsletter/subscribe` | POST | Nhập email hợp lệ (`khachhang@gmail.com`) nhận mã ưu đãi; kiểm tra email sai định dạng. | <span style="color:#22c55e">PASS</span> | Trả về JSON chứa mã `LUXE2026`, chặn email rác. |
| **1.4** | **Danh Mục Bộ Sưu Tập** | `/bo-suu-tap` | GET | Hiển thị danh sách 12 chai nước hoa, hiển thị badge Best Seller / New, phân trang sản phẩm. | <span style="color:#22c55e">PASS</span> | Bố cục lưới sang trọng, card ảnh hiển thị sắc nét. |
| **1.5** | **Tìm kiếm từ khóa** | `/bo-suu-tap?keyword=aventus` | GET | Nhập từ khóa sản phẩm cụ thể ("aventus", "chanel") kiểm tra độ chính xác kết quả trả về. | <span style="color:#22c55e">PASS</span> | Khớp chính xác tên và mô tả sản phẩm. |
| **1.6** | **Trạng thái Tìm kiếm Rỗng** | `/bo-suu-tap?keyword=xyz999` | GET | Tìm kiếm từ khóa không tồn tại, kiểm tra Empty State và nút Reset bộ lọc. | <span style="color:#22c55e">PASS</span> | Hiển thị thông báo "Không Tìm Thấy Sản Phẩm", schema cập nhật `numberOfItems: 0`. |
| **1.7** | **Bộ lọc Thương hiệu** | `/bo-suu-tap?brandSlug=creed` | GET | Lọc danh sách theo thương hiệu cao cấp (Creed, Chanel, Dior, Le Labo...). | <span style="color:#22c55e">PASS</span> | Lọc chính xác 2 sản phẩm của thương hiệu Creed. |
| **1.8** | **Bộ lọc Nhóm hương** | `/bo-suu-tap?familySlug=huong-go-woody` | GET | Lọc sản phẩm theo gia đình mùi hương (Hương Gỗ, Hoa Cỏ, Phương Đông, Cam Chanh). | <span style="color:#22c55e">PASS</span> | Trả về chuẩn danh sách thuộc nhóm hương gỗ. |
| **1.9** | **Sắp xếp theo Giá** | `/bo-suu-tap?sortBy=price_asc` | GET | Sắp xếp giá tăng dần (`price_asc`) và giảm dần (`price_desc`). | <span style="color:#22c55e">PASS</span> | Giá tăng dần: 3.3M -> 6.8M; giá giảm dần: 8.9M -> 3.65M chuẩn xác. |
| **1.10** | **Chi Tiết Sản Phẩm** | `/nuoc-hoa/chanel-coco-mademoiselle` | GET | Xem tháp hương 3 tầng (Đầu, Giữa, Cuối), dung tích, giá tiền, ảnh sản phẩm, mô tả nghệ thuật. | <span style="color:#22c55e">PASS</span> | Trực quan, phong cách Haute Parfumerie rất đẹp mắt. |
| **1.11** | **Chuyển Đổi Phiên Bản Variant** | `/nuoc-hoa/{slug}` | DOM Event | Click chọn nút dung tích (10ml Chiết / 50ml / 100ml Fullbox), cập nhật giá tức thời. | <span style="color:#22c55e">PASS</span> | JS cập nhật giá thời gian thực, truyền đúng variant ID vào nút thêm giỏ. |
| **1.12** | **Xử lý Trang 404 Sản phẩm** | `/nuoc-hoa/san-pham-khong-ton-tai` | GET | Truy cập slug nước hoa không có trong hệ thống. | <span style="color:#22c55e">PASS</span> | Trả về mã HTTP 404 Not Found an toàn, không rò rỉ stack trace. |
| **1.13** | **Đăng ký Nhận Tin Có Hàng** | `/api/v1/back-in-stock/subscribe` | POST | Gửi yêu cầu đăng ký nhận thông báo khi sản phẩm hết hàng về lại kho. | <span style="color:#22c55e">PASS</span> | Nhận email thành công, validate dữ liệu đầu vào tốt. |
| **1.14** | **AI Scent Quiz Trắc Nghiệm** | `/scent-quiz` | GET & POST | Trải nghiệm bài trắc nghiệm 5 bước tìm phong cách mùi hương cá nhân. | <span style="color:#22c55e">PASS</span> | Tính toán Archetype, match score (%) và gợi ý 4 chai nước hoa phù hợp. |
| **1.15** | **So Sánh Mùi Hương** | `/so-sanh?p1=1&p2=2` | GET | Đặt 2 chai nước hoa cạnh nhau để so sánh nốt hương, độ tỏa hương và độ lưu hương. | <span style="color:#22c55e">PASS</span> | Bảng so sánh trực quan, hiển thị nốt hương tương quan rõ nét. |
| **1.16** | **Hộp Khám Phá Discovery Box** | `/discovery-box` | GET | Quy trình 3 bước tùy biến hộp quà: Chọn vỏ hộp, chọn 3-5 ống chiết, in thiệp khắc tên. | <span style="color:#f59e0b">WARNING</span> | Chức năng hoạt động tốt nhưng phát hiện lỗi lồng thẻ `<main>` (DEF-07). |
| **1.17** | **Đặt Lịch Thử Mùi VIP Tasting** | `/dat-lich-trai-nghiem` | GET & POST | Đăng ký buổi thử hương riêng tư tại Boutique với chuyên gia thẩm hương. | <span style="color:#f59e0b">WARNING</span> | Đặt lịch thành công cấp mã VIP-00001, nhưng view con lồng thẻ `<main>` (DEF-07). |
| **1.18** | **Tạp Chí Nước Hoa (Blog Index)**| `/blog` | GET | Danh sách 9 bài viết chuyên sâu về kiến thức nước hoa Niche, lọc theo chuyên mục pill. | <span style="color:#22c55e">PASS</span> | Bố cục tạp chí thời trang, phân trang mượt mà. |
| **1.19** | **Chi Tiết Bài Viết (Blog Detail)**| `/blog/xu-huong-nuoc-hoa-niche-2026` | GET | Đọc bài viết chi tiết, danh sách bài liên quan, OpenGraph thẻ bài viết. | <span style="color:#f59e0b">WARNING</span> | Nội dung hiển thị đẹp, nhưng cột chính khai báo lồng thẻ `<main>` (DEF-07). |
| **1.20** | **Chính Sách & Hướng Dẫn Mua Hàng** | `/ve-chung-toi`, `/boutique`, `/chinh-sach-doi-tra`, `/huong-dan-mua-hang`, `/privacy` | GET | Truy cập các trang nội dung pháp lý và giới thiệu thương hiệu. | <span style="color:#22c55e">PASS</span> | 100% trả về HTTP 200 OK, văn phong chuẩn chỉnh. |
| **1.21** | **Chuẩn Hóa SEO & AI Search** | `/robots.txt`, `/sitemap.xml`, `/llms.txt` | GET | Kiểm tra chỉ thị bot tìm kiếm, XML sitemap 54 URL và chuẩn bị tài liệu cho AI agents. | <span style="color:#22c55e">PASS</span> | Hoàn hảo, canonical tag đầy đủ, sitemap cấu trúc chuẩn. |

---

## 2. Phân Hệ 2: Luồng Thương Mại (Commerce & Checkout Flows)

| STT | Màn hình / Tính năng | URL / Endpoint kiểm thử | Phương thức | Kịch bản kiểm thử | Trạng thái | Ghi chú & Đánh giá |
| :---: | :--- | :--- | :---: | :--- | :---: | :--- |
| **2.1** | **Giỏ Hàng Rỗng (Empty Cart)** | `/cart` | GET | Truy cập giỏ hàng khi chưa chọn sản phẩm nào. | <span style="color:#22c55e">PASS</span> | Hiển thị giao diện giỏ rỗng, gợi ý các mùi hương bán chạy. |
| **2.2** | **Thêm Hàng Vào Giỏ (Add to Cart)** | `/api/v1/cart/items` | POST | Thêm chai nước hoa vào giỏ với dung tích xác định qua cookie session khách vãng lai. | <span style="color:#22c55e">PASS</span> | Giỏ hàng cập nhật số lượng và tính tiền tức thời. |
| **2.3** | **Cập Nhật Số Lượng & Xóa Sản Phẩm** | `/api/v1/cart/items/{id}` | PUT/DELETE | Tăng/giảm số lượng chai nước hoa trong giỏ hoặc loại bỏ khỏi danh sách. | <span style="color:#22c55e">PASS</span> | Tính lại phí ship (miễn phí khi đơn >= 2.000.000₫) và tổng tiền chính xác. |
| **2.4** | **Redirect Giỏ Rỗng Khi Thanh Toán** | `/checkout` | GET | Mở trực tiếp trang checkout khi chưa có hàng trong giỏ. | <span style="color:#22c55e">PASS</span> | Hệ thống phản hồi HTTP 302 Found chuyển hướng về `/Cart/Index`. |
| **2.5** | **Giao Diện Thanh Toán (Checkout)** | `/checkout` (khi giỏ có hàng) | GET | Render biểu mẫu nhập thông tin người nhận, địa chỉ, chọn gói quà hoàng gia, chọn COD/VietQR. | <span style="color:#22c55e">PASS</span> | Form hiển thị chuẩn, tính toán tổng tiền động khi tích chọn dịch vụ quà tặng. |
| **2.6** | **Gói Quà Hoàng Gia (+50.000₫)** | `/checkout` | DOM Event | Tích chọn hộp quà nhung đen, mở hộp nhập lời chúc viết tay, cộng 50.000₫ vào tổng thanh toán. | <span style="color:#22c55e">PASS</span> | Hiệu ứng mở rộng mượt mà, lời chúc gửi kèm đơn hàng. |
| **2.7** | **Đồng Ý Chính Sách Dữ Liệu Cá Nhân**| `/checkout` | Form Validation | Kiểm tra checkbox bắt buộc theo Luật 91/2025/QH15 trước khi gửi đơn. | <span style="color:#22c55e">PASS</span> | Bắt buộc tick chọn; chặn submit nếu người dùng bỏ sót. |
| **2.8** | **Thuộc tính An toàn Link Điều khoản** | `/checkout` | DOM Inspection | Kiểm tra thẻ link mở tab mới tới `/huong-dan-mua-hang` và `/privacy`. | <span style="color:#f59e0b">WARNING</span> | Thiếu thuộc tính `rel="noopener noreferrer"` (DEF-10). |
| **2.9** | **Xác Thực Đặt Hàng Trực Tiếp (Order Placement)** | `/checkout` & `/api/v1/orders` | POST | Nhấn nút "Xác Nhận Đặt Hàng Ngay" với phương thức COD và VietQR. | <span style="color:#ef4444;font-weight:bold">FAIL (P0)</span> | **LỖI CHẶN TOÀN BỘ ĐƠN HÀNG:** Client gửi `paymentMethod: 0` gây lỗi "Phương thức thanh toán chưa được hỗ trợ." (DEF-01). |
| **2.10** | **Áp Dụng Mã Giảm Giá Gợi Ý** | `/api/v1/vouchers/validate` | POST | Nhập các mã ưu đãi được gợi ý trên label: `LUXE2026` hoặc `WELCOME`. | <span style="color:#ef4444;font-weight:bold">FAIL (P2)</span> | Hệ thống báo "Mã ưu đãi không hợp lệ hoặc chưa đủ điều kiện áp dụng." do database chưa seed mã này (DEF-04). |
| **2.11** | **Tra Cứu Đơn Hàng Thành Công** | `/tra-cuu-don-hang?orderCode=...&phone=...` | GET | Nhập đúng Mã đơn hàng và Số điện thoại đặt hàng. | <span style="color:#22c55e">PASS</span> | Hiển thị lộ trình trạng thái: Chờ xác nhận -> Đang đóng gói -> Đang giao -> Hoàn tất. |
| **2.12** | **Tra Cứu Thiếu Số Điện Thoại** | `/tra-cuu-don-hang?orderCode=...&phone=` | GET | Chỉ nhập Mã đơn hàng nhưng để trống ô Số điện thoại. | <span style="color:#f59e0b">WARNING</span> | Tiêu đề báo "Không Tìm Thấy Thông Tin Đơn Hàng" gây hiểu nhầm đơn bị hủy (DEF-06). |
| **2.13** | **Kiểm Tra Trạng Thái Flash Sale** | `/api/v1/flashsales/active` | GET | Kiểm tra API sự kiện giảm giá chớp nhoáng trên trang chủ. | <span style="color:#f59e0b">WARNING</span> | Trả về 404 khi không có sự kiện, khối banner trang chủ bị trống (DEF-08). |
| **2.14** | **Đánh Giá Sản Phẩm (Reviews)** | `/api/v1/products/{id}/reviews` | GET & POST | Xem danh sách đánh giá sao và gửi bình luận đánh giá kèm hình ảnh. | <span style="color:#22c55e">PASS</span> | Yêu cầu đăng nhập tài khoản Customer hợp lệ, lọc xss an toàn. |

---

## 3. Phân Hệ 3: Quản Trị & Xác Thực (Admin & Auth Security)

| STT | Màn hình / Tính năng | URL / Endpoint kiểm thử | Phương thức | Kịch bản kiểm thử | Trạng thái | Ghi chú & Đánh giá |
| :---: | :--- | :--- | :---: | :--- | :---: | :--- |
| **3.1** | **Đăng Nhập Khách Hàng / Quản Trị** | `/Account/Login` | GET & POST | Đăng nhập tài khoản với email & password hợp lệ / không hợp lệ. | <span style="color:#22c55e">PASS</span> | Phân loại quyền chính xác, lưu cookie an toàn HTTP-Only SameSite=Strict. |
| **3.2** | **Đăng Ký Tài Khoản Mới** | `/Account/Register` | GET & POST | Đăng ký thành viên mới với họ tên, email, sđt, mật khẩu. | <span style="color:#f59e0b">WARNING</span> | Tạo tài khoản tốt, nhưng lỗi validation trả về tiếng Anh mặc định (DEF-09). |
| **3.3** | **Bảng Điều Khiển Quản Trị (Dashboard)** | `/Admin/AdminDashboard` | GET | Đăng nhập tài khoản Admin, xem tổng quan doanh thu, đơn hàng, khách hàng. | <span style="color:#22c55e">PASS</span> | Biểu đồ hiển thị đẹp, các thẻ KPI thống kê chính xác. |
| **3.4** | **Quản Lý Bảng Dữ Liệu (Database Console)**| `/Admin/AdminDashboard/DatabaseConsole` | GET | Truy cập trang xem và truy vấn các bảng cơ sở dữ liệu hệ thống. | <span style="color:#ef4444;font-weight:bold">FAIL (P1)</span> | **SẬP MÁY CHỦ HTTP 500:** `AmbiguousMatchException` do trùng lặp định tuyến endpoint (DEF-02). |
| **3.5** | **Trang Chủ Quản Lý Danh Mục** | `/Admin/CatalogManager` | GET | Truy cập URL điều hướng chính của Catalog Manager. | <span style="color:#ef4444;font-weight:bold">FAIL (P2)</span> | **LỖI HTTP 404 NOT FOUND:** Controller thiếu action `Index()` (DEF-03). |
| **3.6** | **Quản Lý Thương Hiệu (Brands)** | `/Admin/CatalogManager/Brands` | GET & POST | Thêm, sửa, xóa thương hiệu nước hoa, upload logo, gán nhãn nổi bật. | <span style="color:#22c55e">PASS</span> | Hoạt động chính xác, bảo vệ Antiforgery Token đầy đủ. |
| **3.7** | **Quản Lý Nhóm Hương & Nốt Hương** | `/Admin/CatalogManager/FragranceFamilies`, `/FragranceNotes` | GET & POST | Quản trị tháp hương và nhóm hương chuẩn quốc tế. | <span style="color:#22c55e">PASS</span> | Lưu dữ liệu và phân loại chuẩn xác. |
| **3.8** | **Quản Lý Đơn Hàng (Admin Orders)** | `/Admin/AdminDashboard/Orders` | GET & POST | Xem danh sách đơn hàng, lọc theo trạng thái, cập nhật tiến độ xử lý. | <span style="color:#22c55e">PASS</span> | Phân trang tốt, chi tiết đơn hàng đầy đủ thông tin thanh toán. |
| **3.9** | **Quản Lý Tồn Kho (Admin Inventory)** | `/Admin/AdminDashboard/Inventory` | GET & POST | Điều chỉnh số lượng tồn kho từng phiên bản dung tích chai. | <span style="color:#22c55e">PASS</span> | Cập nhật kho chuẩn, cảnh báo sản phẩm sắp hết hàng. |
| **3.10** | **Quản Lý Khách Hàng (Admin Users)** | `/Admin/AdminDashboard/Users` | GET | Xem danh sách người dùng, tích điểm và phân hạng VIP (Silver, Gold, Diamond). | <span style="color:#22c55e">PASS</span> | Phân hạng tự động dựa trên tổng chi tiêu tích lũy. |
| **3.11** | **Xử Lý Từ Chối Phân Quyền (Access Control)**| `/Admin/AdminDashboard/Users` (bằng tài khoản Khách hàng) | GET | Dùng tài khoản người dùng bình thường cố tình truy cập khu vực Quản trị. | <span style="color:#f59e0b">WARNING</span> | Bị chuyển hướng im lặng về trang chủ thay vì hiển thị AccessDenied (DEF-05). |
| **3.12** | **Chống Chuyển Hướng Mở (Open Redirect)**| `/Account/Login?ReturnUrl=https://google.com` | GET | Thử tấn công lừa đảo chuyển hướng sang trang web giả mạo ngoài hệ thống. | <span style="color:#22c55e">PASS</span> | `Url.IsLocalUrl` chặn hoàn toàn, an toàn chuyển về `/account/profile`. |
| **3.13** | **Bảo Vệ Đổi Mật Khẩu & Hồ Sơ** | `/Account/EditProfile` | GET & POST | Cập nhật thông tin cá nhân và mật khẩu. | <span style="color:#22c55e">PASS</span> | Mã hóa mật khẩu PBKDF2/Identity an toàn, giao diện cần tinh chỉnh (DEF-09). |
