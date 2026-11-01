namespace Perfume.Domain.Entities;

/// <summary>Only a SHA-256 digest of refresh tokens is persisted.</summary>
public sealed class RefreshTokenRecord : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
