using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;

namespace Perfume.Application.Interfaces;

public interface ILoyaltyService : ILoyaltyTierService
{
}

public interface ILoyaltyTierService
{
    UserTier EvaluateTier(decimal cumulativeSpent);
    decimal GetDiscountPercent(UserTier tier);
    int CalculatePoints(decimal cumulativeSpent);
    decimal CalculateProgressPercent(decimal cumulativeSpent);
    decimal CalculateAmountToNextTier(decimal cumulativeSpent);
    UserTier? GetNextTier(UserTier currentTier);
    Task<LoyaltyStatusDto> GetOrSyncUserLoyaltyAsync(string userId, CancellationToken cancellationToken = default);
    Task<OrderVipDiscountPreviewDto> CalculateOrderVipPricingAsync(
        string? userId,
        decimal subTotal,
        decimal voucherDiscount = 0m,
        bool isGiftWrapping = false,
        CancellationToken cancellationToken = default);
    Task<Order> ApplyVipLoyaltyToOrderAsync(Order order, CancellationToken cancellationToken = default);
}
