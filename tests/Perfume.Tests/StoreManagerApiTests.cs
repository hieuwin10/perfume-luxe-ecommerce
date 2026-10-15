using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Web.Controllers;

namespace Perfume.Tests;

public sealed class StoreManagerApiTests
{
    private static PerfumeDbContext CreateDb() => new(new DbContextOptionsBuilder<PerfumeDbContext>()
        .UseInMemoryDatabase($"StoreManagerApi_{Guid.NewGuid():N}")
        .Options);

    private static StoreManagerApiController CreateController(PerfumeDbContext db)
    {
        var controller = new StoreManagerApiController(db, null!);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "manager-1"),
            new Claim(ClaimTypes.Role, "StoreManager")
        }, "Bearer"));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    [Fact]
    public void StoreManagerApi_RequiresStoreManagerPolicy()
    {
        var authorize = typeof(StoreManagerApiController).GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal("StoreManagerApi", authorize!.Policy);
    }

    [Fact]
    public async Task Dashboard_CountsOnlyDeliveredPaidOrdersAsRevenue()
    {
        await using var db = CreateDb();
        var today = DateTime.UtcNow.Date;
        var paid = new Order
        {
            OrderCode = "PL-PAID-1", CustomerName = "A", CustomerEmail = "a@example.com", CustomerPhone = "1",
            ShippingAddress = "Address", CreatedAt = today.AddHours(10), OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed, TotalAmount = 250m, VoucherCode = "LUXE"
        };
        paid.Items.Add(new OrderItem { ProductName = "Oud", ProductId = 1, UnitPrice = 250m, Quantity = 1 });
        db.Orders.Add(paid);
        db.Orders.Add(new Order
        {
            OrderCode = "PL-PENDING-1", CustomerName = "B", CustomerEmail = "b@example.com", CustomerPhone = "2",
            ShippingAddress = "Address", CreatedAt = today.AddHours(11), OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending, TotalAmount = 900m
        });
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await CreateController(db).Dashboard(today, today, default));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.Equal(250m, json.RootElement.GetProperty("revenue").GetDecimal());
        Assert.Equal(1, json.RootElement.GetProperty("paidOrders").GetInt32());
        Assert.Equal(250m, json.RootElement.GetProperty("averageOrderValue").GetDecimal());
        Assert.Equal(2, json.RootElement.GetProperty("orderCount").GetInt32());
    }

    [Fact]
    public async Task ModeratingRejectedReview_RecalculatesProductRating()
    {
        await using var db = CreateDb();
        var brand = new Brand { Name = "House", Slug = "house" };
        var category = new Category { Name = "Parfum", Slug = "parfum" };
        var product = new Product { Name = "Oud", Slug = "oud", Sku = "OUD", Brand = brand, Category = category, BasePrice = 1m, RatingCount = 2, RatingAverage = 3 };
        var rejected = new Review { Product = product, Rating = 5, IsApproved = true, UserName = "A" };
        var kept = new Review { Product = product, Rating = 1, IsApproved = true, UserName = "B" };
        db.AddRange(product, rejected, kept);
        await db.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await CreateController(db).ModerateReview(rejected.Id, new ModerateStoreReviewRequest { Approve = false }, default));

        Assert.NotNull(result.Value);
        Assert.False(rejected.IsApproved);
        Assert.Equal(1, product.RatingCount);
        Assert.Equal(1d, product.RatingAverage);
    }

    [Fact]
    public async Task UpdatingInventory_UpdatesProductAvailability()
    {
        await using var db = CreateDb();
        var product = new Product { Name = "Oud", Slug = "oud", Sku = "OUD", BasePrice = 1m, IsInStock = true };
        var variant = new ProductVariant { Product = product, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "OUD-100", OriginalPrice = 1m, SalePrice = 1m, StockQuantity = 5, IsDefault = true };
        db.AddRange(product, variant);
        await db.SaveChangesAsync();

        var result = await CreateController(db).UpdateInventory(variant.Id, new UpdateInventoryRequest { StockQuantity = 0 }, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(product.IsInStock);
        Assert.Equal(0, variant.StockQuantity);
    }

    [Fact]
    public async Task CancellationOfPaidOrder_IsRejected()
    {
        await using var db = CreateDb();
        var order = new Order
        {
            OrderCode = "PL-PAID-2", CustomerName = "A", CustomerEmail = "a@example.com", CustomerPhone = "1",
            ShippingAddress = "Address", OrderStatus = OrderStatus.Pending, PaymentStatus = PaymentStatus.Completed,
            PaymentMethod = PaymentMethod.BankTransfer, TotalAmount = 10m
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var result = await CreateController(db).UpdateOrderStatus(order.Id, new UpdateStoreOrderStatusRequest { Status = OrderStatus.Cancelled }, default);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(OrderStatus.Pending, order.OrderStatus);
    }

    [Fact]
    public async Task CancellingUnpaidOrder_RestoresStockVoucherAndAvailability()
    {
        await using var db = CreateDb();
        var product = new Product { Name = "Oud", Slug = "oud", Sku = "OUD", BasePrice = 10m, IsInStock = false };
        var variant = new ProductVariant { Product = product, VolumeMl = 100, VolumeLabel = "100ml", VariantSku = "OUD-100", OriginalPrice = 10m, SalePrice = 10m, StockQuantity = 0, IsDefault = true };
        var voucher = new Voucher { Code = "LUXE", UsedCount = 1, UsageLimit = 10 };
        var order = new Order
        {
            OrderCode = "PL-CANCEL-1", CustomerName = "A", CustomerEmail = "a@example.com", CustomerPhone = "1",
            ShippingAddress = "Address", OrderStatus = OrderStatus.Pending, PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.COD, VoucherCode = "LUXE", TotalAmount = 10m
        };
        order.Items.Add(new OrderItem { Product = product, ProductId = product.Id, Variant = variant, ProductName = "Oud", VariantLabel = "100ml", UnitPrice = 10m, Quantity = 2 });
        db.AddRange(product, variant, voucher, order);
        await db.SaveChangesAsync();

        var result = await CreateController(db).UpdateOrderStatus(order.Id, new UpdateStoreOrderStatusRequest { Status = OrderStatus.Cancelled }, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(OrderStatus.Cancelled, order.OrderStatus);
        Assert.Equal(2, variant.StockQuantity);
        Assert.True(product.IsInStock);
        Assert.Equal(0, voucher.UsedCount);
    }

    [Fact]
    public async Task CreatingProduct_CreatesDefaultVariantAndSlug()
    {
        await using var db = CreateDb();
        var brand = new Brand { Name = "Maison", Slug = "maison" };
        var category = new Category { Name = "Nước hoa", Slug = "nuoc-hoa" };
        db.AddRange(brand, category);
        await db.SaveChangesAsync();

        var result = await CreateController(db).CreateProduct(new StoreProductCreateRequest
        {
            Name = "Đêm Trầm",
            Sku = "DEM-TRAM",
            BrandId = brand.Id,
            CategoryId = category.Id,
            BasePrice = 1_250_000m,
            InitialVolumeMl = 50,
            InitialVolumeLabel = "Chai 50ml",
            InitialStockQuantity = 4
        }, default);

        Assert.IsType<CreatedAtActionResult>(result);
        var created = await db.Products.Include(p => p.Variants).SingleAsync(p => p.Sku == "DEM-TRAM");
        Assert.Equal("dem-tram", created.Slug);
        Assert.True(created.IsInStock);
        var variant = Assert.Single(created.Variants);
        Assert.True(variant.IsDefault);
        Assert.Equal(4, variant.StockQuantity);
        Assert.Equal(1_250_000m, variant.SalePrice);
    }

    [Fact]
    public async Task CreatingVoucher_RejectsZeroDiscount()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        var result = await CreateController(db).CreateVoucher(new StoreVoucherRequest
        {
            Code = "ZERO",
            DiscountAmount = 0m,
            UsageLimit = 10,
            ValidFrom = now.AddHours(-1),
            ValidTo = now.AddDays(1),
            IsActive = true
        }, default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Vouchers);
    }
}
