using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly IDatabaseConsoleService _dbConsole;

    public DashboardController(IDatabaseConsoleService dbConsole)
    {
        _dbConsole = dbConsole;
    }

    public async Task<IActionResult> Index()
    {
        var tables = await _dbConsole.GetAllTablesAsync();
        return View(tables);
    }
}
