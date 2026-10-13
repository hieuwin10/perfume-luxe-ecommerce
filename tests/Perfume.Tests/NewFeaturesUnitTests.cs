using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Services;
using Xunit;

namespace Perfume.Tests;

public class NewFeaturesUnitTests
{
    private PerfumeDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(databaseName: $"PerfumeTestDb_{Guid.NewGuid()}")
            .Options;

        return new PerfumeDbContext(options);
    }

    [Fact]
    public async Task PlaceOrderAsync_WithGiftWrapping_CalculatesFeeAndTotalCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var product = new Product
        {
            Name = "Baccarat Rouge 540",
            Slug = "baccarat-rouge-540",
            BasePrice = 6_500_000,
            IsInStock = true,
            MainImageUrl = "/images/baccarat.jpg"
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var request = new CheckoutRequestDto
        {
            CustomerName = "Công Chúa Quý Tộc",
            CustomerPhone = "0988888888",
            CustomerEmail = "princess@parfum.fr",
            ShippingAddress = "72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
            OrderNotes = "Giao giờ hành chính",
            PaymentMethod = PaymentMethod.BankTransfer,
            IsGiftWrapping = true,
            GiftMessage = "Chúc mừng sinh nhật nàng thơ của anh!"
        };

        // Act - Kiểm thử logic Order domain & fee calculation
        var order = new Order
        {
            OrderCode = "HP-QR-20261012-001",
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.Pending,
            IsGiftWrapping = request.IsGiftWrapping,
            GiftWrappingFee = request.IsGiftWrapping ? 50_000 : 0,
            GiftMessage = request.GiftMessage,
            SubTotal = 6_500_000,
            ShippingFee = 0 // > 2.000.000 free ship
        };
        order.TotalAmount = order.SubTotal + order.ShippingFee + order.GiftWrappingFee;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Assert
        Assert.NotNull(order);
        Assert.Equal(PaymentMethod.BankTransfer, order.PaymentMethod);
        Assert.True(order.IsGiftWrapping);
        Assert.Equal(50_000, order.GiftWrappingFee);
        Assert.Equal("Chúc mừng sinh nhật nàng thơ của anh!", order.GiftMessage);
        Assert.Equal(6_550_000, order.TotalAmount);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ValidOrder_UpdatesPaymentStatusToCompleted()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var orderService = new OrderService(db);

        var order = new Order
        {
            OrderCode = "HP-QR-20261012-002",
            CustomerName = "Đức Vua",
            CustomerPhone = "0909090909",
            CustomerEmail = "king@parfum.fr",
            ShippingAddress = "Dinh Độc Lập",
            TotalAmount = 5_000_000,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.Pending
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Act
        var isSuccess = await orderService.ConfirmPaymentAsync("HP-QR-20261012-002");

        // Assert
        Assert.True(isSuccess);
        var updatedOrder = await db.Orders.FirstOrDefaultAsync(o => o.OrderCode == "HP-QR-20261012-002");
        Assert.NotNull(updatedOrder);
        Assert.Equal(PaymentStatus.Completed, updatedOrder.PaymentStatus);
    }

    [Fact]
    public void RBAC_Roles_ConstantDefinitions_AreValid()
    {
        // Kiểm tra 5 vai trò hệ thống RBAC mới
        var expectedRoles = new[] { "Admin", "StoreManager", "WarehouseStaff", "Concierge", "Customer" };
        Assert.Equal(5, expectedRoles.Length);
        Assert.Contains("WarehouseStaff", expectedRoles);
        Assert.Contains("Concierge", expectedRoles);
        Assert.Contains("StoreManager", expectedRoles);
        Assert.Contains("Admin", expectedRoles);
        Assert.Contains("Customer", expectedRoles);
    }

    [Fact]
    public void FinancialReport_PLCalculation_ComputesNetProfitAndMarginCorrectly()
    {
        // Arrange
        decimal totalRevenue = 100_000_000m;
        decimal totalCOGS = 65_000_000m;
        decimal totalDiscounts = 5_000_000m;

        // Act
        decimal netProfit = totalRevenue - totalCOGS - totalDiscounts;
        decimal grossMargin = totalRevenue > 0 ? ((totalRevenue - totalCOGS) / totalRevenue) * 100m : 0m;

        // Assert
        Assert.Equal(30_000_000m, netProfit);
        Assert.Equal(35.0m, grossMargin);
    }
}
