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

public class Team1_StorefrontAndCommerceTests
{
    private static PerfumeDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase($"Team1Db_{Guid.NewGuid():N}")
            .Options;
        return new PerfumeDbContext(options);
    }

    private static (Brand Brand, Category Category, FragranceFamily Family) SeedCoreTaxonomy(
        PerfumeDbContext db,
        int id = 1,
        string brandName = "Creed",
        string brandSlug = "creed",
        string catName = "Niche Luxury",
        string catSlug = "niche-luxury",
        string famName = "Gỗ Hoàng Gia",
        string famSlug = "go-hoang-gia",
        ScentFamily scentType = ScentFamily.Woody)
    {
        var brand = new Brand { Id = id, Name = brandName, Slug = brandSlug, Country = "France", IsFeatured = true };
        var category = new Category { Id = id, Name = catName, Slug = catSlug, IsFeatured = true };
        var family = new FragranceFamily { Id = id, Name = famName, Slug = famSlug, ColorHex = "#D4AF37", ScentType = scentType };
        db.Brands.Add(brand);
        db.Categories.Add(category);
        db.FragranceFamilies.Add(family);
        return (brand, category, family);
    }

    private sealed class NoOpOrderEmailSender : IOrderEmailSender
    {
        public string? LastOrderCode { get; private set; }
        public string? LastTrackingUrl { get; private set; }

        public Task SendOrderConfirmationAsync(Order order, string? guestTrackingUrl = null)
        {
            LastOrderCode = order.OrderCode;
            LastTrackingUrl = guestTrackingUrl;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string fullName, string resetUrl)
        {
            return Task.CompletedTask;
        }
    }

    // =========================================================================
    // PHẦN 1: STOREFRONT UI, CATALOG, FLASH SALE, AI SCENT QUIZ & BLOG (TC-T1-001 -> TC-T1-050)
    // =========================================================================

    [Fact]
    public async Task TC_T1_001_GetFeaturedProductsAsync_ReturnsOnlyFeaturedCappedAt8()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        for (int i = 1; i <= 10; i++)
        {
            db.Products.Add(new Product
            {
                Id = i,
                Name = $"Featured {i}",
                Slug = $"featured-{i}",
                Sku = $"SKU-F-{i}",
                BrandId = b.Id,
                CategoryId = c.Id,
                FragranceFamilyId = f.Id,
                BasePrice = 5_000_000m,
                IsFeatured = i <= 9
            });
        }
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var result = await service.GetFeaturedProductsAsync();

        Assert.Equal(8, result.Count);
        Assert.All(result, p => Assert.StartsWith("Featured ", p.Name));
    }

    [Fact]
    public async Task TC_T1_002_GetBestSellersAsync_ReturnsOnlyBestSellersCappedAt8()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        for (int i = 1; i <= 10; i++)
        {
            db.Products.Add(new Product
            {
                Id = i,
                Name = $"BestSeller {i}",
                Slug = $"best-{i}",
                Sku = $"SKU-B-{i}",
                BrandId = b.Id,
                CategoryId = c.Id,
                FragranceFamilyId = f.Id,
                BasePrice = 6_000_000m,
                IsBestSeller = i <= 9
            });
        }
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var result = await service.GetBestSellersAsync();

        Assert.Equal(8, result.Count);
        Assert.All(result, p => Assert.True(p.IsBestSeller));
    }

    [Fact]
    public async Task TC_T1_003_GetNewArrivalsAsync_ReturnsOnlyNewArrivalsCappedAt8()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        for (int i = 1; i <= 10; i++)
        {
            db.Products.Add(new Product
            {
                Id = i,
                Name = $"NewArrival {i}",
                Slug = $"new-{i}",
                Sku = $"SKU-N-{i}",
                BrandId = b.Id,
                CategoryId = c.Id,
                FragranceFamilyId = f.Id,
                BasePrice = 4_500_000m,
                IsNewArrival = i <= 5
            });
        }
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var result = await service.GetNewArrivalsAsync();

        Assert.Equal(5, result.Count);
        Assert.All(result, p => Assert.True(p.IsNewArrival));
    }

    [Fact]
    public async Task TC_T1_004_GetProductBySlugAsync_ReturnsCompleteProductDetailMetadata()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product
        {
            Id = 10,
            Name = "Creed Aventus EDP",
            Slug = "creed-aventus-edp",
            Sku = "CRD-AV-100",
            ShortDescription = "Biểu tượng quyền lực vương giả",
            FullDescription = "Hương dứa nướng quyện khói bạch dương quý phái.",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            Concentration = Concentration.EDP,
            Gender = GenderTarget.Men,
            Longevity = "8 - 12 giờ",
            Sillage = "Toả xa 2 mét",
            ReleaseYear = 2010,
            SeasonRecommendation = "Bốn mùa",
            BasePrice = 8_500_000m,
            MainImageUrl = "/img/aventus.jpg",
            RatingAverage = 4.9,
            RatingCount = 128,
            IsInStock = true,
            MetaTitle = "Creed Aventus Chính Hãng",
            MetaDescription = "Nước hoa nam Creed Aventus EDP"
        });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var dto = await service.GetProductBySlugAsync("creed-aventus-edp");

        Assert.NotNull(dto);
        Assert.Equal("Creed Aventus EDP", dto.Name);
        Assert.Equal("Creed", dto.BrandName);
        Assert.Equal("France", dto.BrandCountry);
        Assert.Equal("Niche Luxury", dto.CategoryName);
        Assert.Equal("Gỗ Hoàng Gia", dto.FamilyName);
        Assert.Equal("#D4AF37", dto.FamilyColor);
        Assert.Equal(2010, dto.ReleaseYear);
        Assert.Equal(8_500_000m, dto.BasePrice);
        Assert.Equal("Creed Aventus Chính Hãng", dto.MetaTitle);
    }

    [Fact]
    public async Task TC_T1_005_GetProductBySlugAsync_ReturnsNullForUnknownOrSoftDeletedProduct()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product
        {
            Id = 11,
            Name = "Deleted Perfume",
            Slug = "deleted-perfume",
            Sku = "DEL-01",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 3_000_000m,
            IsDeleted = true
        });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        Assert.Null(await service.GetProductBySlugAsync("non-existent-slug"));
        Assert.Null(await service.GetProductBySlugAsync("deleted-perfume"));
    }

    [Fact]
    public async Task TC_T1_006_GetProductByIdAsync_ReturnsDetailForValidIdAndNullForMissing()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product
        {
            Id = 42,
            Name = "Le Labo Santal 33",
            Slug = "le-labo-santal-33",
            Sku = "LL-S33",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 7_200_000m
        });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var found = await service.GetProductByIdAsync(42);
        var missing = await service.GetProductByIdAsync(999);

        Assert.NotNull(found);
        Assert.Equal("le-labo-santal-33", found.Slug);
        Assert.Null(missing);
    }

    [Fact]
    public async Task TC_T1_007_GetProductBySlugAsync_MapsThreeTierScentPyramidCorrectly()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var nTop = new FragranceNote { Id = 1, Name = "Cam Bergamot", Slug = "cam-bergamot", Description = "Tươi mát" };
        var nHeart = new FragranceNote { Id = 2, Name = "Hoa Nhài Grasse", Slug = "hoa-nhai", Description = "Nồng nàn" };
        var nBase = new FragranceNote { Id = 3, Name = "Trầm Hương Oud", Slug = "tram-huong", Description = "Sâu lắng" };
        db.FragranceNotes.AddRange(nTop, nHeart, nBase);

        var p = new Product
        {
            Id = 20,
            Name = "Pyramid Test EDP",
            Slug = "pyramid-test",
            Sku = "PYR-20",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 5_500_000m
        };
        p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 20, FragranceNoteId = 1, FragranceNote = nTop, NoteLevel = NoteLevel.Top });
        p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 20, FragranceNoteId = 2, FragranceNote = nHeart, NoteLevel = NoteLevel.Heart });
        p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 20, FragranceNoteId = 3, FragranceNote = nBase, NoteLevel = NoteLevel.Base });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var dto = await service.GetProductBySlugAsync("pyramid-test");

        Assert.NotNull(dto);
        Assert.Single(dto.TopNotes);
        Assert.Equal("Cam Bergamot", dto.TopNotes[0].Name);
        Assert.Single(dto.HeartNotes);
        Assert.Equal("Hoa Nhài Grasse", dto.HeartNotes[0].Name);
        Assert.Single(dto.BaseNotes);
        Assert.Equal("Trầm Hương Oud", dto.BaseNotes[0].Name);
    }

    [Fact]
    public async Task TC_T1_008_GetProductBySlugAsync_OrdersVariantsByVolumeMlAscending()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product
        {
            Id = 21,
            Name = "Variant Order Perfume",
            Slug = "variant-order",
            Sku = "VO-21",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 6_000_000m
        };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 21, VolumeMl = 100, VolumeLabel = "100ml Full", OriginalPrice = 6_000_000m, SalePrice = 5_800_000m, StockQuantity = 5, IsDefault = true });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 21, VolumeMl = 10, VolumeLabel = "10ml Travel", OriginalPrice = 850_000m, SalePrice = 0m, StockQuantity = 20, IsDefault = false });
        p.Variants.Add(new ProductVariant { Id = 3, ProductId = 21, VolumeMl = 50, VolumeLabel = "50ml Standard", OriginalPrice = 4_200_000m, SalePrice = 3_900_000m, StockQuantity = 8, IsDefault = false });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var dto = await service.GetProductBySlugAsync("variant-order");

        Assert.NotNull(dto);
        Assert.Equal(3, dto.Variants.Count);
        Assert.Equal(new[] { 10, 50, 100 }, dto.Variants.Select(v => v.VolumeMl).ToArray());
        Assert.True(dto.Variants[2].IsDefault);
    }

    [Fact]
    public async Task TC_T1_009_GetProductBySlugAsync_OrdersGalleryImagesByDisplayOrder()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product
        {
            Id = 22,
            Name = "Gallery Order Perfume",
            Slug = "gallery-order",
            Sku = "GO-22",
            BrandId = b.Id,
            CategoryId = c.Id,
            FragranceFamilyId = f.Id,
            BasePrice = 5_000_000m
        };
        p.Images.Add(new ProductImage { Id = 1, ProductId = 22, ImageUrl = "/img/third.jpg", DisplayOrder = 3 });
        p.Images.Add(new ProductImage { Id = 2, ProductId = 22, ImageUrl = "/img/first.jpg", DisplayOrder = 1 });
        p.Images.Add(new ProductImage { Id = 3, ProductId = 22, ImageUrl = "/img/second.jpg", DisplayOrder = 2 });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var dto = await service.GetProductBySlugAsync("gallery-order");

        Assert.NotNull(dto);
        Assert.Equal(new[] { "/img/first.jpg", "/img/second.jpg", "/img/third.jpg" }, dto.GalleryImages.ToArray());
    }

    [Fact]
    public async Task TC_T1_010_GetBrandsAndCategoriesAsync_ReturnsAlphabeticalAndExcludesDeleted()
    {
        using var db = CreateDb();
        db.Brands.AddRange(
            new Brand { Id = 1, Name = "Xerjoff", Slug = "xerjoff" },
            new Brand { Id = 2, Name = "Amouage", Slug = "amouage" },
            new Brand { Id = 3, Name = "Deleted Brand", Slug = "del-brand", IsDeleted = true }
        );
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Unisex Niche", Slug = "unisex-niche" },
            new Category { Id = 2, Name = "Gift Sets", Slug = "gift-sets" },
            new Category { Id = 3, Name = "Deleted Cat", Slug = "del-cat", IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var brands = await service.GetBrandsAsync();
        var categories = await service.GetCategoriesAsync();

        Assert.Equal(new[] { "Amouage", "Xerjoff" }, brands.Select(b => b.Name).ToArray());
        Assert.Equal(new[] { "Gift Sets", "Unisex Niche" }, categories.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task TC_T1_011_GetFragranceFamiliesAndNotesAsync_ReturnsAlphabeticalActiveEntities()
    {
        using var db = CreateDb();
        db.FragranceFamilies.AddRange(
            new FragranceFamily { Id = 1, Name = "Woody", Slug = "woody", ScentType = ScentFamily.Woody },
            new FragranceFamily { Id = 2, Name = "Citrus", Slug = "citrus", ScentType = ScentFamily.Citrus }
        );
        db.FragranceNotes.AddRange(
            new FragranceNote { Id = 1, Name = "Vetiver", Slug = "vetiver" },
            new FragranceNote { Id = 2, Name = "Ambergris", Slug = "ambergris" }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var families = await service.GetFragranceFamiliesAsync();
        var notes = await service.GetFragranceNotesAsync();

        Assert.Equal(new[] { "Citrus", "Woody" }, families.Select(f => f.Name).ToArray());
        Assert.Equal(new[] { "Ambergris", "Vetiver" }, notes.Select(n => n.Name).ToArray());
    }

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

    [Fact]
    public async Task TC_T1_023_FlashSale_GetActiveFlashSaleItemForProductAsync_ResolvesVariantOriginalPrice()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Initio Oud for Greatness", Slug = "initio-ofg", Sku = "INI-OFG", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_800_000m };
        var v = new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 90, VolumeLabel = "90ml EDP", OriginalPrice = 8_000_000m, SalePrice = 7_800_000m, StockQuantity = 10 };
        p.Variants.Add(v);
        db.Products.Add(p);
        db.FlashSales.Add(new FlashSale
        {
            Id = 1,
            Name = "Initio Flash",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem> { new() { Id = 1, ProductId = 1, Product = p, VariantId = 10, Variant = v, FlashSalePrice = 6_400_000m, StockLimit = 8, SoldQuantity = 2 } }
        });
        await db.SaveChangesAsync();

        var fsService = new FlashSaleService(db);
        var itemDto = await fsService.GetActiveFlashSaleItemForProductAsync(1, 10);

        Assert.NotNull(itemDto);
        Assert.Equal(8_000_000m, itemDto.OriginalPrice);
        Assert.Equal(6_400_000m, itemDto.FlashSalePrice);
        Assert.Equal(20, itemDto.DiscountPercent);
    }

    [Fact]
    public async Task TC_T1_024_CatalogFilter_KeywordSearch_MatchesNameBrandAndShortDescription()
    {
        using var db = CreateDb();
        var (b1, c, f) = SeedCoreTaxonomy(db, 1, "Maison Francis Kurkdjian", "mfk");
        var b2 = new Brand { Id = 2, Name = "Xerjoff", Slug = "xerjoff" };
        db.Brands.Add(b2);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Grand Soir", Slug = "grand-soir", Sku = "GS-1", BrandId = 1, CategoryId = c.Id, ShortDescription = "Hổ phách vani ấm áp", BasePrice = 5_500_000m },
            new Product { Id = 2, Name = "Naxos 1861", Slug = "naxos", Sku = "NX-2", BrandId = 2, CategoryId = c.Id, ShortDescription = "Mật ong thuốc lá Sicily", BasePrice = 5_800_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (byName, c1) = await service.GetProductsAsync(new ProductFilterParams { Keyword = "  Grand Soir  " });
        var (byBrand, c2) = await service.GetProductsAsync(new ProductFilterParams { Keyword = "Kurkdjian" });
        var (byDesc, c3) = await service.GetProductsAsync(new ProductFilterParams { Keyword = "Sicily" });

        Assert.Equal(1, c1);
        Assert.Equal("grand-soir", byName[0].Slug);
        Assert.Equal(1, c2);
        Assert.Equal("grand-soir", byBrand[0].Slug);
        Assert.Equal(1, c3);
        Assert.Equal("naxos", byDesc[0].Slug);
    }

    [Fact]
    public async Task TC_T1_025_CatalogFilter_BrandSlug_FiltersExactBrand()
    {
        using var db = CreateDb();
        var (b1, c, f) = SeedCoreTaxonomy(db, 1, "Creed", "creed");
        var b2 = new Brand { Id = 2, Name = "Kilian", Slug = "kilian" };
        db.Brands.Add(b2);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Silver Mountain Water", Slug = "smw", Sku = "SMW", BrandId = 1, CategoryId = c.Id, BasePrice = 7_000_000m },
            new Product { Id = 2, Name = "Black Phantom", Slug = "black-phantom", Sku = "BP", BrandId = 2, CategoryId = c.Id, BasePrice = 6_500_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { BrandSlug = "kilian" });

        Assert.Equal(1, count);
        Assert.Equal("Black Phantom", items[0].Name);
    }

    [Fact]
    public async Task TC_T1_026_CatalogFilter_CategorySlug_FiltersExactCategory()
    {
        using var db = CreateDb();
        var (b, c1, f) = SeedCoreTaxonomy(db, 1, "Creed", "creed", "Niche", "niche");
        var c2 = new Category { Id = 2, Name = "Discovery", Slug = "discovery" };
        db.Categories.Add(c2);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Full Bottle", Slug = "full", Sku = "FB", BrandId = b.Id, CategoryId = 1, BasePrice = 6_000_000m },
            new Product { Id = 2, Name = "Vial Set", Slug = "vial", Sku = "VS", BrandId = b.Id, CategoryId = 2, BasePrice = 950_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { CategorySlug = "discovery" });

        Assert.Equal(1, count);
        Assert.Equal("Vial Set", items[0].Name);
    }

    [Fact]
    public async Task TC_T1_027_CatalogFilter_FamilySlug_FiltersExactFragranceFamily()
    {
        using var db = CreateDb();
        var (b, c, f1) = SeedCoreTaxonomy(db, 1, "Creed", "creed", "Niche", "niche", "Woody", "woody", ScentFamily.Woody);
        var f2 = new FragranceFamily { Id = 2, Name = "Floral", Slug = "floral", ScentType = ScentFamily.Floral };
        db.FragranceFamilies.Add(f2);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Royal Oud", Slug = "royal-oud", Sku = "RO", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = 1, BasePrice = 8_000_000m },
            new Product { Id = 2, Name = "Love in White", Slug = "love-in-white", Sku = "LIW", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = 2, BasePrice = 6_800_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { FamilySlug = "floral" });

        Assert.Equal(1, count);
        Assert.Equal("Love in White", items[0].Name);
    }

    [Fact]
    public async Task TC_T1_028_CatalogFilter_NoteId_ReturnsOnlyProductsContainingNote()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var n1 = new FragranceNote { Id = 10, Name = "Oud", Slug = "oud" };
        var n2 = new FragranceNote { Id = 20, Name = "Rose", Slug = "rose" };
        db.FragranceNotes.AddRange(n1, n2);

        var p1 = new Product { Id = 1, Name = "Oud Satin Mood", Slug = "osm", Sku = "OSM", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_900_000m };
        p1.FragranceNotes.Add(new ProductFragranceNote { ProductId = 1, FragranceNoteId = 10, FragranceNote = n1, NoteLevel = NoteLevel.Base });
        var p2 = new Product { Id = 2, Name = "Aqua Universalis", Slug = "au", Sku = "AU", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_900_000m };
        db.Products.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { NoteId = 10 });

        Assert.Equal(1, count);
        Assert.Equal("Oud Satin Mood", items[0].Name);
    }

    [Fact]
    public async Task TC_T1_029_CatalogFilter_GenderMen_IncludesMenAndUnisexExcludesWomen()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Men Scent", Slug = "men-scent", Sku = "M1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Men, BasePrice = 5_000_000m },
            new Product { Id = 2, Name = "Unisex Scent", Slug = "unisex-scent", Sku = "U1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Unisex, BasePrice = 5_000_000m },
            new Product { Id = 3, Name = "Women Scent", Slug = "women-scent", Sku = "W1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Women, BasePrice = 5_000_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { Gender = GenderTarget.Men });

        Assert.Equal(2, count);
        Assert.Contains(items, i => i.Slug == "men-scent");
        Assert.Contains(items, i => i.Slug == "unisex-scent");
        Assert.DoesNotContain(items, i => i.Slug == "women-scent");
    }

    [Fact]
    public async Task TC_T1_030_CatalogFilter_GenderWomen_IncludesWomenAndUnisexExcludesMen()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Men Scent", Slug = "men-scent", Sku = "M1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Men, BasePrice = 5_000_000m },
            new Product { Id = 2, Name = "Unisex Scent", Slug = "unisex-scent", Sku = "U1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Unisex, BasePrice = 5_000_000m },
            new Product { Id = 3, Name = "Women Scent", Slug = "women-scent", Sku = "W1", BrandId = b.Id, CategoryId = c.Id, Gender = GenderTarget.Women, BasePrice = 5_000_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { Gender = GenderTarget.Women });

        Assert.Equal(2, count);
        Assert.Contains(items, i => i.Slug == "women-scent");
        Assert.Contains(items, i => i.Slug == "unisex-scent");
        Assert.DoesNotContain(items, i => i.Slug == "men-scent");
    }

    [Fact]
    public async Task TC_T1_031_CatalogFilter_Concentration_FiltersExactConcentration()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Extrait Edition", Slug = "extrait", Sku = "EX", BrandId = b.Id, CategoryId = c.Id, Concentration = Concentration.Parfum, BasePrice = 9_500_000m },
            new Product { Id = 2, Name = "EDT Edition", Slug = "edt", Sku = "EDT", BrandId = b.Id, CategoryId = c.Id, Concentration = Concentration.EDT, BasePrice = 3_500_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { Concentration = Concentration.Parfum });

        Assert.Equal(1, count);
        Assert.Equal("Extrait Edition", items[0].Name);
    }

    [Fact]
    public async Task TC_T1_032_CatalogFilter_MinAndMaxPriceBoundaries_InclusiveFiltering()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Low", Slug = "low", Sku = "P1", BrandId = b.Id, CategoryId = c.Id, BasePrice = 2_000_000m },
            new Product { Id = 2, Name = "MidBoundaryLow", Slug = "mid-low", Sku = "P2", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_000_000m },
            new Product { Id = 3, Name = "MidBoundaryHigh", Slug = "mid-high", Sku = "P3", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_000_000m },
            new Product { Id = 4, Name = "High", Slug = "high", Sku = "P4", BrandId = b.Id, CategoryId = c.Id, BasePrice = 12_000_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (items, count) = await service.GetProductsAsync(new ProductFilterParams { MinPrice = 4_000_000m, MaxPrice = 7_000_000m });

        Assert.Equal(2, count);
        Assert.Contains(items, i => i.Slug == "mid-low");
        Assert.Contains(items, i => i.Slug == "mid-high");
    }

    [Fact]
    public async Task TC_T1_033_CatalogFilter_SortByPriceAscAndPriceDesc_OrdersAccurately()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "P1", Slug = "p1", Sku = "S1", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_000_000m },
            new Product { Id = 2, Name = "P2", Slug = "p2", Sku = "S2", BrandId = b.Id, CategoryId = c.Id, BasePrice = 3_000_000m },
            new Product { Id = 3, Name = "P3", Slug = "p3", Sku = "S3", BrandId = b.Id, CategoryId = c.Id, BasePrice = 9_000_000m }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (asc, _) = await service.GetProductsAsync(new ProductFilterParams { SortBy = "price_asc" });
        var (desc, _) = await service.GetProductsAsync(new ProductFilterParams { SortBy = "price_desc" });

        Assert.Equal(new[] { 3_000_000m, 6_000_000m, 9_000_000m }, asc.Select(x => x.BasePrice).ToArray());
        Assert.Equal(new[] { 9_000_000m, 6_000_000m, 3_000_000m }, desc.Select(x => x.BasePrice).ToArray());
    }

    [Fact]
    public async Task TC_T1_034_CatalogFilter_SortByRatingAndNewestDefault_OrdersAccurately()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "R1", Slug = "r1", Sku = "R1", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, RatingAverage = 4.2 },
            new Product { Id = 2, Name = "R2", Slug = "r2", Sku = "R2", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, RatingAverage = 5.0 },
            new Product { Id = 3, Name = "R3", Slug = "r3", Sku = "R3", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, RatingAverage = 4.7 }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (byRating, _) = await service.GetProductsAsync(new ProductFilterParams { SortBy = "rating" });

        Assert.Equal(new[] { "r2", "r3", "r1" }, byRating.Select(x => x.Slug).ToArray());
    }

    [Fact]
    public async Task TC_T1_035_CatalogFilter_PaginationAndNegativePageClamping()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        for (int i = 1; i <= 15; i++)
        {
            db.Products.Add(new Product { Id = i, Name = $"Item {i}", Slug = $"item-{i}", Sku = $"SKU-{i}", BrandId = b.Id, CategoryId = c.Id, BasePrice = i * 100_000m });
        }
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var (page1, total1) = await service.GetProductsAsync(new ProductFilterParams { SortBy = "price_asc", Page = 0, PageSize = 6 });
        var (page3, total3) = await service.GetProductsAsync(new ProductFilterParams { SortBy = "price_asc", Page = 3, PageSize = 6 });

        Assert.Equal(15, total1);
        Assert.Equal(6, page1.Count);
        Assert.Equal("item-1", page1[0].Slug);
        Assert.Equal(15, total3);
        Assert.Equal(3, page3.Count);
        Assert.Equal("item-13", page3[0].Slug);
    }

    [Fact]
    public async Task TC_T1_036_ScentQuiz_Archetype_TheSovereignAristocrat()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Creed", "creed", "Niche", "niche", "Woody", "woody", ScentFamily.Woody);
        db.Products.Add(new Product { Id = 1, Name = "Royal Oud", Slug = "royal-oud", Sku = "RO", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, Gender = GenderTarget.Men, BasePrice = 8_500_000m });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "men",
            Vibe = "luxury",
            SeasonOrOccasion = "evening",
            IntensityPreference = "intense",
            BudgetPreference = "high"
        });

        Assert.Equal("The Sovereign Aristocrat", res.ArchetypeTitle);
        Assert.NotEmpty(res.DominantAccords);
        Assert.Single(res.Items);
    }

    [Fact]
    public async Task TC_T1_037_ScentQuiz_Archetype_TheVelvetEnchantress()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Parfums de Marly", "pdm", "Niche", "niche", "Floral", "floral", ScentFamily.Floral);
        db.Products.Add(new Product { Id = 1, Name = "Delina Exclusif", Slug = "delina-exclusif", Sku = "PDM-DE", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, Gender = GenderTarget.Women, BasePrice = 7_500_000m });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "women",
            Vibe = "romantic",
            SeasonOrOccasion = "evening",
            IntensityPreference = "strong",
            BudgetPreference = "high"
        });

        Assert.Equal("The Velvet Enchantress", res.ArchetypeTitle);
        Assert.Contains(res.KeyNotes, k => k.Contains("Hoa Hồng"));
    }

    [Fact]
    public async Task TC_T1_038_ScentQuiz_Archetype_TheEtherealLuminary()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Creed", "creed", "Niche", "niche", "Citrus", "citrus", ScentFamily.Citrus);
        db.Products.Add(new Product { Id = 1, Name = "Silver Mountain Water", Slug = "smw", Sku = "SMW", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, Gender = GenderTarget.Unisex, BasePrice = 6_500_000m });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "unisex",
            Vibe = "fresh",
            SeasonOrOccasion = "daily",
            IntensityPreference = "moderate",
            BudgetPreference = "mid"
        });

        Assert.Equal("The Ethereal Luminary", res.ArchetypeTitle);
    }

    [Fact]
    public async Task TC_T1_039_ScentQuiz_Archetype_TheMysticAlchemist()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Kilian", "kilian", "Niche", "niche", "Oriental", "oriental", ScentFamily.Oriental);
        db.Products.Add(new Product { Id = 1, Name = "Angels Share", Slug = "angels-share", Sku = "AS", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, Gender = GenderTarget.Unisex, BasePrice = 6_200_000m });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "unisex",
            Vibe = "cozy",
            SeasonOrOccasion = "special",
            IntensityPreference = "intense",
            BudgetPreference = "high"
        });

        Assert.Equal("The Mystic Alchemist", res.ArchetypeTitle);
    }

    [Fact]
    public async Task TC_T1_040_ScentQuiz_Archetype_TheAvantGardeConnoisseur()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Le Labo", "le-labo", "Niche", "niche", "Leather", "leather", ScentFamily.Leather);
        db.Products.Add(new Product { Id = 1, Name = "Another 13", Slug = "another-13", Sku = "A13", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, Gender = GenderTarget.Unisex, BasePrice = 6_800_000m });
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "unisex",
            Vibe = "luxury",
            SeasonOrOccasion = "all",
            IntensityPreference = "strong",
            BudgetPreference = "mid"
        });

        Assert.Equal("The Avant-Garde Connoisseur", res.ArchetypeTitle);
    }

    [Fact]
    public async Task TC_T1_041_ScentQuiz_MatchScoreNormalization_Top4Within85To99Percent()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Tom Ford", "tom-ford", "Niche", "niche", "Woody", "woody", ScentFamily.Woody);
        for (int i = 1; i <= 6; i++)
        {
            db.Products.Add(new Product
            {
                Id = i,
                Name = $"Quiz Perfume {i}",
                Slug = $"quiz-perfume-{i}",
                Sku = $"QP-{i}",
                BrandId = b.Id,
                CategoryId = c.Id,
                FragranceFamilyId = f.Id,
                Gender = i % 2 == 0 ? GenderTarget.Men : GenderTarget.Unisex,
                Concentration = i == 1 ? Concentration.Parfum : Concentration.EDP,
                BasePrice = 3_000_000m + (i * 1_000_000m),
                RatingAverage = 4.4 + (i * 0.1),
                IsBestSeller = i == 1
            });
        }
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "men",
            Vibe = "luxury",
            SeasonOrOccasion = "evening",
            IntensityPreference = "intense",
            BudgetPreference = "high"
        });

        Assert.Equal(4, res.Items.Count);
        Assert.InRange(res.Items[0].MatchScore, 95, 99);
        Assert.InRange(res.Items[1].MatchScore, 92, 95);
        Assert.InRange(res.Items[2].MatchScore, 89, 92);
        Assert.InRange(res.Items[3].MatchScore, 85, 89);
        Assert.True(res.Items[0].MatchScore >= res.Items[1].MatchScore);
        Assert.True(res.Items[1].MatchScore >= res.Items[2].MatchScore);
        Assert.True(res.Items[2].MatchScore >= res.Items[3].MatchScore);
    }

    [Fact]
    public async Task TC_T1_042_ScentQuiz_MatchReasons_CoverAllVibesAndIncludeTopNotes()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db, 1, "Creed", "creed", "Niche", "niche", "Woody", "woody", ScentFamily.Woody);
        var note = new FragranceNote { Id = 1, Name = "Cam Bergamot", Slug = "bergamot" };
        db.FragranceNotes.Add(note);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, BasePrice = 8_000_000m };
        p.FragranceNotes.Add(new ProductFragranceNote { ProductId = 1, FragranceNoteId = 1, FragranceNote = note, NoteLevel = NoteLevel.Top });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        foreach (var vibe in new[] { "luxury", "cozy", "fresh", "romantic", "mysterious" })
        {
            var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest { Vibe = vibe });
            Assert.Single(res.Items);
            Assert.Contains("Cam Bergamot", res.Items[0].MatchReason);
            Assert.False(string.IsNullOrWhiteSpace(res.Items[0].MatchReason));
        }
    }

    [Fact]
    public async Task TC_T1_043_ScentQuiz_ScoringMatrix_WeightsBudgetIntensityAndOccasion()
    {
        using var db = CreateDb();
        var (b, c, fFresh) = SeedCoreTaxonomy(db, 1, "Maison", "maison", "Niche", "niche", "Fresh", "fresh", ScentFamily.Fresh);
        var fOriental = new FragranceFamily { Id = 2, Name = "Oriental", Slug = "oriental", ScentType = ScentFamily.Oriental };
        db.FragranceFamilies.Add(fOriental);

        db.Products.AddRange(
            new Product { Id = 1, Name = "Daily Fresh EDT", Slug = "daily-fresh", Sku = "DF", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = 1, Concentration = Concentration.EDT, BasePrice = 3_200_000m, Gender = GenderTarget.Unisex },
            new Product { Id = 2, Name = "Heavy Night Parfum", Slug = "heavy-night", Sku = "HN", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = 2, Concentration = Concentration.Parfum, BasePrice = 9_500_000m, Gender = GenderTarget.Men }
        );
        await db.SaveChangesAsync();

        var service = new PerfumeCatalogService(db);
        var dailyRes = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest
        {
            GenderPreference = "unisex",
            Vibe = "fresh",
            SeasonOrOccasion = "daily",
            IntensityPreference = "moderate",
            BudgetPreference = "sample"
        });

        Assert.Equal("daily-fresh", dailyRes.Items[0].Slug);
    }

    [Fact]
    public async Task TC_T1_044_ScentQuiz_EmptyDatabase_ReturnsGracefulDefaultArchetype()
    {
        using var db = CreateDb();
        var service = new PerfumeCatalogService(db);

        var res = await service.GetQuizRecommendationsAsync(new QuizAnswerRequest());

        Assert.NotNull(res);
        Assert.Equal("The Ethereal Luminary", res.ArchetypeTitle);
        Assert.Empty(res.Items);
    }

    [Fact]
    public async Task TC_T1_045_BlogService_GetRecentPostsAsync_ReturnsOnlyPublishedAndExcludesDrafts()
    {
        using var db = CreateDb();
        db.BlogPosts.AddRange(
            new BlogPost { Id = 1, Title = "Published 1", Slug = "pub-1", Content = "Body", IsPublished = true },
            new BlogPost { Id = 2, Title = "Draft Post", Slug = "draft-1", Content = "Body", IsPublished = false },
            new BlogPost { Id = 3, Title = "Deleted Post", Slug = "del-1", Content = "Body", IsPublished = true, IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var service = new BlogService(db);
        var recent = await service.GetRecentPostsAsync(6);

        Assert.Single(recent);
        Assert.Equal("pub-1", recent[0].Slug);
    }

    [Fact]
    public async Task TC_T1_046_BlogService_GetPostsPagedAsync_FiltersByCategorySlugAndPaginates()
    {
        using var db = CreateDb();
        var cat1 = new BlogCategory { Id = 1, Name = "Kiến Thức Nước Hoa", Slug = "kien-thuc" };
        var cat2 = new BlogCategory { Id = 2, Name = "Phong Cách Sống", Slug = "phong-cach" };
        db.BlogCategories.AddRange(cat1, cat2);
        db.BlogPosts.AddRange(
            new BlogPost { Id = 1, Title = "Post 1", Slug = "post-1", Content = "C1", CategoryId = 1, Category = cat1, IsPublished = true },
            new BlogPost { Id = 2, Title = "Post 2", Slug = "post-2", Content = "C2", CategoryId = 1, Category = cat1, IsPublished = true },
            new BlogPost { Id = 3, Title = "Post 3", Slug = "post-3", Content = "C3", CategoryId = 2, Category = cat2, IsPublished = true }
        );
        await db.SaveChangesAsync();

        var service = new BlogService(db);
        var (posts, total) = await service.GetPostsPagedAsync("kien-thuc", page: 1, pageSize: 1);

        Assert.Equal(2, total);
        Assert.Single(posts);
        Assert.Equal(1, posts[0].CategoryId);
    }

    [Fact]
    public async Task TC_T1_047_BlogService_GetPostBySlugAsync_IncrementsViewCountAndRejectsDraft()
    {
        using var db = CreateDb();
        db.BlogPosts.AddRange(
            new BlogPost { Id = 1, Title = "Art of Oud", Slug = "art-of-oud", Content = "Oud story", IsPublished = true, ViewCount = 10 },
            new BlogPost { Id = 2, Title = "Hidden Draft", Slug = "hidden-draft", Content = "Draft", IsPublished = false, ViewCount = 0 }
        );
        await db.SaveChangesAsync();

        var service = new BlogService(db);
        var found = await service.GetPostBySlugAsync("art-of-oud");
        var draft = await service.GetPostBySlugAsync("hidden-draft");

        Assert.NotNull(found);
        Assert.Equal(11, found.ViewCount);
        Assert.Null(draft);
    }

    [Fact]
    public async Task TC_T1_048_BlogService_CreatePostAsync_GeneratesVietnameseSlugAutomatically()
    {
        using var db = CreateDb();
        var service = new BlogService(db);

        var created = await service.CreatePostAsync(new BlogPost
        {
            Title = "Nghệ Thuật Thưởng Thức Nước Hoa Niche Đẳng Cấp!",
            Slug = "",
            Content = "Nội dung bài viết",
            IsPublished = true
        });

        Assert.Equal("nghe-thuat-thuong-thuc-nuoc-hoa-niche-dang-cap", created.Slug);
        Assert.False(created.IsDeleted);
    }

    [Fact]
    public async Task TC_T1_049_BlogService_UpdatePostAsync_UpdatesFieldsAndHandlesMissingPost()
    {
        using var db = CreateDb();
        db.BlogPosts.Add(new BlogPost { Id = 5, Title = "Old Title", Slug = "old-title", Content = "Old", IsPublished = false });
        await db.SaveChangesAsync();

        var service = new BlogService(db);
        var ok = await service.UpdatePostAsync(new BlogPost
        {
            Id = 5,
            Title = "Mùi Hương Hoàng Gia",
            Slug = "",
            Summary = "Tóm tắt mới",
            Content = "Nội dung mới",
            AuthorName = "Master Perfumer",
            IsPublished = true
        });
        var notFound = await service.UpdatePostAsync(new BlogPost { Id = 999, Title = "Missing" });

        var updated = await service.GetPostByIdAsync(5);
        Assert.True(ok);
        Assert.False(notFound);
        Assert.NotNull(updated);
        Assert.Equal("mui-huong-hoang-gia", updated.Slug);
        Assert.True(updated.IsPublished);
    }

    [Fact]
    public async Task TC_T1_050_BlogService_DeletePostAsync_SoftDeletesAndExcludesFromAdminAndCategories()
    {
        using var db = CreateDb();
        var cat = new BlogCategory { Id = 1, Name = "Editorial", Slug = "editorial" };
        db.BlogCategories.Add(cat);
        db.BlogPosts.Add(new BlogPost { Id = 7, Title = "To Delete", Slug = "to-delete", Content = "Body", CategoryId = 1, Category = cat, IsPublished = true });
        await db.SaveChangesAsync();

        var service = new BlogService(db);
        Assert.True(await service.DeletePostAsync(7));
        Assert.False(await service.DeletePostAsync(999));

        Assert.Null(await service.GetPostByIdAsync(7));
        Assert.Empty(await service.GetAllPostsForAdminAsync());
        var categories = await service.GetBlogCategoriesAsync();
        Assert.Single(categories);
    }

    // =========================================================================
    // PHẦN 2: SHOPPING CART, CHECKOUT, VOUCHERS, VIETQR & LIVE ROUTES (TC-T1-051 -> TC-T1-100)
    // =========================================================================

    [Fact]
    public async Task TC_T1_051_CustomerCart_GetAsync_WithoutOwnerKeyReturnsEmptyAndGuestCartHashesToken()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var emptyState = await cartService.GetAsync(null, null);
        Assert.Empty(emptyState.Items);
        Assert.Equal(0m, emptyState.SubTotal);

        var guestState = await cartService.AddAsync(null, "raw-guest-token-123", new CartAddItemRequest { ProductSlug = "aventus", Quantity = 1 });
        Assert.Single(guestState.Items);

        var storedCart = await db.ShoppingCarts.FirstOrDefaultAsync();
        Assert.NotNull(storedCart);
        Assert.StartsWith("guest:", storedCart.OwnerKey);
        Assert.DoesNotContain("raw-guest-token-123", storedCart.OwnerKey);
    }

    [Fact]
    public async Task TC_T1_052_CustomerCart_AddAsync_AuthenticatedUser_CreatesUserCartAndCalculatesSubTotal()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Santal 33", Slug = "santal-33", Sku = "S33", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_200_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("user-001", null, new CartAddItemRequest { ProductSlug = "santal-33", Quantity = 2 });

        Assert.Single(state.Items);
        Assert.Equal(2, state.TotalCount);
        Assert.Equal(14_400_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_053_CustomerCart_AddAsync_ExplicitVariantId_UsesSalePriceWhenPositive()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "BR540", Slug = "br540", Sku = "BR540", BrandId = b.Id, CategoryId = c.Id, BasePrice = 9_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 70, VolumeLabel = "70ml EDP", OriginalPrice = 9_000_000m, SalePrice = 8_200_000m, StockQuantity = 10, IsDefault = true });
        p.Variants.Add(new ProductVariant { Id = 11, ProductId = 1, VolumeMl = 200, VolumeLabel = "200ml EDP", OriginalPrice = 15_000_000m, SalePrice = 0m, StockQuantity = 5, IsDefault = false });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "br540", VariantId = 10, Quantity = 1 });
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "br540", VariantId = 11, Quantity = 1 });

        Assert.Equal(2, state.Items.Count);
        Assert.Equal(8_200_000m, state.Items.First(i => i.VariantId == 10).UnitPrice);
        Assert.Equal(15_000_000m, state.Items.First(i => i.VariantId == 11).UnitPrice);
        Assert.Equal(23_200_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_054_CustomerCart_AddAsync_WithoutVariantId_SelectsDefaultVariantAutomatically()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Layton", Slug = "layton", Sku = "LAY", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 750_000m, StockQuantity = 10, IsDefault = false });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 125, VolumeLabel = "125ml Fullbox", OriginalPrice = 6_000_000m, StockQuantity = 10, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "layton", VariantId = null, Quantity = 1 });

        Assert.Single(state.Items);
        Assert.Equal(2, state.Items[0].VariantId);
        Assert.Equal("125ml Fullbox", state.Items[0].VariantLabel);
    }

    [Fact]
    public async Task TC_T1_055_CustomerCart_AddAsync_ProductWithoutVariants_UsesBasePriceAndFullboxLabel()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Pure Oil", Slug = "pure-oil", Sku = "PO", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "pure-oil", Quantity = 3 });

        Assert.Single(state.Items);
        Assert.Null(state.Items[0].VariantId);
        Assert.Equal("Fullbox", state.Items[0].VariantLabel);
        Assert.Equal(4_500_000m, state.Items[0].UnitPrice);
        Assert.Equal(50, state.Items[0].StockQuantity);
    }

    [Fact]
    public async Task TC_T1_056_CustomerCart_AddAsync_ExistingItem_IncrementsQuantityWithoutDuplicatingLine()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 5, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, StockQuantity = 10, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 5, Quantity = 2 });
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 5, Quantity = 3 });

        Assert.Single(state.Items);
        Assert.Equal(5, state.Items[0].Quantity);
        Assert.Equal(42_500_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_057_CustomerCart_AddAsync_QuantityOutOfBoundsOrExceedingStock_ThrowsInvalidOperationException()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Rare Oud", Slug = "rare-oud", Sku = "RO", BrandId = b.Id, CategoryId = c.Id, BasePrice = 12_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 50, VolumeLabel = "50ml", OriginalPrice = 12_000_000m, StockQuantity = 4, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", Quantity = 0 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", Quantity = 51 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", VariantId = 1, Quantity = 5 }));
    }

    [Fact]
    public async Task TC_T1_058_CustomerCart_AddAsync_OutOfStockProductOrZeroStockVariant_RejectsAddition()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var pOut = new Product { Id = 1, Name = "Out Product", Slug = "out-product", Sku = "OP", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = false };
        var pZeroVar = new Product { Id = 2, Name = "Zero Variant", Slug = "zero-var", Sku = "ZV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        pZeroVar.Variants.Add(new ProductVariant { Id = 10, ProductId = 2, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 5_000_000m, StockQuantity = 0, IsDefault = true });
        db.Products.AddRange(pOut, pZeroVar);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "out-product", Quantity = 1 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "zero-var", VariantId = 10, Quantity = 1 }));
    }

    [Fact]
    public async Task TC_T1_059_CustomerCart_AddAsync_InvalidVariantId_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var pNoVar = new Product { Id = 1, Name = "NoVar", Slug = "no-var", Sku = "NV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        var pWithVar = new Product { Id = 2, Name = "WithVar", Slug = "with-var", Sku = "WV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        pWithVar.Variants.Add(new ProductVariant { Id = 5, ProductId = 2, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 5_000_000m, StockQuantity = 5, IsDefault = true });
        db.Products.AddRange(pNoVar, pWithVar);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "no-var", VariantId = 99, Quantity = 1 }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "with-var", VariantId = 999, Quantity = 1 }));
    }

    [Fact]
    public async Task TC_T1_060_CustomerCart_SetQuantityAsync_UpdatesQuantityAndValidatesStockLimit()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Oud Wood", Slug = "oud-wood", Sku = "OW", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 7_000_000m, StockQuantity = 6, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "oud-wood", VariantId = 10, Quantity = 1 });

        var updated = await cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = 10, Quantity = 4 });
        Assert.Equal(4, updated.TotalCount);
        Assert.Equal(28_000_000m, updated.SubTotal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = 10, Quantity = 7 }));
    }

    [Fact]
    public async Task TC_T1_061_CustomerCart_SetQuantityAsync_ZeroQuantity_RemovesLineItem()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Tam Dao", Slug = "tam-dao", Sku = "TD", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_200_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "tam-dao", Quantity = 2 });

        var afterZero = await cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = null, Quantity = 0 });
        Assert.Empty(afterZero.Items);
        Assert.Equal(0m, afterZero.SubTotal);
    }

    [Fact]
    public async Task TC_T1_062_CustomerCart_RemoveAsync_RemovesTargetVariantLineOnly()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 900_000m, StockQuantity = 10 });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, StockQuantity = 10 });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 1, Quantity = 1 });
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 2, Quantity = 1 });

        var afterRemove = await cartService.RemoveAsync("u1", null, 1, 1);
        Assert.Single(afterRemove.Items);
        Assert.Equal(2, afterRemove.Items[0].VariantId);
    }

    [Fact]
    public async Task TC_T1_063_CustomerCart_MergeGuestCartAsync_CombinesQuantitiesClampsToStockAndClearsGuest()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Grand Soir", Slug = "grand-soir", Sku = "GS", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 70, VolumeLabel = "70ml", OriginalPrice = 5_500_000m, StockQuantity = 5, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync(null, "guest-tok-99", new CartAddItemRequest { ProductSlug = "grand-soir", VariantId = 10, Quantity = 3 });
        await cartService.AddAsync("user-login-1", null, new CartAddItemRequest { ProductSlug = "grand-soir", VariantId = 10, Quantity = 4 });

        var merged = await cartService.MergeGuestCartAsync("user-login-1", "guest-tok-99");
        var guestAfter = await cartService.GetAsync(null, "guest-tok-99");

        Assert.Single(merged.Items);
        // 3 + 4 = 7, clamped to StockQuantity = 5
        Assert.Equal(5, merged.Items[0].Quantity);
        Assert.Empty(guestAfter.Items);
    }

    [Fact]
    public async Task TC_T1_064_CustomerCart_ClearAsync_EmptiesAllItems()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Naxos", Slug = "naxos", Sku = "NX", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_800_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "naxos", Quantity = 2 });
        await cartService.ClearAsync("u1", null);

        var state = await cartService.GetAsync("u1", null);
        Assert.Empty(state.Items);
        Assert.Equal(0, state.TotalCount);
        Assert.Equal(0m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_065_OrderService_PlaceOrderAsync_CalculatesSubTotalAcrossMultipleVariants()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Creed Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 950_000m, SalePrice = 850_000m, StockQuantity = 10 });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, SalePrice = 0m, StockQuantity = 10 });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, error, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Nguyễn Văn A",
            CustomerPhone = "0901234567",
            CustomerEmail = "a@luxe.vn",
            ShippingAddress = "88 Tràng Tiền, Hoàn Kiếm, Hà Nội",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, VariantId = 1, Quantity = 2 }, // 2 * 850,000 = 1,700,000
                new() { ProductId = 1, VariantId = 2, Quantity = 1 }  // 1 * 8,500,000 = 8,500,000
            }
        }, "user-1");

        Assert.True(success, error);
        Assert.NotNull(order);
        Assert.Equal(10_200_000m, order.SubTotal);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public async Task TC_T1_066_OrderService_ShippingFee_Applies35000WhenSubTotalUnderThreshold()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Discovery Vial", Slug = "vial", Sku = "VL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 650_000m, IsInStock = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Trần Thị B",
            CustomerPhone = "0912345678",
            CustomerEmail = "b@luxe.vn",
            ShippingAddress = "12 Nguyễn Huệ, Quận 1, TP.HCM",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(650_000m, order.SubTotal);
        Assert.Equal(35_000m, order.ShippingFee);
        Assert.Equal(685_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_067_OrderService_ShippingFee_FreeShippingWhenSubTotalMeetsThreshold()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Luxury Bottle", Slug = "lux", Sku = "LX", BrandId = b.Id, CategoryId = c.Id, BasePrice = 3_500_000m, IsInStock = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Lê Hoàng C",
            CustomerPhone = "0923456789",
            CustomerEmail = "c@luxe.vn",
            ShippingAddress = "72 Lê Thánh Tôn, Quận 1",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(0m, order.ShippingFee);
        Assert.Equal(3_500_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_068_OrderService_PercentageVoucher_CalculatesDiscountAndIncrementsUsedCount()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Santal 33", Slug = "santal-33", Sku = "S33", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "LUXE10",
            DiscountPercent = 10m,
            MinOrderAmount = 1_000_000m,
            UsageLimit = 50,
            UsedCount = 5,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách VIP",
            CustomerPhone = "0988888888",
            CustomerEmail = "vip@luxe.vn",
            ShippingAddress = "Hà Nội",
            VoucherCode = "LUXE10",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(500_000m, order.DiscountAmount);
        Assert.Equal(4_500_000m, order.TotalAmount);
        Assert.Equal(6, (await db.Vouchers.FindAsync(1))!.UsedCount);
    }

    [Fact]
    public async Task TC_T1_069_OrderService_FixedAmountVoucher_AppliesExactFixedDiscount()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Tam Dao", Slug = "tam-dao", Sku = "TD", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "GIFT300K",
            DiscountAmount = 300_000m,
            MinOrderAmount = 2_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách Hàng",
            CustomerPhone = "0977777777",
            CustomerEmail = "kh@luxe.vn",
            ShippingAddress = "Đà Nẵng",
            VoucherCode = "GIFT300K",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.Equal(300_000m, order!.DiscountAmount);
        Assert.Equal(3_700_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_070_OrderService_VoucherWithMaxDiscountAmount_CapsDiscountAtMaxLimit()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Roja Amber Aoud", Slug = "roja-aa", Sku = "RAA", BrandId = b.Id, CategoryId = c.Id, BasePrice = 10_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "VIP20CAP500",
            DiscountPercent = 20m, // 20% of 10M = 2,000,000
            MaxDiscountAmount = 500_000m, // capped at 500,000
            MinOrderAmount = 2_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Hoàng Gia",
            CustomerPhone = "0966666666",
            CustomerEmail = "royal@luxe.vn",
            ShippingAddress = "TP.HCM",
            VoucherCode = "VIP20CAP500",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.Equal(500_000m, order!.DiscountAmount);
        Assert.Equal(9_500_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_071_OrderService_ValidateVoucherAsync_RejectsBelowMinOrderAmount()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "MIN5M",
            DiscountAmount = 500_000m,
            MinOrderAmount = 5_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("MIN5M", 4_999_999m));
        Assert.NotNull(await orderService.ValidateVoucherAsync("MIN5M", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_072_OrderService_ValidateVoucherAsync_RejectsExpiredAndFutureVouchers()
    {
        using var db = CreateDb();
        db.Vouchers.AddRange(
            new Voucher { Id = 1, Code = "EXPIRED", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-10), ValidTo = DateTime.UtcNow.AddMinutes(-5), IsActive = true },
            new Voucher { Id = 2, Code = "FUTURE", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddHours(2), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = true }
        );
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("EXPIRED", 5_000_000m));
        Assert.Null(await orderService.ValidateVoucherAsync("FUTURE", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_073_OrderService_ValidateVoucherAsync_RejectsInactiveAndSoftDeletedVouchers()
    {
        using var db = CreateDb();
        db.Vouchers.AddRange(
            new Voucher { Id = 1, Code = "INACTIVE", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = false },
            new Voucher { Id = 2, Code = "DELETED", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = true, IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("INACTIVE", 5_000_000m));
        Assert.Null(await orderService.ValidateVoucherAsync("DELETED", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_074_OrderService_ValidateVoucherAsync_RejectsWhenUsageLimitExhausted()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "SOLDOUTVOUCHER",
            DiscountPercent = 15m,
            UsageLimit = 20,
            UsedCount = 20,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("SOLDOUTVOUCHER", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_075_OrderService_ValidateVoucherAsync_CaseInsensitiveAndTrimsWhitespace()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "LUXE2026",
            DiscountPercent = 10m,
            UsageLimit = 100,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var v = await orderService.ValidateVoucherAsync("   luxe2026  ", 3_000_000m);

        Assert.NotNull(v);
        Assert.Equal("LUXE2026", v.Code);
        Assert.Null(await orderService.ValidateVoucherAsync("   ", 3_000_000m));
    }

    [Fact]
    public async Task TC_T1_076_OrderService_RoyalGiftWrappingTrue_Adds50000FeeAndPersistsGiftMessage()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Delina", Slug = "delina", Sku = "DEL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Quý Cô Thanh Lịch",
            CustomerPhone = "0933333333",
            CustomerEmail = "lady@luxe.vn",
            ShippingAddress = "88 Đồng Khởi, Quận 1",
            PaymentMethod = PaymentMethod.BankTransfer,
            IsGiftWrapping = true,
            GiftMessage = "  Chúc mừng kỷ niệm tình yêu!  ",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.True(order.IsGiftWrapping);
        Assert.Equal(50_000m, order.GiftWrappingFee);
        Assert.Equal("Chúc mừng kỷ niệm tình yêu!", order.GiftMessage);
        Assert.Equal(6_550_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_077_OrderService_RoyalGiftWrappingFalse_AddsZeroFee()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Delina", Slug = "delina", Sku = "DEL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách Hàng",
            CustomerPhone = "0933333333",
            CustomerEmail = "lady@luxe.vn",
            ShippingAddress = "88 Đồng Khởi, Quận 1",
            PaymentMethod = PaymentMethod.COD,
            IsGiftWrapping = false,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.False(order!.IsGiftWrapping);
        Assert.Equal(0m, order.GiftWrappingFee);
        Assert.Equal(6_500_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_078_OrderService_PaymentMethods_CODAndBankTransfer_GenerateUniqueOrderCodes()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (s1, _, o1) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "User 1", CustomerPhone = "0901111111", CustomerEmail = "u1@luxe.vn", ShippingAddress = "Address 1",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var (s2, _, o2) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "User 2", CustomerPhone = "0902222222", CustomerEmail = "u2@luxe.vn", ShippingAddress = "Address 2",
            PaymentMethod = PaymentMethod.BankTransfer,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(s1);
        Assert.True(s2);
        Assert.StartsWith("LUXE-", o1!.OrderCode);
        Assert.StartsWith("LUXE-", o2!.OrderCode);
        Assert.NotEqual(o1.OrderCode, o2.OrderCode);
        Assert.Equal(PaymentMethod.COD, o1.PaymentMethod);
        Assert.Equal(PaymentMethod.BankTransfer, o2.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, o2.PaymentStatus);
        Assert.Equal(OrderStatus.Pending, o2.OrderStatus);
    }

    [Fact]
    public async Task TC_T1_079_OrderService_UnsupportedPaymentMethods_VNPAYAndMoMo_ReturnControlledError()
    {
        using var db = CreateDb();
        var orderService = new OrderService(db);

        var (sVnpay, errVnpay, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Test", CustomerPhone = "0900000000", CustomerEmail = "t@luxe.vn", ShippingAddress = "HN",
            PaymentMethod = PaymentMethod.VNPAY,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var (sMomo, errMomo, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Test", CustomerPhone = "0900000000", CustomerEmail = "t@luxe.vn", ShippingAddress = "HN",
            PaymentMethod = PaymentMethod.MoMo,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.False(sVnpay);
        Assert.Contains("chưa được hỗ trợ", errVnpay);
        Assert.False(sMomo);
        Assert.Contains("chưa được hỗ trợ", errMomo);
    }

    [Fact]
    public async Task TC_T1_080_OrderService_StockDeduction_DecrementsVariantStockAndRejectsInsufficientStock()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Limited Edition", Slug = "limited", Sku = "LIM", BrandId = b.Id, CategoryId = c.Id, BasePrice = 9_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 9_000_000m, StockQuantity = 3, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (s1, _, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Buyer 1", CustomerPhone = "0901111111", CustomerEmail = "b1@luxe.vn", ShippingAddress = "HCM",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, VariantId = 10, Quantity = 2 } }
        });

        Assert.True(s1);
        var remainingVariant = await db.ProductVariants.FindAsync(10);
        Assert.Equal(1, remainingVariant!.StockQuantity);

        var (s2, err2, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Buyer 2", CustomerPhone = "0902222222", CustomerEmail = "b2@luxe.vn", ShippingAddress = "HCM",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, VariantId = 10, Quantity = 2 } }
        });

        Assert.False(s2);
        Assert.Contains("Không đủ tồn kho", err2);
    }

    [Fact]
    public async Task TC_T1_081_OrderService_Validation_RejectsMissingCustomerInfoEmptyCartAndOver50Quantity()
    {
        using var db = CreateDb();
        var orderService = new OrderService(db);

        var (sMissing, errMissing, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "", CustomerPhone = "0901111111", CustomerEmail = "a@b.c", ShippingAddress = "Addr",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var (sEmpty, errEmpty, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Name", CustomerPhone = "0901111111", CustomerEmail = "a@b.c", ShippingAddress = "Addr",
            Items = new List<CartItemDto>()
        });
        var (sOver, errOver, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Name", CustomerPhone = "0901111111", CustomerEmail = "a@b.c", ShippingAddress = "Addr",
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, VariantId = 1, Quantity = 30 },
                new() { ProductId = 1, VariantId = 1, Quantity = 25 }
            }
        });

        Assert.False(sMissing);
        Assert.Contains("đầy đủ thông tin", errMissing);
        Assert.False(sEmpty);
        Assert.Contains("đang trống", errEmpty);
        Assert.False(sOver);
        Assert.Contains("50", errOver);
    }

    [Fact]
    public async Task TC_T1_082_OrderService_ConfirmPaymentAsync_UpdatesPaymentStatusToCompleted()
    {
        using var db = CreateDb();
        db.Orders.Add(new Order
        {
            Id = 1,
            OrderCode = "LUXE-20261013-VIETQR1",
            CustomerName = "Khách Chuyển Khoản",
            CustomerPhone = "0909999999",
            CustomerEmail = "qr@luxe.vn",
            ShippingAddress = "Hà Nội",
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = 5_000_000m
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.True(await orderService.ConfirmPaymentAsync("LUXE-20261013-VIETQR1"));
        Assert.False(await orderService.ConfirmPaymentAsync("NON-EXISTENT-CODE"));

        var updated = await orderService.GetOrderByCodeAsync("LUXE-20261013-VIETQR1");
        Assert.NotNull(updated);
        Assert.Equal(PaymentStatus.Completed, updated.PaymentStatus);
    }

    [Fact]
    public async Task TC_T1_083_OrderService_GetOrderByCodeAndStatusLifecycle_TransitionsAndRestoresStockOnCancel()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        var v = new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, StockQuantity = 5, IsDefault = true };
        p.Variants.Add(v);
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (_, _, placed) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Lifecycle User", CustomerPhone = "0911223344", CustomerEmail = "lc@luxe.vn", ShippingAddress = "HN",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, VariantId = 10, Quantity = 2 } }
        });

        Assert.Equal(3, (await db.ProductVariants.FindAsync(10))!.StockQuantity);

        var fetched = await orderService.GetOrderByCodeAsync(placed!.OrderCode);
        Assert.NotNull(fetched);
        Assert.Single(fetched.Items);

        // Transition Pending -> Confirmed -> Shipping -> Cancelled & restore stock
        fetched.OrderStatus = OrderStatus.Confirmed;
        await db.SaveChangesAsync();
        Assert.Equal(OrderStatus.Confirmed, fetched.OrderStatus);

        fetched.OrderStatus = OrderStatus.Cancelled;
        foreach (var item in fetched.Items.Where(i => i.VariantId.HasValue))
        {
            var variant = await db.ProductVariants.FindAsync(item.VariantId!.Value);
            if (variant != null) variant.StockQuantity += item.Quantity;
        }
        await db.SaveChangesAsync();

        Assert.Equal(OrderStatus.Cancelled, fetched.OrderStatus);
        Assert.Equal(5, (await db.ProductVariants.FindAsync(10))!.StockQuantity);
    }

    [Fact]
    public async Task TC_T1_084_CheckoutController_VietQRAndConfirmBankTransfer_GeneratesQrUrlAndConfirmsGuestOrder()
    {
        using var db = CreateDb();
        db.Orders.Add(new Order
        {
            Id = 1,
            OrderCode = "LUXE-20261013-GUESTQR",
            UserId = null,
            CustomerName = "Guest VietQR",
            CustomerPhone = "0988776655",
            CustomerEmail = "guest@luxe.vn",
            ShippingAddress = "88 Tràng Tiền",
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = 6_550_000m
        });
        await db.SaveChangesAsync();

        var dpProvider = new EphemeralDataProtectionProvider();
        var protector = dpProvider.CreateProtector("PerfumeLuxe.GuestOrderTracking.v1");
        var guestToken = protector.Protect("LUXE-20261013-GUESTQR");

        var controller = new CheckoutController(
            new OrderService(db),
            new NoOpOrderEmailSender(),
            new CustomerCartService(db),
            null!,
            dpProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var qrResult = await controller.VietQR(null, guestToken) as ViewResult;
        Assert.NotNull(qrResult);
        var qrUrl = controller.ViewBag.QrUrl as string;
        Assert.NotNull(qrUrl);
        Assert.Contains("img.vietqr.io/image/vcb-9988776655-compact2.png", qrUrl);
        Assert.Contains("amount=6550000", qrUrl);
        Assert.Contains("LUXE-20261013-GUESTQR", qrUrl);

        var confirmResult = await controller.ConfirmBankTransfer(null, guestToken) as RedirectToActionResult;
        Assert.NotNull(confirmResult);
        Assert.Equal("Success", confirmResult.ActionName);
        Assert.Equal(PaymentStatus.Completed, (await db.Orders.FirstAsync()).PaymentStatus);
    }

    [Fact]
    public async Task TC_T1_085_StorefrontRoute_HomeIndex_LoadsFlashSaleHeroBannersAndCollections()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.HeroBanners.Add(new HeroBanner { Id = 1, Title = "Haute Parfumerie", ImageUrl = "/hero.jpg", IsActive = true, DisplayOrder = 1 });
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, BasePrice = 8_500_000m, IsFeatured = true, IsBestSeller = true, IsNewArrival = true });
        await db.SaveChangesAsync();

        var controller = new HomeController(
            new PerfumeCatalogService(db),
            new BlogService(db),
            new FlashSaleService(db),
            db);

        var result = await controller.Index() as ViewResult;
        Assert.NotNull(result);
        Assert.Single((List<HeroBanner>)controller.ViewBag.HeroBanners);
        Assert.Single((List<ProductCardDto>)controller.ViewBag.FeaturedProducts);
        Assert.Single((List<ProductCardDto>)controller.ViewBag.BestSellers);
        Assert.Single((List<ProductCardDto>)controller.ViewBag.NewArrivals);
    }

    [Fact]
    public async Task TC_T1_086_StorefrontRoute_CatalogIndex_BoSuuTap_RendersFilteredCatalogAndFacetLists()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, BasePrice = 8_500_000m });
        await db.SaveChangesAsync();

        var controller = new CatalogController(new PerfumeCatalogService(db), db);
        var result = await controller.Index(new ProductFilterParams()) as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsAssignableFrom<List<ProductCardDto>>(result.Model);
        Assert.Single(model);
        Assert.Equal(1, (int)controller.ViewBag.TotalCount);
    }

    [Fact]
    public async Task TC_T1_087_StorefrontRoute_CatalogQuizAndQuizSubmit_ReturnsViewAndJsonRecommendations()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, BasePrice = 8_500_000m });
        await db.SaveChangesAsync();

        var controller = new CatalogController(new PerfumeCatalogService(db), db);
        var viewResult = controller.Quiz() as ViewResult;
        var jsonResult = await controller.QuizSubmit(new QuizAnswerRequest { GenderPreference = "men", Vibe = "luxury" }) as JsonResult;

        Assert.NotNull(viewResult);
        Assert.NotNull(jsonResult);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task TC_T1_088_StorefrontRoute_DiscoveryBox_ReturnsInStockPerfumesOrderedByFeatured()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "InStock Featured", Slug = "in-1", Sku = "IN1", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, IsInStock = true, IsFeatured = true, BasePrice = 5_000_000m },
            new Product { Id = 2, Name = "OutOfStock", Slug = "out-2", Sku = "OUT2", BrandId = b.Id, CategoryId = c.Id, FragranceFamilyId = f.Id, IsInStock = false, BasePrice = 5_000_000m }
        );
        await db.SaveChangesAsync();

        var controller = new CatalogController(new PerfumeCatalogService(db), db);
        var result = await controller.DiscoveryBox() as ViewResult;

        Assert.NotNull(result);
        var model = Assert.IsAssignableFrom<List<DiscoveryPerfumeItemDto>>(result.Model);
        Assert.Single(model);
        Assert.Equal("InStock Featured", model[0].Name);
    }

    [Fact]
    public async Task TC_T1_089_StorefrontRoute_Compare_SoSanh_ComparesSelectedProductsOrDefaultsToFirstTwo()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.AddRange(
            new Product { Id = 1, Name = "Alpha Scent", Slug = "alpha", Sku = "AL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m },
            new Product { Id = 2, Name = "Beta Scent", Slug = "beta", Sku = "BE", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_000_000m },
            new Product { Id = 3, Name = "Gamma Scent", Slug = "gamma", Sku = "GA", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_000_000m }
        );
        await db.SaveChangesAsync();

        var controller = new CatalogController(new PerfumeCatalogService(db), db);
        var defaultCompare = await controller.Compare(null, null, null) as ViewResult;
        var explicitCompare = await controller.Compare(1, 3, null) as ViewResult;

        var defaultModel = Assert.IsAssignableFrom<List<ProductDetailDto>>(defaultCompare!.Model);
        var explicitModel = Assert.IsAssignableFrom<List<ProductDetailDto>>(explicitCompare!.Model);
        Assert.Equal(2, defaultModel.Count);
        Assert.Equal(new[] { 1, 3 }, explicitModel.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task TC_T1_090_StorefrontRoute_BlogIndexAndDetail_RendersHeroPostAndRelatedPosts()
    {
        using var db = CreateDb();
        var cat = new BlogCategory { Id = 1, Name = "Journal", Slug = "journal" };
        db.BlogCategories.Add(cat);
        db.BlogPosts.AddRange(
            new BlogPost { Id = 1, Title = "Article 1", Slug = "article-1", Content = "Content 1", CategoryId = 1, Category = cat, IsPublished = true },
            new BlogPost { Id = 2, Title = "Article 2", Slug = "article-2", Content = "Content 2", CategoryId = 1, Category = cat, IsPublished = true }
        );
        await db.SaveChangesAsync();

        var controller = new BlogController(new BlogService(db));
        var indexRes = await controller.Index(null, 1) as ViewResult;
        var detailRes = await controller.Detail("article-1") as ViewResult;
        var missingRes = await controller.Detail("missing-slug");

        Assert.NotNull(indexRes);
        Assert.NotNull(controller.ViewBag.HeroPost);
        Assert.NotNull(detailRes);
        Assert.IsType<NotFoundResult>(missingRes);
    }

    [Fact]
    public async Task TC_T1_091_StorefrontRoute_CartController_IndexAddUpdateAndRemoveEndpoints()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "cart-user-1") }, "TestAuth"));
        var controller = new CartController(new CustomerCartService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var addRes = await controller.AddToCart(new AddToCartRequest { ProductSlug = "aventus", Quantity = 2 }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(addRes);

        var indexRes = await controller.Index(CancellationToken.None) as ViewResult;
        var cartModel = Assert.IsType<CartStateDto>(indexRes!.Model);
        Assert.Equal(2, cartModel.TotalCount);

        var updRes = await controller.UpdateQuantity(new UpdateCartQuantityRequest { ProductId = 1, Quantity = 3 }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(updRes);

        var remRes = await controller.RemoveItem(new RemoveCartItemRequest { ProductId = 1 }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(remRes);
    }

    [Fact]
    public async Task TC_T1_092_StorefrontRoute_CheckoutController_RedirectsWhenCartEmptyAndValidatesVoucherJson()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher { Id = 1, Code = "LUXE10", DiscountPercent = 10m, UsageLimit = 10, UsedCount = 0, ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "chk-user-1") }, "TestAuth"));

        var controller = new CheckoutController(
            new OrderService(db),
            new NoOpOrderEmailSender(),
            cartService,
            null!,
            new EphemeralDataProtectionProvider())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Empty cart redirects to /Cart
        var emptyRedirect = await controller.Index(CancellationToken.None) as RedirectToActionResult;
        Assert.NotNull(emptyRedirect);
        Assert.Equal("Index", emptyRedirect.ActionName);
        Assert.Equal("Cart", emptyRedirect.ControllerName);

        // Add item and check voucher JSON
        await cartService.AddAsync("chk-user-1", null, new CartAddItemRequest { ProductSlug = "aventus", Quantity = 1 });
        var voucherJson = await controller.CheckVoucher(new VoucherCheckRequest { Code = "LUXE10" }, CancellationToken.None) as JsonResult;
        Assert.NotNull(voucherJson);
    }

    [Fact]
    public async Task TC_T1_093_StorefrontRoute_OrderTrackingController_MatchesLast6DigitsOfPhoneAndHandlesMismatch()
    {
        using var db = CreateDb();
        db.Orders.Add(new Order
        {
            Id = 1,
            OrderCode = "LUXE-20261013-TRACK01",
            CustomerName = "Khách Tra Cứu",
            CustomerPhone = "+84 909 123 456",
            CustomerEmail = "track@luxe.vn",
            ShippingAddress = "Hà Nội",
            TotalAmount = 5_000_000m
        });
        await db.SaveChangesAsync();

        var controller = new OrderTrackingController(db);

        var matched = await controller.Index("luxe-20261013-track01", "123 456") as ViewResult;
        var matchedModel = Assert.IsType<OrderTrackingViewModel>(matched!.Model);
        Assert.True(matchedModel.HasSearched);
        Assert.NotNull(matchedModel.Order);
        Assert.Null(matchedModel.ErrorMessage);

        var wrongPhone = await controller.Index("LUXE-20261013-TRACK01", "0999888777") as ViewResult;
        var wrongModel = Assert.IsType<OrderTrackingViewModel>(wrongPhone!.Model);
        Assert.Null(wrongModel.Order);
        Assert.Contains("không khớp", wrongModel.ErrorMessage);
    }

    [Fact]
    public void TC_T1_094_StorefrontRoute_VeChungToi_RendersAboutView()
    {
        using var db = CreateDb();
        var controller = new HomeController(new PerfumeCatalogService(db), new BlogService(db), new FlashSaleService(db), db);
        Assert.IsType<ViewResult>(controller.About());
    }

    [Fact]
    public void TC_T1_095_StorefrontRoute_Boutique_RendersBoutiqueView()
    {
        using var db = CreateDb();
        var controller = new HomeController(new PerfumeCatalogService(db), new BlogService(db), new FlashSaleService(db), db);
        Assert.IsType<ViewResult>(controller.Boutique());
    }

    [Fact]
    public void TC_T1_096_StorefrontRoute_ChinhSachDoiTra_RendersReturnPolicyView()
    {
        using var db = CreateDb();
        var controller = new HomeController(new PerfumeCatalogService(db), new BlogService(db), new FlashSaleService(db), db);
        Assert.IsType<ViewResult>(controller.ReturnPolicy());
    }

    [Fact]
    public void TC_T1_097_StorefrontRoute_HuongDanMuaHang_RendersBuyingGuideView()
    {
        using var db = CreateDb();
        var controller = new HomeController(new PerfumeCatalogService(db), new BlogService(db), new FlashSaleService(db), db);
        Assert.IsType<ViewResult>(controller.BuyingGuide());
    }

    [Fact]
    public void TC_T1_098_StorefrontRoute_PrivacyAndNewsletterSubscribe_IssuesLuxe2026Voucher()
    {
        using var db = CreateDb();
        var controller = new HomeController(new PerfumeCatalogService(db), new BlogService(db), new FlashSaleService(db), db);

        Assert.IsType<ViewResult>(controller.Privacy());

        var validRes = controller.SubscribeNewsletter(new NewsletterSubscribeRequest { Email = "client@parfum.fr" }) as JsonResult;
        var invalidRes = controller.SubscribeNewsletter(new NewsletterSubscribeRequest { Email = "invalid-email" }) as JsonResult;

        Assert.NotNull(validRes);
        Assert.Contains("LUXE2026", validRes.Value!.ToString());
        Assert.NotNull(invalidRes);
        Assert.Contains("false", invalidRes.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_T1_099_StorefrontRoute_SitemapXml_GeneratesValidXmlWithProductAndBlogUrls()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "creed-aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m });
        db.BlogPosts.Add(new BlogPost { Id = 1, Title = "Oud Guide", Slug = "oud-guide", Content = "Body", IsPublished = true });
        await db.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("perfumeluxe.vn");

        var controller = new SeoController(new PerfumeCatalogService(db), new BlogService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.Sitemap() as ContentResult;
        Assert.NotNull(result);
        Assert.StartsWith("application/xml", result.ContentType);
        Assert.Contains("https://perfumeluxe.vn/bo-suu-tap", result.Content);
        Assert.Contains("https://perfumeluxe.vn/nuoc-hoa/creed-aventus", result.Content);
        Assert.Contains("https://perfumeluxe.vn/blog/oud-guide", result.Content);
    }

    [Fact]
    public async Task TC_T1_100_StorefrontRoute_RobotsTxtAndExperienceBooking_VerifiesAeoBotsAndVipTastingBooking()
    {
        using var db = CreateDb();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("perfumeluxe.vn");

        var seoController = new SeoController(new PerfumeCatalogService(db), new BlogService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
        var robots = seoController.Robots() as ContentResult;
        Assert.NotNull(robots);
        Assert.StartsWith("text/plain", robots.ContentType);
        Assert.Contains("User-agent: GPTBot", robots.Content);
        Assert.Contains("Sitemap: https://perfumeluxe.vn/sitemap.xml", robots.Content);

        var expController = new ExperienceController(db);
        var bookingView = await expController.TastingBooking(new TastingBookingRequestModel
        {
            CustomerName = "Bá Tước Monte Cristo",
            PhoneNumber = "0988112233",
            Email = "count@parfum.fr",
            GuestCount = 2
        }) as ViewResult;

        Assert.NotNull(bookingView);
        Assert.Equal("BookingSuccess", bookingView.ViewName);
        Assert.StartsWith("VIP-", (string)expController.ViewBag.BookingCode);

        var backInStockRes = await expController.SubscribeBackInStock(new BackInStockSubscribeRequest
        {
            ProductId = 1,
            Email = "alert@parfum.fr"
        });
        Assert.IsType<OkObjectResult>(backInStockRes);
    }
}
