namespace Perfume.Domain.Entities;

public class HeroBanner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? TargetUrl { get; set; } = "/bo-suu-tap";
    public string? ButtonText { get; set; } = "Khám Phá Ngay";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
