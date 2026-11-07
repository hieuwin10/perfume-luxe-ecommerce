# BÁO CÁO ĐO LƯỜNG HIỆU NĂNG LATENCY API & QUERY SQL (07/11/2026)
## Môi trường: Live Somee Cloud (`https://perfumeluxe.somee.com`)

---

### 1. TỔNG QUAN KẾT QUẢ KIỂM THỬ

- **Thời điểm đo lường:** 07/11/2026 09:30 AM
- **Tổng số mục kiểm thử:** 32 endpoint & truy vấn SQL
- **Ngưỡng cảnh báo hiệu năng:** `> 1.0 giây (1000 ms)`
- **Tỷ lệ đạt chuẩn (< 1.0s):** **93.75%** (30/32 mục đạt thời gian phản hồi rất nhanh từ 220ms - 830ms)
- **Số mục vượt ngưỡng (> 1.0s):** **2 mục** (6.25%)

---

### 2. BẢNG CHI TIẾT CÁC MỤC VƯỢT QUÁ 1.0 GIÂY (> 1000ms)

| STT | Phương Thức | Endpoint / Thao Tác | Mã HTTP | Thời Gian Phản Hồi | Phân Loại | Nguyên Nhân Kỹ Thuật |
|:---:|:---:|:---|:---:|:---:|:---:|:---|
| **1** | `GET` | **`/`** (Trang chủ Storefront - Cold Load) | `200 OK` | **`1,108.3 ms`** (~1.11s) | SSR Page | Lần đầu nạp (cold cache) trang chủ render HTML dung lượng lớn (~83.5 KB) với nhiều truy vấn nạp dữ liệu: Banner Hero Base64, Bộ sưu tập nổi bật, Flash Sale, Best Sellers và Testimonials. *(Khi cache ấm, thời gian giảm xuống còn 720ms - 760ms)*. |
| **2** | `GET` | **`/admin/admindashboard`** (Dashboard Quản trị viên) | `200 OK` | **`1,487.6 ms`** (~1.49s) | Admin Portal | Action `Index()` thực thi đồng thời chuỗi 7 truy vấn SQL thống kê nặng trên CSDL dùng chung Somee: Tổng doanh thu, Số lượng đơn hàng, Tổng khách hàng, Biểu đồ doanh thu 30 ngày (Chart.js), Top 5 sản phẩm bán chạy, Bảng đơn hàng gần nhất và Kiểm tra CSDL. |

---

### 3. KẾT QUẢ CHI TIẾT CÁC NHÓM API & QUERY SQL

#### 3.1. Nhóm RESTful APIs & Storefront Pages (Đều đạt < 1.0s)
- `GET /api/v1/cart`: **`231.5 ms`** (Cực nhanh)
- `GET /api/v1/catalog/products/chanel-coco-mademoiselle-edp`: **`292.6 ms`**
- `GET /swagger/v1/swagger.json`: **`292.9 ms`**
- `GET /catalog/quiz`: **`290.5 ms`**
- `GET /discovery-box`: **`316.3 ms`**
- `GET /api/v1/orders/track`: **`331.7 ms`**
- `POST /api/v1/vouchers/validate`: **`345.9 ms`**
- `GET /api/v1/catalog/products`: **`413.6 ms`**
- `GET /nuoc-hoa/chanel-coco-mademoiselle-edp`: **`427.7 ms`**
- `GET /so-sanh`: **`456.2 ms`**
- `GET /blog`: **`471.1 ms`**
- `GET /bo-suu-tap`: **`488.5 ms`**
- `GET /api/v1/catalog/products?keyword=Chanel`: **`834.0 ms`** (Truy vấn LIKE tìm kiếm chuỗi không phân biệt hoa thường)

#### 3.2. Nhóm Các Câu Lệnh SQL Trực Tiếp (Database Console Query Runner)
Tất cả các câu truy vấn SQL thực tế trên máy chủ Somee SQL Server đều phản hồi trong khoảng **`525ms - 607ms`**, không có câu nào vượt quá 1.0s:
1. `SELECT COUNT(*) AS TotalProducts FROM Products`: **`607.9 ms`**
2. `SELECT TOP 10 Id, Name, Sku, BasePrice, BrandId FROM Products`: **`525.3 ms`**
3. `SELECT TOP 10 o.Id, o.OrderCode, COUNT(i.Id) FROM Orders ...`: **`526.7 ms`**
4. `SELECT SUM(v.StockQuantity * v.SalePrice) FROM ProductVariants v`: **`537.4 ms`**
5. `Complex Multi-Join: Products + Brands + Categories + FragranceFamilies`: **`538.8 ms`**

---

### 4. ĐỀ XUẤT TỐI ƯU HÓA TUẦN TỚI

1. **Tối ưu Dashboard Quản trị (`/admin/admindashboard`)**:
   - Sử dụng `Task.WhenAll(...)` để chạy bất đồng bộ song song các tác vụ truy vấn thống kê thay vì tuần tự `await` từng câu.
   - Thêm `IMemoryCache` thời lượng 2 - 5 phút cho biểu đồ 30 ngày và thống kê doanh thu nhằm giảm tải áp lực cho SQL Server Somee.
2. **Tối ưu Trang Chủ (`/`)**:
   - Tối ưu thời lượng OutputCache (đặt `ExpireDuration = 300 giây`) để loại bỏ hoàn toàn các lần giật độ trễ cold-start.

