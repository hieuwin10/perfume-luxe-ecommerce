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

public sealed class CatalogStorefrontTests : TestFixtures
{
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


}
