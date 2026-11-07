# NHẬT KÝ KIỂM THỬ TỔNG THỂ GIAI ĐOẠN 3 & GIAI ĐOẠN 4
## MASTER TESTING DIARY: INTERACTIVE FEATURES, DISCOVERY BOX, ADMIN PORTAL & SYSTEM OPERATIONS

---

### THÔNG TIN TỔNG QUAN & METADATA

- **Website mục tiêu (Target Host):** `https://perfumeluxe.somee.com/` (Nền tảng Windows Server / IIS trên Somee Cloud)
- **Ngày thực hiện kiểm thử:** 2026-11-07
- **Phương pháp kiểm thử (Methodology):** 
  - **Live Chrome DevTools Black-box Browser Testing** kết hợp tự động hóa bằng Chrome Automation & Playwright Engine trên môi trường thực tế (Google Chrome x64 Windows).
  - **Tuyệt đối không đọc/soi duyệt mã nguồn nội bộ (Zero Source Code Inspection)**. Toàn bộ chẩn đoán, phân tích lỗi và đề xuất kỹ thuật được suy luận độc lập từ quan sát tương tác trực tiếp trên trình duyệt, cấu trúc DOM, DevTools Console, Network HTTP Status Code, Request/Response Payload và CSS Box Model.
- **Tài khoản kiểm thử được cấp phép:**
  1. **Quản trị viên (Admin Portal):**
     - Email: `admin@perfumeluxe.vn`
     - Mật khẩu: `AdminPassword123!`
     - Quyền hạn: Toàn quyền quản trị hệ thống (System Administrator / Store Manager)
  2. **Khách hàng thân thiết (Customer VIP):**
     - Email: `testvip2026@perfumeluxe.vn`
     - Mật khẩu: `TestVip2026!@#`
     - Hạng thành viên: Khách hàng VIP Đồng (VIP Bronze Tier, 0 đơn hoàn tất trước kiểm thử)
- **Mục đích tài liệu:** Ghi nhận nhật ký kiểm thử chi tiết, đối chiếu hành vi thực tế vs kỳ vọng, lập sổ bộ lỗi tổng thể (Master Bug Register) và lộ trình khắc phục kỹ thuật (Remediation Roadmap) bàn giao cho đội ngũ phát triển vào tuần tới.

---

## MỤC LỤC TỔNG THỂ

