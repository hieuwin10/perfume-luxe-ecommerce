using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;
using Perfume.Web.Models;

namespace Perfume.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;
    private readonly PerfumeDbContext _db;

    public HomeController(IPerfumeCatalogService catalogService, IBlogService blogService, PerfumeDbContext db)
    {
        _catalogService = catalogService;
        _blogService = blogService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.HeroBanners = await _db.HeroBanners
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        ViewBag.FeaturedProducts = await _catalogService.GetFeaturedProductsAsync();
        ViewBag.BestSellers = await _catalogService.GetBestSellersAsync();
        ViewBag.NewArrivals = await _catalogService.GetNewArrivalsAsync();
        ViewBag.Brands = await _catalogService.GetBrandsAsync();
        ViewBag.Families = await _catalogService.GetFragranceFamiliesAsync();
        ViewBag.RecentBlogs = await _blogService.GetRecentPostsAsync(3);

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
