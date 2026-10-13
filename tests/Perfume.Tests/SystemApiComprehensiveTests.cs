using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using Perfume.Web.Controllers;
using Perfume.Web.Security;
using Xunit;

namespace Perfume.Tests;

/// <summary>
/// Comprehensive API & Interface Contract Test Suite (TDD & API-and-Interface-Design)
/// Covers all API endpoints in the system:
/// - Auth & Token APIs (/api/v1/auth/...)
/// - Customer Profile & Orders APIs (/api/v1/customer/...)
/// - Cart & Voucher APIs (/api/v1/cart/..., /api/v1/vouchers/...)
/// - Order Tracking APIs (/api/v1/orders/...)
/// - Discovery Box API (/api/v1/discovery-box/...)
/// - Flash Sale API (/api/v1/flashsales/...)
/// - Loyalty Status API (/api/v1/loyalty/...)
/// - Experiences, Back-In-Stock & Newsletter APIs (/api/v1/experience/..., /api/newsletter/...)
/// - Swagger / OpenAPI Specification (/swagger/v1/swagger.json, /swagger)
/// </summary>
public class SystemApiComprehensiveTests
{
    private static PerfumeDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PerfumeDbContext(options);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=PerfumeDb;Trusted_Connection=True;",
            ["Jwt:Issuer"] = "PerfumeLuxe",
            ["Jwt:Audience"] = "PerfumeLuxe.Clients",
            ["Jwt:SigningKey"] = "PerfumeLuxe_SuperSecret_SigningKey_2026_AtLeast32Bytes!",
            ["Store:Name"] = "PERFUME LUXE"
        };
        if (overrides != null)
        {
            foreach (var kvp in overrides) dict[kvp.Key] = kvp.Value;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static UserManager<AppUser> CreateUserManager(PerfumeDbContext db)
    {
        var store = new UserStore<AppUser, AppRole, PerfumeDbContext, string>(db);
        var options = Options.Create(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 6
            }
        });
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new DummyServiceProvider();
        var logger = NullLogger<UserManager<AppUser>>.Instance;

        return new UserManager<AppUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, logger);
    }

    private static RoleManager<AppRole> CreateRoleManager(PerfumeDbContext db)
    {
        var store = new RoleStore<AppRole, PerfumeDbContext, string>(db);
        var validators = new List<IRoleValidator<AppRole>> { new RoleValidator<AppRole>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = NullLogger<RoleManager<AppRole>>.Instance;
        return new RoleManager<AppRole>(store, validators, keyNormalizer, errors, logger);
    }

    private static CustomerApiController CreateCustomerApiController(
        PerfumeDbContext db,
        string? userId = null,
        string? email = null,
        params string[] roles)
    {
        var catalog = new PerfumeCatalogService(db);
        var carts = new CustomerCartService(db);
        var orders = new OrderService(db);
        var emailSender = new FakeOrderEmailSender();
        var users = CreateUserManager(db);
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var claimsFactory = new UserClaimsPrincipalFactory<AppUser, AppRole>(
            users,
            CreateRoleManager(db),
            Options.Create(new IdentityOptions()));
        var signIn = new SignInManager<AppUser>(
            users,
            httpContextAccessor,
            claimsFactory,
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<AppUser>>.Instance,
            new Microsoft.AspNetCore.Authentication.AuthenticationSchemeProvider(Options.Create(new Microsoft.AspNetCore.Authentication.AuthenticationOptions())),
            new DefaultUserConfirmation<AppUser>());
        var antiforgery = new FakeAntiforgery();
        var config = CreateConfiguration();
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Development };
        var jwt = new JwtTokenService(config, env, NullLogger<JwtTokenService>.Instance);
        var protectionProvider = new EphemeralDataProtectionProvider();

        var controller = new CustomerApiController(
            catalog, carts, orders, emailSender, db, users, signIn, antiforgery, jwt, protectionProvider);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer test-jwt-token";

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, email ?? "user@perfumeluxe.vn"),
                new(ClaimTypes.Email, email ?? "user@perfumeluxe.vn")
            };
            claims.AddRange((roles.Length == 0 ? new[] { "Customer" } : roles).Select(r => new Claim(ClaimTypes.Role, r)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static ApiV1ExtensionsController CreateExtensionsController(
        PerfumeDbContext db,
        string? userId = null,
        string? email = null,
        params string[] roles)
    {
        var flashSale = new FlashSaleService(db);
        var controller = new ApiV1ExtensionsController(db, flashSale);

        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, email ?? "user@perfumeluxe.vn"),
                new(ClaimTypes.Email, email ?? "user@perfumeluxe.vn")
            };
            claims.AddRange((roles.Length == 0 ? new[] { "Customer" } : roles).Select(r => new Claim(ClaimTypes.Role, r)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Perfume.Web";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeOrderEmailSender : IOrderEmailSender
    {
        public Task SendOrderConfirmationAsync(Order order, string? trackingUrl = null) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string fullName, string resetUrl) => Task.CompletedTask;
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => new("cookie", "form", "field", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => new("cookie", "form", "field", "header");
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. AUTHENTICATION & JWT TOKEN LIFECYCLE APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_001_CreateToken_ValidCredentials_Returns200WithBearerAndRefreshToken()
    {
        using var db = CreateInMemoryDbContext();
        var roleManager = CreateRoleManager(db);
        await roleManager.CreateAsync(new AppRole { Name = "Customer", Description = "Customer" });

        var userManager = CreateUserManager(db);
        var user = new AppUser { Id = "user-jwt-1", UserName = "tester@luxe.vn", Email = "tester@luxe.vn", FullName = "Tester" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, "Customer");

        var controller = CreateCustomerApiController(db);
        var request = new CustomerLoginRequest { Email = "tester@luxe.vn", Password = "Password123!" };

        var result = await controller.CreateToken(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiTokenResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    [Fact]
    public async Task TC_API_002_CreateToken_InvalidPassword_Returns401Unauthorized()
    {
        using var db = CreateInMemoryDbContext();
        var userManager = CreateUserManager(db);
        var user = new AppUser { Id = "user-jwt-2", UserName = "tester2@luxe.vn", Email = "tester2@luxe.vn", FullName = "Tester 2" };
        await userManager.CreateAsync(user, "Password123!");

        var controller = CreateCustomerApiController(db);
        var request = new CustomerLoginRequest { Email = "tester2@luxe.vn", Password = "WrongPassword!" };

        var result = await controller.CreateToken(request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task TC_API_003_RefreshToken_ValidToken_Returns200WithNewAccessToken()
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser { Id = "user-rf-1", UserName = "rf@luxe.vn", Email = "rf@luxe.vn", FullName = "Refresh Tester" };
        db.Users.Add(user);
        var rawToken = "valid-refresh-token-12345";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        db.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db);
        var result = await controller.RefreshToken(new RefreshTokenRequest { RefreshToken = rawToken }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiTokenResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    [Fact]
    public async Task TC_API_004_RefreshToken_RevokedOrExpiredToken_Returns401Unauthorized()
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser { Id = "user-rf-2", UserName = "rf2@luxe.vn", Email = "rf2@luxe.vn" };
        db.Users.Add(user);
        var rawToken = "revoked-token-123";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        db.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db);
        var result = await controller.RefreshToken(new RefreshTokenRequest { RefreshToken = rawToken }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task TC_API_005_RevokeToken_ValidToken_RevokesAndReturns204NoContent()
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser { Id = "user-rv-1", UserName = "rv@luxe.vn", Email = "rv@luxe.vn" };
        db.Users.Add(user);
        var rawToken = "token-to-revoke";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        db.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: user.Id);
        var result = await controller.RevokeToken(new RefreshTokenRequest { RefreshToken = rawToken }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var updated = await db.RefreshTokenRecords.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        Assert.NotNull(updated?.RevokedAt);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. CUSTOMER PROFILE & ORDERS APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_006_GetProfile_Authenticated_ReturnsCustomerData()
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser
        {
            Id = "cust-prof-1",
            UserName = "prof@luxe.vn",
            Email = "prof@luxe.vn",
            FullName = "Lady Victoria",
            PhoneNumber = "0988776655",
            Address = "88 Đồng Khởi, Q1",
            LoyaltyPoints = 250,
            Tier = UserTier.Silver
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: user.Id, email: user.Email);
        var result = await controller.Profile();

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CustomerProfileDto>(ok.Value);
        Assert.Equal("Lady Victoria", dto.FullName);
        Assert.Equal("prof@luxe.vn", dto.Email);
        Assert.Equal(250, dto.LoyaltyPoints);
        Assert.Equal("Silver", dto.Tier);
    }

    [Fact]
    public async Task TC_API_007_UpdateProfile_ValidPayload_PersistsChangesAndReturns200()
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser
        {
            Id = "cust-prof-2",
            UserName = "upd@luxe.vn",
            Email = "upd@luxe.vn",
            FullName = "Old Name"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: user.Id, email: user.Email);
        var updateReq = new CustomerProfileUpdateRequest
        {
            FullName = "New Royal Name",
            PhoneNumber = "0912345678",
            Address = "Villa 10, Thảo Điền"
        };

        var result = await controller.UpdateProfile(updateReq);

        var ok = Assert.IsType<OkObjectResult>(result);
        var refreshed = await db.Users.FindAsync(user.Id);
        Assert.Equal("New Royal Name", refreshed?.FullName);
        Assert.Equal("0912345678", refreshed?.PhoneNumber);
        Assert.Equal("Villa 10, Thảo Điền", refreshed?.Address);
    }

    [Fact]
    public async Task TC_API_008_MyOrders_ReturnsOnlyCurrentUserOrders()
    {
        using var db = CreateInMemoryDbContext();
        db.Orders.AddRange(
            new Order { OrderCode = "ORD-USER-1", UserId = "user-ord-1", TotalAmount = 1_000_000m, CustomerName = "User 1" },
            new Order { OrderCode = "ORD-USER-2", UserId = "user-ord-2", TotalAmount = 2_000_000m, CustomerName = "User 2" }
        );
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "user-ord-1");
        var result = await controller.MyOrders(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<IEnumerable<CustomerOrderDto>>(ok.Value);
        Assert.Single(orders);
        Assert.Equal("ORD-USER-1", orders.First().OrderCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. VIP LOYALTY STATUS APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, "Bronze", 0, "Silver", 5000000)]
    [InlineData(5000000, "Silver", 3, "Gold", 10000000)]
    [InlineData(15000000, "Gold", 7, "Diamond", 15000000)]
    [InlineData(30000000, "Diamond", 12, "Max", 0)]
    public async Task TC_API_009_GetLoyaltyStatus_AllTiers_ReturnAccuratePrivileges(
        decimal totalSpent, string expectedTier, int expectedDiscount, string expectedNextTier, decimal expectedAmountToNext)
    {
        using var db = CreateInMemoryDbContext();
        var user = new AppUser { Id = $"vip-tier-{expectedTier}", UserName = $"vip-{expectedTier}@luxe.vn" };
        db.Users.Add(user);

        if (totalSpent > 0)
        {
            db.Orders.Add(new Order
            {
                UserId = user.Id,
                OrderCode = $"ORD-VIP-{expectedTier}",
                TotalAmount = totalSpent,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed,
                CustomerName = "VIP Tester"
            });
        }
        await db.SaveChangesAsync();

        var controller = CreateExtensionsController(db, userId: user.Id, email: user.UserName);
        var result = await controller.GetLoyaltyStatus();

        var ok = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);
        Assert.Equal(expectedTier, status.Tier);
        Assert.Equal(expectedDiscount, status.DiscountPercent);
        Assert.Equal(expectedNextTier, status.NextTier);
        Assert.Equal(expectedAmountToNext, status.AmountToNextTier);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. CART & INVENTORY BOUNDARY APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_010_Cart_AddAndModifyItem_EnforcesStockAndCalculatesTotal()
    {
        using var db = CreateInMemoryDbContext();
        var brand = new Brand { Name = "Roja Parfums", Slug = "roja" };
        var product = new Product { Name = "Elysium Parfum", Slug = "elysium", Brand = brand, BasePrice = 8_000_000m, IsInStock = true };
        var variant = new ProductVariant { Product = product, VariantSku = "ELY-100", VolumeMl = 100, SalePrice = 8_000_000m, StockQuantity = 5 };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "cart-user-1");

        // 1. Add 2 units
        var addRes = await controller.AddCartItem(new CartAddItemRequest { ProductSlug = product.Slug, VariantId = variant.Id, Quantity = 2 }, CancellationToken.None);
        var okAdd = Assert.IsType<OkObjectResult>(addRes);
        var cartDto = Assert.IsType<CartStateDto>(okAdd.Value);
        Assert.Equal(16_000_000m, cartDto.SubTotal);
        Assert.Equal(2, cartDto.TotalCount);

        // 2. Set quantity exceeding stock limit (5 in stock, request 10)
        var setRes = await controller.SetCartItemQuantity(new CartLineRequest { ProductId = product.Id, VariantId = variant.Id, Quantity = 10 }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(setRes);

        // 3. Remove item from cart
        var delRes = await controller.RemoveCartItem(product.Id, variant.Id, CancellationToken.None);
        var okDel = Assert.IsType<OkObjectResult>(delRes);
        var emptiedCart = Assert.IsType<CartStateDto>(okDel.Value);
        Assert.Equal(0, emptiedCart.TotalCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5. VOUCHER & CHECKOUT ENGINE APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_011_ValidateVoucher_AppliesDiscountWithMaxCap()
    {
        using var db = CreateInMemoryDbContext();
        var brand = new Brand { Name = "Clive Christian", Slug = "clive" };
        var product = new Product { Name = "No. 1", Slug = "no1", Brand = brand, BasePrice = 10_000_000m, IsInStock = true };
        var variant = new ProductVariant { Product = product, VolumeMl = 50, SalePrice = 10_000_000m, StockQuantity = 10 };
        var voucher = new Voucher
        {
            Code = "ROYAL20",
            DiscountPercent = 20, // 20%
            MaxDiscountAmount = 1_000_000m, // Cap at 1M
            MinOrderAmount = 5_000_000m,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1)
        };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);
        db.Vouchers.Add(voucher);

        var cart = new ShoppingCart { UserId = "vouch-user", OwnerKey = "user:vouch-user", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        cart.Items.Add(new ShoppingCartItem { ProductId = product.Id, VariantId = variant.Id, Quantity = 1 });
        db.ShoppingCarts.Add(cart);
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "vouch-user");
        var result = await controller.ValidateVoucher(new VoucherValidationRequest { Code = "royal20" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<VoucherValidationResponse>(ok.Value);
        Assert.True(response.Valid);
        Assert.Equal(1_000_000m, response.DiscountAmount); // Capped at 1,000,000đ instead of 2,000,000đ
    }

    [Fact]
    public async Task TC_API_012_PlaceOrder_AppliesFreeShippingAndDeductsInventory()
    {
        using var db = CreateInMemoryDbContext();
        var brand = new Brand { Name = "Creed", Slug = "creed" };
        var product = new Product { Name = "Aventus", Slug = "aventus", Brand = brand, BasePrice = 7_500_000m, IsInStock = true };
        var variant = new ProductVariant { Product = product, VariantSku = "AV-100", VolumeMl = 100, SalePrice = 7_500_000m, StockQuantity = 10 };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);

        var cart = new ShoppingCart { UserId = "order-user", OwnerKey = "user:order-user", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        cart.Items.Add(new ShoppingCartItem { ProductId = product.Id, VariantId = variant.Id, Quantity = 1 });
        db.ShoppingCarts.Add(cart);
        await db.SaveChangesAsync();

        var controller = CreateCustomerApiController(db, userId: "order-user");
        var orderReq = new CustomerOrderRequest
        {
            CustomerName = "Lord Harrington",
            CustomerPhone = "0909998877",
            CustomerEmail = "harrington@nobility.uk",
            ShippingAddress = "72 Lê Thánh Tôn, Q1, HCM",
            PaymentMethod = PaymentMethod.BankTransfer,
            IsGiftWrapping = true,
            GiftMessage = "Royal Wishes"
        };

        var result = await controller.PlaceOrder(orderReq, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var orderResponse = Assert.IsType<GuestOrderResponse>(ok.Value);
        var orderCode = orderResponse.Order.OrderCode;
        Assert.NotNull(orderCode);
        Assert.StartsWith("LUXE-", orderCode);

        // Verify DB State
        var createdOrder = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        Assert.NotNull(createdOrder);
        Assert.Equal(0m, createdOrder.ShippingFee); // Free shipping because >= 1,000,000đ
        Assert.Equal(50_000m, createdOrder.GiftWrappingFee);
        Assert.Equal(7_550_000m, createdOrder.TotalAmount);

        // Verify stock deducted
        var updatedVariant = await db.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(9, updatedVariant?.StockQuantity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 6. CUSTOM DISCOVERY GIFT BOX 3-STEP BUILDER API
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_013_DiscoveryBox_Valid3StepConfiguration_ComputesPriceAndDiscounts()
    {
        using var db = CreateInMemoryDbContext();
        var p1 = new Product { Name = "Fragrance 1", BasePrice = 5_000_000m, IsInStock = true };
        var p2 = new Product { Name = "Fragrance 2", BasePrice = 6_000_000m, IsInStock = true };
        var p3 = new Product { Name = "Fragrance 3", BasePrice = 7_000_000m, IsInStock = true };
        db.Products.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var controller = CreateExtensionsController(db, userId: "disc-user");
        var request = new DiscoveryBoxApiRequest
        {
            BoxStyle = "RoyalVelvet",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = p1.Id, VialSize = "2ml" },
                new() { ProductId = p2.Id, VialSize = "2ml" },
                new() { ProductId = p3.Id, VialSize = "5ml" }
            },
            GreetingMessage = "Happy Birthday"
        };

        var result = await controller.ValidateAndAddDiscoveryBox(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);
        Assert.True(response.Valid);
        Assert.True(response.TotalPrice > 0);
        Assert.Equal(3, response.VialCount);
    }

    [Theory]
    [InlineData("InvalidStyle", 3, "2ml", "Valid message", "Kiểu hộp không hợp lệ")]
    [InlineData("RoyalVelvet", 2, "2ml", "Valid message", "Discovery Box yêu cầu chọn từ 3 đến 5 ống chiết")]
    [InlineData("RoyalVelvet", 6, "2ml", "Valid message", "Discovery Box yêu cầu chọn từ 3 đến 5 ống chiết")]
    public async Task TC_API_014_DiscoveryBox_BoundaryViolations_Return400BadRequest(
        string style, int vialCount, string volume, string msg, string expectedSubError)
    {
        using var db = CreateInMemoryDbContext();
        var p1 = new Product { Name = "Sample", BasePrice = 5_000_000m, IsInStock = true };
        db.Products.Add(p1);
        await db.SaveChangesAsync();

        var controller = CreateExtensionsController(db);
        var vials = Enumerable.Range(1, vialCount).Select(_ => new DiscoveryVialApiSelectionDto { ProductId = p1.Id, VialSize = volume }).ToList();

        var request = new DiscoveryBoxApiRequest
        {
            BoxStyle = style,
            Vials = vials,
            GreetingMessage = msg
        };

        var result = await controller.ValidateAndAddDiscoveryBox(request);
        var badReq = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badReq.Value, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        Assert.Contains(expectedSubError, json);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 7. FLASH SALE & PROMOTION APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_015_GetActiveFlashSale_OngoingCampaign_Returns200WithDiscountMetrics()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Product { Name = "Baccarat Rouge 540", BasePrice = 8_000_000m, IsInStock = true };
        db.Products.Add(product);

        var fs = new FlashSale
        {
            Name = "Flash Sale Midnight Luxury",
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddMinutes(90),
            IsActive = true
        };
        fs.Items.Add(new FlashSaleItem
        {
            Product = product,
            FlashSalePrice = 6_400_000m,
            StockLimit = 20,
            SoldQuantity = 5
        });
        db.FlashSales.Add(fs);
        await db.SaveChangesAsync();

        var controller = CreateExtensionsController(db);
        var result = await controller.GetActiveFlashSale();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FlashSaleActiveApiResponse>(ok.Value);
        Assert.Equal("Flash Sale Midnight Luxury", response.Name);
        Assert.True(response.RemainingSeconds > 0);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task TC_API_016_GetActiveFlashSale_NoActiveCampaign_Returns404NotFound()
    {
        using var db = CreateInMemoryDbContext();
        var controller = CreateExtensionsController(db);
        var result = await controller.GetActiveFlashSale();
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 8. EXPERIENCES, BACK-IN-STOCK, NEWSLETTER & ORDER TRACKING APIS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_API_017_TastingBooking_ValidRequest_CreatesBookingAndReturns201()
    {
        using var db = CreateInMemoryDbContext();
        var controller = CreateExtensionsController(db);
        var request = new TastingBookingApiRequest
        {
            CustomerName = "Duchess Catherine",
            PhoneNumber = "0933221100",
            Email = "catherine@monarchy.uk",
            BoutiqueLocation = "Flagship 88 Tràng Tiền",
            BookingDate = DateTime.Today.AddDays(2),
            TimeSlot = "15:00 - 16:30",
            GuestCount = 2
        };

        var result = await controller.CreateTastingBooking(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var bookingInDb = await db.PrivateTastingBookings.FirstOrDefaultAsync(b => b.CustomerName == "Duchess Catherine");
        Assert.NotNull(bookingInDb);
        Assert.Equal("Pending", bookingInDb.Status);
    }

    [Fact]
    public async Task TC_API_018_SubscribeBackInStock_ValidPayload_PersistsNotificationAlert()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Product { Name = "Sold Out Luxury", BasePrice = 12_000_000m };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var controller = new ExperienceController(db);
        var request = new BackInStockSubscribeRequest
        {
            ProductId = product.Id,
            Email = "waitlist@perfumeluxe.vn",
            Phone = "0988001122"
        };

        var result = await controller.SubscribeBackInStock(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var alert = await db.BackInStockAlerts.FirstOrDefaultAsync(a => a.CustomerEmail == "waitlist@perfumeluxe.vn");
        Assert.NotNull(alert);
        Assert.Equal(product.Id, alert.ProductId);
    }

    [Fact]
    public void TC_API_019_SubscribeNewsletter_ValidEmail_ReturnsPromoVoucher()
    {
        using var db = CreateInMemoryDbContext();
        var catalog = new PerfumeCatalogService(db);
        var blog = new BlogService(db);
        var flash = new FlashSaleService(db);
        var controller = new HomeController(catalog, blog, flash, db);

        var result = controller.SubscribeNewsletter(new NewsletterSubscribeRequest { Email = "vip.guest@gmail.com" });

        var jsonResult = Assert.IsType<JsonResult>(result);
        var json = JsonSerializer.Serialize(jsonResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("LUXE2026", doc.RootElement.GetProperty("voucher").GetString());
    }

    [Fact]
    public async Task TC_API_020_TrackOrder_ValidAndInvalidOrderCodes()
    {
        using var db = CreateInMemoryDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-TRACK-999",
            OrderStatus = OrderStatus.Shipping,
            PaymentStatus = PaymentStatus.Completed,
            TotalAmount = 5_000_000m,
            CustomerName = "Lord Sterling"
        });
        await db.SaveChangesAsync();

        var controller = CreateExtensionsController(db);

        // 1. Valid code
        var validRes = await controller.TrackOrder("ord-track-999");
        var ok = Assert.IsType<OkObjectResult>(validRes);
        var response = Assert.IsType<OrderTrackApiResponse>(ok.Value);
        Assert.Equal("ORD-TRACK-999", response.OrderCode);
        Assert.Equal("Shipping", response.OrderStatus);

        // 2. Non-existent code
        var notFoundRes = await controller.TrackOrder("ORD-NOT-FOUND");
        Assert.IsType<NotFoundObjectResult>(notFoundRes);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 9. OPENAPI SPECIFICATION & SWAGGER UI ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TC_API_021_SwaggerJson_ReturnsValidOpenApi3Spec()
    {
        using var db = CreateInMemoryDbContext();
        var controller = CreateExtensionsController(db);

        var result = controller.GetOpenApiSpec();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("application/json; charset=utf-8", contentResult.ContentType);
        Assert.NotNull(contentResult.Content);

        using var doc = JsonDocument.Parse(contentResult.Content);
        var root = doc.RootElement;
        Assert.Equal("3.0.1", root.GetProperty("openapi").GetString());
        Assert.True(root.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/api/v1/flashsales/active", out _));
        Assert.True(paths.TryGetProperty("/api/v1/loyalty/status", out _));
        Assert.True(paths.TryGetProperty("/api/v1/discovery-box/validate-and-add", out _));
        Assert.True(paths.TryGetProperty("/api/v1/orders/track", out _));
        Assert.True(paths.TryGetProperty("/api/v1/experience/tasting-booking", out _));
    }

    [Fact]
    public void TC_API_022_SwaggerUi_ReturnsInteractiveHtmlPage()
    {
        using var db = CreateInMemoryDbContext();
        var controller = CreateExtensionsController(db);

        var result = controller.GetSwaggerUi();

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", contentResult.ContentType);
        Assert.NotNull(contentResult.Content);
        Assert.Contains("SwaggerUIBundle", contentResult.Content);
        Assert.Contains("/swagger/v1/swagger.json", contentResult.Content);
    }
}
