using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

/// <summary>
/// Controller cho phep khach hang tra cuu tien do don hang theo ma don va so dien thoai
/// ma khong can dang nhap tai khoan.
/// </summary>
public class OrderTrackingController : Controller
{
    private readonly PerfumeDbContext _db;

    // ==========================================
    // 1. KHOI TAO CONTROLLER
    // ==========================================
    public OrderTrackingController(PerfumeDbContext db)
    {
        _db = db;
    }

    // ==========================================
    // 2. TRA CUU TIEN DO DON HANG (TIM KIEM THEO MA & SDT)
    // ==========================================
    [HttpGet]
    [Route("tra-cuu-don-hang")]
    [Route("order-tracking")]
    public async Task<IActionResult> Index(string? orderCode, string? phone)
    {
        var model = new OrderTrackingViewModel
        {
            OrderCode = orderCode?.Trim(),
            Phone = phone?.Trim()
        };

        if (!string.IsNullOrWhiteSpace(model.OrderCode) && !string.IsNullOrWhiteSpace(model.Phone))
        {
            // Lam sach so dien thoai de so khop linh hoat (chi giu lai chu so)
            var cleanPhone = new string(model.Phone.Where(char.IsDigit).ToArray());
            var queryCode = model.OrderCode.Trim().ToUpperInvariant();

            // Truy van don hang kem danh sach chi tiet san pham
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == queryCode);

            if (order != null)
            {
                var orderCleanPhone = new string(order.CustomerPhone.Where(char.IsDigit).ToArray());
                // Kiem tra khop so dien thoai (trung toan bo hoac khop it nhat 6 so cuoi)
                bool phoneMatches = orderCleanPhone == cleanPhone || 
                    (orderCleanPhone.Length >= 6 && cleanPhone.Length >= 6 && 
                     (orderCleanPhone.EndsWith(cleanPhone) || cleanPhone.EndsWith(orderCleanPhone)));

                if (phoneMatches)
                {
                    model.Order = order;
                    model.HasSearched = true;
                }
                else
                {
                    model.HasSearched = true;
                    model.ErrorMessage = "Số điện thoại nhận hàng không khớp với mã đơn hàng đã nhập.";
                }
            }
            else
            {
                model.HasSearched = true;
                model.ErrorMessage = "Không tìm thấy đơn hàng với mã \"" + model.OrderCode + "\". Vui lòng kiểm tra lại tin nhắn hoặc email đặt hàng.";
            }
        }

        return View(model);
    }
}

public class OrderTrackingViewModel
{
    public string? OrderCode { get; set; }
    public string? Phone { get; set; }
    public bool HasSearched { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}
