# KẾ HOẠCH HÀNH ĐỘNG & BẢO TRÌ SEO (ACTION PLAN)
**Dự án**: PERFUME LUXE  
**Trạng thái**: Tất cả các hạng mục trọng yếu đã được khắc phục trực tiếp trong mã nguồn.

---

## 1. Các Hạng Mục Đã Khắc Phục (Completed Actions)

| Hạng mục | Mức độ ưu tiên | Hành động đã thực hiện | Tác động |
|:---|:---:|:---|:---|
| **Canonical URL Cleanup** | P0 (Khẩn cấp) | Loại bỏ QueryString khỏi Canonical mặc định trong `_Layout.cshtml` | Loại bỏ nguy cơ trùng lặp hàng nghìn URL do tham số marketing |
| **Bổ sung XML Sitemap** | P0 (Khẩn cấp) | Thêm 8 landing pages tĩnh vào `SeoController.Sitemap()` | Đảm bảo Googlebot lập chỉ mục toàn diện các trang dịch vụ |
| **Robots.txt & Security** | P1 (Cao) | Thêm `Disallow: /swagger/` và mở quyền cho AI crawler | Bảo vệ API docs nội bộ, sẵn sàng cho AI Search |
| **Khai báo `/llms.txt`** | P1 (Cao) | Tạo action `LlmsTxt()` & map route tại `Program.cs` | Thúc đẩy hiển thị nguồn trích dẫn trên SearchGPT / Perplexity |
| **Product BreadcrumbList** | P1 (Cao) | Chèn schema `BreadcrumbList` vào `Catalog/Detail.cshtml` | Kích hoạt thanh breadcrumb đa tầng trên SERP Google |
| **Store / LocalBusiness** | P1 (Cao) | Khai báo schema 2 Flagship Store tại `Boutique.cshtml` | Nâng cao thứ hạng tìm kiếm địa phương (Hà Nội, TP.HCM) |
| **Đồng bộ Hotline NAP** | P2 (Trung bình) | Chuẩn hóa số điện thoại `+84-24-3828-9999` trong `Index.cshtml` | Thắt chặt uy tín thực thể (Knowledge Graph Entity) |
| **Meta Description các trang** | P2 (Trung bình) | Bổ sung meta description cho trang So Sánh, Discovery, Tasting | Cải thiện CTR tự nhiên từ kết quả tìm kiếm |
| **Code Simplification Sitemap** | P3 (Tối ưu mã) | Đơn giản hóa vòng lặp lấy danh mục & bài viết blog trong `SeoController` | Giảm độ phức tạp thuật toán, loại bỏ rác URL phân trang trong sitemap |

---

## 2. Danh Mục Khuyến Nghị Vận Hành Tiếp Theo (Operational Roadmap)

### Tháng 1: Đăng ký & Xác thực công cụ quản trị
1. **Google Search Console**:
   - Truy cập GSC và submit sơ đồ trang web: `https://perfumeluxe.vn/sitemap.xml`.
   - Giám sát mục "Trang (Pages)" để kiểm tra tỷ lệ index của các URL `/nuoc-hoa/*` và `/blog/*`.
2. **Google Business Profile**:
   - Tạo hồ sơ doanh nghiệp cho 2 cửa hàng (Tràng Tiền & Đồng Khởi) với tên và số điện thoại đồng bộ chính xác với Schema đã cấu hình.

### Hàng quý: Kiểm tra định kỳ (Quarterly Healthcheck)
- Chạy kiểm tra định kỳ Schema Rich Results Test của Google để cập nhật bất kỳ thay đổi thuộc tính nào.
- Giữ vững nguyên tắc cập nhật `Title` và `MetaDescription` độc bản mỗi khi tạo bài viết blog hoặc sản phẩm mới.
