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

public sealed class ScentQuizAndBlogTests : TestFixtures
{
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


}
