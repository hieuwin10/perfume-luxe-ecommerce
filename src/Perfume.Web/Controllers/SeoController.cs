using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Web.Seo;

namespace Perfume.Web.Controllers;

public class SeoController : Controller
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;
    private readonly IConfiguration _configuration;

    public SeoController(IPerfumeCatalogService catalogService, IBlogService blogService, IConfiguration? configuration = null)
    {
        _catalogService = catalogService;
        _blogService = blogService;
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken = default)
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var entries = new List<(string Url, DateTime? LastModified)>();

        Add("/");
        Add("/bo-suu-tap");
        Add("/blog");
        Add("/catalog/quiz");

        const int pageSize = 1000;
        var productPage = 1;
        while (true)
        {
            var (products, total) = await _catalogService.GetProductsAsync(new ProductFilterParams
            {
                Page = productPage,
                PageSize = pageSize,
                SortBy = "newest"
            });

            foreach (var product in products)
                Add($"/nuoc-hoa/{Uri.EscapeDataString(product.Slug)}", product.UpdatedAt ?? product.CreatedAt);

            if (products.Count == 0 || productPage * pageSize >= total) break;
            productPage++;
        }

        var categories = await _blogService.GetBlogCategoriesAsync();
        foreach (var category in categories)
        {
            var (_, categoryPostCount) = await _blogService.GetPostsPagedAsync(category.Slug, 1, pageSize);
            if (categoryPostCount == 0) continue;
            var categoryQuery = $"category={Uri.EscapeDataString(category.Slug)}";
            Add($"/blog?{categoryQuery}");
            var categoryPages = (int)Math.Ceiling(categoryPostCount / (double)pageSize);
            for (var categoryPage = 2; categoryPage <= categoryPages; categoryPage++)
                Add($"/blog?page={categoryPage}&{categoryQuery}");
        }

        var (firstPosts, totalPosts) = await _blogService.GetPostsPagedAsync(null, 1, pageSize);
        AddPosts(firstPosts);
        var totalBlogPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
        for (var blogPage = 2; blogPage <= totalBlogPages; blogPage++)
        {
            Add($"/blog?page={blogPage}");
            var (posts, _) = await _blogService.GetPostsPagedAsync(null, blogPage, pageSize);
            AddPosts(posts);
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset",
                entries.Select(entry => new XElement(SitemapNamespace + "url",
                    new XElement(SitemapNamespace + "loc", entry.Url),
                    entry.LastModified.HasValue
                        ? new XElement(SitemapNamespace + "lastmod", DateTime.SpecifyKind(entry.LastModified.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        : null))));

        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);

        void Add(string path, DateTime? lastModified = null) => entries.Add(($"{baseUrl}{path}", lastModified));
        void AddPosts(IEnumerable<Perfume.Domain.Entities.BlogPost> posts)
        {
            foreach (var post in posts)
                Add($"/blog/{Uri.EscapeDataString(post.Slug)}", post.UpdatedAt ?? post.CreatedAt);
        }
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var content = string.Join('\n', new[]
        {
            "User-agent: *",
            "Disallow: /admin/",
            "Disallow: /account/",
            "Disallow: /cart/",
            "Disallow: /checkout/",
            "Disallow: /api/",
            "Disallow: /tra-cuu-don-hang",
            "Disallow: /order-tracking",
            "Allow: /",
            string.Empty,
            "User-agent: GPTBot",
            "Allow: /",
            "User-agent: PerplexityBot",
            "Allow: /",
            "User-agent: ClaudeBot",
            "Allow: /",
            string.Empty,
            $"Sitemap: {baseUrl}/sitemap.xml",
            string.Empty
        });

        return Content(content, "text/plain", Encoding.UTF8);
    }
}
