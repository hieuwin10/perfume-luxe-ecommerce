namespace Perfume.Domain.Entities;

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
