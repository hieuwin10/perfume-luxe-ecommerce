# NHẬT KÝ KIỂM THỬ TOÀN DIỆN GIAO DIỆN & TRẢI NGHIỆM NGƯỜI DÙNG
## Dự án: Perfume Luxe - Haute Parfumerie E-Commerce
**Phiên thực hiện**: Sáng 10/11/2026 (06:39 - 07:02 Asia/Saigon)  
**Tác giả**: Antigravity Autonomous Pair Programmer  
**Mục tiêu**: Đóng vai người dùng thực tế kiểm tra toàn bộ giao diện website trên môi trường live `https://perfumeluxe.somee.com/`, phát hiện và ghi nhận 100% tất cả các lỗi giao diện còn tồn đọng, **bảo toàn mã nguồn** để chuẩn bị phương án khắc phục triệt để trong ca chiều.

---

## 1. TỔNG QUAN PHẠM VI & PHƯƠNG PHÁP KIỂM THỬ

* **Môi trường Live**: `https://perfumeluxe.somee.com/` (Máy chủ Kestrel, .NET 8 MVC, Clean Architecture).
* **Công cụ thực thi**: 
  - Trình duyệt **Google Chrome** kết hợp **Chrome DevTools Protocol (CDP)** và **Playwright**.
  - Kiểm thử đồng thời trên 2 độ phân giải chuẩn: **Desktop Full HD (1920x1080)** và **Mobile iPhone (390x844)**.
* **Số lượng màn hình & luồng tương tác**: **34 màn hình** bao phủ 100% storefront và các tính năng cốt lõi.

---

## 2. KẾT QUẢ ĐÁNH GIÁ CHI TIẾT TỪNG PHÂN HỆ

