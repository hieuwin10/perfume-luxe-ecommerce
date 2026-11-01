using Perfume.Application.DTOs;
using Perfume.Domain.Entities;

namespace Perfume.Application.Interfaces;

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
