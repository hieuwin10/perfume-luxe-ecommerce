# BMAD: CODING STANDARDS & NGUYÊN TẮC LẬP TRÌNH

> **Phân hệ:** Quy Chuẩn Kỹ Thuật & Hướng Dẫn Chú Thích Mã Nguồn  
> **Áp dụng cho:** Toàn bộ Repository Perfume Luxe (.NET 8, C# 12)  

---

## 1. Nguyên Tắc Kiến Trúc Sạch (Clean Architecture & SOLID)

1. **Phân Tầng Độc Lập**:
   - `Perfume.Domain`: Không phụ thuộc vào bất kỳ thư viện bên ngoài hay EF Core nào.
   - `Perfume.Application`: Chỉ chứa Interfaces, DTOs, Business Rules.
   - `Perfume.Infrastructure`: Triển khai truy vấn CSDL, migration, dịch vụ ngoài.
   - `Perfume.Web`: Đóng vai trò Presentation (MVC Controllers, Razor Views, REST API).

2. **Chống Truy Vấn N+1**:
   - Nghiêm cấm truy vấn CSDL lặp lại trong vòng for.
   - Luôn tải trước các khóa ngoại bằng `Include` hoặc gom danh sách ID cần thiết và truy vấn batch thành `Dictionary<int, Entity>`.

3. **Tối Ưu Change Tracker Với `.AsNoTracking()`**:
   - Mọi câu truy vấn dữ liệu chỉ phục vụ hiển thị màn hình hoặc trả về API DTO bắt buộc sử dụng `.AsNoTracking()`.

4. **Giao Dịch Đặt Hàng Serializable**:
   - Toàn bộ thao tác trừ tồn kho và tạo đơn hàng phải được bọc trong `IsolationLevel.Serializable` để tránh xung đột tranh chấp số lượng bán đồng thời.

---

## 2. Quy Định Bắt Buộc Về Chú Thích Code (Tiếng Việt Không Dấu)

Theo yêu cầu chuẩn hóa dự án:
- Toàn bộ chú thích (comments) trong mã nguồn C# (.cs) backend phải được viết bằng **tiếng Việt không dấu** (`tieng viet khong dau`).
- Phân đoạn code rõ ràng với tiêu đề chuẩn: `// ==========================================`.
- Mục đích: Đảm bảo tương thích tuyệt đối với mọi trình biên dịch, công cụ phân tích tĩnh, bảng mã terminal console và CI/CD runner không hỗ trợ UTF-8 locale.

### Ví Dụ Mẫu Hợp Chuẩn:
```csharp
// ==========================================
// 1. KHOI TAO SERVICE VA DEPENDENCY INJECTION
// ==========================================
// Khoi tao service voi DbContext va MemoryCache de tang toc do truy van

// ==========================================
// 2. XU LY DAT HANG TRANSACTION SERIALIZABLE
// ==========================================
// Kiem tra ton kho Flash Sale truoc khi tru suat mua hop le
// Bat dau transaction voi muc co lap Serializable de chong race condition
```
