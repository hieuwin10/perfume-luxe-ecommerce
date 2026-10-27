using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

[ApiController]
[Route("api/v1/store-manager")]
[Authorize(Policy = "StoreManagerApi")]
public sealed class StoreManagerInventoryApiController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IAntiforgery _antiforgery;

    public StoreManagerInventoryApiController(PerfumeDbContext db, IAntiforgery antiforgery)
    {
        _db = db;
        _antiforgery = antiforgery;
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] int lowStockThreshold = 5, [FromQuery] bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        lowStockThreshold = Math.Clamp(lowStockThreshold, 0, 100000);
        var query = _db.ProductVariants.AsNoTracking().Include(v => v.Product).AsQueryable();
        if (lowStockOnly) query = query.Where(v => v.StockQuantity <= lowStockThreshold);
        var rows = await query.OrderBy(v => v.StockQuantity).ThenBy(v => v.Product!.Name)
            .Select(v => new { v.Id, v.ProductId, productName = v.Product!.Name, v.VolumeLabel, v.VariantSku, v.StockQuantity, v.OriginalPrice, v.SalePrice })
            .ToListAsync(cancellationToken);
        return Ok(new { lowStockThreshold, items = rows });
    }

    [HttpPut("inventory/{variantId:int}")]
    public async Task<IActionResult> UpdateInventory(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
        if (variant == null) return NotFound();
        variant.StockQuantity = request.StockQuantity;
        variant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await RefreshProductStockAsync(variant.ProductId, cancellationToken);
        return Ok(new { variant.Id, variant.ProductId, variant.VariantSku, variant.StockQuantity, variant.UpdatedAt });
    }

    private async Task<bool> ValidateCookieWriteAsync()
    {
        if (Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return true;
        try { await _antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private IActionResult AntiforgeryFailure() => BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });

    private async Task RefreshProductStockAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product == null) return;
        product.IsInStock = product.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0);
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
