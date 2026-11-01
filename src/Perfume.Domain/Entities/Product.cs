using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    public int BrandId { get; set; }
    public Brand? Brand { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? FragranceFamilyId { get; set; }
    public FragranceFamily? FragranceFamily { get; set; }

    public GenderTarget Gender { get; set; } = GenderTarget.Unisex;
    public Concentration Concentration { get; set; } = Concentration.EDP;
    
    public string? Longevity { get; set; } = "7 - 12 giờ";
    public string? Sillage { get; set; } = "Toả hương trong bán kính 1 - 2 mét";
    public int? ReleaseYear { get; set; }
    public string? SeasonRecommendation { get; set; } = "Bốn mùa, Ngày và Đêm";

    public decimal BasePrice { get; set; }
    public string? MainImageUrl { get; set; }

    public bool IsFeatured { get; set; } = false;
    public bool IsBestSeller { get; set; } = false;
    public bool IsNewArrival { get; set; } = false;
    public bool IsInStock { get; set; } = true;

    public double RatingAverage { get; set; } = 5.0;
    public int RatingCount { get; set; } = 0;
    public int ViewCount { get; set; } = 0;

    // SEO Optimization Fields
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductFragranceNote> FragranceNotes { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
