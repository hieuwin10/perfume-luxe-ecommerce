namespace Perfume.Application.Interfaces;

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
