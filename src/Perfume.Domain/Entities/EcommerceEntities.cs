using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty; // PF-20261010-XXXX
    public string? UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? OrderNotes { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal LoyaltyDiscountAmount { get; set; } = 0;
    public UserTier? AppliedLoyaltyTier { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    public string? VoucherCode { get; set; }
    public string? TrackingNumber { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }

    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice => UnitPrice * Quantity;

    public bool IsCustomBox { get; set; } = false;
    public string? CustomBoxConfigJson { get; set; }
}

public class Voucher : BaseEntity
{
    public string Code { get; set; } = string.Empty; // LUXE20, NOEL2026...
    public string Description { get; set; } = string.Empty;
    public decimal? DiscountPercent { get; set; } // e.g. 10%
    public decimal? DiscountAmount { get; set; } // e.g. 100.000₫
    public decimal MinOrderAmount { get; set; } = 0;
    public decimal? MaxDiscountAmount { get; set; }
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; } = 0;
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
    public bool IsActive { get; set; } = true;
}

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

public class BlogCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string AuthorName { get; set; } = "Perfume Master";
    public int ViewCount { get; set; } = 0;
    public bool IsPublished { get; set; } = true;

    public int? CategoryId { get; set; }
    public BlogCategory? Category { get; set; }

    // SEO Optimization
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class UserLoyalty : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; } = 0;
    public UserTier Tier { get; set; } = UserTier.Bronze;
    public decimal TotalSpent { get; set; } = 0;
}

public class FlashSale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BannerUrl { get; set; }

    public ICollection<FlashSaleItem> Items { get; set; } = new List<FlashSaleItem>();
}

public class FlashSaleItem : BaseEntity
{
    public int FlashSaleId { get; set; }
    public FlashSale? FlashSale { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public decimal FlashSalePrice { get; set; }
    public int StockLimit { get; set; }
    public int SoldQuantity { get; set; } = 0;
}
