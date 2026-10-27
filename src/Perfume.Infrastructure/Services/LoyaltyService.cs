using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    public const decimal SilverThreshold = 5_000_000m;
    public const decimal GoldThreshold = 15_000_000m;
    public const decimal DiamondThreshold = 30_000_000m;
    public const decimal PointsUnitAmount = 10_000m;

    private readonly PerfumeDbContext _db;

    public LoyaltyService(PerfumeDbContext db)
    {
        _db = db;
    }

    public UserTier EvaluateTier(decimal cumulativeSpent)
    {
        if (cumulativeSpent >= DiamondThreshold) return UserTier.Diamond;
        if (cumulativeSpent >= GoldThreshold) return UserTier.Gold;
        if (cumulativeSpent >= SilverThreshold) return UserTier.Silver;
        return UserTier.Bronze;
    }

    public decimal GetDiscountPercent(UserTier tier) => tier switch
    {
        UserTier.Diamond => 12m,
        UserTier.Gold => 7m,
        UserTier.Silver => 3m,
        _ => 0m
    };

    public int CalculatePoints(decimal cumulativeSpent)
    {
        if (cumulativeSpent <= 0m) return 0;
        return (int)Math.Floor(cumulativeSpent / PointsUnitAmount);
    }

    public UserTier? GetNextTier(UserTier currentTier) => currentTier switch
    {
        UserTier.Bronze => UserTier.Silver,
        UserTier.Silver => UserTier.Gold,
        UserTier.Gold => UserTier.Diamond,
        UserTier.Diamond => null,
        _ => UserTier.Silver
    };

    public decimal CalculateProgressPercent(decimal cumulativeSpent)
    {
        if (cumulativeSpent <= 0m) return 0m;
        if (cumulativeSpent >= DiamondThreshold) return 100m;

        if (cumulativeSpent < SilverThreshold)
        {
            return Math.Clamp(Math.Round((cumulativeSpent / SilverThreshold) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
        }

        if (cumulativeSpent < GoldThreshold)
        {
            var span = GoldThreshold - SilverThreshold; // 10,000,000
            return Math.Clamp(Math.Round(((cumulativeSpent - SilverThreshold) / span) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
        }

        // Gold toward Diamond
        var goldSpan = DiamondThreshold - GoldThreshold; // 15,000,000
        return Math.Clamp(Math.Round(((cumulativeSpent - GoldThreshold) / goldSpan) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
    }

    public decimal CalculateAmountToNextTier(decimal cumulativeSpent)
    {
        var sanitized = Math.Max(0m, cumulativeSpent);
        if (sanitized >= DiamondThreshold) return 0m;
        if (sanitized >= GoldThreshold) return DiamondThreshold - sanitized;
        if (sanitized >= SilverThreshold) return GoldThreshold - sanitized;
        return SilverThreshold - sanitized;
    }

    public async Task<LoyaltyStatusDto> GetOrSyncUserLoyaltyAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new LoyaltyStatusDto
            {
                UserId = string.Empty,
                CurrentTier = UserTier.Bronze,
                TierName = UserTier.Bronze.ToString(),
                NextTier = UserTier.Silver,
                NextTierName = UserTier.Silver.ToString(),
                TotalSpent = 0m,
                LoyaltyPoints = 0,
                DiscountPercent = 0m,
                ProgressPercent = 0m,
                AmountToNextTier = SilverThreshold,
                QualifyingOrderCount = 0
            };
        }

        var userOrders = await _db.Orders
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .ToListAsync(cancellationToken);

        var qualifyingOrders = userOrders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled
                     && o.PaymentStatus != PaymentStatus.Failed
                     && o.PaymentStatus != PaymentStatus.Refunded
                     && (o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed))
            .ToList();

        var loyaltyRecord = await _db.UserLoyalties
            .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        decimal cumulativeSpent;
        if (userOrders.Count > 0)
        {
            cumulativeSpent = qualifyingOrders.Sum(o => o.TotalAmount);
        }
        else
        {
            cumulativeSpent = loyaltyRecord?.TotalSpent ?? 0m;
        }

        var evaluatedTier = EvaluateTier(cumulativeSpent);
        var calculatedPoints = CalculatePoints(cumulativeSpent);
        var nextTier = GetNextTier(evaluatedTier);

        if (loyaltyRecord == null)
        {
            loyaltyRecord = new UserLoyalty
            {
                UserId = userId,
                TotalSpent = cumulativeSpent,
                Tier = evaluatedTier,
                Points = calculatedPoints
            };
            _db.UserLoyalties.Add(loyaltyRecord);
        }
        else
        {
            loyaltyRecord.TotalSpent = cumulativeSpent;
            loyaltyRecord.Tier = evaluatedTier;
            loyaltyRecord.Points = calculatedPoints;
        }

        var appUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (appUser != null)
        {
            appUser.Tier = evaluatedTier;
            appUser.LoyaltyPoints = calculatedPoints;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new LoyaltyStatusDto
        {
            UserId = userId,
            CurrentTier = evaluatedTier,
            TierName = evaluatedTier.ToString(),
            NextTier = nextTier,
            NextTierName = nextTier?.ToString(),
            TotalSpent = cumulativeSpent,
            LoyaltyPoints = calculatedPoints,
            DiscountPercent = GetDiscountPercent(evaluatedTier),
            ProgressPercent = CalculateProgressPercent(cumulativeSpent),
            AmountToNextTier = CalculateAmountToNextTier(cumulativeSpent),
            QualifyingOrderCount = qualifyingOrders.Count
        };
    }

    public async Task<OrderVipDiscountPreviewDto> CalculateOrderVipPricingAsync(
        string? userId,
        decimal subTotal,
        decimal voucherDiscount = 0m,
        bool isGiftWrapping = false,
        CancellationToken cancellationToken = default)
    {
        var safeSubTotal = Math.Max(0m, subTotal);
        var safeVoucherDiscount = Math.Clamp(voucherDiscount, 0m, safeSubTotal);

        UserTier appliedTier = UserTier.Bronze;
        decimal vipDiscountPercent = 0m;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var status = await GetOrSyncUserLoyaltyAsync(userId, cancellationToken);
            appliedTier = status.CurrentTier;
            vipDiscountPercent = status.DiscountPercent;
        }

        var vipDiscountAmount = Math.Round(safeSubTotal * (vipDiscountPercent / 100m), 0, MidpointRounding.AwayFromZero);
        if (vipDiscountAmount + safeVoucherDiscount > safeSubTotal)
        {
            vipDiscountAmount = Math.Max(0m, safeSubTotal - safeVoucherDiscount);
        }

        var giftWrappingFee = isGiftWrapping ? 50_000m : 0m;
        var shippingFee = safeSubTotal == 0m ? 0m : (safeSubTotal >= 2_000_000m ? 0m : 35_000m);
        var finalTotal = Math.Max(0m, safeSubTotal - safeVoucherDiscount - vipDiscountAmount + shippingFee + giftWrappingFee);

        return new OrderVipDiscountPreviewDto
        {
            UserId = userId,
            AppliedTier = appliedTier,
            VipDiscountPercent = vipDiscountPercent,
            SubTotal = safeSubTotal,
            VipDiscountAmount = vipDiscountAmount,
            VoucherDiscountAmount = safeVoucherDiscount,
            ShippingFee = shippingFee,
            GiftWrappingFee = giftWrappingFee,
            FinalTotalAmount = finalTotal,
            EarnedPointsOnCompletion = CalculatePoints(finalTotal)
        };
    }

    public async Task<Order> ApplyVipLoyaltyToOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));

        if (order.SubTotal <= 0m && order.Items.Count > 0)
        {
            order.SubTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        }

        var preview = await CalculateOrderVipPricingAsync(
            order.UserId,
            order.SubTotal,
            order.DiscountAmount,
            order.IsGiftWrapping,
            cancellationToken);

        order.AppliedLoyaltyTier = preview.AppliedTier;
        order.LoyaltyDiscountAmount = preview.VipDiscountAmount;
        order.GiftWrappingFee = preview.GiftWrappingFee;
        order.ShippingFee = preview.ShippingFee;
        order.TotalAmount = preview.FinalTotalAmount;

        return order;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PART 4: 3-STEP DISCOVERY GIFT BOX BUILDER SERVICE
// ═══════════════════════════════════════════════════════════════════════════
