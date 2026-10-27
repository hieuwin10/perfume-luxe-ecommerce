using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Perfume.Web.Controllers;
using Xunit;

namespace Perfume.Tests;

public sealed class FlashSaleTests : TestFixtures
{
    [Fact]
    public async Task TC_T1_012_ProductCardDto_MapsTop3TopNotesOnly()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product
        {
            Id = 25,
            Name = "Top Notes Card Test",
            Slug = "top-notes-card",
            Sku = "TNC-25",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 4_000_000m,
            IsFeatured = true
        };
        for (int i = 1; i <= 4; i++)
        {
            var note = new FragranceNote { Id = i, Name = $"TopNote{i}", Slug = $"top-note-{i}" };
            db.FragranceNotes.Add(note);
            p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 25, FragranceNoteId = i, FragranceNote = note, NoteLevel = NoteLevel.Top });
        }
        var heartNote = new FragranceNote { Id = 5, Name = "HeartNote", Slug = "heart-note" };
        db.FragranceNotes.Add(heartNote);
        p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 25, FragranceNoteId = 5, FragranceNote = heartNote, NoteLevel = NoteLevel.Heart });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var cards = await service.GetFeaturedProductsAsync();

        Assert.Single(cards);
        Assert.Equal(3, cards[0].TopNotes.Count);
        Assert.DoesNotContain("HeartNote", cards[0].TopNotes);
    }

    [Fact]
    public async Task TC_T1_013_FlashSale_GetActiveFlashSaleAsync_ReturnsOngoingCampaignWithCountdown()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var product = new Product { Id = 1, Name = "Baccarat Rouge 540", Slug = "br-540", Sku = "BR540", BrandId = b.Id, CategoryId = c.Id, BasePrice = 9_000_000m };
        var fs = new FlashSale
        {
            Id = 1,
            Name = "Golden Hour Flash Sale",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { Id = 1, ProductId = 1, Product = product, FlashSalePrice = 7_200_000m, StockLimit = 10, SoldQuantity = 3 }
            }
        };
        db.Products.Add(product);
        db.FlashSales.Add(fs);
        await db.SaveChangesAsync();

        var service = new FlashSaleService(db);
        var active = await service.GetActiveFlashSaleAsync();

        Assert.NotNull(active);
        Assert.True(active.IsOngoing);
        Assert.True((active.EndTime - DateTime.UtcNow).TotalSeconds > 3600);
        Assert.Single(active.Items);
        Assert.Equal(7, active.Items[0].RemainingQuantity);
        Assert.Equal(20, active.Items[0].DiscountPercent);
    }

    [Fact]
    public async Task TC_T1_014_FlashSale_MultipleActiveCampaigns_SelectsEarliestEnding()
    {
        using var db = CreateDb();
        db.FlashSales.AddRange(
            new FlashSale { Id = 1, Name = "Later Sale", StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddHours(6), IsActive = true },
            new FlashSale { Id = 2, Name = "Sooner Sale", StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddHours(1), IsActive = true }
        );
        await db.SaveChangesAsync();

        var service = new FlashSaleService(db);
        var active = await service.GetActiveFlashSaleAsync();

        Assert.NotNull(active);
        Assert.Equal("Sooner Sale", active.Name);
    }

    [Fact]
    public void TC_T1_015_FlashSaleItemDto_DiscountRoundingAndZeroPriceSafety()
    {
        var dtoRounded = new FlashSaleItemDto { OriginalPrice = 8_500_000m, FlashSalePrice = 6_500_000m, StockLimit = 10, SoldQuantity = 4 };
        // (1 - 6.5/8.5) * 100 = 23.529% -> rounds to 24%
        Assert.Equal(24, dtoRounded.DiscountPercent);
        Assert.Equal(6, dtoRounded.RemainingQuantity);

        var dtoZero = new FlashSaleItemDto { OriginalPrice = 0m, FlashSalePrice = 1_000_000m, StockLimit = 5, SoldQuantity = 10 };
        Assert.Equal(0, dtoZero.DiscountPercent);
        Assert.Equal(0, dtoZero.RemainingQuantity);
    }

    [Fact]
    public async Task TC_T1_016_FlashSale_EnrichesProductCardAndProductDetailBadges()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Kilian Angels Share", Slug = "kilian-angels-share", Sku = "KIL-AS", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_000_000m, IsFeatured = true };
        var fs = new FlashSale
        {
            Id = 1,
            Name = "Midnight Sale",
            StartTime = DateTime.UtcNow.AddMinutes(-15),
            EndTime = DateTime.UtcNow.AddHours(3),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 4_500_000m, StockLimit = 12, SoldQuantity = 5 }
            }
        };
        db.Products.Add(p);
        db.FlashSales.Add(fs);
        await db.SaveChangesAsync();

        var catalog = new PerfumeCatalogService(db);
        var detail = await catalog.GetProductBySlugAsync("kilian-angels-share");
        var cards = await catalog.GetFeaturedProductsAsync();

        Assert.NotNull(detail);
        Assert.True(detail.IsFlashSale);
        Assert.Equal(4_500_000m, detail.FlashSalePrice);
        Assert.Equal(25, detail.FlashSaleDiscountPercent);
        Assert.Equal(12, detail.FlashSaleStockLimit);
        Assert.Equal(5, detail.FlashSaleSoldQuantity);

        Assert.Single(cards);
        Assert.True(cards[0].IsFlashSale);
        Assert.Equal(4_500_000m, cards[0].FlashSalePrice);
        Assert.Equal(25, cards[0].FlashSaleDiscountPercent);
    }

    [Fact]
    public async Task TC_T1_017_FlashSale_ExpiredCampaign_FallsBackToBasePrice()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Tom Ford Oud Wood", Slug = "tf-oud-wood", Sku = "TF-OW", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_000_000m, IsFeatured = true };
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "Expired Campaign",
            StartTime = DateTime.UtcNow.AddHours(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 5_000_000m, StockLimit = 10, SoldQuantity = 2 } }
        });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var catalog = new PerfumeCatalogService(db);

        Assert.Null(await fsService.GetActiveFlashSaleAsync());
        var detail = await catalog.GetProductBySlugAsync("tf-oud-wood");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);
        Assert.Equal(7_000_000m, detail.BasePrice);
    }

    [Fact]
    public async Task TC_T1_018_FlashSale_FutureStartTime_IsNotActiveYet()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Roja Parfums", Slug = "roja-parfums", Sku = "ROJ-01", BrandId = b.Id, CategoryId = c.Id, BasePrice = 10_000_000m };
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "Tomorrow Sale",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(10),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 8_000_000m, StockLimit = 5, SoldQuantity = 0 } }
        });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var catalog = new PerfumeCatalogService(db);

        Assert.Null(await fsService.GetActiveFlashSaleAsync());
        var detail = await catalog.GetProductBySlugAsync("roja-parfums");
        Assert.False(detail!.IsFlashSale);
    }

    [Fact]
    public async Task TC_T1_019_FlashSale_ToggleStatusAsync_DisablesAndReEnablesPromotion()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Byredo Gypsy Water", Slug = "byredo-gypsy-water", Sku = "BYR-GW", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_200_000m };
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "Toggle Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-20),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 4_160_000m, StockLimit = 10, SoldQuantity = 1 } }
        });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var catalog = new PerfumeCatalogService(db);

        Assert.True(await fsService.ToggleStatusAsync(1));
        Assert.Null(await fsService.GetActiveFlashSaleAsync());
        Assert.False((await catalog.GetProductBySlugAsync("byredo-gypsy-water"))!.IsFlashSale);

        Assert.True(await fsService.ToggleStatusAsync(1));
        Assert.NotNull(await fsService.GetActiveFlashSaleAsync());
        Assert.True((await catalog.GetProductBySlugAsync("byredo-gypsy-water"))!.IsFlashSale);
    }

    [Fact]
    public async Task TC_T1_020_FlashSale_DeleteAsync_SoftDeletesAndExcludesCampaign()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Diptyque Tam Dao", Slug = "diptyque-tam-dao", Sku = "DIP-TD", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_200_000m };
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "To Be Deleted",
            StartTime = DateTime.UtcNow.AddMinutes(-20),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 3_500_000m, StockLimit = 10, SoldQuantity = 0 } }
        });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var catalog = new PerfumeCatalogService(db);

        Assert.True(await fsService.DeleteAsync(1));
        Assert.Null(await fsService.GetActiveFlashSaleAsync());
        Assert.Empty(await fsService.GetAllFlashSalesAsync());
        Assert.False((await catalog.GetProductBySlugAsync("diptyque-tam-dao"))!.IsFlashSale);
    }

    [Fact]
    public async Task TC_T1_021_FlashSale_StockExhausted_SoldQuantityEqualsStockLimit_RevertsToBasePrice()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Parfums de Marly Layton", Slug = "pdm-layton", Sku = "PDM-LT", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_200_000m, IsFeatured = true };
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "Sold Out Flash Item",
            StartTime = DateTime.UtcNow.AddMinutes(-20),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, FlashSalePrice = 4_800_000m, StockLimit = 5, SoldQuantity = 5 } }
        });
        await db.SaveChangesAsync();

        var catalog = new PerfumeCatalogService(db);
        var fsService = new FlashSaleService(db);

        var detail = await catalog.GetProductBySlugAsync("pdm-layton");
        var cards = await catalog.GetFeaturedProductsAsync();
        var activeItem = await fsService.GetActiveFlashSaleItemForProductAsync(1);

        Assert.False(detail!.IsFlashSale);
        Assert.False(cards[0].IsFlashSale);
        Assert.Null(activeItem);
    }

    [Fact]
    public async Task TC_T1_022_FlashSale_CreateAndGetByIdAsync_PersistsCampaignAndItems()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Amouage Reflection", Slug = "amouage-reflection", Sku = "AMO-RF", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_500_000m });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var created = await fsService.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Royal Gala Flash",
            Description = "Ưu đãi hoàng gia",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(4),
            IsActive = true,
            BannerUrl = "/banners/gala.jpg",
            Items = new List<CreateFlashSaleItemRequest>
            {
                new() { ProductId = 1, FlashSalePrice = 6_000_000m, StockLimit = 15 }
            }
        });

        Assert.Equal("Royal Gala Flash", created.Name);
        Assert.Single(created.Items);
        Assert.Equal(20, created.Items[0].DiscountPercent);
        Assert.Equal(15, created.Items[0].RemainingQuantity);
    }


}
