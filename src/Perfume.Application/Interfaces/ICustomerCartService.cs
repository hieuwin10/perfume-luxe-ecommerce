using Perfume.Application.DTOs;

namespace Perfume.Application.Interfaces;

public interface ICustomerCartService
{
    Task<CartStateDto> GetAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<CartStateDto> AddAsync(string? userId, string? guestToken, CartAddItemRequest request, CancellationToken cancellationToken = default);
    Task<CartStateDto> SetQuantityAsync(string? userId, string? guestToken, CartLineRequest request, CancellationToken cancellationToken = default);
    Task<CartStateDto> RemoveAsync(string? userId, string? guestToken, int productId, int? variantId, CancellationToken cancellationToken = default);
    Task<CartStateDto> MergeGuestCartAsync(string userId, string? guestToken, CancellationToken cancellationToken = default);
    Task ClearAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default);
}
