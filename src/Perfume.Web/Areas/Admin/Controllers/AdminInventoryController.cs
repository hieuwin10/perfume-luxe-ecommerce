using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/AdminDashboard/[action]")]
[Authorize(Roles = "Admin,StoreManager")]
public class AdminInventoryController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IFlashSaleService _flashSaleService;

    public AdminInventoryController(
        PerfumeDbContext _db,
        UserManager<AppUser> _userManager,
        IFlashSaleService _flashSaleService)
    {
        this._db = _db;
        this._userManager = _userManager;
        this._flashSaleService = _flashSaleService;
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
            return View("~/Areas/Admin/Views/AdminDashboard/EditProduct.cshtml", model);
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
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) { p.IsDeleted = true; await _db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xóa sản phẩm (soft delete).";
        return RedirectToAction(nameof(Products));
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

        return View("~/Areas/Admin/Views/AdminDashboard/Inventory.cshtml", model);
    }

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
        return View("~/Areas/Admin/Views/AdminDashboard/Products.cshtml", products);
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


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FragranceFamilies = await _db.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
            return View("~/Areas/Admin/Views/AdminDashboard/CreateProduct.cshtml", model);
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




    [HttpPost]
    [Authorize(Roles = "Admin")]
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



    [HttpPost]
    [Authorize(Roles = "Admin")]
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


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Banners()
    {
        var banners = await _db.HeroBanners
            .AsNoTracking()
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View("~/Areas/Admin/Views/AdminDashboard/Banners.cshtml", banners);
    }



    [HttpPost]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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


    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FlashSales()
    {
        var flashSales = await _flashSaleService.GetAllFlashSalesAsync();
        var products = await _db.Products
            .Include(p => p.Brand)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Products = products;
        return View("~/Areas/Admin/Views/AdminDashboard/FlashSales.cshtml", flashSales);
    }



    [HttpPost]
    [Authorize(Roles = "Admin")]
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
}
