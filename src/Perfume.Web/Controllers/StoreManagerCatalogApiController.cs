using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
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
public sealed class StoreManagerCatalogApiController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IAntiforgery _antiforgery;

    public StoreManagerCatalogApiController(PerfumeDbContext db, IAntiforgery antiforgery)
    {
        _db = db;
        _antiforgery = antiforgery;
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category).Include(p => p.Variants).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term) || (p.Brand != null && p.Brand.Name.Contains(term)));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = products.Select(p => new
        {
            p.Id, p.Name, p.Slug, p.Sku, p.BasePrice, p.IsInStock,
            brand = p.Brand?.Name,
            category = p.Category?.Name,
            variants = p.Variants.OrderBy(v => v.VolumeMl).Select(v => new
            {
                v.Id, v.VolumeMl, v.VolumeLabel, v.VariantSku, v.OriginalPrice, v.SalePrice, v.StockQuantity, v.IsDefault
            })
        });
        return Ok(new { items, totalCount, page, pageSize, pageCount = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> Product(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category)
            .Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null) return NotFound();
        return Ok(new
        {
            product.Id, product.Name, product.Slug, product.Sku, product.ShortDescription, product.FullDescription,
            product.BasePrice, product.MainImageUrl, product.Gender, product.Concentration, product.IsInStock,
            product.MetaTitle, product.MetaDescription,
            brand = product.Brand?.Name,
            category = product.Category?.Name,
            variants = product.Variants.OrderBy(v => v.VolumeMl).Select(v => new
            {
                v.Id, v.VolumeMl, v.VolumeLabel, v.VariantSku, v.OriginalPrice, v.SalePrice, v.StockQuantity, v.IsDefault
            })
        });
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(StoreProductCreateRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        if (!await _db.Brands.AnyAsync(b => b.Id == request.BrandId, cancellationToken) ||
            !await _db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            return BadRequest(new { message = "Thương hiệu hoặc danh mục không tồn tại." });
        if (request.FragranceFamilyId.HasValue && !await _db.FragranceFamilies.AnyAsync(f => f.Id == request.FragranceFamilyId.Value, cancellationToken))
            return BadRequest(new { message = "Nhóm hương không tồn tại." });

        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Sku == sku, cancellationToken))
            return Conflict(new { message = "Mã SKU sản phẩm đã được sử dụng." });
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? CreateSlug(request.Name) : CreateSlug(request.Slug);
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest(new { message = "Không tạo được đường dẫn sản phẩm từ tên đã nhập." });
        if (await _db.Products.AnyAsync(p => p.Slug == slug, cancellationToken))
            return Conflict(new { message = "Đường dẫn sản phẩm đã tồn tại; hãy chọn slug khác." });
        var variantSku = $"{sku}-V{request.InitialVolumeMl}";
        if (variantSku.Length > 80 || await _db.ProductVariants.AnyAsync(v => v.VariantSku == variantSku, cancellationToken))
            return Conflict(new { message = "Không tạo được SKU duy nhất cho phiên bản đầu tiên." });

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Sku = sku,
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            FragranceFamilyId = request.FragranceFamilyId,
            Gender = request.Gender,
            Concentration = request.Concentration,
            BasePrice = request.BasePrice,
            MainImageUrl = request.MainImageUrl?.Trim(),
            ShortDescription = request.ShortDescription?.Trim(),
            FullDescription = request.FullDescription?.Trim(),
            IsInStock = request.InitialStockQuantity > 0,
            IsFeatured = request.IsFeatured,
            IsBestSeller = request.IsBestSeller,
            IsNewArrival = request.IsNewArrival,
            RatingCount = 0,
            RatingAverage = 0,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(new ProductVariant
        {
            VolumeMl = request.InitialVolumeMl,
            VolumeLabel = request.InitialVolumeLabel.Trim(),
            VariantSku = variantSku,
            OriginalPrice = request.BasePrice,
            SalePrice = request.BasePrice,
            StockQuantity = request.InitialStockQuantity,
            IsDefault = true
        });
        if (!string.IsNullOrWhiteSpace(product.MainImageUrl))
            product.Images.Add(new ProductImage { ImageUrl = product.MainImageUrl, AltText = product.Name, IsMain = true, DisplayOrder = 1 });

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Product), new { id = product.Id }, new { product.Id, product.Name, product.Slug, product.Sku, product.BasePrice, product.IsInStock });
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, StoreProductUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null) return NotFound();
        if (request.IsInStock && await _db.ProductVariants.AnyAsync(v => v.ProductId == id, cancellationToken) &&
            !await _db.ProductVariants.AnyAsync(v => v.ProductId == id && v.StockQuantity > 0, cancellationToken))
            return Conflict(new { message = "Không thể bật bán khi tất cả phiên bản đều hết hàng." });
        product.Name = request.Name.Trim();
        product.ShortDescription = request.ShortDescription?.Trim();
        product.FullDescription = request.FullDescription?.Trim();
        product.BasePrice = request.BasePrice;
        product.Gender = request.Gender;
        product.Concentration = request.Concentration;
        product.IsInStock = request.IsInStock;
        product.MainImageUrl = request.MainImageUrl?.Trim();
        product.MetaTitle = request.MetaTitle?.Trim();
        product.MetaDescription = request.MetaDescription?.Trim();
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { product.Id, product.Name, product.Slug, product.Sku, product.BasePrice, product.IsInStock, product.UpdatedAt });
    }

    [HttpPut("products/{id:int}/availability")]
    public async Task<IActionResult> UpdateProductAvailability(int id, UpdateProductAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null) return NotFound();
        if (request.IsInStock && await _db.ProductVariants.AnyAsync(v => v.ProductId == id, cancellationToken) &&
            !await _db.ProductVariants.AnyAsync(v => v.ProductId == id && v.StockQuantity > 0, cancellationToken))
            return Conflict(new { message = "Không thể bật bán khi tất cả phiên bản đều hết hàng." });
        product.BasePrice = request.BasePrice;
        product.IsInStock = request.IsInStock;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { product.Id, product.BasePrice, product.IsInStock, product.UpdatedAt });
    }

    [HttpPost("products/{productId:int}/variants")]
    public async Task<IActionResult> CreateVariant(int productId, StoreVariantRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        if (request.SalePrice > 0 && request.SalePrice > request.OriginalPrice)
            return BadRequest(new { message = "Giá bán không được cao hơn giá gốc." });
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product == null) return NotFound();
        if (await _db.ProductVariants.AnyAsync(v => v.VariantSku == request.VariantSku.Trim() || (v.ProductId == productId && v.VolumeMl == request.VolumeMl), cancellationToken))
            return Conflict(new { message = "SKU hoặc dung tích này đã tồn tại." });
        var hasVariants = await _db.ProductVariants.AnyAsync(v => v.ProductId == productId, cancellationToken);
        var variant = new ProductVariant
        {
            ProductId = productId,
            VolumeMl = request.VolumeMl,
            VolumeLabel = request.VolumeLabel.Trim(),
            VariantSku = request.VariantSku.Trim(),
            OriginalPrice = request.OriginalPrice,
            SalePrice = request.SalePrice,
            StockQuantity = request.StockQuantity,
            IsDefault = request.IsDefault || !hasVariants
        };
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (variant.IsDefault)
            await _db.ProductVariants.Where(v => v.ProductId == productId).ExecuteUpdateAsync(update => update.SetProperty(v => v.IsDefault, false), cancellationToken);
        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(cancellationToken);
        await RefreshProductStockAsync(productId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(Product), new { id = productId }, new { variant.Id, variant.ProductId, variant.VolumeMl, variant.VolumeLabel, variant.VariantSku, variant.OriginalPrice, variant.SalePrice, variant.StockQuantity, variant.IsDefault });
    }

    [HttpPut("variants/{variantId:int}")]
    public async Task<IActionResult> UpdateVariant(int variantId, StoreVariantRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        if (request.SalePrice > 0 && request.SalePrice > request.OriginalPrice)
            return BadRequest(new { message = "Giá bán không được cao hơn giá gốc." });
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
        if (variant == null) return NotFound();
        if (await _db.ProductVariants.AnyAsync(v => v.Id != variantId && (v.VariantSku == request.VariantSku.Trim() || (v.ProductId == variant.ProductId && v.VolumeMl == request.VolumeMl)), cancellationToken))
            return Conflict(new { message = "SKU hoặc dung tích này đã tồn tại." });
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
            await _db.ProductVariants.Where(v => v.ProductId == variant.ProductId && v.Id != variantId).ExecuteUpdateAsync(update => update.SetProperty(v => v.IsDefault, false), cancellationToken);
        variant.VolumeMl = request.VolumeMl;
        variant.VolumeLabel = request.VolumeLabel.Trim();
        variant.VariantSku = request.VariantSku.Trim();
        variant.OriginalPrice = request.OriginalPrice;
        variant.SalePrice = request.SalePrice;
        variant.StockQuantity = request.StockQuantity;
        variant.IsDefault = request.IsDefault || variant.IsDefault;
        variant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await RefreshProductStockAsync(variant.ProductId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { variant.Id, variant.ProductId, variant.VolumeMl, variant.VolumeLabel, variant.VariantSku, variant.OriginalPrice, variant.SalePrice, variant.StockQuantity, variant.IsDefault, variant.UpdatedAt });
    }

    [HttpDelete("variants/{variantId:int}")]
    public async Task<IActionResult> DeleteVariant(int variantId, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
        if (variant == null) return NotFound();
        if (await _db.OrderItems.AnyAsync(i => i.VariantId == variantId, cancellationToken) ||
            await _db.ShoppingCartItems.AnyAsync(i => i.VariantId == variantId, cancellationToken))
            return Conflict(new { message = "Không thể xóa phiên bản đã xuất hiện trong đơn hàng hoặc giỏ hàng." });
        var wasDefault = variant.IsDefault;
        var productId = variant.ProductId;
        variant.IsDeleted = true;
        if (wasDefault)
        {
            var replacement = await _db.ProductVariants.Where(v => v.ProductId == productId && v.Id != variantId)
                .OrderBy(v => v.VolumeMl).FirstOrDefaultAsync(cancellationToken);
            if (replacement != null) replacement.IsDefault = true;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await RefreshProductStockAsync(productId, cancellationToken);
        return NoContent();
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

    private static DateTime Utc(DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime,
        DateTimeKind.Local => dateTime.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
    };

    private static string CreateSlug(string value)
    {
        var decomposed = value.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(withoutMarks.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }
}
