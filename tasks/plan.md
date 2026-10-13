# KẾ HOẠCH NÂNG CẤP PHÂN HỆ AI FRAGRANCE FINDER QUIZ
**Dự án**: Perfume Luxe - Haute Parfumerie E-Commerce  
**Tiêu chuẩn**: AI-First Architecture & Clean Architecture (.NET 8 MVC)

---

## 1. Mục Tiêu & Tầm Nhìn Dự Án
Chuyển đổi tính năng Trắc nghiệm chọn mùi hương từ bộ lọc tĩnh 3 câu thành **AI Fragrance Finder - Động Cơ Tư Vấn Mùi Hương Cá Nhân Hóa Chuẩn Haute Parfumerie**:
1. **Trải nghiệm người dùng 5 Bước (5-Step Luxury Flow)**: Đối tượng -> Phong thái (Vibe) -> Hoàn cảnh -> Cường độ & Lưu tỏa -> Ngân sách & Định dạng.
2. **Thuật toán Ma Trận Điểm Số (Hybrid Scent Matrix Engine)**:
   - Chấm điểm đa chiều (Match Score %) từ 80% đến 99%.
   - Phân tích nốt hương tương đồng (Top/Heart/Base notes), nhóm hương (Woody, Oriental, Fresh, Floral, Gourmand, Leather...), độ lưu tỏa, nồng độ (EDP, Parfum, Extrait) và tầm giá.
   - Giải thích minh bạch lý do đề xuất (*Explainable AI*).
3. **Thẻ Danh Phận Mùi Hương (Scent Archetype & Luxury Dossier)**:
   - Tự động sinh Archetype độc bản (ví dụ: *The Sovereign Aristocrat*, *The Velvet Enchanter*, *The Ethereal Luminary*...).
   - Hiển thị bản phân tích phong cách sống, nhóm hương chủ đạo và nốt hương vàng.
4. **Tiện ích E-Commerce & Lan tỏa**:
   - Thêm nhanh vào giỏ hàng qua AJAX Drawer.
   - Xem chi tiết Kim tự tháp 3 tầng hương.
   - Sao chép liên kết Hồ sơ mùi hương độc bản để chia sẻ.

---

## 2. Thiết Kế Kiến Trúc & Luồng Dữ Liệu

```
[User Interface - Quiz.cshtml]
      │
      │ 5-Step Quiz Data (POST /Catalog/QuizSubmit)
      ▼
[CatalogController.cs]
      │
      │ QuizAnswerRequest DTO
      ▼
[IPerfumeCatalogService.cs] ──> [PerfumeCatalogService.cs]
      │
      ├── 1. Lấy tập dữ liệu Products (kèm Brand, FragranceFamily, FragranceNotes, ProductVariants)
      ├── 2. Chấm điểm ma trận (Gender + Scent Family + Occasion + Intensity + Budget + Rating)
      ├── 3. Tính toán Match Score (%) & Trích xuất Top 4 sản phẩm điểm cao nhất
      ├── 4. Tổng hợp Scent Archetype, Phân tích phong cách & Lời giải thích AI riêng
      │
      ▼
[QuizRecommendationResultDto]
      │
      ▼
[Client Response & Luxury Scent Dossier Rendering]
```

---

## 3. Danh Sách Tác Vụ Triển Khai (Vertical Slices)

### Tác Vụ 1: Nâng Cấp Hợp Đồng Dữ Liệu (DTOs & Interface)
- Mở rộng `QuizAnswerRequest`: Thêm `IntensityPreference` và `BudgetPreference`.
- Tạo `QuizProductRecommendationDto`: Kèm `MatchScore`, `MatchReason`, `Concentration`, `FamilyName`, `TopNotes`.
- Tạo `QuizRecommendationResultDto`: Kèm `ArchetypeTitle`, `ArchetypeSubtitle`, `ArchetypeDescription`, `DominantAccords`, `KeyNotes`, `Items`.
- Cập nhật `IPerfumeCatalogService.cs`.

### Tác Vụ 2: Xây Dựng Động Cơ Đề Xuất & Chấm Điểm AI (Backend Engine)
- Triển khai thuật toán tính điểm trọng số `Hybrid Scent Matrix Engine` trong `PerfumeCatalogService.cs`.
- Tích hợp bảng ánh xạ `Scent Archetype Generator` (sinh danh phận mùi hương từ tổ hợp answers).
- Sinh lời giải thích AI tự động cho từng chai nước hoa.
- Đảm bảo fallback thông minh nếu sản phẩm có điểm số gần nhau.

### Tác Vụ 3: Thiết Kế Giao Diện Trắc Nghiệm 5 Bước & Hồ Sơ Dossier (Frontend UI)
- Tái cấu trúc `src/Perfume.Web/Views/Catalog/Quiz.cshtml`:
  - 5 bước câu hỏi sang trọng với icon FontAwesome Pro / Luxury Gold.
  - Thanh tiến trình động 20% -> 100%.
  - Nút "Quay lại" (Back) mượt mà cho từng bước.
  - Hiệu ứng chuyển động (Fade-in / Slide transition).
- Xây dựng giao diện `Luxury Scent Dossier`:
  - Khung viền mạ vàng Champagne sang trọng.
  - Huy hiệu Archetype & phân tích nốt hương.
  - Thẻ sản phẩm với Badge Match Score (%) phát sáng vàng.
  - Hộp "Lý do AI tuyển chọn cho bạn".
  - Nút Thêm vào giỏ hàng AJAX, Xem chi tiết, Làm lại, và Sao chép link chia sẻ.

### Tác Vụ 4: Kiểm Thử Trực Quan & Xác Minh Toàn Diện
- Build dự án và khởi chạy ứng dụng.
- Dùng Chrome DevTools MCP thực hiện trắc nghiệm thực tế, kiểm tra render dossier, tính toán Match Score, thêm giỏ hàng.
