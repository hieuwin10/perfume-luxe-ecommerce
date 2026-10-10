using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly PerfumeDbContext _db;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, PerfumeDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    // ─── LOGIN ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập email và mật khẩu.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Redirect by role
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
            if (roles.Contains("StoreManager"))
                return RedirectToAction("Index", "StoreManager", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Email hoặc mật khẩu không đúng.";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ─── REGISTER ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin.";
            return View();
        }

        if (password != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View();
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            ViewBag.Error = "Email này đã được sử dụng.";
            return View();
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = $"Chào mừng {fullName} đến với PERFUME LUXE! 🌸";
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
        return View();
    }

    // ─── LOGOUT ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ─── MY ACCOUNT ───────────────────────────────────────────
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");
        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = roles;
        return View(user);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        return View(user);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(string fullName, string? address, string? phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));
        fullName = (fullName ?? "").Trim();
        if (fullName.Length is < 2 or > 100 || (address?.Length ?? 0) > 500 || (phoneNumber?.Length ?? 0) > 30)
        {
            ModelState.AddModelError("", "Vui lòng kiểm tra lại họ tên, địa chỉ và số điện thoại.");
            return View(user);
        }

        user.FullName = fullName;
        user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(user);
        }
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        var orders = await _db.Orders.Include(o => o.Items).Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync();
        return View(orders);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        return order == null ? NotFound() : View(order);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
