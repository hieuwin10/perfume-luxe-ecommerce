using System.ComponentModel.DataAnnotations;
using Perfume.Domain.Enums;

namespace Perfume.Application.DTOs;

public class ProductFilterParams
{
    public string? Keyword { get; set; }
    public string? BrandSlug { get; set; }
    public string? CategorySlug { get; set; }
    public string? FamilySlug { get; set; }
    public int? NoteId { get; set; }
    public GenderTarget? Gender { get; set; }
    public Concentration? Concentration { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } // price_asc, price_desc, rating, newest
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class ProductCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? BrandCountry { get; set; }
    public string? MainImageUrl { get; set; }
    public decimal BasePrice { get; set; }
    public string? FamilyName { get; set; }
    public string? FamilyColor { get; set; }
    public Concentration Concentration { get; set; }
    public GenderTarget Gender { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsNewArrival { get; set; }
    public bool IsBestSeller { get; set; }
    public List<string> TopNotes { get; set; } = new();
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string BrandSlug { get; set; } = string.Empty;
    public string? BrandCountry { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? FamilyName { get; set; }
    public string? FamilyColor { get; set; }
    public Concentration Concentration { get; set; }
    public GenderTarget Gender { get; set; }
    public string? Longevity { get; set; }
    public string? Sillage { get; set; }
    public int? ReleaseYear { get; set; }
    public string? SeasonRecommendation { get; set; }
    public decimal BasePrice { get; set; }
    public string? MainImageUrl { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsInStock { get; set; }

    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    // Pyramid Scent Notes
    public List<FragranceNoteDto> TopNotes { get; set; } = new();
    public List<FragranceNoteDto> HeartNotes { get; set; } = new();
    public List<FragranceNoteDto> BaseNotes { get; set; } = new();

    // Variants
    public List<ProductVariantDto> Variants { get; set; } = new();

    // Images
    public List<string> GalleryImages { get; set; } = new();
}

public class FragranceNoteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ProductVariantDto
{
    public int Id { get; set; }
    public int VolumeMl { get; set; }
    public string VolumeLabel { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsDefault { get; set; }
}

public class CartItemDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string VariantLabel { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal TotalPrice => UnitPrice * Quantity;
}

public class CheckoutRequestDto
{
    [Required, StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;
    [Required, Phone, StringLength(32)]
    public string CustomerPhone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    public string CustomerEmail { get; set; } = string.Empty;
    [Required, StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? OrderNotes { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public string? VoucherCode { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
}

public class QuizAnswerRequest
{
    public string GenderPreference { get; set; } = "unisex"; // men, women, unisex
    public string Vibe { get; set; } = "luxury"; // luxury, fresh, romantic, cozy, mysterious
    public string SeasonOrOccasion { get; set; } = "all"; // evening, daily, all, special
    public string IntensityPreference { get; set; } = "strong"; // moderate, strong, intense
    public string BudgetPreference { get; set; } = "mid"; // sample, mid, high
}

public class QuizProductRecommendationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string? BrandCountry { get; set; }
    public string? MainImageUrl { get; set; }
    public decimal BasePrice { get; set; }
    public string? FamilyName { get; set; }
    public string? FamilyColor { get; set; }
    public Concentration Concentration { get; set; }
    public GenderTarget Gender { get; set; }
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public string? Longevity { get; set; }
    public string? Sillage { get; set; }
    public int MatchScore { get; set; } // e.g. 98 (%)
    public string MatchReason { get; set; } = string.Empty;
    public List<string> TopNotes { get; set; } = new();
}

public class QuizRecommendationResultDto
{
    public string ArchetypeTitle { get; set; } = string.Empty;
    public string ArchetypeSubtitle { get; set; } = string.Empty;
    public string ArchetypeDescription { get; set; } = string.Empty;
    public List<string> DominantAccords { get; set; } = new();
    public List<string> KeyNotes { get; set; } = new();
    public string BestOccasions { get; set; } = string.Empty;
    public List<QuizProductRecommendationDto> Items { get; set; } = new();
}

