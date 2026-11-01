using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class FragranceFamily : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Gỗ trầm ấm, Hoa cỏ thanh lịch, Phương đông quyến rũ...
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorHex { get; set; } = "#D4AF37"; // Mã màu đại diện
    public ScentFamily ScentType { get; set; } = ScentFamily.Woody;

    public ICollection<Product> Products { get; set; } = [];
}
