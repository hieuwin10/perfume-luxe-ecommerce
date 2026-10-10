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
    public async Task<IActionResult> Index()
    {
        var posts = await _blogService.GetRecentPostsAsync(12);
        var categories = await _blogService.GetBlogCategoriesAsync();
        ViewBag.Categories = categories;
        return View(posts);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await _blogService.GetPostBySlugAsync(slug);
        if (post == null) return NotFound();

        var recentPosts = await _blogService.GetRecentPostsAsync(4);
        ViewBag.RecentPosts = recentPosts.Where(p => p.Id != post.Id).Take(3).ToList();

        return View(post);
    }
}
