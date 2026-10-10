using Perfume.Application.DTOs;
using Perfume.Domain.Entities;

namespace Perfume.Application.Interfaces;

public interface IPerfumeCatalogService
{
    Task<List<ProductCardDto>> GetFeaturedProductsAsync();
    Task<List<ProductCardDto>> GetBestSellersAsync();
    Task<List<ProductCardDto>> GetNewArrivalsAsync();
    Task<(List<ProductCardDto> Items, int TotalCount)> GetProductsAsync(ProductFilterParams filter);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<List<Brand>> GetBrandsAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<FragranceFamily>> GetFragranceFamiliesAsync();
    Task<List<FragranceNote>> GetFragranceNotesAsync();
    Task<List<ProductCardDto>> GetQuizRecommendationsAsync(QuizAnswerRequest request);
}

public interface IOrderService
{
    Task<(bool Success, string? Error, Order? Order)> PlaceOrderAsync(CheckoutRequestDto request, string? userId = null);
    Task<Voucher?> ValidateVoucherAsync(string code, decimal currentSubTotal);
    Task<Order?> GetOrderByCodeAsync(string orderCode);
}
