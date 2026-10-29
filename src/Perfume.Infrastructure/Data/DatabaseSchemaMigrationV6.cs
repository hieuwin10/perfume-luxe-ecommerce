namespace Perfume.Infrastructure;

internal static class DatabaseSchemaMigrationV6
{
    public const string Version = "20261029_06_performance_indexes";
    public const string Sql = @"
-- Performance Indexing for High-Traffic Catalog Queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CreatedAt ON dbo.Products(CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_BasePrice' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_BasePrice ON dbo.Products(BasePrice);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_IsFeatured_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_IsFeatured_CreatedAt ON dbo.Products(IsFeatured, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_IsBestSeller_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_IsBestSeller_CreatedAt ON dbo.Products(IsBestSeller, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_IsNewArrival_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_IsNewArrival_CreatedAt ON dbo.Products(IsNewArrival, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_BrandId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_BrandId ON dbo.Products(BrandId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_FragranceFamilyId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_FragranceFamilyId ON dbo.Products(FragranceFamilyId);

-- Performance Indexing for Orders & Order Items
-- Chuyen doi cot UserId sang nvarchar(450) de cho phep tao chi muc B-Tree tren SQL Server
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'dbo' 
      AND TABLE_NAME = 'Orders' 
      AND COLUMN_NAME = 'UserId' 
      AND (CHARACTER_MAXIMUM_LENGTH = -1 OR CHARACTER_MAXIMUM_LENGTH > 450)
)
BEGIN
    ALTER TABLE dbo.Orders ALTER COLUMN UserId nvarchar(450) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_UserId_CreatedAt ON dbo.Orders(UserId, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_OrderStatus_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_OrderStatus_CreatedAt ON dbo.Orders(OrderStatus, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_CreatedAt ON dbo.Orders(CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems(ProductId);

-- Performance Indexing for Reviews
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reviews_ProductId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Reviews'))
    CREATE INDEX IX_Reviews_ProductId_CreatedAt ON dbo.Reviews(ProductId, CreatedAt DESC);

-- Performance Indexing for Blog Posts
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BlogPosts_IsPublished_CreatedAt' AND object_id = OBJECT_ID(N'dbo.BlogPosts'))
    CREATE INDEX IX_BlogPosts_IsPublished_CreatedAt ON dbo.BlogPosts(IsPublished, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BlogPosts_CategoryId' AND object_id = OBJECT_ID(N'dbo.BlogPosts'))
    CREATE INDEX IX_BlogPosts_CategoryId ON dbo.BlogPosts(CategoryId);

-- Performance Indexing for Product Variants
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductVariants_ProductId_VolumeMl' AND object_id = OBJECT_ID(N'dbo.ProductVariants'))
    CREATE INDEX IX_ProductVariants_ProductId_VolumeMl ON dbo.ProductVariants(ProductId, VolumeMl);

-- Performance Indexing for Stock Adjustments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockAdjustments_VariantId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.StockAdjustments'))
    CREATE INDEX IX_StockAdjustments_VariantId_CreatedAt ON dbo.StockAdjustments(VariantId, CreatedAt DESC);

INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
}

