namespace Perfume.Application.Interfaces;

public class TableInfo
{
    public string Schema { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public decimal TotalSpaceMB { get; set; }
    public DateTime? CreateDate { get; set; }
}

public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
}

public class TableDataResult
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRows / PageSize);
}

public class QueryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public int RowsAffected { get; set; }
    public bool Truncated { get; set; }
    public List<string> ColumnNames { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public interface IDatabaseConsoleService
{
    Task<List<TableInfo>> GetAllTablesAsync();
    Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName);
    Task<TableDataResult> GetTableDataAsync(string tableName, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, bool isAscending = true);
    Task<Dictionary<string, object?>?> GetTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue);
    Task<bool> InsertTableRowAsync(string tableName, Dictionary<string, object?> values);
    Task<bool> UpdateTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue, Dictionary<string, object?> values);
    Task<bool> DeleteTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue);
    Task<QueryResult> ExecuteRawQueryAsync(string sql);
    Task<QueryResult> ExecuteScriptFileAsync(string scriptContent);
}
