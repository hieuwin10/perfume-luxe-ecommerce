using Microsoft.AspNetCore.Identity;
using Perfume.Domain.Enums;

namespace Perfume.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public UserTier Tier { get; set; } = UserTier.Bronze;
    public int LoyaltyPoints { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppRole : IdentityRole
{
    public string? Description { get; set; }
}
