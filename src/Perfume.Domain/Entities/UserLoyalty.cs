using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class UserLoyalty : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; } = 0;
    public UserTier Tier { get; set; } = UserTier.Bronze;
    public decimal TotalSpent { get; set; } = 0;
}
