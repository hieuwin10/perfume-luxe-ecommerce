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

public sealed class CustomerApiTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_097_CustomerApiController_ValidateVoucher_ComputesCappedDiscountOnCart()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Dior", Slug = "dior" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "J'adore", Slug = "jadore", Sku = "DR-JD", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 4_000_000m, IsInStock = true };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.Vouchers.Add(new Voucher
        {
            Code = "VIP20",
            Description = "Giảm 20% tối đa 500k",
            DiscountPercent = 20m,
            MinOrderAmount = 2_000_000m,
            MaxDiscountAmount = 500_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var carts = new CustomerCartService(db);
        await carts.AddAsync("cust-v1", null, new CartAddItemRequest { ProductSlug = "jadore", Quantity = 1 });

        var controller = CreateCustomerApiController(db, userId: "cust-v1");
        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateVoucher(new VoucherValidationRequest { Code = "VIP20" }, CancellationToken.None));
        var res = Assert.IsType<VoucherValidationResponse>(ok.Value);

        Assert.True(res.Valid);
        // 20% of 4,000,000 is 800,000, capped at 500,000
        Assert.Equal(500_000m, res.DiscountAmount);
    }

    // ─── TC-T3-098: CustomerApiController Profile & MyOrders ──────────────────
    [Fact]
    public async Task TC_T3_098_CustomerApiController_ProfileAndMyOrders_ReturnAuthenticatedCustomerData()
    {
        using var db = CreateDbContext();
        var user = new AppUser
        {
            Id = "cust-98",
            UserName = "c98@test.vn",
            Email = "c98@test.vn",
            FullName = "Khách Hàng 98",
            Tier = UserTier.Gold,
            LoyaltyPoints = 250
        };
        db.Users.Add(user);
        db.Orders.Add(new Order
        {
            Id = 501,
            OrderCode = "LUXE-C98-01",
            UserId = "cust-98",
            CustomerName = "Khách Hàng 98",
            CustomerPhone = "0909",
            CustomerEmail = "c98@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 5_200_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "cust-98");

        var profileOk = Assert.IsType<OkObjectResult>(await controller.Profile());
        var profileDto = Assert.IsType<CustomerProfileDto>(profileOk.Value);
        Assert.Equal("Khách Hàng 98", profileDto.FullName);
        Assert.Equal("Gold", profileDto.Tier);

        var ordersOk = Assert.IsType<OkObjectResult>(await controller.MyOrders(CancellationToken.None));
        var ordersList = Assert.IsAssignableFrom<IEnumerable<CustomerOrderDto>>(ordersOk.Value).ToList();
        Assert.Single(ordersList);
        Assert.Equal("LUXE-C98-01", ordersList[0].OrderCode);
    }

    // ─── TC-T3-099: CustomerApiController ProductReviews filters unapproved ───
    [Fact]
    public async Task TC_T3_099_CustomerApiController_ProductReviews_ReturnsOnlyApprovedReviewsAndComputesAverage()
    {
        using var db = CreateDbContext();
        db.Reviews.AddRange(
            new Review { ProductId = 10, UserName = "A", Rating = 5, Comment = "Tuyệt vời", IsApproved = true },
            new Review { ProductId = 10, UserName = "B", Rating = 4, Comment = "Thơm lâu", IsApproved = true },
            new Review { ProductId = 10, UserName = "Spam", Rating = 1, Comment = "Chưa duyệt", IsApproved = false });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db);
        var ok = Assert.IsType<OkObjectResult>(await controller.ProductReviews(10, CancellationToken.None));
        var res = Assert.IsType<ProductReviewsResponse>(ok.Value);

        Assert.Equal(2, res.RatingCount);
        Assert.Equal(4.5, res.RatingAverage);
        Assert.Equal(2, res.Items.Count);
    }

    // ─── TC-T3-100: CustomerApiController RevokeToken marks RevokedAt ─────────
    [Fact]
    public async Task TC_T3_100_CustomerApiController_RevokeToken_RevokesRefreshTokenAndReturnsNoContent()
    {
        using var db = CreateDbContext();
        const string rawToken = "my-secret-refresh-token-100";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

        db.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            UserId = "cust-100",
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(15),
            RevokedAt = null
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "cust-100");
        var res = await controller.RevokeToken(new RefreshTokenRequest { RefreshToken = rawToken }, CancellationToken.None);
        Assert.IsType<NoContentResult>(res);

        var record = await db.RefreshTokenRecords.FirstAsync(r => r.TokenHash == tokenHash);
        Assert.NotNull(record.RevokedAt);
    }

}
