# 💎 ĐẶC TẢ TÍNH NĂNG NGHIỆP VỤ (BUSINESS FEATURES SPECIFICATION)

> **Lĩnh Vực**: Nền Tảng Thương Mại Điện Tử Nước Hoa Xa Xỉ (Haute Parfumerie)  
> **Trải Nghiệm Khách Hàng**: Tinh tế, Sang trọng, Đậm chất nghệ thuật khứu giác

---

## 1. Danh Mục Nước Hoa & Kim Tự Tháp 3 Tầng Hương (Scent Pyramid)

Mỗi chai nước hoa không đơn thuần là một món hàng, mà là một tác phẩm nghệ thuật khứu giác được cấu trúc gồm:

### 1.1. Phân Tầng Nồng Độ Tinh Dầu (Fragrance Concentrations)
- **Extrait de Parfum (20% - 40%)**: Đậm đặc nhất, lưu hương 12 - 24 giờ trên da.
- **Eau de Parfum - EDP (15% - 20%)**: Phổ biến và cân bằng hoàn hảo, lưu hương 8 - 12 giờ.
- **Eau de Toilette - EDT (5% - 15%)**: Tươi mát, thanh lịch ban ngày, lưu hương 4 - 6 giờ.
- **Eau de Cologne - EDC (2% - 4%)**: Nhẹ nhàng, sảng khoái sau khi tắm.

### 1.2. Kim Tự Tháp 3 Tầng Hương Tương Tác
```
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
- Trên giao diện [Trang Chi Tiết Sản Phẩm](/catalog/detail/{slug}), khách hàng có thể bấm tương tác vào từng tầng hương để xem hình ảnh và mô tả cảm xúc của từng nốt hương thành phần.

---

## 2. Trắc Nghiệm Mùi Hương Thông Minh (AI Fragrance Finder Quiz)

- **Đường dẫn**: `/catalog/quiz`
- **Mục tiêu**: Giúp khách hàng tìm ra chai nước hoa "chân ái" (Signature Scent) chỉ trong 3 phút mà không cần đến trực tiếp quầy thử.
- **Quy trình trắc nghiệm 6 khía cạnh khứu giác**:
  1. **Không gian & Thời điểm sử dụng**: Công sở thanh lịch, Tiệc đêm sang trọng, Hẹn hò lãng mạn, hay Dạo phố thư giãn.
  2. **Thời tiết & Mùa trong năm**: Xuân hè tươi mát hay Thu đông ấm áp.
  3. **Nhóm hương mong muốn**: Gỗ trầm ấm, Hoa cỏ quyến rũ, Trái cây mọng nước, hay Thảo mộc tươi xanh.
  4. **Cường độ tỏa hương (Sillage)**: Thoang thoảng tinh tế hay Nổi bật chiếm trọn không gian.
  5. **Độ lưu hương kỳ vọng (Longevity)**: Vừa phải trong buổi họp hay Bền bỉ suốt ngày dài.
  6. **Độ tuổi & Phong cách sống**: Cổ điển quý phái, Tối giản hiện đại, hay Phá cách độc bản.
- **Thuật toán Matching Engine**:
  - Đối chiếu vector thuộc tính câu trả lời với bảng `FragranceFamilies` và ma trận nốt hương `ProductFragranceNotes`.
  - Tính điểm tương thích phần trăm (Compatibility Match Score từ 85% đến 99%).
  - Trả về top 3 chai nước hoa hoàn hảo nhất kèm lý do gợi ý và nút mua ngay.

---

## 3. Động Cơ Giỏ Hàng & Mã Khuyến Mãi (Cart & Voucher Engine)

### 3.1. Mini-Cart Drawer
- Giỏ hàng trượt từ cạnh phải màn hình với animation mượt mà.
- Thao tác Thêm vào giỏ, Tăng/Giảm số lượng, Xóa sản phẩm được xử lý tức thì không cần reload trang.
- Tự động kiểm tra số lượng tồn kho còn lại của phiên bản trước khi cho phép tăng số lượng.

### 3.2. Voucher Validation Engine
- Kiểm tra tính hợp lệ đa điều kiện:
  1. Mã tồn tại và đang kích hoạt (`IsActive = true`).
  2. Thời gian nằm trong khoảng `ValidFrom <= UtcNow <= ValidTo`.
  3. Số lượt đã dùng chưa vượt quá giới hạn (`UsedCount < UsageLimit`).
  4. Tổng giá trị đơn hàng đạt mức tối thiểu (`SubTotal >= MinOrderAmount`).
- Hỗ trợ cả 2 phương thức chiết khấu:
  - **Giảm theo phần trăm**: `Discount = SubTotal * Percent%` (có chặn mức trần `MaxDiscountAmount`).
  - **Giảm số tiền cố định**: `Discount = FixedAmount`.

---

## 4. Cổng Thanh Toán & Quy Trình Checkout (Multi-Payment)

Hỗ trợ 4 phương thức thanh toán linh hoạt:
1. **COD (Giao hàng thu tiền mặt tận nơi)**: Khách hàng kiểm tra sản phẩm trước khi thanh toán cho shipper.
2. **VNPAY QR**: Quét mã QR qua ứng dụng ngân hàng hoặc ví VNPAY.
3. **Chuyển khoản ngân hàng trực tiếp**: Hiển thị thông tin tài khoản ngân hàng và mã nội dung chuyển khoản tự động theo mã đơn.
4. **Ví điện tử MoMo**: Thanh toán một chạm qua ví MoMo.

---

## 5. Tạp Chí Haute Parfumerie & Giáo Dục Khứu Giác (Editorial Blog)

- **Đường dẫn**: `/blog`
- Cung cấp kho tàng 12+ bài viết chuyên sâu về:
  - Lịch sử ngành công nghiệp nước hoa Pháp và Ý.
  - Nghệ thuật kết hợp mùi hương (Layering Fragrances).
  - Hướng dẫn bảo quản nước hoa không bị biến mùi trong khí hậu nhiệt đới.
  - Phân biệt nước hoa Niche độc bản và Designer đại chúng.
- Tích hợp chuẩn SEO `Schema.org/BlogPosting` và thẻ OpenGraph chia sẻ mạng xã hội.

---

## 6. Hệ Thống Đánh Giá & Xếp Hạng (Reviews & Moderation)

- Chỉ khách hàng đã đăng nhập và mua sản phẩm mới được cấp huy hiệu **[Người Mua Đã Xác Minh]**.
- Đánh giá từ 1 đến 5 sao kèm bình luận chi tiết về độ bám tỏa và cảm nhận mùi hương.
- Quy trình kiểm duyệt 2 bước tại phân hệ Quản lý cửa hàng trước khi xuất hiện trên trang web.
