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

public sealed class ExperienceAndSwaggerApiTests : TestFixtures
{
    [Fact]
    public async Task TC_T3_082_ApiV1_TrackOrder_VerifiesOptionalPhoneParameter()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-PHONE-CHK",
            CustomerName = "Khách SĐT",
            CustomerPhone = "0909123456",
            CustomerEmail = "p@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 5_000_000m,
            OrderStatus = OrderStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        // Wrong phone -> 400 BadRequest
        var badRes = await controller.TrackOrder("LUXE-PHONE-CHK", "0999000111");
        Assert.IsType<BadRequestObjectResult>(badRes);

        // Matching last 6 digits -> 200 OK
        var okRes = await controller.TrackOrder("LUXE-PHONE-CHK", "123456");
        Assert.IsType<OkObjectResult>(okRes);
    }

    // ─── TC-T3-083: POST /api/v1/experience/tasting-booking valid -> 201 ──────
    [Fact]
    public async Task TC_T3_083_ApiV1_CreateTastingBooking_Returns201CreatedAndPersistsInDatabase()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new TastingBookingApiRequest
        {
            CustomerName = "Hoàng Tử Hương Thơm",
            PhoneNumber = "0912345678",
            Email = "prince@perfumeluxe.vn",
            BoutiqueLocation = "Flagship 88 Đồng Khởi, Quận 1, TP.HCM",
            BookingDate = DateTime.UtcNow.Date.AddDays(3),
            TimeSlot = "16:00 - 17:30",
            GuestCount = 2,
            PreferredScentFamilies = "Woody, Leather",
            Notes = "Chuẩn bị rượu Champagne"
        };

        var createdResult = Assert.IsType<ObjectResult>(await controller.CreateTastingBooking(req));
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);

        var dto = Assert.IsType<TastingBookingApiResponse>(createdResult.Value);
        Assert.StartsWith("VIP-", dto.BookingCode);
        Assert.Equal("Pending", dto.Status);
        Assert.Equal(2, dto.GuestCount);
        Assert.Equal(1, await db.PrivateTastingBookings.CountAsync());
    }

    // ─── TC-T3-084: POST /api/v1/experience/tasting-booking missing name/phone -> 400 ─
    [Theory]
    [InlineData("", "0909123456")]
    [InlineData("Nguyễn Văn A", "")]
    [InlineData("   ", "   ")]
    public async Task TC_T3_084_ApiV1_CreateTastingBooking_Returns400WhenNameOrPhoneMissing(string name, string phone)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.CreateTastingBooking(new TastingBookingApiRequest
        {
            CustomerName = name,
            PhoneNumber = phone,
            BookingDate = DateTime.UtcNow.Date.AddDays(1),
            GuestCount = 1
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-085: POST /api/v1/experience/tasting-booking invalid phone digits -> 400 ─
    [Fact]
    public async Task TC_T3_085_ApiV1_CreateTastingBooking_Returns400WhenPhoneHasTooFewDigits()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.CreateTastingBooking(new TastingBookingApiRequest
        {
            CustomerName = "Khách Hàng",
            PhoneNumber = "12345",
            BookingDate = DateTime.UtcNow.Date.AddDays(1),
            GuestCount = 2
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-086: POST /api/v1/experience/tasting-booking guestCount out of range -> 400 ─
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(20)]
    public async Task TC_T3_086_ApiV1_CreateTastingBooking_Returns400WhenGuestCountOutside1To6(int guestCount)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.CreateTastingBooking(new TastingBookingApiRequest
        {
            CustomerName = "Khách Hàng",
            PhoneNumber = "0909123456",
            BookingDate = DateTime.UtcNow.Date.AddDays(1),
            GuestCount = guestCount
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-087: POST /api/v1/experience/tasting-booking past date -> 400 ──
    [Fact]
    public async Task TC_T3_087_ApiV1_CreateTastingBooking_Returns400WhenBookingDateIsInPast()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.CreateTastingBooking(new TastingBookingApiRequest
        {
            CustomerName = "Khách Quá Khứ",
            PhoneNumber = "0909123456",
            BookingDate = DateTime.UtcNow.Date.AddDays(-2),
            GuestCount = 2
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-088: POST /api/v1/experience/tasting-booking default location/slot ─
    [Fact]
    public async Task TC_T3_088_ApiV1_CreateTastingBooking_AppliesDefaultBoutiqueLocationAndTimeSlot()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = Assert.IsType<ObjectResult>(await controller.CreateTastingBooking(new TastingBookingApiRequest
        {
            CustomerName = "Khách Mặc Định",
            PhoneNumber = "0909123456",
            BoutiqueLocation = "",
            TimeSlot = "",
            BookingDate = DateTime.UtcNow.Date.AddDays(1),
            GuestCount = 1
        }));

        var dto = Assert.IsType<TastingBookingApiResponse>(res.Value);
        Assert.Equal("Flagship 88 Tràng Tiền, Hà Nội", dto.BoutiqueLocation);
        Assert.Equal("14:00 - 15:30", dto.TimeSlot);
    }

    // ─── TC-T3-089: GET /swagger/v1/swagger.json returns OpenAPI 3.0.1 JSON ───
    [Fact]
    public void TC_T3_089_SwaggerJson_ReturnsValidOpenApi301Specification()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var contentResult = Assert.IsType<ContentResult>(controller.GetOpenApiSpec());
        Assert.Equal("application/json; charset=utf-8", contentResult.ContentType);

        using var doc = JsonDocument.Parse(contentResult.Content!);
        Assert.Equal("3.0.1", doc.RootElement.GetProperty("openapi").GetString());
        Assert.Equal("v1", doc.RootElement.GetProperty("info").GetProperty("version").GetString());
    }

    // ─── TC-T3-090: GET /swagger/v1/swagger.json documents all API v1 paths ───
    [Theory]
    [InlineData("/api/v1/flashsales/active")]
    [InlineData("/api/v1/loyalty/status")]
    [InlineData("/api/v1/discovery-box/validate-and-add")]
    [InlineData("/api/v1/orders/track")]
    [InlineData("/api/v1/experience/tasting-booking")]
    [InlineData("/api/v1/catalog/products")]
    [InlineData("/api/v1/auth/token")]
    public void TC_T3_090_SwaggerJson_ContainsAllRequiredApiV1Paths(string expectedPath)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var contentResult = Assert.IsType<ContentResult>(controller.GetOpenApiSpec());
        using var doc = JsonDocument.Parse(contentResult.Content!);
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty(expectedPath, out _), $"Missing path {expectedPath} in swagger.json");
    }

    // ─── TC-T3-091: GET /swagger/v1/swagger.json includes Bearer JWT scheme ───
    [Fact]
    public void TC_T3_091_SwaggerJson_DefinesJwtBearerSecurityScheme()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var contentResult = Assert.IsType<ContentResult>(controller.GetOpenApiSpec());
        using var doc = JsonDocument.Parse(contentResult.Content!);
        var bearer = doc.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    // ─── TC-T3-092: GET /swagger returns interactive Swagger UI HTML ──────────
    [Fact]
    public void TC_T3_092_SwaggerUi_ReturnsInteractiveHtmlWithSwaggerUiBundleAndTryItOut()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var contentResult = Assert.IsType<ContentResult>(controller.GetSwaggerUi());
        Assert.Equal("text/html; charset=utf-8", contentResult.ContentType);
        Assert.Contains("swagger-ui-bundle.js", contentResult.Content);
        Assert.Contains("/swagger/v1/swagger.json", contentResult.Content);
        Assert.Contains("tryItOutEnabled: true", contentResult.Content);
    }

    // ─── TC-T3-093: JwtTokenService creates & validates token claims ──────────
    [Fact]
    public void TC_T3_093_JwtTokenService_CreatesAndValidatesTokenWithNameIdentifierEmailAndRoles()
    {
        var config = CreateConfiguration();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Development };
        var jwtService = new JwtTokenService(config, env, NullLogger<JwtTokenService>.Instance);

        var user = new AppUser
        {
            Id = "user-jwt-001",
            Email = "jwt@perfumeluxe.vn",
            FullName = "JWT Tester"
        };

        var (token, expiresAt) = jwtService.CreateAccessToken(user, new[] { "Admin", "StoreManager" });
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTime.UtcNow);

        var principal = jwtService.ValidateAccessToken(token);
        Assert.NotNull(principal);
        Assert.Equal("user-jwt-001", principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("JWT Tester", principal.FindFirstValue(ClaimTypes.Name));
        Assert.True(principal.IsInRole("Admin"));
        Assert.True(principal.IsInRole("StoreManager"));
    }

    // ─── TC-T3-094: JwtTokenService rejects tampered or invalid tokens ────────
    [Fact]
    public void TC_T3_094_JwtTokenService_RejectsTamperedAndMalformedTokens()
    {
        var config = CreateConfiguration();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Development };
        var jwtService = new JwtTokenService(config, env, NullLogger<JwtTokenService>.Instance);

        var user = new AppUser { Id = "u-1", Email = "u@test.vn", FullName = "U" };
        var (token, _) = jwtService.CreateAccessToken(user, new[] { "Customer" });

        // Tamper with signature
        var tampered = token[..^4] + "XXXX";
        Assert.Null(jwtService.ValidateAccessToken(tampered));
        Assert.Null(jwtService.ValidateAccessToken("not.a.jwt"));
        Assert.Null(jwtService.ValidateAccessToken(""));
    }

    // ─── TC-T3-095: JwtTokenService enforces >= 32 byte key in Production ─────
    [Fact]
    public void TC_T3_095_JwtTokenService_ThrowsWhenSigningKeyMissingOrTooShortInProduction()
    {
        var prodEnv = new FakeHostEnvironment { EnvironmentName = Environments.Production };

        var missingKeyConfig = CreateConfiguration(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "" });
        Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(missingKeyConfig, prodEnv, NullLogger<JwtTokenService>.Instance));

        var shortKeyConfig = CreateConfiguration(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "too-short-key" });
        Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(shortKeyConfig, prodEnv, NullLogger<JwtTokenService>.Instance));
    }

    // ─── TC-T3-096: CustomerApiController catalog products & slug lookup ──────
    [Fact]
    public async Task TC_T3_096_CustomerApiController_ProductsAndProductBySlug_ReturnExpectedResults()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Chanel", Slug = "chanel" };
        var cat = new Category { Id = 1, Name = "Women", Slug = "women" };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(new Product
        {
            Id = 1,
            Name = "Coco Mademoiselle",
            Slug = "coco-mademoiselle",
            Sku = "CH-CM",
            BrandId = 1,
            Brand = brand,
            CategoryId = 1,
            Category = cat,
            BasePrice = 4_100_000m,
            IsInStock = true
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db);

        var listOk = Assert.IsType<OkObjectResult>(await controller.Products(new ProductFilterParams { Page = 1, PageSize = 12 }));
        Assert.NotNull(listOk.Value);

        var detailOk = Assert.IsType<OkObjectResult>(await controller.Product("coco-mademoiselle"));
        var detailDto = Assert.IsType<ProductDetailDto>(detailOk.Value);
        Assert.Equal("Coco Mademoiselle", detailDto.Name);

        var missing = await controller.Product("non-existent-slug");
        Assert.IsType<NotFoundResult>(missing);
    }

    // ─── TC-T3-097: CustomerApiController ValidateVoucher with cap ────────────

}
