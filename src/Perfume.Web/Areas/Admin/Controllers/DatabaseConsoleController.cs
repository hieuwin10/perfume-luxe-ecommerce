using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DatabaseConsoleController : Controller
{
    private readonly IDatabaseConsoleService _dbConsole;
    private readonly IWebHostEnvironment _env;
    private readonly PerfumeDbContext _db;
    private readonly ILogger<DatabaseConsoleController> _logger;

    public DatabaseConsoleController(IDatabaseConsoleService dbConsole, IWebHostEnvironment env, PerfumeDbContext db, ILogger<DatabaseConsoleController> logger)
    { _dbConsole = dbConsole; _env = env; _db = db; _logger = logger; }

    public async Task<IActionResult> Index(string? table, int page = 1, int pageSize = 15, string? sortBy = null, bool asc = true)
    {
        List<TableInfo> tables;
        try
        {
            tables = await _dbConsole.GetAllTablesAsync();
            ViewBag.ConnectionStatus = "Đang hoạt động";
        }
        catch
        {
            tables = new List<TableInfo>();
            ViewBag.ConnectionStatus = "Không kết nối được";
            ViewBag.Error = "Không thể kết nối database PerfumeDb.";
        }
        var activeTable = table ?? tables.FirstOrDefault(t => t.TableName == "Products")?.TableName ?? tables.FirstOrDefault()?.TableName ?? "";
        TableDataResult? tableData = null;
        if (!string.IsNullOrEmpty(activeTable))
        {
            try { tableData = await _dbConsole.GetTableDataAsync(activeTable, page, Math.Clamp(pageSize, 1, 100), null, sortBy, asc); }
            catch { ViewBag.Error = "Không thể mở bảng đã chọn. Kiểm tra tên bảng và database."; }
        }
        ViewBag.Tables = tables;
        ViewBag.ActiveTable = activeTable;
        return View(tableData);
    }

    [HttpGet]
    public async Task<IActionResult> GetColumns(string table) => Json(await _dbConsole.GetTableColumnsAsync(table));

    [HttpGet]
    public async Task<IActionResult> GetRow(string table, string pkCol, string pkVal)
    {
        var row = await _dbConsole.GetTableRowAsync(table, pkCol, pkVal);
        return row == null ? NotFound() : Json(row);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRow(string table, string? pkCol, string? pkVal, bool confirmed, IFormCollection form)
    {
        if (!confirmed) return BadRequest("Xác nhận riêng trước khi ghi dữ liệu.");
        var values = new Dictionary<string, object?>();
        foreach (var key in form.Keys)
        {
            if (key is "__RequestVerificationToken" or "table" or "pkCol" or "pkVal" or "confirmed") continue;
            var value = form[key].ToString();
            values[key] = string.IsNullOrWhiteSpace(value) ? null : value;
        }
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var success = !string.IsNullOrWhiteSpace(pkCol) && !string.IsNullOrWhiteSpace(pkVal)
                ? await _dbConsole.UpdateTableRowAsync(table, pkCol, pkVal, values)
                : await _dbConsole.InsertTableRowAsync(table, values);
            await AuditAsync(success ? "ROW_SAVE" : "ROW_SAVE_FAILED", table, null, success, success ? 1 : 0, timer.ElapsedMilliseconds);
            TempData[success ? "Message" : "Error"] = success ? "Lưu bản ghi thành công!" : "Không thể lưu bản ghi.";
        }
        catch
        {
            await AuditAsync("ROW_SAVE_FAILED", table, null, false, null, timer.ElapsedMilliseconds);
            TempData["Error"] = "Không thể lưu bản ghi. Kiểm tra dữ liệu nhập và ràng buộc của bảng.";
        }
        return RedirectToAction(nameof(Index), new { table });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRow(string table, string pkCol, string pkVal, bool confirmed)
    {
        if (!confirmed) return BadRequest("Xác nhận riêng trước khi xóa dữ liệu.");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var success = await _dbConsole.DeleteTableRowAsync(table, pkCol, pkVal);
            await AuditAsync(success ? "ROW_DELETE" : "ROW_DELETE_FAILED", table, null, success, success ? 1 : 0, timer.ElapsedMilliseconds);
            TempData[success ? "Message" : "Error"] = success ? "Đã xóa bản ghi thành công!" : "Xóa thất bại.";
        }
        catch
        {
            await AuditAsync("ROW_DELETE_FAILED", table, null, false, null, timer.ElapsedMilliseconds);
            TempData["Error"] = "Không thể xóa bản ghi. Kiểm tra ràng buộc dữ liệu.";
        }
        return RedirectToAction(nameof(Index), new { table });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Sql)) return Json(new { success = false, errorMessage = "Câu lệnh SQL không được để trống." });
        if (RequiresConfirmation(model.Sql) && !model.Confirmed)
            return Json(new { success = false, confirmationRequired = true, errorMessage = "Lệnh này có thể thay đổi dữ liệu hoặc cấu trúc. Xác nhận riêng để tiếp tục." });
        var result = await _dbConsole.ExecuteRawQueryAsync(model.Sql);
        await AuditAsync("SQL_QUERY", "PerfumeDb", model.Sql, result.Success, result.RowsAffected, result.ElapsedMilliseconds);
        return Json(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteScript([FromBody] ScriptModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Script)) return Json(new { success = false, errorMessage = "Nội dung Script trống." });
        if (!model.Confirmed) return Json(new { success = false, confirmationRequired = true, errorMessage = "Script có thể thay đổi nhiều dữ liệu. Xác nhận riêng để tiếp tục." });
        var result = await _dbConsole.ExecuteScriptFileAsync(model.Script);
        await AuditAsync("SQL_SCRIPT", "PerfumeDb", model.Script, result.Success, result.RowsAffected, result.ElapsedMilliseconds);
        return Json(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDefaultSeedScript(bool confirmed)
    {
        if (!confirmed) return Json(new { success = false, confirmationRequired = true, errorMessage = "Seed script sẽ ghi dữ liệu. Xác nhận để tiếp tục." });
        try
        {
            var paths = new[]
            {
                Path.Combine(_env.ContentRootPath, "..", "..", "scripts", "seed_perfume_data.sql"),
                Path.Combine(_env.ContentRootPath, "scripts", "seed_perfume_data.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "scripts", "seed_perfume_data.sql")
            };
            var path = paths.FirstOrDefault(System.IO.File.Exists);
            if (path == null) return Json(new { success = false, errorMessage = "Không tìm thấy scripts/seed_perfume_data.sql." });
            var script = await System.IO.File.ReadAllTextAsync(path);
            var result = await _dbConsole.ExecuteScriptFileAsync(script);
            await AuditAsync("SEED_SCRIPT", "PerfumeDb", script, result.Success, result.RowsAffected, result.ElapsedMilliseconds);
            return Json(new { success = result.Success, errorMessage = result.ErrorMessage, rowsAffected = result.RowsAffected, elapsed = result.ElapsedMilliseconds });
        }
        catch
        {
            await AuditAsync("SEED_SCRIPT", "PerfumeDb", null, false, null, 0);
            return Json(new { success = false, errorMessage = "Không thể nạp seed script. Kiểm tra file và quyền database." });
        }
    }

    private static bool RequiresConfirmation(string sql)
    {
        var normalized = Regex.Replace(sql, @"--[^\r\n]*|/\*[\s\S]*?\*/|N?'(?:''|[^'])*'", " ", RegexOptions.IgnoreCase).Trim();
        if (!Regex.IsMatch(normalized, @"^SELECT\b", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(normalized, @"\b(INTO|INSERT|UPDATE|DELETE|MERGE|ALTER|DROP|CREATE|TRUNCATE|EXEC|EXECUTE|GRANT|REVOKE|DBCC|BACKUP|RESTORE)\b", RegexOptions.IgnoreCase)) return true;
        var semicolons = normalized.Count(c => c == ';');
        return semicolons > 1 || (semicolons == 1 && !normalized.TrimEnd().EndsWith(';'));
    }

    private async Task AuditAsync(string action, string target, string? command, bool success, int? rows, long elapsed)
    {
        try
        {
            var safeTarget = Regex.Replace(target, @"[^A-Za-z0-9_.-]", "");
            if (safeTarget.Length > 128) safeTarget = safeTarget[..128];
            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                Action = action,
                Target = safeTarget,
                CommandHash = string.IsNullOrEmpty(command) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command))),
                Succeeded = success,
                RowsAffected = rows,
                ElapsedMilliseconds = elapsed
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Could not write a Database Console audit entry."); }
    }
}

public class QueryModel { public string Sql { get; set; } = string.Empty; public bool Confirmed { get; set; } }
public class ScriptModel { public string Script { get; set; } = string.Empty; public bool Confirmed { get; set; } }
