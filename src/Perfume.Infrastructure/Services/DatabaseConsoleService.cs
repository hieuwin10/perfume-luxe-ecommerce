using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Perfume.Application.Interfaces;

namespace Perfume.Infrastructure.Services;

public class DatabaseConsoleService : IDatabaseConsoleService
{
    private readonly string _connectionString;

    public DatabaseConsoleService(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not found.");
        var builder = new SqlConnectionStringBuilder(configured);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog) || !builder.InitialCatalog.Contains("PerfumeDb", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tên Database trong ConnectionString không thuộc PerfumeDb hợp lệ.");
        _connectionString = builder.ConnectionString;
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<List<TableInfo>> GetAllTablesAsync()
    {
        using var db = CreateConnection();
        const string sql = @"
            SELECT 
                s.name AS [Schema],
                t.name AS [TableName],
                p.rows AS [RowCount],
                CAST(ROUND(((SUM(a.total_pages) * 8) / 1024.00), 2) AS NUMERIC(36, 2)) AS [TotalSpaceMB],
                t.create_date AS [CreateDate]
            FROM sys.tables t
            INNER JOIN sys.indexes i ON t.object_id = i.object_id
            INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
            INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
            LEFT OUTER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.is_ms_shipped = 0 AND i.index_id <= 1
            GROUP BY t.name, s.name, p.rows, t.create_date
            ORDER BY t.name;";

        var result = await db.QueryAsync<TableInfo>(sql);
        return result.ToList();
    }

    public async Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName)
    {
        using var db = CreateConnection();
        const string sql = @"
            SELECT 
                c.COLUMN_NAME AS ColumnName,
                c.DATA_TYPE AS DataType,
                c.CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                CAST(CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS BIT) AS IsNullable,
                CAST(CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsPrimaryKey,
                CAST(COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS BIT) AS IsIdentity,
                c.COLUMN_DEFAULT AS DefaultValue
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA AND c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION;";

        var columns = await db.QueryAsync<ColumnInfo>(sql, new { TableName = tableName });
        return columns.ToList();
    }

    public async Task<TableDataResult> GetTableDataAsync(string tableName, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, bool isAscending = true)
    {
        page = Math.Clamp(page, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var safeTableName = await ValidateTableNameAsync(tableName);
        var columns = await GetTableColumnsAsync(safeTableName);
        
        var primaryKeyCol = columns.FirstOrDefault(c => c.IsPrimaryKey)?.ColumnName 
                            ?? columns.FirstOrDefault()?.ColumnName 
                            ?? "Id";

        var sortCol = columns.Any(c => c.ColumnName.Equals(sortBy, StringComparison.OrdinalIgnoreCase)) 
            ? sortBy! 
            : primaryKeyCol;

        var sortDirection = isAscending ? "ASC" : "DESC";

        using var db = CreateConnection();

        // Count total rows
        string countSql = $"SELECT COUNT(1) FROM [{safeTableName}]";
        int totalRows = await db.ExecuteScalarAsync<int>(countSql);

        // Fetch paginated rows
        int offset = (Math.Max(1, page) - 1) * pageSize;
        string dataSql = $@"
            SELECT * FROM [{safeTableName}]
            ORDER BY [{sortCol}] {sortDirection}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

        var rawRows = await db.QueryAsync(dataSql, new { Offset = offset, PageSize = pageSize });

        var rows = new List<Dictionary<string, object?>>();
        foreach (IDictionary<string, object> row in rawRows)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in row)
            {
                dict[kvp.Key] = kvp.Value;
            }
            rows.Add(dict);
        }

        return new TableDataResult
        {
            TableName = safeTableName,
            Columns = columns,
            Rows = rows,
            TotalRows = totalRows,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<Dictionary<string, object?>?> GetTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue)
    {
        var safeTableName = await ValidateTableNameAsync(tableName);
        var columns = await GetTableColumnsAsync(safeTableName);
        if (!columns.Any(c => c.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Invalid primary key column");

        using var db = CreateConnection();
        string sql = $"SELECT TOP 1 * FROM [{safeTableName}] WHERE [{primaryKeyColumn}] = @Id";
        var rawRow = await db.QueryFirstOrDefaultAsync(sql, new { Id = primaryKeyValue });
        if (rawRow == null) return null;

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in (IDictionary<string, object>)rawRow)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }

    public async Task<bool> InsertTableRowAsync(string tableName, Dictionary<string, object?> values)
    {
        var safeTableName = await ValidateTableNameAsync(tableName);
        var columns = await GetTableColumnsAsync(safeTableName);

        // Exclude identity columns from INSERT
        var validColumns = columns.Where(c => !c.IsIdentity && values.ContainsKey(c.ColumnName)).ToList();
        if (!validColumns.Any()) return false;

        var colNames = string.Join(", ", validColumns.Select(c => $"[{c.ColumnName}]"));
        var paramNames = string.Join(", ", validColumns.Select(c => $"@{c.ColumnName}"));

        var sql = $"INSERT INTO [{safeTableName}] ({colNames}) VALUES ({paramNames})";

        var parameters = new DynamicParameters();
        foreach (var col in validColumns)
        {
            parameters.Add(col.ColumnName, values[col.ColumnName]);
        }

        using var db = CreateConnection();
        var affected = await db.ExecuteAsync(sql, parameters);
        return affected > 0;
    }

    public async Task<bool> UpdateTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue, Dictionary<string, object?> values)
    {
        var safeTableName = await ValidateTableNameAsync(tableName);
        var columns = await GetTableColumnsAsync(safeTableName);

        var validColumns = columns.Where(c => !c.IsPrimaryKey && !c.IsIdentity && values.ContainsKey(c.ColumnName)).ToList();
        if (!validColumns.Any()) return false;

        var setClauses = string.Join(", ", validColumns.Select(c => $"[{c.ColumnName}] = @{c.ColumnName}"));
        var sql = $"UPDATE [{safeTableName}] SET {setClauses} WHERE [{primaryKeyColumn}] = @__PkVal";

        var parameters = new DynamicParameters();
        parameters.Add("__PkVal", primaryKeyValue);
        foreach (var col in validColumns)
        {
            parameters.Add(col.ColumnName, values[col.ColumnName]);
        }

        using var db = CreateConnection();
        var affected = await db.ExecuteAsync(sql, parameters);
        return affected > 0;
    }

    public async Task<bool> DeleteTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue)
    {
        var safeTableName = await ValidateTableNameAsync(tableName);
        var columns = await GetTableColumnsAsync(safeTableName);
        if (!columns.Any(c => c.ColumnName.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Invalid primary key column");

        using var db = CreateConnection();
        string sql = $"DELETE FROM [{safeTableName}] WHERE [{primaryKeyColumn}] = @Id";
        var affected = await db.ExecuteAsync(sql, new { Id = primaryKeyValue });
        return affected > 0;
    }

    public async Task<QueryResult> ExecuteRawQueryAsync(string sql)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult();

        try
        {
            EnsureApplicationDatabaseScope(sql);
            using var db = CreateConnection();
            await db.OpenAsync();

            var trimmed = sql.Trim();
            bool isSelect = trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase);

            if (isSelect)
            {
                using var reader = await db.ExecuteReaderAsync(new CommandDefinition(sql, commandTimeout: 30));
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.ColumnNames.Add(reader.GetName(i));
                }

                while (result.Rows.Count < 200 && await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var colName = reader.GetName(i);
                        row[colName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    result.Rows.Add(row);
                }
                result.RowsAffected = result.Rows.Count;
                result.Truncated = await reader.ReadAsync();
            }
            else
            {
                result.RowsAffected = await db.ExecuteAsync(new CommandDefinition(sql, commandTimeout: 30));
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<QueryResult> ExecuteScriptFileAsync(string scriptContent)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult();

        try
        {
            EnsureApplicationDatabaseScope(scriptContent);
            using var db = CreateConnection();
            await db.OpenAsync();

            // Split by T-SQL GO command
            var batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            int totalAffected = 0;

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                totalAffected += await db.ExecuteAsync(new CommandDefinition(trimmed, commandTimeout: 60));
            }

            result.Success = true;
            result.RowsAffected = totalAffected;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    private static void EnsureApplicationDatabaseScope(string sql)
    {
        var normalized = Regex.Replace(sql, @"--[^\r\n]*|/\*[\s\S]*?\*/|N?'(?:''|[^'])*'", " ", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(normalized, @"\b(USE|OPENROWSET|OPENDATASOURCE|OPENQUERY|BACKUP|RESTORE|ATTACH|DETACH|EXEC|EXECUTE)\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(normalized, @"(?:\[[^\]]+\]|[A-Za-z_][\w$#]*)\s*\.\s*(?:(?:\[[^\]]+\]|[A-Za-z_][\w$#]*)\s*)?\.\s*(?:\[[^\]]+\]|[A-Za-z_][\w$#]*)", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(normalized, @"\b(DBCC|SHUTDOWN|KILL|CREATE\s+LOGIN|ALTER\s+LOGIN|DROP\s+LOGIN|CREATE\s+SERVER|ALTER\s+SERVER|DROP\s+SERVER)\b", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Database Console chỉ cho phép thao tác trong database PerfumeDb; lệnh đổi database hoặc tham chiếu database khác bị chặn.");
    }

    private async Task<string> ValidateTableNameAsync(string tableName)
    {
        var tables = await GetAllTablesAsync();
        var matched = tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
        if (matched == null)
        {
            throw new ArgumentException($"Bảng '{tableName}' không tồn tại trong cơ sở dữ liệu.");
        }
        return matched.TableName;
    }
}
