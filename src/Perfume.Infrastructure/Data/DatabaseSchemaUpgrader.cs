using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure;

public static class DatabaseSchemaUpgrader
{
    private static readonly (string Version, string Sql)[] Migrations =
    [
        (DatabaseSchemaMigrationV1.Version, DatabaseSchemaMigrationV1.Sql),
        (DatabaseSchemaMigrationV2.Version, DatabaseSchemaMigrationV2.Sql),
        (DatabaseSchemaMigrationV3.Version, DatabaseSchemaMigrationV3.Sql),
        (DatabaseSchemaMigrationV4.Version, DatabaseSchemaMigrationV4.Sql),
        (DatabaseSchemaMigrationV5.Version, DatabaseSchemaMigrationV5.Sql)
    ];

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
