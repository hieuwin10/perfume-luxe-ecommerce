using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly PerfumeDbContext _context;

    public OrderService(PerfumeDbContext context)
    {
        _context = context;
    }

    public async Task<Voucher?> ValidateVoucherAsync(string code, decimal currentSubTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(v => v.Code == code.Trim().ToUpper() && v.IsActive);

        if (voucher == null) return null;
        if (voucher.ValidFrom > DateTime.UtcNow || voucher.ValidTo < DateTime.UtcNow) return null;
        if (voucher.UsedCount >= voucher.UsageLimit) return null;
        if (currentSubTotal < voucher.MinOrderAmount) return null;

        return voucher;
    }

    public async Task<(bool Success, string? Error, Order? Order)> PlaceOrderAsync(CheckoutRequestDto request, string? userId = null)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return (false, "Giỏ hàng của bạn đang trống.", null);
        }

        decimal subTotal = request.Items.Sum(i => i.UnitPrice * i.Quantity);
        decimal discountAmount = 0;

        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var voucher = await ValidateVoucherAsync(request.VoucherCode, subTotal);
            if (voucher != null)
            {
                if (voucher.DiscountPercent.HasValue)
                {
                    discountAmount = subTotal * (voucher.DiscountPercent.Value / 100m);
                    if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
                    {
                        discountAmount = voucher.MaxDiscountAmount.Value;
                    }
                }
                else if (voucher.DiscountAmount.HasValue)
                {
                    discountAmount = voucher.DiscountAmount.Value;
                }

                voucher.UsedCount += 1;
            }
        }

        decimal shippingFee = subTotal >= 2000000 ? 0 : 35000;
        decimal totalAmount = Math.Max(0, subTotal - discountAmount + shippingFee);

        var order = new Order
        {
            OrderCode = $"PF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            UserId = userId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            OrderNotes = request.OrderNotes,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            ShippingFee = shippingFee,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = request.PaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Completed,
            OrderStatus = OrderStatus.Pending,
            VoucherCode = request.VoucherCode,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = item.ProductName,
                VariantLabel = item.VariantLabel,
                ProductImageUrl = item.ProductImageUrl,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return (true, null, order);
    }

    public async Task<Order?> GetOrderByCodeAsync(string orderCode)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
    }
}
