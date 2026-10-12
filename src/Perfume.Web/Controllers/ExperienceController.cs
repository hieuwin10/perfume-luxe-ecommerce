using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

public class ExperienceController : Controller
{
    private readonly PerfumeDbContext _db;

    public ExperienceController(PerfumeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Route("dat-lich-trai-nghiem")]
    [Route("private-tasting")]
    public async Task<IActionResult> TastingBooking()
    {
        ViewBag.Families = await _db.FragranceFamilies
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync();

        return View();
    }

    [HttpPost]
    [Route("dat-lich-trai-nghiem")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TastingBooking(TastingBookingRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            ViewBag.Error = "Vui lòng nhập họ tên và số điện thoại liên hệ.";
            ViewBag.Families = await _db.FragranceFamilies.AsNoTracking().ToListAsync();
            return View(request);
        }

        var booking = new PrivateTastingBooking
        {
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email?.Trim(),
            BoutiqueLocation = string.IsNullOrWhiteSpace(request.BoutiqueLocation) ? "Flagship 88 Tràng Tiền, Hà Nội" : request.BoutiqueLocation.Trim(),
            BookingDate = request.BookingDate < DateTime.Today ? DateTime.Today.AddDays(1) : request.BookingDate,
            TimeSlot = string.IsNullOrWhiteSpace(request.TimeSlot) ? "14:00 - 15:30" : request.TimeSlot.Trim(),
            GuestCount = Math.Clamp(request.GuestCount, 1, 6),
            PreferredScentFamilies = request.PreferredScentFamilies?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = "Pending"
        };

        _db.PrivateTastingBookings.Add(booking);
        await _db.SaveChangesAsync();

        ViewBag.BookingSuccess = true;
        ViewBag.BookingCode = $"VIP-{booking.Id:D5}";
        ViewBag.Booking = booking;

        return View("BookingSuccess", booking);
    }

    [HttpPost]
    [Route("api/v1/back-in-stock/subscribe")]
    public async Task<IActionResult> SubscribeBackInStock([FromBody] BackInStockSubscribeRequest? request)
    {
        if (request == null || request.ProductId <= 0 || string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
        {
            return BadRequest(new { success = false, message = "Email không hợp lệ hoặc thiếu thông tin sản phẩm." });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // Kiểm tra xem đã đăng ký trước đó chưa
        var existing = await _db.BackInStockAlerts
            .FirstOrDefaultAsync(a => a.ProductId == request.ProductId && a.CustomerEmail == email && !a.IsNotified);

        if (existing == null)
        {
            var alert = new BackInStockAlert
            {
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                CustomerEmail = email,
                CustomerPhone = request.Phone?.Trim(),
                IsNotified = false
            };
            _db.BackInStockAlerts.Add(alert);
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "Đã lưu đăng ký! Chúng tôi sẽ gửi thông báo đến email của bạn ngay khi sản phẩm về kho." });
    }
}

public class TastingBookingRequestModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BoutiqueLocation { get; set; } = "Flagship 88 Tràng Tiền, Hà Nội";
    public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(1);
    public string TimeSlot { get; set; } = "14:00 - 15:30";
    public int GuestCount { get; set; } = 1;
    public string? PreferredScentFamilies { get; set; }
    public string? Notes { get; set; }
}

public class BackInStockSubscribeRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
