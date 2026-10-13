# Original User Request

## 2026-10-12T07:53:02Z

Triển khai toàn diện Hệ thống Bùng Nổ Tính Năng Thương Mại & Marketing (Phần 1) kết hợp Hệ thống RESTful API đồng bộ 100% với giao diện và Cổng kiểm thử trực tiếp Swagger UI (/swagger) chuẩn OpenAPI (Phần 2) cho nền tảng nước hoa cao cấp PERFUME LUXE.

Working directory: C:\Users\Sieu Chu nhiem\Desktop\dotnet
Integrity mode: demo

## Requirements

### R1. Flash Sale & Real-time Countdown Timer Engine
- **Mô hình dữ liệu & Quản trị**: Bổ sung thực thể chiến dịch Flash Sale (tên chiến dịch, thời gian bắt đầu/kết thúc, trạng thái kích hoạt, danh sách sản phẩm/phiên bản áp dụng, số lượng giới hạn và giá Flash Sale độc quyền). Cung cấp giao diện Admin quản lý/kích hoạt chiến dịch tại `/Admin/AdminDashboard/FlashSales`.
- **Giao diện Storefront**: Hiển thị Banner Flash Sale trên Homepage với đồng hồ đếm ngược (Countdown Timer) thời gian thực theo giây; gắn nhãn Flash Sale Badge trên Product Cards và Product Detail. Tự động chuyển về giá gốc khi hết giờ hoặc hết số lượng khuyến mãi.

### R2. Động Cơ Tự Động Thăng Hạng Thành Viên & Đặc Quyền VIP (Loyalty Tier Engine)
- **Tự động thăng hạng**: Cơ chế tự động tính toán tổng tích lũy chi tiêu của tài khoản qua các đơn hàng hoàn tất (`PaymentStatus.Completed` hoặc `OrderStatus.Delivered`) và tự động nâng hạng bậc thành viên theo ngưỡng: Bronze (0đ), Silver (>= 5.000.000đ - giảm 3%), Gold (>= 15.000.000đ - giảm 7%), Diamond (>= 30.000.000đ - giảm 12% + quà độc quyền).
- **Trải nghiệm khách hàng**: Hiển thị thẻ thành viên VIP kỹ thuật số với thanh tiến trình tích lũy (Progress Bar) đến hạng tiếp theo trong trang Hồ sơ cá nhân (`/Account/Profile`). Tự động nhận diện và tính chiết khấu VIP Tier tại bước Checkout kết hợp cùng Voucher.

### R3. Trình Thiết Kế Giỏ Quà Cá Nhân Hóa (Custom Discovery Gift Box Builder)
- **Trải nghiệm tương tác 3 bước**:
  1. *Chọn mẫu hộp hoàng gia*: Hộp Gỗ Sơn Mài Cổ Điển, Hộp Nhung Đen Hoàng Gia, hoặc Bao Da Thủ Công.
  2. *Chọn 3-5 ống vial mẫu thử*: Khách hàng tự do chọn các mùi hương yêu thích từ danh mục sản phẩm (kèm hiển thị dung tích 2ml-5ml và giá từng ống).
  3. *Cá nhân hóa lời chúc*: Nhập thông điệp thiệp mạ vàng, chọn phông chữ viết tay nghệ thuật.
- **Tích hợp thanh toán & Đơn hàng**: Tự động tính tổng giá trị trọn gói (giá hộp + giá các mẫu thử vial), thêm trọn gói vào Giỏ hàng (`/Cart`), đồng bộ chi tiết hộp quà vào Đơn hàng (`/Checkout` và `/Admin/AdminDashboard/OrderDetails`).

