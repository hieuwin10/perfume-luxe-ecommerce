using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
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
    private readonly ISystemSettingService _settingService;

    public AdminDashboardController(PerfumeDbContext db, IDatabaseConsoleService dbConsole, UserManager<AppUser> userManager, ISystemSettingService settingService)
    {
        _db = db;
        _dbConsole = dbConsole;
        _userManager = userManager;
        _settingService = settingService;
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

    // ─── ORDER DETAILS & PACKING SLIP ─────────────────────────────────────────
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> PrintInvoice(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        var settings = await _settingService.GetSettingsAsync();
        ViewBag.Settings = settings;
        return View(order);
    }

    // ─── CREATE PRODUCT ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        ViewBag.Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.FragranceFamilies = await _db.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
        return View(new CreateProductDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FragranceFamilies = await _db.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
            return View(model);
        }

        // Tạo slug tự động nếu trống
        var slug = string.IsNullOrWhiteSpace(model.Slug)
            ? System.Text.RegularExpressions.Regex.Replace(model.Name.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD), @"[^a-z0-9\s-]", "").Replace(" ", "-")
            : model.Slug.Trim().ToLowerInvariant();

        // Kiểm tra trùng slug
        if (await _db.Products.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        var product = new Product
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Sku = model.Sku.Trim().ToUpperInvariant(),
            BrandId = model.BrandId,
            CategoryId = model.CategoryId,
            FragranceFamilyId = model.FragranceFamilyId,
            Gender = model.Gender,
            Concentration = model.Concentration,
            BasePrice = model.BasePrice,
            MainImageUrl = string.IsNullOrWhiteSpace(model.MainImageUrl)
                ? "https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80"
                : model.MainImageUrl.Trim(),
            ShortDescription = model.ShortDescription?.Trim(),
            FullDescription = model.FullDescription?.Trim(),
            Longevity = model.Longevity?.Trim() ?? "7 - 12 giờ",
            Sillage = model.Sillage?.Trim() ?? "Toả hương trong bán kính 1 - 2 mét",
            ReleaseYear = model.ReleaseYear,
            SeasonRecommendation = model.SeasonRecommendation?.Trim() ?? "Bốn mùa",
            IsFeatured = model.IsFeatured,
            IsBestSeller = model.IsBestSeller,
            IsNewArrival = model.IsNewArrival,
            IsInStock = true,
            RatingAverage = 5.0,
            RatingCount = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Thêm ảnh chính vào Images collection
        product.Images.Add(new ProductImage
        {
            ImageUrl = product.MainImageUrl,
            IsMain = true,
            AltText = product.Name,
            DisplayOrder = 1
        });

        // Tạo biến thể ban đầu
        product.Variants.Add(new ProductVariant
        {
            VolumeMl = model.InitialVolumeMl > 0 ? model.InitialVolumeMl : 100,
            VolumeLabel = string.IsNullOrWhiteSpace(model.InitialVolumeLabel) ? $"Chai Full {model.InitialVolumeMl}ml" : model.InitialVolumeLabel.Trim(),
            VariantSku = $"{product.Sku}-V{model.InitialVolumeMl}",
            OriginalPrice = product.BasePrice,
            SalePrice = product.BasePrice,
            StockQuantity = model.InitialStockQuantity >= 0 ? model.InitialStockQuantity : 20,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        });

        // 3 Tầng hương
        async Task AddNotes(string? raw, NoteLevel level)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            int order = 1;
            foreach (var noteName in parts)
            {
                var cleanName = noteName.Trim();
                if (string.IsNullOrWhiteSpace(cleanName)) continue;
                var noteSlug = cleanName.ToLowerInvariant().Replace(" ", "-");
                var noteEntity = await _db.FragranceNotes.FirstOrDefaultAsync(n => n.Name == cleanName || n.Slug == noteSlug);
                if (noteEntity == null)
                {
                    noteEntity = new FragranceNote
                    {
                        Name = cleanName,
                        Slug = noteSlug,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.FragranceNotes.Add(noteEntity);
                    await _db.SaveChangesAsync();
                }
                product.FragranceNotes.Add(new ProductFragranceNote
                {
                    FragranceNoteId = noteEntity.Id,
                    NoteLevel = level,
                    DisplayOrder = order++
                });
            }
        }

        await AddNotes(model.TopNotes, NoteLevel.Top);
        await AddNotes(model.HeartNotes, NoteLevel.Heart);
        await AddNotes(model.BaseNotes, NoteLevel.Base);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm thành công sản phẩm nước hoa '{product.Name}'!";
        return RedirectToAction(nameof(Products));
    }

    // ─── SYSTEM SETTINGS ──────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Settings()
    {
        var settings = await _settingService.GetSettingsAsync();
        return View(settings);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(SystemSettingsDto model)
    {
        if (!ModelState.IsValid) return View(model);
        await _settingService.SaveSettingsAsync(model);
        TempData["Success"] = "Đã lưu cài đặt hệ thống thành công!";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSmtp(string recipientEmail)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            TempData["Error"] = "Vui lòng nhập địa chỉ email người nhận thử nghiệm.";
            return RedirectToAction(nameof(Settings));
        }

        var result = await _settingService.TestSmtpConnectionAsync(recipientEmail.Trim());
        if (result.Success)
            TempData["Success"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Settings));
    }

    // ─── ERD ──────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Erd()
    {
        var tables = await _dbConsole.GetAllTablesAsync();
        return View(tables);
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
