using System.ComponentModel.DataAnnotations;
using Perfume.Domain.Enums;

namespace Perfume.Application.DTOs;

public class CreateProductDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

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
    public int? ReleaseYear { get; set; } = DateTime.UtcNow.Year;
    public string? SeasonRecommendation { get; set; } = "Bốn mùa, Ngày và Đêm";

    // 3 tầng hương phân cách bằng dấu phẩy
    public string? TopNotes { get; set; }
    public string? HeartNotes { get; set; }
    public string? BaseNotes { get; set; }

    // Biến thể mặc định đầu tiên
    public int InitialVolumeMl { get; set; } = 100;
    public string InitialVolumeLabel { get; set; } = "Chai Full 100ml";
    public int InitialStockQuantity { get; set; } = 20;

    public bool IsFeatured { get; set; } = false;
    public bool IsBestSeller { get; set; } = false;
    public bool IsNewArrival { get; set; } = true;
}
