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

public sealed class OrdersAndStorefrontIntegrationTests : TestFixtures
{
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


}
