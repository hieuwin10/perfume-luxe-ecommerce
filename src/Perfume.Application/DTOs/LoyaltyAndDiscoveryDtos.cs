using System.ComponentModel.DataAnnotations;
using Perfume.Domain.Enums;

namespace Perfume.Application.DTOs;

// ═══════════════════════════════════════════════════════════════════════════
// PART 3: CUSTOMER ACCOUNT & AUTOMATIC VIP LOYALTY TIER ENGINE DTOs
// ═══════════════════════════════════════════════════════════════════════════

public sealed class LoyaltyStatusDto
{
    public string UserId { get; set; } = string.Empty;
    public UserTier CurrentTier { get; set; } = UserTier.Bronze;
    public string TierName { get; set; } = "Bronze";
    public UserTier? NextTier { get; set; } = UserTier.Silver;
    public string? NextTierName { get; set; } = "Silver";
    public decimal TotalSpent { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal ProgressPercent { get; set; }
    public decimal AmountToNextTier { get; set; }
    public int QualifyingOrderCount { get; set; }
}

public sealed class OrderVipDiscountPreviewDto
{
    public string? UserId { get; set; }
    public UserTier AppliedTier { get; set; } = UserTier.Bronze;
    public decimal VipDiscountPercent { get; set; }
    public decimal SubTotal { get; set; }
    public decimal VipDiscountAmount { get; set; }
    public decimal VoucherDiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GiftWrappingFee { get; set; }
    public decimal FinalTotalAmount { get; set; }
    public int EarnedPointsOnCompletion { get; set; }
}

public sealed class AccountOperationResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public CustomerProfileDto? Profile { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// PART 4: 3-STEP DISCOVERY GIFT BOX BUILDER DTOs
// ═══════════════════════════════════════════════════════════════════════════

public sealed class DiscoveryBoxStyleOptionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public sealed class DiscoveryVialSelectionDto
{
    public int ProductId { get; set; }
    public int VolumeMl { get; set; } = 2; // 2ml or 5ml
}

public sealed class DiscoveryVialResolvedItemDto
{
    public int SlotIndex { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public int VolumeMl { get; set; }
    public decimal ProductBasePrice { get; set; }
    public decimal VialPrice { get; set; }
}

public sealed class DiscoveryBoxBuildRequestDto
{
    [Required]
    public string BoxStyleCode { get; set; } = "ClassicWood";

    [Required]
    public List<DiscoveryVialSelectionDto> Vials { get; set; } = new();

    public string? RecipientName { get; set; }
    public string? GreetingMessage { get; set; }
    public string GreetingFont { get; set; } = "Playfair Display";
    public int Quantity { get; set; } = 1;
}

public sealed class DiscoveryBoxConfigurationDto
{
    public string BoxStyleCode { get; set; } = string.Empty;
    public string BoxStyleName { get; set; } = string.Empty;
    public decimal BoxPrice { get; set; }
    public List<DiscoveryVialResolvedItemDto> Vials { get; set; } = new();
    public decimal VialsTotalPrice { get; set; }
    public decimal TotalBoxUnitPrice { get; set; }
    public string? RecipientName { get; set; }
    public string? GreetingMessage { get; set; }
    public string GreetingFont { get; set; } = "Playfair Display";
    public string ConfigJson { get; set; } = string.Empty;
}

public sealed class DiscoveryBoxValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public DiscoveryBoxConfigurationDto? Configuration { get; set; }
}
