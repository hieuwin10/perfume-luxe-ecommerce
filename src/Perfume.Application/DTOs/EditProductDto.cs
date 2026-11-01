namespace Perfume.Application.DTOs;

public class EditProductDto : BaseProductDto
{
    public int Id { get; set; }

    public bool IsInStock { get; set; } = true;

    // Danh sách ảnh vệ tinh hiện tại
    public List<ProductImageDto> Images { get; set; } = [];

    // Biến thể hiện tại
    public List<ProductVariantDto> Variants { get; set; } = [];
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}