### R4. Hệ Thống RESTful API Toàn Diện & Cổng Swagger UI Live Test (Phần 2)
- **Nguyên tắc cốt lõi**: Bất kỳ tính năng nào xuất hiện trên giao diện Web (Flash Sale, Loyalty Tier, Custom Gift Box, Đặt lịch VIP Tasting, Tra cứu vận đơn...) đều phải có đầy đủ API Endpoints tương ứng tại `api/v1/...`.
  - `GET /api/v1/flashsales/active`: Lấy danh sách sản phẩm & thời gian chiến dịch Flash Sale đang diễn ra.
  - `GET /api/v1/loyalty/status`: Lấy thông tin cấp bậc thành viên hiện tại, tổng chi tiêu tích lũy, % giảm giá và điểm thưởng.
  - `POST /api/v1/discovery-box/validate-and-add`: Xác thực cấu hình hộp quà (loại hộp, 3-5 vials, thiệp viết tay) và tạo item giỏ hàng.
  - `GET /api/v1/orders/track`: Tra cứu hành trình vận đơn qua API.
  - `POST /api/v1/experience/tasting-booking`: Đặt lịch thử mùi VIP qua API.
- **Tích hợp Swagger UI Live Test (`/swagger`)**:
  - Cấu hình trọn gói `Swashbuckle.AspNetCore` trong `Program.cs`.
  - Cung cấp cổng Swagger UI tại `/swagger` với giao diện trực quan, tài liệu hóa đầy đủ Schema Model DTOs, Parameters, hỗ trợ nút "Try it out" gửi Request và xem Response JSON trực tiếp cho mọi API.

### R5. Kỹ Thuật Hệ Thống & Tự Động Test Toàn Vẹn
- Áp dụng cơ chế nâng cấp schema gia tăng (Additive Schema Upgrader) không làm gián đoạn cấu trúc dữ liệu hiện tại.
- Viết bộ kiểm thử tự động xUnit (TDD) trong dự án `tests/Perfume.Tests` kiểm tra: logic chiết khấu Flash Sale khi còn hạn/hết hạn, logic tự động nâng hạng Loyalty Tier theo chi tiêu, tính toán tổng tiền trọn gói Custom Discovery Box, và các Unit Tests cho API Endpoints mới.
- Tự động chạy Kestrel server kiểm tra thực tế Swagger UI (`/swagger`) và các API endpoints bằng HTTP requests, ghi lại log kết quả kiểm thử.

## Acceptance Criteria

### Tính Năng Giao Diện & Trải Nghiệm Khách Hàng
- [ ] Chiến dịch Flash Sale tạo trong Admin hiển thị chuẩn xác đồng hồ đếm ngược thời gian thực trên Homepage và trang chi tiết sản phẩm.
- [ ] Tài khoản khách hàng tự động được nâng hạng thành viên (ví dụ Silver/Gold) ngay sau khi đơn hàng đủ điều kiện hoàn tất thanh toán.
- [ ] Giao diện Custom Discovery Box Builder cho phép chọn hộp, chọn 3-5 ống mẫu thử vial, nhập lời chúc và bấm "Thêm vào giỏ hàng" thành công.
- [ ] Thông tin hộp quà cá nhân hóa hiển thị chi tiết trong giỏ hàng, trang thanh toán và chi tiết đơn hàng quản trị.

### Hệ Thống API & Swagger UI Live Test
- [ ] 100% tính năng mới có API tương ứng hoạt động hoàn hảo, trả về mã trạng thái HTTP chuẩn mực (200, 201, 400, 404).
- [ ] Truy cập `/swagger` hiển thị đầy đủ tài liệu API, cho phép thực hiện live test (Try it out) trực tiếp mọi endpoints trên trình duyệt.

### Tiêu Chuẩn Kỹ Thuật & Đảm Bảo Chất Lượng
- [ ] Toàn bộ mã nguồn biên dịch đạt 0 Warning, 0 Error (`dotnet build`).
- [ ] Toàn bộ các kiểm thử tự động xUnit vượt qua 100% (`dotnet test`).
- [ ] Tự động chạy live test toàn bộ API và giao diện, kiểm tra response 200 OK.
