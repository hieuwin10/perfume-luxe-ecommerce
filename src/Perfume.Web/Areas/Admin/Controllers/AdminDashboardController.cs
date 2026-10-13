using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.Interfaces;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,StoreManager")]
public class AdminDashboardController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly IDatabaseConsoleService _dbConsole;
    private readonly UserManager<AppUser> _userManager;
    private readonly IFlashSaleService _flashSaleService;

    public AdminDashboardController(
        PerfumeDbContext db, 
        IDatabaseConsoleService dbConsole, 
        UserManager<AppUser> userManager,
        IFlashSaleService flashSaleService)
    {
        _db = db;
        _dbConsole = dbConsole;
        _userManager = userManager;
        _flashSaleService = flashSaleService;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var vm = new AdminDashboardViewModel
        {
            TotalProducts = await _db.Products.CountAsync(),
            TotalOrders = await _db.Orders.CountAsync(),
            TotalUsers = await _db.Users.CountAsync(),
            TotalBrands = await _db.Brands.CountAsync(),
            TotalCategories = await _db.Categories.CountAsync(),
            TotalBlogPosts = await _db.BlogPosts.CountAsync(),
            TotalReviews = await _db.Reviews.CountAsync(),
            TotalVouchers = await _db.Vouchers.CountAsync(),
            RecentUsers = await _db.Users.OrderByDescending(u => u.CreatedAt).Take(8).ToListAsync(),
            RecentOrders = await _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(8)
                .ToListAsync(),
            Tables = await _dbConsole.GetAllTablesAsync(),
        };
        return View(vm);
    }

    // ─── USERS MANAGEMENT ─────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var usersWithRoles = new List<(AppUser User, IList<string> Roles)>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            usersWithRoles.Add((u, roles));
        }
        return View(usersWithRoles);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
    {
        if (newRole is not ("Admin" or "StoreManager" or "Customer"))
        {
            TempData["Error"] = "Role được chọn không hợp lệ.";
            return RedirectToAction(nameof(Users));
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal) && newRole != "Admin")
        {
            TempData["Error"] = "Không thể tự hạ quyền Admin của tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Users));
        }
        if (currentRoles.Contains("Admin") && newRole != "Admin" &&
            (await _userManager.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            TempData["Error"] = "Hệ thống cần giữ lại ít nhất một tài khoản Admin.";
            return RedirectToAction(nameof(Users));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var add = remove.Succeeded ? await _userManager.AddToRoleAsync(user, newRole) : remove;
        if (!add.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = string.Join(" ", add.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã đổi role của {user.FullName} sang {newRole}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal) ||
            (roles.Contains("Admin") && (await _userManager.GetUsersInRoleAsync("Admin")).Count <= 1))
        {
            TempData["Error"] = "Không thể tự xóa tài khoản đang đăng nhập hoặc xóa Admin cuối cùng.";
            return RedirectToAction(nameof(Users));
        }
        var name = user.FullName;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }
        TempData["Success"] = $"Đã xóa tài khoản {name}.";
        return RedirectToAction(nameof(Users));
    }

    // ─── PRODUCTS MANAGEMENT ──────────────────────────────────────────────────
    public async Task<IActionResult> Products(int page = 1, string? search = null)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);
        var query = _db.Products
            .Include(p => p.Variants)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Brand != null && p.Brand.Name.Contains(search)));

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Search = search;
        ViewBag.Total = total;
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) { p.IsDeleted = true; await _db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xóa sản phẩm (soft delete).";
        return RedirectToAction(nameof(Products));
    }

    // ─── ORDERS ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Orders(int page = 1)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);
        var total = await _db.Orders.CountAsync();
        var orders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Total = total;
        return View(orders);
    }

    // ─── ERD ──────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Erd()
    {
        var tables = await _dbConsole.GetAllTablesAsync();
        return View(tables);
    }

    // ─── FLASH SALES MANAGEMENT ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> FlashSales()
    {
        var flashSales = await _flashSaleService.GetAllFlashSalesAsync();
        var products = await _db.Products
            .Include(p => p.Brand)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Products = products;
        return View(flashSales);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFlashSale(
        string name, 
        string? description, 
        DateTime startTime, 
        DateTime endTime, 
        bool isActive, 
        int[] productIds, 
        decimal[] flashSalePrices, 
        int[] stockLimits)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Tên chiến dịch Flash Sale không được để trống.";
            return RedirectToAction(nameof(FlashSales));
        }

        if (endTime <= startTime)
        {
            TempData["Error"] = "Thời gian kết thúc phải diễn ra sau thời gian bắt đầu.";
            return RedirectToAction(nameof(FlashSales));
        }

        var req = new Perfume.Application.DTOs.CreateFlashSaleRequest
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            StartTime = startTime.ToUniversalTime(),
            EndTime = endTime.ToUniversalTime(),
            IsActive = isActive,
            Items = new List<Perfume.Application.DTOs.CreateFlashSaleItemRequest>()
        };

        if (productIds != null && productIds.Length > 0)
        {
            for (int i = 0; i < productIds.Length; i++)
            {
                if (productIds[i] > 0)
                {
                    var price = (flashSalePrices != null && i < flashSalePrices.Length) ? flashSalePrices[i] : 0;
                    var limit = (stockLimits != null && i < stockLimits.Length) ? stockLimits[i] : 10;
                    if (price <= 0)
                    {
                        var prod = await _db.Products.FindAsync(productIds[i]);
                        price = prod != null ? Math.Round(prod.BasePrice * 0.8m, -4) : 100000;
                    }
                    req.Items.Add(new Perfume.Application.DTOs.CreateFlashSaleItemRequest
                    {
                        ProductId = productIds[i],
                        FlashSalePrice = price,
                        StockLimit = limit > 0 ? limit : 10
                    });
                }
            }
        }

        await _flashSaleService.CreateFlashSaleAsync(req);
        TempData["Success"] = $"Đã tạo chiến dịch Flash Sale '{req.Name}' thành công!";
        return RedirectToAction(nameof(FlashSales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFlashSaleStatus(int id)
    {
        var ok = await _flashSaleService.ToggleStatusAsync(id);
        if (ok)
            TempData["Success"] = "Đã cập nhật trạng thái chiến dịch Flash Sale.";
        else
            TempData["Error"] = "Không tìm thấy chiến dịch Flash Sale.";

        return RedirectToAction(nameof(FlashSales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFlashSale(int id)
    {
        var ok = await _flashSaleService.DeleteAsync(id);
        if (ok)
            TempData["Success"] = "Đã xóa chiến dịch Flash Sale.";
        else
            TempData["Error"] = "Không thể xóa chiến dịch Flash Sale.";

        return RedirectToAction(nameof(FlashSales));
    }
}

public class AdminDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int TotalBrands { get; set; }
    public int TotalCategories { get; set; }
    public int TotalBlogPosts { get; set; }
    public int TotalReviews { get; set; }
    public int TotalVouchers { get; set; }
    public IList<AppUser> RecentUsers { get; set; } = new List<AppUser>();
    public IList<Perfume.Domain.Entities.Order> RecentOrders { get; set; } = new List<Perfume.Domain.Entities.Order>();
    public IList<TableInfo> Tables { get; set; } = new List<TableInfo>();
}
