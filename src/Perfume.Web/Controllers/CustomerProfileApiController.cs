using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Web.Security;

namespace Perfume.Web.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CustomerProfileApiController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly IAntiforgery _antiforgery;

    public CustomerProfileApiController(UserManager<AppUser> users, IAntiforgery antiforgery)
    {
        _users = users;
        _antiforgery = antiforgery;
    }

    [HttpGet("customer/profile"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        return user == null ? Unauthorized() : Ok(ToProfile(user));
    }

    [HttpPut("customer/profile"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> UpdateProfile(CustomerProfileUpdateRequest request)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        var result = await _users.UpdateAsync(user);
        return result.Succeeded ? Ok(ToProfile(user)) : BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }

    private async Task<bool> ValidateCookieWriteAsync()
    {
        if (Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return true;
        try { await _antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static CustomerProfileDto ToProfile(AppUser user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        Tier = user.Tier.ToString(),
        LoyaltyPoints = user.LoyaltyPoints
    };
}
