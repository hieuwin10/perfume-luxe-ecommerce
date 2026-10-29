# BMAD PILLAR D: DATABASE & TRIỂN KHAI VẬN HÀNH

> **Phân hệ:** Cơ Sở Dữ Liệu, Migration Engine V1 - V6 & Hạ Tầng  
> **Dự án:** Perfume Luxe  

---

## 1. Cơ Chế Tự Động Nâng Cấp Schema (Migration Engine V1 - V6)

Dự án áp dụng cơ chế **Custom Additive Schema Migration Engine** thông qua `DatabaseSchemaUpgrader.cs`:

```csharp
private static readonly (string Version, string Sql)[] Migrations =
[
    (DatabaseSchemaMigrationV1.Version, DatabaseSchemaMigrationV1.Sql), // V1: Gio hang, Audit Log, Refresh Tokens
    (DatabaseSchemaMigrationV2.Version, DatabaseSchemaMigrationV2.Sql), // V2: Cau hinh cua hang, Banner, Ton kho
    (DatabaseSchemaMigrationV3.Version, DatabaseSchemaMigrationV3.Sql), // V3: Flash Sale, Loyalty VIP
    (DatabaseSchemaMigrationV4.Version, DatabaseSchemaMigrationV4.Sql), // V4: He thong log loi tap trung
    (DatabaseSchemaMigrationV5.Version, DatabaseSchemaMigrationV5.Sql), // V5: Seed du lieu bai viet SEO
    (DatabaseSchemaMigrationV6.Version, DatabaseSchemaMigrationV6.Sql)  // V6: He thong chi muc Indexing toc do cao
];
```

### Nguyên Tắc An Toàn:
1. **Lịch sử phiên bản độc lập**: Bảng `dbo.__PerfumeSchemaVersions` ghi nhận từng bản đã hoàn tất.
2. **Giao dịch SQL Transaction**: Mỗi bản migration chạy trong một transaction riêng biệt. Nếu xảy ra lỗi cú pháp hoặc ràng buộc, toàn bộ transaction tự động rollback, bảo vệ nguyên vẹn cấu trúc CSDL hiện hữu.
3. **Tự động sao lưu (Backup)**: Trước khi chạy migration, nếu biến `Database:BackupPath` được cấu hình, hệ thống sẽ tự động tạo file snapshot `.bak`.

---

## 2. Chiến Lược Đánh Chỉ Mục Hiệu Năng Cao (B-Tree Indexing - V6)

Để đảm bảo thời gian phản hồi dưới 15ms khi số lượng sản phẩm và đơn hàng tăng trưởng, các chỉ mục B-Tree Index chuyên biệt được áp dụng:

| Tên Chỉ Mục | Bảng | Cột Đánh Index | Mục Đích Tối Ưu Truy Vấn |
|---|---|---|---|
| `IX_Products_CreatedAt` | `Products` | `CreatedAt DESC` | Sắp xếp sản phẩm mới nhất mặc định tại trang chủ & catalog. |
| `IX_Products_BasePrice` | `Products` | `BasePrice` | Lọc khoảng giá `MinPrice - MaxPrice` và sắp xếp giá tăng/giảm. |
| `IX_Products_IsFeatured_CreatedAt` | `Products` | `IsFeatured, CreatedAt DESC` | Tải ngay 8 sản phẩm nổi bật trang chủ không cần quét bảng. |
| `IX_Products_IsBestSeller_CreatedAt` | `Products` | `IsBestSeller, CreatedAt DESC` | Tải nhanh danh sách sản phẩm bán chạy nhất. |
| `IX_Products_IsNewArrival_CreatedAt` | `Products` | `IsNewArrival, CreatedAt DESC` | Tải nhanh bộ sưu tập hàng mới về. |
| `IX_Products_BrandId` | `Products` | `BrandId` | Tối ưu hóa phép JOIN giữa bảng Products và Brands. |
| `IX_Products_CategoryId` | `Products` | `CategoryId` | Tối ưu hóa phép lọc nước hoa theo danh mục. |
| `IX_Products_FragranceFamilyId` | `Products` | `FragranceFamilyId` | Tối ưu hóa phép lọc nước hoa theo nhóm hương. |
| `IX_Orders_UserId_CreatedAt` | `Orders` | `UserId, CreatedAt DESC` | Xem lịch sử đơn hàng của khách hàng theo thời gian thực. |
| `IX_Orders_OrderStatus_CreatedAt` | `Orders` | `OrderStatus, CreatedAt DESC` | Quản trị viên lọc đơn theo trạng thái (Chờ duyệt, Đang giao). |
| `IX_OrderItems_OrderId` | `OrderItems` | `OrderId` | Tải chi tiết các món hàng theo mã đơn trong 1 round-trip. |
| `IX_Reviews_ProductId_CreatedAt` | `Reviews` | `ProductId, CreatedAt DESC` | Tải các bài đánh giá của nước hoa theo thứ tự mới nhất. |
| `IX_BlogPosts_IsPublished_CreatedAt` | `BlogPosts` | `IsPublished, CreatedAt DESC` | Tải danh sách bài viết blog đã xuất bản phân trang. |

---

## 3. Hạ Tầng Triển Khai Docker & Vận Hành

- **`Dockerfile`**: Sử dụng base image `mcr.microsoft.com/dotnet/aspnet:8.0` kết hợp `sdk:8.0` biên dịch qua Multi-stage build, giảm thiểu dung lượng container thành phẩm.
- **`docker-compose.yml`**: Khởi chạy đồng bộ ứng dụng web ASP.NET Core kèm máy chủ SQL Server 2022 Linux container hóa với volume lưu trữ bền vững (`docker_data`).

---

## 4. Hệ Thống Kiểm Thử Tự Động (Quality Assurance)

Dự án sở hữu bộ test toàn diện trong `tests/Perfume.Tests/`:
- **Số lượng kiểm thử**: **411 test cases** (100% Passed).
- **Công nghệ**: xUnit + Moq + EF Core InMemory Database.
- **Thời gian chạy**: ~2 giây.
