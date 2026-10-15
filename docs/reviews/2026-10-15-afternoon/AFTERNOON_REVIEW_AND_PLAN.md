# Rà soát kinh doanh, vận hành và kế hoạch chiều — Perfume Luxe

**Ngày:** 15/10/2026 (Asia/Saigon)\
**Phạm vi:** rà soát mã nguồn ở commit `b7e3899`, kiểm tra mới trong phiên chiều; không sử dụng kết quả hay báo cáo của phiên test buổi trưa.\
**Môi trường:** ASP.NET Core + LocalDB cô lập; không dùng Docker.

## Tóm tắt cho quyết định

Dự án có độ phủ tính năng thương mại điện tử rộng và bộ kiểm thử đang xanh. Tuy nhiên, chưa có dữ liệu kinh doanh thật để kết luận về doanh thu/chuyển đổi; một số rủi ro về tài khoản khởi tạo, dữ liệu Flash Sale và ánh xạ EF cần xử lý trước khi đưa cho khách thật. Lượt chạy LocalDB mới không có sản phẩm seed nên chưa thể xác nhận trải nghiệm mua hàng bằng dữ liệu hoàn chỉnh. Khuyến nghị ưu tiên bảo mật và độ tin cậy dữ liệu, sau đó chạy kịch bản mua hàng end-to-end trên dữ liệu test rõ ràng.

## Kiểm chứng mới trong phiên chiều

| Hạng mục | Kết quả |
|---|---|
| `dotnet test PerfumeEcommerce.sln --no-restore` | 404 passed, 0 failed, 0 skipped |
| `dotnet build PerfumeEcommerce.sln --no-restore --configuration Release` | Thành công, 0 warning, 0 error |
| Release tests (`--no-build`) | 404 passed, 0 failed, 0 skipped |
| Khởi động ASP.NET với database LocalDB tạm | Thành công; migrations chạy được trên database cô lập |
| HTTP probe | 17 URL: 11 × 200, 3 × 302, 2 × 401 (đúng với route yêu cầu đăng nhập), 1 × 404 |
| `/blog` và `/Blog/Index` | `/blog` trả 200, alias `/Blog/Index` trả 404; cần thống nhất canonical URL hoặc thêm alias nếu có liên kết cũ |
| `/api/v1/catalog/products` trên DB mới | HTTP 200 nhưng danh sách trống; thiếu seed catalog trong môi trường sạch |
| Dọn môi trường | Đã dừng ASP.NET và LocalDB; đã xóa database scratch; không tạo đơn hàng hay thay đổi PerfumeDb |

Kiểm tra qua trình duyệt Chrome không hoàn tất do chính sách URL của phiên Computer Use chặn điều khiển cửa sổ trình duyệt Windows. Vì vậy, HTTP GET không thay thế cho kiểm tra tương tác giao diện. Bộ kiểm tra file PowerPoint phát hiện vài đường/shape tràn khỏi khung; cùng vị trí cảnh báo đã có trong template gốc, và không thấy nội dung bị cắt khi render từng slide.

## Nhận định kinh doanh

### Điểm thuận lợi

- Có nền tảng cho catalog, tài khoản khách hàng, giỏ hàng/đặt hàng, theo dõi đơn, đánh giá, blog và công cụ quản trị; phạm vi phù hợp để dựng một pilot bán hàng.
- 404 unit/integration tests pass và ứng dụng khởi động được trên schema mới là tín hiệu tốt về tính ổn định kỹ thuật của build hiện tại.
- REST API customer và giao diện storefront tạo cơ sở để sau này tích hợp thêm kênh bán hàng.

### Khoảng trống trước pilot thương mại

- Không có số liệu phiên truy cập, tìm kiếm, thêm giỏ, checkout, đơn giao thành công, hoàn/hủy, chi phí giao hàng, giá vốn hay voucher sử dụng. Chưa thể tính conversion, AOV, biên lợi nhuận hoặc hiệu quả khuyến mãi.
- Cần đo funnel từ xem sản phẩm → thêm giỏ → bắt đầu checkout → đặt đơn → giao thành công; phân tách guest/customer và nguồn truy cập.
- Flash Sale cần lấy lượng bán từ đơn hợp lệ hoặc được ghi rõ là demo. Ghi số lượng đã bán giả vào dữ liệu sản phẩm làm sai tín hiệu khan hiếm và báo cáo.
- Trước pilot nên chốt nhóm khách hàng mục tiêu, sản phẩm chủ lực, chính sách đổi trả/giao hàng và ngưỡng lãi đóng góp tối thiểu cho từng đơn.

## Phát hiện kỹ thuật theo mức ưu tiên

