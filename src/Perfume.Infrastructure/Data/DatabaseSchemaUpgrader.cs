using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure;

public static class DatabaseSchemaUpgrader
{
    private const string Version = "20261010_01_review_images_and_db_audit";

    public static async Task ApplyAsync(PerfumeDbContext db, IConfiguration configuration, ILogger logger, bool databaseCreatedThisStart, CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var closeWhenDone = connection.State != System.Data.ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "IF OBJECT_ID(N'dbo.__PerfumeSchemaVersions', N'U') IS NULL BEGIN CREATE TABLE dbo.__PerfumeSchemaVersions (Version nvarchar(120) NOT NULL PRIMARY KEY, AppliedAt datetime2 NOT NULL CONSTRAINT DF_PerfumeSchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME()); END";
                await command.ExecuteNonQueryAsync(cancellationToken);
                command.CommandText = "SELECT COUNT(1) FROM dbo.__PerfumeSchemaVersions WHERE Version = @version";
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
                migration.CommandText = "IF COL_LENGTH(N'dbo.Reviews', N'ImageUrls') IS NULL ALTER TABLE dbo.Reviews ADD ImageUrls nvarchar(max) NULL; IF OBJECT_ID(N'dbo.DatabaseAuditLogs', N'U') IS NULL BEGIN CREATE TABLE dbo.DatabaseAuditLogs (Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseAuditLogs PRIMARY KEY, IsDeleted bit NOT NULL CONSTRAINT DF_DatabaseAuditLogs_IsDeleted DEFAULT 0, CreatedAt datetime2 NOT NULL CONSTRAINT DF_DatabaseAuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2 NULL, ActorUserId nvarchar(450) NULL, Action nvarchar(80) NOT NULL, Target nvarchar(128) NULL, CommandHash varchar(64) NULL, Succeeded bit NOT NULL, RowsAffected int NULL, ElapsedMilliseconds bigint NOT NULL CONSTRAINT DF_DatabaseAuditLogs_Elapsed DEFAULT 0); CREATE INDEX IX_DatabaseAuditLogs_CreatedAt ON dbo.DatabaseAuditLogs(CreatedAt); END; INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
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