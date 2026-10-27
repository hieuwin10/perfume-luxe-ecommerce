using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Perfume.Web.Areas.Admin.Controllers;
using Perfume.Web.Controllers;
using Perfume.Web.Security;
using Xunit;

namespace Perfume.Tests;

public sealed class LoyaltyApiTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_057_ApiV1_GetActiveFlashSale_ReturnsDiscountPercentAndRemainingStockPerItem()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "MFK", Slug = "mfk" };
        var cat = new Category { Id = 1, Name = "Extrait", Slug = "extrait" };
        var prod = new Product { Id = 1, Name = "BR540 Extrait", Slug = "br540-extrait", Sku = "MFK-540E", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 10_000_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.FlashSales.Add(new FlashSale
        {
            Name = "MFK Exclusive",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { ProductId = 1, Product = prod, FlashSalePrice = 7_500_000m, StockLimit = 20, SoldQuantity = 8 }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        var item = Assert.Single(payload.Items);
        Assert.Equal(10_000_000m, item.OriginalPrice);
        Assert.Equal(7_500_000m, item.FlashSalePrice);
        Assert.Equal(25, item.DiscountPercent);
        Assert.Equal(12, item.RemainingQuantity);
    }

    // ─── TC-T3-058: GET /api/v1/flashsales/active excludes soft-deleted items ─
    [Fact]
    public async Task TC_T3_058_ApiV1_GetActiveFlashSale_ExcludesSoftDeletedFlashSaleItems()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Le Labo", Slug = "le-labo" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Another 13", Slug = "another-13", Sku = "LL-A13", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 6_000_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.FlashSales.Add(new FlashSale
        {
            Name = "Filter Deleted Items",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { ProductId = 1, Product = prod, FlashSalePrice = 4_800_000m, StockLimit = 10, IsDeleted = false },
                new() { ProductId = 1, Product = prod, FlashSalePrice = 3_000_000m, StockLimit = 10, IsDeleted = true }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        Assert.Single(payload.Items);
        Assert.Equal(4_800_000m, payload.Items[0].FlashSalePrice);
    }

    // ─── TC-T3-059: GET /api/v1/loyalty/status unauthenticated -> 401 ─────────
    [Fact]
    public async Task TC_T3_059_ApiV1_GetLoyaltyStatus_Returns401WhenUnauthenticated()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: null);

        var res = await controller.GetLoyaltyStatus();
        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ─── TC-T3-060: GET /api/v1/loyalty/status unknown user -> 401 ────────────
    [Fact]
    public async Task TC_T3_060_ApiV1_GetLoyaltyStatus_Returns401WhenUserDoesNotExistInDb()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "ghost-user-id", email: "ghost@test.vn", roles: "Customer");

        var res = await controller.GetLoyaltyStatus();
        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ─── TC-T3-061: GET /api/v1/loyalty/status Bronze tier ────────────────────
    [Fact]
    public async Task TC_T3_061_ApiV1_GetLoyaltyStatus_ReturnsBronzeTierWithZeroDiscountForNewUser()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-bronze", UserName = "bronze@test.vn", Email = "bronze@test.vn", FullName = "Bronze Member" });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-bronze", email: "bronze@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Bronze", dto.Tier);
        Assert.Equal(0m, dto.TotalSpent);
        Assert.Equal(0, dto.DiscountPercent);
        Assert.Equal("Silver", dto.NextTier);
        Assert.Equal(5_000_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-062: GET /api/v1/loyalty/status Silver tier (3% discount) ──────
    [Fact]
    public async Task TC_T3_062_ApiV1_GetLoyaltyStatus_ReturnsSilverTierWith3PercentDiscount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-silver", UserName = "silver@test.vn", Email = "silver@test.vn", FullName = "Silver Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-SILVER",
            UserId = "u-silver",
            CustomerName = "Silver Member",
            CustomerPhone = "0901",
            CustomerEmail = "silver@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 6_500_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-silver", email: "silver@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Silver", dto.Tier);
        Assert.Equal(6_500_000m, dto.TotalSpent);
        Assert.Equal(3, dto.DiscountPercent);
        Assert.Equal(65, dto.Points);
        Assert.Equal("Gold", dto.NextTier);
        Assert.Equal(8_500_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-063: GET /api/v1/loyalty/status Gold tier (7% discount) ────────
    [Fact]
    public async Task TC_T3_063_ApiV1_GetLoyaltyStatus_ReturnsGoldTierWith7PercentDiscount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-gold", UserName = "gold@test.vn", Email = "gold@test.vn", FullName = "Gold Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-GOLD",
            UserId = "u-gold",
            CustomerName = "Gold Member",
            CustomerPhone = "0902",
            CustomerEmail = "gold@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 18_000_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-gold", email: "gold@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Gold", dto.Tier);
        Assert.Equal(18_000_000m, dto.TotalSpent);
        Assert.Equal(7, dto.DiscountPercent);
        Assert.Equal(180, dto.Points);
        Assert.Equal("Diamond", dto.NextTier);
        Assert.Equal(12_000_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-064: GET /api/v1/loyalty/status Diamond tier (12% discount) ────
    [Fact]
    public async Task TC_T3_064_ApiV1_GetLoyaltyStatus_ReturnsDiamondTierWith12PercentDiscountAndZeroNextAmount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-diamond", UserName = "diamond@test.vn", Email = "diamond@test.vn", FullName = "Diamond Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-DIAMOND",
            UserId = "u-diamond",
            CustomerName = "Diamond Member",
            CustomerPhone = "0903",
            CustomerEmail = "diamond@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 35_000_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-diamond", email: "diamond@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Diamond", dto.Tier);
        Assert.Equal(35_000_000m, dto.TotalSpent);
        Assert.Equal(12, dto.DiscountPercent);
        Assert.Equal("Max", dto.NextTier);
        Assert.Equal(0m, dto.AmountToNextTier);
    }

    // ─── TC-T3-065: GET /api/v1/loyalty/status ignores non-Delivered orders ───
    [Fact]
    public async Task TC_T3_065_ApiV1_GetLoyaltyStatus_IgnoresPendingAndCancelledOrdersInTotalSpent()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-pending", UserName = "p@test.vn", Email = "p@test.vn", FullName = "Pending Buyer" });
        db.Orders.AddRange(
            new Order { OrderCode = "P-1", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 20_000_000m, OrderStatus = OrderStatus.Pending },
            new Order { OrderCode = "P-2", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 20_000_000m, OrderStatus = OrderStatus.Cancelled },
            new Order { OrderCode = "P-3", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 2_000_000m, OrderStatus = OrderStatus.Delivered });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-pending", email: "p@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal(2_000_000m, dto.TotalSpent);
        Assert.Equal("Bronze", dto.Tier);
    }

    // ─── TC-T3-066: GET /api/v1/loyalty/status respects UserLoyalty record ────
    [Fact]
    public async Task TC_T3_066_ApiV1_GetLoyaltyStatus_RespectsManualVipUpgradeInUserLoyaltyTable()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-vip-manual", UserName = "vipm@test.vn", Email = "vipm@test.vn", FullName = "Invited VIP" });
        db.UserLoyalties.Add(new UserLoyalty
        {
            UserId = "u-vip-manual",
            Tier = UserTier.Gold,
            Points = 500,
            TotalSpent = 16_000_000m
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-vip-manual", email: "vipm@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Gold", dto.Tier);
        Assert.Equal(7, dto.DiscountPercent);
        Assert.Equal(500, dto.Points);
        Assert.Equal(16_000_000m, dto.TotalSpent);
    }

    // ─── TC-T3-067: Loyalty tier boundary thresholds ──────────────────────────
    [Theory]
    [InlineData(0, UserTier.Bronze, 0, "Silver", 5_000_000)]
    [InlineData(4_999_999, UserTier.Bronze, 0, "Silver", 1)]
    [InlineData(5_000_000, UserTier.Silver, 3, "Gold", 10_000_000)]
    [InlineData(14_999_999, UserTier.Silver, 3, "Gold", 1)]
    [InlineData(15_000_000, UserTier.Gold, 7, "Diamond", 15_000_000)]
    [InlineData(29_999_999, UserTier.Gold, 7, "Diamond", 1)]
    [InlineData(30_000_000, UserTier.Diamond, 12, "Max", 0)]
    public void TC_T3_067_ApiV1_LoyaltyTierBoundaries_MatchSpecification(
        decimal spent,
        UserTier expectedTier,
        int expectedDiscount,
        string expectedNextTier,
        decimal expectedAmountToNext)
    {
        var tier = ApiV1ExtensionsController.CalculateTierFromSpent(spent);
        var (discount, nextTier, amountToNext) = ApiV1ExtensionsController.GetTierProgress(tier, spent);

        Assert.Equal(expectedTier, tier);
        Assert.Equal(expectedDiscount, discount);
        Assert.Equal(expectedNextTier, nextTier);
        Assert.Equal(expectedAmountToNext, amountToNext);
    }

    // ─── TC-T3-068: POST /api/v1/discovery-box/validate-and-add ClassicWood 3 vials ─
    [Fact]
    public async Task TC_T3_068_ApiV1_DiscoveryBox_ValidatesClassicWoodWith3Vials2ml()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = "Happy Anniversary!",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "2ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        Assert.True(res.Valid);
        Assert.Equal("ClassicWood", res.BoxStyle);
        Assert.Equal(3, res.VialCount);
        // 3 * 180,000 + 150,000 box = 690,000 (0% discount)
        Assert.Equal(690_000m, res.SubTotal);
        Assert.Equal(0m, res.DiscountAmount);
        Assert.Equal(690_000m, res.TotalPrice);
    }

    // ─── TC-T3-069: POST /api/v1/discovery-box/validate-and-add RoyalVelvet 4 vials ─

}
