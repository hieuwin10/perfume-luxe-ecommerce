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
    Task<ProductDetailDto?> GetProductByIdAsync(int id);
    Task<List<Brand>> GetBrandsAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<FragranceFamily>> GetFragranceFamiliesAsync();
    Task<List<FragranceNote>> GetFragranceNotesAsync();
    Task<QuizRecommendationResultDto> GetQuizRecommendationsAsync(QuizAnswerRequest request);
}
