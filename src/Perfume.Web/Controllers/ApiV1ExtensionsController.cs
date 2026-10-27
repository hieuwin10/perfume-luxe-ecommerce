using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

/// <summary>Compatibility adapter for legacy direct controller consumers.</summary>
[NonController]
public sealed class ApiV1ExtensionsController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IFlashSaleService _flashSales;

    public ApiV1ExtensionsController(PerfumeDbContext db, IFlashSaleService flashSaleService)
    {
        _db = db;
        _flashSales = flashSaleService;
    }

    private T AttachContext<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = ControllerContext;
        return controller;
    }

    public Task<IActionResult> GetActiveFlashSale(CancellationToken cancellationToken = default) =>
        AttachContext(new ApiV1PromotionsController(_flashSales)).GetActiveFlashSale(cancellationToken);

    public Task<IActionResult> GetLoyaltyStatus(CancellationToken cancellationToken = default) =>
        AttachContext(new ApiV1LoyaltyController(_db)).GetLoyaltyStatus(cancellationToken);

    public Task<IActionResult> ValidateAndAddDiscoveryBox(DiscoveryBoxApiRequest? request, CancellationToken cancellationToken = default) =>
        AttachContext(new ApiV1DiscoveryBoxController(_db)).ValidateAndAddDiscoveryBox(request, cancellationToken);

    public Task<IActionResult> TrackOrder(string? orderCode, string? phone = null, CancellationToken cancellationToken = default) =>
        AttachContext(new ApiV1OrderTrackingController(_db)).TrackOrder(orderCode, phone, cancellationToken);

    public Task<IActionResult> CreateTastingBooking(TastingBookingApiRequest? request, CancellationToken cancellationToken = default) =>
        AttachContext(new ApiV1ExperienceController(_db)).CreateTastingBooking(request, cancellationToken);

    public IActionResult GetOpenApiSpec() =>
        AttachContext(new ApiV1DocumentationController()).GetOpenApiSpec();

    public IActionResult GetSwaggerUi() =>
        AttachContext(new ApiV1DocumentationController()).GetSwaggerUi();

    public static UserTier CalculateTierFromSpent(decimal totalSpent) => ApiV1LoyaltyController.CalculateTierFromSpent(totalSpent);

    public static (int DiscountPercent, string NextTier, decimal AmountToNextTier) GetTierProgress(UserTier tier, decimal totalSpent) =>
        ApiV1LoyaltyController.GetTierProgress(tier, totalSpent);
}
