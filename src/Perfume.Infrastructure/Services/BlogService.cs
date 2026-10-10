using Microsoft.EntityFrameworkCore;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class BlogService : IBlogService
{
    private readonly PerfumeDbContext _context;

    public BlogService(PerfumeDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlogPost>> GetRecentPostsAsync(int count = 6)
    {
        return await _context.BlogPosts
            .Include(b => b.Category)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var post = await _context.BlogPosts
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);

        if (post != null)
        {
            post.ViewCount += 1;
            await _context.SaveChangesAsync();
        }

        return post;
    }

    public async Task<List<BlogCategory>> GetBlogCategoriesAsync()
    {
        return await _context.BlogCategories
            .Include(c => c.Posts)
            .ToListAsync();
    }
}
