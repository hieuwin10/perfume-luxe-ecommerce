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
using Perfume.Infrastructure.Services;

namespace Perfume.Web.Controllers;

[ApiController]
public sealed class ApiV1DiscoveryBoxController : ControllerBase
{
    private const string GuestCartCookie = "plx_guest_cart";
    private readonly PerfumeDbContext _db;
    private readonly IDiscoveryBoxService _discoveryBoxService;
    private static readonly HashSet<string> AllowedBoxStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClassicWood",
        "RoyalVelvet",
        "ArtisanLeather"
    };

    public ApiV1DiscoveryBoxController(PerfumeDbContext db, IDiscoveryBoxService? discoveryBoxService = null)
    {
        _db = db;
        _discoveryBoxService = discoveryBoxService ?? new DiscoveryBoxService(db);
    }

    private string? EnsureGuestToken()
    {
        if (HttpContext == null) return null;
        var token = Request?.Cookies[GuestCartCookie];
        if (!string.IsNullOrWhiteSpace(token) && token.Length <= 128) return token;
        token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Response?.Cookies.Append(GuestCartCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request?.IsHttps ?? false,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30)
        });
        return token;
    }

    // ─── 3. POST /api/v1/discovery-box/validate-and-add ───────────────────────
    [HttpPost("api/v1/discovery-box/validate-and-add")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateAndAddDiscoveryBox(
        [FromBody] DiscoveryBoxApiRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { valid = false, message = "Dữ liệu cấu hình Discovery Box không hợp lệ." });
        }

        var style = string.IsNullOrWhiteSpace(request.BoxStyle) ? "ClassicWood" : request.BoxStyle.Trim();
        if (!AllowedBoxStyles.Contains(style))
        {
            return BadRequest(new
            {
                valid = false,
                message = "Kiểu hộp không hợp lệ. Chỉ chấp nhận ClassicWood, RoyalVelvet hoặc ArtisanLeather."
            });
        }

        if (request.Vials == null || request.Vials.Count < 3 || request.Vials.Count > 5)
        {
            return BadRequest(new
            {
                valid = false,
                message = "Discovery Box yêu cầu chọn từ 3 đến 5 ống chiết (vials)."
            });
        }

        if (!string.IsNullOrEmpty(request.GreetingMessage) && request.GreetingMessage.Length > 250)
        {
            return BadRequest(new
            {
                valid = false,
                message = "Thông điệp thiệp tặng kèm không được vượt quá 250 ký tự."
            });
        }

        decimal vialsTotal = 0m;
        foreach (var vial in request.Vials)
        {
            if (vial.ProductId <= 0)
            {
                return BadRequest(new { valid = false, message = "ProductId của ống chiết không hợp lệ." });
            }

            var normalizedSize = (vial.VialSize ?? "2ml").Trim().ToLowerInvariant();
            if (normalizedSize is not ("2ml" or "5ml"))
            {
                return BadRequest(new { valid = false, message = "Dung tích ống chiết chỉ hỗ trợ 2ml hoặc 5ml." });
            }

            if (await _db.Products.AnyAsync(cancellationToken))
            {
                var exists = await _db.Products.AnyAsync(p => p.Id == vial.ProductId && !p.IsDeleted, cancellationToken);
                if (!exists)
                {
                    return BadRequest(new { valid = false, message = $"Sản phẩm #{vial.ProductId} không tồn tại hoặc đã ngừng kinh doanh." });
                }
            }

            vialsTotal += normalizedSize == "5ml" ? 350_000m : 180_000m;
        }

        decimal boxFee = style.ToLowerInvariant() switch
        {
            "royalvelvet" => 200_000m,
            "artisanleather" => 300_000m,
            _ => 150_000m
        };

        var subTotal = vialsTotal + boxFee;
        decimal discountRate = request.Vials.Count switch
        {
            5 => 0.10m,
            4 => 0.05m,
            _ => 0.00m
        };

        var discountAmount = Math.Round(subTotal * discountRate, 0);
        var totalPrice = subTotal - discountAmount;

        var normalizedBoxStyle = AllowedBoxStyles.First(s => s.Equals(style, StringComparison.OrdinalIgnoreCase));

        if (HttpContext != null)
        {
            try
            {
                var guestToken = EnsureGuestToken();
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var ownerKey = !string.IsNullOrWhiteSpace(userId)
                    ? "user:" + userId
                    : (!string.IsNullOrWhiteSpace(guestToken)
                        ? "guest:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(guestToken))).ToLowerInvariant()
                        : null);

                if (ownerKey != null)
                {
                    var cart = await _db.ShoppingCarts
                        .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey, cancellationToken);

                    if (cart == null)
                    {
                        cart = new ShoppingCart
                        {
                            OwnerKey = ownerKey,
                            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                            ExpiresAt = DateTime.UtcNow.AddDays(30)
                        };
                        _db.ShoppingCarts.Add(cart);
                        await _db.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        cart.ExpiresAt = DateTime.UtcNow.AddDays(30);
                    }

                    var configJson = JsonSerializer.Serialize(new
                    {
                        boxStyleCode = normalizedBoxStyle,
                        boxStyleName = normalizedBoxStyle switch
                        {
                            "RoyalVelvet" => "Hộp Nhung Đen Huyền Bí",
                            "ArtisanLeather" => "Túi Da Thủ Công Viền Vàng",
                            _ => "Hộp Gỗ Sơn Mài Hoàng Gia"
                        },
                        boxPrice = boxFee,
                        vialsTotalPrice = vialsTotal,
                        subTotal = subTotal,
                        discountAmount = discountAmount,
                        totalBoxUnitPrice = totalPrice,
                        recipientName = request.RecipientName,
                        greetingMessage = request.GreetingMessage,
                        greetingFont = string.IsNullOrWhiteSpace(request.GreetingFont) ? "Playfair Display" : request.GreetingFont,
                        vials = request.Vials.Select((v, idx) => new
                        {
                            slot = idx + 1,
                            productId = v.ProductId,
                            volumeMl = (v.VialSize ?? "2ml").Trim().ToLowerInvariant() == "5ml" ? 5 : 2,
                            price = (v.VialSize ?? "2ml").Trim().ToLowerInvariant() == "5ml" ? 350_000m : 180_000m
                        })
                    });

                    var firstProductId = request.Vials[0].ProductId;
                    var cartItem = new ShoppingCartItem
                    {
                        ShoppingCartId = cart.Id,
                        ProductId = firstProductId,
                        VariantId = null,
                        Quantity = 1,
                        IsCustomBox = true,
                        CustomBoxConfigJson = configJson,
                        CustomUnitPrice = totalPrice
                    };

                    cart.Items.Add(cartItem);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch { }
        }

        return Ok(new DiscoveryBoxApiResponse
        {
            Valid = true,
            BoxStyle = normalizedBoxStyle,
            VialCount = request.Vials.Count,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TotalPrice = totalPrice,
            GreetingMessage = request.GreetingMessage?.Trim(),
            Message = $"Đã xác thực và thêm Discovery Box ({normalizedBoxStyle} - {request.Vials.Count} ống chiết) thành công."
        });
    }
}
