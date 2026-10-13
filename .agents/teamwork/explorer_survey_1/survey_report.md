# Báo Cáo Khảo Sát Kiến Trúc Hệ Thống, Cơ Sở Dữ Liệu & Thiết Kế Schema Gia Tăng (Survey Report)

**Dự án**: Nền tảng thương mại nước hoa cao cấp PERFUME LUXE  
**Thực hiện bởi**: `explorer_survey_1` (Exploration Specialist)  
**Thời gian hoàn thành**: 2026-10-12  
**Mục tiêu**: Khảo sát toàn diện cấu trúc solution, dự án, EF Core DbContext, entities, cơ chế migration/seed và thiết kế mô hình dữ liệu gia tăng (additive schema) phục vụ các yêu cầu R1 (Flash Sale), R2 (Loyalty Tiers), R3 (Custom Discovery Box), R4 (RESTful API & Swagger) và R5 (Additive Schema Upgrader).

---

## 1. Tổng Quan Cấu Trúc Solution & Dependencies

### 1.1. Cấu trúc Solution (`PerfumeEcommerce.sln`)
Giải pháp được xây dựng trên nền tảng **.NET 8.0** theo mô hình Clean Architecture phân lớp:

```
PerfumeEcommerce.sln
│
├── src/
│   ├── Perfume.Domain/          # Cốt lõi nghiệp vụ (Entities, Enums)
│   ├── Perfume.Application/     # Giao tiếp ứng dụng (DTOs, Service Interfaces)
│   ├── Perfume.Infrastructure/  # Hạ tầng dữ liệu (EF Core DbContext, Identity, Services, SchemaUpgrader)
│   └── Perfume.Web/             # Web MVC, Controllers, Areas/Admin, REST API, Razor Views
│
├── scripts/
│   └── seed_perfume_data.sql    # Kịch bản nạp dữ liệu mẫu ban đầu bằng T-SQL độc lập
│
└── tests/                       # [Cần bổ sung theo R5]: Perfume.Tests (xUnit TDD)
```

### 1.2. Chi Tiết Dự Án & Các Phụ Thuộc (Project Dependencies)

