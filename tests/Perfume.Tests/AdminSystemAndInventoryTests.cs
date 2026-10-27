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

public sealed class AdminSystemAndInventoryTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_017_FlashSaleService_CreateFlashSaleAsync_PersistsCampaignAndItems()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Chanel", Slug = "chanel" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Bleu de Chanel", Slug = "bleu-de-chanel", Sku = "CH-BDC", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 4_200_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Midnight Luxury Sale",
            Description = "Ưu đãi nửa đêm",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(3),
            IsActive = true,
            Items = new List<CreateFlashSaleItemRequest>
            {
                new() { ProductId = 1, FlashSalePrice = 3_360_000m, StockLimit = 15 }
            }
        });

        Assert.True(created.Id > 0);
        Assert.Equal("Midnight Luxury Sale", created.Name);
        Assert.True(created.IsOngoing);
        var item = Assert.Single(created.Items);
        Assert.Equal(3_360_000m, item.FlashSalePrice);
        Assert.Equal(4_200_000m, item.OriginalPrice);
        Assert.Equal(20, item.DiscountPercent);
        Assert.Equal(15, item.RemainingQuantity);
    }

    // ─── TC-T3-018: FlashSaleService.GetAllFlashSalesAsync & GetFlashSaleById ─
    [Fact]
    public async Task TC_T3_018_FlashSaleService_GetAllAndGetById_ReturnOrderedCampaigns()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);

        var fs1 = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Sale 1",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });
        var fs2 = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Sale 2",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = false
        });

        var all = await service.GetAllFlashSalesAsync();
        Assert.Equal(2, all.Count);

        var fetched = await service.GetFlashSaleByIdAsync(fs1.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Sale 1", fetched!.Name);
        Assert.Null(await service.GetFlashSaleByIdAsync(99999));
    }

    // ─── TC-T3-019: FlashSaleService.ToggleStatusAsync ────────────────────────
    [Fact]
    public async Task TC_T3_019_FlashSaleService_ToggleStatusAsync_FlipsActiveStateAndHandlesMissingId()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Toggle Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });

        Assert.True(await service.ToggleStatusAsync(created.Id));
        var afterFirstToggle = await service.GetFlashSaleByIdAsync(created.Id);
        Assert.False(afterFirstToggle!.IsActive);

        Assert.True(await service.ToggleStatusAsync(created.Id));
        var afterSecondToggle = await service.GetFlashSaleByIdAsync(created.Id);
        Assert.True(afterSecondToggle!.IsActive);

        Assert.False(await service.ToggleStatusAsync(88888));
    }

    // ─── TC-T3-020: FlashSaleService.DeleteAsync soft delete ──────────────────
    [Fact]
    public async Task TC_T3_020_FlashSaleService_DeleteAsync_SoftDeletesAndExcludesFromQueries()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Delete Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });

        Assert.True(await service.DeleteAsync(created.Id));
        Assert.Empty(await service.GetAllFlashSalesAsync());
        Assert.Null(await service.GetActiveFlashSaleAsync());
        Assert.False(await service.DeleteAsync(77777));
    }

    // ─── TC-T3-021: AdminDashboardController.CreateFlashSale validation ───────
    [Fact]
    public async Task TC_T3_021_AdminDashboard_CreateFlashSale_ValidatesDatesAndApplies80PercentFallbackPrice()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Amouage", Slug = "amouage" };
        var cat = new Category { Id = 1, Name = "Parfum", Slug = "parfum" };
        var prod = new Product { Id = 1, Name = "Reflection Man", Slug = "reflection-man", Sku = "AMG-RM", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 7_500_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        // Invalid: empty name
        await controller.CreateFlashSale("", "desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), true, Array.Empty<int>(), Array.Empty<decimal>(), Array.Empty<int>());
        Assert.NotNull(controller.TempData["Error"]);

        // Invalid: endTime <= startTime
        var now = DateTime.UtcNow;
        await controller.CreateFlashSale("Bad Time", "desc", now, now.AddHours(-1), true, Array.Empty<int>(), Array.Empty<decimal>(), Array.Empty<int>());
        Assert.Contains("Thời gian kết thúc", controller.TempData["Error"]?.ToString());

        // Valid with explicit flashSalePrice and 0 stockLimit -> falls back to StockLimit = 10
        await controller.CreateFlashSale("Amouage Flash", "desc", now.AddMinutes(-5), now.AddHours(4), true, new[] { 1 }, new[] { 6_000_000m }, new[] { 0 });
        var saved = await db.FlashSales.Include(f => f.Items).FirstOrDefaultAsync(f => f.Name == "Amouage Flash");
        Assert.NotNull(saved);
        Assert.Equal(6_000_000m, saved!.Items.First().FlashSalePrice);
        Assert.Equal(10, saved.Items.First().StockLimit);
    }

    // ─── TC-T3-022: SystemSettingService.GetSettingsAsync fallback defaults ───
    [Fact]
    public async Task TC_T3_022_SystemSettingService_GetSettingsAsync_ReturnsConfigurationFallbackWhenDbEmpty()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var settings = await service.GetSettingsAsync();

        Assert.Equal("PERFUME LUXE", settings.StoreName);
        Assert.Equal(2_000_000m, settings.FreeShippingThreshold);
        Assert.Equal(35_000m, settings.DefaultShippingFee);
    }

    // ─── TC-T3-023: SystemSettingService.SaveSettingsAsync persistence ────────
    [Fact]
    public async Task TC_T3_023_SystemSettingService_SaveSettingsAsync_CreatesAndUpdatesStoreConfiguration()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var custom = new SystemSettingsDto
        {
            StoreName = "PERFUME LUXE PARIS",
            Hotline = "1800 9999",
            FreeShippingThreshold = 1_500_000m,
            DefaultShippingFee = 40_000m,
            SmtpEnabled = false
        };

        await service.SaveSettingsAsync(custom);
        var loaded1 = await service.GetSettingsAsync();
        Assert.Equal("PERFUME LUXE PARIS", loaded1.StoreName);
        Assert.Equal(1_500_000m, loaded1.FreeShippingThreshold);

        // Update existing setting record
        custom.StoreName = "PERFUME LUXE MILANO";
        await service.SaveSettingsAsync(custom);
        Assert.Equal(1, await db.SystemSettings.CountAsync());
        var loaded2 = await service.GetSettingsAsync();
        Assert.Equal("PERFUME LUXE MILANO", loaded2.StoreName);
    }

    // ─── TC-T3-024: SystemSettingService.TestSmtpConnectionAsync when disabled ─
    [Fact]
    public async Task TC_T3_024_SystemSettingService_TestSmtpConnectionAsync_ReturnsFalseWhenSmtpDisabled()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration(new Dictionary<string, string?> { ["Smtp:Enabled"] = "false" });
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var (success, message) = await service.TestSmtpConnectionAsync("test@perfumeluxe.vn");
        Assert.False(success);
        Assert.Contains("SMTP", message);
    }

    // ─── TC-T3-025: HeroBanner create & update via AdminDashboardController ───
    [Fact]
    public async Task TC_T3_025_AdminDashboard_SaveBanner_CreatesAndUpdatesHeroBanner()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        // Create banner with default TargetUrl & ButtonText
        await controller.SaveBanner(null, "Bộ Sưu Tập Thu Đông", "Hương gỗ trầm ấm", "https://cdn.test/banner1.jpg", null, null, 1, true);
        var created = await db.HeroBanners.FirstOrDefaultAsync();
        Assert.NotNull(created);
        Assert.Equal("Bộ Sưu Tập Thu Đông", created!.Title);
        Assert.Equal("/bo-suu-tap", created.TargetUrl);
        Assert.Equal("Khám Phá Ngay", created.ButtonText);

        // Update banner
        await controller.SaveBanner(created.Id, "Bộ Sưu Tập Mùa Lễ Hội", "Limited Edition", "https://cdn.test/banner2.jpg", "/flash-sale", "Mua Ngay", 2, true);
        var updated = await db.HeroBanners.FindAsync(created.Id);
        Assert.Equal("Bộ Sưu Tập Mùa Lễ Hội", updated!.Title);
        Assert.Equal("/flash-sale", updated.TargetUrl);
        Assert.Equal("Mua Ngay", updated.ButtonText);
        Assert.Equal(2, updated.DisplayOrder);
    }

    // ─── TC-T3-026: HeroBanner toggle active & delete ─────────────────────────
    [Fact]
    public async Task TC_T3_026_AdminDashboard_ToggleAndDeleteBanner_UpdatesAndRemovesEntity()
    {
        using var db = CreateDbContext();
        var banner = new HeroBanner { Title = "Banner Test", ImageUrl = "https://cdn.test/b.jpg", IsActive = true };
        db.HeroBanners.Add(banner);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.ToggleBannerActive(banner.Id);
        var toggled = await db.HeroBanners.FindAsync(banner.Id);
        Assert.False(toggled!.IsActive);

        await controller.DeleteBanner(banner.Id);
        Assert.Equal(0, await db.HeroBanners.CountAsync());
    }

    // ─── TC-T3-027: AdjustStock positive restock & Product.IsInStock sync ─────
    [Fact]
    public async Task TC_T3_027_AdminDashboard_AdjustStock_PositiveRestockUpdatesStockRecordsUnitCostAndSyncsProductInStock()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Diptyque", Slug = "diptyque" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Fleur de Peau", Slug = "fleur-de-peau", Sku = "DIP-FDP", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 4_800_000m, IsInStock = false };
        var variant = new ProductVariant { Id = 10, ProductId = 1, Product = prod, VolumeMl = 75, VolumeLabel = "75ml", VariantSku = "DIP-FDP-75", OriginalPrice = 4_800_000m, SalePrice = 4_800_000m, StockQuantity = 0 };
        prod.Variants.Add(variant);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.AdjustStock(10, 12, 2_100_000m, "Nhập hàng Pháp", "Lô 2026-10");

        var updatedVariant = await db.ProductVariants.FindAsync(10);
        var updatedProduct = await db.Products.FindAsync(1);
        var adjustment = await db.StockAdjustments.FirstOrDefaultAsync(a => a.VariantId == 10);

        Assert.Equal(12, updatedVariant!.StockQuantity);
        Assert.True(updatedProduct!.IsInStock);
        Assert.NotNull(adjustment);
        Assert.Equal(0, adjustment!.OldQuantity);
        Assert.Equal(12, adjustment.AdjustedQuantity);
        Assert.Equal(12, adjustment.NewQuantity);
        Assert.Equal(2_100_000m, adjustment.UnitCost);
        Assert.Equal("Nhập hàng Pháp", adjustment.Reason);
    }

    // ─── TC-T3-028: AdjustStock negative deduction clamps to 0 & syncs out-of-stock ─
    [Fact]
    public async Task TC_T3_028_AdminDashboard_AdjustStock_NegativeDepletionClampsZeroAndMarksProductOutOfStock()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Kilian", Slug = "kilian" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Angels Share", Slug = "angels-share", Sku = "KIL-AS", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 6_200_000m, IsInStock = true };
        var variant = new ProductVariant { Id = 20, ProductId = 1, Product = prod, VolumeMl = 50, VolumeLabel = "50ml", VariantSku = "KIL-AS-50", OriginalPrice = 6_200_000m, SalePrice = 6_200_000m, StockQuantity = 4 };
        prod.Variants.Add(variant);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.AdjustStock(20, -10, null, "Kiểm kê hao hụt", "Vỡ khi vận chuyển");

        var updatedVariant = await db.ProductVariants.FindAsync(20);
        var updatedProduct = await db.Products.FindAsync(1);

        Assert.Equal(0, updatedVariant!.StockQuantity);
        Assert.False(updatedProduct!.IsInStock);
    }

    // ─── TC-T3-029: AdjustStock validation for 0 quantity & missing variant ───
    [Fact]
    public async Task TC_T3_029_AdminDashboard_AdjustStock_RejectsZeroQuantityAndMissingVariant()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        await controller.AdjustStock(1, 0, null, "Test", null);
        Assert.Contains("khác 0", controller.TempData["Error"]?.ToString());

        await controller.AdjustStock(9999, 5, 1_000_000m, "Test", null);
        Assert.Contains("Không tìm thấy", controller.TempData["Error"]?.ToString());
        Assert.Equal(0, await db.StockAdjustments.CountAsync());
    }

    // ─── TC-T3-030: Inventory action filters (all, low, out) & search ─────────

}
