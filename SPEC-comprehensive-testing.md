# Spec: Comprehensive Multi-Part Testing & TDD Suite (`PERFUME LUXE`)

## 0. Capability Map (Phân Rã Các Phần Kiểm Thử Độc Lập)

Theo chuẩn `/spec-driven-development` (Phase 0) và `/enterprise`, toàn bộ hệ thống **PerfumeLuxe** được chia thành **6 Phần Kiểm Thử (Test Modules)** độc lập, có thứ tự phụ thuộc rõ ràng để chạy TDD (`RED -> GREEN -> REFACTOR`) và nghiệm thu từng phần:

| Module ID | Tên Phần Kiểm Thử (Testing Part) | Phạm Vi Kiểm Thử (Scope & Routes) | Phụ Thuộc (`Depends on`) |
|---|---|---|---|
| `part-1-storefront-ui` | **Phần 1: Storefront, Catalog & Flash Sale UI** | Trang chủ (`/`), Countdown Banner Flash Sale, Tủ nước hoa (`/Catalog`), Lọc nâng cao, Chi tiết sản phẩm (`/Product/{slug}`), AI Scent Quiz (`/Quiz`), Tạp chí (`/Blog`), Trang tĩnh | — |
| `part-2-commerce-checkout` | **Phần 2: Giỏ Hàng, Thanh Toán, Gói Quà Hoàng Gia & VietQR** | Giỏ hàng (`/Cart`), Wishlist (`/Wishlist`), So sánh (`/Compare`), Checkout (`/Checkout`), Gói quà Hoàng Gia (`+50.000₫` + Thiệp viết tay), Cổng VietQR Napas247 (`/Checkout/VietQR`), Tra cứu đơn (`/TrackOrder`) | `part-1-storefront-ui` |
| `part-3-account-loyalty` | **Phần 3: Tài Khoản & Hạng Thành Viên VIP (Loyalty Engine)** | Đăng nhập/Đăng ký (`/Account/Login`, `/Account/Register`), Hồ sơ (`/Account/Profile`), Thẻ VIP kỹ thuật số & Thanh tiến trình (`Bronze -> Silver -> Gold -> Diamond`), Tự động chiết khấu VIP tại `/Checkout` | `part-2-commerce-checkout` |
| `part-4-discovery-box` | **Phần 4: Bộ Quà Tặng Chiết Thử 3 Bước (Discovery Gift Box)** | Trình thiết kế 3 bước (Chọn vỏ hộp Sơn mài/Nhung/Da $\rightarrow$ Chọn 3–5 ống chiết 2ml–5ml $\rightarrow$ Thiệp ép kim), Lưu cấu hình JSON vào giỏ hàng & đơn hàng | `part-2-commerce-checkout` |
| `part-5-admin-erp` | **Phần 5: Quản Trị Admin, P&L Tài Chính, RBAC & Bảo Mật** | Dashboard (`/Admin/AdminDashboard`), Quản lý Flash Sale (`/Admin/AdminDashboard/FlashSales`), Báo cáo P&L & Xuất CSV (`/FinancialReport`), Phân quyền 5 cấp RBAC, Nhật ký bảo mật (`/AuditLogs`), Sao lưu DB (`/DatabaseBackup`) | `part-1-storefront-ui`, `part-2-commerce-checkout` |
| `part-6-rest-api-swagger` | **Phần 6: Hệ Thống RESTful API v1 & Live Swagger UI** | Cổng `/swagger` (`swagger.json` v1), `GET /api/v1/flashsales/active`, `GET /api/v1/loyalty/status`, `POST /api/v1/discovery-box/validate-and-add`, `GET /api/v1/orders/track`, `POST /api/v1/experience/tasting-booking` | `part-1-storefront-ui`, `part-3-account-loyalty`, `part-4-discovery-box` |

**Thứ tự thực thi (Build & Test Order):**
`part-1-storefront-ui` $\rightarrow$ `part-2-commerce-checkout` $\rightarrow$ (`part-3-account-loyalty`, `part-4-discovery-box`, `part-5-admin-erp`) $\rightarrow$ `part-6-rest-api-swagger`

---

## 1. Objective (Mục Tiêu)

Xây dựng và thực thi bộ kiểm thử toàn diện theo phương pháp **Test-Driven Development (TDD)** kết hợp **Browser Runtime Verification (Chrome DevTools MCP)** cho toàn bộ nền tảng thương mại điện tử **PerfumeLuxe**, chia thành **6 phần độc lập** để người dùng và đội ngũ AI Enterprise kiểm soát chất lượng từng phần một cách minh bạch, không bỏ sót bất kỳ giao diện hay API nào.

---

## 2. Tech Stack (Công Nghệ Sử Dụng)