| Dự án | Loại SDK / Target | Phụ thuộc dự án (Project References) | Thư viện NuGet chính (Package References) |
|---|---|---|---|
| **Perfume.Domain** | `Microsoft.NET.Sdk`<br>`net8.0` | Không có (Pure Domain) | Không phụ thuộc NuGet bên ngoài |
| **Perfume.Application** | `Microsoft.NET.Sdk`<br>`net8.0` | `Perfume.Domain` | Không phụ thuộc NuGet bên ngoài |
| **Perfume.Infrastructure** | `Microsoft.NET.Sdk`<br>`net8.0` | `Perfume.Domain`<br>`Perfume.Application` | • `Microsoft.EntityFrameworkCore.SqlServer` (8.0.11)<br>• `Microsoft.EntityFrameworkCore.Design` (8.0.11)<br>• `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (8.0.11)<br>• `Microsoft.Data.SqlClient` (5.2.2)<br>• `Dapper` (2.1.89)<br>• `StackExchange.Redis` (3.4.0)<br>• `<FrameworkReference Include="Microsoft.AspNetCore.App" />` |
| **Perfume.Web** | `Microsoft.NET.Sdk.Web`<br>`net8.0` | `Perfume.Domain`<br>`Perfume.Application`<br>`Perfume.Infrastructure` | • `Microsoft.EntityFrameworkCore.Design` (8.0.11)<br>• `System.IdentityModel.Tokens.Jwt` (6.35.0)<br>*Cần bổ sung `Swashbuckle.AspNetCore` (6.5.0+) cho Swagger UI theo R4* |

---

## 2. Khảo Sát EF Core DbContext, Entities Hiện Hữu & Mối Quan Hệ

### 2.1. Lớp DbContext (`PerfumeDbContext`)
- **Vị trí**: `src/Perfume.Infrastructure/Data/PerfumeDbContext.cs`
- **Kế thừa**: `IdentityDbContext<AppUser, AppRole, string>`
- **Bảng Identity tùy biến**:
  - `Users` (`AppUser`: kế thừa `IdentityUser`, mở rộng `FullName`, `AvatarUrl`, `Address`, `Tier`, `LoyaltyPoints`, `CreatedAt`)
  - `Roles` (`AppRole`: mở rộng `Description`)
  - `UserRoles`, `UserClaims`, `UserLogins`, `RoleClaims`, `UserTokens`
- **Tự động gán thời gian**: `SaveChangesAsync` tự động gán `CreatedAt = DateTime.UtcNow` khi thêm mới và `UpdatedAt = DateTime.UtcNow` khi cập nhật cho mọi thực thể kế thừa `BaseEntity`.

### 2.2. Danh Mục Thực Thể (Entity Catalog) & Mối Quan Hệ

#### Nhóm Catalog (Quản lý mùi hương & sản phẩm)
1. **`Brand`**: Tên thương hiệu, Slug, Quốc gia, Logo, IsFeatured. Liên kết 1-n với `Product` (DeleteBehavior.Restrict).
2. **`Category`**: Danh mục (Nước hoa Nam, Nữ, Unisex, Niche...), Slug, Giới tính, IsFeatured. Liên kết 1-n với `Product` (DeleteBehavior.Restrict).
3. **`FragranceFamily`**: Nhóm hương (Woody, Floral, Oriental, Fresh, Gourmand, Leather), ColorHex, ScentType. Liên kết 1-n với `Product` (DeleteBehavior.SetNull).
4. **`FragranceNote`**: Từng nốt hương (Bergamot, Cedarwood, Bourbon Vanilla...). Liên kết n-n với `Product` qua bảng nối `ProductFragranceNote`.
5. **`Product`**:
   - Khóa chính `Id` (int), `Slug` (unique), `Sku` (unique).
   - Thuộc tính mô tả, nồng độ (`Concentration`), giới tính (`GenderTarget`), độ lưu hương (`Longevity`), độ tỏa hương (`Sillage`), năm phát hành, giá gốc `BasePrice`, ảnh đại diện `MainImageUrl`.
   - Cờ trạng thái: `IsFeatured`, `IsBestSeller`, `IsNewArrival`, `IsInStock`.
   - Điểm đánh giá: `RatingAverage`, `RatingCount`, `ViewCount`.
   - Quan hệ: `Variants` (1-n), `FragranceNotes` (1-n nối), `Images` (1-n), `Reviews` (1-n).
   - Global Query Filter: `!p.IsDeleted`.
6. **`ProductVariant`**:
   - `VolumeMl` (2, 10, 50, 100, 200), `VolumeLabel`, `VariantSku`, `OriginalPrice`, `SalePrice`, `StockQuantity`, `IsDefault`.
   - FK tới `Product` (DeleteBehavior.Cascade).
7. **`ProductFragranceNote`**:
   - Khóa phức hợp `(ProductId, FragranceNoteId, NoteLevel)` (Top, Heart, Base).
8. **`ProductImage`**: Ảnh gallery sản phẩm, thứ tự hiển thị `DisplayOrder`.

#### Nhóm Thương Mại & Khách Hàng (Ecommerce & Customer)
1. **`Order`**:
   - `OrderCode` (định dạng `PF-YYYYMMDD-XXXX`, Unique).
   - Thông tin khách hàng: `UserId` (nullable - hỗ trợ cả khách vãng lai và thành viên), `CustomerName`, `CustomerPhone`, `CustomerEmail`, `ShippingAddress`, `City`, `OrderNotes`.
   - Tài chính: `SubTotal`, `DiscountAmount`, `ShippingFee`, `TotalAmount` (Decimal 18, 2).
   - Trạng thái: `PaymentMethod` (COD, VNPAY, MoMo, BankTransfer), `PaymentStatus` (Pending, Completed, Failed, Refunded), `OrderStatus` (Pending, Confirmed, Shipping, Delivered, Cancelled).
   - Khuyến mãi: `VoucherCode`, `TrackingNumber`.
   - Quan hệ: `Items` (1-n tới `OrderItem`).
2. **`OrderItem`**:
   - Liên kết `OrderId`, `ProductId`, `VariantId`.
   - Lưu trữ snapshot: `ProductName`, `VariantLabel`, `ProductImageUrl`, `UnitPrice`, `Quantity`, `TotalPrice`.
3. **`Voucher`**:
   - `Code` (Unique), `DiscountPercent`, `DiscountAmount`, `MinOrderAmount`, `MaxDiscountAmount`, `UsageLimit`, `UsedCount`, `ValidFrom`, `ValidTo`, `IsActive`.
4. **`ShoppingCart` & `ShoppingCartItem`**:
   - `ShoppingCart`: Hỗ trợ cả khách đăng nhập (`UserId`) và khách vãng lai (`OwnerKey` có tiền tố `guest:` / `user:`), thời hạn `ExpiresAt` (30 ngày).
   - `ShoppingCartItem`: Lưu `ProductId`, `VariantId`, `Quantity`.
5. **`UserLoyalty`**:
   - `UserId`, `Points`, `Tier` (`UserTier`: Bronze = 1, Silver = 2, Gold = 3, Diamond = 4), `TotalSpent`.
6. **`RefreshTokenRecord`**:
   - Lưu trữ SHA-256 hash của Refresh Token, phục vụ xác thực JWT API (`/api/v1/auth/token`, `/api/v1/auth/refresh`).
7. **`DatabaseAuditLog`**:
   - Ghi vết hành động DDL/DML, thời gian thực thi, số dòng ảnh hưởng trên bảng.

---

## 3. Khảo Sát Cơ Chế Database Migrations, Schema Upgrader & Seeding

### 3.1. Cơ Chế Khởi Tạo Cơ Sở Dữ Liệu
Hệ thống hiện tại **không sử dụng EF Core Migrations dạng commit files thông thường**, mà sử dụng cơ chế lai kết hợp độ tin cậy cao:
1. Khi ứng dụng khởi động (`Program.cs`):
   ```csharp
   var databaseCreated = await db.Database.EnsureCreatedAsync();
   await DatabaseSchemaUpgrader.ApplyAsync(db, builder.Configuration, logger, databaseCreated);
   ```
   - Nếu database chưa tồn tại, `EnsureCreatedAsync()` tạo toàn bộ bảng theo metadata EF Core.
   - Tiếp theo, `DatabaseSchemaUpgrader.ApplyAsync()` kiểm tra bảng kiểm soát phiên bản `dbo.__PerfumeSchemaVersions`.
2. Khởi tạo quyền & tài khoản ban đầu:
   - Tự động tạo 3 vai trò: `Admin`, `StoreManager`, `Customer`.
   - Nếu cấu hình `InitialAdmin:Email` và `InitialAdmin:Password`, tự động tạo tài khoản quản trị viên tối cao.

### 3.2. Cơ Chế Nâng Cấp Schema Gia Tăng (`DatabaseSchemaUpgrader`)
- **Vị trí**: `src/Perfume.Infrastructure/Data/DatabaseSchemaUpgrader.cs`
- **Bảng phiên bản**: `dbo.__PerfumeSchemaVersions (Version nvarchar(120) PRIMARY KEY, AppliedAt datetime2)`
- **Đặc điểm bảo vệ dữ liệu**:
  - Tự động kiểm tra `SELECT COUNT(1) FROM dbo.__PerfumeSchemaVersions WHERE Version = @version`.
  - Nếu đã chạy, bỏ qua ngay lập tức.
  - Nếu có cấu hình `Database:BackupPath`, thực hiện lệnh `BACKUP DATABASE [PerfumeDb]` trước khi nâng cấp.
  - Toàn bộ câu lệnh nâng cấp được bọc trong `SqlTransaction`. Lỗi phát sinh sẽ tự động `Rollback`.
  - Mọi câu lệnh DDL đều có điều kiện bảo vệ: `IF OBJECT_ID(...) IS NULL CREATE TABLE...`, `IF COL_LENGTH(...) IS NULL ALTER TABLE... ADD...`.

### 3.3. Kịch Bản Nạp Dữ Liệu Mẫu (`seed_perfume_data.sql`)
- Tệp `scripts/seed_perfume_data.sql` là một kịch bản T-SQL hoàn chỉnh, nạp:
  - 8 thương hiệu cao cấp (Dior, Chanel, Creed, Tom Ford, Le Labo, Maison Margiela, Kilian, YSL).
  - 5 danh mục chính (Nước Hoa Nam, Nước Hoa Nữ, Nước Hoa Unisex, Nước Hoa Niche, Nước Hoa Chiết 10ml).
  - 6 nhóm hương (Woody, Floral, Oriental, Fresh, Gourmand, Leather).
  - 12 nốt hương tiêu biểu.
  - Các dòng nước hoa kinh điển (Dior Sauvage Elixir, Chanel Bleu de Chanel, Creed Aventus, Tom Ford Tobacco Vanille...) cùng các biến thể dung tích 2ml, 10ml, 50ml, 100ml.
  - Toàn bộ câu lệnh đều sử dụng `IF NOT EXISTS (...)` đảm bảo chạy lặp lại an toàn (idempotent).

---

## 4. Thiết Kế Mô Hình Dữ Liệu & Schema Gia Tăng Cho Các Yêu Cầu Mới

### 4.1. Yêu Cầu R1: Flash Sale Campaign Engine

#### A. Mô hình C# Domain Entities
Thêm vào `Perfume.Domain/Entities/EcommerceEntities.cs`:

```csharp
public class FlashSale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BannerUrl { get; set; }

    public ICollection<FlashSaleItem> Items { get; set; } = new List<FlashSaleItem>();
}

