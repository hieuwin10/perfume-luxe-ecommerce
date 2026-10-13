using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure;

public static class DatabaseSchemaUpgrader
{
    private static readonly (string Version, string Sql)[] Migrations = new[]
    {
        ("20261011_01_customer_carts_and_refresh_tokens", @"
IF OBJECT_ID(N'dbo.__PerfumeSchemaVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__PerfumeSchemaVersions
    (
        Version nvarchar(120) NOT NULL PRIMARY KEY,
        AppliedAt datetime2 NOT NULL CONSTRAINT DF_PerfumeSchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME()
    );
END;
IF COL_LENGTH(N'dbo.Reviews', N'ImageUrls') IS NULL ALTER TABLE dbo.Reviews ADD ImageUrls nvarchar(max) NULL;
IF OBJECT_ID(N'dbo.DatabaseAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseAuditLogs
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseAuditLogs PRIMARY KEY,
        IsDeleted bit NOT NULL CONSTRAINT DF_DatabaseAuditLogs_IsDeleted DEFAULT 0,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_DatabaseAuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        ActorUserId nvarchar(450) NULL,
        Action nvarchar(80) NOT NULL,
        Target nvarchar(128) NULL,
        CommandHash varchar(64) NULL,
        Succeeded bit NOT NULL,
        RowsAffected int NULL,
        ElapsedMilliseconds bigint NOT NULL CONSTRAINT DF_DatabaseAuditLogs_Elapsed DEFAULT 0
    );
    CREATE INDEX IX_DatabaseAuditLogs_CreatedAt ON dbo.DatabaseAuditLogs(CreatedAt);
END;
IF OBJECT_ID(N'dbo.ShoppingCarts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShoppingCarts
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShoppingCarts PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ShoppingCarts_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_ShoppingCarts_IsDeleted DEFAULT 0,
        OwnerKey nvarchar(520) NOT NULL,
        UserId nvarchar(450) NULL,
        ExpiresAt datetime2 NOT NULL,
        CONSTRAINT FK_ShoppingCarts_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_ShoppingCarts_OwnerKey ON dbo.ShoppingCarts(OwnerKey);
    CREATE UNIQUE INDEX IX_ShoppingCarts_UserId ON dbo.ShoppingCarts(UserId) WHERE UserId IS NOT NULL;
END;
IF OBJECT_ID(N'dbo.ShoppingCartItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShoppingCartItems
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShoppingCartItems PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ShoppingCartItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_ShoppingCartItems_IsDeleted DEFAULT 0,
        ShoppingCartId int NOT NULL,
        ProductId int NOT NULL,
        VariantId int NULL,
        Quantity int NOT NULL,
        CONSTRAINT FK_ShoppingCartItems_ShoppingCarts_ShoppingCartId FOREIGN KEY (ShoppingCartId) REFERENCES dbo.ShoppingCarts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ShoppingCartItems_Products_ProductId FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_ShoppingCartItems_ProductVariants_VariantId FOREIGN KEY (VariantId) REFERENCES dbo.ProductVariants(Id),
        CONSTRAINT CK_ShoppingCartItems_Quantity CHECK (Quantity > 0)
    );
    CREATE INDEX IX_ShoppingCartItems_ShoppingCartId_ProductId_VariantId ON dbo.ShoppingCartItems(ShoppingCartId, ProductId, VariantId);
END;
IF OBJECT_ID(N'dbo.RefreshTokenRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokenRecords
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokenRecords PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_RefreshTokenRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_RefreshTokenRecords_IsDeleted DEFAULT 0,
        UserId nvarchar(450) NOT NULL,
        TokenHash nvarchar(64) NOT NULL,
        ExpiresAt datetime2 NOT NULL,
        RevokedAt datetime2 NULL,
        ReplacedByTokenHash nvarchar(64) NULL,
        CONSTRAINT FK_RefreshTokenRecords_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_RefreshTokenRecords_TokenHash ON dbo.RefreshTokenRecords(TokenHash);
    CREATE INDEX IX_RefreshTokenRecords_UserId_ExpiresAt ON dbo.RefreshTokenRecords(UserId, ExpiresAt);
END;
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);"),

        ("20261012_02_tastings_and_alerts", @"
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
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);"),

        ("20261012_03_flashsales_loyalty_discoverybox", @"
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
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);"),

        ("20261013_04_system_error_logs", @"
IF OBJECT_ID(N'dbo.SystemErrorLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemErrorLogs
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemErrorLogs PRIMARY KEY,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_SystemErrorLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_SystemErrorLogs_IsDeleted DEFAULT 0,
        ErrorCode nvarchar(100) NOT NULL,
        Message nvarchar(max) NOT NULL,
        StackTrace nvarchar(max) NULL,
        RequestPath nvarchar(500) NULL,
        HttpMethod nvarchar(20) NULL,
        ClientIp nvarchar(100) NULL,
        UserAgent nvarchar(500) NULL,
        StatusCode int NOT NULL CONSTRAINT DF_SystemErrorLogs_StatusCode DEFAULT 500,
        ActorUserId nvarchar(450) NULL
    );
    CREATE INDEX IX_SystemErrorLogs_CreatedAt ON dbo.SystemErrorLogs(CreatedAt DESC);
    CREATE INDEX IX_SystemErrorLogs_ErrorCode ON dbo.SystemErrorLogs(ErrorCode);
END;
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);")
    };

    public static async Task ApplyAsync(PerfumeDbContext db, IConfiguration configuration, ILogger logger, bool databaseCreatedThisStart, CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using (var initCommand = connection.CreateCommand())
            {
                initCommand.CommandText = @"
IF OBJECT_ID(N'dbo.__PerfumeSchemaVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__PerfumeSchemaVersions
    (
        Version nvarchar(120) NOT NULL PRIMARY KEY,
        AppliedAt datetime2 NOT NULL CONSTRAINT DF_PerfumeSchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME()
    );
END;";
                await initCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var appliedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var getVersions = connection.CreateCommand())
            {
                getVersions.CommandText = "SELECT Version FROM dbo.__PerfumeSchemaVersions;";
                await using var reader = await getVersions.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    appliedVersions.Add(reader.GetString(0));
                }
            }

            var pendingMigrations = Migrations.Where(m => !appliedVersions.Contains(m.Version)).ToList();
            if (!pendingMigrations.Any())
            {
                return;
            }

            if (!databaseCreatedThisStart)
            {
                var backupDirectory = configuration["Database:BackupPath"];
                if (!string.IsNullOrWhiteSpace(backupDirectory))
                {
                    try
                    {
                        var backupPath = $"{backupDirectory.TrimEnd('/', '\\')}/PerfumeDb-before-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
                        await using var backup = connection.CreateCommand();
                        backup.CommandText = "DECLARE @p nvarchar(4000) = @backupPath; DECLARE @sql nvarchar(max) = N'BACKUP DATABASE [PerfumeDb] TO DISK = N''' + REPLACE(@p, N'''', N'''''') + N''' WITH COPY_ONLY, INIT'; EXEC sys.sp_executesql @sql;";
                        backup.Parameters.Add(new SqlParameter("@backupPath", backupPath));
                        backup.CommandTimeout = 0;
                        await backup.ExecuteNonQueryAsync(cancellationToken);
                        logger.LogInformation("Database backup completed before applying schema updates.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Database backup could not be completed at '{BackupDirectory}', continuing with migration safely.", backupDirectory);
                    }
                }
                else
                {
                    logger.LogInformation("Database:BackupPath is not configured; skipping pre-migration database backup.");
                }
            }

            foreach (var (version, sql) in pendingMigrations)
            {
                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
                try
                {
                    await using var migration = connection.CreateCommand();
                    migration.Transaction = transaction;
                    migration.CommandText = sql;
                    migration.Parameters.Add(new SqlParameter("@version", version));
                    await migration.ExecuteNonQueryAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    logger.LogInformation("Applied additive database schema update {Version}.", version);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogError(ex, "Failed applying schema update {Version}, transaction rolled back.", version);
                    throw;
                }
            }
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }
}
