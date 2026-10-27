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
public sealed class ApiV1LoyaltyController : ControllerBase
{
    private readonly PerfumeDbContext _db;

    public ApiV1LoyaltyController(PerfumeDbContext db) => _db = db;

    // ─── 2. GET /api/v1/loyalty/status ────────────────────────────────────────
    [HttpGet("api/v1/loyalty/status")]
    [Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> GetLoyaltyStatus(CancellationToken cancellationToken = default)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để tra cứu hạng thành viên VIP." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Không tìm thấy định danh tài khoản hợp lệ." });
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var loyalty = await _db.UserLoyalties.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        if (user == null && loyalty == null)
        {
            return Unauthorized(new { message = "Tài khoản không tồn tại trong hệ thống." });
        }

        var deliveredSpent = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.OrderStatus == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var totalSpent = loyalty != null && loyalty.TotalSpent > deliveredSpent
            ? loyalty.TotalSpent
            : deliveredSpent;

        var computedTier = CalculateTierFromSpent(totalSpent);
        var storedTier = loyalty?.Tier ?? user?.Tier ?? UserTier.Bronze;
        var effectiveTier = storedTier > computedTier ? storedTier : computedTier;

        var (discountPercent, nextTier, amountToNextTier) = GetTierProgress(effectiveTier, totalSpent);
        var points = loyalty?.Points > 0
            ? loyalty.Points
            : (user?.LoyaltyPoints > 0 ? user.LoyaltyPoints : (int)(totalSpent / 100_000m));

        return Ok(new LoyaltyStatusApiResponse
        {
            UserId = userId,
            Tier = effectiveTier.ToString(),
            TotalSpent = totalSpent,
            DiscountPercent = discountPercent,
            Points = points,
            NextTier = nextTier,
            AmountToNextTier = amountToNextTier
        });
    }

    public static UserTier CalculateTierFromSpent(decimal totalSpent)
    {
        if (totalSpent >= 30_000_000m) return UserTier.Diamond;
        if (totalSpent >= 15_000_000m) return UserTier.Gold;
        if (totalSpent >= 5_000_000m) return UserTier.Silver;
        return UserTier.Bronze;
    }

    public static (int DiscountPercent, string NextTier, decimal AmountToNextTier) GetTierProgress(UserTier tier, decimal totalSpent)
    {
        return tier switch
        {
            UserTier.Diamond => (12, "Max", 0m),
            UserTier.Gold => (7, "Diamond", Math.Max(0m, 30_000_000m - totalSpent)),
            UserTier.Silver => (3, "Gold", Math.Max(0m, 15_000_000m - totalSpent)),
            _ => (0, "Silver", Math.Max(0m, 5_000_000m - totalSpent))
        };
    }
}
