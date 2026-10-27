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
public sealed class ApiV1DiscoveryBoxController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private static readonly HashSet<string> AllowedBoxStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClassicWood",
        "RoyalVelvet",
        "ArtisanLeather"
    };

    public ApiV1DiscoveryBoxController(PerfumeDbContext db) => _db = db;

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
