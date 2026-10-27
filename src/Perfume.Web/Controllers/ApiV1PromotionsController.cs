using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

[ApiController]
public sealed class ApiV1PromotionsController : ControllerBase
{
    private readonly IFlashSaleService _flashSaleService;

    public ApiV1PromotionsController(IFlashSaleService flashSaleService) => _flashSaleService = flashSaleService;

    // ─── 1. GET /api/v1/flashsales/active ─────────────────────────────────────
    [HttpGet("api/v1/flashsales/active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveFlashSale(CancellationToken cancellationToken = default)
    {
        var active = await _flashSaleService.GetActiveFlashSaleAsync(cancellationToken);
        if (active == null)
        {
            return NotFound(new { message = "Hiện không có chiến dịch Flash Sale nào đang diễn ra." });
        }

        var remainingSeconds = (long)Math.Max(0, (active.EndTime - DateTime.UtcNow).TotalSeconds);

        return Ok(new FlashSaleActiveApiResponse
        {
            Id = active.Id,
            Name = active.Name,
            Description = active.Description,
            StartTime = active.StartTime,
            EndTime = active.EndTime,
            IsActive = active.IsActive,
            IsOngoing = active.IsOngoing,
            RemainingSeconds = remainingSeconds,
            BannerUrl = active.BannerUrl,
            Items = active.Items
        });
    }
}
