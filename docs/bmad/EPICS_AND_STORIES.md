# BMAD: EPICS & USER STORIES

> **Phân hệ:** Quản Lý Yêu Cầu & Kế Hoạch Chức Năng  
> **Dự án:** Perfume Luxe  
> **Trạng thái:** Hoàn tất triển khai & Đạt chuẩn kiểm thử (100% Done)  

---

## 1. Bảng Phân Rã Epics Cốt Lõi

| Epic ID | Tên Epic | Mô Tả Nghiệp Vụ | Trạng Thái |
|---|---|---|---|
| **EPC-01** | **Multi-Faceted Catalog** | Tìm kiếm đa chiều theo bộ lọc nốt hương, thương hiệu, nhóm hương, nồng độ, giá tiền và cache taxonomy. | ✅ Hoàn thành |
| **EPC-02** | **AI Scent Quiz** | Trắc nghiệm 4 bước khám phá cá tính mùi hương, thuật toán chấm điểm ma trận và sinh danh hiệu Scent Archetype. | ✅ Hoàn thành |
| **EPC-03** | **Discovery Box Experience** | Hộp quà tự chọn 3-5 ống chiết (2ml/5ml), tùy biến kiểu hộp hoàng gia và in thiệp ép kim cá nhân hóa. | ✅ Hoàn thành |
| **EPC-04** | **Flash Sale Engine** | Chiến dịch giờ vàng đếm ngược chính xác, quản lý hạn mức tồn kho riêng biệt chống bán vượt. | ✅ Hoàn thành |
| **EPC-05** | **Cart & Checkout Lifecycle** | Giỏ hàng kép (Guest cookie SHA-256 + User account), tự động gộp giỏ, giao dịch đặt hàng Serializable. | ✅ Hoàn thành |
| **EPC-06** | **VIP Loyalty Engine** | 4 hạng thẻ thành viên (Bronze, Silver, Gold, Diamond), tự động thăng hạng theo chi tiêu và tích điểm. | ✅ Hoàn thành |
| **EPC-07** | **Store Operations & Analytics** | Báo cáo tài chính P&L, biên lợi nhuận, theo dõi giá vốn COGS và quản lý điều chỉnh tồn kho. | ✅ Hoàn thành |
| **EPC-08** | **Database Schema Evolution** | Động cơ nâng cấp Schema tự động V1 đến V6, giao dịch SQL Transaction có rollback và B-Tree Indexing. | ✅ Hoàn thành |

---

## 2. Danh Sách User Stories Trọng Tâm

### US-CAT-01: Lọc Nước Hoa Đa Chiều
- **Là một**: Khách hàng mua sắm.
- **Tôi muốn**: Lọc sản phẩm theo thương hiệu, nhóm hương, nồng độ và khoảng giá.
- **Để**: Tìm được đúng chai nước hoa phù hợp với sở thích của mình.
- **Tiêu chuẩn nghiệm thu (AC)**: Kết quả lọc trả về dưới 150ms, hiển thị đúng số lượng và phân trang chính xác.

### US-QUIZ-01: Trắc Nghiệm Mùi Hương Thông Minh
- **Là một**: Khách hàng mới chưa định hình được mùi hương yêu thích.
- **Tôi muốn**: Tham gia trắc nghiệm nhanh 4 câu hỏi.
- **Để**: Nhận được hồ sơ cá tính Scent Archetype và top 4 sản phẩm đề xuất phù hợp nhất.
- **Tiêu chuẩn nghiệm thu (AC)**: Match score chuẩn hóa từ 85% - 99%, có nút "Thêm vào giỏ" trực tiếp từ kết quả.

### US-DISC-01: Tùy Biến Hộp Quà Discovery Box
- **Là một**: Khách hàng muốn trải nghiệm nhiều mùi hương hoặc làm quà tặng.
- **Tôi muốn**: Tự chọn 3-5 mẫu chiết (2ml hoặc 5ml), chọn kiểu hộp và nhập lời chúc in thiệp ép kim.
- **Để**: Có một hộp quà độc bản cao cấp cho riêng mình hoặc bạn bè.
- **Tiêu chuẩn nghiệm thu (AC)**: Đơn giá được tính đúng tỷ lệ dung tích, thông tin thiệp được lưu trữ chuẩn JSON trong giỏ hàng.

### US-CHCK-01: Đặt Hàng Giao Dịch An Toàn
- **Là một**: Khách hàng thanh toán đơn hàng.
- **Tôi muốn**: Được áp dụng mã giảm giá voucher, chọn thanh toán COD hoặc VietQR.
- **Để**: Đặt hàng an toàn, không bị tình trạng hết hàng giữa chừng.
- **Tiêu chuẩn nghiệm thu (AC)**: Thao tác trừ tồn kho được bảo vệ bằng Database Transaction mức Serializable, có mã tra cứu vận đơn ngay sau khi hoàn tất.
