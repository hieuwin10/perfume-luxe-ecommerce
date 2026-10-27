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

public sealed class StorefrontPagesAndSeoTests : TestFixtures
{
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
