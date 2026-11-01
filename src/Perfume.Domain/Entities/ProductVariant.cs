namespace Perfume.Domain.Entities;

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
