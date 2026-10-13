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

    [HttpGet]
    [Route("so-sanh")]
    [Route("catalog/compare")]
    public async Task<IActionResult> Compare(int? id1, int? id2, int? id3)
    {
        var allProducts = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .OrderBy(p => p.Name)
            .Select(p => new CompareProductOptionDto
            {
                Id = p.Id,
                Name = p.Name,
                BrandName = p.Brand != null ? p.Brand.Name : ""
            })
            .ToListAsync();

        ViewBag.AllProducts = allProducts;

        var selectedIds = new List<int>();
        if (id1.HasValue && id1.Value > 0) selectedIds.Add(id1.Value);
        if (id2.HasValue && id2.Value > 0) selectedIds.Add(id2.Value);
        if (id3.HasValue && id3.Value > 0) selectedIds.Add(id3.Value);

        if (!selectedIds.Any() && allProducts.Count >= 2)
        {
            selectedIds.Add(allProducts[0].Id);
            selectedIds.Add(allProducts[1].Id);
        }

        var productsToCompare = new List<ProductDetailDto>();
        foreach (var id in selectedIds.Distinct())
        {
            var p = await _catalogService.GetProductByIdAsync(id);
            if (p != null) productsToCompare.Add(p);
        }

        return View(productsToCompare);
    }

    [HttpGet]
    [Route("discovery-box")]
    [Route("hop-kham-pha")]
    public async Task<IActionResult> DiscoveryBox()
    {
        var availablePerfumes = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.FragranceFamily)
            .Where(p => !p.IsDeleted && p.IsInStock)
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.Name)
            .Select(p => new DiscoveryPerfumeItemDto
            {
                Id = p.Id,
                Name = p.Name,
                BrandName = p.Brand != null ? p.Brand.Name : "",
                MainImageUrl = p.MainImageUrl,
                FamilyName = p.FragranceFamily != null ? p.FragranceFamily.Name : "Đa dạng",
                FamilyColor = p.FragranceFamily != null ? p.FragranceFamily.ColorHex : "#D4AF37",
                Gender = p.Gender.ToString()
            })
            .ToListAsync();

        return View(availablePerfumes);
    }
}

public class CompareProductOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
}

public class DiscoveryPerfumeItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string? FamilyColor { get; set; } = "#D4AF37";
    public string Gender { get; set; } = "Unisex";
}
