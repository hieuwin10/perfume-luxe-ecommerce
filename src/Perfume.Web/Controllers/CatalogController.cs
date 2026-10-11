using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Perfume.Web.Controllers;

public class CatalogController : Controller
{
    private readonly IPerfumeCatalogService _catalogService;
    private readonly PerfumeDbContext _db;

    public CatalogController(IPerfumeCatalogService catalogService, PerfumeDbContext db)
    {
        _catalogService = catalogService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductFilterParams filter)
    {
        var (items, totalCount) = await _catalogService.GetProductsAsync(filter);
        var brands = await _catalogService.GetBrandsAsync();
        var categories = await _catalogService.GetCategoriesAsync();
        var families = await _catalogService.GetFragranceFamiliesAsync();
        var notes = await _catalogService.GetFragranceNotesAsync();

        ViewBag.Filter = filter;
        ViewBag.TotalCount = totalCount;
        ViewBag.Brands = brands;
        ViewBag.Categories = categories;
        ViewBag.Families = families;
        ViewBag.Notes = notes;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var product = await _catalogService.GetProductBySlugAsync(slug);
        if (product == null) return NotFound();

        // Get related products in the same family or brand
        var relatedFilter = new ProductFilterParams { FamilySlug = product.FamilyName, PageSize = 4 };
        var (relatedItems, _) = await _catalogService.GetProductsAsync(relatedFilter);
        ViewBag.RelatedProducts = relatedItems.Where(p => p.Id != product.Id).Take(4).ToList();
        ViewBag.ApprovedReviews = await _db.Reviews.Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync();

        return View(product);
    }

    [HttpGet]
    public IActionResult Quiz()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> QuizSubmit([FromBody] QuizAnswerRequest request)
    {
        var result = await _catalogService.GetQuizRecommendationsAsync(request);
        return Json(new { success = true, result, items = result.Items });
    }
}
