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

public sealed class OrderPricingAndVoucherTests : TestFixtures
{
    [Fact]
    public async Task TC_T1_068_OrderService_PercentageVoucher_CalculatesDiscountAndIncrementsUsedCount()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Santal 33", Slug = "santal-33", Sku = "S33", BrandId = b.Id, CategoryId = c.Id, BasePrice = 5_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "LUXE10",
            DiscountPercent = 10m,
            MinOrderAmount = 1_000_000m,
            UsageLimit = 50,
            UsedCount = 5,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách VIP",
            CustomerPhone = "0988888888",
            CustomerEmail = "vip@luxe.vn",
            ShippingAddress = "Hà Nội",
            VoucherCode = "LUXE10",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal(500_000m, order.DiscountAmount);
        Assert.Equal(4_500_000m, order.TotalAmount);
        Assert.Equal(6, (await db.Vouchers.FindAsync(1))!.UsedCount);
    }

    [Fact]
    public async Task TC_T1_069_OrderService_FixedAmountVoucher_AppliesExactFixedDiscount()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Tam Dao", Slug = "tam-dao", Sku = "TD", BrandId = b.Id, CategoryId = c.Id, BasePrice = 4_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "GIFT300K",
            DiscountAmount = 300_000m,
            MinOrderAmount = 2_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách Hàng",
            CustomerPhone = "0977777777",
            CustomerEmail = "kh@luxe.vn",
            ShippingAddress = "Đà Nẵng",
            VoucherCode = "GIFT300K",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.Equal(300_000m, order!.DiscountAmount);
        Assert.Equal(3_700_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_070_OrderService_VoucherWithMaxDiscountAmount_CapsDiscountAtMaxLimit()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Roja Amber Aoud", Slug = "roja-aa", Sku = "RAA", BrandId = b.Id, CategoryId = c.Id, BasePrice = 10_000_000m, IsInStock = true });
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "VIP20CAP500",
            DiscountPercent = 20m, // 20% of 10M = 2,000,000
            MaxDiscountAmount = 500_000m, // capped at 500,000
            MinOrderAmount = 2_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Hoàng Gia",
            CustomerPhone = "0966666666",
            CustomerEmail = "royal@luxe.vn",
            ShippingAddress = "TP.HCM",
            VoucherCode = "VIP20CAP500",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.Equal(500_000m, order!.DiscountAmount);
        Assert.Equal(9_500_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_071_OrderService_ValidateVoucherAsync_RejectsBelowMinOrderAmount()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "MIN5M",
            DiscountAmount = 500_000m,
            MinOrderAmount = 5_000_000m,
            UsageLimit = 10,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("MIN5M", 4_999_999m));
        Assert.NotNull(await orderService.ValidateVoucherAsync("MIN5M", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_072_OrderService_ValidateVoucherAsync_RejectsExpiredAndFutureVouchers()
    {
        using var db = CreateDb();
        db.Vouchers.AddRange(
            new Voucher { Id = 1, Code = "EXPIRED", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-10), ValidTo = DateTime.UtcNow.AddMinutes(-5), IsActive = true },
            new Voucher { Id = 2, Code = "FUTURE", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddHours(2), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = true }
        );
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("EXPIRED", 5_000_000m));
        Assert.Null(await orderService.ValidateVoucherAsync("FUTURE", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_073_OrderService_ValidateVoucherAsync_RejectsInactiveAndSoftDeletedVouchers()
    {
        using var db = CreateDb();
        db.Vouchers.AddRange(
            new Voucher { Id = 1, Code = "INACTIVE", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = false },
            new Voucher { Id = 2, Code = "DELETED", DiscountPercent = 10m, ValidFrom = DateTime.UtcNow.AddDays(-1), ValidTo = DateTime.UtcNow.AddDays(5), IsActive = true, IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("INACTIVE", 5_000_000m));
        Assert.Null(await orderService.ValidateVoucherAsync("DELETED", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_074_OrderService_ValidateVoucherAsync_RejectsWhenUsageLimitExhausted()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "SOLDOUTVOUCHER",
            DiscountPercent = 15m,
            UsageLimit = 20,
            UsedCount = 20,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        Assert.Null(await orderService.ValidateVoucherAsync("SOLDOUTVOUCHER", 5_000_000m));
    }

    [Fact]
    public async Task TC_T1_075_OrderService_ValidateVoucherAsync_CaseInsensitiveAndTrimsWhitespace()
    {
        using var db = CreateDb();
        db.Vouchers.Add(new Voucher
        {
            Id = 1,
            Code = "LUXE2026",
            DiscountPercent = 10m,
            UsageLimit = 100,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var v = await orderService.ValidateVoucherAsync("   luxe2026  ", 3_000_000m);

        Assert.NotNull(v);
        Assert.Equal("LUXE2026", v.Code);
        Assert.Null(await orderService.ValidateVoucherAsync("   ", 3_000_000m));
    }

    [Fact]
    public async Task TC_T1_076_OrderService_RoyalGiftWrappingTrue_Adds50000FeeAndPersistsGiftMessage()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Delina", Slug = "delina", Sku = "DEL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Quý Cô Thanh Lịch",
            CustomerPhone = "0933333333",
            CustomerEmail = "lady@luxe.vn",
            ShippingAddress = "88 Đồng Khởi, Quận 1",
            PaymentMethod = PaymentMethod.BankTransfer,
            IsGiftWrapping = true,
            GiftMessage = "  Chúc mừng kỷ niệm tình yêu!  ",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.NotNull(order);
        Assert.True(order.IsGiftWrapping);
        Assert.Equal(50_000m, order.GiftWrappingFee);
        Assert.Equal("Chúc mừng kỷ niệm tình yêu!", order.GiftMessage);
        Assert.Equal(6_550_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_077_OrderService_RoyalGiftWrappingFalse_AddsZeroFee()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Delina", Slug = "delina", Sku = "DEL", BrandId = b.Id, CategoryId = c.Id, BasePrice = 6_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (success, _, order) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Khách Hàng",
            CustomerPhone = "0933333333",
            CustomerEmail = "lady@luxe.vn",
            ShippingAddress = "88 Đồng Khởi, Quận 1",
            PaymentMethod = PaymentMethod.COD,
            IsGiftWrapping = false,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(success);
        Assert.False(order!.IsGiftWrapping);
        Assert.Equal(0m, order.GiftWrappingFee);
        Assert.Equal(6_500_000m, order.TotalAmount);
    }

    [Fact]
    public async Task TC_T1_078_OrderService_PaymentMethods_CODAndBankTransfer_GenerateUniqueOrderCodes()
    {
        using var db = CreateDb();
        var (b, c, f) = SeedCoreTaxonomy(db);
        db.Products.Add(new Product { Id = 1, Name = "Aventus", Slug = "aventus", Sku = "AV", BrandId = b.Id, CategoryId = c.Id, BasePrice = 8_500_000m, IsInStock = true });
        await db.SaveChangesAsync();

        var orderService = new OrderService(db);
        var (s1, _, o1) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "User 1", CustomerPhone = "0901111111", CustomerEmail = "u1@luxe.vn", ShippingAddress = "Address 1",
            PaymentMethod = PaymentMethod.COD,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var (s2, _, o2) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "User 2", CustomerPhone = "0902222222", CustomerEmail = "u2@luxe.vn", ShippingAddress = "Address 2",
            PaymentMethod = PaymentMethod.BankTransfer,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.True(s1);
        Assert.True(s2);
        Assert.StartsWith("LUXE-", o1!.OrderCode);
        Assert.StartsWith("LUXE-", o2!.OrderCode);
        Assert.NotEqual(o1.OrderCode, o2.OrderCode);
        Assert.Equal(PaymentMethod.COD, o1.PaymentMethod);
        Assert.Equal(PaymentMethod.BankTransfer, o2.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, o2.PaymentStatus);
        Assert.Equal(OrderStatus.Pending, o2.OrderStatus);
    }

    [Fact]
    public async Task TC_T1_079_OrderService_UnsupportedPaymentMethods_VNPAYAndMoMo_ReturnControlledError()
    {
        using var db = CreateDb();
        var orderService = new OrderService(db);

        var (sVnpay, errVnpay, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Test", CustomerPhone = "0900000000", CustomerEmail = "t@luxe.vn", ShippingAddress = "HN",
            PaymentMethod = PaymentMethod.VNPAY,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var (sMomo, errMomo, _) = await orderService.PlaceOrderAsync(new CheckoutRequestDto
        {
            CustomerName = "Test", CustomerPhone = "0900000000", CustomerEmail = "t@luxe.vn", ShippingAddress = "HN",
            PaymentMethod = PaymentMethod.MoMo,
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        Assert.False(sVnpay);
        Assert.Contains("chưa được hỗ trợ", errVnpay);
        Assert.False(sMomo);
        Assert.Contains("chưa được hỗ trợ", errMomo);
    }


}
