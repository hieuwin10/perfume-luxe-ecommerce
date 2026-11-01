namespace Perfume.Domain.Entities;

public class BackInStockAlert : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? VariantId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public bool IsNotified { get; set; } = false;
    public DateTime? NotifiedAt { get; set; }
}
