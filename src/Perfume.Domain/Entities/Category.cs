using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public GenderTarget? Gender { get; set; }
    public bool IsFeatured { get; set; } = false;

    public ICollection<Product> Products { get; set; } = [];
}
