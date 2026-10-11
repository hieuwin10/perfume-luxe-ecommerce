using Perfume.Domain.Entities;

namespace Perfume.Application.Interfaces;

public interface IBlogService
{
    Task<List<BlogPost>> GetRecentPostsAsync(int count = 6);
    Task<(List<BlogPost> Posts, int TotalCount)> GetPostsPagedAsync(string? categorySlug = null, int page = 1, int pageSize = 9);
    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost?> GetPostByIdAsync(int id);
    Task<List<BlogCategory>> GetBlogCategoriesAsync();
    Task<List<BlogPost>> GetAllPostsForAdminAsync();
    Task<BlogPost> CreatePostAsync(BlogPost post);
    Task<bool> UpdatePostAsync(BlogPost post);
    Task<bool> DeletePostAsync(int id);
}
