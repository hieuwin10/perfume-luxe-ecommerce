using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class FlashSaleService : IFlashSaleService
{
    private readonly PerfumeDbContext _context;

    public FlashSaleService(PerfumeDbContext context)
    {
        _context = context;
    }

    public async Task<FlashSaleDto?> GetActiveFlashSaleAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var flashSale = await _context.FlashSales
            .Include(f => f.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Brand)
            .Include(f => f.Items)
                .ThenInclude(i => i.Variant)
            .Where(f => f.IsActive && f.StartTime <= now && f.EndTime > now)
            .OrderBy(f => f.EndTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (flashSale == null) return null;
        return MapToDto(flashSale);
    }

    public async Task<List<FlashSaleDto>> GetAllFlashSalesAsync(CancellationToken cancellationToken = default)
    {
        var flashSales = await _context.FlashSales
            .Include(f => f.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Brand)
            .Include(f => f.Items)
                .ThenInclude(i => i.Variant)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return flashSales.Select(MapToDto).ToList();
    }

    public async Task<FlashSaleDto?> GetFlashSaleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var flashSale = await _context.FlashSales
            .Include(f => f.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.Brand)
            .Include(f => f.Items)
                .ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (flashSale == null) return null;
        return MapToDto(flashSale);
    }

    public async Task<FlashSaleDto> CreateFlashSaleAsync(CreateFlashSaleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new FlashSale
        {
            Name = request.Name,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive,
            BannerUrl = request.BannerUrl
        };

        foreach (var item in request.Items)
        {
            entity.Items.Add(new FlashSaleItem
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                FlashSalePrice = item.FlashSalePrice,
                StockLimit = item.StockLimit,
                SoldQuantity = 0
            });
        }

        _context.FlashSales.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetFlashSaleByIdAsync(entity.Id, cancellationToken)) ?? MapToDto(entity);
    }

    public async Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var fs = await _context.FlashSales.FindAsync(new object[] { id }, cancellationToken);
        if (fs == null) return false;

        fs.IsActive = !fs.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var fs = await _context.FlashSales.FindAsync(new object[] { id }, cancellationToken);
        if (fs == null) return false;

        fs.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FlashSaleItemDto?> GetActiveFlashSaleItemForProductAsync(int productId, int? variantId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var item = await _context.FlashSaleItems
            .Include(i => i.FlashSale)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Brand)
            .Include(i => i.Variant)
            .Where(i => i.ProductId == productId
                && (!variantId.HasValue || i.VariantId == variantId.Value || i.VariantId == null)
                && i.FlashSale != null
                && i.FlashSale.IsActive
                && i.FlashSale.StartTime <= now
                && i.FlashSale.EndTime > now
                && i.SoldQuantity < i.StockLimit)
            .OrderBy(i => i.FlashSalePrice)
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null) return null;

        var origPrice = item.Variant != null && item.Variant.OriginalPrice > 0
            ? item.Variant.OriginalPrice
            : (item.Product?.BasePrice ?? 0);

        return new FlashSaleItemDto
        {
            Id = item.Id,
            FlashSaleId = item.FlashSaleId,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            ProductSlug = item.Product?.Slug ?? string.Empty,
            ProductImageUrl = item.Product?.MainImageUrl,
            BrandName = item.Product?.Brand?.Name,
            VariantId = item.VariantId,
            VariantLabel = item.Variant?.VolumeLabel,
            OriginalPrice = origPrice,
            FlashSalePrice = item.FlashSalePrice,
            StockLimit = item.StockLimit,
            SoldQuantity = item.SoldQuantity
        };
    }

    private static FlashSaleDto MapToDto(FlashSale f)
    {
        return new FlashSaleDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            StartTime = f.StartTime,
            EndTime = f.EndTime,
            IsActive = f.IsActive,
            BannerUrl = f.BannerUrl,
            CreatedAt = f.CreatedAt,
            Items = f.Items.Where(i => !i.IsDeleted).Select(i =>
            {
                var origPrice = i.Variant != null && i.Variant.OriginalPrice > 0
                    ? i.Variant.OriginalPrice
                    : (i.Product?.BasePrice ?? 0);

                return new FlashSaleItemDto
                {
                    Id = i.Id,
                    FlashSaleId = i.FlashSaleId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductSlug = i.Product?.Slug ?? string.Empty,
                    ProductImageUrl = i.Product?.MainImageUrl,
                    BrandName = i.Product?.Brand?.Name,
                    VariantId = i.VariantId,
                    VariantLabel = i.Variant?.VolumeLabel,
                    OriginalPrice = origPrice,
                    FlashSalePrice = i.FlashSalePrice,
                    StockLimit = i.StockLimit,
                    SoldQuantity = i.SoldQuantity
                };
            }).ToList()
        };
    }
}
