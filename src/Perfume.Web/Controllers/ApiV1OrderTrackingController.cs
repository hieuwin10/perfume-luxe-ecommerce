using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

[ApiController]
public sealed class ApiV1OrderTrackingController : ControllerBase
{
    private readonly PerfumeDbContext _db;

    public ApiV1OrderTrackingController(PerfumeDbContext db) => _db = db;

    // ─── 4. GET /api/v1/orders/track?orderCode={code} ─────────────────────────
    [HttpGet("api/v1/orders/track")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackOrder(
        [FromQuery] string? orderCode,
        [FromQuery] string? phone = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return BadRequest(new { message = "Vui lòng cung cấp mã đơn hàng (orderCode) để tra cứu." });
        }

        var normalizedCode = orderCode.Trim().ToUpperInvariant();
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == normalizedCode, cancellationToken);

        if (order == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng với mã '{orderCode.Trim()}'." });
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            var orderCleanPhone = new string(order.CustomerPhone.Where(char.IsDigit).ToArray());
            bool phoneMatches = orderCleanPhone == cleanPhone ||
                (orderCleanPhone.Length >= 6 && cleanPhone.Length >= 6 &&
                 (orderCleanPhone.EndsWith(cleanPhone) || cleanPhone.EndsWith(orderCleanPhone)));

            if (!phoneMatches)
            {
                return BadRequest(new { message = "Số điện thoại xác thực không khớp với đơn hàng." });
            }
        }

        var timeline = BuildOrderTimeline(order);

        return Ok(new OrderTrackApiResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            OrderStatus = order.OrderStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            CustomerName = order.CustomerName,
            ShippingAddress = order.ShippingAddress,
            City = order.City,
            TrackingNumber = order.TrackingNumber,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ShippingFee = order.ShippingFee,
            IsGiftWrapping = order.IsGiftWrapping,
            GiftWrappingFee = order.GiftWrappingFee,
            GiftMessage = order.GiftMessage,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new CustomerOrderLineDto
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                ProductName = i.ProductName,
                VariantLabel = i.VariantLabel,
                ProductImageUrl = i.ProductImageUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            Timeline = timeline
        });
    }

    private static List<OrderTimelineStepDto> BuildOrderTimeline(Order order)
    {
        var steps = new List<OrderTimelineStepDto>
        {
            new()
            {
                Stage = "Pending",
                Title = "Đặt hàng thành công",
                Completed = true,
                Timestamp = order.CreatedAt
            },
            new()
            {
                Stage = "Confirmed",
                Title = "Đã xác nhận & đóng gói Haute Parfumerie",
                Completed = order.OrderStatus is OrderStatus.Confirmed or OrderStatus.Shipping or OrderStatus.Delivered,
                Timestamp = order.UpdatedAt ?? order.CreatedAt
            },
            new()
            {
                Stage = "Shipping",
                Title = "Đang vận chuyển chuyên biệt",
                Completed = order.OrderStatus is OrderStatus.Shipping or OrderStatus.Delivered,
                Timestamp = order.UpdatedAt
            },
            new()
            {
                Stage = "Delivered",
                Title = "Giao hàng hoàn tất",
                Completed = order.OrderStatus == OrderStatus.Delivered,
                Timestamp = order.UpdatedAt
            }
        };

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            steps.Add(new OrderTimelineStepDto
            {
                Stage = "Cancelled",
                Title = "Đơn hàng đã hủy",
                Completed = true,
                Timestamp = order.UpdatedAt ?? DateTime.UtcNow
            });
        }

        return steps;
    }
}
