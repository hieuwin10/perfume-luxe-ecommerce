using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure;

/// <summary>
/// Bo nang cap Database Schema tu dong, quan ly cac ban migration tuy chinh V1 den V6
/// va dam bao tinh an toan qua SQL Transaction rollback.
/// </summary>
public static class DatabaseSchemaUpgrader
{
    // ==========================================
    // 1. DANH SACH CAC BAN MIGRATION (V1 - V6)
    // ==========================================
    private static readonly (string Version, string Sql)[] Migrations =
    [
        (DatabaseSchemaMigrationV1.Version, DatabaseSchemaMigrationV1.Sql),
        (DatabaseSchemaMigrationV2.Version, DatabaseSchemaMigrationV2.Sql),
        (DatabaseSchemaMigrationV3.Version, DatabaseSchemaMigrationV3.Sql),
        (DatabaseSchemaMigrationV4.Version, DatabaseSchemaMigrationV4.Sql),
        (DatabaseSchemaMigrationV5.Version, DatabaseSchemaMigrationV5.Sql),
        (DatabaseSchemaMigrationV6.Version, DatabaseSchemaMigrationV6.Sql)
    ];

    // ==========================================
    // 2. PHUONG THUC THUC THI CHINH (APPLY ASYNC)
    // ==========================================
    public static async Task ApplyAsync(PerfumeDbContext db, IConfiguration configuration, ILogger logger, bool databaseCreatedThisStart, CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);

        try
        {
            // Buoc 1: Tao bang quan ly phien ban neu chua ton tai
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

            // Buoc 2: Doc danh sach cac phien ban da ap dung tu truoc
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

            // Buoc 3: Loc ra nhung ban migration chua duoc thuc thi
            var pendingMigrations = Migrations.Where(m => !appliedVersions.Contains(m.Version)).ToList();
            if (!pendingMigrations.Any())
            {
                return;
            }

            // Buoc 4: Sao luu (Backup) co so du lieu truoc khi chay neu duoc cau hinh BackupPath
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

            // Buoc 5: Chay tung migration ben trong SQL Transaction co Rollback neu gap loi
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
                    // Rollback transaction ngay lap tuc de bao ve tinh toan ven cua CSDL
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogError(ex, "Failed to apply additive schema update {Version}.", version);
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
