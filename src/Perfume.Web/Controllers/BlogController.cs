using Microsoft.AspNetCore.Mvc;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Controllers;

public class BlogController : Controller
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? category, int page = 1)
    {
        if (page < 1) page = 1;
        const int pageSize = 9;

        var (posts, totalCount) = await _blogService.GetPostsPagedAsync(category, page, pageSize);
        var categories = await _blogService.GetBlogCategoriesAsync();

        ViewBag.Categories = categories;
        ViewBag.CurrentCategory = category;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.TotalCount = totalCount;

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

        return View(post);
    }
}
