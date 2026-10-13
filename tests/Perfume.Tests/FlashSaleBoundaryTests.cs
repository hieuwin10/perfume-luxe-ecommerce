using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Services;
using Xunit;

namespace Perfume.Tests;

public class FlashSaleBoundaryTests
{
    private PerfumeDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new PerfumeDbContext(options);
    }

    private (Brand Brand, Category Category) SeedBaseEntities(PerfumeDbContext db, int seedId)
    {
        var brand = new Brand { Id = seedId, Name = $"Brand {seedId}", Slug = $"brand-{seedId}" };
        var category = new Category { Id = seedId, Name = $"Category {seedId}", Slug = $"cat-{seedId}" };
        db.Brands.Add(brand);
        db.Categories.Add(category);
        return (brand, category);
    }

    [Fact]
    public async Task Boundary1_CampaignEndTimeInThePast_ReturnsNullAndRevertsPrice()
    {
        // Arrange: Campaign that ended 10 minutes ago
        var db = CreateInMemoryDbContext(nameof(Boundary1_CampaignEndTimeInThePast_ReturnsNullAndRevertsPrice));
        var (brand, category) = SeedBaseEntities(db, 1);
        var product = new Product
        {
            Id = 1,
            Name = "Aventus",
            Slug = "creed-aventus",
            Sku = "CRD-AVN",
            BasePrice = 8500000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true,
            IsFeatured = true
        };
        var expiredFlashSale = new FlashSale
        {
            Id = 1,
            Name = "Flash Sale Ended",
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new()
                {
                    Id = 1,
                    ProductId = 1,
                    Product = product,
                    FlashSalePrice = 6500000,
                    StockLimit = 5,
                    SoldQuantity = 1
                }
            }
        };

        db.Products.Add(product);
        db.FlashSales.Add(expiredFlashSale);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Act & Assert 1: Active campaign query returns null
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.Null(activeFs);

        // Act & Assert 2: Product detail shows standard price, NOT flash sale price
        var detail = await catalogService.GetProductBySlugAsync("creed-aventus");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);
        Assert.Equal(8500000, detail.BasePrice);

        // Act & Assert 3: Product cards do not flag flash sale
        var cards = await catalogService.GetFeaturedProductsAsync();
        var card = cards.FirstOrDefault(c => c.Id == 1);
        Assert.NotNull(card);
        Assert.False(card.IsFlashSale);
        Assert.Null(card.FlashSalePrice);

        // Act & Assert 4: DTO IsOngoing returns false
        var dto = new FlashSaleDto
        {
            IsActive = true,
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = DateTime.UtcNow.AddMinutes(-10)
        };
        Assert.False(dto.IsOngoing);
    }

    [Fact]
    public async Task Boundary2_CampaignToggledIsActiveFalse_ExcludesFromStorefront()
    {
        // Arrange: Valid ongoing time window, but IsActive = false
        var db = CreateInMemoryDbContext(nameof(Boundary2_CampaignToggledIsActiveFalse_ExcludesFromStorefront));
        var (brand, category) = SeedBaseEntities(db, 2);
        var product = new Product
        {
            Id = 2,
            Name = "Tobacco Vanille",
            Slug = "tf-tobacco-vanille",
            Sku = "TF-TV-50",
            BasePrice = 7200000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };
        var inactiveFlashSale = new FlashSale
        {
            Id = 2,
            Name = "Deactivated Flash Sale",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = false,
            Items = new List<FlashSaleItem>
            {
                new()
                {
                    Id = 2,
                    ProductId = 2,
                    Product = product,
                    FlashSalePrice = 5500000,
                    StockLimit = 10,
                    SoldQuantity = 0
                }
            }
        };

        db.Products.Add(product);
        db.FlashSales.Add(inactiveFlashSale);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Act & Assert 1: Service returns null for active campaign
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.Null(activeFs);

        // Act & Assert 2: Product detail does NOT apply flash sale
        var detail = await catalogService.GetProductBySlugAsync("tf-tobacco-vanille");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);

        // Act & Assert 3: Toggle status to active -> now returns active
        var toggleRes = await flashSaleService.ToggleStatusAsync(2);
        Assert.True(toggleRes);

        var activatedFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.NotNull(activatedFs);
        Assert.Equal(2, activatedFs.Id);
        Assert.True(activatedFs.IsActive);
        Assert.True(activatedFs.IsOngoing);

        // Act & Assert 4: Product detail now shows flash sale
        var activeDetail = await catalogService.GetProductBySlugAsync("tf-tobacco-vanille");
        Assert.NotNull(activeDetail);
        Assert.True(activeDetail.IsFlashSale);
        Assert.Equal(5500000, activeDetail.FlashSalePrice);
        Assert.True(activeDetail.FlashSaleDiscountPercent > 0);
    }

    [Fact]
    public async Task Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion()
    {
        // Arrange: Campaign is active, but SoldQuantity == StockLimit (10/10)
        var db = CreateInMemoryDbContext(nameof(Boundary3_SoldQuantityGreaterOrEqualStockLimit_ExhaustsFlashPromotion));
        var (brand, category) = SeedBaseEntities(db, 3);
        var product = new Product
        {
            Id = 3,
            Name = "Baccarat Rouge 540",
            Slug = "mfk-br540",
            Sku = "MFK-540",
            BasePrice = 9000000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };
        var flashSale = new FlashSale
        {
            Id = 3,
            Name = "Luxury Gold Rush",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new()
                {
                    Id = 3,
                    ProductId = 3,
                    Product = product,
                    FlashSalePrice = 7500000,
                    StockLimit = 10,
                    SoldQuantity = 10 // Exhausted!
                }
            }
        };

        db.Products.Add(product);
        db.FlashSales.Add(flashSale);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Act & Assert 1: Product detail automatically excludes sold-out flash item (reverts to BasePrice)
        var detail = await catalogService.GetProductBySlugAsync("mfk-br540");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);
        Assert.Equal(9000000, detail.BasePrice);

        // Act & Assert 2: Product cards automatically exclude sold-out flash item
        var (items, _) = await catalogService.GetProductsAsync(new ProductFilterParams());
        var card = items.FirstOrDefault(c => c.Id == 3);
        Assert.NotNull(card);
        Assert.False(card.IsFlashSale);
        Assert.Null(card.FlashSalePrice);

        // Act & Assert 3: DTO RemainingQuantity is 0 and never negative even if SoldQuantity > StockLimit
        var itemDto = new FlashSaleItemDto
        {
            OriginalPrice = 9000000,
            FlashSalePrice = 7500000,
            StockLimit = 10,
            SoldQuantity = 15 // edge case: oversubscribed
        };
        Assert.Equal(0, itemDto.RemainingQuantity);
    }

    [Fact]
    public async Task Boundary4_NoActiveFlashSaleCampaignExists_GracefulHandlingWithoutNRE()
    {
        // Arrange: Fresh DB with products, but ZERO flash sales
        var db = CreateInMemoryDbContext(nameof(Boundary4_NoActiveFlashSaleCampaignExists_GracefulHandlingWithoutNRE));
        var (brand, category) = SeedBaseEntities(db, 4);
        var product = new Product
        {
            Id = 4,
            Name = "Sauvage Elixir",
            Slug = "dior-sauvage-elixir",
            Sku = "DIOR-SE",
            BasePrice = 4500000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Act & Assert 1: GetActiveFlashSaleAsync returns null without exception
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.Null(activeFs);

        // Act & Assert 2: GetAllFlashSalesAsync returns empty list without exception
        var allFs = await flashSaleService.GetAllFlashSalesAsync();
        Assert.NotNull(allFs);
        Assert.Empty(allFs);

        // Act & Assert 3: GetFlashSaleByIdAsync for non-existent ID returns null
        var fsById = await flashSaleService.GetFlashSaleByIdAsync(9999);
        Assert.Null(fsById);

        // Act & Assert 4: Toggle / Delete for non-existent ID returns false safely
        var toggleResult = await flashSaleService.ToggleStatusAsync(9999);
        Assert.False(toggleResult);
        var deleteResult = await flashSaleService.DeleteAsync(9999);
        Assert.False(deleteResult);

        // Act & Assert 5: Product detail handles absence cleanly
        var detail = await catalogService.GetProductBySlugAsync("dior-sauvage-elixir");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);
        Assert.Equal(4500000, detail.BasePrice);

        // Act & Assert 6: Catalog query handles absence cleanly
        var (cards, count) = await catalogService.GetProductsAsync(new ProductFilterParams());
        Assert.Equal(1, count);
        Assert.Single(cards);
        Assert.False(cards[0].IsFlashSale);
        Assert.Null(cards[0].FlashSalePrice);
    }

    [Fact]
    public void Boundary5_EdgeCaseCalculations_ZeroPricesAndStockLimits_NoDivideByZero()
    {
        // Edge Case: OriginalPrice = 0 or negative, StockLimit = 0
        var itemDto1 = new FlashSaleItemDto
        {
            OriginalPrice = 0,
            FlashSalePrice = 500000,
            StockLimit = 0,
            SoldQuantity = 0
        };

        // Assert: DiscountPercent is 0 without DivideByZeroException
        Assert.Equal(0, itemDto1.DiscountPercent);
        Assert.Equal(0, itemDto1.RemainingQuantity);

        var itemDto2 = new FlashSaleItemDto
        {
            OriginalPrice = -100,
            FlashSalePrice = 500000,
            StockLimit = 5,
            SoldQuantity = 2
        };
        Assert.Equal(0, itemDto2.DiscountPercent);
        Assert.Equal(3, itemDto2.RemainingQuantity);

        // Standard percentage rounding test
        var itemDto3 = new FlashSaleItemDto
        {
            OriginalPrice = 2000000,
            FlashSalePrice = 1400000, // 30% discount
            StockLimit = 20,
            SoldQuantity = 5
        };
        Assert.Equal(30, itemDto3.DiscountPercent);
        Assert.Equal(15, itemDto3.RemainingQuantity);
    }

    [Fact]
    public async Task Boundary6_SoftDeletedFlashSale_IsExcludedByGlobalQueryFilter()
    {
        var db = CreateInMemoryDbContext(nameof(Boundary6_SoftDeletedFlashSale_IsExcludedByGlobalQueryFilter));
        var (brand, category) = SeedBaseEntities(db, 5);
        var product = new Product
        {
            Id = 5,
            Name = "Bleu de Chanel",
            Slug = "bleu-de-chanel",
            Sku = "BDC-EDP",
            BasePrice = 4200000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };
        var flashSale = new FlashSale
        {
            Id = 5,
            Name = "Deleted Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            IsDeleted = false,
            Items = new List<FlashSaleItem>
            {
                new()
                {
                    Id = 5,
                    ProductId = 5,
                    Product = product,
                    FlashSalePrice = 3500000,
                    StockLimit = 10,
                    SoldQuantity = 0
                }
            }
        };

        db.Products.Add(product);
        db.FlashSales.Add(flashSale);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Active before deletion
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.NotNull(activeFs);

        // Delete campaign
        var delOk = await flashSaleService.DeleteAsync(5);
        Assert.True(delOk);

        // After deletion, GetActiveFlashSaleAsync returns null
        var afterDeleteFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.Null(afterDeleteFs);

        // Catalog does not apply deleted flash sale
        var detail = await catalogService.GetProductBySlugAsync("bleu-de-chanel");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);
    }

    [Fact]
    public async Task Boundary7_CampaignStartTimeInTheFuture_IsNotYetActive()
    {
        var db = CreateInMemoryDbContext(nameof(Boundary7_CampaignStartTimeInTheFuture_IsNotYetActive));
        var (brand, category) = SeedBaseEntities(db, 7);
        var product = new Product
        {
            Id = 7,
            Name = "Future Fragrance",
            Slug = "future-fragrance",
            Sku = "FF-01",
            BasePrice = 3000000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };
        var futureFlashSale = new FlashSale
        {
            Id = 7,
            Name = "Upcoming Tomorrow Sale",
            StartTime = DateTime.UtcNow.AddHours(10), // Starts in 10 hours
            EndTime = DateTime.UtcNow.AddHours(24),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new()
                {
                    Id = 7,
                    ProductId = 7,
                    Product = product,
                    FlashSalePrice = 2000000,
                    StockLimit = 5,
                    SoldQuantity = 0
                }
            }
        };

        db.Products.Add(product);
        db.FlashSales.Add(futureFlashSale);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var catalogService = new PerfumeCatalogService(db);

        // Act: Should not be active yet
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();
        Assert.Null(activeFs);

        var detail = await catalogService.GetProductBySlugAsync("future-fragrance");
        Assert.NotNull(detail);
        Assert.False(detail.IsFlashSale);
        Assert.Null(detail.FlashSalePrice);

        // DTO check
        var dto = new FlashSaleDto
        {
            IsActive = true,
            StartTime = DateTime.UtcNow.AddHours(10),
            EndTime = DateTime.UtcNow.AddHours(24)
        };
        Assert.False(dto.IsOngoing);
    }

    [Fact]
    public async Task Boundary8_MultipleConcurrentCampaigns_PicksEarliestEnding()
    {
        var db = CreateInMemoryDbContext(nameof(Boundary8_MultipleConcurrentCampaigns_PicksEarliestEnding));
        var (brand, category) = SeedBaseEntities(db, 8);
        var product = new Product
        {
            Id = 8,
            Name = "Roja Elysium",
            Slug = "roja-elysium",
            Sku = "ROJA-ELY",
            BasePrice = 11000000,
            BrandId = brand.Id,
            Brand = brand,
            CategoryId = category.Id,
            Category = category,
            IsInStock = true
        };

        // Campaign A: ends in 1 hour
        var fsUrgent = new FlashSale
        {
            Id = 81,
            Name = "Ending Soon Flash Sale",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        // Campaign B: ends in 5 hours
        var fsLater = new FlashSale
        {
            Id = 82,
            Name = "Longer Flash Sale",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddHours(5),
            IsActive = true
        };

        db.Products.Add(product);
        db.FlashSales.AddRange(fsUrgent, fsLater);
        await db.SaveChangesAsync();

        var flashSaleService = new FlashSaleService(db);
        var activeFs = await flashSaleService.GetActiveFlashSaleAsync();

        // Must pick fsUrgent (ordered by EndTime)
        Assert.NotNull(activeFs);
        Assert.Equal(81, activeFs.Id);
        Assert.Equal("Ending Soon Flash Sale", activeFs.Name);
    }
}
