using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Perfume.Web.Security;
using Perfume.Web.Services;

namespace Perfume.Web.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CustomerOrdersApiController : ControllerBase
{
    private const string GuestCartCookie = "plx_guest_cart";
    private readonly PerfumeDbContext _db;
    private readonly ICustomerCartService _carts;
    private readonly IOrderService _orders;
    private readonly IOrderEmailSender _email;
    private readonly IAntiforgery _antiforgery;
    private readonly IDataProtector _guestOrderProtector;
    private readonly ILogger<CustomerOrdersApiController>? _logger;

    public CustomerOrdersApiController(PerfumeDbContext db, ICustomerCartService carts, IOrderService orders,
        IOrderEmailSender email, IAntiforgery antiforgery, IDataProtectionProvider protectionProvider,
        ILogger<CustomerOrdersApiController>? logger = null)
    {
        _db = db;
        _carts = carts;
        _orders = orders;
        _email = email;
        _antiforgery = antiforgery;
        _guestOrderProtector = protectionProvider.CreateProtector("PerfumeLuxe.GuestOrderTracking.v1");
        _logger = logger;
    }

    [HttpGet("customer/orders"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return Ok(orders.Select(ToOrderDto));
    }

    [HttpGet("customer/orders/{id:int}"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> MyOrder(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);
        return order == null ? NotFound() : Ok(ToOrderDto(order));
    }

    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder(CustomerOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var userId = CurrentUserId();
        var guestToken = Request.Cookies[GuestCartCookie];
        var cart = await _carts.GetAsync(userId, guestToken, cancellationToken);
        if (cart.Items.Count == 0) return BadRequest(new { message = "Giỏ hàng của bạn đang trống." });
        if (cart.Items.Any(i => !i.IsAvailable)) return BadRequest(new { message = "Có sản phẩm đã hết hàng hoặc thay đổi tồn kho. Vui lòng cập nhật giỏ hàng." });

        var checkout = new CheckoutRequestDto
        {
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            ShippingAddress = request.ShippingAddress.Trim(),
            City = request.City?.Trim(),
            OrderNotes = request.OrderNotes?.Trim(),
            VoucherCode = request.VoucherCode?.Trim(),
            PaymentMethod = request.PaymentMethod,
            IsGiftWrapping = request.IsGiftWrapping,
            GiftMessage = request.GiftMessage?.Trim(),
            Items = cart.Items.Select(i => new CartItemDto { ProductId = i.ProductId, VariantId = i.VariantId, Quantity = i.Quantity }).ToList()
        };
        var (success, error, order) = await _orders.PlaceOrderAsync(checkout, userId);
        if (!success || order == null) return BadRequest(new { message = error ?? "Không thể hoàn tất đơn hàng." });

        await _carts.ClearAsync(userId, guestToken, cancellationToken);
        string? trackingToken = null;
        string? trackingUrl = null;
        if (order.UserId == null)
        {
            trackingToken = _guestOrderProtector.Protect(order.OrderCode);
            trackingUrl = Url?.Action("Success", "Checkout", new { token = trackingToken }, Request.Scheme)
                ?? $"/checkout/success?token={trackingToken}";
        }
        try
        {
            await _email.SendOrderConfirmationAsync(order, trackingUrl);
        }
        catch
        {
            _logger?.LogWarning("Order {OrderCode} was saved, but its confirmation email could not be sent.", order.OrderCode);
        }
        var targetAction = order.PaymentMethod == PaymentMethod.BankTransfer ? "VietQR" : "Success";
        var response = new GuestOrderResponse
        {
            Order = ToOrderDto(order),
            TrackingToken = trackingToken,
            SuccessUrl = order.UserId == null
                ? (Url?.Action(targetAction, "Checkout", new { token = trackingToken }, Request.Scheme) ?? $"/checkout/{targetAction.ToLowerInvariant()}?token={trackingToken}")
                : (Url?.Action(targetAction, "Checkout", new { orderCode = order.OrderCode }, Request.Scheme) ?? $"/checkout/{targetAction.ToLowerInvariant()}?orderCode={order.OrderCode}")
        };
        return Ok(response);
    }

    [HttpGet("orders/guest")]
    public async Task<IActionResult> TrackGuestOrder([FromQuery] string token)
    {
        string code;
        try { code = _guestOrderProtector.Unprotect(token); }
        catch { return NotFound(); }
        var order = await _orders.GetOrderByCodeAsync(code);
        return order == null || order.UserId != null ? NotFound() : Ok(ToOrderDto(order));
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<bool> ValidateCookieWriteAsync()
    {
        if (Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return true;
        try { await _antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static CustomerOrderDto ToOrderDto(Order order) => new()
    {
        Id = order.Id,
        OrderCode = order.OrderCode,
        CreatedAt = order.CreatedAt,
        OrderStatus = order.OrderStatus.ToString(),
        PaymentStatus = order.PaymentStatus.ToString(),
        PaymentMethod = order.PaymentMethod.ToString(),
        SubTotal = order.SubTotal,
        DiscountAmount = order.DiscountAmount,
        ShippingFee = order.ShippingFee,
        TotalAmount = order.TotalAmount,
        IsGiftWrapping = order.IsGiftWrapping,
        GiftWrappingFee = order.GiftWrappingFee,
        GiftMessage = order.GiftMessage,
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        CustomerEmail = order.CustomerEmail,
        ShippingAddress = order.ShippingAddress,
        City = order.City,
        TrackingNumber = order.TrackingNumber,
        Items = order.Items.Select(i => new CustomerOrderLineDto
        {
            ProductId = i.ProductId,
            VariantId = i.VariantId,
            ProductName = i.ProductName,
            VariantLabel = i.VariantLabel,
            ProductImageUrl = i.ProductImageUrl,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
