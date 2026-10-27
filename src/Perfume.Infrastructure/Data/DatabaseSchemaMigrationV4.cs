namespace Perfume.Infrastructure;

internal static class DatabaseSchemaMigrationV4
{
    public const string Version = "20261013_04_system_error_logs";
    public const string Sql = @"
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
INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
}