| Mức | Phát hiện và bằng chứng | Tác động | Hướng xử lý |
|---|---|---|---|
| P1 | `src/Perfume.Web/appsettings.Development.json` chứa cấu hình thông tin đăng nhập admin mặc định. `Program.cs:79-102` có luồng cấp tài khoản; nếu email admin đã tồn tại thì ứng dụng tạo reset token và đặt lại mật khẩu từ cấu hình mỗi lần khởi động. | Rủi ro truy cập trái phép; vận hành có thể vô tình thay mật khẩu admin khi restart. | Gỡ thông tin xác thực khỏi source; bootstrap một lần từ secret local; không reset mật khẩu user đã tồn tại khi app khởi động. Thay secret nếu đã từng chia sẻ hoặc deploy. |
| P1 | `DatabaseSchemaUpgrader.cs:287-301` thêm dữ liệu `SoldQuantity` mẫu cho Flash Sale khi migration chạy và có sản phẩm. | Hiển thị bằng chứng xã hội/độ khan hiếm không dựa trên giao dịch thật; làm lệch dashboard. | Chuyển dữ liệu demo sang seed chỉ chạy ở môi trường Development/test; production tính từ đơn hợp lệ. |
| P2 | EF cảnh báo tại startup: `FragranceNote` có global query filter nhưng quan hệ bắt buộc với `ProductFragranceNote`; `Order.GiftWrappingFee` và `StockAdjustment.UnitCost` thiếu precision rõ ràng. | Lọc có thể ẩn dữ liệu liên quan ngoài dự kiến; tiền/giá vốn có nguy cơ bị làm tròn hoặc cắt khi lưu. | Quyết định quan hệ/filter và cấu hình precision theo đơn vị tiền tệ; bổ sung kiểm tra migration và round-trip tiền. |
| P2 | `/blog` hoạt động còn `/Blog/Index` trả 404; route `blog/{slug}` được đăng ký trước route mặc định trong `Program.cs:118-124`. | Link cũ hoặc URL do người dùng nhập có thể gãy. | Chọn một URL chuẩn, thêm redirect/alias có chủ đích và kiểm tra link nội bộ/canonical. |
| P2 | Data Protection key ghi xuống disk không mã hóa ở cấu hình Development; SMTP tắt mặc định. | Cần cấu hình key persistence/encryption và email thực tế trước môi trường triển khai; hiện chưa chứng minh email đặt hàng. | Lưu key an toàn theo môi trường; cấu hình SMTP/secret và kiểm tra Mailpit/local mail + lỗi SMTP không làm mất đơn. |
| P2 | Database sạch khởi tạo thành công nhưng catalog API trả danh sách rỗng. | Demo/pilot mới có thể lên trang nhưng không có sản phẩm để mua. | Tạo seed catalog dùng cho demo/test có thể lặp an toàn; tách rõ demo seed khỏi dữ liệu production. |

## Kế hoạch hoàn thiện trong buổi chiều

| Khung giờ mục tiêu | Việc cần làm | Điều kiện hoàn tất |
|---|---|---|
| 14:15–15:00 | Xử lý P1 admin credential và luồng reset khi startup; rà lại secrets đã commit. | App không chứa mật khẩu mặc định, restart không đổi mật khẩu admin; hướng dẫn cấu hình local rõ ràng. |
| 15:00–15:45 | Chặn số liệu Flash Sale demo ở production; xử lý EF precision và quan hệ filter; thống nhất URL Blog. | Seed demo chỉ ở môi trường phù hợp; cảnh báo mapping được giải quyết hoặc ghi nhận có chủ đích; URL blog nhất quán. |
| 15:45–16:45 | Chạy luồng ASP.NET trên LocalDB sạch và dữ liệu demo: guest/customer, giỏ → COD → trạng thái đơn, RBAC, voucher/tồn kho, email và đánh giá. | Có kết quả cho từng luồng; test thất bại được sửa hoặc ghi lại rõ ràng; không dùng database local thật. |
| 16:45–17:15 | Chạy lại Release build/tests, rà diff và secret scan thủ công, kiểm tra trạng thái Git. | Build/test xanh; chỉ các file dự kiến được đưa vào thay đổi. |
| Cuối ngày | Push nhánh/PR sau khi xem diff và xác nhận nội dung phát hành. | Không push report buổi trưa; không đưa dữ liệu test/secret vào commit. |

*Đây là lịch mục tiêu, không phải cam kết các mục sửa đã hoàn thành. Chỉ có build/test và smoke run được xác nhận trong lượt hiện tại; các mục xử lý và luồng end-to-end cần thực hiện tiếp.*

## Ranh giới đánh giá

- Đây là đánh giá source và smoke checks trên máy phát triển, không phải pentest hay xác nhận sẵn sàng production.
- Không có dữ liệu bán hàng thật nên các khuyến nghị kinh doanh là giả thuyết cần đo, không phải dự báo doanh thu.
- Không kiểm tra Chrome UI, email SMTP ngoài môi trường local, hay giao dịch thật.
- Blog vẫn công khai; CMS bài viết/SEO nâng cao giai đoạn sau. Quiz không nằm trong đợt này.
- Không sử dụng kết quả phiên test buổi trưa.
