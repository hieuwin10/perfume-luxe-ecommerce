using System.Data;
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
    public OrderService(PerfumeDbContext context) => _context = context;

    public async Task<Voucher?> ValidateVoucherAsync(string code, decimal currentSubTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var now = DateTime.UtcNow;
        var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code.Trim().ToUpper() && v.IsActive);
        if (voucher == null || voucher.ValidFrom > now || voucher.ValidTo < now ||
            voucher.UsedCount >= voucher.UsageLimit || currentSubTotal < voucher.MinOrderAmount) return null;
        return voucher;
    }

    public async Task<(bool Success, string? Error, Order? Order)> PlaceOrderAsync(CheckoutRequestDto request, string? userId = null)
    {
        if (request.PaymentMethod != PaymentMethod.COD && request.PaymentMethod != PaymentMethod.BankTransfer)
            return (false, "Phương thức thanh toán chưa được hỗ trợ.", null);
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerPhone) ||
            string.IsNullOrWhiteSpace(request.CustomerEmail) || string.IsNullOrWhiteSpace(request.ShippingAddress))
            return (false, "Vui lòng nhập đầy đủ thông tin giao hàng.", null);
        if (request.Items == null || request.Items.Count == 0) return (false, "Giỏ hàng của bạn đang trống.", null);

        var lines = request.Items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => new { i.ProductId, i.VariantId })
            .Select(g => new { g.Key.ProductId, g.Key.VariantId, Quantity = g.Sum(i => i.Quantity) })
            .ToList();
        if (lines.Count == 0 || lines.Any(i => i.Quantity > 50))
            return (false, "Số lượng mỗi phiên bản không được vượt quá 50.", null);

        var isRelational = _context.Database.IsRelational();
        await using var transaction = isRelational
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        try
        {
            var now = DateTime.UtcNow;
            var order = new Order
            {
                OrderCode = $"LUXE-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
                UserId = userId,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                CustomerEmail = request.CustomerEmail.Trim(),
                ShippingAddress = request.ShippingAddress.Trim(),
                City = request.City?.Trim(),
                OrderNotes = request.OrderNotes?.Trim(),
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                OrderStatus = OrderStatus.Pending,
                IsGiftWrapping = request.IsGiftWrapping,
                GiftWrappingFee = request.IsGiftWrapping ? 50_000 : 0,
                GiftMessage = request.GiftMessage?.Trim(),
                CreatedAt = now
            };

            foreach (var line in lines)
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == line.ProductId && p.IsInStock);
                if (product == null) return (false, "Một sản phẩm trong giỏ đã ngừng bán.", null);

                ProductVariant? variant = null;
                decimal unitPrice;
                string variantLabel;
                if (product.Variants.Count > 0)
                {
                    variant = line.VariantId.HasValue
                        ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId.Value)
                        : product.Variants.FirstOrDefault(v => v.IsDefault) ?? product.Variants.FirstOrDefault();
                    if (variant == null) return (false, "Phiên bản sản phẩm không hợp lệ.", null);
                    if (isRelational)
                    {
                        var reserved = await _context.ProductVariants
                            .Where(v => v.Id == variant.Id && v.StockQuantity >= line.Quantity)
                            .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - line.Quantity));
                        if (reserved != 1) return (false, $"Không đủ tồn kho cho {product.Name} - {variant.VolumeLabel}.", null);
                    }
                    else
                    {
                        if (variant.StockQuantity < line.Quantity)
                            return (false, $"Không đủ tồn kho cho {product.Name} - {variant.VolumeLabel}.", null);
                        variant.StockQuantity -= line.Quantity;
                    }
                    unitPrice = variant.SalePrice > 0 ? variant.SalePrice : variant.OriginalPrice;
                    if (unitPrice <= 0) unitPrice = product.BasePrice;
                    variantLabel = variant.VolumeLabel;
                }
                else
                {
                    if (!product.IsInStock) return (false, $"Sản phẩm {product.Name} đã hết hàng.", null);
                    unitPrice = product.BasePrice;
                    variantLabel = "Fullbox";
                }

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    VariantId = variant?.Id,
                    ProductName = product.Name,
                    VariantLabel = variantLabel,
                    ProductImageUrl = product.MainImageUrl,
                    UnitPrice = unitPrice,
                    Quantity = line.Quantity
                });
            }

            order.SubTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            if (!string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                var voucher = await ValidateVoucherAsync(request.VoucherCode, order.SubTotal);
                if (voucher == null) return (false, "Mã ưu đãi không hợp lệ, đã hết lượt hoặc không đủ điều kiện.", null);
                if (isRelational)
                {
                    var used = await _context.Vouchers
                        .Where(v => v.Id == voucher.Id && v.UsedCount < v.UsageLimit && v.IsActive && v.ValidFrom <= now && v.ValidTo >= now)
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.UsedCount, v => v.UsedCount + 1));
                    if (used != 1) return (false, "Mã ưu đãi vừa hết lượt sử dụng. Vui lòng thử mã khác.", null);
                }
                else
                {
                    if (voucher.UsedCount >= voucher.UsageLimit || !voucher.IsActive || voucher.ValidFrom > now || voucher.ValidTo < now)
                        return (false, "Mã ưu đãi vừa hết lượt sử dụng. Vui lòng thử mã khác.", null);
                    voucher.UsedCount += 1;
                }

                var discount = voucher.DiscountPercent.HasValue
                    ? order.SubTotal * voucher.DiscountPercent.Value / 100m
                    : voucher.DiscountAmount ?? 0m;
                if (voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
                order.DiscountAmount = Math.Min(order.SubTotal, discount);
                order.VoucherCode = voucher.Code;
            }

            order.ShippingFee = order.SubTotal >= 2_000_000 ? 0 : 35_000;
            order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount + order.ShippingFee + order.GiftWrappingFee);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            if (transaction != null) await transaction.CommitAsync();
            return (true, null, order);
        }
        catch
        {
            if (transaction != null) await transaction.RollbackAsync();
            return (false, "Không thể hoàn tất đơn hàng. Tồn kho có thể vừa thay đổi; vui lòng kiểm tra giỏ hàng và thử lại.", null);
        }
    }

    public async Task<Order?> GetOrderByCodeAsync(string orderCode) =>
        await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderCode == orderCode);

    public async Task<bool> ConfirmPaymentAsync(string orderCode)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        if (order == null) return false;
        order.PaymentStatus = PaymentStatus.Completed;
        await _context.SaveChangesAsync();
        return true;
    }
}
