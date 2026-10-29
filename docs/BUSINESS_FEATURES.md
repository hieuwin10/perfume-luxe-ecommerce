# 💎 ĐẶC TẢ TÍNH NĂNG NGHIỆP VỤ (BUSINESS FEATURES SPECIFICATION)

> **Lĩnh Vực**: Nền Tảng Thương Mại Điện Tử Nước Hoa Xa Xỉ (Haute Parfumerie)  
> **Trải Nghiệm Khách Hàng**: Tinh tế, Sang trọng, Đậm chất nghệ thuật khứu giác  
> **Chuẩn Hóa Kỹ Thuật**: BMAD Framework - Pillar B (Business)  

---

## 1. Danh Mục Nước Hoa & Kim Tự Tháp 3 Tầng Hương (Scent Pyramid)

Mỗi chai nước hoa không đơn thuần là một món hàng, mà là một tác phẩm nghệ thuật khứu giác được cấu trúc gồm:

### 1.1. Phân Tầng Nồng Độ Tinh Dầu (Fragrance Concentrations)
- **Extrait de Parfum (20% - 40%)**: Đậm đặc nhất, lưu hương 12 - 24 giờ trên da.
- **Eau de Parfum - EDP (15% - 20%)**: Phổ biến và cân bằng hoàn hảo, lưu hương 8 - 12 giờ.
- **Eau de Toilette - EDT (5% - 15%)**: Tươi mát, thanh lịch ban ngày, lưu hương 4 - 6 giờ.
- **Eau de Cologne - EDC (2% - 4%)**: Nhẹ nhàng, sảng khoái sau khi tắm.

### 1.2. Kim Tự Tháp 3 Tầng Hương Tương Tác
```text
                  /\
                 /  \      HƯƠNG ĐẦU (Top Notes): 15 Phút Đầu
                /    \     (Cam Bergamot, Quả mọng, Tiêu hồng...)
               /------\
              /        \   HƯƠNG GIỮA (Heart Notes): 3 - 5 Giờ Tiếp Theo
             /          \  (Hoa hồng Thổ Nhĩ Kỳ, Nhài Sambac, Diên vĩ...)
            /------------\
           /              \ HƯƠNG CUỐI (Base Notes): 8 - 12 Giờ Bền Bỉ
          /                \(Gỗ đàn hương, Hổ phách, Xạ hương trắng, Vani...)
         +------------------+
```
- Trên giao diện [Trang Chi Tiết Sản Phẩm](/nuoc-hoa/{slug}), khách hàng có thể bấm tương tác vào từng tầng hương để xem hình ảnh và mô tả cảm xúc của từng nốt hương thành phần.

---

## 2. Trắc Nghiệm Mùi Hương Thông Minh Bằng AI (AI Fragrance Finder Quiz)

- **Đường dẫn**: `/catalog/quiz`
- **Mục tiêu**: Giúp khách hàng tìm ra chai nước hoa "chân ái" (Signature Scent) chỉ trong 3 phút mà không cần đến trực tiếp quầy thử.
- **Quy trình trắc nghiệm ma trận 4 chiều**:
  1. **Giới tính & Phong cách cá nhân**: Men, Women, Unisex.
  2. **Không gian & Thời điểm sử dụng**: Công sở thanh lịch, Tiệc đêm sang trọng, Hẹn hò lãng mạn, hay Dạo phố thư giãn.
  3. **Cường độ tỏa hương (Sillage)**: Thoang thoảng tinh tế, Vừa phải hay Nổi bật chiếm trọn không gian.
  4. **Nhóm nốt hương mong muốn**: Gỗ trầm ấm, Hoa cỏ quyến rũ, Trái cây mọng nước, hay Phương Đông bí ẩn.
- **Thuật toán Matching Engine**:
  - Đối chiếu vector thuộc tính câu trả lời với bảng `FragranceFamilies` và ma trận nốt hương `ProductFragranceNotes`.
  - Tính điểm tương thích phần trăm (Compatibility Match Score từ 85% đến 99%).
  - Gán danh hiệu mùi hương độc bản (*The Sovereign Aristocrat*, *The Velvet Enchantress*, *The Ethereal Luminary*...).
  - Trả về Top 4 chai nước hoa hoàn hảo nhất kèm lý do gợi ý và nút mua ngay.

---

## 3. Tùy Biến Hộp Quà Khám Phá (Discovery Box Builder)

- **Đường dẫn**: `/discovery-box`
- **Mục tiêu**: Cho phép khách hàng tự phối hợp từ 3 đến 5 ống chiết VIP (dung tích 2ml hoặc 5ml) để thẩm định mùi hương hoặc làm quà tặng cao cấp.
- **Tùy biến bao bì hoàng gia**:
  1. *Hộp Gỗ Sơn Mài Hoàng Gia* (ClassicWood - 250.000₫): Chế tác từ gỗ óc chó phủ sơn mài bóng, lót lụa tơ tằm vàng kim.
  2. *Hộp Nhung Đen Huyền Bí* (RoyalVelvet - 200.000₫): Hộp nhung đen tuyền thắt nơ ruy-băng mạ vàng 24K.
  3. *Túi Da Thủ Công Viền Vàng* (ArtisanLeather - 180.000₫): Bao da thủ công khâu tay tinh xảo, tiện lợi du lịch.
- **Cá nhân hóa thiệp ép kim**: Khách hàng nhập lời chúc và chọn 1 trong 3 font chữ thanh lịch (*Playfair Display*, *Dancing Script*, *Cormorant Garamond*).
- **Tính giá động**: Tự động tính giá ống mẫu dựa theo tỷ lệ dung tích so với giá gốc chai fullbox.

