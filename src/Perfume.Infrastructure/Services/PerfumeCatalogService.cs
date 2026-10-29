using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

/// <summary>
/// Service xu ly nghiep vu catalog, tim kiem san pham da chieu va quan ly danh muc mui huong.
/// </summary>
public partial class PerfumeCatalogService : IPerfumeCatalogService
{
    private readonly PerfumeDbContext _context;
    private readonly IMemoryCache? _cache;

    // ==========================================
    // 1. KHOI TAO SERVICE VA DEPENDENCY INJECTION
    // ==========================================
    // Khoi tao service voi PerfumeDbContext va tuy chon IMemoryCache de cache du lieu tinh
    public PerfumeCatalogService(PerfumeDbContext context, IMemoryCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    // ==========================================
    // 2. CAC TRUY VAN TRANG CHU & DANH MUC NOI BAT
    // ==========================================

    // Lay toi da 8 san pham tieu bieu (Featured) hien thi trang chu
    public async Task<List<ProductCardDto>> GetFeaturedProductsAsync()
    {
        return await QueryProductCards(_context.Products.AsNoTracking().Where(p => p.IsFeatured).Take(8));
    }

    // Lay toi da 8 san pham ban chay nhat (Best Sellers)
    public async Task<List<ProductCardDto>> GetBestSellersAsync()
    {
        return await QueryProductCards(_context.Products.AsNoTracking().Where(p => p.IsBestSeller).Take(8));
    }

    // Lay toi da 8 san pham moi ve (New Arrivals)
    public async Task<List<ProductCardDto>> GetNewArrivalsAsync()
    {
        return await QueryProductCards(_context.Products.AsNoTracking().Where(p => p.IsNewArrival).Take(8));
    }

    // ==========================================
    // 3. BO LOC SAN PHAM DA CHIEU & PHAN TRANG
    // ==========================================

    // Loc san pham theo tu khoa, thuong hieu, nhom huong, nong do, gia va sap xep
    public async Task<(List<ProductCardDto> Items, int TotalCount)> GetProductsAsync(ProductFilterParams filter)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        // Loc theo tu khoa tim kiem (ten, thuong hieu, mo ta ngan)
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || p.Brand!.Name.Contains(kw) || p.ShortDescription!.Contains(kw));
        }

        // Loc theo slug cua thuong hieu
        if (!string.IsNullOrWhiteSpace(filter.BrandSlug))
        {
            query = query.Where(p => p.Brand != null && p.Brand.Slug == filter.BrandSlug);
        }

        // Loc theo slug cua danh muc
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == filter.CategorySlug);
        }

        // Loc theo slug cua nhom huong (Fragrance Family)
        if (!string.IsNullOrWhiteSpace(filter.FamilySlug))
        {
            query = query.Where(p => p.FragranceFamily != null && p.FragranceFamily.Slug == filter.FamilySlug);
        }

        // Loc theo not huong cu the
        if (filter.NoteId.HasValue)
        {
            query = query.Where(p => p.FragranceNotes.Any(fn => fn.FragranceNoteId == filter.NoteId.Value));
        }

        // Loc theo gioi tinh muc tieu (Unisex phu hop voi ca Nam va Nu)
        if (filter.Gender.HasValue)
        {
            query = query.Where(p => p.Gender == filter.Gender.Value || p.Gender == GenderTarget.Unisex);
        }

        // Loc theo nong do tinh dau (EDC, EDT, EDP, Extrait)
        if (filter.Concentration.HasValue)
        {
            query = query.Where(p => p.Concentration == filter.Concentration.Value);
        }

        // Loc theo khoang gia (toi thieu va toi da)
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);
        }

        // Dem tong so ban ghi thoa man bo loc truoc khi phan trang
        int totalCount = await query.CountAsync();

        // Sap xep danh sach theo tieu chi yeu cau
        query = filter.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "rating" => query.OrderByDescending(p => p.RatingAverage),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // Ap dung phan trang (Skip / Take)
        int skip = (Math.Max(1, filter.Page) - 1) * filter.PageSize;
        var paginatedQuery = query.Skip(skip).Take(filter.PageSize);

        // Truy van va chuyen doi sang Card DTO
        var items = await QueryProductCards(paginatedQuery);
        return (items, totalCount);
    }

    // ==========================================
    // 4. TRUY VAN CHI TIET SAN PHAM (DETAIL)
    // ==========================================

    // Lay chi tiet san pham day du kem cac tang huong theo Slug
    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.FragranceFamily)
            .Include(p => p.Variants.OrderBy(v => v.VolumeMl))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.FragranceNotes)
                .ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        return product == null ? null : await MapToProductDetailDtoAsync(product);
    }

    // Lay chi tiet san pham day du kem cac tang huong theo ID
    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.FragranceFamily)
            .Include(p => p.Variants.OrderBy(v => v.VolumeMl))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.FragranceNotes)
                .ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : await MapToProductDetailDtoAsync(product);
    }

    // Chuyen doi Entity Product sang ProductDetailDto kem thong tin Flash Sale hien huu
    private async Task<ProductDetailDto> MapToProductDetailDtoAsync(Product product)
    {
        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            BrandName = product.Brand?.Name ?? string.Empty,
            BrandSlug = product.Brand?.Slug ?? string.Empty,
            BrandCountry = product.Brand?.Country,
            CategoryName = product.Category?.Name ?? string.Empty,
            FamilyName = product.FragranceFamily?.Name,
            FamilyColor = product.FragranceFamily?.ColorHex,
            Concentration = product.Concentration,
            Gender = product.Gender,
            Longevity = product.Longevity,
            Sillage = product.Sillage,
            ReleaseYear = product.ReleaseYear,
            SeasonRecommendation = product.SeasonRecommendation,
            BasePrice = product.BasePrice,
            MainImageUrl = product.MainImageUrl,
            RatingAverage = product.RatingAverage,
            RatingCount = product.RatingCount,
            IsInStock = product.IsInStock,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            TopNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
            HeartNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Heart && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
            BaseNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Base && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
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
            }).ToList(),
            GalleryImages = product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList()
        };

        // Kiem tra san pham co dang trong chuong trinh Flash Sale con han va con suat khong
        var now = DateTime.UtcNow;
        var activeFlashItem = await _context.FlashSaleItems
            .AsNoTracking()
            .Include(i => i.FlashSale)
            .Where(i => i.ProductId == product.Id
                && i.FlashSale != null
                && i.FlashSale.IsActive
                && i.FlashSale.StartTime <= now
                && i.FlashSale.EndTime > now
                && i.SoldQuantity < i.StockLimit)
            .OrderBy(i => i.FlashSalePrice)
            .FirstOrDefaultAsync();

        if (activeFlashItem != null)
        {
            dto.IsFlashSale = true;
            dto.FlashSalePrice = activeFlashItem.FlashSalePrice;
            dto.FlashSaleDiscountPercent = dto.BasePrice > 0
                ? (int)Math.Round((1m - (activeFlashItem.FlashSalePrice / dto.BasePrice)) * 100m)
                : 0;
            dto.FlashSaleEndTime = activeFlashItem.FlashSale?.EndTime;
            dto.FlashSaleStockLimit = activeFlashItem.StockLimit;
            dto.FlashSaleSoldQuantity = activeFlashItem.SoldQuantity;
        }

        return dto;
    }

    // ==========================================
    // 5. CACHING CAC DANH MUC PHAN LOAI TINH
    // ==========================================

    // Lay danh sach thuong hieu (co ap dung MemoryCache 1 gio)
    public async Task<List<Brand>> GetBrandsAsync()
    {
        if (_cache != null)
        {
            return await _cache.GetOrCreateAsync("catalog_brands_all", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
            }) ?? new List<Brand>();
        }
        return await _context.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
    }

    // Lay danh sach danh muc nganh hang (co ap dung MemoryCache 1 gio)
    public async Task<List<Category>> GetCategoriesAsync()
    {
        if (_cache != null)
        {
            return await _cache.GetOrCreateAsync("catalog_categories_all", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            }) ?? new List<Category>();
        }
        return await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    // Lay danh sach nhom huong (co ap dung MemoryCache 1 gio)
    public async Task<List<FragranceFamily>> GetFragranceFamiliesAsync()
    {
        if (_cache != null)
        {
            return await _cache.GetOrCreateAsync("catalog_families_all", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.FragranceFamilies.AsNoTracking().OrderBy(f => f.Name).ToListAsync();
            }) ?? new List<FragranceFamily>();
        }
        return await _context.FragranceFamilies.AsNoTracking().OrderBy(f => f.Name).ToListAsync();
    }

    // Lay danh sach tat ca not huong (co ap dung MemoryCache 1 gio)
    public async Task<List<FragranceNote>> GetFragranceNotesAsync()
    {
        if (_cache != null)
        {
            return await _cache.GetOrCreateAsync("catalog_notes_all", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.FragranceNotes.AsNoTracking().OrderBy(n => n.Name).ToListAsync();
            }) ?? new List<FragranceNote>();
        }
        return await _context.FragranceNotes.AsNoTracking().OrderBy(n => n.Name).ToListAsync();
    }

    // ==========================================
    // 6. HAM PHU TRO TRUY VAN VA MAP PRODUCT CARD
    // ==========================================

    // Ham phu tro lay thong tin Card san pham va kiem tra Flash Sale hang loat
    private async Task<List<ProductCardDto>> QueryProductCards(IQueryable<Product> query)
    {
        var cards = await query
            .AsNoTracking()
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                BrandCountry = p.Brand != null ? p.Brand.Country : null,
                MainImageUrl = p.MainImageUrl,
                BasePrice = p.BasePrice,
                FamilyName = p.FragranceFamily != null ? p.FragranceFamily.Name : null,
                FamilyColor = p.FragranceFamily != null ? p.FragranceFamily.ColorHex : null,
                Concentration = p.Concentration,
                Gender = p.Gender,
                RatingAverage = p.RatingAverage,
                RatingCount = p.RatingCount,
                IsNewArrival = p.IsNewArrival,
                IsBestSeller = p.IsBestSeller,
                TopNotes = p.FragranceNotes
                    .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
                    .Select(fn => fn.FragranceNote!.Name)
                    .Take(3)
                    .ToList()
            })
            .ToListAsync();

        if (cards.Any())
        {
            // Tinh toan thong tin Flash Sale cho tap san pham trong 1 truy van duy nhat
            var now = DateTime.UtcNow;
            var productIds = cards.Select(c => c.Id).ToList();
            var activeItems = await _context.FlashSaleItems
                .AsNoTracking()
                .Include(i => i.FlashSale)
                .Where(i => productIds.Contains(i.ProductId)
                    && i.FlashSale != null
                    && i.FlashSale.IsActive
                    && i.FlashSale.StartTime <= now
                    && i.FlashSale.EndTime > now
                    && i.SoldQuantity < i.StockLimit)
                .ToListAsync();

            foreach (var card in cards)
            {
                var flashItem = activeItems.FirstOrDefault(i => i.ProductId == card.Id);
                if (flashItem != null)
                {
                    card.IsFlashSale = true;
                    card.FlashSalePrice = flashItem.FlashSalePrice;
                    card.FlashSaleDiscountPercent = card.BasePrice > 0
                        ? (int)Math.Round((1m - (flashItem.FlashSalePrice / card.BasePrice)) * 100m)
                        : 0;
                }
            }
        }

        return cards;
    }
}
