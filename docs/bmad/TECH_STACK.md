# BMAD: CÔNG NGHỆ & NGĂN XẾP KỸ THUẬT (TECH STACK)

> **Phân hệ:** Quyết Định Công Nghệ & Khung Phát Triển  
> **Dự án:** Perfume Luxe  

---

## 1. Bảng Ngăn Xếp Công Nghệ (Technology Stack)

| Thành Phần | Công Nghệ Sử Dụng | Phiên Bản | Lý Do Lựa Chọn & Giá Trị Kỹ Thuật |
|---|---|---|---|
| **Nền tảng / Runtime** | .NET (C# 12) | 8.0 LTS | Hiệu năng cao, an toàn bộ nhớ, hỗ trợ dài hạn (LTS), đa nền tảng (Linux & Windows). |
| **Kiến trúc ứng dụng** | Clean / Onion Architecture | - | Tách biệt hoàn toàn Business Logic khỏi UI và CSDL, dễ mở rộng và kiểm thử độc lập. |
| **Giao diện Web** | ASP.NET Core MVC + Razor | 8.0 | Server-side rendering chuẩn SEO, tốc độ tải trang ban đầu cực nhanh, trải nghiệm mượt mà. |
| **ORM / Truy xuất dữ liệu**| Entity Framework Core | 8.0.8 | Type safety với LINQ, hỗ trợ DbContext pooling, tối ưu câu lệnh SQL sinh ra. |
| **Hệ quản trị CSDL** | Microsoft SQL Server / Azure SQL | 2022 | Hỗ trợ giao dịch ACID chặt chẽ, khóa cấp độ dòng (Row-level lock) và B-Tree Indexing. |
| **Bộ nhớ đệm trong RAM** | `Microsoft.Extensions.Caching.Memory` | 8.0 | Tăng tốc độ truy xuất các bảng phân loại tĩnh (Taxonomy) với độ trễ 0ms. |
| **Kiểm thử tự động** | xUnit + Moq + EF Core InMemory | - | 411 bài kiểm thử tự động toàn diện, tốc độ chạy ~2 giây. |
| **Đóng gói & Triển khai** | Docker & Docker Compose | Multi-stage | Đóng gói môi trường đồng nhất, dễ dàng triển khai lên VPS, Azure hoặc Kubernetes. |