---

## 4. Chương Trình Khách Hàng Thân Thiết VIP (Loyalty Engine)

- Tự động đồng bộ và thăng hạng dựa trên tổng chi tiêu thực tế tích lũy từ các đơn hàng giao thành công hoặc đã thanh toán:
  - **Bronze (Đồng)**: Hạng khởi đầu (< 5.000.000 VNĐ).
  - **Silver (Bạc)**: Chi tiêu ≥ 5.000.000 VNĐ (Tự động giảm 3% mọi đơn hàng).
  - **Gold (Vàng)**: Chi tiêu ≥ 15.000.000 VNĐ (Tự động giảm 7% mọi đơn hàng).
  - **Diamond (Kim Cương)**: Chi tiêu ≥ 30.000.000 VNĐ (Tự động giảm 12% tối đa).
- **Điểm thưởng tích lũy**: Mỗi 10.000 VNĐ chi tiêu quy đổi thành 1 điểm thưởng.
- **Giao diện trực quan**: Hiển thị thanh tiến trình thăng hạng (Progress Bar %) và số tiền còn thiếu để đạt hạng kế tiếp.

---

## 5. Sự Kiện Flash Sale Giờ Vàng Độc Bản

- **Đặc điểm**: Chiến dịch ưu đãi giới hạn thời gian thực đếm ngược theo giây UTC (`StartTime <= now < EndTime`).
- **Kiểm soát tồn kho nghiêm ngặt**: Mỗi sản phẩm trong Flash Sale có hạn mức bán độc lập (`StockLimit`), tự động chặn đặt hàng khi đã bán hết suất (`SoldQuantity >= StockLimit`) mà không ảnh hưởng đến tồn kho catalog gốc.

---

## 6. Động Cơ Giỏ Hàng Kép & Mã Khuyến Mãi (Cart & Voucher Engine)

### 6.1. Giỏ Hàng Kép (Dual Cart Architecture)
- **Khách vãng lai**: Sử dụng token ẩn danh SHA-256 an toàn lưu trong Cookie 30 ngày.
- **Thành viên**: Giỏ hàng lưu bền vững theo `UserId`.
- **Hợp nhất giỏ (Cart Merge)**: Khi khách đăng nhập hoặc đăng ký, toàn bộ món hàng từ giỏ khách vãng lai được tự động gộp vào tài khoản (`MergeGuestCartAsync`).

### 6.2. Voucher Validation Engine
- Kiểm tra tính hợp lệ đa điều kiện:
  1. Mã tồn tại và đang kích hoạt (`IsActive = true`).
  2. Thời gian nằm trong khoảng `ValidFrom <= UtcNow <= ValidTo`.
  3. Số lượt đã dùng chưa vượt quá giới hạn (`UsedCount < UsageLimit`).
  4. Tổng giá trị đơn hàng đạt mức tối thiểu (`SubTotal >= MinOrderAmount`).
- Hỗ trợ chiết khấu theo phần trăm (`Discount = SubTotal * Percent%` có chặn mức trần) hoặc số tiền cố định.

---

## 7. Cổng Thanh Toán & Quy Trình Checkout (Multi-Payment)

Hỗ trợ các phương thức thanh toán linh hoạt:
1. **COD (Giao hàng thu tiền mặt tận nơi)**: Khách hàng kiểm tra sản phẩm trước khi thanh toán cho shipper.
2. **VietQR (Chuyển khoản tự động)**: Tự động sinh mã Vietcombank QR Code chứa chính xác số tiền cần trả và mã đơn hàng làm nội dung chuyển khoản.
3. **VNPAY QR & Ví MoMo**: Sẵn sàng tích hợp cổng thanh toán trực tuyến.

---

## 8. Tra Cứu Đơn Hàng Realtime Không Cần Đăng Nhập

- **Đường dẫn**: `/tra-cuu-don-hang`
- Khách hàng chỉ cần nhập mã đơn (`LUXE-YYYYMMDD-...`) và số điện thoại đặt hàng để kiểm tra tiến trình 5 giai đoạn: *Chờ Xác Nhận → Đã Xác Nhận → Đang Vận Chuyển → Đã Giao → Hoàn Tất*.

---

## 9. Tạp Chí Haute Parfumerie & Giáo Dục Khứu Giác (Editorial Blog)

- **Đường dẫn**: `/blog`
- Cung cấp kho tàng 12+ bài viết chuyên sâu chuẩn SEO (tích hợp tự động qua Migration V5) về nghệ thuật Layering, lịch sử nước hoa Pháp/Ý, xu hướng nước hoa Niche 2026.

---

## 10. Hệ Thống Đánh Giá & Xếp Hạng (Reviews & Moderation)

- Chỉ khách hàng đã mua sản phẩm mới được cấp huy hiệu **[Người Mua Đã Xác Minh]**.
- Đánh giá từ 1 đến 5 sao kèm bình luận chi tiết về độ bám tỏa và cảm nhận mùi hương.
- Quy trình kiểm duyệt 2 bước tại phân hệ Quản lý cửa hàng trước khi xuất hiện trên trang web.

---

> [!NOTE]
> Toàn bộ các tính năng nghiệp vụ trên được tích hợp và quy chiếu trong hồ sơ **BMAD Pillar B** tại [`docs/bmad/BUSINESS.md`](./bmad/BUSINESS.md).
