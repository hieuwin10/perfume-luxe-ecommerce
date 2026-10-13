using System.ComponentModel.DataAnnotations;

namespace Perfume.Application.DTOs;

public class FlashSaleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsOngoing => IsActive && StartTime <= DateTime.UtcNow && EndTime > DateTime.UtcNow;
    public string? BannerUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FlashSaleItemDto> Items { get; set; } = new();
}

public class FlashSaleItemDto
{
    public int Id { get; set; }
    public int FlashSaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? BrandName { get; set; }
    public int? VariantId { get; set; }
    public string? VariantLabel { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal FlashSalePrice { get; set; }
    public int StockLimit { get; set; }
    public int SoldQuantity { get; set; }
    public int RemainingQuantity => Math.Max(0, StockLimit - SoldQuantity);
    public int DiscountPercent => OriginalPrice > 0 ? (int)Math.Round((1m - (FlashSalePrice / OriginalPrice)) * 100m) : 0;
}

public class CreateFlashSaleRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên chiến dịch Flash Sale")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? BannerUrl { get; set; }

    public List<CreateFlashSaleItemRequest> Items { get; set; } = new();
}

public class CreateFlashSaleItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
    public int ProductId { get; set; }

    public int? VariantId { get; set; }

    [Range(1000, 1000000000, ErrorMessage = "Giá Flash Sale phải lớn hơn 1.000₫")]
    public decimal FlashSalePrice { get; set; }

    [Range(1, 10000, ErrorMessage = "Số lượng giới hạn từ 1 đến 10.000")]
    public int StockLimit { get; set; } = 10;
}