### 2.1. Phân hệ Hoạt động Xuất sắc (Đạt Chuẩn Haute Parfumerie)
1. **AI Fragrance Finder Quiz (`/catalog/quiz`)**:
   - Vượt qua 5 bước trắc nghiệm mượt mà: *Đối tượng ➔ Phong thái ➔ Hoàn cảnh ➔ Cường độ lưu tỏa ➔ Ngân sách & Định dạng*.
   - Động cơ đề xuất ma trận tính điểm chính xác và sinh ra **Thẻ Danh Phận Mùi Hương (Scent Archetype Dossier)** kèm **4 sản phẩm đề xuất tối ưu** (Ví dụ: Archetype *The Sovereign Aristocrat* đề xuất *Creed Aventus, Tom Ford Tobacco Vanille, Kilian Angels' Share, Bleu de Chanel*).
2. **Bộ Sưu Tập Nước Hoa (`/bo-suu-tap`)**:
   - Hiển thị đầy đủ 15 kiệt tác mùi hương chính hãng với hình ảnh sắc nét.
   - Bộ lọc đa chiều (Giới tính, Thương hiệu, Nhóm hương) và chức năng xóa lọc hoạt động trơn tru.
3. **Công Cụ So Sánh Mùi Hương (`/so-sanh`)**:
   - Bảng so sánh 2 chai (*Bleu de Chanel Parfum* vs *Chanel Coco Mademoiselle EDP*) hiển thị đầy đủ tiêu chí: Xuất xứ, Nhóm hương, Nồng độ, Độ lưu tỏa, Kim tự tháp 3 tầng hương, Giá bán.
   - Hoàn toàn tương thích responsive trên thiết bị di động, không bị vỡ bố cục.
4. **Hộp Quà Discovery Box (`/discovery-box`)**:
   - Tải thành công 16 ống mẫu thử (Vials 10ml) cao cấp, giao diện chọn combo sang trọng.
5. **Đặt Lịch Trải Nghiệm Tasting VIP Concierge (`/dat-lich-trai-nghiem`)**:
   - Form đặt lịch đầy đủ 12 trường thông tin, tích hợp token bảo mật CSRF.
6. **Bảo Mật Khu Vực Quản Trị (`/admin`)**:
   - Cơ chế ủy quyền an toàn: Mọi truy cập vào `/admin` khi chưa đăng nhập đều bị chặn và chuyển hướng về `/account/login?ReturnUrl=%2Fadmin`.

---

### 2.2. Bảng Tổng Hợp 7 Lỗi Giao Diện Cần Xử Lý Vào Ca Chiều

| Mã Lỗi | Phân Hệ / URL | Mức Độ | Mô Tả Hiện Tượng Chi Tiết | Nguyên Nhân & Hướng Xử Lý Ca Chiều |
| :---: | :--- | :---: | :--- | :--- |
| **UI-01** | **Điều Hướng / Routing**<br>`/Home/About`<br>`/Home/Boutique`<br>`/Home/ReturnPolicy`<br>`/Home/BuyingGuide`<br>`/Home/Privacy`<br>`/Catalog/DiscoveryBox`<br>`/Catalog/Compare`<br>`/Experience/TastingBooking`<br>`/OrderTracking` | 🔴 **Critical** | Khách hàng hoặc bot truy cập theo đường dẫn MVC cũ thì Kestrel trả về **404 Not Found** trắng xóa. | **Nguyên nhân**: Website đã đổi sang canonical slug tiếng Việt nhưng thiếu cơ chế rewrite/redirect cho các route cũ.<br>**Hướng xử lý**: Cấu hình `MapControllerRoute` hoặc middleware tự động `RedirectPermanent (301)` từ 9 route MVC cũ về slug tiếng Việt tương ứng. |
| **UI-02** | **Mobile Header & Drawer**<br>`Header di động (390x844)` | 🔴 **Critical** | 1. Nút đóng menu `#closeMobileNavBtn` bị đặt tọa độ âm `left: -63px` nằm ngoài khung nhìn (outside viewport).<br>2. Xuất hiện thanh cuộn ngang nhẹ (`docWidth > 390px`) khi mở trang chủ mobile. | **Nguyên nhân**: Lỗi CSS transform/positioning trên drawer navigation.<br>**Hướng xử lý**: Chuẩn hóa CSS drawer theo Bootstrap 5 / Offcanvas tiêu chuẩn, gỡ bỏ thuộc tính định vị âm và thêm `overflow-x: hidden` trên body. |
| **UI-03** | **Media / Ảnh Vỡ**<br>`Trang Chủ & Blog` | 🟠 **Major** | 3 ảnh bài viết tạp chí mùi hương chuyên đề trên trang chủ và một số ảnh thumbnail dẫn link Unsplash bị lỗi không tải được (`naturalWidth = 0`). | **Nguyên nhân**: Liên kết ngoài Unsplash bị chặn hotlinking hoặc quá tải CDN.<br>**Hướng xử lý**: Tải ảnh chất lượng cao về lưu trữ cục bộ trong thư mục `wwwroot/images/blogs/` hoặc dùng placeholder WebP sang trọng nội bộ. |
| **UI-04** | **Form Validation**<br>`/account/login`<br>`/account/register` | 🟠 **Major** | Khi người dùng nhấn nút "Đăng Nhập" hoặc "Đăng Ký" với các ô dữ liệu để trống, giao diện hoàn toàn không có phản hồi viền đỏ hay câu báo lỗi validation nào. | **Nguyên nhân**: View chưa kích hoạt script kiểm tra form phía client (`_ValidationScriptsPartial` hoặc unobtrusive validation).<br>**Hướng xử lý**: Nhúng partial validation và bổ sung thẻ span `asp-validation-for` kèm class Bootstrap `is-invalid`. |
| **UI-05** | **Đồng Bộ Giỏ Hàng Realtime**<br>`/nuoc-hoa/{slug}` | 🟠 **Major** | Khi khách hàng nhấn "Thêm vào giỏ" ở trang chi tiết sản phẩm, hàng đã vào session nhưng số đếm giỏ hàng trên Header (Cart Count Badge) không cập nhật tức thì nếu chưa F5. | **Nguyên nhân**: Sự kiện AJAX thêm hàng thiếu callback cập nhật lại DOM header badge.<br>**Hướng xử lý**: Dispatch custom event `cart:updated` và viết hàm cập nhật số đếm badge realtime ngay sau khi API trả về 200. |
| **UI-06** | **Tra Cứu Đơn Hàng**<br>`/tra-cuu-don-hang` | 🟡 **Minor** | Khi nhập mã đơn hàng không tồn tại (VD: `ORD-9999999`), giao diện không hiển thị khung cảnh báo màu đỏ (Alert) thông báo không tìm thấy đơn. | **Nguyên nhân**: Thiếu khối thông báo lỗi khi kết quả tra cứu trả về null.<br>**Hướng xử lý**: Bổ sung khối `<div class="alert alert-warning">` hiển thị thông báo hướng dẫn người dùng kiểm tra lại mã đơn hoặc số điện thoại. |
| **UI-07** | **Thanh Tìm Kiếm Desktop**<br>`Header Desktop` | 🟡 **Minor** | Nút tìm kiếm trên Header Desktop dạng icon chưa hiển thị dropdown gợi ý sản phẩm nổi bật ngay khi nhấp chuột. | **Nguyên nhân**: Chưa có component autocomplete drop-down.<br>**Hướng xử lý**: Bổ sung popup tìm kiếm nhanh với danh sách top 3 chai nước hoa bán chạy nhất. |

---

## 3. KẾ HOẠCH TRIỂN KHAI CA CHIỀU (CHI TIẾT TỪNG BƯỚC)

```mermaid
flowchart LR
    Step1["1. Fix 9 Route 301<br>(Program.cs)"] --> Step2["2. Thay Ảnh Unsplash<br>(wwwroot/images)"]
    Step2 --> Step3["3. Sửa CSS Mobile Drawer<br>(Header/Offcanvas)"]
    Step3 --> Step4["4. Kích hoạt Form Validation<br>(Login/Register)"]
    Step4 --> Step5["5. Đồng bộ Cart Realtime<br>(Detail Page JS)"]
    Step5 --> Step6["6. Thêm Alert Tra Cứu Đơn<br>(OrderTracking View)"]
    Step6 --> Verification["7. Chạy Playwright E2E<br>Kiểm tra xanh 100%"]
    Verification --> GitCommit["8. Commit & Push Git<br>Hoàn tất ca chiều"]
```

---

## 4. XÁC NHẬN BẢO TOÀN TRẠNG THÁI HỆ THỐNG
* **Mã nguồn dự án**: Được bảo toàn nguyên trạng, không phát sinh bất kỳ thay đổi nào trong ca sáng theo đúng chỉ thị.
* **Báo cáo và nhật ký**: Đã được lưu vào 2 vị trí độc lập:
  1. `docs/reviews/NHAT_KY_KIEM_THU_GIAO_DIEN_2026-11-10.md`
  2. `C:\Users\Sieu chu nhiem\.gemini\antigravity\brain\8f90ef09-ad38-4e16-9795-e468108de7e4\BAO_CAO_LOI_GIAO_DIEN_SANG_10_11.md`
* **Lệnh tắt máy**: Đã thiết lập lệnh tắt máy tính tự động `shutdown.exe /s /t 120`.
