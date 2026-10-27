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

public sealed class AdminAnalyticsTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_008_FinancialReport_InventoryValuationByBrandComputesUnitsCostRetailAndProfit()
    {
        using var db = CreateDbContext();
        var brand1 = new Brand { Id = 1, Name = "Creed", Slug = "creed" };
        var brand2 = new Brand { Id = 2, Name = "Le Labo", Slug = "le-labo" };
        var cat = new Category { Id = 1, Name = "Niche", Slug = "niche" };
        var prod1 = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "CRD-1", BrandId = 1, Brand = brand1, CategoryId = 1, Category = cat, BasePrice = 8_000_000m };
        var prod2 = new Product { Id = 2, Name = "Santal 33", Slug = "santal-33", Sku = "LLB-1", BrandId = 2, Brand = brand2, CategoryId = 1, Category = cat, BasePrice = 6_000_000m };

        var v1 = new ProductVariant { Id = 10, ProductId = 1, Product = prod1, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "CRD-100", OriginalPrice = 8_000_000m, SalePrice = 8_000_000m, StockQuantity = 5 };
        var v2 = new ProductVariant { Id = 20, ProductId = 2, Product = prod2, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "LLB-100", OriginalPrice = 6_000_000m, SalePrice = 6_000_000m, StockQuantity = 10 };

        db.Brands.AddRange(brand1, brand2);
        db.Categories.Add(cat);
        db.Products.AddRange(prod1, prod2);
        db.ProductVariants.AddRange(v1, v2);
        db.StockAdjustments.Add(new StockAdjustment { VariantId = 10, UnitCost = 3_000_000m, Reason = "Nhập Creed" });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(15, vm.TotalStockQuantity);
        // Creed: 5 * 3,000,000 = 15,000,000 cost; 5 * 8,000,000 = 40,000,000 retail -> 25,000,000 profit
        // Le Labo: 10 * (6,000,000 * 0.42 = 2,520,000) = 25,200,000 cost; 10 * 6,000,000 = 60,000,000 retail -> 34,800,000 profit
        Assert.Equal(40_200_000m, vm.TotalInventoryCostValue);
        Assert.Equal(100_000_000m, vm.TotalInventoryRetailValue);
        Assert.Equal(59_800_000m, vm.UnrealizedGrossProfit);

        var creedVal = Assert.Single(vm.BrandValuations, b => b.BrandName == "Creed");
        Assert.Equal(5, creedVal.TotalUnits);
        Assert.Equal(15_000_000m, creedVal.TotalCostValue);
        Assert.Equal(40_000_000m, creedVal.TotalRetailValue);
        Assert.Equal(25_000_000m, creedVal.PotentialProfit);
    }

    // ─── TC-T3-009: Inventory Valuation price fallback priority ───────────────
    [Fact]
    public async Task TC_T3_009_FinancialReport_InventoryValuationFallsBackFromSalePriceToOriginalPriceToBasePrice()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Tom Ford", Slug = "tom-ford" };
        var cat = new Category { Id = 1, Name = "Luxury", Slug = "luxury" };
        var prod = new Product { Id = 1, Name = "Oud Wood", Slug = "oud-wood", Sku = "TF-OW", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 7_000_000m };
        var v1 = new ProductVariant { Id = 1, ProductId = 1, Product = prod, VolumeMl = 50, VolumeLabel = "50ml", VariantSku = "TF-50", SalePrice = 0m, OriginalPrice = 5_000_000m, StockQuantity = 2 };
        var v2 = new ProductVariant { Id = 2, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "TF-100", SalePrice = 0m, OriginalPrice = 0m, StockQuantity = 1 };

        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.AddRange(v1, v2);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // v1 uses OriginalPrice (2 * 5,000,000 = 10,000,000), v2 uses Product.BasePrice (1 * 7,000,000 = 7,000,000)
        Assert.Equal(17_000_000m, vm.TotalInventoryRetailValue);
    }

    // ─── TC-T3-010: FinancialReport 14-day chart & fragrance family chart ─────
    [Fact]
    public async Task TC_T3_010_FinancialReport_Populates14DayRevenueProfitChartAndFragranceFamilyBreakdown()
    {
        using var db = CreateDbContext();
        var fam = new FragranceFamily { Id = 1, Name = "Woody Oriental", Slug = "woody-oriental" };
        var brand = new Brand { Id = 1, Name = "MFK", Slug = "mfk" };
        var cat = new Category { Id = 1, Name = "Extrait", Slug = "extrait" };
        var prod = new Product { Id = 1, Name = "Grand Soir", Slug = "grand-soir", Sku = "MFK-GS", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, FragranceFamilyId = 1, FragranceFamily = fam, BasePrice = 6_000_000m };

        db.FragranceFamilies.Add(fam);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CHART-01",
            CustomerName = "Khách Biểu Đồ",
            CustomerPhone = "0901234567",
            CustomerEmail = "chart@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 6_000_000m,
            TotalAmount = 6_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Product = prod, ProductName = "Grand Soir", UnitPrice = 6_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(14, vm.DailyChartLabels.Count);
        Assert.Equal(14, vm.DailyRevenueData.Count);
        Assert.Equal(14, vm.DailyProfitData.Count);
        Assert.Contains("Woody Oriental", vm.FamilyChartLabels);
        Assert.Contains(6_000_000m, vm.FamilyChartData);
    }

    // ─── TC-T3-011: ExportFinancialOrdersCsv UTF-8 BOM verification ───────────
    [Fact]
    public async Task TC_T3_011_ExportFinancialOrdersCsv_IncludesUtf8BomAndCorrectContentType()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CSV-001",
            CustomerName = "Lê Hoàng Bảo",
            CustomerPhone = "0912345678",
            CustomerEmail = "bao@test.vn",
            ShippingAddress = "Q1, HCM",
            SubTotal = 4_000_000m,
            DiscountAmount = 200_000m,
            GiftWrappingFee = 50_000m,
            ShippingFee = 0m,
            TotalAmount = 3_850_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportFinancialOrdersCsv(null, null));

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.StartsWith("PerfumeLuxe_Financial_Orders_", fileResult.FileDownloadName);
        // UTF-8 BOM bytes: 0xEF, 0xBB, 0xBF
        Assert.True(fileResult.FileContents.Length >= 3);
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);
    }

    // ─── TC-T3-012: ExportFinancialOrdersCsv header & row calculation ─────────
    [Fact]
    public async Task TC_T3_012_ExportFinancialOrdersCsv_ContainsExpectedColumnsAndCalculatedRowValues()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CSV-002",
            CustomerName = "Phạm Minh Châu",
            CustomerPhone = "0987654321",
            CustomerEmail = "chau@test.vn",
            ShippingAddress = "Hà Nội",
            SubTotal = 5_000_000m,
            DiscountAmount = 0m,
            GiftWrappingFee = 50_000m,
            ShippingFee = 0m,
            TotalAmount = 5_050_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Rose 31", UnitPrice = 5_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportFinancialOrdersCsv(null, null));
        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("Mã Đơn,Ngày Đặt,Khách Hàng,Phương Thức,Tiền Hàng,Giảm Giá,Gói Quà,Phí Ship,Doanh Thu Thuần,Giá Vốn Ước Tính,Lợi Nhuận Gộp,Biên Lãi %", csvText);
        Assert.Contains("\"LUXE-CSV-002\"", csvText);
        Assert.Contains("\"Phạm Minh Châu\"", csvText);
        // COGS = 5,000,000 * 0.42 = 2,100,000; Profit = 5,050,000 - 2,100,000 = 2,950,000
        Assert.Contains("5000000,0,50000,0,5050000,2100000,2950000", csvText);
    }

    // ─── TC-T3-013: ExportInventoryValuationCsv UTF-8 BOM & row accuracy ──────
    [Fact]
    public async Task TC_T3_013_ExportInventoryValuationCsv_IncludesUtf8BomAndAccurateValuationColumns()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Byredo", Slug = "byredo" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Gypsy Water", Slug = "gypsy-water", Sku = "BYR-GW", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 5_500_000m };
        var variant = new ProductVariant { Id = 55, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "BYR-GW-100", OriginalPrice = 5_500_000m, SalePrice = 5_000_000m, StockQuantity = 4 };

        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.Add(variant);
        db.StockAdjustments.Add(new StockAdjustment { VariantId = 55, UnitCost = 2_000_000m, Reason = "Nhập kho" });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportInventoryValuationCsv());

        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);

        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("ID Variant,Mã SKU,Tên Nước Hoa,Thương Hiệu,Dung Tích,Tồn Kho,Đơn Giá Vốn,Đơn Giá Niêm Yết,Tổng Giá Trị Vốn,Tổng Giá Trị Bán Lẻ,Lợi Nhuận Tiềm Năng", csvText);
        // 4 units * 2,000,000 cost = 8,000,000; 4 * 5,000,000 retail = 20,000,000; profit = 12,000,000
        Assert.Contains("55,\"BYR-GW-100\",\"Gypsy Water\",\"Byredo\",\"100ml\",4,2000000,5000000,8000000,20000000,12000000", csvText);
    }

    // ─── TC-T3-014: StoreManagerController.ExportCsv UTF-8 BOM & CSV injection ─
    [Fact]
    public async Task TC_T3_014_StoreManagerExportCsv_IncludesBomAndEscapesFormulaInjectionPayloads()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-SAFE-CSV",
            CustomerName = "=cmd|' /C calc'!A0",
            CustomerPhone = "0901112222",
            CustomerEmail = "csv@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 2_000_000m,
            TotalAmount = 2_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new StoreManagerController(db);
        AttachHttpContext(controller, "mgr-1", "mgr@perfumeluxe.vn", "StoreManager");

        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportCsv(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);
        // Formula injection payload starting with '=' must be prefixed with single quote "'"
        Assert.Contains("\"'=cmd|' /C calc'!A0\"", csv);
    }

    // ─── TC-T3-015: StoreManagerController.Index KPI aggregation ──────────────
    [Fact]
    public async Task TC_T3_015_StoreManagerIndex_CalculatesRevenueCancellationRateAndStockAlerts()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Dior", Slug = "dior" };
        var cat = new Category { Id = 1, Name = "Men", Slug = "men" };
        var prod = new Product { Id = 1, Name = "Sauvage", Slug = "sauvage", Sku = "DIOR-SV", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 3_500_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.AddRange(
            new ProductVariant { Id = 1, ProductId = 1, Product = prod, VolumeMl = 60, VolumeLabel = "60ml", VariantSku = "SV-60", OriginalPrice = 3_000_000m, SalePrice = 3_000_000m, StockQuantity = 3 },
            new ProductVariant { Id = 2, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "SV-100", OriginalPrice = 4_000_000m, SalePrice = 4_000_000m, StockQuantity = 0 });

        db.Orders.AddRange(
            new Order
            {
                OrderCode = "SM-1",
                CustomerName = "VIP 1",
                CustomerEmail = "vip1@test.vn",
                CustomerPhone = "0901",
                ShippingAddress = "HCM",
                SubTotal = 4_000_000m,
                DiscountAmount = 200_000m,
                TotalAmount = 3_800_000m,
                VoucherCode = "LUXE200",
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "SM-2",
                CustomerName = "Cancel 1",
                CustomerEmail = "c1@test.vn",
                CustomerPhone = "0902",
                ShippingAddress = "HCM",
                SubTotal = 2_000_000m,
                TotalAmount = 2_000_000m,
                OrderStatus = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = new StoreManagerController(db);
        AttachHttpContext(controller, "mgr-1", "mgr@perfumeluxe.vn", "StoreManager");

        var result = Assert.IsType<ViewResult>(await controller.Index(null, null, "today"));
        var vm = Assert.IsType<StoreManagerDashboardViewModel>(result.Model);

        Assert.Equal(3_800_000m, vm.Revenue);
        Assert.Equal(2, vm.OrderCount);
        Assert.Equal(1, vm.PaidOrders);
        Assert.Equal(1, vm.CancelledOrders);
        Assert.Equal(50.0, vm.CancellationRate);
        Assert.Equal(2_000_000m, vm.LostRevenue);
        Assert.Single(vm.LowStock);
        Assert.Single(vm.OutOfStock);
    }

    // ─── TC-T3-016: AdminDashboardController.Index role-based routing ─────────
    [Fact]
    public async Task TC_T3_016_AdminDashboardIndex_RedirectsStoreManagerAndRendersForAdmin()
    {
        using var db = CreateDbContext();

        // StoreManager without Admin role -> redirects to StoreManager/Index
        var storeMgrController = CreateAdminController(db, actorUserId: "mgr-1", roles: "StoreManager");
        var redirect = Assert.IsType<RedirectToActionResult>(await storeMgrController.Index());
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("StoreManager", redirect.ControllerName);

        // Admin role -> renders ViewResult with AdminDashboardViewModel
        var adminController = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");
        var view = Assert.IsType<ViewResult>(await adminController.Index());
        Assert.IsType<AdminDashboardViewModel>(view.Model);
    }

    // ─── TC-T3-017: FlashSaleService.CreateFlashSaleAsync ─────────────────────

}
