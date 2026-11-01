using Perfume.Application.DTOs;
using Perfume.Domain.Entities;

namespace Perfume.Application.Interfaces;

public interface IOrderService
{
    Task<(bool Success, string? Error, Order? Order)> PlaceOrderAsync(CheckoutRequestDto request, string? userId = null);
    Task<Voucher?> ValidateVoucherAsync(string code, decimal currentSubTotal);
    Task<Order?> GetOrderByCodeAsync(string orderCode);
    Task<bool> ConfirmPaymentAsync(string orderCode);
}
