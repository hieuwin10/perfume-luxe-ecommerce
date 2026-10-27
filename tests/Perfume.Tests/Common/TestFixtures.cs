using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
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

namespace Perfume.Tests;

public abstract class TestFixtures
{
    protected static PerfumeDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase($"Team1Db_{Guid.NewGuid():N}")
            .Options;
        return new PerfumeDbContext(options);
    }

    protected static (Brand Brand, Category Category, FragranceFamily Family) SeedCoreTaxonomy(
        PerfumeDbContext db,
        int id = 1,
        string brandName = "Creed",
        string brandSlug = "creed",
        string catName = "Niche Luxury",
        string catSlug = "niche-luxury",
        string famName = "Gỗ Hoàng Gia",
        string famSlug = "go-hoang-gia",
        ScentFamily scentType = ScentFamily.Woody)
    {
        var brand = new Brand { Id = id, Name = brandName, Slug = brandSlug, Country = "France", IsFeatured = true };
        var category = new Category { Id = id, Name = catName, Slug = catSlug, IsFeatured = true };
        var family = new FragranceFamily { Id = id, Name = famName, Slug = famSlug, ColorHex = "#D4AF37", ScentType = scentType };
        db.Brands.Add(brand);
        db.Categories.Add(category);
        db.FragranceFamilies.Add(family);
        return (brand, category, family);
    }

    protected sealed class NoOpOrderEmailSender : IOrderEmailSender
    {
        public string? LastOrderCode { get; private set; }
        public string? LastTrackingUrl { get; private set; }

        public Task SendOrderConfirmationAsync(Order order, string? guestTrackingUrl = null)
        {
            LastOrderCode = order.OrderCode;
            LastTrackingUrl = guestTrackingUrl;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string fullName, string resetUrl)
        {
            return Task.CompletedTask;
        }
    }

    protected static PerfumeDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(dbName ?? $"Team3_TestDb_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new PerfumeDbContext(options);
    }

    protected static UserManager<AppUser> CreateUserManager(PerfumeDbContext db)
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

    protected static RoleManager<AppRole> CreateRoleManager(PerfumeDbContext db)
    {
        var store = new RoleStore<AppRole, PerfumeDbContext, string>(db);
        var validators = new List<IRoleValidator<AppRole>> { new RoleValidator<AppRole>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = NullLogger<RoleManager<AppRole>>.Instance;

        return new RoleManager<AppRole>(store, validators, keyNormalizer, errors, logger);
    }

    protected static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
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

    protected static void AttachHttpContext(Controller controller, string? userId = null, string? email = null, params string[] roles)
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

    protected static void AttachApiContext(ControllerBase controller, string? userId = null, string? email = null, bool bearerHeader = true, params string[] roles)
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

    protected static AdminDashboardController CreateAdminController(
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

    protected static CustomerApiController CreateCustomerApiController(
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

    // ─── LIGHTWEIGHT TEST DOUBLES ─────────────────────────────────────────────

    protected sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    protected sealed class FakeTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kvp in values) _data[kvp.Key] = kvp.Value;
        }
    }

    protected sealed class FakeDatabaseConsoleService : IDatabaseConsoleService
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

    protected sealed class FakeOrderEmailSender : IOrderEmailSender
    {
        public Task SendOrderConfirmationAsync(Order order, string? trackingUrl = null) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string fullName, string resetUrl) => Task.CompletedTask;
    }

    protected sealed class FakeAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => new("req", "cookie", "form", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => new("req", "cookie", "form", "header");
        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);
        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    protected sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Perfume.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    protected sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Perfume.Web";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
