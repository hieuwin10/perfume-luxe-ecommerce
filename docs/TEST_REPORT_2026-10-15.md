# Báo cáo kiểm thử ASP.NET — 2026-10-15

## Phạm vi

- Repo: `Desktop\dotnet`, nhánh `main`, commit `b7e3899`.
- Không dùng Docker. Web chạy bằng .NET 8 và LocalDB có sẵn (`MSSQLLocalDB`).
- `PerfumeDb` đã tồn tại với schema marker `20261012_02_tastings_and_alerts`; các role hệ thống đã có. Ứng dụng chỉ được gọi bằng các GET đọc dữ liệu; không tạo tài khoản, thay đổi giỏ, đặt đơn, hay gửi đánh giá.
- Sau kiểm tra, tiến trình ASP.NET đã dừng và LocalDB được trả về trạng thái dừng ban đầu.

## Kết quả

| Kiểm tra | Kết quả |
|---|---|
| `dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:minimal` | **404 pass, 0 fail, 0 skip** (4 giây) |
| Build các project Domain, Application, Infrastructure, Web và Tests | Thành công trong lượt `dotnet test`, tất cả target `net8.0` |
| Trang chủ `/` | 200 |
| Catalog `/bo-suu-tap` và lọc từ khóa | 200 |
| Chi tiết sản phẩm `/nuoc-hoa/le-labo-santal-33` | 200 |
| Blog `/blog` và bài viết mẫu | 200 |
| Giỏ `/cart`; checkout khi giỏ trống `/checkout` | 200; checkout đưa về giỏ trống, không tạo đơn |
| Đăng nhập, đăng ký, quên mật khẩu | 200 |
| Tra cứu đơn hàng, sitemap, robots | 200 |
| API danh sách/chi tiết sản phẩm và danh sách đánh giá | 200 |
| API hồ sơ và đơn hàng khi chưa đăng nhập | 401 như kỳ vọng |
| URL `/Blog/Index` | 404; URL điều hướng chính `/blog` hoạt động bình thường |

## Vấn đề phát hiện

1. **P3 — Alias blog kiểu MVC trả 404.** `/Blog/Index` bị route `blog/{slug}` nhận thành slug `Index`; route này được đăng ký trước route mặc định trong `src/Perfume.Web/Program.cs` (khoảng dòng 120–124). Điều hướng `/blog` trong giao diện vẫn hoạt động. Nên khai báo route index rõ ràng hoặc giới hạn route chi tiết để tránh nhận `Index`.
2. **P2 — Chưa cấu hình độ chính xác rõ cho một số tiền.** `Order.GiftWrappingFee` và `StockAdjustment.UnitCost` là `decimal` nhưng chưa có `HasPrecision` trong `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs` (cấu hình Order quanh dòng 158; StockAdjustment quanh dòng 310). Nên đặt precision tiền tệ rõ ràng và kiểm tra schema trước khi thêm migration. `UserLoyalty.TotalSpent` hiện đã có precision 18,2.
3. **P2 — Bộ lọc soft-delete của nốt hương có thể ảnh hưởng quan hệ bắt buộc.** `FragranceNote` có global query filter, còn quan hệ bắt buộc `ProductFragranceNote.FragranceNote` không có filter tương ứng (`PerfumeDbContext.cs` quanh dòng 87 và 140). Khi nốt hương bị soft-delete, EF Core có thể lọc cả hàng liên kết trong truy vấn; cần quyết định đồng bộ filter hoặc làm navigation tùy chọn, rồi thêm test trường hợp này.

## Giới hạn

- Chưa gửi form đăng ký/đăng nhập, sửa hồ sơ, thêm/xóa giỏ, áp voucher, tạo đơn COD, hủy đơn hoặc gửi đánh giá trên database đang dùng. Đây là các thao tác ghi; chưa có bản database cô lập để chạy end-to-end an toàn.
- Lượt `dotnet test` đầu trong sandbox bị VSTest không kết nối được testhost sau 90 giây; chạy lại với quyền tiến trình được duyệt hoàn tất đủ 404 test. Không cần Docker.
- Không thay đổi source ứng dụng; chỉ thêm báo cáo này vào repo. Chưa commit file báo cáo.
