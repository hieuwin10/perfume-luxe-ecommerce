using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Perfume.Application.Interfaces;
using Perfume.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Perfume.Web.Models;
using Perfume.Web.Controllers;

namespace Perfume.Web.Services;

/// <summary>
/// Dịch vụ chạy nền định kỳ làm mới cache bộ nhớ (In-Memory Cache Warmer)
/// cho Trang chủ và Admin Dashboard. Giúp người dùng và quản trị viên luôn truy cập tức thì (< 200ms)
/// mà không bao giờ bị dính độ trễ cold-start của CSDL từ xa.
/// </summary>
public sealed class BackgroundCacheWarmerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundCacheWarmerService> _logger;

    public BackgroundCacheWarmerService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundCacheWarmerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ 3 giây lúc khởi động để toàn bộ dịch vụ Kestrel và DB Migration sẵn sàng
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cache = scope.ServiceProvider.GetService<IMemoryCache>();
                if (cache != null)
                {
                    // 1. Warm Dashboard Analytics Cache
                    var dashboardService = scope.ServiceProvider.GetService<IAdminDashboardAnalyticsService>();
                    if (dashboardService != null)
                    {
                        await dashboardService.BuildDashboardAsync();
                    }

                    // 2. Warm Homepage Composite Cache
                    var db = scope.ServiceProvider.GetService<PerfumeDbContext>();
                    var catalog = scope.ServiceProvider.GetService<IPerfumeCatalogService>();
                    var flashSale = scope.ServiceProvider.GetService<IFlashSaleService>();
                    var blog = scope.ServiceProvider.GetService<IBlogService>();

                    if (db != null && catalog != null && flashSale != null && blog != null)
                    {
                        var homeData = new HomeCompositeCache
                        {
                            ActiveFlashSale = await flashSale.GetActiveFlashSaleAsync(),
                            HeroBanners = await db.HeroBanners
                                .AsNoTracking()
                                .Where(b => b.IsActive)
                                .OrderBy(b => b.DisplayOrder)
                                .ThenByDescending(b => b.CreatedAt)
                                .ToListAsync(stoppingToken),
                            FeaturedProducts = await catalog.GetFeaturedProductsAsync(),
                            BestSellers = await catalog.GetBestSellersAsync(),
                            NewArrivals = await catalog.GetNewArrivalsAsync(),
                            Brands = await catalog.GetBrandsAsync(),
                            Families = await catalog.GetFragranceFamiliesAsync(),
                            RecentBlogs = await blog.GetRecentPostsAsync(3)
                        };

                        cache.Set("home_composite_cache_v2", homeData, TimeSpan.FromMinutes(30));
                    }
                }
            }
            catch (Exception ex)
            {
                // Không để lỗi nền làm gián đoạn ứng dụng
                _logger.LogWarning(ex, "BackgroundCacheWarmerService: Lỗi làm mới cache định kỳ.");
            }

            // Lặp lại mỗi 90 giây
            await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken);
        }
    }
}
