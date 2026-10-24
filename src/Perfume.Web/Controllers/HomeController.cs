using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;
using Perfume.Web.Models;

namespace Perfume.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;
    private readonly IFlashSaleService _flashSaleService;
    private readonly PerfumeDbContext _db;
    private readonly IMemoryCache _cache;

    public HomeController(
        IPerfumeCatalogService catalogService,
        IBlogService blogService,
        IFlashSaleService flashSaleService,
        PerfumeDbContext db,
        IMemoryCache? cache = null)
    {
        _catalogService = catalogService;
        _blogService = blogService;
        _flashSaleService = flashSaleService;
        _db = db;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    }

    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Index()
    {
        var data = await _cache.GetOrCreateAsync("home_composite_cache_v2", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);

            var result = new HomeCompositeCache
            {
                ActiveFlashSale = await _flashSaleService.GetActiveFlashSaleAsync(),
                HeroBanners = await _db.HeroBanners
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ThenByDescending(b => b.CreatedAt)
                    .ToListAsync(),
                FeaturedProducts = await _catalogService.GetFeaturedProductsAsync(),
                BestSellers = await _catalogService.GetBestSellersAsync(),
                NewArrivals = await _catalogService.GetNewArrivalsAsync(),
                Brands = await _catalogService.GetBrandsAsync(),
                Families = await _catalogService.GetFragranceFamiliesAsync(),
                RecentBlogs = await _blogService.GetRecentPostsAsync(3)
            };
            return result;
        });

        ViewBag.ActiveFlashSale = data?.ActiveFlashSale;
        ViewBag.HeroBanners = data?.HeroBanners ?? new List<HeroBanner>();
        ViewBag.FeaturedProducts = data?.FeaturedProducts ?? new List<ProductCardDto>();
        ViewBag.BestSellers = data?.BestSellers ?? new List<ProductCardDto>();
        ViewBag.NewArrivals = data?.NewArrivals ?? new List<ProductCardDto>();
        ViewBag.Brands = data?.Brands ?? new List<Brand>();
        ViewBag.Families = data?.Families ?? new List<FragranceFamily>();
        ViewBag.RecentBlogs = data?.RecentBlogs ?? new List<BlogPost>();

        return View();
    }

    [HttpGet]
    [Route("ve-chung-toi")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    [Route("boutique")]
    public IActionResult Boutique()
    {
        return View();
    }

    [HttpGet]
    [Route("chinh-sach-doi-tra")]
    public IActionResult ReturnPolicy()
    {
        return View();
    }

    [HttpGet]
    [Route("huong-dan-mua-hang")]
    public IActionResult BuyingGuide()
    {
        return View();
    }

    [HttpGet]
    [Route("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    [Route("api/newsletter/subscribe")]
    public IActionResult SubscribeNewsletter([FromBody] NewsletterSubscribeRequest? request)
    {
        var email = request?.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
        {
            return Json(new { success = false, message = "Vui lòng nhập địa chỉ email hợp lệ." });
        }

        return Json(new
        {
            success = true,
            voucher = "LUXE2026",
            discount = "10%",
            message = "Chúc mừng quý khách đã gia nhập câu lạc bộ Haute Parfumerie! Mã ưu đãi 10% [LUXE2026] đã sẵn sàng cho đơn hàng đầu tiên."
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class NewsletterSubscribeRequest
{
    public string? Email { get; set; }
}

public class HomeCompositeCache
{
    public FlashSaleDto? ActiveFlashSale { get; set; }
    public List<HeroBanner> HeroBanners { get; set; } = new();
    public List<ProductCardDto> FeaturedProducts { get; set; } = new();
    public List<ProductCardDto> BestSellers { get; set; } = new();
    public List<ProductCardDto> NewArrivals { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public List<FragranceFamily> Families { get; set; } = new();
    public List<BlogPost> RecentBlogs { get; set; } = new();
}
