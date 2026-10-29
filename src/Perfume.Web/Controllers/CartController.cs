using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Controllers;

/// <summary>
/// Controller quan ly gio hang mua sam, ho tro kien truc gio hang kep (Guest va Member)
/// voi co che tu dong sinh token an danh luu cookie 30 ngay.
/// </summary>
public class CartController : Controller
{
    private const string GuestCartCookie = "plx_guest_cart";
    private readonly ICustomerCartService _carts;

    // ==========================================
    // 1. KHOI TAO CONTROLLER
    // ==========================================
    public CartController(ICustomerCartService carts) => _carts = carts;

    // ==========================================
    // 2. XEM GIO HANG & LAY TRANG THAI REALTIME
    // ==========================================
    // Trang hien thi chi tiet gio hang
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken));

    // API tra ve trang thai gio hang phuc vu header badge va mini cart
    [HttpGet]
    public async Task<IActionResult> GetCartState(CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        return Json(new { items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
    }

    // ==========================================
    // 3. THEM SAN PHAM VAO GIO HANG
    // ==========================================
    // Xu ly yeu cau them san pham va phien ban vao gio
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

    // ==========================================
    // 4. CAP NHAT SO LUONG SAN PHAM
    // ==========================================
    // Cap nhat so luong cua mot dong san pham trong gio
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

    // ==========================================
    // 5. XOA SAN PHAM KHOI GIO HANG
    // ==========================================
    // Xoa hoan toan mot san pham khoi gio hang
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemRequest request, CancellationToken cancellationToken)
    {
        var cart = await _carts.RemoveAsync(CurrentUserId(), EnsureGuestToken(), request.ProductId, request.VariantId, cancellationToken);
        return Ok(new { success = true, items = cart.Items, totalAmount = cart.SubTotal, totalCount = cart.TotalCount });
    }

    // ==========================================
    // 6. HAM PHU TRO NGUOI DUNG VA GUEST TOKEN
    // ==========================================
    // Lay UserId tu ClaimsPrincipal neu da dang nhap
    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // Kiem tra hoac tao moi token an danh cho khach vang lai luu trong cookie 30 ngay
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
