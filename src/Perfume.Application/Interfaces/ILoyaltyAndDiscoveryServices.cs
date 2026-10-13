using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;

namespace Perfume.Application.Interfaces;

public interface ICustomerAccountService
{
    Task<AccountOperationResultDto> RegisterCustomerAsync(CustomerRegisterRequest request, CancellationToken cancellationToken = default);
    Task<AccountOperationResultDto> AuthenticateCustomerAsync(CustomerLoginRequest request, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<AccountOperationResultDto> UpdateProfileAsync(string userId, CustomerProfileUpdateRequest request, CancellationToken cancellationToken = default);
    bool ValidatePasswordPolicy(string? password, out List<string> errors);
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

public interface IDiscoveryBoxService
{
    IReadOnlyList<DiscoveryBoxStyleOptionDto> GetAvailableBoxStyles();
    IReadOnlyList<string> GetAllowedFonts();
    decimal CalculateSampleVialPrice(decimal productBasePrice, int volumeMl);
    Task<DiscoveryBoxValidationResultDto> ValidateAndBuildConfigurationAsync(
        DiscoveryBoxBuildRequestDto request,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, ShoppingCartItem? CartItem)> AddDiscoveryBoxToCartAsync(
        string? userId,
        string? guestToken,
        DiscoveryBoxBuildRequestDto request,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, Order? Order)> PlaceDiscoveryBoxOrderAsync(
        string? userId,
        CheckoutRequestDto checkoutRequest,
        DiscoveryBoxBuildRequestDto boxRequest,
        CancellationToken cancellationToken = default);
}
