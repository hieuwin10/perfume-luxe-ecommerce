# Project: Perfume Luxe E-Commerce - Backend Refactoring & Streamlining

## Architecture
- **Framework**: .NET 8 LTS Clean Architecture.
- **Tầng dự án**:
  - `src/Perfume.Domain`: Thực thể cốt lõi, Enums, BaseEntity, Domain rules. Không phụ thuộc thư viện ngoài.
  - `src/Perfume.Application`: Interfaces dịch vụ, DTOs, CQRS/Service contracts, Validators, Helpers. Phụ thuộc Domain.
  - `src/Perfume.Infrastructure`: Triển khai dịch vụ, EF Core `PerfumeDbContext`, Migrations/SchemaUpgrader, Identity `AppUser`, Caching, Email. Phụ thuộc Application & Domain.
  - `src/Perfume.Web`: Presentation layer (Controllers, APIs, Middlewares, Program.cs). Phụ thuộc Application & Infrastructure. **Ràng buộc tuyệt đối**: Bảo toàn 100% giao diện frontend (.cshtml, .css, .js).
  - `tests/Perfume.Tests`: Bộ kiểm thử hồi quy gồm 23 test classes, 411 unit & integration tests trên EF Core InMemory.

## Feature Inventory
Bảng danh mục toàn bộ các hạng mục tinh gọn phát hiện từ giai đoạn Khảo sát (Survey Phase):

