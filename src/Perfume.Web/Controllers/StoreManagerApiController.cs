using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Text;
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
public sealed class StoreManagerApiController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IAntiforgery _antiforgery;

    public StoreManagerApiController(PerfumeDbContext db, IAntiforgery antiforgery)
    {
        _db = db;
        _antiforgery = antiforgery;
    }

    [NonAction]
    public Task<IActionResult> CreateProduct(StoreProductCreateRequest request, CancellationToken cancellationToken) =>
        AttachCompatibilityContext(new StoreManagerCatalogApiController(_db, _antiforgery)).CreateProduct(request, cancellationToken);

    [NonAction]
    public Task<IActionResult> UpdateInventory(int variantId, UpdateInventoryRequest request, CancellationToken cancellationToken) =>
        AttachCompatibilityContext(new StoreManagerInventoryApiController(_db, _antiforgery)).UpdateInventory(variantId, request, cancellationToken);

    [NonAction]
    public Task<IActionResult> CreateVoucher(StoreVoucherRequest request, CancellationToken cancellationToken) =>
        AttachCompatibilityContext(new StoreManagerPromotionsApiController(_db, _antiforgery)).CreateVoucher(request, cancellationToken);

    private T AttachCompatibilityContext<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = ControllerContext;
        return controller;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var fromDate = (from ?? today.AddDays(-29)).Date;
        var toDate = (to ?? today).Date;
        if (toDate < fromDate) return BadRequest(new { message = "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu." });
        var endExclusive = toDate.AddDays(1);
        var period = _db.Orders.Where(o => o.CreatedAt >= fromDate && o.CreatedAt < endExclusive);
        var paid = period.Where(o => o.OrderStatus == OrderStatus.Delivered && o.PaymentStatus == PaymentStatus.Completed);
        var revenue = await paid.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;
        var paidCount = await paid.CountAsync(cancellationToken);
        var totalOrders = await period.CountAsync(cancellationToken);
        var cancelled = await period.CountAsync(o => o.OrderStatus == OrderStatus.Cancelled, cancellationToken);
        var statuses = await period.GroupBy(o => o.OrderStatus)
            .Select(group => new { status = group.Key.ToString(), count = group.Count() })
            .ToDictionaryAsync(x => x.status, x => x.count, cancellationToken);
        var topProducts = await _db.OrderItems
            .Where(i => i.Order != null && i.Order.CreatedAt >= fromDate && i.Order.CreatedAt < endExclusive &&
                        i.Order.OrderStatus == OrderStatus.Delivered && i.Order.PaymentStatus == PaymentStatus.Completed)
            .GroupBy(i => i.ProductName)
            .Select(group => new { name = group.Key, quantity = group.Sum(i => i.Quantity), revenue = group.Sum(i => i.UnitPrice * i.Quantity) })
            .OrderByDescending(x => x.quantity).Take(10).ToListAsync(cancellationToken);
        var stock = await _db.ProductVariants.AsNoTracking().Include(v => v.Product)
            .Where(v => v.StockQuantity <= 5)
            .OrderBy(v => v.StockQuantity).ThenBy(v => v.Product!.Name)
            .Select(v => new { variantId = v.Id, productId = v.ProductId, productName = v.Product!.Name, v.VolumeLabel, v.VariantSku, v.StockQuantity })
            .ToListAsync(cancellationToken);
        var vouchers = await paid.Where(o => o.VoucherCode != null)
            .GroupBy(o => o.VoucherCode!)
            .Select(group => new { code = group.Key, orders = group.Count(), revenue = group.Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.orders).ToListAsync(cancellationToken);

        return Ok(new
        {
            from = fromDate,
            to = toDate,
            revenue,
            paidOrders = paidCount,
            averageOrderValue = paidCount == 0 ? 0m : revenue / paidCount,
            orderCount = totalOrders,
            pendingOrders = statuses.GetValueOrDefault(OrderStatus.Pending.ToString()),
            cancelledOrders = cancelled,
            cancellationRate = totalOrders == 0 ? 0 : Math.Round(cancelled * 100d / totalOrders, 1),
            statuses,
            topProducts,
            lowStock = stock.Where(v => v.StockQuantity > 0),
            outOfStock = stock.Where(v => v.StockQuantity == 0),
            voucherStats = vouchers
        });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] OrderStatus? status, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.OrderStatus == status.Value);
        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items = items.Select(ToOrderDto), totalCount, page, pageSize, pageCount = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> Order(int id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return order == null ? NotFound() : Ok(ToOrderDto(order));
    }

    [HttpPut("orders/{id:int}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateStoreOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null) return NotFound();
        var valid = request.Status == order.OrderStatus ||
            (order.OrderStatus == OrderStatus.Pending && request.Status is OrderStatus.Confirmed or OrderStatus.Cancelled) ||
            (order.OrderStatus == OrderStatus.Confirmed && request.Status is OrderStatus.Shipping or OrderStatus.Cancelled) ||
            (order.OrderStatus == OrderStatus.Shipping && request.Status == OrderStatus.Delivered);
        if (!valid) return Conflict(new { message = "Trạng thái đơn không hợp lệ." });
        if (request.Status == OrderStatus.Cancelled && order.PaymentStatus == PaymentStatus.Completed)
            return Conflict(new { message = "Đơn đã thanh toán; cần xử lý hoàn tiền trước khi hủy." });

        try
        {
            if (request.Status == OrderStatus.Cancelled && order.OrderStatus != OrderStatus.Cancelled)
            {
                foreach (var item in order.Items.Where(i => i.VariantId.HasValue))
                {
                    if (_db.Database.IsRelational())
                    {
                        await _db.ProductVariants.Where(v => v.Id == item.VariantId!.Value)
                            .ExecuteUpdateAsync(update => update.SetProperty(v => v.StockQuantity, v => v.StockQuantity + item.Quantity), cancellationToken);
                    }
                    else
                    {
                        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.VariantId!.Value, cancellationToken);
                        if (variant != null) variant.StockQuantity += item.Quantity;
                    }
                }
                foreach (var productId in order.Items.Where(i => i.VariantId.HasValue).Select(i => i.ProductId).Distinct())
                    await RefreshProductStockAsync(productId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(order.VoucherCode))
                {
                    if (_db.Database.IsRelational())
                    {
                        await _db.Vouchers.Where(v => v.Code == order.VoucherCode && v.UsedCount > 0)
                            .ExecuteUpdateAsync(update => update.SetProperty(v => v.UsedCount, v => v.UsedCount - 1), cancellationToken);
                    }
                    else
                    {
                        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == order.VoucherCode && v.UsedCount > 0, cancellationToken);
                        if (voucher != null) voucher.UsedCount--;
                    }
                }
            }
            order.OrderStatus = request.Status;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return Ok(new { order.Id, order.OrderCode, order.OrderStatus, order.PaymentStatus });
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpPost("orders/{id:int}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null) return NotFound();
        var canConfirmCod = order.PaymentMethod == PaymentMethod.COD && order.OrderStatus == OrderStatus.Delivered;
        var canConfirmTransfer = order.PaymentMethod == PaymentMethod.BankTransfer && order.PaymentStatus == PaymentStatus.Pending && order.OrderStatus != OrderStatus.Cancelled;
        if (!canConfirmCod && !canConfirmTransfer)
            return Conflict(new { message = "Chỉ xác nhận COD sau khi giao thành công hoặc chuyển khoản đang chờ đối soát." });
        order.PaymentStatus = PaymentStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { order.Id, order.OrderCode, order.PaymentStatus });
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews([FromQuery] bool? approved, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Reviews.AsNoTracking().Include(r => r.Product).AsQueryable();
        if (approved.HasValue) query = query.Where(r => r.IsApproved == approved.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.Id, r.ProductId, productName = r.Product!.Name, r.UserName, r.Rating, r.Comment, r.ImageUrls, r.IsApproved, r.CreatedAt })
            .ToListAsync(cancellationToken);
        return Ok(new { items, totalCount, page, pageSize, pageCount = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    [HttpPut("reviews/{reviewId:int}/moderation")]
    public async Task<IActionResult> ModerateReview(int reviewId, ModerateStoreReviewRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review == null) return NotFound();
        review.IsApproved = request.Approve;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == review.ProductId, cancellationToken);
        if (product != null)
        {
            var otherApproved = _db.Reviews.Where(r => r.ProductId == review.ProductId && r.IsApproved && r.Id != reviewId);
            var count = await otherApproved.CountAsync(cancellationToken) + (request.Approve ? 1 : 0);
            var ratingSum = await otherApproved.SumAsync(r => (int?)r.Rating, cancellationToken) ?? 0;
            product.RatingCount = count;
            product.RatingAverage = count == 0 ? 0 : (ratingSum + (request.Approve ? review.Rating : 0)) / (double)count;
            product.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { review.Id, review.ProductId, review.IsApproved, message = request.Approve ? "Đánh giá đã được duyệt." : "Đánh giá đã bị từ chối." });
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

    private static object ToOrderDto(Order order) => new
    {
        order.Id, order.OrderCode, order.CreatedAt,
        orderStatus = order.OrderStatus.ToString(), paymentStatus = order.PaymentStatus.ToString(), paymentMethod = order.PaymentMethod.ToString(),
        order.CustomerName, order.CustomerPhone, order.CustomerEmail, order.ShippingAddress, order.City,
        order.SubTotal, order.DiscountAmount, order.ShippingFee, order.TotalAmount, order.VoucherCode, order.TrackingNumber,
        items = order.Items.Select(i => new { i.ProductId, i.VariantId, i.ProductName, i.VariantLabel, i.UnitPrice, i.Quantity, totalPrice = i.UnitPrice * i.Quantity })
    };
}

public sealed class UpdateStoreOrderStatusRequest
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}

