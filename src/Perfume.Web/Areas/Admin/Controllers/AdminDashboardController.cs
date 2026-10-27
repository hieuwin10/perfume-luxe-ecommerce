using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Web.Services;

namespace Perfume.Web.Areas.Admin.Controllers;

[NonController]
[Area("Admin")]
[Authorize(Roles = "Admin,StoreManager")]
public sealed class AdminDashboardController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly IDatabaseConsoleService _dbConsole;
    private readonly UserManager<AppUser> _userManager;
    private readonly IFlashSaleService _flashSaleService;
    private readonly ISystemSettingService _settingService;
    private readonly IBlogService _blogService;
    private readonly IConfiguration _configuration;

    public AdminDashboardController(
        PerfumeDbContext db,
        IDatabaseConsoleService dbConsole,
        UserManager<AppUser> userManager,
        IFlashSaleService flashSaleService,
        ISystemSettingService settingService,
        IBlogService blogService,
        IConfiguration configuration)
    {
        _db = db;
        _dbConsole = dbConsole;
        _userManager = userManager;
        _flashSaleService = flashSaleService;
        _settingService = settingService;
        _blogService = blogService;
        _configuration = configuration;
    }

    [Authorize(Roles = "Admin,StoreManager")]
    public Task<IActionResult> Index() => Forward(new AdminAnalyticsController(_db, new AdminDashboardAnalyticsService(_db, _dbConsole))).Index();

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> FinancialReport(DateTime? fromDate, DateTime? toDate, string? preset = "30days") =>
        Forward(new AdminAnalyticsController(_db, new AdminDashboardAnalyticsService(_db, _dbConsole))).FinancialReport(fromDate, toDate, preset);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ExportFinancialOrdersCsv(DateTime? fromDate, DateTime? toDate) =>
        Forward(new AdminAnalyticsController(_db, new AdminDashboardAnalyticsService(_db, _dbConsole))).ExportFinancialOrdersCsv(fromDate, toDate);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ExportInventoryValuationCsv() =>
        Forward(new AdminAnalyticsController(_db, new AdminDashboardAnalyticsService(_db, _dbConsole))).ExportInventoryValuationCsv();

    public Task<IActionResult> Inventory(string? filter = "all", string? search = null) =>
        Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).Inventory(filter, search);

    public Task<IActionResult> AdjustStock(int variantId, int adjustedQuantity, decimal? unitCost, string reason, string? note) =>
        Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).AdjustStock(variantId, adjustedQuantity, unitCost, reason, note);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> CreateFlashSale(string name, string? description, DateTime startTime, DateTime endTime, bool isActive, int[] productIds, decimal[] flashSalePrices, int[] stockLimits) =>
        Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).CreateFlashSale(name, description, startTime, endTime, isActive, productIds, flashSalePrices, stockLimits);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> SaveBanner(int? id, string title, string? subtitle, string imageUrl, string? targetUrl, string? buttonText, int displayOrder, bool isActive) =>
        Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).SaveBanner(id, title, subtitle, imageUrl, targetUrl, buttonText, displayOrder, isActive);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ToggleBannerActive(int id) => Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).ToggleBannerActive(id);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> DeleteBanner(int id) => Forward(new AdminInventoryController(_db, _userManager, _flashSaleService)).DeleteBanner(id);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ChangeUserRole(string userId, string newRole) => Forward(CreateSystemController()).ChangeUserRole(userId, newRole);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> DeleteUser(string userId) => Forward(CreateSystemController()).DeleteUser(userId);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Tastings(string? status) => Forward(CreateSystemController()).Tastings(status);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> UpdateTastingStatus(int id, string status) => Forward(CreateSystemController()).UpdateTastingStatus(id, status);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> AuditLogs(string? actionFilter, bool? succeededFilter, string? actorFilter, int page = 1) =>
        Forward(CreateSystemController()).AuditLogs(actionFilter, succeededFilter, actorFilter, page);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> SystemHealth() => Forward(CreateSystemController()).SystemHealth();

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> FlushCache() => Forward(CreateSystemController()).FlushCache();

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> Users() => Forward(CreateSystemController()).Users();

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ErrorLogs(string? search, int? statusCode, int page = 1) =>
        Forward(CreateSystemController()).ErrorLogs(search, statusCode, page);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ClearErrorLogs(string mode) =>
        Forward(CreateSystemController()).ClearErrorLogs(mode);

    [Authorize(Roles = "Admin")]
    public Task<IActionResult> ToggleLockUser(string userId) =>
        Forward(CreateSystemController()).ToggleLockUser(userId);


    private AdminSystemController CreateSystemController() => new(
        _db, _dbConsole, _userManager, _settingService, _blogService, _configuration);

    private T Forward<T>(T controller) where T : Controller
    {
        controller.ControllerContext = ControllerContext;
        controller.TempData = TempData;
        return controller;
    }
}
