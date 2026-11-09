# BÁO CÁO TOÀN DIỆN (CẬP NHẬT ĐỢT 2 - CHROME DEVTOOLS AUDIT)
# RÀ SOÁT LỖI GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG (UI/UX AUDIT)

**Dự án**: Perfume Luxe - Haute Parfumerie E-Commerce  
**Nền tảng kiểm thử thực tế**: `https://perfumeluxe.somee.com/` (Google Chrome DevTools Protocol Live Emulation)  
**Thời gian hoàn thành**: 07:00 Sáng 10/11/2026 (Ca kiểm thử độc lập)  
**Nguyên tắc tuân thủ**: *Ghi nhận 100% tất cả lỗi còn lại của web, không sửa đổi mã nguồn, chuẩn bị sẵn sàng danh mục xử lý tập trung cho ca chiều.*

---

## 1. TỔNG HỢP TOÀN BỘ CÁC LỖI GIAO DIỆN PHÁT HIỆN QUA CHROME DEVTOOLS

| Mã Lỗi | Hạng Mục / Trang Bị Ảnh Hưởng | Mức Độ | Tóm Tắt Hiện Tượng & Nguyên Nhân Kỹ Thuật |
| :--- | :--- | :---: | :--- |
| **UI-01** | **Điều Hướng & Routing (9 Route cũ)** | 🔴 **Critical** | Các URL chuẩn ASP.NET MVC (`/Home/About`, `/Home/Boutique`, `/Home/ReturnPolicy`, `/Home/BuyingGuide`, `/Home/Privacy`, `/Catalog/DiscoveryBox`, `/Catalog/Compare`, `/Experience/TastingBooking`, `/OrderTracking`) bị trả về **HTTP 404 Not Found** do thiếu middleware Redirect 301 sang slug tiếng Việt. |
| **UI-02** | **Mobile Header & Drawer Menu** | 🔴 **Critical** | Trên điện thoại iPhone (390x844), nút đóng menu `#closeMobileNavBtn` bị đặt tọa độ âm (`left: -63px`), văng ra ngoài màn hình (`outside viewport`). Header có phần tử gây tràn viền ngang (`docWidth > window.innerWidth`). |
| **UI-03** | **Ảnh Vỡ (Broken Images) Trang Chủ & Blog** | 🟠 **Major** | 3 ảnh bài viết chuyên đề tạp chí mùi hương trên trang chủ và một số ảnh thumbnail dẫn link Unsplash bị lỗi không tải được (`naturalWidth = 0`), làm mất thẩm mỹ trang web xa xỉ. |
| **UI-04** | **Form Validation (Login & Register)** | 🟠 **Major** | Tại trang `/account/login` và `/account/register`, khi bấm nút "Đăng Nhập" / "Đăng Ký" với form trống, giao diện **hoàn toàn không hiển thị thông báo lỗi kiểm tra dữ liệu** (không có viền đỏ hay thông báo "Vui lòng nhập Email/Mật khẩu"). |
| **UI-05** | **Đồng Bộ Giỏ Hàng Realtime (Detail Page)** | 🟠 **Major** | Khi ở trang Chi tiết sản phẩm (`/nuoc-hoa/chanel-coco-mademoiselle-edp`) bấm nút "Thêm vào giỏ", sản phẩm đã vào session nhưng **Huy hiệu số lượng giỏ hàng trên Header (Cart Count Badge) không cập nhật tức thì**, dễ khiến khách hàng click nhiều lần. |
| **UI-06** | **Tra Cứu Đơn Hàng Không Có Alert Phản Hồi** | 🟡 **Minor** | Tại trang `/tra-cuu-don-hang`, khi nhập một mã đơn hàng không tồn tại (ví dụ: `ORD-9999999`), giao diện không hiển thị khung thông báo lỗi màu đỏ rõ ràng (Alert) báo cho khách biết mã đơn không chính xác. |
| **UI-07** | **Thanh Tìm Kiếm Trên Header Desktop** | 🟡 **Minor** | Icon kính lúp trên Header Desktop chưa tích hợp ô gợi ý sản phẩm nổi bật (Autocomplete Dropdown) khi người dùng vừa nhấp chuột. |

---

## 2. KẾT QUẢ ĐÁNH GIÁ CÁC TÍNH NĂNG HOẠT ĐỘNG TỐT (VERIFIED WORKING)

1. **AI Fragrance Finder Quiz (`/catalog/quiz`)**:
   - Trải nghiệm 5 bước cực kỳ mượt mà.
   - Sau khi hoàn thành Step 5, hệ thống tự động chấm điểm ma trận và sinh **Thẻ Danh Phận Mùi Hương (Scent Archetype Dossier)** kèm **4 sản phẩm đề xuất** chuẩn xác theo sở thích.
2. **Bộ Sưu Tập Nước Hoa (`/bo-suu-tap`)**:
   - Hiển thị đầy đủ 15 kiệt tác mùi hương từ Chanel, Dior, Creed, Tom Ford, Kilian, Le Labo...
   - Bộ lọc đa chiều (Giới tính, Thương hiệu, Nhóm hương) hoạt động chuẩn.
3. **Công Cụ So Sánh Mùi Hương (`/so-sanh`)**:
   - Bảng so sánh hiển thị đầy đủ các tiêu chuẩn kỹ thuật: Nồng độ, Độ lưu tỏa, Kim tự tháp 3 tầng hương, Tầm giá.
   - Đáp ứng chuẩn responsive trên điện thoại (không bị tràn viền).
4. **Hộp Quà Discovery Box (`/discovery-box`)**:
   - Load thành công 16 ống mẫu chiết nước hoa chính hãng để người dùng tự do phối chọn combo.
5. **Đặt Lịch Trải Nghiệm Tasting VIP Concierge (`/dat-lich-trai-nghiem`)**:
   - Form đặt lịch đầy đủ 12 trường thông tin cần thiết kèm bảo mật CSRF Token (`__RequestVerificationToken`).
6. **Bảo Mật Khu Vực Quản Trị (`/admin`)**:
   - Được bảo vệ an toàn tuyệt đối, người dùng chưa đăng nhập sẽ lập tức bị chặn và chuyển hướng về `/account/login?ReturnUrl=%2Fadmin`.

---

## 3. DANH SÁCH TÁC VỤ THỰC THI CHO CA CHIỀU (ACTION PLAN)

1. **Routing (`Program.cs`)**: Thêm cấu hình redirect các route MVC cũ sang slug tiếng Việt mới.
2. **Media**: Tải bộ ảnh thay thế và cập nhật đường dẫn ảnh blog cục bộ trong database/view.
3. **CSS / Mobile Layout**: Sửa class `#closeMobileNavBtn`, gỡ bỏ `left: -63px`, bổ sung `overflow-x: hidden`.
4. **JavaScript Form Validation**: Nhúng bundle `_ValidationScriptsPartial` cho `Login.cshtml` và `Register.cshtml`.
5. **Cart Realtime**: Bổ sung hàm cập nhật DOM số lượng giỏ hàng sau khi AJAX thêm hàng thành công.
6. **Order Tracking**: Thêm khung thông báo `<div class="alert alert-warning">` khi API tra cứu trả về rỗng.