public sealed class UpdateProductAvailabilityRequest
{
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal BasePrice { get; set; }
    public bool IsInStock { get; set; }
}

public sealed class StoreProductUpdateRequest
{
    [Required, StringLength(255, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? ShortDescription { get; set; }
    [StringLength(20000)]
    public string? FullDescription { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal BasePrice { get; set; }
    [EnumDataType(typeof(GenderTarget))]
    public GenderTarget Gender { get; set; }
    [EnumDataType(typeof(Concentration))]
    public Concentration Concentration { get; set; }
    [StringLength(1000)]
    public string? MainImageUrl { get; set; }
    [StringLength(200)]
    public string? MetaTitle { get; set; }
    [StringLength(500)]
    public string? MetaDescription { get; set; }
    public bool IsInStock { get; set; }
}

public sealed class StoreProductCreateRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    [StringLength(200)]
    public string? Slug { get; set; }
    [Required, StringLength(80, MinimumLength = 2)]
    public string Sku { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
    public int? FragranceFamilyId { get; set; }
    [EnumDataType(typeof(GenderTarget))]
    public GenderTarget Gender { get; set; } = GenderTarget.Unisex;
    [EnumDataType(typeof(Concentration))]
    public Concentration Concentration { get; set; } = Concentration.EDP;
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal BasePrice { get; set; }
    [StringLength(1000)]
    public string? MainImageUrl { get; set; }
    [StringLength(1000)]
    public string? ShortDescription { get; set; }
    [StringLength(20000)]
    public string? FullDescription { get; set; }
    [Range(1, 2000)]
    public int InitialVolumeMl { get; set; } = 100;
    [Required, StringLength(80, MinimumLength = 1)]
    public string InitialVolumeLabel { get; set; } = "Chai Full 100ml";
    [Range(0, 100000)]
    public int InitialStockQuantity { get; set; } = 20;
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsNewArrival { get; set; } = true;
}

public sealed class UpdateInventoryRequest
{
    [Range(0, 100000)]
    public int StockQuantity { get; set; }
}

public sealed class StoreVariantRequest
{
    [Range(1, 2000)]
    public int VolumeMl { get; set; }
    [Required, StringLength(80, MinimumLength = 1)]
    public string VolumeLabel { get; set; } = string.Empty;
    [Required, StringLength(80, MinimumLength = 1)]
    public string VariantSku { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal OriginalPrice { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal SalePrice { get; set; }
    [Range(0, 100000)]
    public int StockQuantity { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class StoreVoucherRequest
{
    [Required, StringLength(40, MinimumLength = 3)]
    public string Code { get; set; } = string.Empty;
    [StringLength(250)]
    public string? Description { get; set; }
    [Range(typeof(decimal), "0", "100")]
    public decimal? DiscountPercent { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal? DiscountAmount { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal MinOrderAmount { get; set; }
    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal? MaxDiscountAmount { get; set; }
    [Range(1, 1000000)]
    public int UsageLimit { get; set; } = 100;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ModerateStoreReviewRequest
{
    public bool Approve { get; set; }
}