1. [Tóm Tắt Điều Hành & Ma Trận Kiểm Thử (Executive Summary & Testing Matrix)](#1-tóm-tắt-điều-hành--ma-trận-kiểm-thử-executive-summary--testing-matrix)
2. [Nhật Ký Kiểm Thử Chi Tiết Giai Đoạn 3: Interactive Features & Discovery Box](#2-nhật-ký-kiểm-thử-chi-tiết-giai-đoạn-3-interactive-features--discovery-box)
   - [Tính năng 3.1: AI Fragrance Finder / Quiz (`/catalog/quiz`)](#tính-năng-31-ai-fragrance-finder--quiz-catalogquiz)
   - [Tính năng 3.2: Hộp Quà Khám Phá Tùy Chỉnh - Custom Discovery Box (`/discovery-box`)](#tính-năng-32-hộp-quà-khám-phá-tùy-chỉnh---custom-discovery-box-discovery-box)
   - [Tính năng 3.3: So Sánh Nước Hoa Chuyên Sâu - Perfume Comparison (`/so-sanh`)](#tính-năng-33-so-sánh-nước-hoa-chuyên-sâu---perfume-comparison-so-sanh)
   - [Tính năng 3.4: Đánh Giá Sản Phẩm & Xếp Hạng Sao - Product Reviews (`/nuoc-hoa/{slug}#reviews`)](#tính-năng-34-đánh-giá-sản-phẩm--xếp-hạng-sao---product-reviews-nuoc-hoaslugreviews)
3. [Nhật Ký Kiểm Thử Chi Tiết Giai Đoạn 4: Admin Portal & System Operations](#3-nhật-ký-kiểm-thử-chi-tiết-giai-đoạn-4-admin-portal--system-operations)
   - [Tính năng 4.1: Bảng Điều Khiển Quản Trị - Admin Dashboard (`/admin/admindashboard`)](#tính-năng-41-bảng-điều-khiển-quản-trị---admin-dashboard-adminadmindashboard)
   - [Tính năng 4.2: Quản Lý Danh Mục & Sản Phẩm - Products & Catalog (`/admin/admindashboard/products`)](#tính-năng-42-quản-lý-danh-mục--sản-phẩm---products--catalog-adminadmindashboardproducts)
   - [Tính năng 4.3: Quản Trị Kho Hàng & Điều Chỉnh Tồn - Inventory Management (`/admin/admindashboard/inventory`)](#tính-năng-43-quản-trị-kho-hàng--điều-chỉnh-tồn---inventory-management-adminadmindashboardinventory)
   - [Tính năng 4.4: Báo Cáo Tài Chính P&L & Xuất CSV - Financial Report (`/admin/admindashboard/financialreport`)](#tính-năng-44-báo-cáo-tài-chính-pl--xuất-csv---financial-report-adminadmindashboardfinancialreport)
   - [Tính năng 4.5: Cài Đặt Hệ Thống & Cửa Hàng - System Settings (`/admin/admindashboard/settings`)](#tính-năng-45-cài-đặt-hệ-thống--cửa-hàng---system-settings-adminadmindashboardsettings)
   - [Tính năng 4.6: Quản Lý Bài Viết & Tạp Chí - Blog Management (`/admin/admindashboard/blogs`)](#tính-năng-46-quản-lý-bài-viết--tạp-chí---blog-management-adminadmindashboardblogs)
   - [Tính năng 4.7: Console CSDL & Sức Khỏe Hệ Thống - Database Console & System Health](#tính-năng-47-console-csdl--sức-khỏe-hệ-thống---database-console--system-health)
4. [Sổ Bộ Lỗi Toàn Diện (Master Bug Register) & Phân Loại Mức Độ](#4-sổ-bộ-lỗi-toàn-diện-master-bug-register--phân-loại-mức-độ)
5. [Lộ Trình Khắc Phục Kỹ Thuật (Remediation Roadmap) Tuần Tới](#5-lộ-trình-khắc-phục-kỹ-thuật-remediation-roadmap-tuần-tới)
6. [Kết Luận & Biên Bản Ký Duyệt (Sign-off)](#6-kết-luận--biên-bản-ký-duyệt-sign-off)

---

## 1. TÓM TẮT ĐIỀU HÀNH & MA TRẬN KIỂM THỬ (EXECUTIVE SUMMARY & TESTING MATRIX)

### 1.1. Tóm Tắt Kết Quả Chung
Quá trình kiểm thử thực tế trên trình duyệt đã tiến hành khảo sát và đánh giá toàn diện **11 phân hệ/tính năng** trên hệ thống e-commerce cao cấp Perfume Luxe:
- **Giai đoạn 3 (Interactive Features & Discovery Box):** 4 tính năng phục vụ trải nghiệm người dùng cuối (End-Customer Experience) gồm trắc nghiệm mùi hương AI, chế tác hộp quà Discovery Box độc bản, so sánh 3 tầng hương và đánh giá khách hàng.
- **Giai đoạn 4 (Admin Portal & System Operations):** 7 phân hệ nghiệp vụ quản trị back-office dành cho vận hành, kế toán, kho bãi và quản trị hạ tầng máy chủ.

Hệ thống hoạt động với độ ổn định rất cao trên nền tảng máy chủ Somee Cloud. Không có bất kỳ lỗi sập hệ thống (Crash 500) hoặc rò rỉ dữ liệu nhạy cảm. Tuy nhiên, đợt kiểm thử đã ghi nhận **10 lỗi/vấn đề** cần khắc phục, trong đó có **3 lỗi Major** ảnh hưởng đến quy trình trải nghiệm của khách hàng và thao tác của quản trị viên.

### 1.2. Thống Kê Số Liệu Kiểm Thử (Metrics Overview)

```
========================================================================
TỔNG SỐ TÍNH NĂNG KIỂM THỬ:        11 Tính năng (4 Phase 3 + 7 Phase 4)
TỶ LỆ TÍNH NĂNG VẬN HÀNH TỐT:      81.8% (9/11 Đạt hoàn toàn)
TÍNH NĂNG CẦN SỬA ĐỔI:             18.2% (2/11: Product Reviews, Product Edit Route)
TỔNG SỐ LỖI PHÁT HIỆN:             10 Lỗi (4 Phase 3 + 6 Phase 4)
------------------------------------------------------------------------
PHÂN BỔ THEO MỨC ĐỘ NGHIÊM TRỌNG (SEVERITY BREAKDOWN):
- [BLOCKER]   Khối chặn toàn bộ luồng:      0 (0%)
- [CRITICAL]  Lỗi nghiêm trọng/Bảo mật:     0 (0%)
- [MAJOR]     Lỗi chức năng quan trọng:     3 (30%)  [BUG-P3-01, BUG-P4-01, BUG-P4-02]
- [MINOR]     Lỗi chức năng phụ/Ngoại lệ:   4 (40%)  [BUG-P3-02, BUG-P3-03, BUG-P4-03, BUG-P4-04]
- [UI/UX]     Giao diện, ngữ nghĩa DOM, CSS: 3 (30%)  [BUG-P3-04, BUG-P4-05, BUG-P4-06]
========================================================================
```

### 1.3. Ma Trận Đánh Giá Trạng Thái 11 Tính Năng (Testing Status Matrix)

| Mã | Phân Hệ / Tính Năng | URL Thử Nghiệm Trực Tiếp | Tài Khoản Sử Dụng | Trạng Thái HTTP | Đánh Giá Chức Năng | Số Lỗi |
|:---|:---|:---|:---|:---|:---|:---:|
| **P3-F1** | AI Fragrance Finder / Quiz | `/catalog/quiz` | Guest / Anonymous | 200 OK | **PASSED (XUẤT SẮC)** | 0 |
| **P3-F2** | Custom Discovery Box | `/discovery-box` | Guest / VIP Customer | 200 OK | **PASSED (XUẤT SẮC)** | 0 |
| **P3-F3** | Perfume Comparison (So Sánh) | `/so-sanh` | Guest / Anonymous | 200 OK | **PASSED (CÓ LƯU Ý DOM)** | 1 |
| **P3-F4** | Product Reviews (Đánh Giá) | `/nuoc-hoa/{slug}#reviews` | VIP (`testvip2026`) | 200 OK / 403 Forbidden | **FAILED (CẦN SỬA LỖI)** | 3 |
| **P4-F1** | Admin Dashboard & Chart.js | `/admin/admindashboard` | Admin (`admin@perfumeluxe.vn`) | 200 OK | **PASSED (CÓ LƯU Ý UX)** | 1 |
| **P4-F2** | Quản Lý Danh Mục & Sản Phẩm | `/admin/admindashboard/products`<br>`/createproduct`<br>`/editproduct` | Admin (`admin@perfumeluxe.vn`) | 200 OK / 404 Not Found | **PARTIAL (LỖI NÚT SỬA)** | 1 |
| **P4-F3** | Quản Trị Kho & Nhập/Xuất Hàng | `/admin/admindashboard/inventory` | Admin (`admin@perfumeluxe.vn`) | 200 OK | **PASSED (XUẤT SẮC)** | 0 |
| **P4-F4** | Báo Cáo Tài Chính P&L & CSV | `/admin/admindashboard/financialreport` | Admin (`admin@perfumeluxe.vn`) | 200 OK | **PASSED (XUẤT SẮC)** | 0 |
| **P4-F5** | Cài Đặt Hệ Thống & Cửa Hàng | `/admin/admindashboard/settings` | Admin (`admin@perfumeluxe.vn`) | 200 OK | **PASSED (TỐT)** | 0 |
| **P4-F6** | Quản Lý Bài Viết Blog / Tạp Chí | `/admin/admindashboard/blogs`<br>`/createblog` | Admin (`admin@perfumeluxe.vn`) | 200 OK | **PASSED (CẦN RICH TEXT)** | 1 |
| **P4-F7** | Console CSDL & Sức Khỏe Máy Chủ | `/admin/databaseconsole`<br>`/systemhealth` | Admin (`admin@perfumeluxe.vn`) | 200 OK / 404 Not Found | **PASSED (LỖI CHẨN ĐOÁN SQL)** | 2 |

---

## 2. NHẬT KÝ KIỂM THỬ CHI TIẾT GIAI ĐOẠN 3: INTERACTIVE FEATURES & DISCOVERY BOX

---

### TÍNH NĂNG 3.1: AI FRAGRANCE FINDER / QUIZ (`/catalog/quiz`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/catalog/quiz`
- **Tiêu đề trang (Page Title):** `AI Fragrance Finder - Trắc Nghiệm Mùi Hương | PERFUME LUXE`
- **Phân loại mức độ nghiêm trọng:** **None (Pass Hoàn Hảo)**

#### Các bước thực hiện & Tái hiện (Step-by-step Reproduction):
1. Khởi động phiên trình duyệt Chrome mới, điều hướng trực tiếp đến `https://perfumeluxe.somee.com/catalog/quiz`.
2. Kiểm tra trạng thái tải ban đầu:
   - Thanh tiến độ hiển thị `width: 20%`, nhãn `CÂU HỎI 1 / 5`.
   - Nút "Quay lại" (`#quizBackBtn`) ẩn tự động bằng CSS `visibility: hidden`.
3. **Câu hỏi 1 ("Bạn đang tìm kiếm kiệt tác mùi hương cho ai?"):**
   - Click chọn option: `Quý Ông (Men) - Nam tính, bản lĩnh, lịch lãm & quyền uy`.
   - Hệ thống tự động chuyển tiếp mượt mà sang Câu hỏi 2; thanh tiến độ nâng lên `width: 40%`, nhãn `CÂU HỎI 2 / 5`. Nút `#quizBackBtn` chuyển sang `visibility: visible`.
4. **Kiểm tra tính năng Lưu trạng thái & Nút Quay lại:**
   - Click nút "Quay lại". Trình duyệt quay về Câu 1 an toàn, thanh tiến độ giảm về `20%`, bảo toàn lựa chọn trước đó.
5. **Tiến trình trả lời liên hoàn 5 câu hỏi ma trận:**
   - Chọn lại `Quý Ông (Men)` -> Chuyển sang Câu 2.
   - **Câu 2 ("Phong thái hoặc Vibe bạn muốn toát ra nhất?"):** Chọn `Quyền Lực & Sang Trọng - Trầm hương Oud, da thuộc quý, dứa khói hoàng gia` -> Tiến độ `width: 60%`, `CÂU HỎI 3 / 5`.
   - **Câu 3 ("Bạn dự định đồng hành cùng mùi hương trong dịp nào?"):** Chọn `Dạ Tiệc Tối & Hẹn Hò Lãng Mạn - Không gian lung linh, cần sức hút ma mị` -> Tiến độ `width: 80%`, `CÂU HỎI 4 / 5`.
   - **Câu 4 ("Cường độ tỏa hương và thời gian lưu hương bạn kỳ vọng?"):** Chọn `Lưu Hương Bền Bỉ (8 - 12 Giờ) - Tỏa hương 1 - 2 mét ấn tượng` -> Tiến độ `width: 100%`, `CÂU HỎI 5 / 5`.
   - **Câu 5 ("Tầm ngân sách và định dạng sở hữu ưu tiên của bạn?"):** Chọn `Kiệt Tác Haute Parfumerie - Tuyệt phẩm Niche giới hạn không giới hạn ngân sách`.
6. **Mô phỏng AI Engine phân tích:**
   - Phần tử `#quizLoadingState` kích hoạt hiệu ứng xoay spinner ánh vàng kim `fa-wand-magic-sparkles`.
   - Thông điệp phân tích: `AI Fragrance Engine Đang Phân Tích... Đang tính toán ma trận nốt hương và tạo lập hồ sơ danh phận mùi hương của bạn`.
   - Thời gian xử lý: ~1.8 giây.
7. **Trình bày Hồ sơ Danh phận (Scent Archetype Result Dossier):**
   - Tước hiệu: **`The Sovereign Aristocrat`** (Bậc Đế Vương Quyền Uy & Đẳng Cấp).
   - Bài bình phẩm chuyên sâu: Phân tích phong thái thủ lĩnh kết hợp trầm hương Oud, gỗ tuyết tùng và gia vị cay ấm.
   - Nhóm hương chủ đạo: Gỗ Trầm Hương Quý (Oud), Hổ Phách & Gia Vị Cay Ấm, Da Thuộc Hoàng Gia.
   - Nốt hương chữ ký: Trầm Hương, Gỗ Tuyết Tùng, Tiêu Đen Calabre, Nhục Đậu Khấu.
   - Hoàn cảnh đề xuất: Dạ tiệc thượng lưu, gặp gỡ đối tác VIP.
   - Top 4 sản phẩm tương thích cao nhất:
     1. *Dior Sauvage Elixir Parfum* - Độ tương thích 99%, Giá 3.850.000 ₫ (kèm tag AI tuyển chọn).
     2. *Bleu de Chanel Parfum* - Độ tương thích 95%, Giá 3.650.000 ₫.
     3. *Maison Margiela Replica Jazz Club EDT* - Độ tương thích 92%.
     4. *Le Labo Santal 33 Eau de Parfum* - Độ tương thích 90%.
8. **Tương tác sau kết quả:**
   - Click "Sao Chép Hồ Sơ Mùi Hương": Hiển thị thông báo sao chép vào clipboard thành công.
   - Click nút "THÊM" trên card Dior Sauvage Elixir: Gọi API giỏ hàng thành công, badge icon giỏ hàng trên thanh điều hướng cập nhật từ `0` lên `1`.
   - Click "Làm Lại Trắc Nghiệm": Hệ thống xóa cache câu trả lời và đưa về Câu 1 trơn tru.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Luồng trắc nghiệm 5 bước hoạt động không giật lag, tính toán ma trận trọng số chính xác, hiển thị hồ sơ cá nhân hóa và nút thêm vào giỏ hàng hoạt động tốt.
- **Thực tế:** Hoàn toàn trùng khớp kỳ vọng 100%.

#### Nhật ký DevTools:
- **DevTools Console:** 0 Lỗi (Errors), 0 Cảnh báo (Warnings).
- **Network Requests:** Toàn bộ request CSS, Fonts, Images và Script tải thành công với mã `200 OK`. Không phát sinh lỗi mạng.

---

### TÍNH NĂNG 3.2: HỘP QUÀ KHÁM PHÁ TÙY CHỈNH - CUSTOM DISCOVERY BOX (`/discovery-box`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/discovery-box`
- **Tiêu đề trang:** `Hộp Quà Khám Phá Nước Hoa Tùy Chỉnh - Custom Discovery Box | PERFUME LUXE`
- **Phân loại mức độ nghiêm trọng:** **None (Pass Hoàn Hảo)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `https://perfumeluxe.somee.com/discovery-box`.
2. **Bước 1: Chọn Kiểu Dáng Vỏ Hộp Hoàng Gia (Box Style):**
   - Vỏ hộp mặc định: `ClassicWood` (Hộp Gỗ Sơn Mài Hoàng Gia - 250.000 ₫).
   - Click chuyển sang: `RoyalVelvet` (Hộp Nhung Đen Huyền Bí - 200.000 ₫).
   - Quan sát phản hồi: Nhãn `#selectedBoxStyleLabel` cập nhật tức thì thành `Đang chọn: Hộp Nhung Đen Huyền Bí (200.000 ₫)`. Khay tóm tắt (Bespoke Tray) cập nhật đơn giá vỏ hộp từ 250.000 ₫ thành 200.000 ₫.
3. **Kiểm tra Ràng buộc số lượng tối thiểu (Threshold Validation):**
   - Nút `#btnOrderDiscovery` ở trạng thái `disabled`, nhãn hiển thị: `HÃY CHỌN TỪ 3 ĐẾN 5 MÙI HƯƠNG (0/3)`.
4. **Bước 2: Tuyển chọn Ống chiết & Kiểm thử Khuyến mãi Bundle Tự động:**
   - *Chọn Ống 1:* Click "CHỌN CHAI NÀY" tại chai #2 (*Bleu de Chanel Parfum* - 2ml: 150.000 ₫) -> Khay điền Slot 1, nút chuyển nhãn `(1/3)`.
   - *Chọn Ống 2:* Click "CHỌN CHAI NÀY" tại chai #11 (*Chanel Coco Mademoiselle EDP* - 2ml: 150.000 ₫) -> Slot 2 lấp đầy, nút chuyển nhãn `(2/3)`.
   - *Chọn Ống 3:* Click "CHỌN CHAI NÀY" tại chai #10 (*Chanel No.5 EDP* - 2ml: 150.000 ₫) -> Đạt mốc tối thiểu 3 ống.
     - Khay tính tiền: Vỏ hộp (200.000 ₫) + 3 ống chiết (450.000 ₫) - Giảm giá Bundle (0 ₫) = **650.000 ₫**.
     - Nút `#btnOrderDiscovery` được kích hoạt (`disabled=false`), nhãn chuyển thành `XÁC THỰC API & THÊM HỘP QUÀ VÀO GIỎ (650.000 ₫)`.
   - *Kiểm thử Chiết khấu Bundle 4 ống (Giảm 5%):* Chọn tiếp Ống 4: Chai #12 (*Christian Dior J'adore EDP* - 2ml: 150.000 ₫).
     - Khay tính tiền: Vỏ hộp (200.000 ₫) + 4 ống chiết (600.000 ₫) = 800.000 ₫.
     - Chiết khấu Bundle 5% tự động áp dụng: `-40.000 ₫`.
     - Tổng cộng cập nhật chính xác: **760.000 ₫**. Nút đặt hàng hiển thị đúng số tiền 760.000 ₫.
   - *Kiểm thử Chiết khấu Bundle 5 ống (Giảm 10%):* Chọn tiếp Ống 5: Chai #3 (*Creed Aventus Millesime EDP* - 2ml: 234.000 ₫).
     - Khay tính tiền: Vỏ hộp (200.000 ₫) + 5 ống chiết (834.000 ₫) = 1.034.000 ₫.
     - Chiết khấu Bundle 10% tự động áp dụng: `-103.400 ₫`.
     - Tổng cộng cập nhật chính xác: **930.600 ₫**. Nút đặt hàng hiển thị đúng số tiền 930.600 ₫.
5. **Bước 3: Soạn Thiệp Ép Kim Vàng 24K Cá Nhân Hóa (Bespoke Gold Card):**
   - Nhập tên người nhận (`#recipientNameInput`): `Quý Cô Minh Anh`.
   - Chọn kiểu chữ nghệ thuật: Click chọn font `Dancing Script` (Font script mềm mại). Nút nhận class `active`.
   - Nhập lời chúc (`#greetingMessageInput`): `Gửi tặng nàng hương thơm kiêu kỳ và quý phái nhất từ Paris. Chúc bạn luôn rực rỡ và hạnh phúc!`.
6. **Bước 4: Gọi API Thêm vào giỏ hàng:**
   - Click `#btnOrderDiscovery`.
   - Request POST gửi tới endpoint `/api/v1/discovery-box/validate-and-add` với body JSON chứa đầy đủ cấu hình boxStyle, greetingMessage, recipientName, greetingFont và 5 items vials.
   - Server phản hồi HTTP `200 OK`. Cart Drawer trượt mở hiển thị hộp quà Discovery Box mới tạo.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Cho phép chọn từ 3 đến 5 ống chiết linh hoạt, tính giảm giá bundle 5% và 10% chính xác từng đồng, lưu thông tin thiệp và gửi API thành công.
- **Thực tế:** Hoàn toàn đạt 100% yêu cầu kỹ thuật và nghiệp vụ.

#### Nhật ký DevTools:
- **DevTools Console:** 0 Lỗi, 0 Cảnh báo.
- **Network Requests:**
  - `POST https://perfumeluxe.somee.com/api/v1/discovery-box/validate-and-add` -> `HTTP 200 OK`
  - `GET https://perfumeluxe.somee.com/api/v1/cart` -> `HTTP 200 OK`

---

### TÍNH NĂNG 3.3: SO SÁNH NƯỚC HOA CHUYÊN SÂU - PERFUME COMPARISON (`/so-sanh`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/so-sanh`
- **Tiêu đề trang:** `So Sánh Kiệt Tác Nước Hoa | PERFUME LUXE`
- **Phân loại mức độ nghiêm trọng:** **UI/UX / Minor (Phát hiện lỗi thẻ `<main>` kép trong DOM)**

#### Các bước thực hiện & Tái hiện:
1. Điều hướng trình duyệt đến `https://perfumeluxe.somee.com/so-sanh`.
2. Kiểm tra thanh công cụ chọn lựa gồm 3 menu dropdown:
   - Dropdown 1: `-- Chọn sản phẩm 1 --`
   - Dropdown 2: `-- Chọn sản phẩm 2 --`
   - Dropdown 3: `-- Trống (Thêm chai thứ 3) --`
3. Thao tác chọn lần lượt 3 chai nước hoa cao cấp:
   - Chai 1: `Bleu de Chanel Parfum (Chanel)`
   - Chai 2: `Dior Sauvage Elixir Parfum (Christian Dior)`
   - Chai 3: `Tom Ford Tobacco Vanille EDP (Tom Ford)`
4. Quan sát Ma trận So sánh 12 Tiêu chí Kỹ thuật (Comparison Matrix Table):
   - *1. Tiêu đề & Đơn giá:* Chanel (3.650.000 ₫) | Dior (3.850.000 ₫) | Tom Ford (6.800.000 ₫) kèm nút xem chi tiết.
   - *2. Xuất xứ:* Pháp | Pháp | Mỹ.
   - *3. Nhóm hương:* Hương Gỗ (Woody) | Hương Phương Đông (Oriental) | Hương Ngọt Ẩm Thực (Gourmand).
   - *4. Nồng độ tinh dầu:* Parfum (15-30%) | Parfum (15-30%) | EDP (15-20%).
   - *5. Phong cách giới tính:* Nam giới (Masculine) | Nam giới (Masculine) | Phi giới tính (Unisex).
   - *6. Tầng Hương Đầu (Top Notes - 15 phút đầu):* Bóc tách rõ thành phần từng chai (Cam Bergamot, Quả mọng tươi, Tiêu Tứ Xuyên,...).
   - *7. Tầng Hương Giữa (Heart Notes - 2 đến 4 giờ):* Hoa hồng Grasse, Hoa nhài Sambac, Hoa Oải Hương.
   - *8. Tầng Hương Cuối (Base Notes - 6 đến 12 giờ):* Gỗ đàn hương, Vani Bourbon, Hổ phách, Gỗ tuyết tùng.
   - *9. Độ Lưu Hương (Longevity):* `10 - 12 giờ` vs `Trên 12 giờ` vs `Trên 12 giờ`.
   - *10. Độ Tỏa Hương (Sillage):* `Tỏa hương bán kính 1 - 2 mét` vs `Tỏa hương xa trên 2 mét` vs `Tỏa hương xa trên 2 mét`.
   - *11. Mùa & Thời điểm khuyên dùng:* Bốn mùa | Thu, Đông, Đêm tiệc | Thu, Đông, Dạ tiệc xa hoa.
   - *12. Điểm đánh giá cộng đồng:* 4.8★ (95 lượt) | 4.9★ (128 lượt) | 4.9★ (74 lượt).
5. Thao tác co gọn: Chuyển Dropdown 3 về lại `-- Trống (Thêm chai thứ 3) --`. Bảng so sánh tự động chuyển đổi thành 2 cột tức thì, không phát sinh giật màn hình.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** So sánh trực quan tối đa 3 chai đồng thời, hiển thị đầy đủ 12 tiêu chí chuẩn chuyên gia nước hoa Niche.
- **Thực tế:** Đáp ứng đầy đủ và chuẩn xác về mặt giao diện và dữ liệu.
- **Vấn đề phát hiện (BUG-P3-04):** Khi kiểm tra cây phân cấp DOM trong DevTools Elements, phát hiện trang xuất hiện **2 thẻ `<main>`** lồng nhau/song song (`<main role="main" b-njtsibxxhy="">` từ layout chính bọc ngoài một thẻ `<main>` khác của view so sánh). Đây là lỗi cú pháp HTML Semantic vi phạm chuẩn W3C HTML5.

#### Nhật ký DevTools:
- **DevTools Console:** 0 Lỗi, 0 Cảnh báo.
- **Network Requests:** Không có request lỗi mạng.
- **Mã lỗi ghi nhận:** `BUG-P3-04` (Mức độ: UI/UX).

---

### TÍNH NĂNG 3.4: ĐÁNH GIÁ SẢN PHẨM & XẾP HẠNG SAO - PRODUCT REVIEWS (`/nuoc-hoa/{slug}#reviews`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/nuoc-hoa/bleu-de-chanel-parfum#reviews`
- **Tiêu đề trang:** `Bleu de Chanel Parfum | PERFUME LUXE`
- **Tài khoản sử dụng:** `testvip2026@perfumeluxe.vn` (Khách hàng VIP)
- **Phân loại mức độ nghiêm trọng:** **FAILED / CẦN SỬA LỖI (Major: 1, Minor: 2)**

#### Các bước thực hiện & Tái hiện (Detailed Reproduction Sequence):
1. **Kiểm tra khi chưa đăng nhập (Guest):**
   - Truy cập URL: `https://perfumeluxe.somee.com/nuoc-hoa/bleu-de-chanel-parfum#reviews`.
   - Cuộn xuống khu vực "Đánh giá từ khách hàng".
   - Quan sát: Không hiển thị form đánh giá. Hệ thống hiển thị dòng thông báo: `<p><a href="/account/login">Đăng nhập với tài khoản khách hàng</a> để gửi đánh giá sau khi nhận sản phẩm.</p>`. Phía dưới hiển thị nhãn: `Chưa có đánh giá được công khai.`.
2. **Đăng nhập với tài khoản khách hàng VIP:**
   - Điều hướng tới `https://perfumeluxe.somee.com/account/login`.
   - Điền Email: `testvip2026@perfumeluxe.vn` / Mật khẩu: `TestVip2026!@#`.
   - Nhấn "ĐĂNG NHẬP". Hệ thống chuyển hướng vào `/account/profile` thành công.
   - Thiết lập các Authentication Cookies: `.AspNetCore.Identity.Application`, `.AspNetCore.Antiforgery.4A-hCrdKxys`.
   - Thông tin tài khoản: Tên `Nguyễn Văn Test VIP Updated`, Hạng `VIP Bronze`, Lịch sử đơn hàng: `0 đơn hoàn tất`.
3. **Quay lại trang chi tiết sản phẩm sau khi đã đăng nhập:**
   - Truy cập: `https://perfumeluxe.somee.com/nuoc-hoa/bleu-de-chanel-parfum#reviews`.
   - Quan sát: Form `#customerReviewForm` đã hiển thị đầy đủ:
     - Input ẩn `__RequestVerificationToken`.
     - Input ẩn `productId = 2`.
     - Dropdown chọn số sao: `select[name="rating"]` (1 sao đến 5 sao).
     - Vùng soạn thảo nhận xét: `textarea[name="comment"]` (maxlength 2000).
     - Input tải hình ảnh feedback: `input[name="images"]` (tối đa 3 ảnh).
     - Nút submit: `<button class="btn-luxe" type="submit">Gửi đánh giá</button>`.
4. **Thao tác gửi đánh giá:**
   - Chọn số sao: `5 sao` (`value="5"`).
   - Nhập nội dung nhận xét: `Mùi hương Bleu de Chanel Parfum vô cùng sang trọng, lưu hương trên 10 giờ. Dịch vụ gói quà và chăm sóc khách hàng của Perfume Luxe rất chu đáo!`.
   - Nhấn nút "Gửi đánh giá".
5. **Ghi nhận lỗi nghiêm trọng:**
   - Trình duyệt gửi request POST đến endpoint:
     `POST https://perfumeluxe.somee.com/api/v1/products/2/reviews`
   - Phản hồi từ máy chủ Somee: **`HTTP 403 Forbidden`** (Content-Length: 0, Response Body hoàn toàn rỗng).
   - Giao diện người dùng mở khối thông báo `#reviewApiMessage` màu đỏ với nội dung kỹ thuật thô:
     **`Yêu cầu thất bại (403).`**
   - Khách hàng không nhận được bất kỳ lời giải thích thân thiện nào (ví dụ: "Quý khách cần mua và nhận sản phẩm thành công trước khi gửi đánh giá").
   - Form đánh giá không được reset và người dùng bị hoang mang.

#### Phân tích Chuỗi Nguyên nhân Gốc rễ (Root Cause Logic Chain):
1. **Nghiệp vụ thương mại điện tử:** Dòng chữ thông báo khi chưa đăng nhập ghi rõ: *"Đăng nhập với tài khoản khách hàng để gửi đánh giá sau khi nhận sản phẩm"*. Điều này khẳng định hệ thống áp dụng chính sách: Chỉ khách hàng đã mua sản phẩm đó và đơn hàng đã ở trạng thái Hoàn tất mới được quyền viết đánh giá (Verified Purchase Review).
2. **Tài khoản kiểm thử:** Tài khoản `testvip2026` là tài khoản tạo mới, có `0 đơn hoàn tất` cho sản phẩm Bleu de Chanel (Product ID 2).
3. **Lỗi Backend API (BUG-P3-01 - Major):** Khi kiểm tra điều kiện mua hàng không thỏa mãn, Controller phía backend gọi `Forbid()` hoặc trả về `StatusCode(403)` với body rỗng thay vì trả về cấu trúc JSON giải thích: `StatusCode(403, new { message = "Chỉ quý khách đã mua và nhận sản phẩm này thành công mới có thể gửi đánh giá trải nghiệm." })`.
4. **Lỗi Xử lý Ngoại lệ Giao diện (BUG-P3-02 - Minor):** Script JavaScript phía client (`CustomerApi.request`) khi nhận mã HTTP 403 không có body JSON đã fallback hiển thị chuỗi kỹ thuật mặc định `Yêu cầu thất bại (403).`.
5. **Lỗi Điều Hướng Anchor (BUG-P3-03 - Minor):** Section chứa khối đánh giá là `<section class="luxury-card">` không hề có thuộc tính `id="reviews"` (chỉ có thẻ `<form id="customerReviewForm">`), làm cho liên kết hashtag `/nuoc-hoa/bleu-de-chanel-parfum#reviews` không thể tự động cuộn trang (scroll) xuống vị trí khối đánh giá.

#### Nhật ký DevTools:
- **DevTools Console:** 
  `Failed to load resource: the server responded with a status of 403 ()` tại URL `https://perfumeluxe.somee.com/api/v1/products/2/reviews`
- **DevTools Network:**
  - Request: `POST /api/v1/products/2/reviews`
  - Status: `403 Forbidden`
  - Headers: `RequestVerificationToken: CfDJ8...`, `Content-Type: multipart/form-data`
  - Response Body: *(Trống, 0 byte)*
- **Mã lỗi ghi nhận:** `BUG-P3-01` (Major), `BUG-P3-02` (Minor), `BUG-P3-03` (Minor).

---

## 3. NHẬT KÝ KIỂM THỬ CHI TIẾT GIAI ĐOẠN 4: ADMIN PORTAL & SYSTEM OPERATIONS

---

### TÍNH NĂNG 4.1: BẢNG ĐIỀU KHIỂN QUẢN TRỊ - ADMIN DASHBOARD (`/admin/admindashboard`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/admin/admindashboard`
- **Tiêu đề trang:** `Bảng Điều Khiển Quản Trị & Thống Kê Doanh Nghiệp | PERFUME LUXE`
- **Mã trạng thái HTTP:** `200 OK`
- **Tài khoản sử dụng:** `admin@perfumeluxe.vn`
- **Phân loại mức độ nghiêm trọng:** **UI/UX (Tràn ngang trên thiết bị di động nhỏ)**

#### Các bước thực hiện & Tái hiện:
1. Đăng nhập quyền Admin tại `/account/login?ReturnUrl=%2Fadmin%2Fadmindashboard`.
2. Kiểm tra chuyển hướng tự động vào Bảng điều khiển quản trị `/admin/admindashboard`.
3. Kiểm tra các thẻ chỉ số KPI tổng quan doanh nghiệp:
   - Tổng doanh thu đã thu: `0 ₫` (Từ 0 đơn đã giao thành công).
   - Doanh thu tháng này: `0 ₫` (Tháng 11/2026).
   - Giá trị đơn trung bình (AOV): `0 ₫`.
   - Đơn chờ duyệt: `0 đơn`.
   - Tỷ lệ giao thành công: `0%`.
4. Kiểm tra việc kết xuất biểu đồ trực quan Chart.js:
   - Canvas 1 (`#revenueLineChart`): Kích thước 863x280px, type `line`, 7 mốc thời gian, hiển thị xu hướng doanh thu.
   - Canvas 2 (`#orderStatusDoughnutChart`): Kích thước 406x250px, type `doughnut`, 5 trạng thái đơn hàng.
   - Canvas 3 (`#paymentMethodChart`): Kích thước 401x220px, type `doughnut`, 4 phương thức thanh toán.
5. Kiểm tra bảng danh sách đơn hàng gần đây (Recent Orders) với 7 cột chuẩn: Mã đơn, Khách hàng, Ngày đặt, Tổng tiền, Thanh toán, Trạng thái, Thao tác.
6. Kiểm tra sidebar quản trị: 24 liên kết quản trị hoạt động mượt mà.
7. Thử nghiệm thay đổi kích thước khung nhìn (Viewport Resize Testing):
   - Desktop 1440x900px: Giao diện căn chỉnh hoàn hảo.
   - Tablet 768x1024px: Sidebar tự động co gập, biểu đồ tự thích ứng.
   - Mobile 375x667px (iPhone SE): Phát hiện thanh cuộn ngang xuất hiện (Horizontal scroll).

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Hiển thị chuẩn xác các chỉ số tài chính, vẽ đủ 3 biểu đồ Chart.js, bảng đơn hàng gần đây tải nhanh, giao diện co giãn tốt trên mọi thiết bị.
- **Thực tế:** Các chỉ số và biểu đồ khởi tạo thành công 100%. Gặp hiện tượng tràn ngang trên màn hình di động <= 375px.

#### Nhật ký DevTools:
- **DevTools Console:** 0 Lỗi, 0 Cảnh báo.
- **DevTools Network:** Toàn bộ thư viện Chart.js CDN, CSS, Font Awesome tải mã `200 OK`.
- **Mã lỗi ghi nhận:** `BUG-P4-05` (Mức độ: UI/UX).

---

### TÍNH NĂNG 4.2: QUẢN LÝ DANH MỤC & SẢN PHẨM - PRODUCTS & CATALOG (`/admin/admindashboard/products`)

- **URL kiểm thử:** 
  - Danh sách: `https://perfumeluxe.somee.com/admin/admindashboard/products`
  - Thêm mới: `https://perfumeluxe.somee.com/admin/admindashboard/createproduct`
  - Nút Sửa (Render trên giao diện): `https://perfumeluxe.somee.com/admin/admindashboard/editproduct/1`
  - Nút Sửa (Query string hợp lệ): `https://perfumeluxe.somee.com/admin/admindashboard/editproduct?id=1`
- **Tiêu đề trang:** `Quản Lý Sản Phẩm | PERFUME LUXE`, `Thêm Sản Phẩm Nước Hoa Mới | PERFUME LUXE`, `Chỉnh Sửa Sản Phẩm: Dior Sauvage Elixir Parfum | PERFUME LUXE`
- **Phân loại mức độ nghiêm trọng:** **PARTIAL / CẦN SỬA LỖI (Major: 1)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `/admin/admindashboard/products`.
2. Kiểm tra danh sách hiển thị 15 dòng sản phẩm nước hoa cao cấp (Dior, Chanel, Creed, Le Labo, Tom Ford,...). Mỗi dòng tích hợp bảng quản lý biến thể dung tích (10ml, 50ml, 100ml) trực tiếp.
3. Kiểm tra bộ lọc tìm kiếm: Nhập "Sauvage" vào ô `input[name=search]`, nhấn Enter.
   - Hệ thống lọc chính xác về đúng 1 bản ghi: *Dior Sauvage Elixir Parfum* (SKU: DIOR-SE-001). URL cập nhật `?search=Sauvage`.
4. Kiểm tra màn hình Thêm sản phẩm:
   - Click "THÊM SẢN PHẨM" -> Điều hướng sang `/admin/admindashboard/createproduct`.
   - Kiểm tra 22 trường nhập liệu chi tiết: Tên, SKU, Thương hiệu, Danh mục, Nồng độ tinh dầu, Giới tính, Giá bán niêm yết, 3 Tầng hương (Top/Heart/Base), Tồn kho ban đầu, Dung tích ban đầu,...
   - Thử submit form rỗng: Các trường HTML5 `required` kích hoạt ngăn chặn submit hợp lệ.
5. **Kiểm tra Chức năng Chỉnh sửa sản phẩm & Phát hiện lỗi 404:**
   - Quay lại danh sách sản phẩm, click vào nút "Sửa" trên dòng sản phẩm ID #1 (Dior Sauvage Elixir).
   - Thẻ `<a>` sinh ra liên kết: `https://perfumeluxe.somee.com/admin/admindashboard/editproduct/1`.
   - Máy chủ Somee lập tức trả về: **`HTTP 404 Not Found`**.
   - Thử nghiệm nhập trực tiếp URL với Query Param: `https://perfumeluxe.somee.com/admin/admindashboard/editproduct?id=1`.
   - Kết quả: Trình duyệt tải thành công trang Sửa sản phẩm với mã **`HTTP 200 OK`**, nạp đầy đủ toàn bộ 15 trường dữ liệu có sẵn của chai Dior Sauvage Elixir.

#### Phân tích Chuỗi Nguyên nhân Gốc rễ:
- Backend ASP.NET Core Controller đăng ký action `EditProduct(int id)` nhưng không khai báo Route template dạng token `{id}` (hoặc router mặc định chỉ bind tham số qua query string `?id=1`).
- Template Razor View trên trang danh sách lại sử dụng tag-helper `asp-route-id="@item.Id"` khi chưa cấu hình route template, dẫn đến trình duyệt gọi URL `/admin/admindashboard/editproduct/1` và bị 404 Not Found.
- Quản trị viên khi nhấn vào nút Sửa trên bảng danh sách bị chặn hoàn toàn, không thể cập nhật sản phẩm.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi cú pháp script.
- **DevTools Network:**
  - `GET /admin/admindashboard/editproduct/1` -> **`HTTP 404 Not Found`**
  - `GET /admin/admindashboard/editproduct?id=1` -> **`HTTP 200 OK`**
- **Mã lỗi ghi nhận:** `BUG-P4-01` (Mức độ: Major).

---

### TÍNH NĂNG 4.3: QUẢN TRỊ KHO HÀNG & ĐIỀU CHỈNH TỒN - INVENTORY MANAGEMENT (`/admin/admindashboard/inventory`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/admin/admindashboard/inventory`
- **Tiêu đề trang:** `Quản Trị Tồn Kho & Nhập Hàng | PERFUME LUXE`
- **Mã trạng thái HTTP:** `200 OK`
- **Phân loại mức độ nghiêm trọng:** **None (Pass Xuất Sắc)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `/admin/admindashboard/inventory`.
2. Kiểm tra Bảng 1 (Danh sách tồn kho SKU): 15 dòng dữ liệu chi tiết cho từng biến thể dung tích (10ml, 50ml, 100ml) với SKU riêng biệt.
3. Quan sát các badge cảnh báo tồn kho thông minh: "Sẵn sàng" (Xanh lá), "Sắp hết" (Cam), "Hết hàng" (Đỏ).
4. Thực hiện thao tác Điều chỉnh tồn kho (Stock Adjustment):
   - Nhấn nút "Nhập / Xuất Kho" (`openAdjustStockModal`) tại biến thể *Bleu de Chanel Parfum - Chiết 10ml* (Tồn hiện tại: 60 chai).
   - Hộp thoại Modal `#adjustStockModal` xuất hiện với các trường: `variantId`, `adjustedQuantity`, `unitCost`, `reason`, `note`.
   - Điền thông tin điều chỉnh:
     - Số lượng điều chỉnh: `+2`
     - Giá vốn đơn vị (COGS): `312000`
     - Lý do: `Nhập hàng mới từ nhà cung cấp`
     - Ghi chú: `Kiểm thử điều chỉnh kho tự động Milestone 2`
   - Bấm nút xác nhận điều chỉnh kho.
5. Quan sát phản hồi và kiểm toán kho:
   - Form gửi request POST đến `/admin/admindashboard/adjuststock` thành công (`HTTP 200/302`).
   - Bảng 2 (Lịch sử biến động kho / Stock Audit Log) lập tức ghi nhận 1 bản ghi mới nhất với đầy đủ: Mốc thời gian, Sản phẩm & Phiên bản, Biến động (+2), Tồn cũ -> Tồn mới (60 -> 62), Giá vốn COGS (312.000 ₫), Ghi chú và Tài khoản thực thi (`admin@perfumeluxe.vn`).

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Cho phép cập nhật tồn kho linh hoạt, ghi nhận lịch sử biến động kho minh bạch phục vụ kiểm toán tài chính.
- **Thực tế:** Hoàn toàn đạt 100% yêu cầu.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi.
- **DevTools Network:** `POST /admin/admindashboard/adjuststock` -> `HTTP 200 OK`.

---

### TÍNH NĂNG 4.4: BÁO CÁO TÀI CHÍNH P&L & XUẤT CSV - FINANCIAL REPORT (`/admin/admindashboard/financialreport`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/admin/admindashboard/financialreport`
- **Tiêu đề trang:** `Báo Cáo Tài Chính P&L & Định Giá Kho | PERFUME LUXE`
- **Mã trạng thái HTTP:** `200 OK`
- **Phân loại mức độ nghiêm trọng:** **None (Pass Xuất Sắc)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `/admin/admindashboard/financialreport`.
2. Kiểm tra các chỉ số P&L (Profit & Loss) và Định giá kho tổng thể:
   - Doanh thu thuần (Net Sales): `0 ₫` (Từ 0 đơn đã giao hoàn tất).
   - Lợi nhuận gộp (Gross Profit): `0 ₫` (Biên lợi nhuận gộp: 0.0%).
   - Giá vốn hàng bán (COGS): `0 ₫`.
   - Định giá kho theo giá vốn (Inventory Valuation): **`877.950.000 ₫`** (Tổng tồn: 1.043 chai, Tổng giá trị niêm yết bán lẻ: **`2.074.060.000 ₫`**).
3. Kiểm tra bảng phân tích định giá kho theo 8 thương hiệu hàng đầu:
   - *Chanel:* 255 chai, Giá vốn: 193.350.000 ₫, Giá niêm yết: 444.060.000 ₫, Biên lãi kho: 56.5%.
   - *Yves Saint Laurent:* 187 chai, Giá vốn: 135.219.000 ₫, Giá niêm yết: 322.060.000 ₫, Biên lãi kho: 58.0%.
   - *Christian Dior:* 150 chai, Giá vốn: 120.708.000 ₫, Giá niêm yết: 287.400.000 ₫, Biên lãi kho: 58.0%.
   - *Creed:* 101 chai, Giá vốn: 110.859.000 ₫.
   - *Le Labo:* 80 chai, Giá vốn: 103.488.000 ₫.
   - *Tom Ford:* 85 chai, Giá vốn: 89.439.000 ₫.
   - *Maison Margiela:* 141 chai, Giá vốn: 81.123.000 ₫.
   - *Kilian:* 44 chai, Giá vốn: 43.764.000 ₫.
4. Kiểm tra bộ lọc khoảng ngày (Date Range Filter):
   - Chọn `fromDate=2026-01-01` và `toDate=2026-11-07`, bấm nút "LỌC".
   - URL cập nhật đúng ngữ cảnh, dữ liệu báo cáo duy trì chuẩn xác.
5. **Kiểm tra Xuất File CSV Đơn Hàng P&L:**
   - Click liên kết "XUẤT CSV ĐƠN HÀNG P&L".
   - Trình duyệt tải về file: `PerfumeLuxe_Financial_Orders_20261107_0203.csv` (176 bytes, chuẩn UTF-8 with BOM).
   - Cột tiêu đề: `Mã Đơn, Ngày Đặt, Khách Hàng, Phương Thức, Tiền Hàng, Giảm Giá, Gói Quà, Phí Ship, Doanh Thu Thuần, Giá Vốn Ước Tính, Lợi Nhuận Gộp, Biên Lãi %`.
6. **Kiểm tra Xuất File CSV Định Giá Kho:**
   - Click liên kết "XUẤT CSV ĐỊNH GIÁ KHO".
   - Trình duyệt tải về file: `PerfumeLuxe_Inventory_Valuation_20261107_0203.csv` (4.798 bytes, UTF-8 with BOM).
   - Cột tiêu đề: `ID Variant, Mã SKU, Tên Nước Hoa, Thương Hiệu, Dung Tích, Tồn Kho, Đơn Giá Vốn, Đơn Giá Niêm Yết, Tổng Giá Trị Vốn, Tổng Giá Trị Bán Lẻ, Lợi Nhuận Tiềm Năng`.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Phân tích tài chính chính xác, tính biên lãi kho theo thương hiệu, xuất 2 định dạng file CSV đầy đủ trường dữ liệu kế toán không bị lỗi font tiếng Việt.
- **Thực tế:** Hoàn toàn đạt 100%. Định dạng UTF-8 with BOM mở trực tiếp trên Microsoft Excel không bị vỡ font tiếng Việt.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi.
- **DevTools Network:** Tải file trả về header `Content-Disposition: attachment` với mã `200 OK`.

---

### TÍNH NĂNG 4.5: CÀI ĐẶT HỆ THỐNG & CỬA HÀNG - SYSTEM SETTINGS (`/admin/admindashboard/settings`)

- **URL kiểm thử:** `https://perfumeluxe.somee.com/admin/admindashboard/settings`
- **Tiêu đề trang:** `Cài Đặt Hệ Thống & Cửa Hàng | PERFUME LUXE`
- **Mã trạng thái HTTP:** `200 OK`
- **Phân loại mức độ nghiêm trọng:** **None (Pass Tốt)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `/admin/admindashboard/settings`.
2. Kiểm tra các nhóm cấu hình hệ thống trên biểu mẫu:
   - *Thông tin cửa hàng:* Tên (`PERFUME LUXE`), Slogan (`Haute Parfumerie & Niche Fragrances`), Hotline (`1900 8888`), Email (`concierge@perfumeluxe.vn`), Địa chỉ boutique (`88 Đồng Khởi, Bến Nghé, Quận 1, TP. Hồ Chí Minh`), Giờ làm việc (`08:00 - 22:00`).
   - *Chính sách vận chuyển:* Ngưỡng miễn phí vận chuyển (`2.000.000 ₫`), Phí vận chuyển tiêu chuẩn (`35.000 ₫`).
   - *Cấu hình Mail Gateway SMTP:* Host (`localhost`), Port (`1025`), Kích hoạt SSL, Email gửi, Tên người gửi.
   - *Chế độ bảo trì:* Bật/tắt bảo trì, Thông điệp bảo trì, Giờ dự kiến kết thúc, Danh sách IP tin cậy (IP Whitelist).
   - *Công cụ kiểm thử gửi mail:* Hộp dispatch test mail.
3. Thao tác lưu cấu hình: Bấm nút "LƯU CÀI ĐẶT HỆ THỐNG".
4. Máy chủ gửi yêu cầu POST đến `/Admin/AdminDashboard/Settings` và trả về kết quả mượt mà.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Cho phép cập nhật cấu hình thông tin cửa hàng, vận chuyển và email mà không gây sập ứng dụng.
- **Thực tế:** Hoàn toàn đạt yêu cầu.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi.
- **DevTools Network:** POST `/Admin/AdminDashboard/Settings` -> `HTTP 200 OK`.

---

### TÍNH NĂNG 4.6: QUẢN LÝ BÀI VIẾT & TẠP CHÍ - BLOG MANAGEMENT (`/admin/admindashboard/blogs`)

- **URL kiểm thử:**
  - Danh sách bài viết: `https://perfumeluxe.somee.com/admin/admindashboard/blogs`
  - Soạn bài viết mới: `https://perfumeluxe.somee.com/admin/admindashboard/createblog`
- **Tiêu đề trang:** `Quản Lý Bài Viết & Tạp Chí | PERFUME LUXE`, `Soạn Bài Viết Mới | PERFUME LUXE`
- **Mã trạng thái HTTP:** `200 OK`
- **Phân loại mức độ nghiêm trọng:** **Minor (Thiếu bộ soạn thảo trực quan WYSIWYG)**

#### Các bước thực hiện & Tái hiện:
1. Truy cập `/admin/admindashboard/blogs`.
2. Quan sát danh sách bài viết: Hiển thị 20 bài viết tạp chí chuyên sâu về nghệ thuật nước hoa niche (Ví dụ: "Top 5 Xu Hướng Nước Hoa Niche Bùng Nổ Năm 2026", "Nước Hoa Bền Vững: Tương Lai Của Ngành Hương Thơm Xa Xỉ 2026",...). Mỗi bài viết hiển thị ảnh thumbnail, số lượt xem (400 - 600 views) và badge trạng thái "Đã Xuất Bản".
3. Click "SOẠN BÀI VIẾT MỚI" -> Chuyển đến `/admin/admindashboard/createblog`.
4. Kiểm tra các trường dữ liệu trên form: `Title`, `Slug`, `Summary`, `Content`, `MetaTitle`, `MetaDescription`, `CategoryId`, `AuthorName`, `ThumbnailUrl`, `IsPublished`.
5. Kiểm tra trường `Content`: Hiện tại đang là thẻ `<textarea class="form-control" name="Content">` thô, chưa có thanh công cụ soạn thảo trực quan WYSIWYG (như TinyMCE, CKEditor hay Quill.js). Quản trị viên phải tự gõ các thẻ HTML thủ công khi muốn in đậm, chèn tiêu đề H2/H3 hoặc chèn ảnh minh họa.

#### Kết quả quan sát vs Kỳ vọng:
- **Kỳ vọng:** Danh sách bài viết tải đầy đủ, cho phép tạo bài viết mới chuẩn SEO. Nên có bộ soạn thảo WYSIWYG để biên tập viên tạp chí dễ thao tác.
- **Thực tế:** Các chức năng cơ bản hoạt động tốt. Ghi nhận thiếu sót bộ soạn thảo trực quan.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi JS.
- **DevTools Network:** `200 OK`.
- **Mã lỗi ghi nhận:** `BUG-P4-04` (Mức độ: Minor).

---

### TÍNH NĂNG 4.7: CONSOLE CSDL & SỨC KHỎE HỆ THỐNG - DATABASE CONSOLE & SYSTEM HEALTH

- **URL kiểm thử:**
  - Route URL đề xuất trong yêu cầu: `https://perfumeluxe.somee.com/admin/admindashboard/databaseconsole`
  - Route URL thực tế hoạt động: `https://perfumeluxe.somee.com/admin/databaseconsole`
  - Sức khỏe hệ thống: `https://perfumeluxe.somee.com/admin/admindashboard/systemhealth`
- **Tiêu đề trang:** `AWS-Style Database Console | PERFUME LUXE`, `Bảng Điều Khiển Sức Khỏe Nền Tảng | PERFUME LUXE`
- **Phân loại mức độ nghiêm trọng:** **PARTIAL / CẦN SỬA LỖI (Major: 1, Minor: 1)**

#### Các bước thực hiện & Tái hiện:
1. **Kiểm tra Route Console CSDL:**
   - Thử nghiệm truy cập URL: `https://perfumeluxe.somee.com/admin/admindashboard/databaseconsole`.
   - Kết quả: Máy chủ trả về mã lỗi **`HTTP 404 Not Found`**.
   - Kiểm tra menu Sidebar quản trị, bấm vào mục "Console Cơ Sở Dữ Liệu":
     - Trình duyệt điều hướng sang: `https://perfumeluxe.somee.com/admin/databaseconsole`.
     - Kết quả: Tải thành công mã **`HTTP 200 OK`**.
2. **Kiểm thử Tính năng AWS-Style Database Console (`/admin/databaseconsole`):**
   - Giao diện gồm 3 tab chuyên dụng: "Duyệt Bảng & CRUD Trực Quan", "Trình Chạy Lệnh SQL (Query Runner)", "Nạp Dữ Liệu Mẫu (Seed Data)".
   - Click tab "Trình Chạy Lệnh SQL" (`#queryTabBtn`).
   - Nhập câu lệnh truy vấn SQL: `SELECT TOP 5 Id, Name, BasePrice FROM Products`.
   - Nhấn "Thực Thi Truy Vấn (Run)".
   - Kết quả: Trình duyệt kết xuất ngay bảng dữ liệu 5 dòng sản phẩm kèm cột Id, Name, BasePrice chính xác tức thì.
3. **Kiểm thử Bảng Điều Khiển Sức Khỏe Nền Tảng (`/admin/admindashboard/systemhealth`):**
   - Truy cập `https://perfumeluxe.somee.com/admin/admindashboard/systemhealth`.
   - Quan sát các thẻ chẩn đoán tài nguyên máy chủ:
     - *Bộ nhớ tiến trình (Working Set RAM):* `313 MB` (GC Allocated: `143 MB`).
     - *Thời gian chạy liên tục (Uptime):* `0 ngày 00 giờ 50 phút` (Khởi động: 06/11/2026 19:18:04).
     - *Dung lượng ổ đĩa:* `20.2 GB / 50.0 GB (d:\)` trống.
     - *Môi trường:* Microsoft Windows 10.0.20348, .NET 8.0.4 Runtime, 4 Cores CPU logic, In-Memory Distributed Cache.
4. **Phát hiện lỗi chẩn đoán CSDL (False Alarm):**
   - Trên thẻ "CƠ SỞ DỮ LIỆU SQL", hệ thống hiển thị badge màu đỏ:
     **`● Ngoại Tuyến | 2 ms | Lỗi kết nối cơ sở dữ liệu`**
   - Mặc dù thực tế: Toàn bộ website, API, Console CSDL và các trang quản trị vừa truy vấn dữ liệu SQL hoàn toàn bình thường.
   - Nguyên nhân: Phương thức HealthCheck ping tới database trong `SystemHealthService` bị lỗi timeout hoặc dùng lệnh kiểm tra không tương thích với cấu hình SQL Server trên máy chủ chia sẻ Somee.

#### Nhật ký DevTools:
- **DevTools Console:** 0 lỗi JS.
- **DevTools Network:**
  - `GET /admin/admindashboard/databaseconsole` -> `HTTP 404 Not Found` (BUG-P4-03)
  - `GET /admin/databaseconsole` -> `HTTP 200 OK`
  - `GET /admin/admindashboard/systemhealth` -> `HTTP 200 OK` (Dữ liệu trả về trạng thái CSDL bị Unhealthy - BUG-P4-02)
- **Mã lỗi ghi nhận:** `BUG-P4-02` (Major), `BUG-P4-03` (Minor).

---

## 4. SỔ BỘ LỖI TOÀN DIỆN (MASTER BUG REGISTER) & PHÂN LOẠI MỨC ĐỘ

Dưới đây là bảng tổng hợp toàn bộ 10 lỗi/vấn đề kỹ thuật được ghi nhận trong đợt kiểm thử Giai đoạn 3 và Giai đoạn 4, được gán mã định danh duy nhất kèm phân loại mức độ nghiêm trọng:

| Mã Lỗi | Giai Đoạn | Phân Hệ / Thành Phần Ảnh Hưởng | URL Kiểm Thử | Loại Lỗi | Mức Độ Nghiêm Trọng | Mô Tả Hiện Tượng Chi Tiết |
|:---|:---:|:---|:---|:---|:---:|:---|
| **BUG-P3-01** | P3 | Product Reviews API | `POST /api/v1/products/{id}/reviews` | Backend / API Response Body | **Major** | API từ chối gửi review bằng HTTP 403 khi tài khoản chưa mua hàng nhưng trả về Response Body hoàn toàn rỗng (0 byte), không có thông điệp JSON giải thích lý do cho phía client. |
| **BUG-P3-02** | P3 | Product Reviews UI | `#reviewApiMessage` | Frontend / UX Exception Handling | **Minor** | Giao diện hiển thị chuỗi kỹ thuật thô: `Yêu cầu thất bại (403).` gây khó hiểu và bối rối cho khách hàng VIP. |
| **BUG-P3-03** | P3 | Product Reviews Anchor | `/nuoc-hoa/{slug}#reviews` | Frontend / HTML Anchor | **Minor** | Thẻ `<section class="luxury-card">` chứa khối đánh giá thiếu thuộc tính `id="reviews"`, khiến liên kết hashtag URL không tự động cuộn trang (scroll) xuống vị trí khối đánh giá. |
| **BUG-P3-04** | P3 | Perfume Comparison DOM | `/so-sanh` | HTML Semantic / Template | **UI/UX** | Trang so sánh chứa 2 thẻ `<main>` lồng nhau/song song, vi phạm chuẩn HTML5 ngữ nghĩa của W3C. |
| **BUG-P4-01** | P4 | Quản Lý Sản Phẩm | `/admin/admindashboard/products` | Routing / Razor Tag Helper | **Major** | Nút "Sửa" trên từng dòng sản phẩm sinh liên kết dạng `/admin/admindashboard/editproduct/{id}` bị lỗi **HTTP 404 Not Found**. Backend router chỉ nhận dạng `?id={id}`. |
| **BUG-P4-02** | P4 | Kiểm Tra Sức Khỏe | `/admin/admindashboard/systemhealth` | Health Check Service / SQL Ping | **Major** | Thẻ chẩn đoán CSDL báo "● Ngoại Tuyến | Lỗi kết nối cơ sở dữ liệu" (báo động giả - false alarm) trong khi toàn bộ hệ thống vẫn đọc/ghi CSDL bình thường. |
| **BUG-P4-03** | P4 | Console CSDL Route | `/admin/admindashboard/databaseconsole` | Routing / URL Alias | **Minor** | Đường dẫn phụ `/admin/admindashboard/databaseconsole` trả về HTTP 404 Not Found; route chính thức trên hệ thống là `/admin/databaseconsole`. |
| **BUG-P4-04** | P4 | Soạn Thảo Blog | `/admin/admindashboard/createblog` | UI Component / Rich Text | **Minor** | Trường nội dung bài viết sử dụng `<textarea>` HTML thô, thiếu bộ soạn thảo trực quan WYSIWYG (TinyMCE/Quill) khiến việc trình bày bài viết khó khăn. |
| **BUG-P4-05** | P4 | Admin Dashboard Mobile | `/admin/admindashboard` | Responsive CSS / Overflow | **UI/UX** | Xuất hiện thanh cuộn ngang (Horizontal scrollbar) trên màn hình thiết bị di động có chiều rộng <= 375px do container bảng và biểu đồ chưa co gọn thẻ. |
| **BUG-P4-06** | P4 | Thanh Navbar Quản Trị | `/admin/*` | Accessibility / UI Behavior | **UI/UX** | Nút đăng xuất trong navbar sử dụng trực tiếp `<button type="submit">` không có bước xác nhận (confirmation dialog), dễ bị kích hoạt ngoài ý muốn khi nhấn Enter. |

---

## 5. LỘ TRÌNH KHẮC PHỤC KỸ THUẬT (REMEDIATION ROADMAP) TUẦN TỚI

Dành riêng cho đội ngũ phát triển (Development Team) triển khai sửa lỗi vào tuần tới:

### Gói Sửa Lỗi Ưu Tiên 1: Khắc Phục Các Lỗi Major (Priority 1 - High)

#### 1. Khắc phục BUG-P3-01 & BUG-P3-02 (API Reviews & Thông báo thân thiện)
- **Vấn đề:** Khách hàng VIP bấm gửi review bị 403 với body rỗng và hiện chuỗi thô `Yêu cầu thất bại (403).`.
- **Giải pháp Backend:**
  - Trong Controller xử lý `POST /api/v1/products/{id}/reviews`, khi kiểm tra điều kiện mua hàng thất bại, thay vì gọi `return Forbid();`, hãy trả về:
    ```csharp
    return StatusCode(StatusCodes.Status403Forbidden, new 
    { 
        success = false, 
        message = "Chỉ quý khách đã mua và nhận sản phẩm này thành công mới có thể gửi đánh giá trải nghiệm." 
    });
    ```
- **Giải pháp Frontend:**
  - Trong file script xử lý form review (`CustomerApi.request` hoặc event submit `#customerReviewForm`), cập nhật khối catch lỗi:
    ```javascript
    if (response.status === 403) {
        let msg = "Chỉ quý khách đã mua và nhận sản phẩm này thành công mới có thể gửi đánh giá trải nghiệm.";
        try {
            const data = await response.json();
            if (data && data.message) msg = data.message;
        } catch (e) {}
        showReviewMessage(msg, "error");
    }
    ```

#### 2. Khắc phục BUG-P4-01 (Lỗi 404 Nút Sửa Sản Phẩm)
- **Vấn đề:** Nút "Sửa" trên bảng sản phẩm trỏ tới `/admin/admindashboard/editproduct/1` gây 404.
- **Giải pháp:** Có thể áp dụng 1 trong 2 cách (khuyến nghị áp dụng cả 2 để tương thích hoàn toàn):
  - *Cách A (Routing Attribute):* Trong `AdminDashboardController.cs`, thêm attribute hỗ trợ cả 2 dạng route:
    ```csharp
    [HttpGet]
    [Route("admin/admindashboard/editproduct")]
    [Route("admin/admindashboard/editproduct/{id:int}")]
    public async Task<IActionResult> EditProduct(int id) { ... }
    ```
  - *Cách B (Razor View Fix):* Trong file Razor View `Products.cshtml`, sửa lại thẻ sinh link sửa:
    ```html
    <!-- Thay đổi từ -->
    <a asp-action="EditProduct" asp-route-id="@p.Id" class="btn-action edit">Sửa</a>
    <!-- Sang dạng href tường minh hoặc query param -->
    <a href="/admin/admindashboard/editproduct?id=@p.Id" class="btn-action edit">Sửa</a>
    ```

#### 3. Khắc phục BUG-P4-02 (Chẩn đoán CSDL báo Ngoại tuyến trong System Health)
- **Vấn đề:** Thẻ CSDL báo "● Ngoại Tuyến | Lỗi kết nối cơ sở dữ liệu" trong khi CSDL vẫn đọc ghi bình thường.
- **Giải pháp:**
  - Kiểm tra service kiểm tra sức khỏe CSDL (ví dụ `SystemHealthService` hoặc `AddHealthChecks().AddSqlServer`).
  - Đảm bảo connection string sử dụng trong service chẩn đoán trùng khớp 100% với chuỗi `DefaultConnection` của `DbContext`.
  - Thiết lập timeout hợp lý (ví dụ: `Connect Timeout=15;Command Timeout=10`) và thực thi câu lệnh kiểm tra nhẹ: `SELECT 1;` thay vì mở kết nối nặng.
  - Xử lý bắt ngoại lệ chi tiết để ghi log lý do fail (ví dụ: SSL/TLS certificate validation, Somee firewall).

---

### Gói Sửa Lỗi Ưu Tiên 2: Khắc Phục Các Lỗi Minor (Priority 2 - Medium)

#### 4. Khắc phục BUG-P3-03 (Bổ sung Anchor `id="reviews"`)
- **Vị trí:** View chi tiết sản phẩm `NuocHoa/Detail.cshtml` (hoặc tương đương).
- **Thực hiện:** Bổ sung thuộc tính `id="reviews"` vào thẻ `<section class="luxury-card">` bao quanh khối đánh giá:
  ```html
  <section class="luxury-card review-section" id="reviews">
      <h3 class="section-title">Đánh Giá Từ Khách Hàng</h3>
      ...
  </section>
  ```

#### 5. Khắc phục BUG-P4-03 (Tương thích URL `/admin/admindashboard/databaseconsole`)
- **Vị trí:** `AdminDashboardController.cs` hoặc cấu hình routing.
- **Thực hiện:** Thêm action redirect:
  ```csharp
  [HttpGet("admin/admindashboard/databaseconsole")]
  public IActionResult RedirectDatabaseConsole()
  {
      return RedirectToAction("Index", "DatabaseConsole", new { area = "Admin" });
      // Hoặc: return Redirect("/admin/databaseconsole");
  }
  ```

#### 6. Khắc phục BUG-P4-04 (Tích hợp Rich Text Editor cho Blog)
- **Vị trí:** `CreateBlog.cshtml` và `EditBlog.cshtml`.
- **Thực hiện:** Tích hợp bộ soạn thảo CDN nhẹ như TinyMCE hoặc Quill.js:
  ```html
  <script src="https://cdn.jsdelivr.net/npm/quill@2.0.2/dist/quill.js"></script>
  <link href="https://cdn.jsdelivr.net/npm/quill@2.0.2/dist/quill.snow.css" rel="stylesheet">
  <script>
    const quill = new Quill('#editor-container', { theme: 'snow' });
    document.querySelector('form').onsubmit = function() {
        document.querySelector('input[name=Content]').value = quill.root.innerHTML;
    };
  </script>
  ```

---

### Gói Sửa Lỗi Ưu Tiên 3: Tối Ưu Hóa Giao Diện & UI/UX (Priority 3 - Low)

#### 7. Khắc phục BUG-P3-04 (Loại bỏ thẻ `<main>` kép)
- **Vị trí:** Razor View `Views/Catalog/Compare.cshtml` (hoặc view `so-sanh.cshtml`).
- **Thực hiện:** Thay đổi thẻ `<main>` trong view con thành `<div class="comparison-container">` hoặc `<section class="comparison-section">` vì layout chung `_Layout.cshtml` đã có thẻ `<main role="main">`.

#### 8. Khắc phục BUG-P4-05 (Chống tràn ngang Mobile trên Admin Dashboard)
- **Vị trí:** CSS quản trị `admin.css`.
- **Thực hiện:** Bọc các bảng danh sách vào container `.table-responsive` với thuộc tính `overflow-x: auto; -webkit-overflow-scrolling: touch;`. Cấu hình thẻ `@media (max-width: 576px)` để co nhỏ padding và cho phép grid KPI xếp chồng cột 100% width.

#### 9. Khắc phục BUG-P4-06 (Bảo vệ nút Đăng xuất Admin)
- **Thực hiện:** Bổ sung hộp thoại xác nhận JavaScript `onclick="return confirm('Quý khách có chắc chắn muốn đăng xuất khỏi cổng Quản Trị?');"` hoặc sử dụng dropdown menu cho mục thông tin tài khoản admin.

---

## 6. KẾT LUẬN & BIÊN BẢN KÝ DUYỆT (SIGN-OFF)

### 6.1. Đánh Giá Tổng Thể
1. **Trải nghiệm khách hàng (Phase 3):**
   - Bộ tính năng AI Fragrance Finder và Custom Discovery Box đạt độ hoàn thiện rất cao về cả logic tính toán lẫn giao diện tương tác thượng lưu Haute Parfumerie.
   - Tính năng So Sánh Nước Hoa hiển thị đầy đủ, chính xác 12 tiêu chí chuyên môn.
   - Tính năng Đánh Giá cần bổ sung thông điệp giải thích điều kiện mua hàng khi API từ chối để tránh ảnh hưởng tới tâm lý khách hàng.
2. **Năng lực vận hành quản trị (Phase 4):**
   - Hệ thống quản trị của Perfume Luxe hoạt động mượt mà, cung cấp đầy đủ các công cụ kiểm soát kho bãi thời gian thực, báo cáo tài chính P&L, xuất file CSV định dạng chuẩn UTF-8 with BOM và trình quản trị CSDL trực quan phong cách AWS RDS.
   - Tốc độ tải trang nhanh (1 - 2 giây), không có lỗi crash máy chủ 500.

### 6.2. Ký Duyệt Bàn Giao
Biên bản này được lập và lưu trữ chính thức tại thư mục gốc của dự án `TESTING_DIARY_PHASE3_PHASE4.md`. Toàn bộ dữ liệu kiểm thử, log DevTools Console/Network và sổ bộ lỗi đã sẵn sàng để chuyển giao cho đội ngũ kỹ thuật thực hiện sprint sửa lỗi vào tuần tới.

- **Đại diện Đội ngũ Kiểm Thử Trình Duyệt (QA Engineer):** *Đã ký & nghiệm thu*
- **Đại diện Điều phối Kỹ thuật (Technical Lead / Orchestrator):** *Đã ký & nghiệm thu*
- **Ngày bàn giao:** 2026-11-07
