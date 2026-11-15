# Chuyên Đề 03: Hồ Sơ Chi Tiết Từng Khiếm Khuyết (Detailed Defect Reports)
**Phạm vi áp dụng:** Website live [https://perfumeluxe.somee.com/](https://perfumeluxe.somee.com/)  
**Định dạng hồ sơ:** Chuẩn kiểm thử phần mềm quốc tế IEEE Std 829 & ISTQB Defect Tracking  

---

### DEF-01: Lỗi Gián Đoạn Đặt Hàng Do Mâu Thuẫn Enum PaymentMethod
- **Mức độ nghiêm trọng:** `P0 - BLOCKER` (Tối khẩn cấp)
- **Phân loại khiếm khuyết:** Lỗi Hợp Đồng API & Logic Nghiệp Vụ (API Contract & Business Logic)
- **Phân hệ ảnh hưởng:** Luồng Thanh Toán Thương Mại (Commerce / Checkout)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/checkout` và `https://perfumeluxe.somee.com/api/v1/orders`
- **Tiền điều kiện:** Giỏ hàng có ít nhất 01 sản phẩm còn hàng trong kho.
- **Các bước tái hiện (Reproduction Steps):**
  1. Truy cập trang chi tiết sản phẩm bất kỳ (ví dụ: `/nuoc-hoa/chanel-coco-mademoiselle`).
  2. Chọn dung tích 100ml Fullbox và nhấn nút "Thêm Vào Giỏ Hàng".
  3. Mở trang thanh toán `/checkout`.
  4. Điền đầy đủ thông tin người nhận hợp lệ:
     - Họ và tên: `Nguyễn Văn A`
     - Số điện thoại: `0912345678`
     - Email: `khachhang@gmail.com`
     - Địa chỉ nhận hàng: `Số 123 Đường Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh`
  5. Tích chọn checkbox: "Tôi đã đọc, hiểu rõ và tự nguyện đồng ý với Điều khoản mua hàng và Chính sách bảo vệ dữ liệu cá nhân theo Luật 91/2025/QH15."
  6. Giữ phương thức thanh toán mặc định là "Thanh toán khi nhận hàng (COD)".
  7. Nhấn nút "Xác Nhận Đặt Hàng Ngay".
- **Kết quả thực tế (Actual Behavior):**
  - Biểu mẫu hiển thị hộp thông báo lỗi màu đỏ ngay đầu trang:  
    `"Phương thức thanh toán chưa được hỗ trợ."`
  - Nếu người dùng chuyển sang chọn "Chuyển khoản ngân hàng VietQR": Client gửi `paymentMethod: 1` (backend hiểu là COD), và khi database ghi nhận lỗi giao dịch relational update, hệ thống quăng thông báo lỗi:  
    `"Không thể hoàn tất đơn hàng. Tồn kho có thể vừa thay đổi; vui lòng kiểm tra giỏ hàng và thử lại."`
  - Đơn hàng không bao giờ được tạo, khách hàng hoàn toàn bế tắc tại trang checkout.
- **Kết quả mong đợi (Expected Behavior):**
  - Đơn hàng COD được tạo thành công, điều hướng người dùng tới trang thông báo hoàn tất `/checkout/success?token=...` kèm mã đơn hàng định dạng `LUXE-YYYYMMDD-XXXXX`.
  - Đơn hàng VietQR điều hướng tới trang hiển thị mã QR Vietcombank động `/checkout/vietqr?token=...`.
- **Tác động nghiệp vụ (Impact Analysis):**
  - **Tổn thất 100% doanh thu:** Khách hàng không thể hoàn tất bất kỳ giao dịch mua sắm nào trên website live. Đây là lỗi nghiêm trọng nhất của một hệ thống thương mại điện tử.

---

### DEF-02: Sập Máy Chủ HTTP 500 Trang Database Console Quản Trị
- **Mức độ nghiêm trọng:** `P1 - CRITICAL` (Nghiêm trọng)
- **Phân loại khiếm khuyết:** Lỗi Hạ Tầng Định Tuyến & Sập Máy Chủ (Endpoint Routing Ambiguity)
- **Phân hệ ảnh hưởng:** Quản Trị Hệ Thống (Admin Area)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/Admin/AdminDashboard/DatabaseConsole`
- **Tiền điều kiện:** Không (Xảy ra ở tầng Routing trước cả khi kiểm tra xác thực người dùng).
- **Các bước tái hiện (Reproduction Steps):**
  1. Mở trình duyệt web hoặc terminal curl.
  2. Gửi yêu cầu GET tới URL: `https://perfumeluxe.somee.com/Admin/AdminDashboard/DatabaseConsole`
- **Kết quả thực tế (Actual Behavior):**
  - Máy chủ Kestrel trả về mã trạng thái **HTTP 500 Internal Server Error**.
  - Giao diện hiển thị trang lỗi sự cố:  
    `"Đã Xảy Ra Sự Cố Máy Chủ. Hệ thống đã tự động ghi nhận nhật ký lỗi kỹ thuật: ERR-20261115-XXXXXX"`
  - Kèm quảng cáo hosting của Somee.com chèn vào cuối trang.
- **Kết quả mong đợi (Expected Behavior):**
  - Nếu chưa đăng nhập: Phải chuyển hướng an toàn về `/Account/Login?ReturnUrl=...`.
  - Nếu đã đăng nhập quyền Admin: Phải hiển thị giao diện bảng điều khiển cơ sở dữ liệu `DatabaseConsole` cho phép xem danh sách bảng và dữ liệu.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Quản trị viên hệ thống không thể giám sát tình trạng cơ sở dữ liệu, kiểm tra cấu trúc bảng hoặc xem xét dữ liệu thực tế trên môi trường sản xuất.

---

### DEF-03: Lỗi 404 Not Found Tại Trang Quản Lý Danh Mục Gốc
- **Mức độ nghiêm trọng:** `P2 - HIGH` (Mức độ cao)
- **Phân loại khiếm khuyết:** Liên Kết Hỏng / Thiếu Action Mặc Định (Broken Link / Missing Action)
- **Phân hệ ảnh hưởng:** Quản Trị Danh Mục Hàng Hóa (Catalog Manager)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/Admin/CatalogManager`
- **Tiền điều kiện:** Đăng nhập tài khoản có quyền `Admin` hoặc `StoreManager`.
- **Các bước tái hiện (Reproduction Steps):**
  1. Đăng nhập vào trang quản trị.
  2. Nhấp vào liên kết menu danh mục chính "Quản Lý Danh Mục" hoặc nhập trực tiếp URL: `https://perfumeluxe.somee.com/Admin/CatalogManager`.
- **Kết quả thực tế (Actual Behavior):**
  - Trình duyệt trả về trang trắng với mã trạng thái **HTTP 404 Not Found**.
  - Các trang con như `/Admin/CatalogManager/Brands`, `/Admin/CatalogManager/FragranceFamilies` vẫn tồn tại nếu gõ đúng đường dẫn chi tiết.
- **Kết quả mong đợi (Expected Behavior):**
  - Truy cập `/Admin/CatalogManager` phải tự động hiển thị hoặc chuyển hướng về trang quản lý thương hiệu mặc định (`/Admin/CatalogManager/Brands`).
- **Tác động nghiệp vụ (Impact Analysis):**
  - Gây gián đoạn trải nghiệm quản trị, tạo ấn tượng trang web đang phát triển dở dang và thiếu tính hoàn thiện.

---

### DEF-04: Mã Ưu Đãi Gợi Ý Trên Form Thanh Toán Bị Vô Hiệu Hóa
- **Mức độ nghiêm trọng:** `P2 - HIGH` (Mức độ cao)
- **Phân loại khiếm khuyết:** Lỗi Logic Dữ Liệu & Tính Đồng Bộ UI (Data Inconsistency & UI Mismatch)
- **Phân hệ ảnh hưởng:** Luồng Khuyến Mãi & Thanh Toán (Commerce / Promotions)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/checkout` và `/api/v1/vouchers/validate`
- **Tiền điều kiện:** Giỏ hàng có sản phẩm bất kỳ.
- **Các bước tái hiện (Reproduction Steps):**
  1. Mở trang thanh toán `/checkout`.
  2. Quan sát dòng chữ hướng dẫn tại ô nhập mã giảm giá:  
     `"Mã Giảm Giá / Ưu Đãi (Thử: LUXE2026 hoặc WELCOME)"`
  3. Nhập mã `LUXE2026` vào ô input và nhấn nút "Áp Dụng".
  4. Lặp lại với mã `WELCOME`.
- **Kết quả thực tế (Actual Behavior):**
  - Dưới ô nhập liệu xuất hiện thông báo màu đỏ:  
     `"Mã ưu đãi không hợp lệ hoặc chưa đủ điều kiện áp dụng."`
  - Tổng số tiền cần thanh toán không được áp dụng mức giảm 10% như đã hứa khi đăng ký Newsletter ở trang chủ.
- **Kết quả mong đợi (Expected Behavior):**
  - Khi nhập mã `LUXE2026`, hệ thống phải xác thực thành công, hiển thị dòng chữ xanh: `"Áp dụng thành công mã LUXE2026."` và trừ tương ứng 10% giá trị giỏ hàng vào tổng đơn.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Gây ức chế tâm lý khách hàng tại bước chốt đơn (abandoned cart), giảm độ uy tín thương hiệu cao cấp do đưa ra gợi ý nhưng hệ thống từ chối.

---

### DEF-05: Vòng Lặp Chuyển Hướng Im Lặng Khi Thiếu Quyền Quản Trị
- **Mức độ nghiêm trọng:** `P2 - HIGH` (Mức độ cao)
- **Phân loại khiếm khuyết:** Lỗi Điều Khiển Quyền Truy Cập & UX (Access Control & UX Feedback)
- **Phân hệ ảnh hưởng:** Quản Trị & Xác Thực (Admin / Authorization)
- **URL ảnh hưởng:** Tất cả các URL quản trị: `https://perfumeluxe.somee.com/Admin/AdminDashboard/*`
- **Tiền điều kiện:** Đã đăng nhập bằng tài khoản người dùng bình thường (`Customer`).
- **Các bước tái hiện (Reproduction Steps):**
  1. Đăng ký/đăng nhập tài khoản khách hàng thông thường trên website.
  2. Nhập URL quản trị trên thanh địa chỉ: `/Admin/AdminDashboard/Users` hoặc `/Admin/AdminDashboard/Orders`.
- **Kết quả thực tế (Actual Behavior):**
  - Máy chủ trả về mã HTTP 302 Found chuyển hướng tới: `/Account/Login?ReturnUrl=%2FAdmin%2FAdminDashboard%2FUsers`.
  - Tại `/Account/Login`, do người dùng đã có phiên đăng nhập, hệ thống tiếp tục trả về HTTP 302 Found chuyển hướng người dùng thẳng về Trang Chủ (`/`).
  - Người dùng không nhìn thấy bất kỳ thông báo lỗi nào (không thấy trang `/Account/AccessDenied` hay thông điệp "Bạn không có quyền truy cập khu vực này").
- **Kết quả mong đợi (Expected Behavior):**
  - Khi người dùng đã đăng nhập nhưng không đủ quyền (thiếu Role `Admin`), hệ thống phải hiển thị rõ ràng trang **Truy Cập Bị Từ Chối (HTTP 403 Forbidden / Access Denied)** với thông điệp giải thích rõ ràng và nút quay lại trang cá nhân.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Hành vi im lặng khiến người dùng tưởng website bị lỗi tải trang, không đạt tiêu chuẩn thiết kế hệ thống bảo mật chuyên nghiệp.

---

### DEF-06: Tiêu Đề Báo Sai Ngữ Nghĩa Khi Tra Cứu Đơn Thiếu SĐT
- **Mức độ nghiêm trọng:** `P3 - MEDIUM` (Trung bình)
- **Phân loại khiếm khuyết:** Trải Nghiệm Người Dùng & Bản Đồ Văn Bản (UI/UX Copywriting)
- **Phân hệ ảnh hưởng:** Tra Cứu Đơn Hàng (Order Tracking)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/tra-cuu-don-hang?orderCode=LUXE-20261011-9A3F&phone=`
- **Tiền điều kiện:** Có mã đơn hàng hợp lệ đã đặt trong hệ thống.
- **Các bước tái hiện (Reproduction Steps):**
  1. Mở trang Tra cứu đơn hàng `/tra-cuu-don-hang`.
  2. Điền mã đơn hàng vào ô "Mã Đơn Hàng" (ví dụ: `LUXE-20261011-9A3F`).
  3. Để trống ô "Số Điện Thoại Nhận Hàng".
  4. Nhấn nút "Tra Cứu Ngay".
- **Kết quả thực tế (Actual Behavior):**
  - Màn hình hiển thị icon chấm than đỏ và tiêu đề lớn:  
    `"Không Tìm Thấy Thông Tin Đơn Hàng"`
  - Bên dưới hiển thị dòng chữ xám nhỏ: *"Vui lòng nhập thêm Số điện thoại nhận hàng để bảo mật và xác thực đơn hàng."*
- **Kết quả mong đợi (Expected Behavior):**
  - Tiêu đề lớn phải phản ánh đúng bản chất yêu cầu:  
    `"Vui Lòng Nhập Số Điện Thoại Để Xác Thực Đơn Hàng"`
  - Tiêu đề `"Không Tìm Thấy Thông Tin Đơn Hàng"` chỉ nên hiển thị khi cả 2 thông tin đã nhập đầy đủ nhưng mã đơn không tồn tại hoặc số điện thoại không khớp.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Làm khách hàng hoảng loạn vì nghĩ rằng đơn hàng mình vừa đặt và thanh toán đã bị xóa mất khỏi hệ thống.

---

### DEF-07: Lặp Lồng Thẻ `<main>` Vi Phạm Chuẩn HTML5 & WCAG 2.1
- **Mức độ nghiêm trọng:** `P3 - MEDIUM` (Trung bình)
- **Phân loại khiếm khuyết:** Tiêu Chuẩn Ngữ Nghĩa W3C & Tiếp Cận (HTML5 Semantics & WCAG Accessibility)
- **Phân hệ ảnh hưởng:** Giao Diện Storefront (Discovery Box, Private Tasting, Blog Detail)
- **URL ảnh hưởng:**
  - `https://perfumeluxe.somee.com/discovery-box`
  - `https://perfumeluxe.somee.com/dat-lich-trai-nghiem`
  - `https://perfumeluxe.somee.com/blog/xu-huong-nuoc-hoa-niche-2026`
- **Tiền điều kiện:** Truy cập các URL trên.
- **Các bước tái hiện (Reproduction Steps):**
  1. Mở trang `https://perfumeluxe.somee.com/discovery-box`.
  2. Bấm chuột phải chọn `View Page Source` (Xem nguồn trang) hoặc Inspect Element.
  3. Tìm kiếm chuỗi `<main`.
- **Kết quả thực tế (Actual Behavior):**
  - Thẻ `<main role="main">` từ file layout chung `_Layout.cshtml` bọc toàn bộ nội dung.
  - Ngay bên trong nội dung của view con, lập trình viên lại khai báo thêm thẻ `<main style="max-width: 1360px; ...">` hoặc `<main class="blog-content-col">`.
  - Cấu trúc DOM xuất hiện 2 thẻ `<main>` lồng nhau trực tiếp: `<main><main>...</main></main>`.
- **Kết quả mong đợi (Expected Behavior):**
  - Trong một tài liệu HTML5 hợp lệ, mỗi trang chỉ được phép tồn tại duy nhất một thẻ `<main>` hiển thị làm mốc nội dung chính (Primary Landmark). Các khối tử bên trong phải sử dụng thẻ `<section>`, `<article>` hoặc `<div>`.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Vi phạm chuẩn W3C HTML5 validator và chuẩn tiếp cận người khiếm thị WCAG 2.1 (Screen readers như JAWS, NVDA bị xung đột điều hướng landmark).

---

### DEF-08: Khối Banner Flash Sale Để Trống Trên Trang Chủ
- **Mức độ nghiêm trọng:** `P3 - MEDIUM` (Trung bình)
- **Phân loại khiếm khuyết:** Trải Nghiệm Giao Diện & Quản Lý Trạng Thái (UI State Handling)
- **Phân hệ ảnh hưởng:** Trang Chủ (Homepage Storefront)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/`
- **Tiền điều kiện:** Hệ thống không có chiến dịch Flash Sale nào đang hoạt động (`/api/v1/flashsales/active` trả về 404).
- **Các bước tái hiện (Reproduction Steps):**
  1. Truy cập trang chủ `https://perfumeluxe.somee.com/`.
  2. Cuộn trang kiểm tra vị trí giữa Hero Banner và Danh sách Best Sellers.
- **Kết quả thực tế (Actual Behavior):**
  - Do không có sự kiện đang diễn ra, khu vực Flash Sale biến mất hoàn toàn hoặc để lại khoảng trắng, không có bất kỳ thông tin nào nhắc nhở khách hàng về các phiên ưu đãi tiếp theo.
- **Kết quả mong đợi (Expected Behavior):**
  - Khi không có Flash Sale, hệ thống nên hiển thị một banner tinh tế với thông điệp:  
    `"Phiên Flash Sale Haute Parfumerie tiếp theo đang được chuẩn bị. Đăng ký thành viên để nhận thông báo sớm nhất!"` kèm nút đăng ký câu lạc bộ.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Mất đi cơ hội duy trì sự tò mò và hứng khởi mua sắm định kỳ của khách hàng thân thiết.

---

### DEF-09: Thông Báo Lỗi Form Hiển Thị Tiếng Anh Kỹ Thuật ProblemDetails
- **Mức độ nghiêm trọng:** `P3 - MEDIUM` (Trung bình)
- **Phân loại khiếm khuyết:** Bản Địa Hóa & Xử Lý Lỗi Phía Client (I18n & Client-side Error Extraction)
- **Phân hệ ảnh hưởng:** Xác Thực & Hồ Sơ Thành Viên (Auth & Profile)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/Account/Register` và `/Account/EditProfile`
- **Tiền điều kiện:** Truy cập form đăng ký hoặc cập nhật thông tin.
- **Các bước tái hiện (Reproduction Steps):**
  1. Vào trang đăng ký tài khoản `/Account/Register`.
  2. Nhập các trường thông tin nhưng cố tình để số điện thoại sai định dạng hoặc mật khẩu không khớp.
  3. Nhấn "Đăng Ký".
- **Kết quả thực tế (Actual Behavior):**
  - Hộp thông báo màu đỏ hiển thị chuỗi chữ tiếng Anh mặc định của ASP.NET Core:  
    `"One or more validation errors occurred."`
  - Người dùng không được chỉ rõ cụ thể trường nào đang bị lỗi và cần chỉnh sửa những gì.
- **Kết quả mong đợi (Expected Behavior):**
  - Giao diện phải hiển thị tiếng Việt chi tiết:  
    `"Số điện thoại không đúng định dạng. Vui lòng nhập từ 10 đến 11 chữ số."` hoặc `"Mật khẩu xác nhận không trùng khớp."`
- **Tác động nghiệp vụ (Impact Analysis):**
  - Gây khó khăn cho khách hàng Việt Nam khi thao tác đăng ký tài khoản, làm giảm tỷ lệ đăng ký người dùng mới.

---

### DEF-10: Thiếu Thuộc Tính `rel="noopener noreferrer"` Tại Link Checkout
- **Mức độ nghiêm trọng:** `P4 - LOW` (Mức độ thấp / Khuyến nghị bảo mật)
- **Phân loại khiếm khuyết:** An Toàn Điều Hướng Thẻ Siêu Liên Kết (HTML Link Security & Performance)
- **Phân hệ ảnh hưởng:** Trang Thanh Toán (Checkout)
- **URL ảnh hưởng:** `https://perfumeluxe.somee.com/checkout`
- **Tiền điều kiện:** Truy cập trang thanh toán `/checkout`.
- **Các bước tái hiện (Reproduction Steps):**
  1. Mở trang `/checkout`.
  2. Kiểm tra mã HTML tại dòng 222-223:
     ```html
     <a href="/huong-dan-mua-hang" target="_blank" ...>Điều khoản mua hàng</a>
     <a href="/privacy" target="_blank" ...>Chính sách bảo vệ dữ liệu cá nhân</a>
     ```
- **Kết quả thực tế (Actual Behavior):**
  - Các thẻ `<a>` có thuộc tính `target="_blank"` nhưng không có thuộc tính `rel="noopener noreferrer"`.
- **Kết quả mong đợi (Expected Behavior):**
  - Mọi liên kết có `target="_blank"` phải luôn đi kèm `rel="noopener noreferrer"` để ngăn chặn nguy cơ tấn công `tabnabbing` (thao túng đối tượng `window.opener`) và cải thiện hiệu năng giải phóng luồng xử lý của trình duyệt.
- **Tác động nghiệp vụ (Impact Analysis):**
  - Rủi ro an ninh ở mức thấp, tuy nhiên cần bổ sung để đáp ứng hoàn toàn các tiêu chuẩn kiểm định an toàn web hiện đại.
