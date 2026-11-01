namespace Perfume.Domain.Entities;

public class FlashSale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BannerUrl { get; set; }

    public ICollection<FlashSaleItem> Items { get; set; } = [];
}
