namespace Perfume.Domain.Entities;

public class StockAdjustment : BaseEntity
{
    public int VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public int OldQuantity { get; set; }
    public int AdjustedQuantity { get; set; } // +10 or -2
    public int NewQuantity { get; set; }

    public decimal? UnitCost { get; set; } // Giá vốn nhập kho
    public string Reason { get; set; } = "Nhập hàng mới"; // Nhập hàng mới, Trả hàng, Kiểm kê sai lệch...
    public string? Note { get; set; }
    public string? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
}
