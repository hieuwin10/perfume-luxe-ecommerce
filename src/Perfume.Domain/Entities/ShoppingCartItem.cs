namespace Perfume.Domain.Entities;

public sealed class ShoppingCartItem : BaseEntity
{
    public int ShoppingCartId { get; set; }
    public ShoppingCart? ShoppingCart { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
    public bool IsCustomBox { get; set; } = false;
    public string? CustomBoxConfigJson { get; set; }
    public decimal? CustomUnitPrice { get; set; }
}
