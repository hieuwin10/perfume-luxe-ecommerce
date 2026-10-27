using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

[ApiController]
[Route("api/v1/store-manager")]
[Authorize(Policy = "StoreManagerApi")]
public sealed class StoreManagerPromotionsApiController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IAntiforgery _antiforgery;

    public StoreManagerPromotionsApiController(PerfumeDbContext db, IAntiforgery antiforgery)
    {
        _db = db;
        _antiforgery = antiforgery;
    }

    [HttpGet("vouchers")]
    public async Task<IActionResult> Vouchers(CancellationToken cancellationToken) =>
        Ok(await _db.Vouchers.AsNoTracking().OrderByDescending(v => v.CreatedAt).Select(v => new
        {
            v.Id, v.Code, v.Description, v.DiscountPercent, v.DiscountAmount, v.MinOrderAmount, v.MaxDiscountAmount,
            v.UsageLimit, v.UsedCount, v.ValidFrom, v.ValidTo, v.IsActive
        }).ToListAsync(cancellationToken));

    [HttpPost("vouchers")]
    public async Task<IActionResult> CreateVoucher(StoreVoucherRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var validation = await ValidateVoucherRequestAsync(request, null, cancellationToken);
        if (validation != null) return validation;
        var voucher = new Voucher();
        ApplyVoucher(voucher, request);
        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/store-manager/vouchers/{voucher.Id}", new { voucher.Id, voucher.Code, voucher.IsActive });
    }

    [HttpPut("vouchers/{id:int}")]
    public async Task<IActionResult> UpdateVoucher(int id, StoreVoucherRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return AntiforgeryFailure();
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (voucher == null) return NotFound();
        var validation = await ValidateVoucherRequestAsync(request, id, cancellationToken);
        if (validation != null) return validation;
        ApplyVoucher(voucher, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { voucher.Id, voucher.Code, voucher.IsActive, voucher.UsedCount });
    }

    private async Task<IActionResult?> ValidateVoucherRequestAsync(StoreVoucherRequest request, int? excludedId, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (code.Length is < 3 or > 40 || request.UsageLimit < 1 || Utc(request.ValidTo) <= Utc(request.ValidFrom) ||
            (request.DiscountPercent.HasValue == request.DiscountAmount.HasValue) ||
            (request.DiscountPercent.GetValueOrDefault() <= 0 && request.DiscountAmount.GetValueOrDefault() <= 0) ||
            (request.DiscountPercent.HasValue && request.DiscountPercent.Value is < 0 or > 100) ||
            (request.DiscountAmount.HasValue && request.DiscountAmount.Value < 0) ||
            request.MinOrderAmount < 0 || request.MaxDiscountAmount < 0)
            return BadRequest(new { message = "Thông tin voucher chưa hợp lệ." });
        if (await _db.Vouchers.AnyAsync(v => v.Code == code && (!excludedId.HasValue || v.Id != excludedId.Value), cancellationToken))
            return Conflict(new { message = "Mã voucher này đã tồn tại." });
        if (excludedId.HasValue)
        {
            var existing = await _db.Vouchers.FirstOrDefaultAsync(v => v.Id == excludedId.Value, cancellationToken);
            if (existing != null && request.UsageLimit < existing.UsedCount)
                return BadRequest(new { message = "Giới hạn sử dụng không được thấp hơn số lượt đã dùng." });
        }
        return null;
    }

    private static void ApplyVoucher(Voucher voucher, StoreVoucherRequest request)
    {
        voucher.Code = request.Code.Trim().ToUpperInvariant();
        voucher.Description = request.Description?.Trim() ?? string.Empty;
        voucher.DiscountPercent = request.DiscountPercent;
        voucher.DiscountAmount = request.DiscountAmount;
        voucher.MinOrderAmount = request.MinOrderAmount;
        voucher.MaxDiscountAmount = request.MaxDiscountAmount;
        voucher.UsageLimit = request.UsageLimit;
        voucher.ValidFrom = Utc(request.ValidFrom);
        voucher.ValidTo = Utc(request.ValidTo);
        voucher.IsActive = request.IsActive;
    }

    private async Task<bool> ValidateCookieWriteAsync()
    {
        if (Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return true;
        try { await _antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private IActionResult AntiforgeryFailure() => BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
    private static DateTime Utc(DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => dateTime,
        DateTimeKind.Local => dateTime.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
    };
}
