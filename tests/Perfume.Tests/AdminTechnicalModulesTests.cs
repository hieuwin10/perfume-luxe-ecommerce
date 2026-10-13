using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Perfume.Web.Areas.Admin.Controllers;
using Perfume.Web.Middlewares;
using Xunit;

namespace Perfume.Tests;

public class AdminTechnicalModulesTests
{
    private static PerfumeDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PerfumeDbContext(options);
    }

    private static UserManager<AppUser> CreateUserManager(PerfumeDbContext db)
    {
        var store = new UserStore<AppUser, AppRole, PerfumeDbContext, string>(db);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger<UserManager<AppUser>>.Instance;

        return new UserManager<AppUser>(
            store, options, passwordHasher, userValidators, passwordValidators,
            keyNormalizer, errors, services, logger);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=PerfumeDb;Trusted_Connection=True;",
            ["Store:Name"] = "PERFUME LUXE"
        };
        if (overrides != null)
        {
            foreach (var kvp in overrides) dict[kvp.Key] = kvp.Value;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static AdminDashboardController CreateAdminController(
        PerfumeDbContext db,
        IConfiguration? config = null,
        string? actorUserId = "admin-123",
        params string[] roles)
    {
        var configuration = config ?? CreateConfiguration();
        var userManager = CreateUserManager(db);
        var flashSale = new FlashSaleService(db);
        var settingService = new SystemSettingService(db, configuration, NullLogger<SystemSettingService>.Instance);
        var blogService = new BlogService(db);
        var dbConsole = new FakeDatabaseConsoleService();

        var controller = new AdminDashboardController(
            db, dbConsole, userManager, flashSale, settingService, blogService, configuration);

        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(actorUserId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, actorUserId),
                new(ClaimTypes.Name, "Admin Tester"),
                new(ClaimTypes.Email, "admin@perfumeluxe.vn")
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
        return controller;
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

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MODULE 1: SYSTEM HEALTH CONSOLE & 1-CLICK CACHE PURGE
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_M1_001_SystemHealth_ReturnsHealthyMetrics()
    {
        using var db = CreateInMemoryDbContext();
        db.DatabaseAuditLogs.Add(new DatabaseAuditLog { Action = "LOGIN", Succeeded = true });
        db.SystemErrorLogs.Add(new SystemErrorLog { ErrorCode = "ERR-001", Message = "Sample" });
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db, roles: "Admin");
        var result = await controller.SystemHealth();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SystemHealthViewModel>(viewResult.Model);

        Assert.NotNull(model.DotNetVersion);
        Assert.NotNull(model.OsDescription);
        Assert.True(model.ProcessorCount >= 1);
        Assert.True(model.ServerUptime >= TimeSpan.Zero);
        Assert.Equal(1, model.TotalAuditLogsCount);
        Assert.Equal(1, model.TotalErrorLogsCount);
    }

    [Fact]
    public async Task TC_M1_002_FlushCache_PurgesAndRecordsAuditLog()
    {
        using var db = CreateInMemoryDbContext();
        var controller = CreateAdminController(db, roles: "Admin");

        var result = await controller.FlushCache();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminDashboardController.SystemHealth), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);

        var auditLog = await db.DatabaseAuditLogs.FirstOrDefaultAsync(a => a.Action == "PURGE_SYSTEM_CACHE");
        Assert.NotNull(auditLog);
        Assert.True(auditLog.Succeeded);
        Assert.Equal("ApplicationMemoryAndRuntimeCache", auditLog.Target);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MODULE 2: ENTERPRISE MAINTENANCE MODE MIDDLEWARE
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_M2_001_MaintenanceMiddleware_AllowsRequest_WhenMaintenanceDisabled()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto { IsMaintenanceMode = false });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/bo-suu-tap";

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task TC_M2_002_MaintenanceMiddleware_AllowsAdmin_WhenMaintenanceEnabled()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto
        {
            IsMaintenanceMode = true,
            MaintenanceMessage = "Đang bảo trì định kỳ"
        });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/bo-suu-tap";
        var claims = new[] { new Claim(ClaimTypes.Name, "AdminUser"), new Claim(ClaimTypes.Role, "Admin") };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie"));

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task TC_M2_003_MaintenanceMiddleware_AllowsWhitelistedIp_WhenMaintenanceEnabled()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto
        {
            IsMaintenanceMode = true,
            AllowedBypassIps = "192.168.1.100, 10.0.0.1"
        });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/bo-suu-tap";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/js/app.js")]
    [InlineData("/account/login")]
    [InlineData("/account/logout")]
    public async Task TC_M2_004_MaintenanceMiddleware_AllowsStaticAndLoginRoutes(string route)
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto { IsMaintenanceMode = true });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = route;

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; ctx.Response.StatusCode = 200; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task TC_M2_005_MaintenanceMiddleware_BlocksCustomerWith503Html()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto
        {
            IsMaintenanceMode = true,
            MaintenanceMessage = "Bảo trì nâng cấp bảo mật",
            ExpectedEndTime = "22:00 hôm nay"
        });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/bo-suu-tap";
        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(503, context.Response.StatusCode);

        memStream.Position = 0;
        using var reader = new StreamReader(memStream);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Contains("Hệ Thống Đang Nâng Cấp", responseBody);
        Assert.Contains("Bảo trì nâng cấp bảo mật", responseBody);
        Assert.Contains("22:00 hôm nay", responseBody);
    }

    [Fact]
    public async Task TC_M2_006_MaintenanceMiddleware_BlocksApiRouteWith503Json()
    {
        using var db = CreateInMemoryDbContext();
        var config = CreateConfiguration();
        var settingService = new SystemSettingService(db, config, NullLogger<SystemSettingService>.Instance);
        await settingService.SaveSettingsAsync(new SystemSettingsDto
        {
            IsMaintenanceMode = true,
            MaintenanceMessage = "API maintenance active"
        });

        var services = new ServiceCollection();
        services.AddSingleton<ISystemSettingService>(settingService);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/api/v1/products";
        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        bool nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new MaintenanceMiddleware(next, NullLogger<MaintenanceMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType);

        memStream.Position = 0;
        using var reader = new StreamReader(memStream);
        var json = await reader.ReadToEndAsync();
        Assert.Contains("API maintenance active", json);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // MODULE 3: SYSTEM EXCEPTION LOGGING & CRASH LOGS TRACKER
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TC_M3_001_ExceptionLoggingMiddleware_CapturesException_LogsToDbAndReturnsErrorCode()
    {
        using var db = CreateInMemoryDbContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/cart/checkout";
        context.Request.Method = "POST";
        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        RequestDelegate next = _ => throw new InvalidOperationException("Simulated catastrophic crash");

        var middleware = new SystemExceptionLoggingMiddleware(next, NullLogger<SystemExceptionLoggingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        // Verify logged in DB
        var errorLog = await db.SystemErrorLogs.FirstOrDefaultAsync();
        Assert.NotNull(errorLog);
        Assert.StartsWith("ERR-", errorLog.ErrorCode);
        Assert.Equal("Simulated catastrophic crash", errorLog.Message);
        Assert.Equal("/cart/checkout", errorLog.RequestPath);
        Assert.Equal("POST", errorLog.HttpMethod);
        Assert.Equal(500, errorLog.StatusCode);

        // Verify HTML output contains ErrorCode
        memStream.Position = 0;
        using var reader = new StreamReader(memStream);
        var html = await reader.ReadToEndAsync();
        Assert.Contains(errorLog.ErrorCode, html);
    }

    [Fact]
    public async Task TC_M3_002_ExceptionLoggingMiddleware_CapturesException_OnApiRoute_ReturnsJsonErrorCode()
    {
        using var db = CreateInMemoryDbContext();
        var services = new ServiceCollection();
        services.AddSingleton(db);
        var sp = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = sp };
        context.Request.Path = "/api/v1/checkout/process";
        context.Request.Method = "POST";
        var memStream = new MemoryStream();
        context.Response.Body = memStream;

        RequestDelegate next = _ => throw new ArgumentException("Invalid API token payload");

        var middleware = new SystemExceptionLoggingMiddleware(next, NullLogger<SystemExceptionLoggingMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType);

        memStream.Position = 0;
        using var reader = new StreamReader(memStream);
        var json = await reader.ReadToEndAsync();
        Assert.Contains("errorCode", json);
        Assert.Contains("ERR-", json);
    }

    [Fact]
    public async Task TC_M3_003_ErrorLogs_FiltersAndPaginates()
    {
        using var db = CreateInMemoryDbContext();
        db.SystemErrorLogs.AddRange(
            new SystemErrorLog { ErrorCode = "ERR-100", Message = "NullReference", StatusCode = 500, RequestPath = "/home" },
            new SystemErrorLog { ErrorCode = "ERR-200", Message = "Timeout", StatusCode = 503, RequestPath = "/catalog" },
            new SystemErrorLog { ErrorCode = "ERR-300", Message = "BadRequest", StatusCode = 400, RequestPath = "/api/test" }
        );
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db, roles: "Admin");

        // Filter by search
        var searchResult = Assert.IsType<ViewResult>(await controller.ErrorLogs("Timeout", null, 1));
        var searchVm = Assert.IsType<SystemErrorLogsViewModel>(searchResult.Model);
        Assert.Single(searchVm.Logs);
        Assert.Equal("ERR-200", searchVm.Logs[0].ErrorCode);

        // Filter by status code
        var statusResult = Assert.IsType<ViewResult>(await controller.ErrorLogs(null, 500, 1));
        var statusVm = Assert.IsType<SystemErrorLogsViewModel>(statusResult.Model);
        Assert.Single(statusVm.Logs);
        Assert.Equal("ERR-100", statusVm.Logs[0].ErrorCode);
    }

    [Fact]
    public async Task TC_M3_004_ClearErrorLogs_RemovesAllOrOldLogs_AndAudits()
    {
        using var db = CreateInMemoryDbContext();
        db.SystemErrorLogs.AddRange(
            new SystemErrorLog { ErrorCode = "ERR-OLD", Message = "Old 45 days ago", CreatedAt = DateTime.UtcNow.AddDays(-45) },
            new SystemErrorLog { ErrorCode = "ERR-RECENT", Message = "Recent 2 days ago", CreatedAt = DateTime.UtcNow.AddDays(-2) }
        );
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db, roles: "Admin");

        // Clear older than 30 days
        var oldResult = await controller.ClearErrorLogs("old");
        Assert.IsType<RedirectToActionResult>(oldResult);
        var remainingAfterOld = await db.SystemErrorLogs.ToListAsync();
        Assert.Single(remainingAfterOld);
        Assert.Equal("ERR-RECENT", remainingAfterOld[0].ErrorCode);

        // Clear all
        var allResult = await controller.ClearErrorLogs("all");
        Assert.IsType<RedirectToActionResult>(allResult);
        var remainingAfterAll = await db.SystemErrorLogs.ToListAsync();
        Assert.Empty(remainingAfterAll);

        // Verify audit log entries
        var auditLogs = await db.DatabaseAuditLogs.ToListAsync();
        Assert.Contains(auditLogs, a => a.Action == "PURGE_OLD_ERROR_LOGS");
        Assert.Contains(auditLogs, a => a.Action == "CLEAR_ALL_ERROR_LOGS");
    }
}
