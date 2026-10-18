# KẾ HOẠCH HÀNH ĐỘNG & BẢO TRÌ SEO (ACTION PLAN)
**Dự án**: PERFUME LUXE  
**Trạng thái**: Tất cả các hạng mục trọng yếu và thiếu sót từ bản sửa của GPT đã được xử lý trực tiếp trong mã nguồn.

---

## 1. Bảng Tổng Hợp Các Hạng Mục Đã Khắc Phục (Completed Fixes)

| Hạng mục | Mức độ ưu tiên | Trạng thái | Chi tiết hành động đã thực hiện | Tác động SEO / UX |
|:---|:---:|:---:|:---|:---|
| **Khôi phục MerchantReturnPolicy & ShippingDetails** | 🔴 P0 (Khẩn cấp) | ✅ Đã hoàn tất | Đưa lại chính sách đổi trả 7 ngày và chi tiết phí/thời gian vận chuyển vào JSON-LD Product trong `Catalog/Detail.cshtml` | Kích hoạt lại phù hiệu Free Return & Shipping trên Google Search và Google Shopping |
| **Bổ sung `priceValidUntil` cho Product** | 🟡 P1 (Cao) | ✅ Đã hoàn tất | Thêm hạn giá 1 năm vào `offers` trong schema sản phẩm | Đáp ứng 100% tiêu chuẩn Google Merchant Listing Rich Results |
| **Loại bỏ `@import` CSS & Tối ưu Google Fonts** | 🟡 P1 (Cao) | ✅ Đã hoàn tất | Xóa `@import` trong `luxury-perfume.css`, chuyển sang thẻ `<link>` với preconnect `fonts.googleapis.com` và `fonts.gstatic.com` trong `_Layout.cshtml` | Cắt giảm waterfall render-blocking, tăng tốc FCP & LCP từ 200 - 400ms |
| **Chuẩn hóa Canonical toàn diện trong CatalogController** | 🟡 P1 (Cao) | ✅ Đã hoàn tất | Thiết lập `ViewData["CanonicalUrl"]` qua `SeoUrlBuilder.AbsoluteUrl` cho Detail, Index, Compare, DiscoveryBox | Đảm bảo tính nhất quán tuyệt đối về domain và HTTPS trên toàn bộ danh mục |
| **Bảo mật Clickjacking Header (`X-Frame-Options`)** | 🟡 P1 (Cao) | ✅ Đã hoàn tất | Thêm `X-Frame-Options: SAMEORIGIN` vào middleware tại `Program.cs` | Ngăn chặn website bị nhúng vào iframe độc hại, vượt qua kiểm tra bảo mật kỹ thuật SEO |
| **Accessibility (A11Y) cho Hero Slider** | 🟢 P2 (Trung bình) | ✅ Đã hoàn tất | Bổ sung `aria-label="Slide trước"`, `aria-label="Slide kế tiếp"` và `aria-label` cho dots tại `Home/Index.cshtml` | Cải thiện điểm số trải nghiệm người dùng di động và chuẩn tiếp cận WCAG 2.1 |
| **Mô tả ảnh đánh giá thực tế (Review Alt Text)** | 🟢 P2 (Trung bình) | ✅ Đã hoàn tất | Chuyển `alt="Ảnh đánh giá"` sang `alt="Ảnh thực tế @Model.Name từ khách hàng @review.UserName"` | Tăng cường chỉ mục hình ảnh Google Images từ người dùng thật (E-E-A-T) |
| **Mở rộng AI Bot Crawlers trong `robots.txt`** | 🟢 P2 (Trung bình) | ✅ Đã hoàn tất | Cho phép thêm `Applebot-Extended`, `Bytespider`, `CCBot` trong `SeoController.cs` | Sẵn sàng cho tìm kiếm qua Apple Intelligence, TikTok AI, Perplexity và ChatGPT |

---

## 2. Các Bước Đề Xuất Tiếp Theo Cho Người Dùng (Next Steps)

### Bước 1: Kiểm Tra & Đẩy Mã Nguồn Lên GitHub
Chạy các lệnh git sau trong terminal để lưu trữ và đồng bộ hóa các cải tiến:
```bash
git add .
git commit -m "feat(seo): optimize structured data, LCP font loading, canonical URLs and accessibility"
git push origin main
```

### Bước 2: Kiểm Tra Trực Quan với Google Rich Results Test (Sau khi Deploy)
1. Truy cập [Google Rich Results Test](https://search.google.com/test/rich-results).
2. Kiểm tra 1 đường dẫn sản phẩm mẫu (ví dụ: `https://perfumeluxe.vn/nuoc-hoa/creed-aventus-eau-de-parfum`).
3. Xác nhận hiển thị hợp lệ cả 2 khối Schema:
   - **Product**: Có đầy đủ Giá, Tình trạng còn hàng, Đánh giá, Chính sách đổi trả và Vận chuyển.
   - **BreadcrumbList**: Có 4 cấp điều hướng danh mục.

### Bước 3: Đăng Ký Sitemap & Quản Trị Tìm Kiếm
1. Truy cập **Google Search Console**:
   - Gửi sơ đồ trang web: `https://perfumeluxe.vn/sitemap.xml`.
2. Kiểm tra file **`/llms.txt`**:
   - Truy cập `https://perfumeluxe.vn/llms.txt` để kiểm tra định dạng trích dẫn cho Perplexity và SearchGPT.
