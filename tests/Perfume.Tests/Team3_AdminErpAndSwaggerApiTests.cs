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

public class Team3_AdminErpAndSwaggerApiTests
{
    // ─── TEST INFRASTRUCTURE HELPERS ──────────────────────────────────────────

    private static PerfumeDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(dbName ?? $"Team3_TestDb_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new PerfumeDbContext(options);
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
        var validators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new DummyServiceProvider();
        var logger = NullLogger<UserManager<AppUser>>.Instance;

        return new UserManager<AppUser>(
            store,
            options,
            passwordHasher,
            validators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
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

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=PerfumeDb;Trusted_Connection=True;TrustServerCertificate=True;",
            ["Store:Name"] = "PERFUME LUXE",
            ["Store:Slogan"] = "Haute Parfumerie & Niche Fragrances",
            ["Store:Hotline"] = "1900 8888",
            ["Store:ContactEmail"] = "concierge@perfumeluxe.vn",
            ["Store:Address"] = "88 Đồng Khởi, Quận 1, TP.HCM",
            ["Smtp:Enabled"] = "false",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "1025",
            ["Jwt:Issuer"] = "PerfumeLuxe",
            ["Jwt:Audience"] = "PerfumeLuxe.Clients",
            ["Jwt:SigningKey"] = "PerfumeLuxe_SuperSecret_SigningKey_2026_AtLeast32Bytes!"
        };

        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                settings[kvp.Key] = kvp.Value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static void AttachHttpContext(Controller controller, string? userId = null, string? email = null, params string[] roles)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, email ?? "user@perfumeluxe.vn"),
                new(ClaimTypes.Email, email ?? "user@perfumeluxe.vn")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
    }

    private static void AttachApiContext(ControllerBase controller, string? userId = null, string? email = null, bool bearerHeader = true, params string[] roles)
    {
        var httpContext = new DefaultHttpContext();
        if (bearerHeader)
        {
            httpContext.Request.Headers.Authorization = "Bearer test-jwt-token";
        }

        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, email ?? "vip@perfumeluxe.vn"),
                new(ClaimTypes.Email, email ?? "vip@perfumeluxe.vn")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static AdminDashboardController CreateAdminController(
        PerfumeDbContext db,
        IDatabaseConsoleService? dbConsole = null,
        IConfiguration? config = null,
        string? actorUserId = "admin-1",
        params string[] roles)
    {
        var configuration = config ?? CreateConfiguration();
        var userManager = CreateUserManager(db);
        var flashSaleService = new FlashSaleService(db);
        var settingService = new SystemSettingService(db, configuration, NullLogger<SystemSettingService>.Instance);
        var blogService = new BlogService(db);
        var consoleService = dbConsole ?? new FakeDatabaseConsoleService();

        var controller = new AdminDashboardController(
            db,
            consoleService,
            userManager,
            flashSaleService,
            settingService,
            blogService,
            configuration);

        AttachHttpContext(controller, actorUserId, "admin@perfumeluxe.vn", roles.Length == 0 ? new[] { "Admin" } : roles);
        return controller;
    }

    private static CustomerApiController CreateCustomerApiController(
        PerfumeDbContext db,
        string? userId = null,
        params string[] roles)
    {
        var catalog = new PerfumeCatalogService(db);
        var carts = new CustomerCartService(db);
        var orders = new OrderService(db);
        var email = new FakeOrderEmailSender();
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
            catalog,
            carts,
            orders,
            email,
            db,
            users,
            signIn,
            antiforgery,
            jwt,
            protectionProvider);

        AttachApiContext(controller, userId, "customer@perfumeluxe.vn", true, roles.Length == 0 ? new[] { "Customer" } : roles);
        return controller;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PHẦN 5: ADMIN ERP, FINANCIAL P&L, 5-TIER RBAC, SECURITY AUDIT (TC-T3-001..050)
    // ═══════════════════════════════════════════════════════════════════════════

    // ─── TC-T3-001: FinancialReport Gross Sales, Discounts, Fees & Net Revenue ─
    [Fact]
    public async Task TC_T3_001_FinancialReport_ComputesGrossSalesDiscountsFeesAndNetRevenue()
    {
        using var db = CreateDbContext();
        db.Orders.AddRange(
            new Order
            {
                OrderCode = "LUXE-FIN-001",
                CustomerName = "Nguyễn Văn A",
                CustomerPhone = "0901111111",
                CustomerEmail = "a@test.vn",
                ShippingAddress = "Q1, HCM",
                SubTotal = 5_000_000m,
                DiscountAmount = 500_000m,
                GiftWrappingFee = 50_000m,
                ShippingFee = 0m,
                TotalAmount = 4_550_000m,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Order
            {
                OrderCode = "LUXE-FIN-002",
                CustomerName = "Trần Thị B",
                CustomerPhone = "0902222222",
                CustomerEmail = "b@test.vn",
                ShippingAddress = "Q3, HCM",
                SubTotal = 1_500_000m,
                DiscountAmount = 100_000m,
                GiftWrappingFee = 0m,
                ShippingFee = 35_000m,
                TotalAmount = 1_435_000m,
                OrderStatus = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(2, vm.TotalOrdersCount);
        Assert.Equal(6_500_000m, vm.GrossSales);
        Assert.Equal(600_000m, vm.VoucherDiscounts);
        Assert.Equal(50_000m, vm.GiftWrappingFeesCollected);
        Assert.Equal(35_000m, vm.ShippingFeesCollected);
        Assert.Equal(5_985_000m, vm.NetRevenue);
    }

    // ─── TC-T3-002: FinancialReport excludes Pending/Unpaid & Cancelled orders ─
    [Fact]
    public async Task TC_T3_002_FinancialReport_ExcludesPendingUnpaidAndCancelledOrders()
    {
        using var db = CreateDbContext();
        db.Orders.AddRange(
            new Order
            {
                OrderCode = "LUXE-DELIVERED",
                CustomerName = "Khách Hoàn Tất",
                CustomerPhone = "0901",
                CustomerEmail = "ok@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 4_000_000m,
                TotalAmount = 4_000_000m,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "LUXE-PENDING-UNPAID",
                CustomerName = "Khách Chờ",
                CustomerPhone = "0902",
                CustomerEmail = "wait@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 9_000_000m,
                TotalAmount = 9_000_000m,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "LUXE-CANCELLED",
                CustomerName = "Khách Hủy",
                CustomerPhone = "0903",
                CustomerEmail = "cancel@test.vn",
                ShippingAddress = "HCM",
                SubTotal = 7_000_000m,
                TotalAmount = 7_000_000m,
                OrderStatus = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(1, vm.TotalOrdersCount);
        Assert.Equal(4_000_000m, vm.NetRevenue);
    }

    // ─── TC-T3-003: FinancialReport COGS uses latest StockAdjustment.UnitCost ─
    [Fact]
    public async Task TC_T3_003_FinancialReport_CogsUsesLatestStockAdjustmentUnitCost()
    {
        using var db = CreateDbContext();
        db.StockAdjustments.AddRange(
            new StockAdjustment
            {
                VariantId = 101,
                OldQuantity = 0,
                AdjustedQuantity = 10,
                NewQuantity = 10,
                UnitCost = 2_000_000m,
                Reason = "Lô cũ"
            },
            new StockAdjustment
            {
                VariantId = 101,
                OldQuantity = 10,
                AdjustedQuantity = 5,
                NewQuantity = 15,
                UnitCost = 2_400_000m,
                Reason = "Lô mới"
            });
        await db.SaveChangesAsync();

        // Ensure second adjustment has a strictly newer CreatedAt timestamp
        var latestAdj = await db.StockAdjustments.FirstAsync(a => a.Reason == "Lô mới");
        latestAdj.CreatedAt = DateTime.UtcNow.AddMinutes(5);

        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-COGS-001",
            CustomerName = "Khách VIP",
            CustomerPhone = "0909",
            CustomerEmail = "vip@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 12_000_000m,
            TotalAmount = 12_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101,
                    ProductName = "Santal 33",
                    VariantLabel = "100ml",
                    UnitPrice = 6_000_000m,
                    Quantity = 2
                }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 2 units * 2,400,000 latest UnitCost = 4,800,000
        Assert.Equal(4_800_000m, vm.TotalCogs);
        Assert.Equal(7_200_000m, vm.GrossProfit);
        Assert.Equal(60.0, vm.GrossMarginPercent);
    }

    // ─── TC-T3-004: FinancialReport COGS falls back to 42% of retail price ────
    [Fact]
    public async Task TC_T3_004_FinancialReport_CogsFallsBackTo42PercentWhenNoUnitCostRecorded()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-COGS-42",
            CustomerName = "Khách 42%",
            CustomerPhone = "0908",
            CustomerEmail = "c42@test.vn",
            ShippingAddress = "HN",
            SubTotal = 5_000_000m,
            TotalAmount = 5_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = 2,
                    VariantId = 202,
                    ProductName = "Baccarat Rouge 540",
                    VariantLabel = "70ml",
                    UnitPrice = 5_000_000m,
                    Quantity = 1
                }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 5,000,000 * 0.42 = 2,100,000
        Assert.Equal(2_100_000m, vm.TotalCogs);
        Assert.Equal(2_900_000m, vm.GrossProfit);
        Assert.Equal(58.0, vm.GrossMarginPercent);
    }

    // ─── TC-T3-005: FinancialReport mixed COGS (StockAdjustment + 42% fallback) ─
    [Fact]
    public async Task TC_T3_005_FinancialReport_MixedOrderItemsCombinesRecordedCostAnd42PercentFallback()
    {
        using var db = CreateDbContext();
        db.StockAdjustments.Add(new StockAdjustment
        {
            VariantId = 301,
            OldQuantity = 0,
            AdjustedQuantity = 10,
            NewQuantity = 10,
            UnitCost = 1_500_000m,
            Reason = "Nhập kho"
        });
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-MIX-COGS",
            CustomerName = "Khách Mix",
            CustomerPhone = "0907",
            CustomerEmail = "mix@test.vn",
            ShippingAddress = "Đà Nẵng",
            SubTotal = 8_000_000m,
            TotalAmount = 8_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, VariantId = 301, ProductName = "Chai Có Giá Vốn", UnitPrice = 4_000_000m, Quantity = 1 },
                new() { ProductId = 2, VariantId = 302, ProductName = "Chai 42%", UnitPrice = 4_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // 1,500,000 + (4,000,000 * 0.42 = 1,680,000) = 3,180,000
        Assert.Equal(3_180_000m, vm.TotalCogs);
        Assert.Equal(4_820_000m, vm.GrossProfit);
    }

    // ─── TC-T3-006: FinancialReport GrossMarginPercent & AverageOrderValue ─────
    [Fact]
    public async Task TC_T3_006_FinancialReport_ZeroOrdersProducesSafeZeroMarginAndZeroAov()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(0, vm.TotalOrdersCount);
        Assert.Equal(0m, vm.NetRevenue);
        Assert.Equal(0m, vm.TotalCogs);
        Assert.Equal(0m, vm.GrossProfit);
        Assert.Equal(0.0, vm.GrossMarginPercent);
        Assert.Equal(0m, vm.AverageOrderValue);
    }

    // ─── TC-T3-007: FinancialReport preset filters ────────────────────────────
    [Theory]
    [InlineData("today")]
    [InlineData("7days")]
    [InlineData("thismonth")]
    [InlineData("thisyear")]
    [InlineData("all")]
    [InlineData("30days")]
    public async Task TC_T3_007_FinancialReport_PresetsFilterOrdersCorrectly(string preset)
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = $"LUXE-{preset.ToUpperInvariant()}",
            CustomerName = "Khách Preset",
            CustomerPhone = "0905555555",
            CustomerEmail = "preset@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 3_000_000m,
            TotalAmount = 3_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, preset));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(preset, vm.Preset);
        Assert.Equal(1, vm.TotalOrdersCount);
        Assert.Equal(3_000_000m, vm.NetRevenue);
    }

    // ─── TC-T3-008: Inventory Valuation by Brand ──────────────────────────────
    [Fact]
    public async Task TC_T3_008_FinancialReport_InventoryValuationByBrandComputesUnitsCostRetailAndProfit()
    {
        using var db = CreateDbContext();
        var brand1 = new Brand { Id = 1, Name = "Creed", Slug = "creed" };
        var brand2 = new Brand { Id = 2, Name = "Le Labo", Slug = "le-labo" };
        var cat = new Category { Id = 1, Name = "Niche", Slug = "niche" };
        var prod1 = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "CRD-1", BrandId = 1, Brand = brand1, CategoryId = 1, Category = cat, BasePrice = 8_000_000m };
        var prod2 = new Product { Id = 2, Name = "Santal 33", Slug = "santal-33", Sku = "LLB-1", BrandId = 2, Brand = brand2, CategoryId = 1, Category = cat, BasePrice = 6_000_000m };

        var v1 = new ProductVariant { Id = 10, ProductId = 1, Product = prod1, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "CRD-100", OriginalPrice = 8_000_000m, SalePrice = 8_000_000m, StockQuantity = 5 };
        var v2 = new ProductVariant { Id = 20, ProductId = 2, Product = prod2, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "LLB-100", OriginalPrice = 6_000_000m, SalePrice = 6_000_000m, StockQuantity = 10 };

        db.Brands.AddRange(brand1, brand2);
        db.Categories.Add(cat);
        db.Products.AddRange(prod1, prod2);
        db.ProductVariants.AddRange(v1, v2);
        db.StockAdjustments.Add(new StockAdjustment { VariantId = 10, UnitCost = 3_000_000m, Reason = "Nhập Creed" });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(15, vm.TotalStockQuantity);
        // Creed: 5 * 3,000,000 = 15,000,000 cost; 5 * 8,000,000 = 40,000,000 retail -> 25,000,000 profit
        // Le Labo: 10 * (6,000,000 * 0.42 = 2,520,000) = 25,200,000 cost; 10 * 6,000,000 = 60,000,000 retail -> 34,800,000 profit
        Assert.Equal(40_200_000m, vm.TotalInventoryCostValue);
        Assert.Equal(100_000_000m, vm.TotalInventoryRetailValue);
        Assert.Equal(59_800_000m, vm.UnrealizedGrossProfit);

        var creedVal = Assert.Single(vm.BrandValuations, b => b.BrandName == "Creed");
        Assert.Equal(5, creedVal.TotalUnits);
        Assert.Equal(15_000_000m, creedVal.TotalCostValue);
        Assert.Equal(40_000_000m, creedVal.TotalRetailValue);
        Assert.Equal(25_000_000m, creedVal.PotentialProfit);
    }

    // ─── TC-T3-009: Inventory Valuation price fallback priority ───────────────
    [Fact]
    public async Task TC_T3_009_FinancialReport_InventoryValuationFallsBackFromSalePriceToOriginalPriceToBasePrice()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Tom Ford", Slug = "tom-ford" };
        var cat = new Category { Id = 1, Name = "Luxury", Slug = "luxury" };
        var prod = new Product { Id = 1, Name = "Oud Wood", Slug = "oud-wood", Sku = "TF-OW", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 7_000_000m };
        var v1 = new ProductVariant { Id = 1, ProductId = 1, Product = prod, VolumeMl = 50, VolumeLabel = "50ml", VariantSku = "TF-50", SalePrice = 0m, OriginalPrice = 5_000_000m, StockQuantity = 2 };
        var v2 = new ProductVariant { Id = 2, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "TF-100", SalePrice = 0m, OriginalPrice = 0m, StockQuantity = 1 };

        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.AddRange(v1, v2);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        // v1 uses OriginalPrice (2 * 5,000,000 = 10,000,000), v2 uses Product.BasePrice (1 * 7,000,000 = 7,000,000)
        Assert.Equal(17_000_000m, vm.TotalInventoryRetailValue);
    }

    // ─── TC-T3-010: FinancialReport 14-day chart & fragrance family chart ─────
    [Fact]
    public async Task TC_T3_010_FinancialReport_Populates14DayRevenueProfitChartAndFragranceFamilyBreakdown()
    {
        using var db = CreateDbContext();
        var fam = new FragranceFamily { Id = 1, Name = "Woody Oriental", Slug = "woody-oriental" };
        var brand = new Brand { Id = 1, Name = "MFK", Slug = "mfk" };
        var cat = new Category { Id = 1, Name = "Extrait", Slug = "extrait" };
        var prod = new Product { Id = 1, Name = "Grand Soir", Slug = "grand-soir", Sku = "MFK-GS", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, FragranceFamilyId = 1, FragranceFamily = fam, BasePrice = 6_000_000m };

        db.FragranceFamilies.Add(fam);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CHART-01",
            CustomerName = "Khách Biểu Đồ",
            CustomerPhone = "0901234567",
            CustomerEmail = "chart@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 6_000_000m,
            TotalAmount = 6_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, Product = prod, ProductName = "Grand Soir", UnitPrice = 6_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var result = Assert.IsType<ViewResult>(await controller.FinancialReport(null, null, "30days"));
        var vm = Assert.IsType<FinancialReportViewModel>(result.Model);

        Assert.Equal(14, vm.DailyChartLabels.Count);
        Assert.Equal(14, vm.DailyRevenueData.Count);
        Assert.Equal(14, vm.DailyProfitData.Count);
        Assert.Contains("Woody Oriental", vm.FamilyChartLabels);
        Assert.Contains(6_000_000m, vm.FamilyChartData);
    }

    // ─── TC-T3-011: ExportFinancialOrdersCsv UTF-8 BOM verification ───────────
    [Fact]
    public async Task TC_T3_011_ExportFinancialOrdersCsv_IncludesUtf8BomAndCorrectContentType()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CSV-001",
            CustomerName = "Lê Hoàng Bảo",
            CustomerPhone = "0912345678",
            CustomerEmail = "bao@test.vn",
            ShippingAddress = "Q1, HCM",
            SubTotal = 4_000_000m,
            DiscountAmount = 200_000m,
            GiftWrappingFee = 50_000m,
            ShippingFee = 0m,
            TotalAmount = 3_850_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportFinancialOrdersCsv(null, null));

        Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);
        Assert.StartsWith("PerfumeLuxe_Financial_Orders_", fileResult.FileDownloadName);
        // UTF-8 BOM bytes: 0xEF, 0xBB, 0xBF
        Assert.True(fileResult.FileContents.Length >= 3);
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);
    }

    // ─── TC-T3-012: ExportFinancialOrdersCsv header & row calculation ─────────
    [Fact]
    public async Task TC_T3_012_ExportFinancialOrdersCsv_ContainsExpectedColumnsAndCalculatedRowValues()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CSV-002",
            CustomerName = "Phạm Minh Châu",
            CustomerPhone = "0987654321",
            CustomerEmail = "chau@test.vn",
            ShippingAddress = "Hà Nội",
            SubTotal = 5_000_000m,
            DiscountAmount = 0m,
            GiftWrappingFee = 50_000m,
            ShippingFee = 0m,
            TotalAmount = 5_050_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Rose 31", UnitPrice = 5_000_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportFinancialOrdersCsv(null, null));
        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("Mã Đơn,Ngày Đặt,Khách Hàng,Phương Thức,Tiền Hàng,Giảm Giá,Gói Quà,Phí Ship,Doanh Thu Thuần,Giá Vốn Ước Tính,Lợi Nhuận Gộp,Biên Lãi %", csvText);
        Assert.Contains("\"LUXE-CSV-002\"", csvText);
        Assert.Contains("\"Phạm Minh Châu\"", csvText);
        // COGS = 5,000,000 * 0.42 = 2,100,000; Profit = 5,050,000 - 2,100,000 = 2,950,000
        Assert.Contains("5000000,0,50000,0,5050000,2100000,2950000", csvText);
    }

    // ─── TC-T3-013: ExportInventoryValuationCsv UTF-8 BOM & row accuracy ──────
    [Fact]
    public async Task TC_T3_013_ExportInventoryValuationCsv_IncludesUtf8BomAndAccurateValuationColumns()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Byredo", Slug = "byredo" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Gypsy Water", Slug = "gypsy-water", Sku = "BYR-GW", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 5_500_000m };
        var variant = new ProductVariant { Id = 55, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "BYR-GW-100", OriginalPrice = 5_500_000m, SalePrice = 5_000_000m, StockQuantity = 4 };

        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.Add(variant);
        db.StockAdjustments.Add(new StockAdjustment { VariantId = 55, UnitCost = 2_000_000m, Reason = "Nhập kho" });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportInventoryValuationCsv());

        Assert.Equal(0xEF, fileResult.FileContents[0]);
        Assert.Equal(0xBB, fileResult.FileContents[1]);
        Assert.Equal(0xBF, fileResult.FileContents[2]);

        var csvText = Encoding.UTF8.GetString(fileResult.FileContents);
        Assert.Contains("ID Variant,Mã SKU,Tên Nước Hoa,Thương Hiệu,Dung Tích,Tồn Kho,Đơn Giá Vốn,Đơn Giá Niêm Yết,Tổng Giá Trị Vốn,Tổng Giá Trị Bán Lẻ,Lợi Nhuận Tiềm Năng", csvText);
        // 4 units * 2,000,000 cost = 8,000,000; 4 * 5,000,000 retail = 20,000,000; profit = 12,000,000
        Assert.Contains("55,\"BYR-GW-100\",\"Gypsy Water\",\"Byredo\",\"100ml\",4,2000000,5000000,8000000,20000000,12000000", csvText);
    }

    // ─── TC-T3-014: StoreManagerController.ExportCsv UTF-8 BOM & CSV injection ─
    [Fact]
    public async Task TC_T3_014_StoreManagerExportCsv_IncludesBomAndEscapesFormulaInjectionPayloads()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-SAFE-CSV",
            CustomerName = "=cmd|' /C calc'!A0",
            CustomerPhone = "0901112222",
            CustomerEmail = "csv@test.vn",
            ShippingAddress = "HCM",
            SubTotal = 2_000_000m,
            TotalAmount = 2_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var controller = new StoreManagerController(db);
        AttachHttpContext(controller, "mgr-1", "mgr@perfumeluxe.vn", "StoreManager");

        var fileResult = Assert.IsType<FileContentResult>(await controller.ExportCsv(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));
        Assert.Equal(0xEF, fileResult.FileContents[0]);
        var csv = Encoding.UTF8.GetString(fileResult.FileContents);
        // Formula injection payload starting with '=' must be prefixed with single quote "'"
        Assert.Contains("\"'=cmd|' /C calc'!A0\"", csv);
    }

    // ─── TC-T3-015: StoreManagerController.Index KPI aggregation ──────────────
    [Fact]
    public async Task TC_T3_015_StoreManagerIndex_CalculatesRevenueCancellationRateAndStockAlerts()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Dior", Slug = "dior" };
        var cat = new Category { Id = 1, Name = "Men", Slug = "men" };
        var prod = new Product { Id = 1, Name = "Sauvage", Slug = "sauvage", Sku = "DIOR-SV", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 3_500_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.ProductVariants.AddRange(
            new ProductVariant { Id = 1, ProductId = 1, Product = prod, VolumeMl = 60, VolumeLabel = "60ml", VariantSku = "SV-60", OriginalPrice = 3_000_000m, SalePrice = 3_000_000m, StockQuantity = 3 },
            new ProductVariant { Id = 2, ProductId = 1, Product = prod, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "SV-100", OriginalPrice = 4_000_000m, SalePrice = 4_000_000m, StockQuantity = 0 });

        db.Orders.AddRange(
            new Order
            {
                OrderCode = "SM-1",
                CustomerName = "VIP 1",
                CustomerEmail = "vip1@test.vn",
                CustomerPhone = "0901",
                ShippingAddress = "HCM",
                SubTotal = 4_000_000m,
                DiscountAmount = 200_000m,
                TotalAmount = 3_800_000m,
                VoucherCode = "LUXE200",
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            },
            new Order
            {
                OrderCode = "SM-2",
                CustomerName = "Cancel 1",
                CustomerEmail = "c1@test.vn",
                CustomerPhone = "0902",
                ShippingAddress = "HCM",
                SubTotal = 2_000_000m,
                TotalAmount = 2_000_000m,
                OrderStatus = OrderStatus.Cancelled,
                PaymentStatus = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var controller = new StoreManagerController(db);
        AttachHttpContext(controller, "mgr-1", "mgr@perfumeluxe.vn", "StoreManager");

        var result = Assert.IsType<ViewResult>(await controller.Index(null, null, "today"));
        var vm = Assert.IsType<StoreManagerDashboardViewModel>(result.Model);

        Assert.Equal(3_800_000m, vm.Revenue);
        Assert.Equal(2, vm.OrderCount);
        Assert.Equal(1, vm.PaidOrders);
        Assert.Equal(1, vm.CancelledOrders);
        Assert.Equal(50.0, vm.CancellationRate);
        Assert.Equal(2_000_000m, vm.LostRevenue);
        Assert.Single(vm.LowStock);
        Assert.Single(vm.OutOfStock);
    }

    // ─── TC-T3-016: AdminDashboardController.Index role-based routing ─────────
    [Fact]
    public async Task TC_T3_016_AdminDashboardIndex_RedirectsStoreManagerAndRendersForAdmin()
    {
        using var db = CreateDbContext();

        // StoreManager without Admin role -> redirects to StoreManager/Index
        var storeMgrController = CreateAdminController(db, actorUserId: "mgr-1", roles: "StoreManager");
        var redirect = Assert.IsType<RedirectToActionResult>(await storeMgrController.Index());
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("StoreManager", redirect.ControllerName);

        // Admin role -> renders ViewResult with AdminDashboardViewModel
        var adminController = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");
        var view = Assert.IsType<ViewResult>(await adminController.Index());
        Assert.IsType<AdminDashboardViewModel>(view.Model);
    }

    // ─── TC-T3-017: FlashSaleService.CreateFlashSaleAsync ─────────────────────
    [Fact]
    public async Task TC_T3_017_FlashSaleService_CreateFlashSaleAsync_PersistsCampaignAndItems()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Chanel", Slug = "chanel" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Bleu de Chanel", Slug = "bleu-de-chanel", Sku = "CH-BDC", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 4_200_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Midnight Luxury Sale",
            Description = "Ưu đãi nửa đêm",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddHours(3),
            IsActive = true,
            Items = new List<CreateFlashSaleItemRequest>
            {
                new() { ProductId = 1, FlashSalePrice = 3_360_000m, StockLimit = 15 }
            }
        });

        Assert.True(created.Id > 0);
        Assert.Equal("Midnight Luxury Sale", created.Name);
        Assert.True(created.IsOngoing);
        var item = Assert.Single(created.Items);
        Assert.Equal(3_360_000m, item.FlashSalePrice);
        Assert.Equal(4_200_000m, item.OriginalPrice);
        Assert.Equal(20, item.DiscountPercent);
        Assert.Equal(15, item.RemainingQuantity);
    }

    // ─── TC-T3-018: FlashSaleService.GetAllFlashSalesAsync & GetFlashSaleById ─
    [Fact]
    public async Task TC_T3_018_FlashSaleService_GetAllAndGetById_ReturnOrderedCampaigns()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);

        var fs1 = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Sale 1",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });
        var fs2 = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Sale 2",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = false
        });

        var all = await service.GetAllFlashSalesAsync();
        Assert.Equal(2, all.Count);

        var fetched = await service.GetFlashSaleByIdAsync(fs1.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Sale 1", fetched!.Name);
        Assert.Null(await service.GetFlashSaleByIdAsync(99999));
    }

    // ─── TC-T3-019: FlashSaleService.ToggleStatusAsync ────────────────────────
    [Fact]
    public async Task TC_T3_019_FlashSaleService_ToggleStatusAsync_FlipsActiveStateAndHandlesMissingId()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Toggle Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });

        Assert.True(await service.ToggleStatusAsync(created.Id));
        var afterFirstToggle = await service.GetFlashSaleByIdAsync(created.Id);
        Assert.False(afterFirstToggle!.IsActive);

        Assert.True(await service.ToggleStatusAsync(created.Id));
        var afterSecondToggle = await service.GetFlashSaleByIdAsync(created.Id);
        Assert.True(afterSecondToggle!.IsActive);

        Assert.False(await service.ToggleStatusAsync(88888));
    }

    // ─── TC-T3-020: FlashSaleService.DeleteAsync soft delete ──────────────────
    [Fact]
    public async Task TC_T3_020_FlashSaleService_DeleteAsync_SoftDeletesAndExcludesFromQueries()
    {
        using var db = CreateDbContext();
        var service = new FlashSaleService(db);
        var created = await service.CreateFlashSaleAsync(new CreateFlashSaleRequest
        {
            Name = "Delete Campaign",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });

        Assert.True(await service.DeleteAsync(created.Id));
        Assert.Empty(await service.GetAllFlashSalesAsync());
        Assert.Null(await service.GetActiveFlashSaleAsync());
        Assert.False(await service.DeleteAsync(77777));
    }

    // ─── TC-T3-021: AdminDashboardController.CreateFlashSale validation ───────
    [Fact]
    public async Task TC_T3_021_AdminDashboard_CreateFlashSale_ValidatesDatesAndApplies80PercentFallbackPrice()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Amouage", Slug = "amouage" };
        var cat = new Category { Id = 1, Name = "Parfum", Slug = "parfum" };
        var prod = new Product { Id = 1, Name = "Reflection Man", Slug = "reflection-man", Sku = "AMG-RM", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 7_500_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        // Invalid: empty name
        await controller.CreateFlashSale("", "desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), true, Array.Empty<int>(), Array.Empty<decimal>(), Array.Empty<int>());
        Assert.NotNull(controller.TempData["Error"]);

        // Invalid: endTime <= startTime
        var now = DateTime.UtcNow;
        await controller.CreateFlashSale("Bad Time", "desc", now, now.AddHours(-1), true, Array.Empty<int>(), Array.Empty<decimal>(), Array.Empty<int>());
        Assert.Contains("Thời gian kết thúc", controller.TempData["Error"]?.ToString());

        // Valid with explicit flashSalePrice and 0 stockLimit -> falls back to StockLimit = 10
        await controller.CreateFlashSale("Amouage Flash", "desc", now.AddMinutes(-5), now.AddHours(4), true, new[] { 1 }, new[] { 6_000_000m }, new[] { 0 });
        var saved = await db.FlashSales.Include(f => f.Items).FirstOrDefaultAsync(f => f.Name == "Amouage Flash");
        Assert.NotNull(saved);
        Assert.Equal(6_000_000m, saved!.Items.First().FlashSalePrice);
        Assert.Equal(10, saved.Items.First().StockLimit);
    }

    // ─── TC-T3-022: SystemSettingService.GetSettingsAsync fallback defaults ───
    [Fact]
    public async Task TC_T3_022_SystemSettingService_GetSettingsAsync_ReturnsConfigurationFallbackWhenDbEmpty()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var settings = await service.GetSettingsAsync();

        Assert.Equal("PERFUME LUXE", settings.StoreName);
        Assert.Equal(2_000_000m, settings.FreeShippingThreshold);
        Assert.Equal(35_000m, settings.DefaultShippingFee);
    }

    // ─── TC-T3-023: SystemSettingService.SaveSettingsAsync persistence ────────
    [Fact]
    public async Task TC_T3_023_SystemSettingService_SaveSettingsAsync_CreatesAndUpdatesStoreConfiguration()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var custom = new SystemSettingsDto
        {
            StoreName = "PERFUME LUXE PARIS",
            Hotline = "1800 9999",
            FreeShippingThreshold = 1_500_000m,
            DefaultShippingFee = 40_000m,
            SmtpEnabled = false
        };

        await service.SaveSettingsAsync(custom);
        var loaded1 = await service.GetSettingsAsync();
        Assert.Equal("PERFUME LUXE PARIS", loaded1.StoreName);
        Assert.Equal(1_500_000m, loaded1.FreeShippingThreshold);

        // Update existing setting record
        custom.StoreName = "PERFUME LUXE MILANO";
        await service.SaveSettingsAsync(custom);
        Assert.Equal(1, await db.SystemSettings.CountAsync());
        var loaded2 = await service.GetSettingsAsync();
        Assert.Equal("PERFUME LUXE MILANO", loaded2.StoreName);
    }

    // ─── TC-T3-024: SystemSettingService.TestSmtpConnectionAsync when disabled ─
    [Fact]
    public async Task TC_T3_024_SystemSettingService_TestSmtpConnectionAsync_ReturnsFalseWhenSmtpDisabled()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration(new Dictionary<string, string?> { ["Smtp:Enabled"] = "false" });
        var service = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);

        var (success, message) = await service.TestSmtpConnectionAsync("test@perfumeluxe.vn");
        Assert.False(success);
        Assert.Contains("SMTP", message);
    }

    // ─── TC-T3-025: HeroBanner create & update via AdminDashboardController ───
    [Fact]
    public async Task TC_T3_025_AdminDashboard_SaveBanner_CreatesAndUpdatesHeroBanner()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        // Create banner with default TargetUrl & ButtonText
        await controller.SaveBanner(null, "Bộ Sưu Tập Thu Đông", "Hương gỗ trầm ấm", "https://cdn.test/banner1.jpg", null, null, 1, true);
        var created = await db.HeroBanners.FirstOrDefaultAsync();
        Assert.NotNull(created);
        Assert.Equal("Bộ Sưu Tập Thu Đông", created!.Title);
        Assert.Equal("/bo-suu-tap", created.TargetUrl);
        Assert.Equal("Khám Phá Ngay", created.ButtonText);

        // Update banner
        await controller.SaveBanner(created.Id, "Bộ Sưu Tập Mùa Lễ Hội", "Limited Edition", "https://cdn.test/banner2.jpg", "/flash-sale", "Mua Ngay", 2, true);
        var updated = await db.HeroBanners.FindAsync(created.Id);
        Assert.Equal("Bộ Sưu Tập Mùa Lễ Hội", updated!.Title);
        Assert.Equal("/flash-sale", updated.TargetUrl);
        Assert.Equal("Mua Ngay", updated.ButtonText);
        Assert.Equal(2, updated.DisplayOrder);
    }

    // ─── TC-T3-026: HeroBanner toggle active & delete ─────────────────────────
    [Fact]
    public async Task TC_T3_026_AdminDashboard_ToggleAndDeleteBanner_UpdatesAndRemovesEntity()
    {
        using var db = CreateDbContext();
        var banner = new HeroBanner { Title = "Banner Test", ImageUrl = "https://cdn.test/b.jpg", IsActive = true };
        db.HeroBanners.Add(banner);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.ToggleBannerActive(banner.Id);
        var toggled = await db.HeroBanners.FindAsync(banner.Id);
        Assert.False(toggled!.IsActive);

        await controller.DeleteBanner(banner.Id);
        Assert.Equal(0, await db.HeroBanners.CountAsync());
    }

    // ─── TC-T3-027: AdjustStock positive restock & Product.IsInStock sync ─────
    [Fact]
    public async Task TC_T3_027_AdminDashboard_AdjustStock_PositiveRestockUpdatesStockRecordsUnitCostAndSyncsProductInStock()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Diptyque", Slug = "diptyque" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Fleur de Peau", Slug = "fleur-de-peau", Sku = "DIP-FDP", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 4_800_000m, IsInStock = false };
        var variant = new ProductVariant { Id = 10, ProductId = 1, Product = prod, VolumeMl = 75, VolumeLabel = "75ml", VariantSku = "DIP-FDP-75", OriginalPrice = 4_800_000m, SalePrice = 4_800_000m, StockQuantity = 0 };
        prod.Variants.Add(variant);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.AdjustStock(10, 12, 2_100_000m, "Nhập hàng Pháp", "Lô 2026-10");

        var updatedVariant = await db.ProductVariants.FindAsync(10);
        var updatedProduct = await db.Products.FindAsync(1);
        var adjustment = await db.StockAdjustments.FirstOrDefaultAsync(a => a.VariantId == 10);

        Assert.Equal(12, updatedVariant!.StockQuantity);
        Assert.True(updatedProduct!.IsInStock);
        Assert.NotNull(adjustment);
        Assert.Equal(0, adjustment!.OldQuantity);
        Assert.Equal(12, adjustment.AdjustedQuantity);
        Assert.Equal(12, adjustment.NewQuantity);
        Assert.Equal(2_100_000m, adjustment.UnitCost);
        Assert.Equal("Nhập hàng Pháp", adjustment.Reason);
    }

    // ─── TC-T3-028: AdjustStock negative deduction clamps to 0 & syncs out-of-stock ─
    [Fact]
    public async Task TC_T3_028_AdminDashboard_AdjustStock_NegativeDepletionClampsZeroAndMarksProductOutOfStock()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Kilian", Slug = "kilian" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Angels Share", Slug = "angels-share", Sku = "KIL-AS", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 6_200_000m, IsInStock = true };
        var variant = new ProductVariant { Id = 20, ProductId = 1, Product = prod, VolumeMl = 50, VolumeLabel = "50ml", VariantSku = "KIL-AS-50", OriginalPrice = 6_200_000m, SalePrice = 6_200_000m, StockQuantity = 4 };
        prod.Variants.Add(variant);
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        await controller.AdjustStock(20, -10, null, "Kiểm kê hao hụt", "Vỡ khi vận chuyển");

        var updatedVariant = await db.ProductVariants.FindAsync(20);
        var updatedProduct = await db.Products.FindAsync(1);

        Assert.Equal(0, updatedVariant!.StockQuantity);
        Assert.False(updatedProduct!.IsInStock);
    }

    // ─── TC-T3-029: AdjustStock validation for 0 quantity & missing variant ───
    [Fact]
    public async Task TC_T3_029_AdminDashboard_AdjustStock_RejectsZeroQuantityAndMissingVariant()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db);

        await controller.AdjustStock(1, 0, null, "Test", null);
        Assert.Contains("khác 0", controller.TempData["Error"]?.ToString());

        await controller.AdjustStock(9999, 5, 1_000_000m, "Test", null);
        Assert.Contains("Không tìm thấy", controller.TempData["Error"]?.ToString());
        Assert.Equal(0, await db.StockAdjustments.CountAsync());
    }

    // ─── TC-T3-030: Inventory action filters (all, low, out) & search ─────────
    [Fact]
    public async Task TC_T3_030_AdminDashboard_Inventory_FiltersByLowOutAndSearchQuery()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Parfums de Marly", Slug = "pdm" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var pLow = new Product { Id = 1, Name = "Layton", Slug = "layton", Sku = "PDM-L", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, IsInStock = true };
        pLow.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 125, VolumeLabel = "125ml", VariantSku = "PDM-L-125", StockQuantity = 3 });

        var pOut = new Product { Id = 2, Name = "Delina", Slug = "delina", Sku = "PDM-D", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, IsInStock = false };
        pOut.Variants.Add(new ProductVariant { Id = 2, ProductId = 2, VolumeMl = 75, VolumeLabel = "75ml", VariantSku = "PDM-D-75", StockQuantity = 0 });

        var pFull = new Product { Id = 3, Name = "Herod", Slug = "herod", Sku = "PDM-H", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, IsInStock = true };
        pFull.Variants.Add(new ProductVariant { Id = 3, ProductId = 3, VolumeMl = 125, VolumeLabel = "125ml", VariantSku = "PDM-H-125", StockQuantity = 25 });

        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.AddRange(pLow, pOut, pFull);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        var lowView = Assert.IsType<ViewResult>(await controller.Inventory("low", null));
        var lowVm = Assert.IsType<InventoryViewModel>(lowView.Model);
        Assert.Single(lowVm.Items);
        Assert.Equal("Layton", lowVm.Items[0].ProductName);

        var outView = Assert.IsType<ViewResult>(await controller.Inventory("out", null));
        var outVm = Assert.IsType<InventoryViewModel>(outView.Model);
        Assert.Single(outVm.Items);
        Assert.Equal("Delina", outVm.Items[0].ProductName);

        var searchView = Assert.IsType<ViewResult>(await controller.Inventory("all", "Herod"));
        var searchVm = Assert.IsType<InventoryViewModel>(searchView.Model);
        Assert.Single(searchVm.Items);
        Assert.Equal("Herod", searchVm.Items[0].ProductName);
    }

    // ─── TC-T3-031: PrivateTastingBooking status workflow transitions ─────────
    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task TC_T3_031_AdminDashboard_UpdateTastingStatus_TransitionsWorkflowCorrectly(string targetStatus)
    {
        using var db = CreateDbContext();
        var booking = new PrivateTastingBooking
        {
            CustomerName = "Quý Cô Thanh Lịch",
            PhoneNumber = "0909888777",
            BoutiqueLocation = "Flagship Đồng Khởi",
            BookingDate = DateTime.UtcNow.Date.AddDays(2),
            Status = "Pending"
        };
        db.PrivateTastingBookings.Add(booking);
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var redirect = Assert.IsType<RedirectToActionResult>(await controller.UpdateTastingStatus(booking.Id, targetStatus));
        Assert.Equal("Tastings", redirect.ActionName);

        var updated = await db.PrivateTastingBookings.FindAsync(booking.Id);
        Assert.Equal(targetStatus, updated!.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    // ─── TC-T3-032: AdminDashboardController.Tastings status filter ───────────
    [Fact]
    public async Task TC_T3_032_AdminDashboard_Tastings_FiltersBookingsByStatus()
    {
        using var db = CreateDbContext();
        db.PrivateTastingBookings.AddRange(
            new PrivateTastingBooking { CustomerName = "A", PhoneNumber = "0901", Status = "Pending", BookingDate = DateTime.UtcNow.AddDays(1) },
            new PrivateTastingBooking { CustomerName = "B", PhoneNumber = "0902", Status = "Confirmed", BookingDate = DateTime.UtcNow.AddDays(2) },
            new PrivateTastingBooking { CustomerName = "C", PhoneNumber = "0903", Status = "Completed", BookingDate = DateTime.UtcNow.AddDays(3) });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);
        var confirmedResult = Assert.IsType<ViewResult>(await controller.Tastings("Confirmed"));
        var confirmedList = Assert.IsAssignableFrom<List<PrivateTastingBooking>>(confirmedResult.Model);
        Assert.Single(confirmedList);
        Assert.Equal("B", confirmedList[0].CustomerName);

        var allResult = Assert.IsType<ViewResult>(await controller.Tastings("all"));
        var allList = Assert.IsAssignableFrom<List<PrivateTastingBooking>>(allResult.Model);
        Assert.Equal(3, allList.Count);
    }

    // ─── TC-T3-033: BackInStockAlert subscription & deduplication ─────────────
    [Fact]
    public async Task TC_T3_033_ExperienceController_SubscribeBackInStock_PersistsAndDeduplicatesUnnotifiedAlerts()
    {
        using var db = CreateDbContext();
        var controller = new ExperienceController(db);

        // Invalid email -> 400 BadRequest
        var badRes = await controller.SubscribeBackInStock(new BackInStockSubscribeRequest { ProductId = 1, Email = "invalid-email" });
        Assert.IsType<BadRequestObjectResult>(badRes);

        // First valid subscription -> 200 OK & saved
        var ok1 = await controller.SubscribeBackInStock(new BackInStockSubscribeRequest { ProductId = 10, VariantId = 100, Email = "VIP@PerfumeLuxe.vn", Phone = "0909111222" });
        Assert.IsType<OkObjectResult>(ok1);
        Assert.Equal(1, await db.BackInStockAlerts.CountAsync());

        // Duplicate subscription while IsNotified == false -> does not create duplicate row
        var ok2 = await controller.SubscribeBackInStock(new BackInStockSubscribeRequest { ProductId = 10, VariantId = 100, Email = "vip@perfumeluxe.vn" });
        Assert.IsType<OkObjectResult>(ok2);
        Assert.Equal(1, await db.BackInStockAlerts.CountAsync());
    }

    // ─── TC-T3-034: CatalogManagerController referential integrity guardrails ─
    [Fact]
    public async Task TC_T3_034_CatalogManager_PreventsDeletingBrandFamilyOrNoteLinkedToProducts()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Xerjoff", Slug = "xerjoff" };
        var fam = new FragranceFamily { Id = 1, Name = "Citrus Gourmand", Slug = "citrus-gourmand" };
        var note = new FragranceNote { Id = 1, Name = "Bergamot", Slug = "bergamot" };
        var cat = new Category { Id = 1, Name = "Niche", Slug = "niche" };
        var prod = new Product { Id = 1, Name = "Naxos", Slug = "naxos", Sku = "XJ-NX", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, FragranceFamilyId = 1, FragranceFamily = fam };
        prod.FragranceNotes.Add(new ProductFragranceNote { ProductId = 1, FragranceNoteId = 1, FragranceNote = note, NoteLevel = NoteLevel.Top });

        db.Brands.Add(brand);
        db.FragranceFamilies.Add(fam);
        db.FragranceNotes.Add(note);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var controller = new CatalogManagerController(db);
        AttachHttpContext(controller, "admin-1", "admin@perfumeluxe.vn", "Admin");

        await controller.DeleteBrand(1);
        Assert.False((await db.Brands.FindAsync(1))!.IsDeleted);

        await controller.DeleteFragranceFamily(1);
        Assert.False((await db.FragranceFamilies.FindAsync(1))!.IsDeleted);

        await controller.DeleteFragranceNote(1);
        Assert.False((await db.FragranceNotes.FindAsync(1))!.IsDeleted);
    }

    // ─── TC-T3-035: 5-Tier RBAC roles seeding & assignment in Identity ────────
    [Fact]
    public async Task TC_T3_035_Rbac5Tiers_AllFiveRolesCanBeSeededAndAssignedToUsers()
    {
        using var db = CreateDbContext();
        var roleManager = CreateRoleManager(db);
        var userManager = CreateUserManager(db);

        var expectedRoles = new[] { "Admin", "StoreManager", "WarehouseStaff", "Concierge", "Customer" };
        foreach (var roleName in expectedRoles)
        {
            var res = await roleManager.CreateAsync(new AppRole { Name = roleName, Description = $"RBAC {roleName}" });
            Assert.True(res.Succeeded);
        }

        Assert.Equal(5, await db.Roles.CountAsync());

        var staff = new AppUser { UserName = "warehouse@perfumeluxe.vn", Email = "warehouse@perfumeluxe.vn", FullName = "Warehouse Lead" };
        await userManager.CreateAsync(staff, "Pass123!");
        await userManager.AddToRoleAsync(staff, "WarehouseStaff");

        Assert.True(await userManager.IsInRoleAsync(staff, "WarehouseStaff"));
    }

    // ─── TC-T3-036: AdminDashboardController.ChangeUserRole across 5 tiers ────
    [Theory]
    [InlineData("Admin")]
    [InlineData("StoreManager")]
    [InlineData("WarehouseStaff")]
    [InlineData("Concierge")]
    [InlineData("Customer")]
    public async Task TC_T3_036_AdminDashboard_ChangeUserRole_AllowsAllFiveValidRbacRoles(string targetRole)
    {
        using var db = CreateDbContext();
        var roleManager = CreateRoleManager(db);
        var userManager = CreateUserManager(db);

        foreach (var r in new[] { "Admin", "StoreManager", "WarehouseStaff", "Concierge", "Customer" })
        {
            await roleManager.CreateAsync(new AppRole { Name = r });
        }

        var admin1 = new AppUser { Id = "admin-1", UserName = "admin1@test.vn", Email = "admin1@test.vn", FullName = "Admin One" };
        var admin2 = new AppUser { Id = "admin-2", UserName = "admin2@test.vn", Email = "admin2@test.vn", FullName = "Admin Two" };
        var targetUser = new AppUser { Id = "user-target", UserName = "target@test.vn", Email = "target@test.vn", FullName = "Target User" };

        await userManager.CreateAsync(admin1, "Pass123!");
        await userManager.CreateAsync(admin2, "Pass123!");
        await userManager.CreateAsync(targetUser, "Pass123!");
        await userManager.AddToRoleAsync(admin1, "Admin");
        await userManager.AddToRoleAsync(admin2, "Admin");
        await userManager.AddToRoleAsync(targetUser, "Customer");

        var controller = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");
        await controller.ChangeUserRole("user-target", targetRole);

        var updatedRoles = await userManager.GetRolesAsync(targetUser);
        Assert.Single(updatedRoles);
        Assert.Equal(targetRole, updatedRoles[0]);
    }

    // ─── TC-T3-037: ChangeUserRole rejects unknown role ───────────────────────
    [Fact]
    public async Task TC_T3_037_AdminDashboard_ChangeUserRole_RejectsInvalidRoleName()
    {
        using var db = CreateDbContext();
        var controller = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");

        var result = Assert.IsType<RedirectToActionResult>(await controller.ChangeUserRole("any-user", "SuperRootHacker"));
        Assert.Equal("Users", result.ActionName);
        Assert.Contains("không hợp lệ", controller.TempData["Error"]?.ToString());
    }

    // ─── TC-T3-038: ChangeUserRole prevents self-demotion & last Admin demotion ─
    [Fact]
    public async Task TC_T3_038_AdminDashboard_ChangeUserRole_PreventsSelfDemotionAndLastAdminDemotion()
    {
        using var db = CreateDbContext();
        var roleManager = CreateRoleManager(db);
        var userManager = CreateUserManager(db);
        await roleManager.CreateAsync(new AppRole { Name = "Admin" });
        await roleManager.CreateAsync(new AppRole { Name = "Customer" });

        var soleAdmin = new AppUser { Id = "admin-sole", UserName = "sole@test.vn", Email = "sole@test.vn", FullName = "Sole Admin" };
        await userManager.CreateAsync(soleAdmin, "Pass123!");
        await userManager.AddToRoleAsync(soleAdmin, "Admin");

        // 1. Self-demotion attempt by logged-in admin-sole
        var controllerSelf = CreateAdminController(db, actorUserId: "admin-sole", roles: "Admin");
        await controllerSelf.ChangeUserRole("admin-sole", "Customer");
        Assert.Contains("tự hạ quyền", controllerSelf.TempData["Error"]?.ToString());

        // 2. Another actor trying to demote the only remaining Admin
        var controllerOther = CreateAdminController(db, actorUserId: "other-actor", roles: "Admin");
        await controllerOther.ChangeUserRole("admin-sole", "Customer");
        Assert.Contains("ít nhất một tài khoản Admin", controllerOther.TempData["Error"]?.ToString());
        Assert.True(await userManager.IsInRoleAsync(soleAdmin, "Admin"));
    }

    // ─── TC-T3-039: DeleteUser prevents self-deletion & last Admin deletion ───
    [Fact]
    public async Task TC_T3_039_AdminDashboard_DeleteUser_PreventsDeletingSelfOrLastAdmin()
    {
        using var db = CreateDbContext();
        var roleManager = CreateRoleManager(db);
        var userManager = CreateUserManager(db);
        await roleManager.CreateAsync(new AppRole { Name = "Admin" });

        var admin = new AppUser { Id = "admin-1", UserName = "admin@test.vn", Email = "admin@test.vn", FullName = "Main Admin" };
        await userManager.CreateAsync(admin, "Pass123!");
        await userManager.AddToRoleAsync(admin, "Admin");

        var controller = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");
        await controller.DeleteUser("admin-1");

        Assert.Contains("Không thể tự xóa", controller.TempData["Error"]?.ToString());
        Assert.NotNull(await userManager.FindByIdAsync("admin-1"));
    }

    // ─── TC-T3-040: ToggleLockUser locks/unlocks customer & prevents self-lock ─
    [Fact]
    public async Task TC_T3_040_AdminDashboard_ToggleLockUser_LocksAndUnlocksUserAndPreventsSelfLock()
    {
        using var db = CreateDbContext();
        var userManager = CreateUserManager(db);
        var admin = new AppUser { Id = "admin-1", UserName = "admin@test.vn", Email = "admin@test.vn", FullName = "Admin" };
        var customer = new AppUser { Id = "cust-1", UserName = "cust@test.vn", Email = "cust@test.vn", FullName = "Customer", LockoutEnabled = true };
        await userManager.CreateAsync(admin, "Pass123!");
        await userManager.CreateAsync(customer, "Pass123!");

        var controller = CreateAdminController(db, actorUserId: "admin-1", roles: "Admin");

        // Self-lock prevented
        await controller.ToggleLockUser("admin-1");
        Assert.Contains("tự khóa", controller.TempData["Error"]?.ToString());

        // Lock customer
        await controller.ToggleLockUser("cust-1");
        Assert.True(await userManager.IsLockedOutAsync(customer));

        // Unlock customer
        await controller.ToggleLockUser("cust-1");
        Assert.False(await userManager.IsLockedOutAsync(customer));
    }

    // ─── TC-T3-041: RBAC Authorize attribute verification on Admin controllers ─
    [Fact]
    public void TC_T3_041_RbacAuthorizationAttributes_EnforcedOnAdminAndManagerControllers()
    {
        var adminCtrlAuth = typeof(AdminDashboardController).GetCustomAttribute<AuthorizeAttribute>();
        var storeCtrlAuth = typeof(StoreManagerController).GetCustomAttribute<AuthorizeAttribute>();
        var catalogCtrlAuth = typeof(CatalogManagerController).GetCustomAttribute<AuthorizeAttribute>();
        var dbConsoleCtrlAuth = typeof(DatabaseConsoleController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(adminCtrlAuth);
        Assert.Equal("Admin,StoreManager", adminCtrlAuth!.Roles);

        Assert.NotNull(storeCtrlAuth);
        Assert.Equal("Admin,StoreManager", storeCtrlAuth!.Roles);

        Assert.NotNull(catalogCtrlAuth);
        Assert.Equal("Admin,StoreManager", catalogCtrlAuth!.Roles);

        Assert.NotNull(dbConsoleCtrlAuth);
        Assert.Equal("Admin", dbConsoleCtrlAuth!.Roles);

        // Sensitive actions on AdminDashboardController must be [Authorize(Roles = "Admin")]
        var auditMethodAuth = typeof(AdminDashboardController).GetMethod(nameof(AdminDashboardController.AuditLogs))?.GetCustomAttribute<AuthorizeAttribute>();
        var usersMethodAuth = typeof(AdminDashboardController).GetMethod(nameof(AdminDashboardController.Users))?.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal("Admin", auditMethodAuth?.Roles);
        Assert.Equal("Admin", usersMethodAuth?.Roles);
    }

    // ─── TC-T3-042: AuditLogs filtering by action, succeeded, and actor ───────
    [Fact]
    public async Task TC_T3_042_AdminDashboard_AuditLogs_FiltersByActionSucceededAndActor()
    {
        using var db = CreateDbContext();
        db.DatabaseAuditLogs.AddRange(
            new DatabaseAuditLog { ActorUserId = "admin-1", Action = "SQL_QUERY", Target = "Products", Succeeded = true, ElapsedMilliseconds = 12 },
            new DatabaseAuditLog { ActorUserId = "admin-1", Action = "ROW_DELETE_FAILED", Target = "Orders", Succeeded = false, ElapsedMilliseconds = 5 },
            new DatabaseAuditLog { ActorUserId = "admin-2", Action = "DATABASE_BACKUP", Target = "PerfumeDb.bak", Succeeded = true, ElapsedMilliseconds = 450 });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        var res1 = Assert.IsType<ViewResult>(await controller.AuditLogs("SQL_QUERY", true, "admin-1", 1));
        var vm1 = Assert.IsType<AuditLogsViewModel>(res1.Model);
        Assert.Single(vm1.Logs);
        Assert.Equal("SQL_QUERY", vm1.Logs[0].Action);

        var resFailed = Assert.IsType<ViewResult>(await controller.AuditLogs(null, false, null, 1));
        var vmFailed = Assert.IsType<AuditLogsViewModel>(resFailed.Model);
        Assert.Single(vmFailed.Logs);
        Assert.Equal("ROW_DELETE_FAILED", vmFailed.Logs[0].Action);
    }

    // ─── TC-T3-043: AuditLogs pagination (20 per page & clamping) ─────────────
    [Fact]
    public async Task TC_T3_043_AdminDashboard_AuditLogs_Paginates20PerPageAndClampsOutOfRangePage()
    {
        using var db = CreateDbContext();
        for (int i = 1; i <= 25; i++)
        {
            db.DatabaseAuditLogs.Add(new DatabaseAuditLog
            {
                ActorUserId = "admin-1",
                Action = $"ACTION_{i:D2}",
                Target = "PerfumeDb",
                Succeeded = true,
                ElapsedMilliseconds = i
            });
        }
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        var page1Result = Assert.IsType<ViewResult>(await controller.AuditLogs(null, null, null, 1));
        var vmPage1 = Assert.IsType<AuditLogsViewModel>(page1Result.Model);
        Assert.Equal(25, vmPage1.TotalCount);
        Assert.Equal(2, vmPage1.TotalPages);
        Assert.Equal(20, vmPage1.Logs.Count);

        // Requesting page 99 clamps to page 2 (remaining 5 items)
        var page99Result = Assert.IsType<ViewResult>(await controller.AuditLogs(null, null, null, 99));
        var vmPage2 = Assert.IsType<AuditLogsViewModel>(page99Result.Model);
        Assert.Equal(2, vmPage2.CurrentPage);
        Assert.Equal(5, vmPage2.Logs.Count);
    }

    // ─── TC-T3-044: DatabaseConsoleService rejects non-PerfumeDb catalog ──────
    [Theory]
    [InlineData("Server=localhost;Database=master;Trusted_Connection=True;")]
    [InlineData("Server=localhost;Initial Catalog=OtherErpDb;Trusted_Connection=True;")]
    [InlineData("Server=localhost;Trusted_Connection=True;")]
    public void TC_T3_044_DatabaseConsoleService_RejectsNonPerfumeDbInitialCatalog(string badConnectionString)
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = badConnectionString
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new DatabaseConsoleService(config));
        Assert.Contains("PerfumeDb", ex.Message);
    }

    // ─── TC-T3-045: DatabaseConsoleService accepts PerfumeDb catalog ──────────
    [Fact]
    public void TC_T3_045_DatabaseConsoleService_AcceptsPerfumeDbInitialCatalogCaseInsensitive()
    {
        var config = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Initial Catalog=perfumedb;Trusted_Connection=True;"
        });

        var service = new DatabaseConsoleService(config);
        Assert.NotNull(service);
    }

    // ─── TC-T3-046: DatabaseConsoleService blocks cross-DB & dangerous SQL ────
    [Theory]
    [InlineData("USE master; SELECT * FROM sys.tables;")]
    [InlineData("BACKUP DATABASE PerfumeDb TO DISK = 'C:\\temp\\db.bak'")]
    [InlineData("RESTORE DATABASE PerfumeDb FROM DISK = 'C:\\temp\\db.bak'")]
    [InlineData("EXEC sp_configure 'show advanced options', 1")]
    [InlineData("SELECT * FROM OPENROWSET('SQLNCLI', 'Server=evil;', 'SELECT 1')")]
    [InlineData("SHUTDOWN WITH NOWAIT")]
    [InlineData("CREATE LOGIN hacker WITH PASSWORD = '123'")]
    [InlineData("SELECT * FROM master.dbo.sysdatabases")]
    public async Task TC_T3_046_DatabaseConsoleService_ExecuteRawQueryAsync_BlocksForbiddenDatabaseScopeCommands(string forbiddenSql)
    {
        var service = new DatabaseConsoleService(CreateConfiguration());
        var result = await service.ExecuteRawQueryAsync(forbiddenSql);

        Assert.False(result.Success);
        Assert.Contains("PerfumeDb", result.ErrorMessage);
    }

    // ─── TC-T3-047: DatabaseConsoleService blocks forbidden commands in scripts ─
    [Fact]
    public async Task TC_T3_047_DatabaseConsoleService_ExecuteScriptFileAsync_BlocksCrossDatabaseScript()
    {
        var service = new DatabaseConsoleService(CreateConfiguration());
        var script = "SELECT 1;\nGO\nUSE msdb;\nGO";

        var result = await service.ExecuteScriptFileAsync(script);
        Assert.False(result.Success);
        Assert.Contains("PerfumeDb", result.ErrorMessage);
    }

    // ─── TC-T3-048: DatabaseConsoleController requires confirmation for DML/DDL ─
    [Theory]
    [InlineData("DELETE FROM Products WHERE Id = 1")]
    [InlineData("UPDATE Products SET BasePrice = 0")]
    [InlineData("DROP TABLE Products")]
    [InlineData("TRUNCATE TABLE Orders")]
    [InlineData("SELECT * FROM Products; DELETE FROM Products;")]
    public async Task TC_T3_048_DatabaseConsoleController_ExecuteQuery_RequiresConfirmationForDestructiveSql(string destructiveSql)
    {
        using var db = CreateDbContext();
        var fakeConsole = new FakeDatabaseConsoleService();
        var controller = new DatabaseConsoleController(fakeConsole, new FakeWebHostEnvironment(), db, NullLogger<DatabaseConsoleController>.Instance);
        AttachHttpContext(controller, "admin-1", "admin@perfumeluxe.vn", "Admin");

        var jsonResult = Assert.IsType<JsonResult>(await controller.ExecuteQuery(new QueryModel { Sql = destructiveSql, Confirmed = false }));
        var json = JsonSerializer.Serialize(jsonResult.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("confirmationRequired").GetBoolean());
    }

    // ─── TC-T3-049: DatabaseConsoleController SaveRow & DeleteRow guardrails ──
    [Fact]
    public async Task TC_T3_049_DatabaseConsoleController_SaveRowAndDeleteRow_RejectUnconfirmedRequests()
    {
        using var db = CreateDbContext();
        var fakeConsole = new FakeDatabaseConsoleService();
        var controller = new DatabaseConsoleController(fakeConsole, new FakeWebHostEnvironment(), db, NullLogger<DatabaseConsoleController>.Instance);
        AttachHttpContext(controller, "admin-1", "admin@perfumeluxe.vn", "Admin");

        var saveRes = await controller.SaveRow("Products", "Id", "1", confirmed: false, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));
        Assert.IsType<BadRequestObjectResult>(saveRes);

        var deleteRes = await controller.DeleteRow("Products", "Id", "1", confirmed: false);
        Assert.IsType<BadRequestObjectResult>(deleteRes);
    }

    // ─── TC-T3-050: DatabaseConsoleController writes DatabaseAuditLog ─────────
    [Fact]
    public async Task TC_T3_050_DatabaseConsoleController_ExecuteQuery_WritesAuditLogWithCommandHash()
    {
        using var db = CreateDbContext();
        var fakeConsole = new FakeDatabaseConsoleService();
        var controller = new DatabaseConsoleController(fakeConsole, new FakeWebHostEnvironment(), db, NullLogger<DatabaseConsoleController>.Instance);
        AttachHttpContext(controller, "admin-audit", "admin@perfumeluxe.vn", "Admin");

        var result = Assert.IsType<JsonResult>(await controller.ExecuteQuery(new QueryModel
        {
            Sql = "SELECT Id, Name FROM Products",
            Confirmed = false
        }));

        var log = await db.DatabaseAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("admin-audit", log!.ActorUserId);
        Assert.Equal("SQL_QUERY", log.Action);
        Assert.Equal("PerfumeDb", log.Target);
        Assert.True(log.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(log.CommandHash));
        Assert.Equal(64, log.CommandHash!.Length); // SHA-256 hex length
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PHẦN 6: RESTful API v1 & SWAGGER / OPENAPI 3.0.1 (TC-T3-051..100)
    // ═══════════════════════════════════════════════════════════════════════════

    // ─── TC-T3-051: GET /api/v1/flashsales/active when DB empty -> 404 ────────
    [Fact]
    public async Task TC_T3_051_ApiV1_GetActiveFlashSale_Returns404WhenNoFlashSaleExists()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var response = await controller.GetActiveFlashSale();
        Assert.IsType<NotFoundObjectResult>(response);
    }

    // ─── TC-T3-052: GET /api/v1/flashsales/active when IsActive=false -> 404 ──
    [Fact]
    public async Task TC_T3_052_ApiV1_GetActiveFlashSale_Returns404WhenCampaignIsInactive()
    {
        using var db = CreateDbContext();
        db.FlashSales.Add(new FlashSale
        {
            Name = "Inactive Sale",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = false
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var response = await controller.GetActiveFlashSale();
        Assert.IsType<NotFoundObjectResult>(response);
    }

    // ─── TC-T3-053: GET /api/v1/flashsales/active when expired -> 404 ─────────
    [Fact]
    public async Task TC_T3_053_ApiV1_GetActiveFlashSale_Returns404WhenCampaignHasExpired()
    {
        using var db = CreateDbContext();
        db.FlashSales.Add(new FlashSale
        {
            Name = "Past Sale",
            StartTime = DateTime.UtcNow.AddHours(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-1),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var response = await controller.GetActiveFlashSale();
        Assert.IsType<NotFoundObjectResult>(response);
    }

    // ─── TC-T3-054: GET /api/v1/flashsales/active when future -> 404 ──────────
    [Fact]
    public async Task TC_T3_054_ApiV1_GetActiveFlashSale_Returns404WhenCampaignStartsInFuture()
    {
        using var db = CreateDbContext();
        db.FlashSales.Add(new FlashSale
        {
            Name = "Future Sale",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(6),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var response = await controller.GetActiveFlashSale();
        Assert.IsType<NotFoundObjectResult>(response);
    }

    // ─── TC-T3-055: GET /api/v1/flashsales/active ongoing -> 200 OK ───────────
    [Fact]
    public async Task TC_T3_055_ApiV1_GetActiveFlashSale_Returns200OkWithOngoingCampaign()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Creed", Slug = "creed" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Aventus", Slug = "creed-aventus", Sku = "CRD-AV", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 8_500_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.FlashSales.Add(new FlashSale
        {
            Name = "Golden Hour Flash Sale",
            StartTime = DateTime.UtcNow.AddMinutes(-15),
            EndTime = DateTime.UtcNow.AddHours(1),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { ProductId = 1, Product = prod, FlashSalePrice = 6_800_000m, StockLimit = 10, SoldQuantity = 3 }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        Assert.Equal("Golden Hour Flash Sale", payload.Name);
        Assert.True(payload.IsActive);
        Assert.True(payload.IsOngoing);
        Assert.Single(payload.Items);
    }

    // ─── TC-T3-056: GET /api/v1/flashsales/active remainingSeconds accuracy ───
    [Fact]
    public async Task TC_T3_056_ApiV1_GetActiveFlashSale_ComputesPositiveRemainingSeconds()
    {
        using var db = CreateDbContext();
        db.FlashSales.Add(new FlashSale
        {
            Name = "Countdown Test",
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddSeconds(1800),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        Assert.InRange(payload.RemainingSeconds, 1780, 1805);
    }

    // ─── TC-T3-057: GET /api/v1/flashsales/active discounted item metrics ─────
    [Fact]
    public async Task TC_T3_057_ApiV1_GetActiveFlashSale_ReturnsDiscountPercentAndRemainingStockPerItem()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "MFK", Slug = "mfk" };
        var cat = new Category { Id = 1, Name = "Extrait", Slug = "extrait" };
        var prod = new Product { Id = 1, Name = "BR540 Extrait", Slug = "br540-extrait", Sku = "MFK-540E", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 10_000_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.FlashSales.Add(new FlashSale
        {
            Name = "MFK Exclusive",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { ProductId = 1, Product = prod, FlashSalePrice = 7_500_000m, StockLimit = 20, SoldQuantity = 8 }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        var item = Assert.Single(payload.Items);
        Assert.Equal(10_000_000m, item.OriginalPrice);
        Assert.Equal(7_500_000m, item.FlashSalePrice);
        Assert.Equal(25, item.DiscountPercent);
        Assert.Equal(12, item.RemainingQuantity);
    }

    // ─── TC-T3-058: GET /api/v1/flashsales/active excludes soft-deleted items ─
    [Fact]
    public async Task TC_T3_058_ApiV1_GetActiveFlashSale_ExcludesSoftDeletedFlashSaleItems()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Le Labo", Slug = "le-labo" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        var prod = new Product { Id = 1, Name = "Another 13", Slug = "another-13", Sku = "LL-A13", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat, BasePrice = 6_000_000m };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.Add(prod);
        db.FlashSales.Add(new FlashSale
        {
            Name = "Filter Deleted Items",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(2),
            IsActive = true,
            Items = new List<FlashSaleItem>
            {
                new() { ProductId = 1, Product = prod, FlashSalePrice = 4_800_000m, StockLimit = 10, IsDeleted = false },
                new() { ProductId = 1, Product = prod, FlashSalePrice = 3_000_000m, StockLimit = 10, IsDeleted = true }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var okResult = Assert.IsType<OkObjectResult>(await controller.GetActiveFlashSale());
        var payload = Assert.IsType<FlashSaleActiveApiResponse>(okResult.Value);

        Assert.Single(payload.Items);
        Assert.Equal(4_800_000m, payload.Items[0].FlashSalePrice);
    }

    // ─── TC-T3-059: GET /api/v1/loyalty/status unauthenticated -> 401 ─────────
    [Fact]
    public async Task TC_T3_059_ApiV1_GetLoyaltyStatus_Returns401WhenUnauthenticated()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: null);

        var res = await controller.GetLoyaltyStatus();
        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ─── TC-T3-060: GET /api/v1/loyalty/status unknown user -> 401 ────────────
    [Fact]
    public async Task TC_T3_060_ApiV1_GetLoyaltyStatus_Returns401WhenUserDoesNotExistInDb()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "ghost-user-id", email: "ghost@test.vn", roles: "Customer");

        var res = await controller.GetLoyaltyStatus();
        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ─── TC-T3-061: GET /api/v1/loyalty/status Bronze tier ────────────────────
    [Fact]
    public async Task TC_T3_061_ApiV1_GetLoyaltyStatus_ReturnsBronzeTierWithZeroDiscountForNewUser()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-bronze", UserName = "bronze@test.vn", Email = "bronze@test.vn", FullName = "Bronze Member" });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-bronze", email: "bronze@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Bronze", dto.Tier);
        Assert.Equal(0m, dto.TotalSpent);
        Assert.Equal(0, dto.DiscountPercent);
        Assert.Equal("Silver", dto.NextTier);
        Assert.Equal(5_000_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-062: GET /api/v1/loyalty/status Silver tier (3% discount) ──────
    [Fact]
    public async Task TC_T3_062_ApiV1_GetLoyaltyStatus_ReturnsSilverTierWith3PercentDiscount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-silver", UserName = "silver@test.vn", Email = "silver@test.vn", FullName = "Silver Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-SILVER",
            UserId = "u-silver",
            CustomerName = "Silver Member",
            CustomerPhone = "0901",
            CustomerEmail = "silver@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 6_500_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-silver", email: "silver@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Silver", dto.Tier);
        Assert.Equal(6_500_000m, dto.TotalSpent);
        Assert.Equal(3, dto.DiscountPercent);
        Assert.Equal(65, dto.Points);
        Assert.Equal("Gold", dto.NextTier);
        Assert.Equal(8_500_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-063: GET /api/v1/loyalty/status Gold tier (7% discount) ────────
    [Fact]
    public async Task TC_T3_063_ApiV1_GetLoyaltyStatus_ReturnsGoldTierWith7PercentDiscount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-gold", UserName = "gold@test.vn", Email = "gold@test.vn", FullName = "Gold Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-GOLD",
            UserId = "u-gold",
            CustomerName = "Gold Member",
            CustomerPhone = "0902",
            CustomerEmail = "gold@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 18_000_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-gold", email: "gold@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Gold", dto.Tier);
        Assert.Equal(18_000_000m, dto.TotalSpent);
        Assert.Equal(7, dto.DiscountPercent);
        Assert.Equal(180, dto.Points);
        Assert.Equal("Diamond", dto.NextTier);
        Assert.Equal(12_000_000m, dto.AmountToNextTier);
    }

    // ─── TC-T3-064: GET /api/v1/loyalty/status Diamond tier (12% discount) ────
    [Fact]
    public async Task TC_T3_064_ApiV1_GetLoyaltyStatus_ReturnsDiamondTierWith12PercentDiscountAndZeroNextAmount()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-diamond", UserName = "diamond@test.vn", Email = "diamond@test.vn", FullName = "Diamond Member" });
        db.Orders.Add(new Order
        {
            OrderCode = "ORD-DIAMOND",
            UserId = "u-diamond",
            CustomerName = "Diamond Member",
            CustomerPhone = "0903",
            CustomerEmail = "diamond@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 35_000_000m,
            OrderStatus = OrderStatus.Delivered
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-diamond", email: "diamond@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Diamond", dto.Tier);
        Assert.Equal(35_000_000m, dto.TotalSpent);
        Assert.Equal(12, dto.DiscountPercent);
        Assert.Equal("Max", dto.NextTier);
        Assert.Equal(0m, dto.AmountToNextTier);
    }

    // ─── TC-T3-065: GET /api/v1/loyalty/status ignores non-Delivered orders ───
    [Fact]
    public async Task TC_T3_065_ApiV1_GetLoyaltyStatus_IgnoresPendingAndCancelledOrdersInTotalSpent()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-pending", UserName = "p@test.vn", Email = "p@test.vn", FullName = "Pending Buyer" });
        db.Orders.AddRange(
            new Order { OrderCode = "P-1", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 20_000_000m, OrderStatus = OrderStatus.Pending },
            new Order { OrderCode = "P-2", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 20_000_000m, OrderStatus = OrderStatus.Cancelled },
            new Order { OrderCode = "P-3", UserId = "u-pending", CustomerName = "P", CustomerPhone = "09", CustomerEmail = "p@test.vn", ShippingAddress = "HCM", TotalAmount = 2_000_000m, OrderStatus = OrderStatus.Delivered });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-pending", email: "p@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal(2_000_000m, dto.TotalSpent);
        Assert.Equal("Bronze", dto.Tier);
    }

    // ─── TC-T3-066: GET /api/v1/loyalty/status respects UserLoyalty record ────
    [Fact]
    public async Task TC_T3_066_ApiV1_GetLoyaltyStatus_RespectsManualVipUpgradeInUserLoyaltyTable()
    {
        using var db = CreateDbContext();
        db.Users.Add(new AppUser { Id = "u-vip-manual", UserName = "vipm@test.vn", Email = "vipm@test.vn", FullName = "Invited VIP" });
        db.UserLoyalties.Add(new UserLoyalty
        {
            UserId = "u-vip-manual",
            Tier = UserTier.Gold,
            Points = 500,
            TotalSpent = 16_000_000m
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        AttachApiContext(controller, userId: "u-vip-manual", email: "vipm@test.vn", roles: "Customer");

        var ok = Assert.IsType<OkObjectResult>(await controller.GetLoyaltyStatus());
        var dto = Assert.IsType<LoyaltyStatusApiResponse>(ok.Value);

        Assert.Equal("Gold", dto.Tier);
        Assert.Equal(7, dto.DiscountPercent);
        Assert.Equal(500, dto.Points);
        Assert.Equal(16_000_000m, dto.TotalSpent);
    }

    // ─── TC-T3-067: Loyalty tier boundary thresholds ──────────────────────────
    [Theory]
    [InlineData(0, UserTier.Bronze, 0, "Silver", 5_000_000)]
    [InlineData(4_999_999, UserTier.Bronze, 0, "Silver", 1)]
    [InlineData(5_000_000, UserTier.Silver, 3, "Gold", 10_000_000)]
    [InlineData(14_999_999, UserTier.Silver, 3, "Gold", 1)]
    [InlineData(15_000_000, UserTier.Gold, 7, "Diamond", 15_000_000)]
    [InlineData(29_999_999, UserTier.Gold, 7, "Diamond", 1)]
    [InlineData(30_000_000, UserTier.Diamond, 12, "Max", 0)]
    public void TC_T3_067_ApiV1_LoyaltyTierBoundaries_MatchSpecification(
        decimal spent,
        UserTier expectedTier,
        int expectedDiscount,
        string expectedNextTier,
        decimal expectedAmountToNext)
    {
        var tier = ApiV1ExtensionsController.CalculateTierFromSpent(spent);
        var (discount, nextTier, amountToNext) = ApiV1ExtensionsController.GetTierProgress(tier, spent);

        Assert.Equal(expectedTier, tier);
        Assert.Equal(expectedDiscount, discount);
        Assert.Equal(expectedNextTier, nextTier);
        Assert.Equal(expectedAmountToNext, amountToNext);
    }

    // ─── TC-T3-068: POST /api/v1/discovery-box/validate-and-add ClassicWood 3 vials ─
    [Fact]
    public async Task TC_T3_068_ApiV1_DiscoveryBox_ValidatesClassicWoodWith3Vials2ml()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = "Happy Anniversary!",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "2ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        Assert.True(res.Valid);
        Assert.Equal("ClassicWood", res.BoxStyle);
        Assert.Equal(3, res.VialCount);
        // 3 * 180,000 + 150,000 box = 690,000 (0% discount)
        Assert.Equal(690_000m, res.SubTotal);
        Assert.Equal(0m, res.DiscountAmount);
        Assert.Equal(690_000m, res.TotalPrice);
    }

    // ─── TC-T3-069: POST /api/v1/discovery-box/validate-and-add RoyalVelvet 4 vials ─
    [Fact]
    public async Task TC_T3_069_ApiV1_DiscoveryBox_ValidatesRoyalVelvetWith4Vials5mlAnd5PercentDiscount()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "RoyalVelvet",
            GreetingMessage = "Quà tặng sang trọng",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "5ml" },
                new() { ProductId = 2, VialSize = "5ml" },
                new() { ProductId = 3, VialSize = "5ml" },
                new() { ProductId = 4, VialSize = "5ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        Assert.True(res.Valid);
        Assert.Equal("RoyalVelvet", res.BoxStyle);
        Assert.Equal(4, res.VialCount);
        // 4 * 350,000 + 200,000 = 1,600,000; 5% discount = 80,000; Total = 1,520,000
        Assert.Equal(1_600_000m, res.SubTotal);
        Assert.Equal(80_000m, res.DiscountAmount);
        Assert.Equal(1_520_000m, res.TotalPrice);
    }

    // ─── TC-T3-070: POST /api/v1/discovery-box/validate-and-add ArtisanLeather 5 vials ─
    [Fact]
    public async Task TC_T3_070_ApiV1_DiscoveryBox_ValidatesArtisanLeatherWith5VialsAnd10PercentDiscount()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ArtisanLeather",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "5ml" },
                new() { ProductId = 2, VialSize = "5ml" },
                new() { ProductId = 3, VialSize = "2ml" },
                new() { ProductId = 4, VialSize = "2ml" },
                new() { ProductId = 5, VialSize = "2ml" }
            }
        };

        var ok = Assert.IsType<OkObjectResult>(await controller.ValidateAndAddDiscoveryBox(req));
        var res = Assert.IsType<DiscoveryBoxApiResponse>(ok.Value);

        // (2 * 350,000 + 3 * 180,000) + 300,000 = 1,540,000; 10% discount = 154,000; Total = 1,386,000
        Assert.Equal(1_540_000m, res.SubTotal);
        Assert.Equal(154_000m, res.DiscountAmount);
        Assert.Equal(1_386_000m, res.TotalPrice);
    }

    // ─── TC-T3-071: POST /api/v1/discovery-box/validate-and-add < 3 vials -> 400 ─
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TC_T3_071_ApiV1_DiscoveryBox_Returns400WhenFewerThan3Vials(int count)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = Enumerable.Range(1, count).Select(i => new DiscoveryVialApiSelectionDto { ProductId = i, VialSize = "2ml" }).ToList()
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-072: POST /api/v1/discovery-box/validate-and-add > 5 vials -> 400 ─
    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public async Task TC_T3_072_ApiV1_DiscoveryBox_Returns400WhenMoreThan5Vials(int count)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "RoyalVelvet",
            Vials = Enumerable.Range(1, count).Select(i => new DiscoveryVialApiSelectionDto { ProductId = i, VialSize = "2ml" }).ToList()
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-073: POST /api/v1/discovery-box/validate-and-add invalid boxStyle -> 400 ─
    [Theory]
    [InlineData("PlasticBag")]
    [InlineData("NeonMetal")]
    public async Task TC_T3_073_ApiV1_DiscoveryBox_Returns400ForUnsupportedBoxStyle(string invalidStyle)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = invalidStyle,
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "2ml" }
            }
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-074: POST /api/v1/discovery-box/validate-and-add invalid vialSize -> 400 ─
    [Theory]
    [InlineData("10ml")]
    [InlineData("100ml")]
    [InlineData("invalid")]
    public async Task TC_T3_074_ApiV1_DiscoveryBox_Returns400ForUnsupportedVialSize(string invalidSize)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var req = new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = invalidSize },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 3, VialSize = "5ml" }
            }
        };

        var res = await controller.ValidateAndAddDiscoveryBox(req);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-075: POST /api/v1/discovery-box/validate-and-add greeting > 250 chars -> 400 ─
    [Fact]
    public async Task TC_T3_075_ApiV1_DiscoveryBox_ValidatesGreetingMessageMax250Characters()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var vials = new List<DiscoveryVialApiSelectionDto>
        {
            new() { ProductId = 1, VialSize = "2ml" },
            new() { ProductId = 2, VialSize = "2ml" },
            new() { ProductId = 3, VialSize = "2ml" }
        };

        // 251 chars -> 400 BadRequest
        var badRes = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = new string('A', 251),
            Vials = vials
        });
        Assert.IsType<BadRequestObjectResult>(badRes);

        // Exactly 250 chars -> 200 OK
        var okRes = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            GreetingMessage = new string('B', 250),
            Vials = vials
        });
        Assert.IsType<OkObjectResult>(okRes);
    }

    // ─── TC-T3-076: POST /api/v1/discovery-box/validate-and-add non-existent ProductId -> 400 ─
    [Fact]
    public async Task TC_T3_076_ApiV1_DiscoveryBox_Returns400WhenProductDoesNotExistInSeededCatalog()
    {
        using var db = CreateDbContext();
        var brand = new Brand { Id = 1, Name = "Creed", Slug = "creed" };
        var cat = new Category { Id = 1, Name = "EDP", Slug = "edp" };
        db.Brands.Add(brand);
        db.Categories.Add(cat);
        db.Products.AddRange(
            new Product { Id = 1, Name = "P1", Slug = "p1", Sku = "S1", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat },
            new Product { Id = 2, Name = "P2", Slug = "p2", Sku = "S2", BrandId = 1, Brand = brand, CategoryId = 1, Category = cat });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var res = await controller.ValidateAndAddDiscoveryBox(new DiscoveryBoxApiRequest
        {
            BoxStyle = "ClassicWood",
            Vials = new List<DiscoveryVialApiSelectionDto>
            {
                new() { ProductId = 1, VialSize = "2ml" },
                new() { ProductId = 2, VialSize = "2ml" },
                new() { ProductId = 999, VialSize = "2ml" } // missing product
            }
        });

        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-077: GET /api/v1/orders/track missing orderCode -> 400 ─────────
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TC_T3_077_ApiV1_TrackOrder_Returns400WhenOrderCodeIsEmpty(string? emptyCode)
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.TrackOrder(emptyCode);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // ─── TC-T3-078: GET /api/v1/orders/track non-existent orderCode -> 404 ────
    [Fact]
    public async Task TC_T3_078_ApiV1_TrackOrder_Returns404WhenOrderCodeNotFound()
    {
        using var db = CreateDbContext();
        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));

        var res = await controller.TrackOrder("LUXE-NOT-FOUND-999");
        Assert.IsType<NotFoundObjectResult>(res);
    }

    // ─── TC-T3-079: GET /api/v1/orders/track valid order with gift wrapping ───
    [Fact]
    public async Task TC_T3_079_ApiV1_TrackOrder_Returns200OkWithItemsGiftWrappingAndPaymentInfo()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-20261013-VIP1",
            CustomerName = "Trương Ngọc Ánh",
            CustomerPhone = "0908889999",
            CustomerEmail = "anh@test.vn",
            ShippingAddress = "88 Đồng Khởi, Q1",
            City = "TP.HCM",
            TrackingNumber = "VNPOST-888",
            SubTotal = 7_500_000m,
            DiscountAmount = 500_000m,
            ShippingFee = 0m,
            IsGiftWrapping = true,
            GiftWrappingFee = 50_000m,
            GiftMessage = "Gửi tặng người thương",
            TotalAmount = 7_050_000m,
            OrderStatus = OrderStatus.Shipping,
            PaymentStatus = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.BankTransfer,
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Portrait of a Lady", VariantLabel = "100ml", UnitPrice = 7_500_000m, Quantity = 1 }
            }
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("luxe-20261013-vip1"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Equal("LUXE-20261013-VIP1", dto.OrderCode);
        Assert.Equal("Shipping", dto.OrderStatus);
        Assert.Equal("Completed", dto.PaymentStatus);
        Assert.True(dto.IsGiftWrapping);
        Assert.Equal(50_000m, dto.GiftWrappingFee);
        Assert.Equal("Gửi tặng người thương", dto.GiftMessage);
        Assert.Single(dto.Items);
        Assert.Equal("Portrait of a Lady", dto.Items[0].ProductName);
    }

    // ─── TC-T3-080: GET /api/v1/orders/track timeline stages for Delivered ────
    [Fact]
    public async Task TC_T3_080_ApiV1_TrackOrder_TimelineMarksAllFourStagesCompletedWhenDelivered()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-DELIV-100",
            CustomerName = "Khách Nhận Hàng",
            CustomerPhone = "0901234567",
            CustomerEmail = "d@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 4_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("LUXE-DELIV-100"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Equal(4, dto.Timeline.Count);
        Assert.All(dto.Timeline, step => Assert.True(step.Completed));
    }

    // ─── TC-T3-081: GET /api/v1/orders/track timeline includes Cancelled stage ─
    [Fact]
    public async Task TC_T3_081_ApiV1_TrackOrder_TimelineAppendsCancelledStageWhenOrderCancelled()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            OrderCode = "LUXE-CANCEL-100",
            CustomerName = "Khách Hủy Đơn",
            CustomerPhone = "0901234567",
            CustomerEmail = "c@test.vn",
            ShippingAddress = "HCM",
            TotalAmount = 3_000_000m,
            OrderStatus = OrderStatus.Cancelled,
            PaymentStatus = PaymentStatus.Failed
        });
        await db.SaveChangesAsync();

        var controller = new ApiV1ExtensionsController(db, new FlashSaleService(db));
        var ok = Assert.IsType<OkObjectResult>(await controller.TrackOrder("LUXE-CANCEL-100"));
        var dto = Assert.IsType<OrderTrackApiResponse>(ok.Value);

        Assert.Contains(dto.Timeline, s => s.Stage == "Cancelled" && s.Completed);
    }

    // ─── TC-T3-082: GET /api/v1/orders/track phone verification ───────────────
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

    // ─── LIGHTWEIGHT TEST DOUBLES ─────────────────────────────────────────────

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kvp in values) _data[kvp.Key] = kvp.Value;
        }
    }

    private sealed class FakeDatabaseConsoleService : IDatabaseConsoleService
    {
        public Task<List<TableInfo>> GetAllTablesAsync() =>
            Task.FromResult(new List<TableInfo> { new() { Schema = "dbo", TableName = "Products", RowCount = 22 } });

        public Task<List<ColumnInfo>> GetTableColumnsAsync(string tableName) =>
            Task.FromResult(new List<ColumnInfo> { new() { ColumnName = "Id", DataType = "int", IsPrimaryKey = true } });

        public Task<TableDataResult> GetTableDataAsync(string tableName, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, bool isAscending = true) =>
            Task.FromResult(new TableDataResult { TableName = tableName, TotalRows = 1, CurrentPage = page, PageSize = pageSize });

        public Task<Dictionary<string, object?>?> GetTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue) =>
            Task.FromResult<Dictionary<string, object?>?>(new Dictionary<string, object?> { ["Id"] = primaryKeyValue });

        public Task<bool> InsertTableRowAsync(string tableName, Dictionary<string, object?> values) => Task.FromResult(true);
        public Task<bool> UpdateTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue, Dictionary<string, object?> values) => Task.FromResult(true);
        public Task<bool> DeleteTableRowAsync(string tableName, string primaryKeyColumn, object primaryKeyValue) => Task.FromResult(true);

        public Task<QueryResult> ExecuteRawQueryAsync(string sql) =>
            Task.FromResult(new QueryResult { Success = true, RowsAffected = 1, ElapsedMilliseconds = 5 });

        public Task<QueryResult> ExecuteScriptFileAsync(string scriptContent) =>
            Task.FromResult(new QueryResult { Success = true, RowsAffected = 1, ElapsedMilliseconds = 10 });
    }

    private sealed class FakeOrderEmailSender : IOrderEmailSender
    {
        public Task SendOrderConfirmationAsync(Order order, string? trackingUrl = null) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string fullName, string resetUrl) => Task.CompletedTask;
    }

    private sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => new("req", "cookie", "form", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => new("req", "cookie", "form", "header");
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Perfume.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Perfume.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