- **Runtime & Web Framework**: .NET 8.0 (`ASP.NET Core MVC` + `RESTful Web API`)
- **Database**: SQL Server LocalDB (`MSSQLLocalDB` - Database `PerfumeDb`) + `Microsoft.EntityFrameworkCore.InMemory` (8.0.2) cho Unit/Boundary Tests cô lập
- **Test Frameworks**:
  - `xUnit` (2.7.0) + `Moq` (4.20.70)
  - `Microsoft.AspNetCore.Mvc.Testing` (8.0.2) cho Integration Test
  - Live HTTP Runner & **Chrome DevTools MCP** cho E2E Browser UI Inspection (DOM Snapshot, Console Zero-Error, Network Status, Computed CSS)
- **API Documentation**: `Swashbuckle.AspNetCore` (`/swagger`)

---

## 3. Commands (Tập Lệnh Thực Thi Chuẩn)

```powershell
# 1. Build toàn bộ Solution (Yêu cầu: 0 Warnings, 0 Errors)
dotnet build PerfumeEcommerce.sln --configuration Debug

# 2. Chạy toàn bộ Test Suite hiện tại
dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --verbosity normal

# 3. Chạy Focused Test theo từng Phần (trong vòng lặp RED -> GREEN -> REFACTOR)
dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --filter "FullyQualifiedName~Part1"
dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --filter "FullyQualifiedName~FlashSaleBoundaryTests"

# 4. Khởi chạy Kestrel Live Server (nếu cần khởi động lại trên cổng 5000)
dotnet run --project src/Perfume.Web/Perfume.Web.csproj --urls "http://localhost:5000"
```

---

## 4. Project Structure (Cấu Trúc Thư Mục Kiểm Thử)

```text
src/
  Perfume.Domain/          -> Entities, Value Objects, Enums (FlashSale, UserTier, Order...)
  Perfume.Application/     -> DTOs & Interfaces (IFlashSaleService, ILoyaltyTierService, IDiscoveryBoxService...)
  Perfume.Infrastructure/  -> PerfumeDbContext, DatabaseSchemaUpgrader, Business Services
  Perfume.Web/             -> MVC Controllers, Api/v1 Controllers, Razor Views, wwwroot
tests/
  Perfume.Tests/
    FlashSaleBoundaryTests.cs        -> Unit & Boundary tests cho Flash Sale (Part 1)
    Part1_StorefrontUiTests.cs       -> Kiểm thử giao diện & logic Storefront / Catalog / Quiz / Blog
    Part2_CommerceCheckoutTests.cs   -> Kiểm thử Giỏ hàng, Gói quà Hoàng gia (+50k), VietQR & Checkout
    Part3_AccountLoyaltyTests.cs     -> Kiểm thử Hạng thành viên VIP (Bronze/Silver/Gold/Diamond)
    Part4_DiscoveryBoxTests.cs       -> Kiểm thử Bộ quà tặng chiết thử 3 bước (3-5 ống chiết)
    Part5_AdminErpSecurityTests.cs   -> Kiểm thử Admin Dashboard, P&L CSV, 5-tier RBAC, Audit & Backup
    Part6_RestApiSwaggerTests.cs     -> Kiểm thử hợp đồng RESTful API v1 & Swagger JSON/UI
```

---

## 5. Code Style (Quy Chuẩn Viết Test DAMP & State-Based)

Tuân thủ nghiêm ngặt quy tắc trong `test-driven-development`:
- **Arrange - Act - Assert (AAA)** rõ ràng.
- **Test State, Not Interactions**: Kiểm tra kết quả đầu ra và trạng thái DB/HTTP thực tế, ưu tiên Real Service + InMemory DB hoặc Live HTTP thay vì lạm dụng Mock.
- **DAMP over DRY**: Tên hàm test mô tả rõ kịch bản, điều kiện biên và kết quả kỳ vọng.

```csharp
[Fact]
public async Task Part1_ActiveFlashSale_AppliesDiscountOnProductDetail_AndRevertsWhenSoldOut()
{
    // Arrange
    using var db = CreateInMemoryDbContext(nameof(Part1_ActiveFlashSale_AppliesDiscountOnProductDetail_AndRevertsWhenSoldOut));
    var (brand, category) = SeedBaseEntities(db, 101);
    var product = new Product
    {
        Id = 101,
        Name = "Santal 33",
        Slug = "le-labo-santal-33",
        Sku = "LL-S33",
        BasePrice = 6500000m,
        BrandId = brand.Id,
        CategoryId = category.Id,
        IsInStock = true
    };
    db.Products.Add(product);
    db.FlashSales.Add(new FlashSale
    {
        Id = 101,
        Name = "Midnight Golden Hour",
        StartTime = DateTime.UtcNow.AddMinutes(-15),
        EndTime = DateTime.UtcNow.AddHours(2),
        IsActive = true,
        Items = new List<FlashSaleItem>
        {
            new() { Id = 101, ProductId = 101, FlashSalePrice = 5200000m, StockLimit = 5, SoldQuantity = 4 }
        }
    });
    await db.SaveChangesAsync();
    var catalogService = new PerfumeCatalogService(db);

    // Act
    var detail = await catalogService.GetProductBySlugAsync("le-labo-santal-33");

    // Assert
    Assert.NotNull(detail);
    Assert.True(detail.IsFlashSale);
    Assert.Equal(5200000m, detail.FlashSalePrice);
    Assert.Equal(20, detail.FlashSaleDiscountPercent);
}
```

