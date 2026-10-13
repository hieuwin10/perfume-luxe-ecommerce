using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,StoreManager")]
public class StoreManagerController : Controller
{
    private readonly PerfumeDbContext _db;
    public StoreManagerController(PerfumeDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? preset = null)
    {
        var now = DateTime.UtcNow;
        DateTime fromDate;
        DateTime toDate = now.Date;

        if (!string.IsNullOrEmpty(preset))
        {
            switch (preset.ToLowerInvariant())
            {
                case "today":
                    fromDate = now.Date;
                    toDate = now.Date;
                    break;
                case "7days":
                    fromDate = now.Date.AddDays(-6);
                    toDate = now.Date;
                    break;
                case "thismonth":
                    fromDate = new DateTime(now.Year, now.Month, 1);
                    toDate = now.Date;
                    break;
                case "thisquarter":
                    int currentQuarter = (now.Month - 1) / 3 + 1;
                    fromDate = new DateTime(now.Year, (currentQuarter - 1) * 3 + 1, 1);
                    toDate = now.Date;
                    break;
                default:
                    fromDate = (from ?? now.Date.AddDays(-29)).Date;
                    toDate = (to ?? now.Date).Date;
                    break;
            }
        }
        else
        {
            fromDate = (from ?? now.Date.AddDays(-29)).Date;
            toDate = (to ?? now.Date).Date;
        }

        if (toDate < fromDate) toDate = fromDate;
        var endExclusive = toDate.AddDays(1);

        var period = _db.Orders.Where(o => o.CreatedAt >= fromDate && o.CreatedAt < endExclusive);
        var totalOrderCount = await period.CountAsync();
        var paid = period.Where(o => o.OrderStatus == OrderStatus.Delivered && o.PaymentStatus == PaymentStatus.Completed);
        var paidCount = await paid.CountAsync();
        var revenue = await paid.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var cancelledOrdersQuery = period.Where(o => o.OrderStatus == OrderStatus.Cancelled);
        var cancelledCount = await cancelledOrdersQuery.CountAsync();
        var lostRevenue = await cancelledOrdersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        var cancellationRate = totalOrderCount > 0 ? Math.Round((double)cancelledCount / totalOrderCount * 100, 1) : 0;

        var totalDiscountGiven = await period.SumAsync(o => (decimal?)o.DiscountAmount) ?? 0m;
        var voucherOrdersCount = await period.CountAsync(o => !string.IsNullOrEmpty(o.VoucherCode));
        var voucherAdoptionRate = totalOrderCount > 0 ? Math.Round((double)voucherOrdersCount / totalOrderCount * 100, 1) : 0;

        // Payment Methods & Dòng tiền
        var paymentMethods = await period
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new PaymentMethodStat
            {
                Method = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
                PaidAmount = g.Where(o => o.OrderStatus == OrderStatus.Delivered && o.PaymentStatus == PaymentStatus.Completed).Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        var statuses = await period.GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count);

        var topProducts = await _db.OrderItems
            .Where(i => i.Order != null && i.Order.CreatedAt >= fromDate && i.Order.CreatedAt < endExclusive &&
                        i.Order.OrderStatus == OrderStatus.Delivered && i.Order.PaymentStatus == PaymentStatus.Completed)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductStat { Name = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.UnitPrice * i.Quantity) })
            .OrderByDescending(x => x.Quantity).Take(10).ToListAsync();

        // Kho bãi & Tồn kho
        var allVariants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();
        var outOfStockVariants = allVariants.Where(v => v.StockQuantity <= 0).ToList();
        var lowStockVariants = allVariants.Where(v => v.StockQuantity > 0 && v.StockQuantity <= 5).OrderBy(v => v.StockQuantity).ToList();
        var totalStockUnits = allVariants.Sum(v => v.StockQuantity);
        var totalStockValuation = allVariants.Sum(v => v.StockQuantity * (v.SalePrice > 0 ? v.SalePrice : v.OriginalPrice));

        // Khách hàng thân thiết VIP trong kỳ
        var topVipCustomers = await paid
            .Where(o => !string.IsNullOrEmpty(o.CustomerEmail))
            .GroupBy(o => new { o.CustomerName, o.CustomerEmail, o.CustomerPhone })
            .Select(g => new VipCustomerStat
            {
                Name = g.Key.CustomerName,
                Email = g.Key.CustomerEmail,
                Phone = g.Key.CustomerPhone ?? "",
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(5)
            .ToListAsync();

        var voucherStats = await paid.Where(o => o.VoucherCode != null)
            .GroupBy(o => o.VoucherCode!)
            .Select(g => new VoucherUsageStat { Code = g.Key, Orders = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.Orders).ToListAsync();

        return View(new StoreManagerDashboardViewModel
        {
            From = fromDate,
            To = toDate,
            CurrentPreset = preset ?? "custom",
            Revenue = revenue,
            PaidOrders = paidCount,
            AverageOrderValue = paidCount == 0 ? 0 : revenue / paidCount,
            OrderCount = totalOrderCount,
            StatusCounts = statuses,
            TopProducts = topProducts,
            LowStock = lowStockVariants,
            OutOfStock = outOfStockVariants,
            TotalStockUnits = totalStockUnits,
            TotalStockValuation = totalStockValuation,
            VoucherStats = voucherStats,
            PendingOrders = await period.CountAsync(o => o.OrderStatus == OrderStatus.Pending),
            CancelledOrders = cancelledCount,
            CancellationRate = cancellationRate,
            LostRevenue = lostRevenue,
            TotalDiscountGiven = totalDiscountGiven,
            VoucherOrdersCount = voucherOrdersCount,
            VoucherAdoptionRate = voucherAdoptionRate,
            PaymentMethodStats = paymentMethods,
            TopVipCustomers = topVipCustomers
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to)
    {
        var fromDate = (from ?? DateTime.UtcNow.Date.AddDays(-29)).Date;
        var toDate = (to ?? DateTime.UtcNow.Date).Date;
        if (toDate < fromDate) toDate = fromDate;
        var endExclusive = toDate.AddDays(1);
        var orders = await _db.Orders.Where(o => o.CreatedAt >= fromDate && o.CreatedAt < endExclusive)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        static string Cell(string? value)
        {
            var safe = value ?? "";
            if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r') safe = "'" + safe;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
        var csv = new StringBuilder("\uFEFF");
        csv.AppendLine("OrderCode,Customer,CreatedAt,OrderStatus,PaymentStatus,PaymentMethod,SubTotal,Discount,Shipping,Total,Voucher");
        foreach (var order in orders)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Cell(order.OrderCode), Cell(order.CustomerName), Cell(order.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Cell(order.OrderStatus.ToString()), Cell(order.PaymentStatus.ToString()), Cell(order.PaymentMethod.ToString()),
                order.SubTotal.ToString(CultureInfo.InvariantCulture), order.DiscountAmount.ToString(CultureInfo.InvariantCulture),
                order.ShippingFee.ToString(CultureInfo.InvariantCulture), order.TotalAmount.ToString(CultureInfo.InvariantCulture), Cell(order.VoucherCode)
            }));
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"perfume-orders-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.csv");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        var valid = status == order.OrderStatus ||
            (order.OrderStatus == OrderStatus.Pending && status is OrderStatus.Confirmed or OrderStatus.Cancelled) ||
            (order.OrderStatus == OrderStatus.Confirmed && status is OrderStatus.Shipping or OrderStatus.Cancelled) ||
            (order.OrderStatus == OrderStatus.Shipping && status == OrderStatus.Delivered);
        if (!valid) { TempData["Error"] = "Trạng thái đơn không hợp lệ."; return RedirectToAction("Orders", "AdminDashboard"); }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (status == OrderStatus.Cancelled && order.OrderStatus != OrderStatus.Cancelled)
            {
                foreach (var item in order.Items.Where(i => i.VariantId.HasValue))
                    await _db.ProductVariants.Where(v => v.Id == item.VariantId!.Value)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity + item.Quantity));
                if (!string.IsNullOrEmpty(order.VoucherCode))
                    await _db.Vouchers.Where(v => v.Code == order.VoucherCode && v.UsedCount > 0)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.UsedCount, v => v.UsedCount - 1));
            }
            order.OrderStatus = status;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = $"Đã cập nhật đơn {order.OrderCode}.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Không thể cập nhật đơn hàng.";
        }
        return RedirectToAction("Orders", "AdminDashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCodPayment(int orderId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        if (order.PaymentMethod != PaymentMethod.COD || order.OrderStatus != OrderStatus.Delivered)
        {
            TempData["Error"] = "Chỉ xác nhận thu COD cho đơn đã giao thành công.";
            return RedirectToAction("Orders", "AdminDashboard");
        }
        order.PaymentStatus = PaymentStatus.Completed;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xác nhận thu COD đơn {order.OrderCode}.";
        return RedirectToAction("Orders", "AdminDashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBankTransferPayment(int orderId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        order.PaymentStatus = PaymentStatus.Completed;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xác nhận thanh toán chuyển khoản VietQR cho đơn {order.OrderCode}.";
        return RedirectToAction("OrderDetails", "AdminDashboard", new { id = orderId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariantStock(int variantId, int stockQuantity)
    {
        var variant = await _db.ProductVariants.FindAsync(variantId);
        if (variant == null) return NotFound();
        variant.StockQuantity = Math.Clamp(stockQuantity, 0, 100000);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật tồn kho phiên bản.";
        return RedirectToAction("Products", "AdminDashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductAvailability(int productId, decimal basePrice, bool isInStock)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();
        if (basePrice < 0) { TempData["Error"] = "Giá sản phẩm không hợp lệ."; return RedirectToAction("Products", "AdminDashboard"); }
        product.BasePrice = basePrice;
        product.IsInStock = isInStock;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật giá và trạng thái sản phẩm.";
        return RedirectToAction("Products", "AdminDashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariant(int variantId, int volumeMl, string volumeLabel, string variantSku, decimal originalPrice, decimal salePrice, int stockQuantity, bool isDefault = false)
    {
        var variant = await _db.ProductVariants.FindAsync(variantId);
        if (variant == null) return NotFound();
        if (volumeMl < 1 || volumeMl > 2000 || string.IsNullOrWhiteSpace(volumeLabel) || volumeLabel.Length > 80 ||
            string.IsNullOrWhiteSpace(variantSku) || variantSku.Length > 80 || originalPrice < 0 || salePrice < 0 ||
            (salePrice > 0 && salePrice > originalPrice) || stockQuantity < 0 || stockQuantity > 100000)
        {
            TempData["Error"] = "Thông tin phiên bản, giá hoặc tồn kho chưa hợp lệ.";
            return RedirectToAction("Products", "AdminDashboard");
        }
        if (await _db.ProductVariants.AnyAsync(v => v.Id != variantId && v.VariantSku == variantSku.Trim()))
        {
            TempData["Error"] = "Mã SKU phiên bản đã được sử dụng.";
            return RedirectToAction("Products", "AdminDashboard");
        }
        if (isDefault)
            await _db.ProductVariants.Where(v => v.ProductId == variant.ProductId && v.Id != variantId)
                .ExecuteUpdateAsync(q => q.SetProperty(v => v.IsDefault, false));
        variant.IsDefault = isDefault || variant.IsDefault;
        variant.VolumeMl = volumeMl;
        variant.VolumeLabel = volumeLabel.Trim();
        variant.VariantSku = variantSku.Trim();
        variant.OriginalPrice = originalPrice;
        variant.SalePrice = salePrice;
        variant.StockQuantity = stockQuantity;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật phiên bản sản phẩm.";
        return RedirectToAction("Products", "AdminDashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVariant(int productId, int volumeMl, string volumeLabel, string variantSku, decimal originalPrice, decimal salePrice, int stockQuantity, bool isDefault = false)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();
        if (volumeMl < 1 || volumeMl > 2000 || string.IsNullOrWhiteSpace(volumeLabel) || volumeLabel.Length > 80 ||
            string.IsNullOrWhiteSpace(variantSku) || variantSku.Length > 80 || originalPrice < 0 || salePrice < 0 ||
            (salePrice > 0 && salePrice > originalPrice) || stockQuantity < 0 || stockQuantity > 100000)
        {
            TempData["Error"] = "Thông tin phiên bản, giá hoặc tồn kho chưa hợp lệ.";
            return RedirectToAction("Products", "AdminDashboard");
        }
        if (await _db.ProductVariants.AnyAsync(v => v.VariantSku == variantSku.Trim() || (v.ProductId == productId && v.VolumeMl == volumeMl)))
        {
            TempData["Error"] = "SKU hoặc dung tích này đã tồn tại.";
            return RedirectToAction("Products", "AdminDashboard");
        }
        if (isDefault) await _db.ProductVariants.Where(v => v.ProductId == productId).ExecuteUpdateAsync(q => q.SetProperty(v => v.IsDefault, false));
        _db.ProductVariants.Add(new ProductVariant
        {
            ProductId = productId, VolumeMl = volumeMl, VolumeLabel = volumeLabel.Trim(), VariantSku = variantSku.Trim(),
            OriginalPrice = originalPrice, SalePrice = salePrice, StockQuantity = stockQuantity,
            IsDefault = isDefault || !await _db.ProductVariants.AnyAsync(v => v.ProductId == productId)
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm phiên bản sản phẩm.";
        return RedirectToAction("Products", "AdminDashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Vouchers()
    {
        var items = await _db.Vouchers.OrderByDescending(v => v.CreatedAt).ToListAsync();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveVoucher(int? id, string code, string description, decimal? discountPercent, decimal? discountAmount,
        decimal minOrderAmount, decimal? maxDiscountAmount, int usageLimit, DateTime validFrom, DateTime validTo, bool isActive)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length is < 3 or > 40 || usageLimit < 1 || validTo <= validFrom ||
            (discountPercent.HasValue == discountAmount.HasValue) ||
            (discountPercent.HasValue && (discountPercent < 0 || discountPercent > 100)) ||
            (discountAmount.HasValue && discountAmount < 0))
        {
            TempData["Error"] = "Thông tin voucher chưa hợp lệ.";
            return RedirectToAction(nameof(Vouchers));
        }
        var voucher = id.HasValue ? await _db.Vouchers.FindAsync(id.Value) : null;
        if (id.HasValue && voucher == null) return NotFound();
        if (await _db.Vouchers.AnyAsync(v => v.Code == code && (!id.HasValue || v.Id != id.Value)))
        {
            TempData["Error"] = "Mã voucher này đã tồn tại.";
            return RedirectToAction(nameof(Vouchers));
        }
        if (voucher != null && usageLimit < voucher.UsedCount)
        {
            TempData["Error"] = "Giới hạn sử dụng không được thấp hơn số lượt đã dùng.";
            return RedirectToAction(nameof(Vouchers));
        }
        if (maxDiscountAmount < 0 || minOrderAmount < 0)
        {
            TempData["Error"] = "Giá trị giảm tối đa và đơn tối thiểu không được âm.";
            return RedirectToAction(nameof(Vouchers));
        }
        if (voucher == null) { voucher = new Voucher(); _db.Vouchers.Add(voucher); }
        voucher.Code = code; voucher.Description = description?.Trim() ?? "";
        voucher.DiscountPercent = discountPercent; voucher.DiscountAmount = discountAmount;
        voucher.MinOrderAmount = Math.Max(0, minOrderAmount); voucher.MaxDiscountAmount = maxDiscountAmount;
        voucher.UsageLimit = usageLimit; voucher.ValidFrom = DateTime.SpecifyKind(validFrom, DateTimeKind.Utc);
        voucher.ValidTo = DateTime.SpecifyKind(validTo, DateTimeKind.Utc); voucher.IsActive = isActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu voucher.";
        return RedirectToAction(nameof(Vouchers));
    }

    [HttpGet]
    public async Task<IActionResult> Reviews()
    {
        var reviews = await _db.Reviews.Include(r => r.Product).Where(r => !r.IsApproved)
            .OrderBy(r => r.CreatedAt).ToListAsync();
        return View(reviews);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ModerateReview(int reviewId, bool approve)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return NotFound();
        review.IsApproved = approve;
        if (approve)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == review.ProductId);
            if (product != null)
            {
                var approved = _db.Reviews.Where(r => r.ProductId == review.ProductId && r.IsApproved && r.Id != review.Id);
                var count = await approved.CountAsync() + 1;
                var sum = await approved.SumAsync(r => (int?)r.Rating) ?? 0;
                product.RatingCount = count;
                product.RatingAverage = (sum + review.Rating) / (double)count;
            }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = approve ? "Đánh giá đã được duyệt." : "Đánh giá đã bị từ chối.";
        return RedirectToAction(nameof(Reviews));
    }
}

public class StoreManagerDashboardViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string CurrentPreset { get; set; } = "30days";
    public decimal Revenue { get; set; }
    public int PaidOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int OrderCount { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public double CancellationRate { get; set; }
    public decimal LostRevenue { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public int VoucherOrdersCount { get; set; }
    public double VoucherAdoptionRate { get; set; }
    public int TotalStockUnits { get; set; }
    public decimal TotalStockValuation { get; set; }

    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public List<TopProductStat> TopProducts { get; set; } = new();
    public List<ProductVariant> LowStock { get; set; } = new();
    public List<ProductVariant> OutOfStock { get; set; } = new();
    public List<VoucherUsageStat> VoucherStats { get; set; } = new();
    public List<PaymentMethodStat> PaymentMethodStats { get; set; } = new();
    public List<VipCustomerStat> TopVipCustomers { get; set; } = new();
}

public class TopProductStat { public string Name { get; set; } = ""; public int Quantity { get; set; } public decimal Revenue { get; set; } }
public class VoucherUsageStat { public string Code { get; set; } = ""; public int Orders { get; set; } public decimal Revenue { get; set; } }

public class PaymentMethodStat
{
    public string Method { get; set; } = "";
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
}

public class VipCustomerStat
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
