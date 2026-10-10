using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Web.Extensions;

namespace Perfume.Web.Controllers;

public class CheckoutController : Controller
{
    private const string CartSessionKey = "PERFUME_CART_SESSION";
    private readonly IOrderService _orderService;

    public CheckoutController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        ViewBag.Cart = cart;
        ViewBag.SubTotal = cart.Sum(i => i.TotalPrice);
        return View(new CheckoutRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutRequestDto model)
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        model.Items = cart;
        var (success, error, order) = await _orderService.PlaceOrderAsync(model);

        if (!success || order == null)
        {
            ViewBag.Cart = cart;
            ViewBag.SubTotal = cart.Sum(i => i.TotalPrice);
            ViewBag.Error = error ?? "Không thể đặt hàng, vui lòng thử lại.";
            return View("Index", model);
        }

        // Clear Cart
        HttpContext.Session.Remove(CartSessionKey);

        return RedirectToAction("Success", new { orderCode = order.OrderCode });
    }

    [HttpGet]
    public async Task<IActionResult> Success(string orderCode)
    {
        var order = await _orderService.GetOrderByCodeAsync(orderCode);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> CheckVoucher([FromBody] VoucherCheckRequest req)
    {
        var cart = HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
        decimal subTotal = cart.Sum(i => i.TotalPrice);

        var voucher = await _orderService.ValidateVoucherAsync(req.Code, subTotal);
        if (voucher == null)
        {
            return Json(new { valid = false, message = "Mã ưu đãi không hợp lệ hoặc không đủ điều kiện áp dụng." });
        }

        decimal discount = 0;
        if (voucher.DiscountPercent.HasValue)
        {
            discount = subTotal * (voucher.DiscountPercent.Value / 100m);
            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
            {
                discount = voucher.MaxDiscountAmount.Value;
            }
        }
        else if (voucher.DiscountAmount.HasValue)
        {
            discount = voucher.DiscountAmount.Value;
        }

        return Json(new { valid = true, discount, message = $"Áp dụng thành công mã {voucher.Code}: Giảm {discount:N0}₫!" });
    }
}

public class VoucherCheckRequest
{
    public string Code { get; set; } = string.Empty;
}
