using Perfume.Application.DTOs;

namespace Perfume.Application.Interfaces;

public interface IFlashSaleService
{
    Task<FlashSaleDto?> GetActiveFlashSaleAsync(CancellationToken cancellationToken = default);
    Task<List<FlashSaleDto>> GetAllFlashSalesAsync(CancellationToken cancellationToken = default);
    Task<FlashSaleDto?> GetFlashSaleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FlashSaleDto> CreateFlashSaleAsync(CreateFlashSaleRequest request, CancellationToken cancellationToken = default);
    Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<FlashSaleItemDto?> GetActiveFlashSaleItemForProductAsync(int productId, int? variantId = null, CancellationToken cancellationToken = default);
}
