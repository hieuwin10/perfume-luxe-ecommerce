namespace Perfume.Domain.Entities;

/// <summary>A server-owned cart for either an authenticated customer or an anonymous browser.</summary>
public sealed class ShoppingCart : BaseEntity
{
    public string OwnerKey { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
}

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

/// <summary>Only a SHA-256 digest of refresh tokens is persisted.</summary>
public sealed class RefreshTokenRecord : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
