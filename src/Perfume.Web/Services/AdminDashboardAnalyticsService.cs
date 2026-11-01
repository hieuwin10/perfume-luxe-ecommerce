using Microsoft.EntityFrameworkCore;
using Perfume.Application.Interfaces;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Web.Areas.Admin.Controllers;

namespace Perfume.Web.Services;

public interface IAdminDashboardAnalyticsService
{
    Task<AdminDashboardViewModel> BuildDashboardAsync();
}

/// <summary>
/// Service tong hop so lieu phan tich, doanh thu va bao cao bieu do cho Admin Dashboard.
/// </summary>
public sealed class AdminDashboardAnalyticsService(
    PerfumeDbContext _db,
    IDatabaseConsoleService _dbConsole) : IAdminDashboardAnalyticsService
{
    public async Task<AdminDashboardViewModel> BuildDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalOrders = await _db.Orders.CountAsync();
        var totalRevenue = await _db.Orders.Where(o => o.OrderStatus == OrderStatus.Delivered).SumAsync(o => o.TotalAmount);
        var revenueThisMonth = await _db.Orders.Where(o => o.OrderStatus == OrderStatus.Delivered && o.CreatedAt >= startOfMonth).SumAsync(o => o.TotalAmount);
        var avgOrderValue = totalOrders > 0 ? await _db.Orders.AverageAsync(o => o.TotalAmount) : 0;

        var pendingCount = await _db.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending);
        var confirmedCount = await _db.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Confirmed);
        var shippingCount = await _db.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Shipping);
        var deliveredCount = await _db.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Delivered);
        var cancelledCount = await _db.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Cancelled);
        var fulfillmentRate = totalOrders > 0 ? Math.Round((double)deliveredCount / totalOrders * 100, 1) : 0;

        // Bieu do doanh thu 7 ngay gan nhat
        var sevenDaysAgo = now.Date.AddDays(-6);
        var recentDeliveredOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.OrderStatus == OrderStatus.Delivered && o.CreatedAt >= sevenDaysAgo)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync();

        var chartLabels = new List<string>();
        var chartData = new List<decimal>();
        for (int i = 6; i >= 0; i--)
        {
            var date = now.Date.AddDays(-i);
            var nextDate = date.AddDays(1);
            var dayRevenue = recentDeliveredOrders
                .Where(o => o.CreatedAt >= date && o.CreatedAt < nextDate)
                .Sum(o => o.TotalAmount);
            chartLabels.Add(date.ToString("dd/MM"));
            chartData.Add(dayRevenue);
        }

        // Bieu do trang thai don hang
        var orderStatusChart = new Dictionary<string, int>
        {
            { "Đã Giao (Thành công)", deliveredCount },
            { "Đang Vận Chuyển", shippingCount },
            { "Đã Xác Nhận", confirmedCount },
            { "Chờ Xử Lý", pendingCount },
            { "Đã Hủy", cancelledCount }
        };

        // Phan bo phuong thuc thanh toan
        var paymentMethodChart = new Dictionary<string, int>
        {
            { "COD (Tiền mặt)", await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.COD) },
            { "VNPAY QR", await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.VNPAY) },
            { "Chuyển Khoản", await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.BankTransfer) },
            { "Ví MoMo", await _db.Orders.CountAsync(o => o.PaymentMethod == PaymentMethod.MoMo) }
        };

        // Top san pham ban chay nhat
        var topProducts = await _db.OrderItems
            .AsNoTracking()
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

        // Thong ke theo nhom huong
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

            RecentUsers = await _db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).Take(6).ToListAsync(),
            RecentOrders = await _db.Orders.AsNoTracking().Include(o => o.Items).OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync(),
            Tables = await _dbConsole.GetAllTablesAsync(),
        };
        return vm;
    }
}
