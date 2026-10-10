using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public bool IsFeatured { get; set; } = false;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public GenderTarget? Gender { get; set; }
    public bool IsFeatured { get; set; } = false;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class FragranceFamily : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Gỗ trầm ấm, Hoa cỏ thanh lịch, Phương đông quyến rũ...
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorHex { get; set; } = "#D4AF37"; // Mã màu đại diện
    public ScentFamily ScentType { get; set; } = ScentFamily.Woody;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class FragranceNote : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Cam Bergamot, Gỗ tuyết tùng, Vani Bourbon...
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<ProductFragranceNote> ProductNotes { get; set; } = new List<ProductFragranceNote>();
}

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

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductFragranceNote> FragranceNotes { get; set; } = new List<ProductFragranceNote>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int VolumeMl { get; set; } // 2, 10, 50, 100, 200
    public string VolumeLabel { get; set; } = "Chai Full 100ml";
    public string VariantSku { get; set; } = string.Empty;

    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; } = 10;
    public bool IsDefault { get; set; } = false;
}

public class ProductFragranceNote
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int FragranceNoteId { get; set; }
    public FragranceNote? FragranceNote { get; set; }

    public NoteLevel NoteLevel { get; set; } = NoteLevel.Top; // Top / Heart / Base
    public int DisplayOrder { get; set; } = 0;
}

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsMain { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}
