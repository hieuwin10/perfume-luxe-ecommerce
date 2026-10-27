using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/AdminDashboard/[action]")]
[Authorize(Roles = "Admin,StoreManager")]
public class AdminOrdersController : Controller
{
    private readonly PerfumeDbContext _db;
    private readonly ISystemSettingService _settingService;

    public AdminOrdersController(
        PerfumeDbContext _db,
        ISystemSettingService _settingService)
    {
        this._db = _db;
        this._settingService = _settingService;
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return View("~/Areas/Admin/Views/AdminDashboard/OrderDetails.cshtml", order);
    }

    public async Task<IActionResult> Orders(int page = 1, string? search = null, OrderStatus? status = null, PaymentStatus? paymentStatus = null, DateTime? from = null, DateTime? to = null)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);

        var query = _db.Orders
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(o => o.OrderCode.Contains(s) || o.CustomerName.Contains(s) || o.CustomerPhone.Contains(s));
        }

        if (status.HasValue)
            query = query.Where(o => o.OrderStatus == status.Value);

        if (paymentStatus.HasValue)
            query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Total = total;
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.PaymentStatus = paymentStatus;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");

        return View("~/Areas/Admin/Views/AdminDashboard/Orders.cshtml", orders);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderShippingInfo(int orderId, string customerName, string customerPhone, string shippingAddress, string? city, string? orderNotes)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        if (order.OrderStatus is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            TempData["Error"] = "Chỉ có thể thay đổi địa chỉ khi đơn ở trạng thái Chờ duyệt hoặc Đã xác nhận.";
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ tên, số điện thoại và địa chỉ nhận hàng.";
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        order.CustomerName = customerName.Trim();
        order.CustomerPhone = customerPhone.Trim();
        order.ShippingAddress = shippingAddress.Trim();
        order.City = city?.Trim();
        order.OrderNotes = orderNotes?.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật thông tin giao hàng cho đơn #{order.OrderCode}!";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }


    public async Task<IActionResult> PrintInvoice(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        var settings = await _settingService.GetSettingsAsync();
        ViewBag.Settings = settings;
        return View("~/Areas/Admin/Views/AdminDashboard/PrintInvoice.cshtml", order);
    }
}