public class FlashSaleItem : BaseEntity
{
    public int FlashSaleId { get; set; }
    public FlashSale? FlashSale { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public decimal FlashSalePrice { get; set; }
    public int StockLimit { get; set; }
    public int SoldQuantity { get; set; } = 0;
}
```

#### B. Cấu hình Fluent API trong `PerfumeDbContext.cs`
```csharp
public DbSet<FlashSale> FlashSales => Set<FlashSale>();
public DbSet<FlashSaleItem> FlashSaleItems => Set<FlashSaleItem>();

// Trong OnModelCreating:
builder.Entity<FlashSale>(entity =>
{
    entity.Property(f => f.Name).HasMaxLength(200).IsRequired();
    entity.Property(f => f.BannerUrl).HasMaxLength(500);
    entity.HasMany(f => f.Items)
          .WithOne(i => i.FlashSale)
          .HasForeignKey(i => i.FlashSaleId)
          .OnDelete(DeleteBehavior.Cascade);
    entity.HasQueryFilter(f => !f.IsDeleted);
});

builder.Entity<FlashSaleItem>(entity =>
{
    entity.Property(i => i.FlashSalePrice).HasPrecision(18, 2);
    entity.HasOne(i => i.Product)
          .WithMany()
          .HasForeignKey(i => i.ProductId)
          .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(i => i.Variant)
          .WithMany()
          .HasForeignKey(i => i.VariantId)
          .OnDelete(DeleteBehavior.SetNull);
    entity.HasQueryFilter(i => !i.IsDeleted);
});
```

#### C. Kịch bản DDL Gia Tăng cho DatabaseSchemaUpgrader
```sql
IF OBJECT_ID(N'dbo.FlashSales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FlashSales
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FlashSales PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_FlashSales_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_FlashSales_IsDeleted DEFAULT 0,
        Name nvarchar(200) NOT NULL,
        Description nvarchar(1000) NULL,
        StartTime datetime2 NOT NULL,
        EndTime datetime2 NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_FlashSales_IsActive DEFAULT 1,
        BannerUrl nvarchar(500) NULL
    );
    CREATE INDEX IX_FlashSales_IsActive_Time ON dbo.FlashSales(IsActive, StartTime, EndTime);
END;

IF OBJECT_ID(N'dbo.FlashSaleItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FlashSaleItems
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FlashSaleItems PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_FlashSaleItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_FlashSaleItems_IsDeleted DEFAULT 0,
        FlashSaleId int NOT NULL,
        ProductId int NOT NULL,
        VariantId int NULL,
        FlashSalePrice decimal(18,2) NOT NULL,
        StockLimit int NOT NULL,
        SoldQuantity int NOT NULL CONSTRAINT DF_FlashSaleItems_SoldQuantity DEFAULT 0,
        CONSTRAINT FK_FlashSaleItems_FlashSales_FlashSaleId FOREIGN KEY (FlashSaleId) REFERENCES dbo.FlashSales(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FlashSaleItems_Products_ProductId FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_FlashSaleItems_ProductVariants_VariantId FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id),
        CONSTRAINT CK_FlashSaleItems_Limit CHECK (StockLimit >= 0),
        CONSTRAINT CK_FlashSaleItems_Sold CHECK (SoldQuantity >= 0)
    );
    CREATE INDEX IX_FlashSaleItems_FlashSaleId ON dbo.FlashSaleItems(FlashSaleId);
    CREATE INDEX IX_FlashSaleItems_ProductId_VariantId ON dbo.FlashSaleItems(ProductId, VariantId);
END;
```

---

### 4.2. Yêu Cầu R2: Loyalty Tier Engine & VIP Privileges

#### A. Logic Nghiệp Vụ Bậc Thành Viên & Giảm Giá
Hệ thống phân hạng dựa trên tổng chi tiêu đơn hàng đã hoàn tất (`PaymentStatus == PaymentStatus.Completed` hoặc `OrderStatus == OrderStatus.Delivered`):

| Hạng Thành Viên | Ngưỡng Chi Tiêu Tích Lũy | Mức Giảm Giá VIP | Quyền Lợi Kèm Theo |
|---|---|---|---|
| **Bronze** (Đồng) | 0đ – dưới 5.000.000đ | 0% | Tích lũy điểm thưởng |
| **Silver** (Bạc) | Từ 5.000.000đ – dưới 15.000.000đ | **3%** | Ưu đãi sinh nhật, hỗ trợ ưu tiên |
| **Gold** (Vàng) | Từ 15.000.000đ – dưới 30.000.000đ | **7%** | Miễn phí vận chuyển toàn quốc |
| **Diamond** (Kim Cương) | Từ 30.000.000đ trở lên | **12%** | Quà tặng nước hoa Niche độc quyền, cố vấn mùi hương riêng |

#### B. Mở Rộng Mô Hình Dữ Liệu Gia Tăng
1. Cập nhật thực thể `Order` (`src/Perfume.Domain/Entities/EcommerceEntities.cs`):
   - Thêm `public decimal LoyaltyDiscountAmount { get; set; } = 0;` (Số tiền giảm giá thành viên VIP).
   - Thêm `public UserTier? AppliedLoyaltyTier { get; set; }` (Hạng thành viên áp dụng tại thời điểm đặt hàng).
2. Đồng bộ bảng `UserLoyalties`:
   - Lưu trữ `UserId`, `Tier`, `TotalSpent`, `Points`.
   - Khi đơn hàng chuyển sang `Delivered` hoặc `PaymentStatus.Completed`, kích hoạt phương thức:
     `RecalculateUserTierAsync(string userId)`:
     - Tính tổng `SUM(TotalAmount)` của các đơn đã hoàn tất.
     - Xác định cấp bậc mới.
     - Cập nhật đồng bộ vào cả `AppUser.Tier` và `UserLoyalty`.
3. Kịch bản DDL Gia Tăng:
```sql
IF OBJECT_ID(N'dbo.UserLoyalties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLoyalties
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserLoyalties PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_UserLoyalties_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_UserLoyalties_IsDeleted DEFAULT 0,
        UserId nvarchar(450) NOT NULL,
        Points int NOT NULL CONSTRAINT DF_UserLoyalties_Points DEFAULT 0,
        Tier int NOT NULL CONSTRAINT DF_UserLoyalties_Tier DEFAULT 1,
        TotalSpent decimal(18,2) NOT NULL CONSTRAINT DF_UserLoyalties_TotalSpent DEFAULT 0,
        CONSTRAINT FK_UserLoyalties_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_UserLoyalties_UserId ON dbo.UserLoyalties(UserId);
END;

IF COL_LENGTH(N'dbo.Orders', N'LoyaltyDiscountAmount') IS NULL
    ALTER TABLE dbo.Orders ADD LoyaltyDiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_Orders_LoyaltyDiscountAmount DEFAULT 0;

IF COL_LENGTH(N'dbo.Orders', N'AppliedLoyaltyTier') IS NULL
    ALTER TABLE dbo.Orders ADD AppliedLoyaltyTier int NULL;
```

---

### 4.3. Yêu Cầu R3: Custom Discovery Gift Box Builder

#### A. Kiến Trúc Hộp Quà Cá Nhân Hóa (Gift Box Architecture)
Trải nghiệm 3 bước:
1. **Mẫu hộp**:
   - `ClassicWood`: Hộp Gỗ Sơn Mài Cổ Điển (290.000₫)
   - `RoyalVelvet`: Hộp Nhung Đen Hoàng Gia (250.000₫)
   - `ArtisanLeather`: Bao Da Thủ Công (320.000₫)
2. **3 - 5 ống vial mẫu thử**:
   - Khách hàng chọn 3 đến 5 ống mẫu thử từ danh mục (dung tích 2ml - 5ml). Giá mỗi ống tính theo giá mẫu thử thực tế hoặc chiết tính theo tỷ lệ sản phẩm.
3. **Thiệp lời chúc mạ vàng**:
   - Thông điệp chúc mừng (tối đa 250 ký tự).
   - Phông chữ nghệ thuật: `Playfair Display`, `Great Vibes`, `Dancing Script`, `Cinzel`.

#### B. Biểu Diễn Trọn Gói Trong Giỏ Hàng (`ShoppingCartItem`) & Đơn Hàng (`OrderItem`)
Để không làm hỏng cấu trúc relational hiện có và tuân thủ chặt chẽ khóa ngoại `FK_ShoppingCartItems_Products_ProductId` và `FK_OrderItems_Products_ProductId`:
1. **Sản phẩm hệ thống đại diện (System Anchor Product)**:
   - Tự động nạp hoặc đảm bảo sự tồn tại của 1 sản phẩm hệ thống đặc biệt:
     - Tên: `"Custom Discovery Gift Box"`
     - Slug: `"custom-discovery-box"`
     - Sku: `"PLX-DISCOVERY-BOX"`
     - BasePrice: `250000` (giá hộp tiêu chuẩn)
2. **Cột mở rộng gia tăng (Additive Metadata Columns)**:
   - Trong `ShoppingCartItem`:
     - `public bool IsCustomBox { get; set; } = false;`
     - `public string? CustomBoxConfigJson { get; set; }`
     - `public decimal? CustomUnitPrice { get; set; }` (Tổng giá trị hộp + các ống vial)
   - Trong `OrderItem`:
     - `public bool IsCustomBox { get; set; } = false;`
     - `public string? CustomBoxConfigJson { get; set; }`
3. **Cấu trúc DTO Cấu Hình Hộp Quà (`CustomBoxConfigDto`)**:
   ```json
   {
     "boxType": "RoyalVelvet",
     "boxName": "Hộp Nhung Đen Hoàng Gia",
     "boxPrice": 250000,
     "greetingMessage": "Chúc bạn luôn tỏa hương thanh lịch và quyến rũ",
     "greetingFont": "Playfair Display",
     "vials": [
       { "productId": 1, "productName": "Dior Sauvage Elixir", "volumeMl": 2, "price": 120000 },
       { "productId": 3, "productName": "Creed Aventus", "volumeMl": 2, "price": 150000 },
       { "productId": 4, "productName": "Tom Ford Lost Cherry", "volumeMl": 2, "price": 160000 }
     ],
     "totalPrice": 680000
   }
   ```
4. Kịch bản DDL Gia Tăng:
```sql
IF COL_LENGTH(N'dbo.ShoppingCartItems', N'IsCustomBox') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD IsCustomBox bit NOT NULL CONSTRAINT DF_ShoppingCartItems_IsCustomBox DEFAULT 0;

IF COL_LENGTH(N'dbo.ShoppingCartItems', N'CustomBoxConfigJson') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD CustomBoxConfigJson nvarchar(max) NULL;

IF COL_LENGTH(N'dbo.ShoppingCartItems', N'CustomUnitPrice') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD CustomUnitPrice decimal(18,2) NULL;

IF COL_LENGTH(N'dbo.OrderItems', N'IsCustomBox') IS NULL
    ALTER TABLE dbo.OrderItems ADD IsCustomBox bit NOT NULL CONSTRAINT DF_OrderItems_IsCustomBox DEFAULT 0;

IF COL_LENGTH(N'dbo.OrderItems', N'CustomBoxConfigJson') IS NULL
    ALTER TABLE dbo.OrderItems ADD CustomBoxConfigJson nvarchar(max) NULL;
```

---

### 4.4. Yêu Cầu R5: Chiến Lược Nâng Cấp Schema Gia Tăng (Additive Schema Upgrader)

#### A. Thiết Kế Version Checkpoint Tuần Tự
Nâng cấp `DatabaseSchemaUpgrader.cs` để quản lý nhiều phiên bản nâng cấp tuần tự:
- `Version 1`: `"20261011_01_customer_carts_and_refresh_tokens"` (Đã có sẵn).
- `Version 2`: `"20261012_02_flashsales_loyalty_discoverybox"` (Phiên bản mới cho R1, R2, R3).

#### B. Cơ Chế Bảo Vệ Dữ Liệu An Toàn Tuyệt Đối
1. **Kiểm tra phiên bản trước khi chạy**:
   ```csharp
   command.CommandText = "IF OBJECT_ID(N'dbo.__PerfumeSchemaVersions', N'U') IS NULL SELECT CAST(0 AS int); ELSE SELECT COUNT(1) FROM dbo.__PerfumeSchemaVersions WHERE Version = @version";
   ```
2. **Khả năng dự phòng Backup**:
   - Nếu `Database:BackupPath` được cấu hình, thực hiện `BACKUP DATABASE` an toàn.
   - Nếu không có cấu hình `Database:BackupPath` (trong môi trường phát triển máy cá nhân hoặc kiểm thử cục bộ không có quyền ghi thư mục SQL Server), ghi `LogWarning` thay vì làm gián đoạn toàn bộ ứng dụng (`throw Exception`), đảm bảo môi trường CI/CD và máy dev chạy kiểm thử thông suốt.
3. **Giao dịch toàn vẹn (ACID Transaction)**:
   - Toàn bộ DDL của Version 2 được thực thi trong một `SqlTransaction`.
   - Nếu bất kỳ lỗi cú pháp hoặc hệ thống xảy ra, transaction tự động `Rollback`.
   - Chỉ khi toàn bộ DDL thành công, ghi nhận bản ghi phiên bản vào `dbo.__PerfumeSchemaVersions` và `Commit`.

---

## 5. Danh Mục Điểm Kiểm Tra Tính Toàn Vẹn & Kế Hoạch Triển Khai Cho Nhóm

### 5.1. Ma Trận Đảm Bảo Không Ảnh Hưởng Mã Nguồn Hiện Tại (Compatibility Matrix)

| Khu vực | Rủi ro tiềm ẩn | Biện pháp kỹ thuật loại trừ |
|---|---|---|
| **Giỏ hàng (`ShoppingCart`)** | Khách hàng thêm giỏ hàng thông thường bị lỗi do thêm trường hộp quà | Cột `IsCustomBox` mặc định là `0`, `CustomBoxConfigJson` là `NULL`. Code cũ xử lý bình thường nếu không có flag hộp quà. |
| **Đơn hàng (`Order`, `OrderItem`)** | Tính tổng tiền và checkout đơn hàng cũ bị sai lệch | `LoyaltyDiscountAmount` mặc định là `0`. Nếu đơn không có VIP tier, chiết khấu giữ nguyên. |
| **Bảng `UserLoyalty`** | Trùng lặp hoặc thiếu bảng trên các môi trường cũ | DDL kiểm tra `IF OBJECT_ID('dbo.UserLoyalties') IS NULL CREATE TABLE...` an toàn 100%. |
| **Xác thực API (`/api/v1`)** | Xung đột token hoặc phân quyền với session Cookie | Giữ nguyên cấu hình phân tách: API hỗ trợ Bearer Token lẫn Cookie; Swagger UI (`/swagger`) sử dụng trực tiếp các endpoint công khai và token auth. |

### 5.2. Kế Hoạch Phân Công Tiếp Theo (Downstream Action Plan)
1. **Nhóm Database & Backend**:
   - Bổ sung entities `FlashSale`, `FlashSaleItem`, mở rộng `Order`, `OrderItem`, `ShoppingCartItem` trong `Perfume.Domain`.
   - Khai báo DbSets và Fluent API trong `PerfumeDbContext`.
   - Nâng cấp `DatabaseSchemaUpgrader.cs` với Version 2.
   - Cung cấp dịch vụ `IFlashSaleService`, `ILoyaltyTierService`, `IDiscoveryBoxService` trong `Perfume.Application` & `Perfume.Infrastructure`.
2. **Nhóm Web Controllers & Admin**:
   - Cung cấp giao diện Admin tại `/Admin/AdminDashboard/FlashSales`.
   - Cập nhật `/Account/Profile` hiển thị Thẻ thành viên VIP kỹ thuật số và Progress Bar.
   - Cung cấp trang thiết kế hộp quà cá nhân hóa 3 bước và tích hợp giỏ hàng / thanh toán.
3. **Nhóm RESTful API & Swagger UI**:
   - Cài đặt package `Swashbuckle.AspNetCore` cho `Perfume.Web`.
   - Cấu hình Swagger trong `Program.cs` hỗ trợ `/swagger` kèm đầy đủ tài liệu API endpoints (`api/v1/flashsales/active`, `api/v1/loyalty/status`, `api/v1/discovery-box/validate-and-add`, `api/v1/orders/track`, `api/v1/experience/tasting-booking`...).
4. **Nhóm Kiểm Thử (Testing & QA)**:
   - Tạo dự án `tests/Perfume.Tests/Perfume.Tests.csproj` (xUnit).
   - Viết các bộ test unit cho logic Flash Sale (còn hạn/hết hạn/hết số lượng), Loyalty Tier (tự động thăng hạng), Custom Discovery Box (tính giá trọn gói), và API responses.
