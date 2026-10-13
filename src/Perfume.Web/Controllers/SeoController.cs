using System.Text;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Controllers;

public class SeoController : Controller
{
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;

    public SeoController(IPerfumeCatalogService catalogService, IBlogService blogService)
    {
        _catalogService = catalogService;
        _blogService = blogService;
    }

    [HttpGet]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> Sitemap()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var baseUrl = $"{scheme}://{host}";

        var (products, _) = await _catalogService.GetProductsAsync(new ProductFilterParams { PageSize = 1000 });
        var posts = await _blogService.GetRecentPostsAsync(100);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static routes
        AddUrl(sb, baseUrl, "daily", "1.0");
        AddUrl(sb, $"{baseUrl}/bo-suu-tap", "daily", "0.9");
        AddUrl(sb, $"{baseUrl}/bo-suu-tap?gender=Men", "weekly", "0.85");
        AddUrl(sb, $"{baseUrl}/bo-suu-tap?gender=Women", "weekly", "0.85");
        AddUrl(sb, $"{baseUrl}/blog", "weekly", "0.8");
        AddUrl(sb, $"{baseUrl}/catalog/quiz", "monthly", "0.7");

        // Products
        foreach (var p in products)
        {
            AddUrl(sb, $"{baseUrl}/nuoc-hoa/{p.Slug}", "weekly", "0.9");
        }

        // Blogs
        foreach (var b in posts)
        {
            AddUrl(sb, $"{baseUrl}/blog/{b.Slug}", "monthly", "0.8");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet]
    [ResponseCache(Duration = 86400)]
    public IActionResult Robots()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var baseUrl = $"{scheme}://{host}";

        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /cart/");
        sb.AppendLine("Disallow: /checkout/");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine("Allow: /");
        sb.AppendLine();
        sb.AppendLine("# AI & Answer Engine Crawlers (AEO / GEO)");
        sb.AppendLine("User-agent: GPTBot");
        sb.AppendLine("Allow: /");
        sb.AppendLine("User-agent: PerplexityBot");
        sb.AppendLine("Allow: /");
        sb.AppendLine("User-agent: ClaudeBot");
        sb.AppendLine("Allow: /");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    private static void AddUrl(StringBuilder sb, string url, string changeFreq, string priority)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{url}</loc>");
        sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
        sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }
}
