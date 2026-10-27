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

public sealed class AdminAuditAndDatabaseConsoleTests : TestFixtures
{
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

}
