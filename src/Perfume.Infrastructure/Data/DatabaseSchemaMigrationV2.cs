namespace Perfume.Infrastructure;

internal static class DatabaseSchemaMigrationV2
{
    public const string Version = "20261012_02_tastings_and_alerts";
    public const string Sql = @"
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_SystemSettings_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_SystemSettings_IsDeleted DEFAULT 0,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(4000) NOT NULL,
        Description nvarchar(500) NULL
    );
    CREATE UNIQUE INDEX IX_SystemSettings_Key ON dbo.SystemSettings([Key]);
END;
IF OBJECT_ID(N'dbo.HeroBanners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HeroBanners
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_HeroBanners_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_HeroBanners_IsDeleted DEFAULT 0,
        Title nvarchar(250) NOT NULL,
        Subtitle nvarchar(500) NULL,
        ImageUrl nvarchar(1000) NOT NULL,
        TargetUrl nvarchar(500) NULL,
        ButtonText nvarchar(100) NULL,
        DisplayOrder int NOT NULL CONSTRAINT DF_HeroBanners_DisplayOrder DEFAULT 0,
        IsActive bit NOT NULL CONSTRAINT DF_HeroBanners_IsActive DEFAULT 1
    );
END;
IF OBJECT_ID(N'dbo.StockAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockAdjustments
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_StockAdjustments_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_StockAdjustments_IsDeleted DEFAULT 0,
        VariantId int NOT NULL,
        OldQuantity int NOT NULL,
        AdjustedQuantity int NOT NULL,
        NewQuantity int NOT NULL,
        UnitCost decimal(18,2) NULL,
        Reason nvarchar(200) NOT NULL,
        Note nvarchar(500) NULL,
        PerformedByUserId nvarchar(450) NULL,
        PerformedByName nvarchar(150) NULL,
        CONSTRAINT FK_StockAdjustments_Variants_VariantId FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_StockAdjustments_VariantId ON dbo.StockAdjustments(VariantId);
END;
IF OBJECT_ID(N'dbo.PrivateTastingBookings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivateTastingBookings
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_PrivateTastingBookings_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_PrivateTastingBookings_IsDeleted DEFAULT 0,
        CustomerName nvarchar(150) NOT NULL,
        PhoneNumber nvarchar(30) NOT NULL,
        Email nvarchar(256) NULL,
        BoutiqueLocation nvarchar(200) NOT NULL,
        BookingDate datetime2 NOT NULL,
        TimeSlot nvarchar(100) NOT NULL,
        GuestCount int NOT NULL CONSTRAINT DF_PrivateTastingBookings_GuestCount DEFAULT 1,
        PreferredScentFamilies nvarchar(500) NULL,
        Notes nvarchar(1000) NULL,
        Status nvarchar(50) NOT NULL CONSTRAINT DF_PrivateTastingBookings_Status DEFAULT N'Pending'
    );
END;
IF OBJECT_ID(N'dbo.BackInStockAlerts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BackInStockAlerts
    (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_BackInStockAlerts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_BackInStockAlerts_IsDeleted DEFAULT 0,
        ProductId int NOT NULL,
        VariantId int NULL,
        CustomerEmail nvarchar(256) NOT NULL,
        CustomerPhone nvarchar(30) NULL,
        IsNotified bit NOT NULL CONSTRAINT DF_BackInStockAlerts_IsNotified DEFAULT 0,
        NotifiedAt datetime2 NULL,
        CONSTRAINT FK_BackInStockAlerts_Products_ProductId FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_BackInStockAlerts_ProductId ON dbo.BackInStockAlerts(ProductId);
END;
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
}
