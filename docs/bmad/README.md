# PERFUME LUXE — BỘ TÀI LIỆU BMAD (BMAD DOCUMENTATION SUITE)

> **Tiêu chuẩn tài liệu:** BMAD Framework (**B**usiness — **M**odel — **A**rchitecture — **D**atabase)  
> **Dự án:** Perfume Luxe — Nền Tảng Thương Mại Điện Tử Nước Hoa Niche Cao Cấp  
> **Kiến trúc:** Clean Architecture (.NET 8 LTS + EF Core 8 + SQL Server)  
> **Trạng thái kiểm thử:** 411/411 Unit & Integration Tests (100% Passed)  

---

## Danh Mục Tài Liệu BMAD Tinh Gọn

Thư mục `bmad/` được tổ chức chuyên biệt, tinh gọn chỉ chứa toàn bộ hồ sơ kỹ thuật và tài liệu nghiệp vụ của hệ thống:

| Tài Liệu | Trụ Cột | Nội Dung Trọng Tâm |
|---|---|---|
| **[`BUSINESS.md`](./BUSINESS.md)** | **B - Business** | Định vị sản phẩm Haute Parfumerie, luồng khách hàng, AI Scent Quiz, Hộp quà Discovery Box, Flash Sale giờ vàng, Hạng thẻ VIP Loyalty. |
| **[`MODEL.md`](./MODEL.md)** | **M - Model** | Sơ đồ ERD thực thể quan hệ, danh mục Domain Entities, Enums hệ thống và DTOs trao đổi dữ liệu. |
| **[`ARCHITECTURE.md`](./ARCHITECTURE.md)** | **A - Architecture** | Kiến trúc Clean Architecture 4 tầng, Dual Auth (Cookie + JWT), Caching In-Memory, luồng giao dịch Checkout Serializable. |
| **[`DATABASE.md`](./DATABASE.md)** | **D - Database** | Cơ chế tự động nâng cấp Schema Migration V1-V6, chiến lược đánh chỉ mục B-Tree Indexing hiệu năng cao. |
| **[`EPICS_AND_STORIES.md`](./EPICS_AND_STORIES.md)** | **Quản lý Agile** | Phân rã 8 Epic chức năng, danh sách User Stories và Definition of Done (DoD). |
| **[`CODING_STANDARDS.md`](./CODING_STANDARDS.md)** | **Quy chuẩn Code** | Nguyên tắc Clean Code, SOLID, chống N+1 và quy định chú thích code bằng tiếng Việt không dấu. |
| **[`TECH_STACK.md`](./TECH_STACK.md)** | **Công nghệ** | Bảng công nghệ (.NET 8 C# 12, ASP.NET Core MVC, EF Core 8, SQL Server, xUnit, Moq). |

---

## Điều Hướng Nhanh

- Bắt đầu với **[Nghiệp Vụ Hệ Thống (`BUSINESS.md`)](./BUSINESS.md)** để nắm rõ toàn bộ tính năng và trải nghiệm mua sắm xa xỉ.
- Xem **[Kiến Trúc Kỹ Thuật (`ARCHITECTURE.md`)](./ARCHITECTURE.md)** để hiểu luồng Request-Response và bảo mật kép.
- Xem **[Cơ Sở Dữ Liệu (`DATABASE.md`)](./DATABASE.md)** để xem chi tiết các bản Migration V1 - V6 và B-Tree Index.