---

## 6. Testing Strategy (Chiến Lược Kiểm Thử 3 Tầng Cho Mỗi Phần)

Mỗi phần từ **Phần 1 $\rightarrow$ Phần 6** đều phải vượt qua 3 cổng kiểm định (Quality Gates):
1. **Tầng 1 — Unit & Boundary Value Analysis (xUnit)**:
   - Kiểm tra logic nghiệp vụ, giá trị biên (giá = 0, hết giờ Flash Sale, đạt đúng mốc 5.000.000₫ / 15.000.000₫ / 30.000.000₫, chọn dưới 3 ống chiết hoặc trên 5 ống chiết...).
2. **Tầng 2 — HTTP Route & Integration Test**:
   - Xác minh mọi route GET/POST trong phần đó trả về đúng mã HTTP (`200 OK`, `302 Found` khi chặn quyền RBAC, `400 Bad Request` khi dữ liệu sai).
3. **Tầng 3 — Browser Runtime Verification (Chrome DevTools MCP)**:
   - Mở trực tiếp trên trình duyệt tại `http://localhost:5000`, kiểm tra DOM thực tế, đảm bảo **Console = 0 Errors**, và đúng chuẩn thẩm mỹ Luxury (`#D4AF37` / `#111116`).

---

## 7. Boundaries (Ranh Giới Thực Thi)

- **Always do (Luôn thực hiện)**:
  - Chạy `dotnet test` sau mỗi bước chỉnh sửa.
  - Đảm bảo `dotnet build` đạt `0 Warning(s)` và `0 Error(s)`.
  - Giữ nguyên vẹn dữ liệu hiện có trong database `PerfumeDb` (15 sản phẩm nước hoa cao cấp).
- **Ask first (Hỏi ý kiến trước)**:
  - Thay đổi cấu trúc bảng cốt lõi đã có hoặc thêm thư viện NuGet bên ngoài ngoài danh sách đã duyệt.
  - Push code lên GitHub (`git push`).
- **Never do (Tuyệt đối không làm)**:
  - Xóa hoặc skip (`[Fact(Skip = ...)]`) bất kỳ bài test nào để làm xanh bộ test.
  - Dùng lệnh `EnsureDeleted()` làm mất dữ liệu trên database `PerfumeDb` thực tế.

---

## 8. Success Criteria (Tiêu Chí Nghiệm Thu Định Lượng)

- [ ] **Phần 1 (`part-1-storefront-ui`)**: $\ge 10$ xUnit tests PASS + 100% route Storefront/Catalog/FlashSale/Quiz/Blog trả về `200 OK` + Countdown & nhãn giảm giá hiển thị đúng.
- [ ] **Phần 2 (`part-2-commerce-checkout`)**: $\ge 8$ xUnit tests PASS + Tính phí gói quà Hoàng Gia (`+50.000₫`), Voucher, và sinh mã VietQR Napas247 chính xác.
- [ ] **Phần 3 (`part-3-account-loyalty`)**: $\ge 8$ xUnit tests PASS + Tự động xét hạng `Bronze` / `Silver` (3%) / `Gold` (7%) / `Diamond` (12%) và hiển thị thẻ VIP tại `/Account/Profile`.
- [ ] **Phần 4 (`part-4-discovery-box`)**: $\ge 8$ xUnit tests PASS + Chặn cấu hình $<3$ hoặc $>5$ ống chiết, tính đúng tổng tiền `Vỏ hộp + Các ống chiết` và hiển thị chi tiết trong Giỏ hàng/Đơn hàng.
- [ ] **Phần 5 (`part-5-admin-erp`)**: $\ge 8$ xUnit tests PASS + Các trang `/Admin/AdminDashboard`, `/FlashSales`, `/FinancialReport`, `/AuditLogs`, `/DatabaseBackup` hoạt động 100% + Xuất CSV chuẩn UTF-8 BOM.
- [ ] **Phần 6 (`part-6-rest-api-swagger`)**: $\ge 10$ xUnit tests PASS + `/swagger/v1/swagger.json` và 5 RESTful API `/api/v1/...` phản hồi đúng schema & HTTP status code.
- [ ] Tổng số bài test tự động đạt **$\ge 50$ bài test PASS 100%** với **0 cảnh báo biên dịch**.

---

## 9. Open Questions (Câu Hỏi Xác Nhận Trước Khi Chạy)

1. Bạn có đồng ý với cách chia thành **6 Phần Kiểm Thử (Từ Phần 1 đến Phần 6)** như trong **Capability Map** ở trên không?
2. Bạn muốn tôi **chạy tuần tự từng phần và báo cáo ngay sau mỗi phần**, hay **tổng lực chạy tự động liền mạch từ Phần 1 đến hết Phần 6** rồi nghiệm thu tổng thể?
