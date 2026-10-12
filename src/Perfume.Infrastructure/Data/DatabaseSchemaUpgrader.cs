using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure;

public static class DatabaseSchemaUpgrader
{
    private const string Version = "20261012_02_tastings_and_alerts";

    public static async Task ApplyAsync(PerfumeDbContext db, IConfiguration configuration, ILogger logger, bool databaseCreatedThisStart, CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "IF OBJECT_ID(N'dbo.__PerfumeSchemaVersions', N'U') IS NULL SELECT CAST(0 AS int); ELSE SELECT COUNT(1) FROM dbo.__PerfumeSchemaVersions WHERE Version = @version";
                command.Parameters.Add(new SqlParameter("@version", Version));
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0) return;
            }

            if (!databaseCreatedThisStart)
            {
                var backupDirectory = configuration["Database:BackupPath"];
                if (string.IsNullOrWhiteSpace(backupDirectory))
                    throw new InvalidOperationException("A schema update is pending, but Database:BackupPath is not configured. Set it to a directory writable by SQL Server before restarting.");

                var backupPath = $"{backupDirectory.TrimEnd('/', '\\')}/PerfumeDb-before-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
                await using var backup = connection.CreateCommand();
                backup.CommandText = "DECLARE @p nvarchar(4000) = @backupPath; DECLARE @sql nvarchar(max) = N'BACKUP DATABASE [PerfumeDb] TO DISK = N''' + REPLACE(@p, N'''', N'''''') + N''' WITH COPY_ONLY, INIT'; EXEC sys.sp_executesql @sql;";
                backup.Parameters.Add(new SqlParameter("@backupPath", backupPath));
                backup.CommandTimeout = 0;
                await backup.ExecuteNonQueryAsync(cancellationToken);
                logger.LogInformation("Database backup completed before applying schema update {Version}.", Version);
            }

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var migration = connection.CreateCommand();
                migration.Transaction = transaction;
                migration.CommandText = @"
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
                migration.Parameters.Add(new SqlParameter("@version", Version));
                await migration.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogInformation("Applied additive database schema update {Version}.", Version);
            }
            catch { await transaction.RollbackAsync(cancellationToken); throw; }
        }
        finally { if (closeWhenDone) await connection.CloseAsync(); }
    }
}