| # | Feature | Description | Milestone | Source |
|---|---|---|---|---|
| 1 | Domain Entity De-bundling | Tách 4 tệp thực thể nguyên khối (CatalogEntities, EcommerceEntities, AdminEntities, CustomerEntities) thành từng tệp riêng lẻ theo thực thể. Giữ nguyên namespace `Perfume.Domain.Entities`. | M1 | Explorer 1 |
| 2 | Domain & Application Comments Standardization | Chuyển đổi toàn bộ 37 dòng comment có dấu tại Domain (32) và Application (5) sang tiếng Việt không dấu (tieng viet khong dau). | M1 | Explorer 1 |
| 3 | Interface Segregation in Application | Tách `IOrderService` ra khỏi `IPerfumeCatalogService.cs`; tách `ICustomerAccountService`, `ILoyaltyTierService`, `IDiscoveryBoxService` ra khỏi `ILoyaltyAndDiscoveryServices.cs`. Di chuyển DTOs từ `IDatabaseConsoleService.cs` về thư mục DTOs. | M2 | Explorer 1 |
| 4 | DTO Deduplication & Unification | Tạo `BaseProductDto` cho `CreateProductDto` và `EditProductDto` kế thừa; cho `QuizProductRecommendationDto` kế thừa `ProductCardDto`. Di chuyển các Store DTOs từ Web Controllers về `Application/DTOs`. | M2 | Explorer 1 |
| 5 | Shared Utilities Consolidation | Tạo `SlugHelper` chuẩn hóa tiếng Việt dùng `NormalizationForm.FormD` thay thế 4 thuật toán sinh slug phân tán. Gom hằng số freeship/shipping fee vào `CommercePolicyDefaults`. | M2 | Explorer 1, 2 |
| 6 | SystemSettings Memory Caching | Thêm `IMemoryCache` (60s) vào `SystemSettingService.GetSettingsAsync()` để loại bỏ truy vấn DB lặp lại trên mọi HTTP request từ `MaintenanceMiddleware`. | M3 | Explorer 2 |
| 7 | N+1 Query Elimination in PlaceOrderAsync | Batch load Products và Variants trong `OrderService.PlaceOrderAsync` trước vòng lặp trong Serializable transaction. | M3 | Explorer 2 |
| 8 | N+1 Query Elimination in AdminSystemController | Thay thế vòng lặp `GetRolesAsync` đơn lẻ trong `AdminSystemController.Users` bằng 1 truy vấn join duy nhất. | M3 | Explorer 2 |
| 9 | Memory Allocation Streaming in Dashboard Analytics | Chuyển các phép tính tổng, trung bình, đếm trong `AdminDashboardAnalyticsService` sang LINQ-to-Entities trên SQL Server thay vì load toàn bộ Orders vào RAM. | M3 | Explorer 2 |
| 10 | Memory & Query Optimization in Inventory & Catalog | Thêm `AsNoTracking()` cho các truy vấn read-only; áp dụng `ExecuteDeleteAsync()` cho `ClearErrorLogs`; áp dụng projection `.Select()` cho Quiz recommendations. | M3 | Explorer 1, 2 |
| 11 | Dead Code Elimination | Xóa bỏ controller rác `src/Perfume.Web/Areas/Admin/Controllers/DashboardController.cs` (không có View, không có caller). | M4 | Explorer 2 |
| 12 | Controller Boilerplate Deduplication | Gom hàm `ValidateCookieWriteAsync` trùng lặp trên 7 controllers thành extension method; gom hàm `GetOwnerKey` dùng chung. | M4 | Explorer 2 |
| 13 | Web Comments Standardization | Chuyển đổi 27 dòng comment có dấu tại `src/Perfume.Web` sang tiếng Việt không dấu (tieng viet khong dau). | M4 | Explorer 2 |
| 14 | Adapter Shims Preservation | Giữ lại các adapter cần thiết (`AdminDashboardController`, `ApiV1ExtensionsController`, `LoyaltyTierService`) để bảo vệ 411/411 tests không bị phá vỡ. | M4 | Explorer 1, 2, 3 |
| 15 | Regression Testing & CodeRabbit Review | Chạy toàn bộ 411 tests trong `tests/Perfume.Tests` (yêu cầu 100% pass), kiểm tra `git status` (0 file frontend bị sửa), và rà soát mã nguồn qua CodeRabbit CLI. | M5 | Explorer 3 |

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|---|---|---|---|
| M1 | Domain De-bundling | Tách tệp thực thể Domain theo chuẩn per-entity | None | DONE (24 entities, C# 12 `[...]`, 411 tests passed) |
| M2 | Application Refactoring & DTO Deduplication | Tách interfaces vi phạm ISP, kế thừa DTOs, SlugHelper, CommercePolicyDefaults | Independent (Parallel) | DONE (Interfaces split, DTOs deduplicated, 411 tests passed) |
| M3 | Caching & Query Performance Optimization | Caching SystemSettings (60s), khử N+1 PlaceOrderAsync, tối ưu AI Quiz | Independent (Parallel) | DONE (IMemoryCache, batch queries, 411 tests passed) |
| M4 | Web Layer Streamlining & Dead Code Removal | Xóa DashboardController, khử N+1 Users, ExecuteDeleteAsync, ValidateCookieWriteAsync | Independent (Parallel) | DONE (DashboardController removed, SQL aggregates, 411 tests passed) |
| M5 | Quality Assurance, CodeRabbit & Somee Deployment | Chạy 411/411 tests, CodeRabbit review, commit git, deploy Somee & live verification | M1, M2, M3, M4 | DONE (all reviews APPROVE, audit CLEAN, commit b651030, Somee HTTP 200 OK) |

## Interface Contracts
- Giữ nguyên toàn bộ public class names, property names, and method signatures được sử dụng bởi các tests hiện có.
- Namespace `Perfume.Domain.Entities` giữ nguyên khi tách file.
- Namespace `Perfume.Application.Interfaces` giữ nguyên khi tách file.
- Tuyệt đối không thay đổi kiểu trả về của các Controller action mà frontend Razor views hoặc API tests phụ thuộc.

## Code Layout
- `src/Perfume.Domain/Entities/*.cs`: Mỗi entity một file riêng biệt.
- `src/Perfume.Application/Interfaces/*.cs`: Mỗi interface một file riêng biệt.
- `src/Perfume.Application/DTOs/*.cs`: Các DTOs gom nhóm theo domain (Catalog, Customer, Order, System).
- `src/Perfume.Application/Common/*.cs`: Shared helpers (SlugHelper, Constants).
- `src/Perfume.Infrastructure/Services/*.cs`: Implementations của các application interfaces.
- `src/Perfume.Web/`: Chỉ sửa đổi backend C# controllers/services/middlewares. KHÔNG chạm vào `Views/`, `wwwroot/css/`, `wwwroot/js/`.
