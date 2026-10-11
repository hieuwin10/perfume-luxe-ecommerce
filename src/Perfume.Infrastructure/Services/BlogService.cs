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
            .Where(b => b.IsPublished && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(List<BlogPost> Posts, int TotalCount)> GetPostsPagedAsync(string? categorySlug = null, int page = 1, int pageSize = 9)
    {
        var query = _context.BlogPosts
            .Include(b => b.Category)
            .Where(b => b.IsPublished && !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(b => b.Category != null && b.Category.Slug == categorySlug);
        }

        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (posts, totalCount);
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var post = await _context.BlogPosts
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished && !b.IsDeleted);

        if (post != null)
        {
            post.ViewCount += 1;
            await _context.SaveChangesAsync();
        }

        return post;
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return await _context.BlogPosts
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<List<BlogCategory>> GetBlogCategoriesAsync()
    {
        return await _context.BlogCategories
            .Where(c => !c.IsDeleted)
            .Include(c => c.Posts.Where(p => p.IsPublished && !p.IsDeleted))
            .ToListAsync();
    }

    public async Task<List<BlogPost>> GetAllPostsForAdminAsync()
    {
        return await _context.BlogPosts
            .Include(b => b.Category)
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.IsDeleted = false;
        if (string.IsNullOrWhiteSpace(post.Slug))
        {
            post.Slug = GenerateSlug(post.Title);
        }

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<bool> UpdatePostAsync(BlogPost post)
    {
        var existing = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == post.Id && !b.IsDeleted);
        if (existing == null) return false;

        existing.Title = post.Title;
        existing.Slug = string.IsNullOrWhiteSpace(post.Slug) ? GenerateSlug(post.Title) : post.Slug;
        existing.Summary = post.Summary;
        existing.Content = post.Content;
        existing.ThumbnailUrl = post.ThumbnailUrl;
        existing.AuthorName = post.AuthorName;
        existing.CategoryId = post.CategoryId;
        existing.IsPublished = post.IsPublished;
        existing.MetaTitle = post.MetaTitle;
        existing.MetaDescription = post.MetaDescription;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        var post = await _context.BlogPosts.FirstOrDefaultAsync(b => b.Id == id);
        if (post == null) return false;

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];
        var unaccented = text.ToLowerInvariant();
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[àáạảãâầấậẩẫăằắặẳẵ]", "a");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[èéẹẻẽêềếệểễ]", "e");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[ìíịỉĩ]", "i");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[òóọỏõôồốộổỗơờớợởỡ]", "o");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[ùúụủũưừứựửữ]", "u");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[ỳýỵỷỹ]", "y");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[đ]", "d");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"[^a-z0-9\s-]", "");
        unaccented = System.Text.RegularExpressions.Regex.Replace(unaccented, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(unaccented) ? Guid.NewGuid().ToString("N")[..8] : unaccented;
    }
}
