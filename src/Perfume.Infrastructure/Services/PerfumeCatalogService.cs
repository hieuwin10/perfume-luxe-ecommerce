using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class PerfumeCatalogService : IPerfumeCatalogService
{
    private readonly PerfumeDbContext _context;

    public PerfumeCatalogService(PerfumeDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductCardDto>> GetFeaturedProductsAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsFeatured).Take(8));
    }

    public async Task<List<ProductCardDto>> GetBestSellersAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsBestSeller).Take(8));
    }

    public async Task<List<ProductCardDto>> GetNewArrivalsAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsNewArrival).Take(8));
    }

    public async Task<(List<ProductCardDto> Items, int TotalCount)> GetProductsAsync(ProductFilterParams filter)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || p.Brand!.Name.Contains(kw) || p.ShortDescription!.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(filter.BrandSlug))
        {
            query = query.Where(p => p.Brand != null && p.Brand.Slug == filter.BrandSlug);
        }

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == filter.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(filter.FamilySlug))
        {
            query = query.Where(p => p.FragranceFamily != null && p.FragranceFamily.Slug == filter.FamilySlug);
        }

        if (filter.NoteId.HasValue)
        {
            query = query.Where(p => p.FragranceNotes.Any(fn => fn.FragranceNoteId == filter.NoteId.Value));
        }

        if (filter.Gender.HasValue)
        {
            query = query.Where(p => p.Gender == filter.Gender.Value || p.Gender == GenderTarget.Unisex);
        }

        if (filter.Concentration.HasValue)
        {
            query = query.Where(p => p.Concentration == filter.Concentration.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);
        }

        int totalCount = await query.CountAsync();

        query = filter.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "rating" => query.OrderByDescending(p => p.RatingAverage),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        int skip = (Math.Max(1, filter.Page) - 1) * filter.PageSize;
        var paginatedQuery = query.Skip(skip).Take(filter.PageSize);

        var items = await QueryProductCards(paginatedQuery);
        return (items, totalCount);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.FragranceFamily)
            .Include(p => p.Variants.OrderBy(v => v.VolumeMl))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.FragranceNotes)
                .ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (product == null) return null;

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
            Variants = product.Variants.Select(v => new ProductVariantDto
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
            GalleryImages = product.Images.Select(i => i.ImageUrl).ToList()
        };

        var now = DateTime.UtcNow;
        var activeFlashItem = await _context.FlashSaleItems
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

    public async Task<List<Brand>> GetBrandsAsync()
    {
        return await _context.Brands.OrderBy(b => b.Name).ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<List<FragranceFamily>> GetFragranceFamiliesAsync()
    {
        return await _context.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<List<FragranceNote>> GetFragranceNotesAsync()
    {
        return await _context.FragranceNotes.OrderBy(n => n.Name).ToListAsync();
    }

    public async Task<List<ProductCardDto>> GetQuizRecommendationsAsync(QuizAnswerRequest request)
    {
        var query = _context.Products.AsQueryable();

        if (request.GenderPreference == "men")
            query = query.Where(p => p.Gender == GenderTarget.Men || p.Gender == GenderTarget.Unisex);
        else if (request.GenderPreference == "women")
            query = query.Where(p => p.Gender == GenderTarget.Women || p.Gender == GenderTarget.Unisex);

        if (request.Vibe == "cozy")
            query = query.Where(p => p.FragranceFamily != null && (p.FragranceFamily.ScentType == ScentFamily.Woody || p.FragranceFamily.ScentType == ScentFamily.Oriental));
        else if (request.Vibe == "fresh")
            query = query.Where(p => p.FragranceFamily != null && (p.FragranceFamily.ScentType == ScentFamily.Fresh || p.FragranceFamily.ScentType == ScentFamily.Citrus));
        else if (request.Vibe == "romantic")
            query = query.Where(p => p.FragranceFamily != null && (p.FragranceFamily.ScentType == ScentFamily.Floral || p.FragranceFamily.ScentType == ScentFamily.Gourmand));

        var results = await QueryProductCards(query.OrderByDescending(p => p.RatingAverage).Take(4));
        if (!results.Any())
        {
            results = await GetFeaturedProductsAsync();
        }
        return results;
    }

    private async Task<List<ProductCardDto>> QueryProductCards(IQueryable<Product> query)
    {
        var cards = await query
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
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
            var now = DateTime.UtcNow;
            var productIds = cards.Select(c => c.Id).ToList();
            var activeItems = await _context.FlashSaleItems
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
