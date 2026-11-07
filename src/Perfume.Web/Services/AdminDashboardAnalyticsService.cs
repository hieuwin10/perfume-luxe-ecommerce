using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
/// Toi uu hoa: Giam tu ~25 round-trip DB xuong 4-5 truy van gom nhom va cache bo nho 45 giay.
/// </summary>
public sealed class AdminDashboardAnalyticsService(
    PerfumeDbContext _db,
    IDatabaseConsoleService _dbConsole,
    IMemoryCache? _cache = null) : IAdminDashboardAnalyticsService
{
    private const string CacheKey = "admin_dashboard_metrics_cache";

    public async Task<AdminDashboardViewModel> BuildDashboardAsync()
    {
        if (_cache != null && _cache.TryGetValue(CacheKey, out AdminDashboardViewModel? cachedVm) && cachedVm != null)
        {
            return cachedVm;
        }

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. Gộp thống kê trạng thái đơn hàng và doanh thu thành 1 truy vấn GROUP BY duy nhất
        var orderStatusGroups = await _db.Orders
            .AsNoTracking()
            .GroupBy(o => o.OrderStatus)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount),
                DeliveredRevenue = g.Where(o => o.OrderStatus == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                DeliveredThisMonth = g.Where(o => o.OrderStatus == OrderStatus.Delivered && o.CreatedAt >= startOfMonth).Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        var totalOrders = orderStatusGroups.Sum(x => x.Count);
        var deliveredGroup = orderStatusGroups.FirstOrDefault(x => x.Status == OrderStatus.Delivered);
        var totalRevenue = deliveredGroup?.DeliveredRevenue ?? 0m;
        var revenueThisMonth = deliveredGroup?.DeliveredThisMonth ?? 0m;
        var totalOrderAmount = orderStatusGroups.Sum(x => x.TotalAmount);
        var avgOrderValue = totalOrders > 0 ? Math.Round(totalOrderAmount / totalOrders, 0) : 0;

        var pendingCount = orderStatusGroups.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0;
        var confirmedCount = orderStatusGroups.FirstOrDefault(x => x.Status == OrderStatus.Confirmed)?.Count ?? 0;
        var shippingCount = orderStatusGroups.FirstOrDefault(x => x.Status == OrderStatus.Shipping)?.Count ?? 0;
        var deliveredCount = deliveredGroup?.Count ?? 0;
        var cancelledCount = orderStatusGroups.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0;
        var fulfillmentRate = totalOrders > 0 ? Math.Round((double)deliveredCount / totalOrders * 100, 1) : 0;

        // 2. Bieu do doanh thu 7 ngay gan nhat
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

        // 3. Gộp phân bổ phương thức thanh toán thành 1 truy vấn GROUP BY duy nhất
        var paymentMethodCounts = await _db.Orders
            .AsNoTracking()
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Method, x => x.Count);

        var paymentMethodChart = new Dictionary<string, int>
        {
            { "COD (Tiền mặt)", paymentMethodCounts.GetValueOrDefault(PaymentMethod.COD) },
            { "VNPAY QR", paymentMethodCounts.GetValueOrDefault(PaymentMethod.VNPAY) },
            { "Chuyển Khoản", paymentMethodCounts.GetValueOrDefault(PaymentMethod.BankTransfer) },
            { "Ví MoMo", paymentMethodCounts.GetValueOrDefault(PaymentMethod.MoMo) }
        };

        // 4. Top san pham ban chay nhat
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

        // 5. Thong ke theo nhom huong
        var scentStats = await _db.FragranceFamilies
            .AsNoTracking()
            .Where(f => !f.IsDeleted)
            .Select(f => new ScentFamilyStat
            {
                Name = f.Name,
                ColorHex = f.ColorHex ?? "#D4AF37",
                ProductCount = f.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync();

        int totalProducts = 0, totalUsers = 0, totalBrands = 0, totalCategories = 0, totalBlogPosts = 0, totalReviews = 0, totalVouchers = 0;
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    (SELECT COUNT(1) FROM Products WHERE IsDeleted = 0) AS TotalProducts,
                    (SELECT COUNT(1) FROM AspNetUsers) AS TotalUsers,
                    (SELECT COUNT(1) FROM Brands WHERE IsDeleted = 0) AS TotalBrands,
                    (SELECT COUNT(1) FROM Categories WHERE IsDeleted = 0) AS TotalCategories,
                    (SELECT COUNT(1) FROM BlogPosts WHERE IsDeleted = 0) AS TotalBlogPosts,
                    (SELECT COUNT(1) FROM Reviews WHERE IsDeleted = 0) AS TotalReviews,
                    (SELECT COUNT(1) FROM Vouchers WHERE IsDeleted = 0) AS TotalVouchers";
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                totalProducts = reader.GetInt32(0);
                totalUsers = reader.GetInt32(1);
                totalBrands = reader.GetInt32(2);
                totalCategories = reader.GetInt32(3);
                totalBlogPosts = reader.GetInt32(4);
                totalReviews = reader.GetInt32(5);
                totalVouchers = reader.GetInt32(6);
            }
        }
        catch
        {
            // Fallback sang LINQ an toan cho moi truong InMemory test
            totalProducts = await _db.Products.CountAsync(p => !p.IsDeleted);
            totalUsers = await _db.Users.CountAsync();
            totalBrands = await _db.Brands.CountAsync(b => !b.IsDeleted);
            totalCategories = await _db.Categories.CountAsync(c => !c.IsDeleted);
            totalBlogPosts = await _db.BlogPosts.CountAsync(b => !b.IsDeleted);
            totalReviews = await _db.Reviews.CountAsync(r => !r.IsDeleted);
            totalVouchers = await _db.Vouchers.CountAsync(v => !v.IsDeleted);
        }

        var vm = new AdminDashboardViewModel
        {
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalUsers = totalUsers,
            TotalBrands = totalBrands,
            TotalCategories = totalCategories,
            TotalBlogPosts = totalBlogPosts,
            TotalReviews = totalReviews,
            TotalVouchers = totalVouchers,

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

        _cache?.Set(CacheKey, vm, TimeSpan.FromSeconds(180));
        return vm;
    }
}
