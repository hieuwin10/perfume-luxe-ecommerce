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
        if (newRole is not ("Admin" or "StoreManager" or "WarehouseStaff" or "Concierge" or "Customer"))
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
        {
            TempData["Error"] = "Không thể tự khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Users));
        }

        var isLocked = await _userManager.IsLockedOutAsync(user);
        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Đã mở khóa tài khoản của {user.FullName}.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(50));
            TempData["Success"] = $"Đã tạm khóa tài khoản của {user.FullName}.";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CustomerProfile(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == user.Id || o.CustomerEmail == user.Email)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var loyalty = await _db.UserLoyalties.FirstOrDefaultAsync(l => l.UserId == user.Id);
        var isLocked = await _userManager.IsLockedOutAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        ViewBag.Orders = orders;
        ViewBag.Loyalty = loyalty;
        ViewBag.IsLocked = isLocked;
        ViewBag.Roles = roles;
        ViewBag.TotalSpent = orders.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount);
        ViewBag.DeliveredOrdersCount = orders.Count(o => o.OrderStatus == OrderStatus.Delivered);

        return View(user);
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

    // ─── ORDERS & ADVANCED FILTERING ─────────────────────────────────────────
    public async Task<IActionResult> Orders(int page = 1, string? search = null, OrderStatus? status = null, PaymentStatus? paymentStatus = null, DateTime? from = null, DateTime? to = null)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);

        var query = _db.Orders
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(o => o.OrderCode.Contains(s) || o.CustomerName.Contains(s) || o.CustomerPhone.Contains(s));
        }

        if (status.HasValue)
            query = query.Where(o => o.OrderStatus == status.Value);

        if (paymentStatus.HasValue)
            query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Total = total;
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.PaymentStatus = paymentStatus;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderShippingInfo(int orderId, string customerName, string customerPhone, string shippingAddress, string? city, string? orderNotes)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        if (order.OrderStatus is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            TempData["Error"] = "Chỉ có thể thay đổi địa chỉ khi đơn ở trạng thái Chờ duyệt hoặc Đã xác nhận.";
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tên, số điện thoại và địa chỉ nhận hàng.";
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        order.CustomerName = customerName.Trim();
        order.CustomerPhone = customerPhone.Trim();
        order.ShippingAddress = shippingAddress.Trim();
        order.City = city?.Trim();
        order.OrderNotes = orderNotes?.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật thông tin giao hàng cho đơn #{order.OrderCode}!";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
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

    // ─── EDIT PRODUCT ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.FragranceNotes).ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var topNotes = string.Join(", ", product.FragranceNotes.Where(fn => fn.NoteLevel == NoteLevel.Top).OrderBy(fn => fn.DisplayOrder).Select(fn => fn.FragranceNote?.Name).Where(n => !string.IsNullOrEmpty(n)));
        var heartNotes = string.Join(", ", product.FragranceNotes.Where(fn => fn.NoteLevel == NoteLevel.Heart).OrderBy(fn => fn.DisplayOrder).Select(fn => fn.FragranceNote?.Name).Where(n => !string.IsNullOrEmpty(n)));
        var baseNotes = string.Join(", ", product.FragranceNotes.Where(fn => fn.NoteLevel == NoteLevel.Base).OrderBy(fn => fn.DisplayOrder).Select(fn => fn.FragranceNote?.Name).Where(n => !string.IsNullOrEmpty(n)));

        var dto = new EditProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            FragranceFamilyId = product.FragranceFamilyId,
            Gender = product.Gender,
            Concentration = product.Concentration,
            BasePrice = product.BasePrice,
            MainImageUrl = product.MainImageUrl,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Longevity = product.Longevity,
            Sillage = product.Sillage,
            ReleaseYear = product.ReleaseYear,
            SeasonRecommendation = product.SeasonRecommendation,
            IsInStock = product.IsInStock,
            IsFeatured = product.IsFeatured,
            IsBestSeller = product.IsBestSeller,
            IsNewArrival = product.IsNewArrival,
            TopNotes = topNotes,
            HeartNotes = heartNotes,
            BaseNotes = baseNotes,
            Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                AltText = i.AltText,
                IsMain = i.IsMain,
                DisplayOrder = i.DisplayOrder
            }).ToList(),
            Variants = product.Variants.OrderBy(v => v.VolumeMl).Select(v => new ProductVariantDto
            {
                Id = v.Id,
                VolumeMl = v.VolumeMl,
                VolumeLabel = v.VolumeLabel,
                VariantSku = v.VariantSku,
                OriginalPrice = v.OriginalPrice,
                SalePrice = v.SalePrice,
                StockQuantity = v.StockQuantity,
                IsDefault = v.IsDefault
            }).ToList()
        };

        ViewBag.Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.FragranceFamilies = await _db.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(EditProductDto model)
    {
        var product = await _db.Products
            .Include(p => p.FragranceNotes)
            .FirstOrDefaultAsync(p => p.Id == model.Id);
        if (product == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FragranceFamilies = await _db.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
            return View(model);
        }

        product.Name = model.Name.Trim();
        product.Sku = model.Sku.Trim().ToUpperInvariant();
        product.BrandId = model.BrandId;
        product.CategoryId = model.CategoryId;
        product.FragranceFamilyId = model.FragranceFamilyId;
        product.Gender = model.Gender;
        product.Concentration = model.Concentration;
        product.BasePrice = model.BasePrice;
        if (!string.IsNullOrWhiteSpace(model.MainImageUrl)) product.MainImageUrl = model.MainImageUrl.Trim();
        product.ShortDescription = model.ShortDescription?.Trim();
        product.FullDescription = model.FullDescription?.Trim();
        product.Longevity = model.Longevity?.Trim();
        product.Sillage = model.Sillage?.Trim();
        product.ReleaseYear = model.ReleaseYear;
        product.SeasonRecommendation = model.SeasonRecommendation?.Trim();
        product.IsInStock = model.IsInStock;
        product.IsFeatured = model.IsFeatured;
        product.IsBestSeller = model.IsBestSeller;
        product.IsNewArrival = model.IsNewArrival;
        product.UpdatedAt = DateTime.UtcNow;

        // Cập nhật lại 3 tầng hương
        _db.ProductFragranceNotes.RemoveRange(product.FragranceNotes);
        product.FragranceNotes.Clear();

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

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật thông tin sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(EditProduct), new { id = product.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProductGalleryImage(int productId, string imageUrl, string? altText)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            TempData["Error"] = "Vui lòng nhập đường dẫn ảnh CDN hợp lệ.";
            return RedirectToAction(nameof(EditProduct), new { id = productId });
        }

        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound();

        if (product.Images.Count >= 7)
        {
            TempData["Error"] = "Sản phẩm đã đạt giới hạn tối đa ảnh trong Gallery (7 ảnh).";
            return RedirectToAction(nameof(EditProduct), new { id = productId });
        }

        product.Images.Add(new ProductImage
        {
            ImageUrl = imageUrl.Trim(),
            AltText = string.IsNullOrWhiteSpace(altText) ? product.Name : altText.Trim(),
            IsMain = false,
            DisplayOrder = product.Images.Count + 1,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm ảnh vào bộ sưu tập sản phẩm!";
        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainProductGalleryImage(int productId, int imageId)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound();

        var selectedImage = product.Images.FirstOrDefault(i => i.Id == imageId);
        if (selectedImage != null)
        {
            foreach (var img in product.Images) img.IsMain = false;
            selectedImage.IsMain = true;
            product.MainImageUrl = selectedImage.ImageUrl;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thay đổi ảnh đại diện chính của chai nước hoa!";
        }

        return RedirectToAction(nameof(EditProduct), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductGalleryImage(int productId, int imageId)
    {
        var img = await _db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);
        if (img != null)
        {
            _db.Images.Remove(img);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa ảnh khỏi gallery!";
        }
        return RedirectToAction(nameof(EditProduct), new { id = productId });
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

    #region QUẢN TRỊ TỒN KHO & NHẬP HÀNG (INVENTORY & STOCK ADJUSTMENTS)
    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Inventory(string? filter = "all", string? search = null)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || (p.Brand != null && p.Brand.Name.ToLower().Contains(s)));
        }

        var allProducts = await query.ToListAsync();

        var items = allProducts.Select(p => new InventoryProductItem
        {
            ProductId = p.Id,
            ProductName = p.Name,
            BrandName = p.Brand?.Name,
            ImageUrl = p.MainImageUrl,
            TotalStock = p.Variants.Any() ? p.Variants.Sum(v => v.StockQuantity) : 0,
            IsInStock = p.IsInStock,
            Variants = p.Variants.Select(v => new InventoryVariantItem
            {
                VariantId = v.Id,
                VolumeLabel = v.VolumeLabel,
                VariantSku = v.VariantSku,
                SalePrice = v.SalePrice,
                StockQuantity = v.StockQuantity
            }).ToList()
        }).ToList();

        var totalProducts = items.Count;
        var totalStockUnits = items.Sum(i => i.TotalStock);
        var lowStockCount = items.Count(i => i.Variants.Any(v => v.StockQuantity > 0 && v.StockQuantity <= 5) || (i.Variants.Count == 0 && i.TotalStock > 0 && i.TotalStock <= 5));
        var outOfStockCount = items.Count(i => !i.IsInStock || i.TotalStock == 0 || i.Variants.Any(v => v.StockQuantity == 0));

        if (filter == "low")
        {
            items = items.Where(i => i.Variants.Any(v => v.StockQuantity > 0 && v.StockQuantity <= 5) || (i.Variants.Count == 0 && i.TotalStock > 0 && i.TotalStock <= 5)).ToList();
        }
        else if (filter == "out")
        {
            items = items.Where(i => !i.IsInStock || i.TotalStock == 0 || i.Variants.Any(v => v.StockQuantity == 0)).ToList();
        }

        var recentAdjustments = await _db.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Variant)
                .ThenInclude(v => v!.Product)
            .OrderByDescending(a => a.CreatedAt)
            .Take(25)
            .ToListAsync();

        var model = new InventoryViewModel
        {
            TotalProducts = totalProducts,
            TotalStockUnits = totalStockUnits,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            CurrentFilter = filter ?? "all",
            SearchQuery = search,
            Items = items,
            RecentAdjustments = recentAdjustments
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int variantId, int adjustedQuantity, decimal? unitCost, string reason, string? note)
    {
        if (adjustedQuantity == 0)
        {
            TempData["Error"] = "Số lượng thay đổi phải khác 0.";
            return RedirectToAction(nameof(Inventory));
        }

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p!.Variants)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant == null)
        {
            TempData["Error"] = "Không tìm thấy phiên bản sản phẩm.";
            return RedirectToAction(nameof(Inventory));
        }

        var oldQty = variant.StockQuantity;
        var newQty = Math.Max(0, oldQty + adjustedQuantity);
        variant.StockQuantity = newQty;

        var user = await _userManager.GetUserAsync(User);

        var adjustment = new StockAdjustment
        {
            VariantId = variantId,
            OldQuantity = oldQty,
            AdjustedQuantity = adjustedQuantity,
            NewQuantity = newQty,
            UnitCost = unitCost,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Nhập hàng mới" : reason.Trim(),
            Note = note?.Trim(),
            PerformedByUserId = user?.Id,
            PerformedByName = user?.FullName ?? user?.UserName ?? "Quản trị viên"
        };
        _db.StockAdjustments.Add(adjustment);

        // Đồng bộ trạng thái tồn kho sản phẩm cha
        if (variant.Product != null)
        {
            var totalVariantStock = variant.Product.Variants.Sum(v => v.Id == variant.Id ? newQty : v.StockQuantity);
            variant.Product.IsInStock = totalVariantStock > 0;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật tồn kho cho {variant.Product?.Name} ({variant.VolumeLabel}). Tồn mới: {newQty}.";
        return RedirectToAction(nameof(Inventory));
    }
    #endregion

    #region QUẢN TRỊ HERO BANNERS (HERO SLIDER)
    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Banners()
    {
        var banners = await _db.HeroBanners
            .AsNoTracking()
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(banners);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBanner(int? id, string title, string? subtitle, string imageUrl, string? targetUrl, string? buttonText, int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(imageUrl))
        {
            TempData["Error"] = "Vui lòng nhập tiêu đề và link ảnh CDN.";
            return RedirectToAction(nameof(Banners));
        }

        if (id.HasValue && id.Value > 0)
        {
            var banner = await _db.HeroBanners.FindAsync(id.Value);
            if (banner == null) return NotFound();

            banner.Title = title.Trim();
            banner.Subtitle = subtitle?.Trim();
            banner.ImageUrl = imageUrl.Trim();
            banner.TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? "/bo-suu-tap" : targetUrl.Trim();
            banner.ButtonText = string.IsNullOrWhiteSpace(buttonText) ? "Khám Phá Ngay" : buttonText.Trim();
            banner.DisplayOrder = displayOrder;
            banner.IsActive = isActive;
            banner.UpdatedAt = DateTime.UtcNow;
            TempData["Success"] = "Đã cập nhật Hero Banner!";
        }
        else
        {
            var banner = new HeroBanner
            {
                Title = title.Trim(),
                Subtitle = subtitle?.Trim(),
                ImageUrl = imageUrl.Trim(),
                TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? "/bo-suu-tap" : targetUrl.Trim(),
                ButtonText = string.IsNullOrWhiteSpace(buttonText) ? "Khám Phá Ngay" : buttonText.Trim(),
                DisplayOrder = displayOrder,
                IsActive = isActive
            };
            _db.HeroBanners.Add(banner);
            TempData["Success"] = "Đã tạo mới Hero Banner thành công!";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Banners));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBannerActive(int id)
    {
        var banner = await _db.HeroBanners.FindAsync(id);
        if (banner == null) return NotFound();

        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã {(banner.IsActive ? "kích hoạt" : "ẩn")} banner: {banner.Title}.";
        return RedirectToAction(nameof(Banners));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        var banner = await _db.HeroBanners.FindAsync(id);
        if (banner == null) return NotFound();

        _db.HeroBanners.Remove(banner);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa Hero Banner.";
        return RedirectToAction(nameof(Banners));
    }
    #endregion

    #region QUẢN TRỊ LỊCH HẸN PRIVATE TASTING VIP
    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Tastings(string? status)
    {
        var query = _db.PrivateTastingBookings
            .AsNoTracking()
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(b => b.Status == status);
        }

        var list = await query.ToListAsync();
        ViewBag.CurrentStatus = status ?? "all";
        return View(list);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTastingStatus(int id, string status)
    {
        var booking = await _db.PrivateTastingBookings.FindAsync(id);
        if (booking == null) return NotFound();

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái lịch hẹn VIP-{booking.Id:D5} thành: {status}";
        return RedirectToAction(nameof(Tastings));
    }
    #endregion

    #region Báo Cáo Tài Chính P&L (Profit & Loss) & Định Giá Kho
    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> FinancialReport(DateTime? fromDate, DateTime? toDate, string? preset = "30days")
    {
        var now = DateTime.UtcNow;
        DateTime start;
        DateTime end = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;

        switch (preset?.ToLowerInvariant())
        {
            case "today":
                start = now.Date;
                break;
            case "7days":
                start = now.Date.AddDays(-7);
                break;
            case "thismonth":
                start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
            case "thisyear":
                start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
            case "all":
                start = DateTime.MinValue;
                break;
            default: // 30days
                start = fromDate ?? now.Date.AddDays(-30);
                if (fromDate.HasValue) start = fromDate.Value.Date;
                break;
        }

        // 1. Lấy danh sách giá vốn gần nhất từ StockAdjustments
        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        // 2. Query Orders trong kỳ (chỉ tính đơn hoàn tất hoặc đã thanh toán)
        var completedOrders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .Where(o => o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new FinancialReportViewModel
        {
            FromDate = start == DateTime.MinValue ? null : start,
            ToDate = end,
            Preset = preset ?? "30days",
            TotalOrdersCount = completedOrders.Count,
            GrossSales = completedOrders.Sum(o => o.SubTotal),
            VoucherDiscounts = completedOrders.Sum(o => o.DiscountAmount),
            ShippingFeesCollected = completedOrders.Sum(o => o.ShippingFee),
            GiftWrappingFeesCollected = completedOrders.Sum(o => o.GiftWrappingFee),
            NetRevenue = completedOrders.Sum(o => o.TotalAmount)
        };

        // Tính COGS và Lợi nhuận của từng đơn
        decimal totalCogs = 0;
        foreach (var order in completedOrders)
        {
            decimal orderCogs = 0;
            foreach (var item in order.Items)
            {
                decimal itemUnitCost = 0;
                if (item.VariantId.HasValue && recentCosts.TryGetValue(item.VariantId.Value, out var cost))
                {
                    itemUnitCost = cost;
                }
                else
                {
                    // Tỷ lệ giá vốn giả định 42% cho nước hoa Haute Parfumerie
                    itemUnitCost = Math.Round(item.UnitPrice * 0.42m, 0);
                }
                orderCogs += itemUnitCost * item.Quantity;
            }

            totalCogs += orderCogs;
            var orderProfit = order.TotalAmount - orderCogs;
            var marginPct = order.TotalAmount > 0 ? Math.Round((double)orderProfit / (double)order.TotalAmount * 100, 1) : 0;

            vm.OrderEntries.Add(new FinancialOrderEntry
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerName,
                PaymentMethod = order.PaymentMethod.ToString(),
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                GiftWrappingFee = order.GiftWrappingFee,
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                EstimatedCogs = orderCogs,
                GrossProfit = orderProfit,
                MarginPercent = marginPct
            });
        }

        vm.TotalCogs = totalCogs;
        vm.GrossProfit = vm.NetRevenue - vm.TotalCogs;
        vm.GrossMarginPercent = vm.NetRevenue > 0 ? Math.Round((double)vm.GrossProfit / (double)vm.NetRevenue * 100, 1) : 0;
        vm.AverageOrderValue = vm.TotalOrdersCount > 0 ? Math.Round(vm.NetRevenue / vm.TotalOrdersCount, 0) : 0;

        // 3. Định giá kho hiện tại
        var allVariants = await _db.ProductVariants
            .Include(v => v.Product!)
                .ThenInclude(p => p.Brand)
            .Include(v => v.Product!)
                .ThenInclude(p => p.FragranceFamily)
            .Where(v => !v.IsDeleted)
            .ToListAsync();

        vm.TotalStockQuantity = allVariants.Sum(v => v.StockQuantity);
        decimal inventoryCost = 0;
        decimal inventoryRetail = 0;

        var brandValuations = new Dictionary<string, (int Qty, decimal CostVal, decimal RetailVal)>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in allVariants)
        {
            var retailPrice = v.SalePrice > 0 ? v.SalePrice : (v.OriginalPrice > 0 ? v.OriginalPrice : (v.Product?.BasePrice ?? 0));
            decimal costPrice = 0;
            if (recentCosts.TryGetValue(v.Id, out var recordedCost))
            {
                costPrice = recordedCost;
            }
            else
            {
                costPrice = Math.Round(retailPrice * 0.42m, 0);
            }

            var itemCostVal = v.StockQuantity * costPrice;
            var itemRetailVal = v.StockQuantity * retailPrice;

            inventoryCost += itemCostVal;
            inventoryRetail += itemRetailVal;

            var brandName = v.Product?.Brand?.Name ?? "Khác";
            if (!brandValuations.ContainsKey(brandName))
            {
                brandValuations[brandName] = (0, 0, 0);
            }
            var cur = brandValuations[brandName];
            brandValuations[brandName] = (cur.Qty + v.StockQuantity, cur.CostVal + itemCostVal, cur.RetailVal + itemRetailVal);
        }

        vm.TotalInventoryCostValue = inventoryCost;
        vm.TotalInventoryRetailValue = inventoryRetail;
        vm.UnrealizedGrossProfit = Math.Max(0, inventoryRetail - inventoryCost);

        foreach (var kvp in brandValuations.OrderByDescending(b => b.Value.CostVal).Take(10))
        {
            vm.BrandValuations.Add(new BrandInventoryValuation
            {
                BrandName = kvp.Key,
                TotalUnits = kvp.Value.Qty,
                TotalCostValue = kvp.Value.CostVal,
                TotalRetailValue = kvp.Value.RetailVal,
                PotentialProfit = Math.Max(0, kvp.Value.RetailVal - kvp.Value.CostVal)
            });
        }

        // 4. Biểu đồ Chart.js: Doanh thu & Lợi nhuận theo các ngày gần nhất (hoặc 7 ngày qua)
        var chartDays = Enumerable.Range(0, 14)
            .Select(i => now.Date.AddDays(-13 + i))
            .ToList();

        foreach (var day in chartDays)
        {
            var dayOrders = completedOrders.Where(o => o.CreatedAt.Date == day).ToList();
            var dayRevenue = dayOrders.Sum(o => o.TotalAmount);
            var dayCogs = dayOrders.Sum(o => vm.OrderEntries.FirstOrDefault(e => e.OrderId == o.Id)?.EstimatedCogs ?? 0);
            var dayProfit = dayRevenue - dayCogs;

            vm.DailyChartLabels.Add(day.ToString("dd/MM"));
            vm.DailyRevenueData.Add(dayRevenue);
            vm.DailyProfitData.Add(dayProfit);
        }

        // 5. Biểu đồ cơ cấu nhóm hương
        var familyRevenues = completedOrders.SelectMany(o => o.Items)
            .GroupBy(i => i.Product?.FragranceFamily?.Name ?? "Woody / Khác")
            .Select(g => new { FamilyName = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        foreach (var fam in familyRevenues)
        {
            vm.FamilyChartLabels.Add(fam.FamilyName);
            vm.FamilyChartData.Add(fam.Revenue);
        }

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> ExportFinancialOrdersCsv(DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.UtcNow;
        var start = fromDate ?? now.AddDays(-30);
        var end = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : now;

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .Where(o => o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM
        sb.Append('\uFEFF');
        sb.AppendLine("Mã Đơn,Ngày Đặt,Khách Hàng,Phương Thức,Tiền Hàng,Giảm Giá,Gói Quà,Phí Ship,Doanh Thu Thuần,Giá Vốn Ước Tính,Lợi Nhuận Gộp,Biên Lãi %");

        foreach (var o in orders)
        {
            decimal cogs = 0;
            foreach (var item in o.Items)
            {
                decimal uCost = item.VariantId.HasValue && recentCosts.TryGetValue(item.VariantId.Value, out var c)
                    ? c : Math.Round(item.UnitPrice * 0.42m, 0);
                cogs += uCost * item.Quantity;
            }
            var profit = o.TotalAmount - cogs;
            var margin = o.TotalAmount > 0 ? Math.Round((double)profit / (double)o.TotalAmount * 100, 1) : 0;

            sb.AppendLine($"\"{o.OrderCode}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm}\",\"{o.CustomerName}\",\"{o.PaymentMethod}\",{o.SubTotal},{o.DiscountAmount},{o.GiftWrappingFee},{o.ShippingFee},{o.TotalAmount},{cogs},{profit},{margin}%");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"PerfumeLuxe_Financial_Orders_{now:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> ExportInventoryValuationCsv()
    {
        var now = DateTime.UtcNow;
        var variants = await _db.ProductVariants
            .Include(v => v.Product!)
                .ThenInclude(p => p.Brand)
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.Product != null ? v.Product.Name : string.Empty)
            .ToListAsync();

        var recentCosts = await _db.StockAdjustments
            .Where(a => a.UnitCost.HasValue && a.UnitCost.Value > 0)
            .OrderByDescending(a => a.CreatedAt)
            .GroupBy(a => a.VariantId)
            .Select(g => new { VariantId = g.Key, UnitCost = g.First().UnitCost!.Value })
            .ToDictionaryAsync(x => x.VariantId, x => x.UnitCost);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("ID Variant,Mã SKU,Tên Nước Hoa,Thương Hiệu,Dung Tích,Tồn Kho,Đơn Giá Vốn,Đơn Giá Niêm Yết,Tổng Giá Trị Vốn,Tổng Giá Trị Bán Lẻ,Lợi Nhuận Tiềm Năng");

        foreach (var v in variants)
        {
            var retailPrice = v.SalePrice > 0 ? v.SalePrice : (v.OriginalPrice > 0 ? v.OriginalPrice : (v.Product?.BasePrice ?? 0));
            decimal costPrice = recentCosts.TryGetValue(v.Id, out var c) ? c : Math.Round(retailPrice * 0.42m, 0);
            var totalCost = v.StockQuantity * costPrice;
            var totalRetail = v.StockQuantity * retailPrice;
            var potentialProfit = Math.Max(0, totalRetail - totalCost);

            sb.AppendLine($"{v.Id},\"{v.VariantSku}\",\"{v.Product?.Name}\",\"{v.Product?.Brand?.Name}\",\"{v.VolumeLabel}\",{v.StockQuantity},{costPrice},{retailPrice},{totalCost},{totalRetail},{potentialProfit}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"PerfumeLuxe_Inventory_Valuation_{now:yyyyMMdd_HHmm}.csv");
    }
    #endregion

    #region Trung Tâm Nhật Ký Kiểm Toán (Audit Logs) & Sao Lưu DB
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditLogs(string? actionFilter, bool? succeededFilter, string? actorFilter, int page = 1)
    {
        var query = _db.DatabaseAuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (succeededFilter.HasValue)
        {
            query = query.Where(a => a.Succeeded == succeededFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(actorFilter))
        {
            query = query.Where(a => a.ActorUserId != null && a.ActorUserId.Contains(actorFilter));
        }

        var totalCount = await query.CountAsync();
        var pageSize = 20;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new AuditLogsViewModel
        {
            Logs = logs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            ActionFilter = actionFilter,
            SucceededFilter = succeededFilter,
            ActorFilter = actorFilter
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult DatabaseBackup()
    {
        var backupDirectory = _configuration["Database:BackupPath"];
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        }

        var files = new List<BackupFileInfo>();
        if (Directory.Exists(backupDirectory))
        {
            var dirInfo = new DirectoryInfo(backupDirectory);
            foreach (var fi in dirInfo.GetFiles("*.bak").OrderByDescending(f => f.CreationTimeUtc))
            {
                files.Add(new BackupFileInfo
                {
                    FileName = fi.Name,
                    FullPath = fi.FullName,
                    SizeBytes = fi.Length,
                    CreatedAt = fi.CreationTimeUtc
                });
            }
        }

        var vm = new DatabaseBackupViewModel
        {
            BackupDirectory = backupDirectory,
            DirectoryExists = Directory.Exists(backupDirectory),
            BackupFiles = files
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDatabaseBackup()
    {
        var backupDirectory = _configuration["Database:BackupPath"];
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        }

        try
        {
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            var fileName = $"PerfumeDb-Manual-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
            var backupPath = Path.Combine(backupDirectory, fileName);

            var sql = $"BACKUP DATABASE [PerfumeDb] TO DISK = N'{backupPath.Replace("'", "''")}' WITH COPY_ONLY, INIT";
            await _db.Database.ExecuteSqlRawAsync(sql);

            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User),
                Action = "DATABASE_BACKUP",
                Target = fileName,
                Succeeded = true,
                ElapsedMilliseconds = 0
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo bản sao lưu thành công: {fileName}";
        }
        catch (Exception ex)
        {
            _db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = _userManager.GetUserId(User),
                Action = "DATABASE_BACKUP_FAILED",
                Target = ex.Message[..Math.Min(120, ex.Message.Length)],
                Succeeded = false,
                ElapsedMilliseconds = 0
            });
            await _db.SaveChangesAsync();

            TempData["Error"] = $"Không thể sao lưu cơ sở dữ liệu: {ex.Message}";
        }

        return RedirectToAction(nameof(DatabaseBackup));
    }
    #endregion
}

public class AuditLogsViewModel
{
    public List<DatabaseAuditLog> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? ActionFilter { get; set; }
    public bool? SucceededFilter { get; set; }
    public string? ActorFilter { get; set; }
}

public class DatabaseBackupViewModel
{
    public string BackupDirectory { get; set; } = string.Empty;
    public bool DirectoryExists { get; set; }
    public List<BackupFileInfo> BackupFiles { get; set; } = new();
}

public class BackupFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FinancialReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Preset { get; set; } = "30days";

    // P&L Metrics
    public int TotalOrdersCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal VoucherDiscounts { get; set; }
    public decimal ShippingFeesCollected { get; set; }
    public decimal GiftWrappingFeesCollected { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public double GrossMarginPercent { get; set; }
    public decimal AverageOrderValue { get; set; }

    // Inventory Valuation
    public int TotalStockQuantity { get; set; }
    public decimal TotalInventoryCostValue { get; set; }
    public decimal TotalInventoryRetailValue { get; set; }
    public decimal UnrealizedGrossProfit { get; set; }

    // Breakdown Lists
    public List<FinancialOrderEntry> OrderEntries { get; set; } = new();
    public List<BrandInventoryValuation> BrandValuations { get; set; } = new();

    // Chart Data
    public List<string> DailyChartLabels { get; set; } = new();
    public List<decimal> DailyRevenueData { get; set; } = new();
    public List<decimal> DailyProfitData { get; set; } = new();

    public List<string> FamilyChartLabels { get; set; } = new();
    public List<decimal> FamilyChartData { get; set; } = new();
}

public class FinancialOrderEntry
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GiftWrappingFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal EstimatedCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public double MarginPercent { get; set; }
}

public class BrandInventoryValuation
{
    public string BrandName { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialProfit { get; set; }
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

public class InventoryViewModel
{
    public int TotalProducts { get; set; }
    public int TotalStockUnits { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public string CurrentFilter { get; set; } = "all";
    public string? SearchQuery { get; set; }
    public List<InventoryProductItem> Items { get; set; } = new();
    public List<StockAdjustment> RecentAdjustments { get; set; } = new();
}

public class InventoryProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalStock { get; set; }
    public bool IsInStock { get; set; }
    public List<InventoryVariantItem> Variants { get; set; } = new();
}

public class InventoryVariantItem
{
    public int VariantId { get; set; }
    public string VolumeLabel { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
}
