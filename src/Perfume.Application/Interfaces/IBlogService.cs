using Perfume.Domain.Entities;

namespace Perfume.Application.Interfaces;

public interface IBlogService
{
    Task<List<BlogPost>> GetRecentPostsAsync(int count = 6);
    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<List<BlogCategory>> GetBlogCategoriesAsync();
}
