namespace Perfume.Domain.Entities;

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string AuthorName { get; set; } = "Perfume Master";
    public int ViewCount { get; set; } = 0;
    public bool IsPublished { get; set; } = true;

    public int? CategoryId { get; set; }
    public BlogCategory? Category { get; set; }

    // SEO Optimization
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
