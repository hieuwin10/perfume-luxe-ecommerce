namespace Perfume.Domain.Entities;

public class Review : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string? UserId { get; set; }
    public string UserName { get; set; } = "Khách hàng";
    public int Rating { get; set; } = 5; // 1 to 5
    public string Comment { get; set; } = string.Empty;
    public string? LongevityRating { get; set; } // "Rất lâu (trên 8h)", "Tốt (6-8h)"
    public bool IsApproved { get; set; } = false;
    public string? ImageUrls { get; set; }
}
