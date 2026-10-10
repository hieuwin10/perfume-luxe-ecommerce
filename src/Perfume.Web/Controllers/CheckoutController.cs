using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Services;
using Perfume.Web.Extensions;

namespace Perfume.Web.Controllers;

public class CheckoutController : Controller
{
    private const string CartSessionKey = "PERFUME_CART_SESSION";
    private readonly IOrderService _orderService;
    private readonly IOrderEmailSender _emailSender;
    private readonly IDataProtector _guestOrderProtector;

    public CheckoutController(IOrderService orderService, IOrderEmailSender emailSender, IDataProtectionProvider protectionProvider)
    {
        _orderService = orderService;
        _emailSender = emailSender;
        _guestOrderProtector = protectionProvider.CreateProtector("PerfumeLuxe.GuestOrderTracking.v1");
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new();
        if (cart.Count == 0) return RedirectToAction("Index", "Cart");
        ViewBag.Cart = cart;
        ViewBag.SubTotal = cart.Sum(i => i.TotalPrice);
        return View(new CheckoutRequestDto { PaymentMethod = PaymentMethod.COD });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutRequestDto model)
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new();
        if (cart.Count == 0) return RedirectToAction("Index", "Cart");
        model.Items = cart;
        model.PaymentMethod = PaymentMethod.COD;
        if (!ModelState.IsValid)
        {
            ViewBag.Cart = cart;
            ViewBag.SubTotal = cart.Sum(i => i.TotalPrice);
            ViewBag.Error = "Vui lòng kiểm tra lại thông tin giao hàng.";
            return View("Index", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (success, error, order) = await _orderService.PlaceOrderAsync(model, userId);
        if (!success || order == null)
        {
            ViewBag.Cart = cart;
            ViewBag.SubTotal = cart.Sum(i => i.TotalPrice);
            ViewBag.Error = error ?? "Không thể đặt hàng, vui lòng thử lại.";
            return View("Index", model);
        }

        HttpContext.Session.Remove(CartSessionKey);
        string? token = null;
        if (string.IsNullOrEmpty(order.UserId)) token = _guestOrderProtector.Protect(order.OrderCode);
        var trackingUrl = token == null ? null : Url.Action(nameof(Success), "Checkout", new { token }, Request.Scheme);
        await _emailSender.SendOrderConfirmationAsync(order, trackingUrl);
        return order.UserId == null
            ? RedirectToAction(nameof(Success), new { token })
            : RedirectToAction(nameof(Success), new { orderCode = order.OrderCode });
    }

    [HttpGet]
    public async Task<IActionResult> Success(string? orderCode, string? token)
    {
        string? verifiedCode = orderCode;
        if (!string.IsNullOrWhiteSpace(token))
        {
            try { verifiedCode = _guestOrderProtector.Unprotect(token); }
            catch { return NotFound(); }
        }
        else if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(verifiedCode)) return NotFound();
        var order = await _orderService.GetOrderByCodeAsync(verifiedCode);
        if (order == null) return NotFound();
        if (order.UserId == null && string.IsNullOrWhiteSpace(token)) return NotFound();
        if (order.UserId != null && !string.Equals(order.UserId, User.FindFirstValue(ClaimTypes.NameIdentifier), StringComparison.Ordinal))
            return NotFound();

        ViewBag.IsGuest = order.UserId == null;
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckVoucher([FromBody] VoucherCheckRequest req)
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new();
        var subtotal = cart.Sum(i => i.TotalPrice);
        var voucher = await _orderService.ValidateVoucherAsync(req.Code, subtotal);
        if (voucher == null) return Json(new { valid = false, message = "Mã ưu đãi không hợp lệ hoặc không đủ điều kiện áp dụng." });
        var discount = voucher.DiscountPercent.HasValue
            ? subtotal * voucher.DiscountPercent.Value / 100m
            : voucher.DiscountAmount ?? 0m;
        if (voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        return Json(new { valid = true, discount, message = $"Áp dụng thành công mã {voucher.Code}: Giảm {discount:N0}₫!" });
    }
}

public class VoucherCheckRequest { public string Code { get; set; } = string.Empty; }
