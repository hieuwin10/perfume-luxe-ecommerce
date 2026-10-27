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

public sealed class AdminInventoryAndAccessControlTests : TestFixtures
{
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

}
