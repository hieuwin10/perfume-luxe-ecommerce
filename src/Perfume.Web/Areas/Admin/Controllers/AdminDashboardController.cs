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
    private readonly IBlogService _blogService;

    public AdminDashboardController(PerfumeDbContext db, IDatabaseConsoleService dbConsole, UserManager<AppUser> userManager, ISystemSettingService settingService, IBlogService blogService)
    {
        _db = db;
        _dbConsole = dbConsole;
        _userManager = userManager;
        _settingService = settingService;
        _blogService = blogService;
    }

    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole("Admin") && User.IsInRole("StoreManager"))
        {
            return RedirectToAction("Index", "StoreManager");
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allOrders = await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var totalOrders = allOrders.Count;
        var deliveredOrders = allOrders.Where(o => o.OrderStatus == OrderStatus.Delivered).ToList();
        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount);
        var revenueThisMonth = deliveredOrders.Where(o => o.CreatedAt >= startOfMonth).Sum(o => o.TotalAmount);
        var avgOrderValue = totalOrders > 0 ? allOrders.Average(o => o.TotalAmount) : 0;

        var pendingCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Pending);
        var confirmedCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Confirmed);
        var shippingCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Shipping);
        var deliveredCount = deliveredOrders.Count;
        var cancelledCount = allOrders.Count(o => o.OrderStatus == OrderStatus.Cancelled);
        var fulfillmentRate = totalOrders > 0 ? Math.Round((double)deliveredCount / totalOrders * 100, 1) : 0;

        // Biểu đồ doanh thu 7 ngày gần nhất
        var chartLabels = new List<string>();
        var chartData = new List<decimal>();
        for (int i = 6; i >= 0; i--)
        {
            var date = now.Date.AddDays(-i);
            var nextDate = date.AddDays(1);
            var dayRevenue = deliveredOrders
                .Where(o => o.CreatedAt >= date && o.CreatedAt < nextDate)
                .Sum(o => o.TotalAmount);
            chartLabels.Add(date.ToString("dd/MM"));
            chartData.Add(dayRevenue);
        }

        // Biểu đồ trạng thái đơn hàng
        var orderStatusChart = new Dictionary<string, int>
        {
            { "Đã Giao (Thành công)", deliveredCount },
            { "Đang Vận Chuyển", shippingCount },
            { "Đã Xác Nhận", confirmedCount },
            { "Chờ Xử Lý", pendingCount },
            { "Đã Hủy", cancelledCount }
        };

        // Phân bố phương thức thanh toán
        var paymentMethodChart = new Dictionary<string, int>
        {
            { "COD (Tiền mặt)", allOrders.Count(o => o.PaymentMethod == PaymentMethod.COD) },
            { "VNPAY QR", allOrders.Count(o => o.PaymentMethod == PaymentMethod.VNPAY) },
            { "Chuyển Khoản", allOrders.Count(o => o.PaymentMethod == PaymentMethod.BankTransfer) },
            { "Ví MoMo", allOrders.Count(o => o.PaymentMethod == PaymentMethod.MoMo) }
        };

        // Top sản phẩm bán chạy nhất
        var topProducts = await _db.OrderItems
            .GroupBy(i => new { i.ProductId, i.ProductName, i.ProductImageUrl })
            .Select(g => new DashboardTopProductStat
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ImageUrl = g.Key.ProductImageUrl,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync();

        // Thống kê theo nhóm hương
        var scentStats = await _db.FragranceFamilies
            .Where(f => !f.IsDeleted)
            .Select(f => new ScentFamilyStat
            {
                Name = f.Name,
                ColorHex = f.ColorHex ?? "#D4AF37",
                ProductCount = f.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalProducts = await _db.Products.CountAsync(p => !p.IsDeleted),
            TotalOrders = totalOrders,
            TotalUsers = await _db.Users.CountAsync(),
            TotalBrands = await _db.Brands.CountAsync(b => !b.IsDeleted),
            TotalCategories = await _db.Categories.CountAsync(c => !c.IsDeleted),
            TotalBlogPosts = await _db.BlogPosts.CountAsync(b => !b.IsDeleted),
            TotalReviews = await _db.Reviews.CountAsync(r => !r.IsDeleted),
            TotalVouchers = await _db.Vouchers.CountAsync(v => !v.IsDeleted),
            
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            AverageOrderValue = avgOrderValue,
            PendingOrdersCount = pendingCount,
            ConfirmedOrdersCount = confirmedCount,
            ShippingOrdersCount = shippingCount,
            DeliveredOrdersCount = deliveredCount,
            CancelledOrdersCount = cancelledCount,
            FulfillmentRate = fulfillmentRate,

            RevenueChartLabels = chartLabels,
            RevenueChartData = chartData,
            OrderStatusChart = orderStatusChart,
            PaymentMethodChart = paymentMethodChart,
            TopProducts = topProducts,
            ScentFamilyStats = scentStats,

            RecentUsers = await _db.Users.OrderByDescending(u => u.CreatedAt).Take(6).ToListAsync(),
            RecentOrders = allOrders.Take(8).ToList(),
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

    // ─── BLOG MANAGEMENT ──────────────────────────────────────────────────────
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Blogs()
    {
        var posts = await _blogService.GetAllPostsForAdminAsync();
        return View(posts);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CreateBlog()
    {
        ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
        return View(new BlogPost());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBlog(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
        {
            ModelState.AddModelError("", "Tiêu đề và nội dung bài viết không được để trống.");
            ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
            return View(post);
        }

        await _blogService.CreatePostAsync(post);
        TempData["Success"] = $"Đã xuất bản bài viết \"{post.Title}\" thành công!";
        return RedirectToAction(nameof(Blogs));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> EditBlog(int id)
    {
        var post = await _blogService.GetPostByIdAsync(id);
        if (post == null) return NotFound();

        ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
        return View(nameof(CreateBlog), post);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBlog(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Content))
        {
            ModelState.AddModelError("", "Tiêu đề và nội dung bài viết không được để trống.");
            ViewBag.Categories = await _blogService.GetBlogCategoriesAsync();
            return View(nameof(CreateBlog), post);
        }

        var success = await _blogService.UpdatePostAsync(post);
        if (!success) return NotFound();

        TempData["Success"] = $"Đã cập nhật bài viết \"{post.Title}\" thành công!";
        return RedirectToAction(nameof(Blogs));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBlog(int id)
    {
        var success = await _blogService.DeletePostAsync(id);
        if (!success) return NotFound();

        TempData["Success"] = "Đã xóa bài viết thành công!";
        return RedirectToAction(nameof(Blogs));
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

    // Thống kê tài chính & vận hành
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ConfirmedOrdersCount { get; set; }
    public int ShippingOrdersCount { get; set; }
    public int DeliveredOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }
    public double FulfillmentRate { get; set; }

    // Biểu đồ
    public List<string> RevenueChartLabels { get; set; } = new();
    public List<decimal> RevenueChartData { get; set; } = new();
    public Dictionary<string, int> OrderStatusChart { get; set; } = new();
    public Dictionary<string, int> PaymentMethodChart { get; set; } = new();

    // Top sản phẩm & Nhóm hương
    public List<DashboardTopProductStat> TopProducts { get; set; } = new();
    public List<ScentFamilyStat> ScentFamilyStats { get; set; } = new();

    public IList<AppUser> RecentUsers { get; set; } = new List<AppUser>();
    public IList<Perfume.Domain.Entities.Order> RecentOrders { get; set; } = new List<Perfume.Domain.Entities.Order>();
    public IList<TableInfo> Tables { get; set; } = new List<TableInfo>();
}

public class DashboardTopProductStat
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ScentFamilyStat
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#D4AF37";
    public int ProductCount { get; set; }
}
