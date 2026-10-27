namespace Perfume.Infrastructure;

internal static class DatabaseSchemaMigrationV3
{
    public const string Version = "20261012_03_flashsales_loyalty_discoverybox";
    public const string Sql = @"
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

IF COL_LENGTH(N'dbo.OrderItems', N'IsCustomBox') IS NULL
    ALTER TABLE dbo.OrderItems ADD IsCustomBox bit NOT NULL CONSTRAINT DF_OrderItems_IsCustomBox DEFAULT 0;

IF COL_LENGTH(N'dbo.OrderItems', N'CustomBoxConfigJson') IS NULL
    ALTER TABLE dbo.OrderItems ADD CustomBoxConfigJson nvarchar(max) NULL;

IF COL_LENGTH(N'dbo.ShoppingCartItems', N'IsCustomBox') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD IsCustomBox bit NOT NULL CONSTRAINT DF_ShoppingCartItems_IsCustomBox DEFAULT 0;

IF COL_LENGTH(N'dbo.ShoppingCartItems', N'CustomBoxConfigJson') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD CustomBoxConfigJson nvarchar(max) NULL;

IF COL_LENGTH(N'dbo.ShoppingCartItems', N'CustomUnitPrice') IS NULL
    ALTER TABLE dbo.ShoppingCartItems ADD CustomUnitPrice decimal(18,2) NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.FlashSales WHERE IsDeleted = 0 AND IsActive = 1 AND EndTime > SYSUTCDATETIME())
BEGIN
    DECLARE @seedFsId int;
    INSERT INTO dbo.FlashSales (Name, Description, StartTime, EndTime, IsActive, BannerUrl, CreatedAt, IsDeleted)
    VALUES (N'Đặc Quyền Haute Parfumerie - Flash Sale Giờ Vàng',
            N'Chiến dịch ưu đãi đặc quyền cho các tuyệt tác mùi hương vương giả với số lượng giới hạn.',
            DATEADD(hour, -1, SYSUTCDATETIME()),
            DATEADD(day, 2, SYSUTCDATETIME()),
            1,
            NULL,
            SYSUTCDATETIME(),
            0);
    SET @seedFsId = SCOPE_IDENTITY();

    DECLARE @prod1 int, @prod2 int;
    SELECT TOP 1 @prod1 = Id FROM dbo.Products WHERE IsDeleted = 0 ORDER BY Id ASC;
    SELECT TOP 1 @prod2 = Id FROM dbo.Products WHERE IsDeleted = 0 AND Id <> @prod1 ORDER BY Id ASC;

    IF @prod1 IS NOT NULL
    BEGIN
        DECLARE @p1Price decimal(18,2);
        SELECT @p1Price = BasePrice FROM dbo.Products WHERE Id = @prod1;
        INSERT INTO dbo.FlashSaleItems (FlashSaleId, ProductId, VariantId, FlashSalePrice, StockLimit, SoldQuantity, CreatedAt, IsDeleted)
        VALUES (@seedFsId, @prod1, NULL, ROUND(@p1Price * 0.75, -4), 25, 14, SYSUTCDATETIME(), 0);
    END;

    IF @prod2 IS NOT NULL
    BEGIN
        DECLARE @p2Price decimal(18,2);
        SELECT @p2Price = BasePrice FROM dbo.Products WHERE Id = @prod2;
        INSERT INTO dbo.FlashSaleItems (FlashSaleId, ProductId, VariantId, FlashSalePrice, StockLimit, SoldQuantity, CreatedAt, IsDeleted)
        VALUES (@seedFsId, @prod2, NULL, ROUND(@p2Price * 0.80, -4), 30, 22, SYSUTCDATETIME(), 0);
    END;
END;
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
}
