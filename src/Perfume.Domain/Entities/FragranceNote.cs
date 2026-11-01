namespace Perfume.Domain.Entities;

public class FragranceNote : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Cam Bergamot, Gỗ tuyết tùng, Vani Bourbon...
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<ProductFragranceNote> ProductNotes { get; set; } = [];
}
