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
public sealed class ApiV1ExperienceController(PerfumeDbContext db) : ControllerBase
{
    private readonly PerfumeDbContext _db = db;

    // ─── 5. POST /api/v1/experience/tasting-booking ───────────────────────────
    [HttpPost("api/v1/experience/tasting-booking")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateTastingBooking(
        [FromBody] TastingBookingApiRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.CustomerName) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new { message = "Vui lòng nhập đầy đủ họ tên và số điện thoại liên hệ." });
        }

        var digits = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 8 || digits.Length > 15)
        {
            return BadRequest(new { message = "Số điện thoại liên hệ không hợp lệ." });
        }

        if (request.GuestCount < 1 || request.GuestCount > 6)
        {
            return BadRequest(new { message = "Số lượng khách tham dự Private Tasting chỉ từ 1 đến 6 khách." });
        }

        if (request.BookingDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Ngày đặt lịch hẹn phải từ ngày hôm nay trở đi." });
        }

        var booking = new PrivateTastingBooking
        {
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            BoutiqueLocation = string.IsNullOrWhiteSpace(request.BoutiqueLocation)
                ? "Flagship 88 Tràng Tiền, Hà Nội"
                : request.BoutiqueLocation.Trim(),
            BookingDate = request.BookingDate,
            TimeSlot = string.IsNullOrWhiteSpace(request.TimeSlot) ? "14:00 - 15:30" : request.TimeSlot.Trim(),
            GuestCount = request.GuestCount,
            PreferredScentFamilies = request.PreferredScentFamilies?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.PrivateTastingBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new TastingBookingApiResponse
        {
            BookingId = booking.Id,
            BookingCode = $"VIP-{booking.Id:D5}",
            CustomerName = booking.CustomerName,
            PhoneNumber = booking.PhoneNumber,
            Email = booking.Email,
            BoutiqueLocation = booking.BoutiqueLocation,
            BookingDate = booking.BookingDate,
            TimeSlot = booking.TimeSlot,
            GuestCount = booking.GuestCount,
            PreferredScentFamilies = booking.PreferredScentFamilies,
            Status = booking.Status
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
