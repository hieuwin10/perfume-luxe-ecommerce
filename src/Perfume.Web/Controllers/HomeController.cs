using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;
using Perfume.Web.Models;

namespace Perfume.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;
    private readonly IFlashSaleService _flashSaleService;

    public HomeController(IPerfumeCatalogService catalogService, IBlogService blogService, IFlashSaleService flashSaleService)
    {
        _catalogService = catalogService;
        _blogService = blogService;
        _flashSaleService = flashSaleService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveFlashSale = await _flashSaleService.GetActiveFlashSaleAsync();
        ViewBag.FeaturedProducts = await _catalogService.GetFeaturedProductsAsync();
        ViewBag.BestSellers = await _catalogService.GetBestSellersAsync();
        ViewBag.NewArrivals = await _catalogService.GetNewArrivalsAsync();
        ViewBag.Brands = await _catalogService.GetBrandsAsync();
        ViewBag.Families = await _catalogService.GetFragranceFamiliesAsync();
        ViewBag.RecentBlogs = await _blogService.GetRecentPostsAsync(3);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
