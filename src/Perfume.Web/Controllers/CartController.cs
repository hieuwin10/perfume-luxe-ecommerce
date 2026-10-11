using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Controllers;

public class CartController : Controller
{
    private const string GuestCartCookie = "plx_guest_cart";
    private readonly ICustomerCartService _carts;

    public CartController(ICustomerCartService carts) => _carts = carts;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken));

    [HttpGet]
    public async Task<IActionResult> GetCartState(CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        return Json(new { items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _carts.AddAsync(CurrentUserId(), EnsureGuestToken(),
                new CartAddItemRequest { ProductSlug = request.ProductSlug, VariantId = request.VariantId, Quantity = request.Quantity }, cancellationToken);
            return Ok(new { success = true, items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQuantityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _carts.SetQuantityAsync(CurrentUserId(), EnsureGuestToken(),
                new CartLineRequest { ProductId = request.ProductId, VariantId = request.VariantId, Quantity = request.Quantity }, cancellationToken);
            return Ok(new { success = true, items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemRequest request, CancellationToken cancellationToken)
    {
        var cart = await _carts.RemoveAsync(CurrentUserId(), EnsureGuestToken(), request.ProductId, request.VariantId, cancellationToken);
        return Ok(new { success = true, items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private string EnsureGuestToken()
    {
        var token = Request.Cookies[GuestCartCookie];
        if (!string.IsNullOrWhiteSpace(token) && token.Length <= 128) return token;
        token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(GuestCartCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30)
        });
        return token;
    }
}

public class AddToCartRequest
{
    public string ProductSlug { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartQuantityRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveCartItemRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
}
