using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;

namespace Perfume.Web.Controllers;

public class CheckoutController : Controller
{
    private const string GuestCartCookie = "plx_guest_cart";
    private readonly IOrderService _orderService;
    private readonly IOrderEmailSender _emailSender;
    private readonly ICustomerCartService _carts;
    private readonly UserManager<AppUser> _users;
    private readonly IDataProtector _guestOrderProtector;

    public CheckoutController(IOrderService orderService, IOrderEmailSender emailSender, ICustomerCartService carts,
        UserManager<AppUser> users, IDataProtectionProvider protectionProvider)
    {
        _orderService = orderService;
        _emailSender = emailSender;
        _carts = carts;
        _users = users;
        _guestOrderProtector = protectionProvider.CreateProtector("PerfumeLuxe.GuestOrderTracking.v1");
    }

    private async Task<LoyaltyStatusDto?> ResolveUserLoyaltyStatusAsync(AppUser? user, CancellationToken cancellationToken)
    {
        var userId = user?.Id ?? CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var loyaltyService = HttpContext?.RequestServices?.GetService<ILoyaltyTierService>();
        if (loyaltyService == null)
        {
            var db = HttpContext?.RequestServices?.GetService<PerfumeDbContext>();
            if (db != null)
            {
                loyaltyService = new LoyaltyTierService(db);
            }
        }

        if (loyaltyService != null)
        {
            return await loyaltyService.GetOrSyncUserLoyaltyAsync(userId, cancellationToken);
        }

        if (user == null) return null;
        var tier = user.Tier;
        var discountPercent = tier switch
        {
            UserTier.Diamond => 12m,
            UserTier.Gold => 7m,
            UserTier.Silver => 3m,
            _ => 0m
        };
        return new LoyaltyStatusDto
        {
            UserId = user.Id,
            CurrentTier = tier,
            TierName = tier.ToString(),
            LoyaltyPoints = user.LoyaltyPoints,
            DiscountPercent = discountPercent
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var state = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        if (state.Items.Count == 0) return RedirectToAction("Index", "Cart");
        ViewBag.Cart = state.Items;
        ViewBag.SubTotal = state.SubTotal;
        var user = await _users.GetUserAsync(User);
        ViewBag.LoyaltyStatus = await ResolveUserLoyaltyStatusAsync(user, cancellationToken);
        return View(new CheckoutRequestDto
        {
            PaymentMethod = PaymentMethod.COD,
            CustomerName = user?.FullName ?? string.Empty,
            CustomerEmail = user?.Email ?? string.Empty,
            CustomerPhone = user?.PhoneNumber ?? string.Empty,
            ShippingAddress = user?.Address ?? string.Empty
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutRequestDto model, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var guestToken = Request.Cookies[GuestCartCookie];
        var cart = await _carts.GetAsync(userId, guestToken, cancellationToken);
        if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");
        model.Items = cart.Items;
        if (model.PaymentMethod != PaymentMethod.COD && model.PaymentMethod != PaymentMethod.BankTransfer)
        {
            model.PaymentMethod = PaymentMethod.COD;
        }
        if (!ModelState.IsValid)
        {
            var currentUser = await _users.GetUserAsync(User);
            ViewBag.LoyaltyStatus = await ResolveUserLoyaltyStatusAsync(currentUser, cancellationToken);
            ViewBag.Cart = cart.Items;
            ViewBag.SubTotal = cart.SubTotal;
            ViewBag.Error = "Vui lòng kiểm tra lại thông tin giao hàng.";
            return View("Index", model);
        }

        var (success, error, order) = await _orderService.PlaceOrderAsync(model, userId);
        if (!success || order == null)
        {
            var currentUser = await _users.GetUserAsync(User);
            ViewBag.LoyaltyStatus = await ResolveUserLoyaltyStatusAsync(currentUser, cancellationToken);
            ViewBag.Cart = cart.Items;
            ViewBag.SubTotal = cart.SubTotal;
            ViewBag.Error = error ?? "Không thể đặt hàng, vui lòng thử lại.";
            return View("Index", model);
        }

        await _carts.ClearAsync(userId, guestToken, cancellationToken);
        string? token = null;
        if (order.UserId == null) token = _guestOrderProtector.Protect(order.OrderCode);
        var trackingUrl = token == null ? null : Url.Action(nameof(Success), "Checkout", new { token }, Request.Scheme);
        await _emailSender.SendOrderConfirmationAsync(order, trackingUrl);

        if (order.PaymentMethod == PaymentMethod.BankTransfer)
        {
            return order.UserId == null
                ? RedirectToAction(nameof(VietQR), new { token })
                : RedirectToAction(nameof(VietQR), new { orderCode = order.OrderCode });
        }

        return order.UserId == null
            ? RedirectToAction(nameof(Success), new { token })
            : RedirectToAction(nameof(Success), new { orderCode = order.OrderCode });
    }

    [HttpGet]
    public async Task<IActionResult> VietQR(string? orderCode, string? token)
    {
        string? verifiedCode = orderCode;
        if (!string.IsNullOrWhiteSpace(token))
        {
            try { verifiedCode = _guestOrderProtector.Unprotect(token); }
            catch { return NotFound(); }
        }
        else if (string.IsNullOrEmpty(CurrentUserId())) return NotFound();

        if (string.IsNullOrWhiteSpace(verifiedCode)) return NotFound();
        var order = await _orderService.GetOrderByCodeAsync(verifiedCode);
        if (order == null) return NotFound();
        if (order.UserId == null && string.IsNullOrWhiteSpace(token)) return NotFound();
        if (order.UserId != null && !string.Equals(order.UserId, CurrentUserId(), StringComparison.Ordinal)) return NotFound();

        ViewBag.IsGuest = order.UserId == null;
        ViewBag.Token = token;
        ViewBag.BankName = "Ngân hàng TMCP Ngoại Thương Việt Nam (Vietcombank)";
        ViewBag.BankCode = "VCB";
        ViewBag.AccountNumber = "9988776655";
        ViewBag.AccountName = "CONG TY CP PERFUME LUXE";
        ViewBag.TransferContent = order.OrderCode;
        ViewBag.Amount = order.TotalAmount;
        
        var qrUrl = $"https://img.vietqr.io/image/vcb-9988776655-compact2.png?amount={Convert.ToInt64(order.TotalAmount)}&addInfo={Uri.EscapeDataString(order.OrderCode)}&accountName=CONG%20TY%20CP%20PERFUME%20LUXE";
        ViewBag.QrUrl = qrUrl;

        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBankTransfer(string? orderCode, string? token)
    {
        string? verifiedCode = orderCode;
        if (!string.IsNullOrWhiteSpace(token))
        {
            try { verifiedCode = _guestOrderProtector.Unprotect(token); }
            catch { return NotFound(); }
        }
        else if (string.IsNullOrEmpty(CurrentUserId())) return NotFound();

        if (string.IsNullOrWhiteSpace(verifiedCode)) return NotFound();
        var order = await _orderService.GetOrderByCodeAsync(verifiedCode);
        if (order == null) return NotFound();
        if (order.UserId == null && string.IsNullOrWhiteSpace(token)) return NotFound();
        if (order.UserId != null && !string.Equals(order.UserId, CurrentUserId(), StringComparison.Ordinal)) return NotFound();

        await _orderService.ConfirmPaymentAsync(order.OrderCode);

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
        else if (string.IsNullOrEmpty(CurrentUserId())) return NotFound();

        if (string.IsNullOrWhiteSpace(verifiedCode)) return NotFound();
        var order = await _orderService.GetOrderByCodeAsync(verifiedCode);
        if (order == null) return NotFound();
        if (order.UserId == null && string.IsNullOrWhiteSpace(token)) return NotFound();
        if (order.UserId != null && !string.Equals(order.UserId, CurrentUserId(), StringComparison.Ordinal)) return NotFound();
        ViewBag.IsGuest = order.UserId == null;
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckVoucher([FromBody] VoucherCheckRequest request, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        var voucher = await _orderService.ValidateVoucherAsync(request.Code, cart.SubTotal);
        if (voucher == null) return Json(new { valid = false, message = "Mã ưu đãi không hợp lệ hoặc không đủ điều kiện áp dụng." });
        var discount = voucher.DiscountPercent.HasValue
            ? cart.SubTotal * voucher.DiscountPercent.Value / 100m
            : voucher.DiscountAmount ?? 0m;
        if (voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        return Json(new { valid = true, discount, message = $"Áp dụng thành công mã {voucher.Code}: Giảm {discount:N0}₫!" });
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}

public class VoucherCheckRequest { public string Code { get; set; } = string.Empty; }
