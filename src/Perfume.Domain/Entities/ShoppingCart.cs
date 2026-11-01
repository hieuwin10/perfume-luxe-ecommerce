namespace Perfume.Domain.Entities;

/// <summary>A server-owned cart for either an authenticated customer or an anonymous browser.</summary>
public sealed class ShoppingCart : BaseEntity
{
    public string OwnerKey { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ICollection<ShoppingCartItem> Items { get; set; } = [];
}
