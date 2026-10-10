using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DatabaseConsoleController : Controller
{
    private readonly IDatabaseConsoleService _dbConsole;
    private readonly IWebHostEnvironment _env;

    public DatabaseConsoleController(IDatabaseConsoleService dbConsole, IWebHostEnvironment env)
    {
        _dbConsole = dbConsole;
        _env = env;
    }

    public async Task<IActionResult> Index(string? table, int page = 1, int pageSize = 15, string? sortBy = null, bool asc = true)
    {
        var tables = await _dbConsole.GetAllTablesAsync();
        
        string activeTable = table ?? tables.FirstOrDefault(t => t.TableName == "Products")?.TableName 
                             ?? tables.FirstOrDefault()?.TableName ?? "";

        TableDataResult? tableData = null;
        if (!string.IsNullOrEmpty(activeTable))
        {
            try
            {
                tableData = await _dbConsole.GetTableDataAsync(activeTable, page, pageSize, null, sortBy, asc);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }
        }

        ViewBag.Tables = tables;
        ViewBag.ActiveTable = activeTable;
        return View(tableData);
    }

    [HttpGet]
    public async Task<IActionResult> GetColumns(string table)
    {
        var columns = await _dbConsole.GetTableColumnsAsync(table);
        return Json(columns);
    }

    [HttpGet]
    public async Task<IActionResult> GetRow(string table, string pkCol, string pkVal)
    {
        var row = await _dbConsole.GetTableRowAsync(table, pkCol, pkVal);
        if (row == null) return NotFound();
        return Json(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRow(string table, string? pkCol, string? pkVal, IFormCollection form)
    {
        var values = new Dictionary<string, object?>();
        foreach (var key in form.Keys)
        {
            if (key == "__RequestVerificationToken" || key == "table" || key == "pkCol" || key == "pkVal") 
                continue;

            var strVal = form[key].ToString();
            values[key] = string.IsNullOrWhiteSpace(strVal) ? null : strVal;
        }

        bool success;
        if (!string.IsNullOrEmpty(pkCol) && !string.IsNullOrEmpty(pkVal))
        {
            success = await _dbConsole.UpdateTableRowAsync(table, pkCol, pkVal, values);
        }
        else
        {
            success = await _dbConsole.InsertTableRowAsync(table, values);
        }

        TempData["Message"] = success ? "Lưu bản ghi thành công!" : "Không thể lưu bản ghi.";
        return RedirectToAction(nameof(Index), new { table });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRow(string table, string pkCol, string pkVal)
    {
        try
        {
            var success = await _dbConsole.DeleteTableRowAsync(table, pkCol, pkVal);
            TempData["Message"] = success ? "Đã xóa bản ghi thành công!" : "Xóa thất bại.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi xóa: {ex.Message}";
        }
        return RedirectToAction(nameof(Index), new { table });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Sql))
        {
            return Json(new { success = false, errorMessage = "Câu lệnh SQL không được để trống." });
        }

        var result = await _dbConsole.ExecuteRawQueryAsync(model.Sql);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteScript([FromBody] ScriptModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Script))
        {
            return Json(new { success = false, errorMessage = "Nội dung Script trống." });
        }

        var result = await _dbConsole.ExecuteScriptFileAsync(model.Script);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDefaultSeedScript()
    {
        try
        {
            // Read scripts/seed_perfume_data.sql from project root or current working dir
            var candidates = new[]
            {
                Path.Combine(_env.ContentRootPath, "..", "..", "scripts", "seed_perfume_data.sql"),
                Path.Combine(_env.ContentRootPath, "scripts", "seed_perfume_data.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "scripts", "seed_perfume_data.sql")
            };

            string? scriptPath = candidates.FirstOrDefault(System.IO.File.Exists);
            if (scriptPath == null)
            {
                return Json(new { success = false, errorMessage = "Không tìm thấy file scripts/seed_perfume_data.sql trên server." });
            }

            var scriptContent = await System.IO.File.ReadAllTextAsync(scriptPath);
            var result = await _dbConsole.ExecuteScriptFileAsync(scriptContent);
            return Json(new { success = result.Success, errorMessage = result.ErrorMessage, rowsAffected = result.RowsAffected, elapsed = result.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, errorMessage = ex.Message });
        }
    }
}

public class QueryModel
{
    public string Sql { get; set; } = string.Empty;
}

public class ScriptModel
{
    public string Script { get; set; } = string.Empty;
}
