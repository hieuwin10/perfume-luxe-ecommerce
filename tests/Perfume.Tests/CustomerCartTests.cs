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

public sealed class CustomerCartTests : TestFixtures
{
    [Fact]
    public async Task TC_T1_052_CustomerCart_AddAsync_AuthenticatedUser_CreatesUserCartAndCalculatesSubTotal()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Santal 33", Slug = "santal-33", Sku = "S33", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_200_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("user-001", null, new CartAddItemRequest { ProductSlug = "santal-33", Quantity = 2 });

        Assert.Single(state.Items);
        Assert.Equal(2, state.TotalCount);
        Assert.Equal(14_400_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_053_CustomerCart_AddAsync_ExplicitVariantId_UsesSalePriceWhenPositive()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "BR540", Slug = "br540", Sku = "BR540", BrandId = b.Id, CategoryId = c.Id, BasePrice = 9_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 70, VolumeLabel = "70ml EDP", OriginalPrice = 9_000_000m, SalePrice = 8_200_000m, StockQuantity = 10, IsDefault = true });
        p.Variants.Add(new ProductVariant { Id = 11, ProductId = 1, VolumeMl = 200, VolumeLabel = "200ml EDP", OriginalPrice = 15_000_000m, SalePrice = 0m, StockQuantity = 5, IsDefault = false });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "br540", VariantId = 10, Quantity = 1 });
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "br540", VariantId = 11, Quantity = 1 });

        Assert.Equal(2, state.Items.Count);
        Assert.Equal(8_200_000m, state.Items.First(i => i.VariantId == 10).UnitPrice);
        Assert.Equal(15_000_000m, state.Items.First(i => i.VariantId == 11).UnitPrice);
        Assert.Equal(23_200_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_054_CustomerCart_AddAsync_WithoutVariantId_SelectsDefaultVariantAutomatically()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Layton", Slug = "layton", Sku = "LAY", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 750_000m, StockQuantity = 10, IsDefault = false });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 125, VolumeLabel = "125ml Fullbox", OriginalPrice = 6_000_000m, StockQuantity = 10, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "layton", VariantId = null, Quantity = 1 });

        Assert.Single(state.Items);
        Assert.Equal(2, state.Items[0].VariantId);
        Assert.Equal("125ml Fullbox", state.Items[0].VariantLabel);
    }

    [Fact]
    public async Task TC_T1_055_CustomerCart_AddAsync_ProductWithoutVariants_UsesBasePriceAndFullboxLabel()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Pure Oil", Slug = "pure-oil", Sku = "PO", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "pure-oil", Quantity = 3 });

        Assert.Single(state.Items);
        Assert.Null(state.Items[0].VariantId);
        Assert.Equal("Fullbox", state.Items[0].VariantLabel);
        Assert.Equal(4_500_000m, state.Items[0].UnitPrice);
        Assert.Equal(50, state.Items[0].StockQuantity);
    }

    [Fact]
    public async Task TC_T1_056_CustomerCart_AddAsync_ExistingItem_IncrementsQuantityWithoutDuplicatingLine()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 5, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, StockQuantity = 10, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 5, Quantity = 2 });
        var state = await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 5, Quantity = 3 });

        Assert.Single(state.Items);
        Assert.Equal(5, state.Items[0].Quantity);
        Assert.Equal(42_500_000m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_057_CustomerCart_AddAsync_QuantityOutOfBoundsOrExceedingStock_ThrowsInvalidOperationException()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Rare Oud", Slug = "rare-oud", Sku = "RO", BrandId = b.Id, CategoryId = c.Id, BasePrice = 12_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 50, VolumeLabel = "50ml", OriginalPrice = 12_000_000m, StockQuantity = 4, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", Quantity = 0 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", Quantity = 51 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "rare-oud", VariantId = 1, Quantity = 5 }));
    }

    [Fact]
    public async Task TC_T1_058_CustomerCart_AddAsync_OutOfStockProductOrZeroStockVariant_RejectsAddition()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var pOut = new Product { Id = 1, Name = "Out Product", Slug = "out-product", Sku = "OP", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = false };
        var pZeroVar = new Product { Id = 2, Name = "Zero Variant", Slug = "zero-var", Sku = "ZV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        pZeroVar.Variants.Add(new ProductVariant { Id = 10, ProductId = 2, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 5_000_000m, StockQuantity = 0, IsDefault = true });
        db.Products.AddRange(pOut, pZeroVar);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "out-product", Quantity = 1 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "zero-var", VariantId = 10, Quantity = 1 }));
    }

    [Fact]
    public async Task TC_T1_059_CustomerCart_AddAsync_InvalidVariantId_ThrowsKeyNotFoundException()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var pNoVar = new Product { Id = 1, Name = "NoVar", Slug = "no-var", Sku = "NV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        var pWithVar = new Product { Id = 2, Name = "WithVar", Slug = "with-var", Sku = "WV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true };
        pWithVar.Variants.Add(new ProductVariant { Id = 5, ProductId = 2, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 5_000_000m, StockQuantity = 5, IsDefault = true });
        db.Products.AddRange(pNoVar, pWithVar);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "no-var", VariantId = 99, Quantity = 1 }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "with-var", VariantId = 999, Quantity = 1 }));
    }

    [Fact]
    public async Task TC_T1_060_CustomerCart_SetQuantityAsync_UpdatesQuantityAndValidatesStockLimit()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Oud Wood", Slug = "oud-wood", Sku = "OW", BrandId = b.Id, CategoryId = c.Id, BasePrice = 7_000_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 7_000_000m, StockQuantity = 6, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "oud-wood", VariantId = 10, Quantity = 1 });

        var updated = await cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = 10, Quantity = 4 });
        Assert.Equal(4, updated.TotalCount);
        Assert.Equal(28_000_000m, updated.SubTotal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = 10, Quantity = 7 }));
    }

    [Fact]
    public async Task TC_T1_061_CustomerCart_SetQuantityAsync_ZeroQuantity_RemovesLineItem()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Tam Dao", Slug = "tam-dao", Sku = "TD", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_200_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "tam-dao", Quantity = 2 });

        var afterZero = await cartService.SetQuantityAsync("u1", null, new CartLineRequest { ProductId = 1, VariantId = null, Quantity = 0 });
        Assert.Empty(afterZero.Items);
        Assert.Equal(0m, afterZero.SubTotal);
    }

    [Fact]
    public async Task TC_T1_062_CustomerCart_RemoveAsync_RemovesTargetVariantLineOnly()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 900_000m, StockQuantity = 10 });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, StockQuantity = 10 });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 1, Quantity = 1 });
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "aventus", VariantId = 2, Quantity = 1 });

        var afterRemove = await cartService.RemoveAsync("u1", null, 1, 1);
        Assert.Single(afterRemove.Items);
        Assert.Equal(2, afterRemove.Items[0].VariantId);
    }

    [Fact]
    public async Task TC_T1_063_CustomerCart_MergeGuestCartAsync_CombinesQuantitiesClampsToStockAndClearsGuest()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Grand Soir", Slug = "grand-soir", Sku = "GS", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 10, ProductId = 1, VolumeMl = 70, VolumeLabel = "70ml", OriginalPrice = 5_500_000m, StockQuantity = 5, IsDefault = true });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync(null, "guest-tok-99", new CartAddItemRequest { ProductSlug = "grand-soir", VariantId = 10, Quantity = 3 });
        await cartService.AddAsync("user-login-1", null, new CartAddItemRequest { ProductSlug = "grand-soir", VariantId = 10, Quantity = 4 });

        var merged = await cartService.MergeGuestCartAsync("user-login-1", "guest-tok-99");
        var guestAfter = await cartService.GetAsync(null, "guest-tok-99");

        Assert.Single(merged.Items);
        // 3 + 4 = 7, clamped to StockQuantity = 5
        Assert.Equal(5, merged.Items[0].Quantity);
        Assert.Empty(guestAfter.Items);
    }

    [Fact]
    public async Task TC_T1_064_CustomerCart_ClearAsync_EmptiesAllItems()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Naxos", Slug = "naxos", Sku = "NX", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_800_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var cartService = new CustomerCartService(db);
        await cartService.AddAsync("u1", null, new CartAddItemRequest { ProductSlug = "naxos", Quantity = 2 });
        await cartService.ClearAsync("u1", null);

        var state = await cartService.GetAsync("u1", null);
        Assert.Empty(state.Items);
        Assert.Equal(0, state.TotalCount);
        Assert.Equal(0m, state.SubTotal);
    }

    [Fact]
    public async Task TC_T1_065_OrderService_PlaceOrderAsync_CalculatesSubTotalAcrossMultipleVariants()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Creed Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true };
        p.Variants.Add(new ProductVariant { Id = 1, ProductId = 1, VolumeMl = 10, VolumeLabel = "10ml", OriginalPrice = 950_000m, SalePrice = 850_000m, StockQuantity = 10 });
        p.Variants.Add(new ProductVariant { Id = 2, ProductId = 1, VolumeMl = 100, VolumeLabel = "100ml", OriginalPrice = 8_500_000m, SalePrice = 0m, StockQuantity = 10 });
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, error, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Nguyễn Văn A",
            CustomerPhone = "0901234567",
            CustomerEmail = "a@luxe.vn",
            ShippingAddress = "88 Tràng Tiền, Hoàn Kiếm, Hà Nội",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, VariantId = 1, Quantity = 2 }, // 2 * 850,000 = 1,700,000
                new() { ProductId = 1, VariantId = 2, Quantity = 1 }  // 1 * 8,500,000 = 8,500,000
            }
        }, "user-1");

        Assert.True(success, error);
        Assert.NotNull(order);
        Assert.Equal(10_200_000m, order.SubTotal);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public async Task TC_T1_066_OrderService_ShippingFee_Applies35000WhenSubTotalUnderThreshold()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Discovery Vial", Slug = "vial", Sku = "VL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 650_000m, IsInStock = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Trần Thị B",
            CustomerPhone = "0912345678",
            CustomerEmail = "b@luxe.vn",
            ShippingAddress = "12 Nguyễn Huệ, Quận 1, TP.HCM",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(650_000m, order.SubTotal);
        Assert.Equal(35_000m, order.ShippingFee);
        Assert.Equal(685_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_067_OrderService_ShippingFee_FreeShippingWhenSubTotalMeetsThreshold()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        var p = new Product { Id = 1, Name = "Luxury Bottle", Slug = "lux", Sku = "LX", BrandId = b.Id, CategoryId = c.Id, BasePrice = 3_500_000m, IsInStock = true };
        db.Products.Add(p);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Lê Hoàng C",
            CustomerPhone = "0923456789",
            CustomerEmail = "c@luxe.vn",
            ShippingAddress = "72 Lê Thánh Tôn, Quận 1",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(0m, order.ShippingFee);
        Assert.Equal(3_500_000m, order.TotalAmount);
    }


}
