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

public sealed class AdminFinancialReportTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_001_FinancialReport_ComputesGrossSalesDiscountsFeesAndNetRevenue()
    {
        using var db = CreateDbContext();
        db.Orders.AddRange(
            new Order
            {
                OrderCode = "LUXE-FIN-001",
                CustomerName = "Nguyễn Văn A",
                CustomerPhone = "0901111111",
                CustomerEmail = "a@test.vn",
                ShippingAddress = "Q1, HCM",
                SubTotal = 5_000_000m,
                DiscountAmount = 500_000m,
                GiftWrappingFee = 50_000m,
                ShippingFee = 0m,
                TotalAmount = 4_550_000m,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Order
            {
                OrderCode = "LUXE-FIN-002",
                CustomerName = "Trần Thị B",
                CustomerPhone = "0902222222",
                CustomerEmail = "b@test.vn",
                ShippingAddress = "Q3, HCM",
                SubTotal = 1_500_000m,
                DiscountAmount = 100_000m,
                GiftWrappingFee = 0m,
                ShippingFee = 35_000m,
                TotalAmount = 1_435_000m,
                OrderStatus = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(2, vm.TotalOrdersCount);
        Assert.Equal(6_500_000m, vm.GrossSales);
        Assert.Equal(600_000m, vm.VoucherDiscounts);
        Assert.Equal(50_000m, vm.GiftWrappingFeesCollected);
        Assert.Equal(35_000m, vm.ShippingFeesCollected);
        Assert.Equal(5_985_000m, vm.NetRevenue);
    }

    // ─── TC-T3-002: FinancialReport excludes Pending/Unpaid & Cancelled orders ─
    [Fact]
    public async Task TC_T3_002_FinancialReport_ExcludesPendingUnpaidAndCancelledOrders()
    {
        using var db = CreateDbContext();
        db.Orders.AddRange(
            new Order
            {
                OrderCode = "LUXE-DELIVERED",
                CustomerName = "Khách Hoàn Tất",
                CustomerPhone = "0901",
                CustomerEmail = "ok@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 4_000_000m,
                TotalAmount = 4_000_000m,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "LUXE-PENDING-UNPAID",
                CustomerName = "Khách Chờ",
                CustomerPhone = "0902",
                CustomerEmail = "wait@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 9_000_000m,
                TotalAmount = 9_000_000m,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "LUXE-CANCELLED",
                CustomerName = "Khách Hủy",
                CustomerPhone = "0903",
                CustomerEmail = "cancel@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 7_000_000m,
                TotalAmount = 7_000_000m,
                OrderStatus = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(1, vm.TotalOrdersCount);
        Assert.Equal(4_000_000m, vm.NetRevenue);
    }

    // ─── TC-T3-003: FinancialReport COGS uses latest StockAdjustment.UnitCost ─
    [Fact]
    public async Task TC_T3_003_FinancialReport_CogsUsesLatestStockAdjustmentUnitCost()
    {
        using var db = CreateDbContext();
        db.StockAdjustments.AddRange(
            new StockAdjustment
            {
                VariantId = 101,
                OldQuantity = 0,
                AdjustedQuantity = 10,
                NewQuantity = 10,
                UnitCost = 2_000_000m,
                Reason = "Lô cũ"
            },
            new StockAdjustment
            {
                VariantId = 101,
                OldQuantity = 10,
                AdjustedQuantity = 5,
                NewQuantity = 15,
                UnitCost = 2_400_000m,
                Reason = "Lô mới"
            });
        await db.SaveChangesAsync();

        // Ensure second adjustment has a strictly newer CreatedAt timestamp
        var latestAdj = await db.StockAdjustments.FirstAsync(a => a.Reason == "Lô mới");
        latestAdj.CreatedAt = DateTime.UtcNow.AddMinutes(5);

        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-COGS-001",
            CustomerName = "Khách VIP",
            CustomerPhone = "0909",
            CustomerEmail = "vip@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 12_000_000m,
            TotalAmount = 12_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101,
                    ProductName = "Santal 33",
                    VariantLabel = "100ml",
                    UnitPrice = 6_000_000m,
                    Quantity = 2
                }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 2 units * 2,400,000 latest UnitCost = 4,800,000
        Assert.Equal(4_800_000m, vm.TotalCogs);
        Assert.Equal(7_200_000m, vm.GrossProfit);
        Assert.Equal(60.0, vm.GrossMarginPercent);
    }

    // ─── TC-T3-004: FinancialReport COGS falls back to 42% of retail price ────
    [Fact]
    public async Task TC_T3_004_FinancialReport_CogsFallsBackTo42PercentWhenNoUnitCostRecorded()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-COGS-42",
            CustomerName = "Khách 42%",
            CustomerPhone = "0908",
            CustomerEmail = "c42@test.vn",
            ShippingAddress = "HN",
            SubTotal = 5_000_000m,
            TotalAmount = 5_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = 2,
                    VariantId = 202,
                    ProductName = "Baccarat Rouge 540",
                    VariantLabel = "70ml",
                    UnitPrice = 5_000_000m,
                    Quantity = 1
                }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 5,000,000 * 0.42 = 2,100,000
        Assert.Equal(2_100_000m, vm.TotalCogs);
        Assert.Equal(2_900_000m, vm.GrossProfit);
        Assert.Equal(58.0, vm.GrossMarginPercent);
    }

    // ─── TC-T3-005: FinancialReport mixed COGS (StockAdjustment + 42% fallback) ─
    [Fact]
    public async Task TC_T3_005_FinancialReport_MixedOrderItemsCombinesRecordedCostAnd42PercentFallback()
    {
        using var db = CreateDbContext();
        db.StockAdjustments.Add(new StockAdjustment
        {
            VariantId = 301,
            OldQuantity = 0,
            AdjustedQuantity = 10,
            NewQuantity = 10,
            UnitCost = 1_500_000m,
            Reason = "Nhập kho"
        });
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-MIX-COGS",
            CustomerName = "Khách Mix",
            CustomerPhone = "0907",
            CustomerEmail = "mix@test.vn",
            ShippingAddress = "Đà Nẵng",
            SubTotal = 8_000_000m,
            TotalAmount = 8_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, VariantId = 301, ProductName = "Chai Có Giá Vốn", UnitPrice = 4_000_000m, Quantity = 1 },
                new() { ProductId = 2, VariantId = 302, ProductName = "Chai 42%", UnitPrice = 4_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 1,500,000 + (4,000,000 * 0.42 = 1,680,000) = 3,180,000
        Assert.Equal(3_180_000m, vm.TotalCogs);
        Assert.Equal(4_820_000m, vm.GrossProfit);
    }

    // ─── TC-T3-006: FinancialReport GrossMarginPercent & AverageOrderValue ─────
    [Fact]
    public async Task TC_T3_006_FinancialReport_ZeroOrdersProducesSafeZeroMarginAndZeroAov()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(0, vm.TotalOrdersCount);
        Assert.Equal(0m, vm.NetRevenue);
        Assert.Equal(0m, vm.TotalCogs);
        Assert.Equal(0m, vm.GrossProfit);
        Assert.Equal(0.0, vm.GrossMarginPercent);
        Assert.Equal(0m, vm.AverageOrderValue);
    }

    // ─── TC-T3-007: FinancialReport preset filters ────────────────────────────
    [Theory]
    [InlineData("today")]
    [InlineData("7days")]
    [InlineData("thismonth")]
    [InlineData("thisyear")]
    [InlineData("all")]
    [InlineData("30days")]
    public async Task TC_T3_007_FinancialReport_PresetsFilterOrdersCorrectly(string preset)
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = $"LUXE-{preset.ToUpperInvariant()}",
            CustomerName = "Khách Preset",
            CustomerPhone = "0905555555",
            CustomerEmail = "preset@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 3_000_000m,
            TotalAmount = 3_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, preset));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(preset, vm.Preset);
        Assert.Equal(1, vm.TotalOrdersCount);
        Assert.Equal(3_000_000m, vm.NetRevenue);
    }

    // ─── TC-T3-008: Inventory Valuation by Brand ──────────────────────────────

}
