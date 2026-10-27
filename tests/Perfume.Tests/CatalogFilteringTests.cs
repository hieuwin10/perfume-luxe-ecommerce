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

public sealed class CatalogFilteringTests : TestFixtures
{
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


}
