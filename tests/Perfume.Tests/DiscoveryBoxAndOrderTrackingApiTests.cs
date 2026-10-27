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

public sealed class DiscoveryBoxAndOrderTrackingApiTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_069_ApiV1_DiscoveryBox_ValidatesRoyalVelvetWith4Vials5mlAnd5PercentDiscount()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "RoyalVelvet",
            GreetingMessage = "Quà tặng sang trọng",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "5ml" },
                new() { ProductId = 2, VialSize = "5ml" },
                new() { ProductId = 3, VialSize = "5ml" },
                new() { ProductId = 4, VialSize = "5ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        Assert.True(res.Valid);
        Assert.Equal("RoyalVelvet", res.BoxStyle);
        Assert.Equal(4, res.VialCount);
        // 4 * 350,000 + 200,000 = 1,600,000; 5% discount = 80,000; Total = 1,520,000
        Assert.Equal(1_600_000m, res.SubTotal);
        Assert.Equal(80_000m, res.DiscountAmount);
        Assert.Equal(1_520_000m, res.TotalPrice);
    }

    // ─── TC-T3-070: POST /api/v1/discovery-box/validate-and-add ArtisanLeather 5 vials ─
    [Fact]
    public async Task TC_T3_070_ApiV1_DiscoveryBox_ValidatesArtisanLeatherWith5VialsAnd10PercentDiscount()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ArtisanLeather",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "5ml" },
                new() { ProductId = 2, VialSize = "5ml" },
                new() { ProductId = 3, VialSize = "2ml" },
                new() { ProductId = 4, VialSize = "2ml" },
                new() { ProductId = 5, VialSize = "2ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        // (2 * 350,000 + 3 * 180,000) + 300,000 = 1,540,000; 10% discount = 154,000; Total = 1,386,000
        Assert.Equal(1_540_000m, res.SubTotal);
        Assert.Equal(154_000m, res.DiscountAmount);
        Assert.Equal(1_386_000m, res.TotalPrice);
    }

    // ─── TC-T3-071: POST /api/v1/discovery-box/validate-and-add < 3 vials -> 400 ─
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TC_T3_071_ApiV1_DiscoveryBox_Returns400WhenFewerThan3Vials(int count)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = Enumerable.Range(1, count).Select(i => new DiscoveryVialApiSelectionDto { ProductId = i, VialSize = "2ml" }).ToList()
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-072: POST /api/v1/discovery-box/validate-and-add > 5 vials -> 400 ─
    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public async Task TC_T3_072_ApiV1_DiscoveryBox_Returns400WhenMoreThan5Vials(int count)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "RoyalVelvet",
            Vials = Enumerable.Range(1, count).Select(i => new DiscoveryVialApiSelectionDto { ProductId = i, VialSize = "2ml" }).ToList()
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-073: POST /api/v1/discovery-box/validate-and-add invalid boxStyle -> 400 ─
    [Theory]
    [InlineData("PlasticBag")]
    [InlineData("NeonMetal")]
    public async Task TC_T3_073_ApiV1_DiscoveryBox_Returns400ForUnsupportedBoxStyle(string invalidStyle)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = invalidStyle,
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "2ml" }
            }
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-074: POST /api/v1/discovery-box/validate-and-add invalid vialSize -> 400 ─
    [Theory]
    [InlineData("10ml")]
    [InlineData("100ml")]
    [InlineData("invalid")]
    public async Task TC_T3_074_ApiV1_DiscoveryBox_Returns400ForUnsupportedVialSize(string invalidSize)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = invalidSize },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "5ml" }
            }
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-075: POST /api/v1/discovery-box/validate-and-add greeting > 250 chars -> 400 ─
    [Fact]
    public async Task TC_T3_075_ApiV1_DiscoveryBox_ValidatesGreetingMessageMax250Characters()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var vials = new List<DiscoveryVialApiSelectionDto>
        {
            new() { ProductId = 1, VialSize = "2ml" },
            new() { ProductId = 2, VialSize = "2ml" },
            new() { ProductId = 3, VialSize = "2ml" }
        };

        // 251 chars -> 400 BadRequest
        var badRes = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = new string('A', 251),
            Vials = vials
        });
        Assert.IsType<BadRequestObjectResult>(badRes);

        // Exactly 250 chars -> 200 OK
        var okRes = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = new string('B', 250),
            Vials = vials
        });
        Assert.IsType<OkObjectResult>(okRes);
    }

    // ─── TC-T3-076: POST /api/v1/discovery-box/validate-and-add non-existent ProductId -> 400 ─
    [Fact]
    public async Task TC_T3_076_ApiV1_DiscoveryBox_Returns400WhenProductDoesNotExistInSeededCatalog()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Creed", Slug = "creed" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.AddRange(
            new Product { Id = 1, Name = "P1", Slug = "p1", Sku = "S1", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat },
            new Product { Id = 2, Name = "P2", Slug = "p2", Sku = "S2", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var res = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 999, VialSize = "2ml" } // missing product
            }
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-077: GET /api/v1/orders/track missing orderCode -> 400 ─────────
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TC_T3_077_ApiV1_TrackOrder_Returns400WhenOrderCodeIsEmpty(string? emptyCode)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.TrackOrder(emptyCode);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-078: GET /api/v1/orders/track non-existent orderCode -> 404 ────
    [Fact]
    public async Task TC_T3_078_ApiV1_TrackOrder_Returns404WhenOrderCodeNotFound()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.TrackOrder("LUXE-NOT-FOUND-999");
        Assert.IsType<NotFoundObjectResult>(res);
    }

    // ─── TC-T3-079: GET /api/v1/orders/track valid order with gift wrapping ───
    [Fact]
    public async Task TC_T3_079_ApiV1_TrackOrder_Returns200OkWithItemsGiftWrappingAndPaymentInfo()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-20261013-VIP1",
            CustomerName = "Trương Ngọc Ánh",
            CustomerPhone = "0908889999",
            CustomerEmail = "anh@test.vn",
            ShippingAddress = "88 Đồng Khởi, Q1",
            City = "TP.HCM",
            TrackingNumber = "VNPOST-888",
            SubTotal = 7_500_000m,
            DiscountAmount = 500_000m,
            ShippingFee = 0m,
            IsGiftWrapping = true,
            GiftWrappingFee = 50_000m,
            GiftMessage = "Gửi tặng người thương",
            TotalAmount = 7_050_000m,
            OrderStatus = OrderStatus.Shipping,
            PaymentStatus = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.BankTransfer,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Portrait of a Lady", VariantLabel = "100ml", UnitPrice = 7_500_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("luxe-20261013-vip1"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Equal("LUXE-20261013-VIP1", dto.OrderCode);
        Assert.Equal("Shipping", dto.OrderStatus);
        Assert.Equal("Completed", dto.PaymentStatus);
        Assert.True(dto.IsGiftWrapping);
        Assert.Equal(50_000m, dto.GiftWrappingFee);
        Assert.Equal("Gửi tặng người thương", dto.GiftMessage);
        Assert.Single(dto.Items);
        Assert.Equal("Portrait of a Lady", dto.Items[0].ProductName);
    }

    // ─── TC-T3-080: GET /api/v1/orders/track timeline stages for Delivered ────
    [Fact]
    public async Task TC_T3_080_ApiV1_TrackOrder_TimelineMarksAllFourStagesCompletedWhenDelivered()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-DELIV-100",
            CustomerName = "Khách Nhận Hàng",
            CustomerPhone = "0901234567",
            CustomerEmail = "d@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 4_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("LUXE-DELIV-100"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Equal(4, dto.Timeline.Count);
        Assert.All(dto.Timeline, step => Assert.True(step.Completed));
    }

    // ─── TC-T3-081: GET /api/v1/orders/track timeline includes Cancelled stage ─
    [Fact]
    public async Task TC_T3_081_ApiV1_TrackOrder_TimelineAppendsCancelledStageWhenOrderCancelled()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CANCEL-100",
            CustomerName = "Khách Hủy Đơn",
            CustomerPhone = "0901234567",
            CustomerEmail = "c@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 3_000_000m,
            OrderStatus = OrderStatus.Cancelled,
            PaymentStatus = PaymentStatus.Failed
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("LUXE-CANCEL-100"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Contains(dto.Timeline, s => s.Stage == "Cancelled" && s.Completed);
    }

    // ─── TC-T3-082: GET /api/v1/orders/track phone verification ───────────────

}
