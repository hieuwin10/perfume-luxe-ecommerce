using System.ComponentModel.DataAnnotations;
using Perfume.Domain.Enums;

namespace Perfume.Application.DTOs;

public class EditProductDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã SKU sản phẩm")]
    [MaxLength(80)]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    public int? FragranceFamilyId { get; set; }

    public GenderTarget Gender { get; set; } = GenderTarget.Unisex;
    public Concentration Concentration { get; set; } = Concentration.EDP;

    [Range(0, 1000000000, ErrorMessage = "Giá sản phẩm không hợp lệ")]
    public decimal BasePrice { get; set; }

    [MaxLength(1000)]
    public string? MainImageUrl { get; set; }

    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    public string? Longevity { get; set; } = "7 - 12 giờ";
    public string? Sillage { get; set; } = "Toả hương trong bán kính 1 - 2 mét";
    public int? ReleaseYear { get; set; }
    public string? SeasonRecommendation { get; set; } = "Bốn mùa";

    // 3 Tầng hương
    public string? TopNotes { get; set; }
    public string? HeartNotes { get; set; }
    public string? BaseNotes { get; set; }

    public bool IsInStock { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsBestSeller { get; set; } = false;
    public bool IsNewArrival { get; set; } = false;

    // Danh sách ảnh vệ tinh hiện tại
    public List<ProductImageDto> Images { get; set; } = new();

    // Biến thể hiện tại
    public List<ProductVariantDto> Variants { get; set; } = new();
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}

