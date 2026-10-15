using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;
using Perfume.Web.Seo;

namespace Perfume.Web.Controllers;

public class BlogController : Controller
{
    private readonly IBlogService _blogService;
    private readonly IConfiguration _configuration;

    public BlogController(IBlogService blogService, IConfiguration? configuration = null)
    {
        _blogService = blogService;
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category, int page = 1)
    {
        if (page < 1) page = 1;
        const int pageSize = 9;

        var (posts, totalCount) = await _blogService.GetPostsPagedAsync(category, page, pageSize);
        var categories = await _blogService.GetBlogCategoriesAsync();

        if (!string.IsNullOrWhiteSpace(category) && !categories.Any(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase)))
        {
            return NotFound();
        }
        if (!string.IsNullOrWhiteSpace(category))
            category = categories.First(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase)).Slug;

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (page > Math.Max(1, totalPages)) return NotFound();

        ViewBag.Categories = categories;
        ViewBag.CurrentCategory = category;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;

        var baseTitle = string.IsNullOrWhiteSpace(category)
            ? "Tạp Chí Mùi Hương & Cẩm Nang Haute Parfumerie"
            : $"{categories.First(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase)).Name} | Tạp Chí Mùi Hương";
        var pageSuffix = page > 1 ? $" - Trang {page}" : string.Empty;
        ViewData["Title"] = baseTitle + pageSuffix;
        ViewData["MetaDescription"] = string.IsNullOrWhiteSpace(category)
            ? "Khám phá nghệ thuật nước hoa, cách chọn hương theo phong cách và những gợi ý tinh tế giúp bạn tìm mùi hương phù hợp."
            : $"Khám phá kiến thức, bí quyết chọn nước hoa và gợi ý sản phẩm trong chủ đề {categories.First(c => c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase)).Name}.";
        var canonicalPath = "/blog";
        var query = new List<string>();
        if (page > 1) query.Add($"page={page}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (query.Count > 0) canonicalPath += "?" + string.Join("&", query);
        ViewData["CanonicalUrl"] = SeoUrlBuilder.AbsoluteUrl(ControllerContext.HttpContext?.Request, _configuration, canonicalPath);
        ViewData["SeoSiteBaseUrl"] = SeoUrlBuilder.GetSiteBaseUrl(ControllerContext.HttpContext?.Request, _configuration);
        ViewData["OgType"] = "website";

        // Nếu ở trang 1 và không lọc category, lấy bài viết nổi bật đầu tiên làm Hero Article
        if (page == 1 && string.IsNullOrWhiteSpace(category) && posts.Any())
        {
            ViewBag.HeroPost = posts.First();
        }

        return View(posts);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await _blogService.GetPostBySlugAsync(slug);
        if (post == null) return NotFound();

        var recentPosts = await _blogService.GetRecentPostsAsync(6);
        ViewBag.RecentPosts = recentPosts.Where(p => p.Id != post.Id).Take(3).ToList();

        ViewData["Title"] = string.IsNullOrWhiteSpace(post.MetaTitle) ? post.Title : post.MetaTitle.Trim();
        ViewData["MetaDescription"] = string.IsNullOrWhiteSpace(post.MetaDescription) ? post.Summary : post.MetaDescription.Trim();
        ViewData["CanonicalUrl"] = SeoUrlBuilder.AbsoluteUrl(ControllerContext.HttpContext?.Request, _configuration, $"/blog/{Uri.EscapeDataString(post.Slug)}");
        ViewData["SeoSiteBaseUrl"] = SeoUrlBuilder.GetSiteBaseUrl(ControllerContext.HttpContext?.Request, _configuration);
        ViewData["OgType"] = "article";
        ViewData["OgImage"] = SeoUrlBuilder.AbsoluteUrl(Request, _configuration, post.ThumbnailUrl ?? "/images/perfume-luxe-og.jpg");

        return View(post);
    }
}
