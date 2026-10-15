using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Perfume.Web.Security;

namespace Perfume.Web.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CustomerApiController : ControllerBase
{
    private const string GuestCartCookie = "plx_guest_cart";
    private static readonly TimeSpan GuestCartLifetime = TimeSpan.FromDays(30);
    private readonly IPerfumeCatalogService _catalog;
    private readonly ICustomerCartService _carts;
    private readonly IOrderService _orders;
    private readonly IOrderEmailSender _email;
    private readonly PerfumeDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly IAntiforgery _antiforgery;
    private readonly JwtTokenService _jwt;
    private readonly IDataProtector _guestOrderProtector;
    private readonly ILogger<CustomerApiController>? _logger;

    public CustomerApiController(IPerfumeCatalogService catalog, ICustomerCartService carts, IOrderService orders,
        IOrderEmailSender email, PerfumeDbContext db, UserManager<AppUser> users, SignInManager<AppUser> signIn,
        IAntiforgery antiforgery, JwtTokenService jwt, IDataProtectionProvider protectionProvider,
        ILogger<CustomerApiController>? logger = null)
    {
        _catalog = catalog;
        _carts = carts;
        _orders = orders;
        _email = email;
        _db = db;
        _users = users;
        _signIn = signIn;
        _antiforgery = antiforgery;
        _jwt = jwt;
        _guestOrderProtector = protectionProvider.CreateProtector("PerfumeLuxe.GuestOrderTracking.v1");
        _logger = logger;
    }

    [HttpGet("catalog/products")]
    public async Task<IActionResult> Products([FromQuery] ProductFilterParams filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 48);
        var (items, totalCount) = await _catalog.GetProductsAsync(filter);
        return Ok(new { items, totalCount, page = filter.Page, pageSize = filter.PageSize, pageCount = (int)Math.Ceiling(totalCount / (double)filter.PageSize) });
    }

    [HttpGet("catalog/products/{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        var product = await _catalog.GetProductBySlugAsync(slug);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register(CustomerRegisterRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var email = request.Email.Trim();
        if (await _users.FindByEmailAsync(email) != null) return Conflict(new { message = "Email này đã được sử dụng." });

        var user = new AppUser { UserName = email, Email = email, FullName = request.FullName.Trim(), CreatedAt = DateTime.UtcNow };
        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
        var roleResult = await _users.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            return StatusCode(500, new { message = "Chưa tạo được quyền khách hàng. Vui lòng thử lại." });
        }

        await _signIn.SignInAsync(user, isPersistent: false);
        await MergeGuestCartAsync(user.Id, cancellationToken);
        return Ok(new { customer = ToProfile(user), redirectUrl = "/account/profile" });
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user == null) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        var result = await _signIn.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { message = result.IsLockedOut ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần." : "Email hoặc mật khẩu không đúng." });

        await MergeGuestCartAsync(user.Id, cancellationToken);
        var roles = await _users.GetRolesAsync(user);
        var defaultUrl = roles.Contains("Admin") ? "/admin/admindashboard" : roles.Contains("StoreManager") ? "/admin/storemanager" : "/account/profile";
        var redirectUrl = !string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl) ? request.ReturnUrl : defaultUrl;
        return Ok(new { customer = ToProfile(user), redirectUrl });
    }

    [HttpPost("auth/logout"), Authorize(AuthenticationSchemes = "Identity.Application")]
    public async Task<IActionResult> Logout()
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        await _signIn.SignOutAsync();
        return NoContent();
    }

    [HttpPost("auth/token")]
    public async Task<IActionResult> CreateToken(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user == null) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        var result = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        return Ok(await CreateTokenPairAsync(user, cancellationToken));
    }

    [HttpPost("auth/refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = HashToken(request.RefreshToken);
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var current = await _db.RefreshTokenRecords.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (current == null || current.ExpiresAt <= DateTime.UtcNow || current.RevokedAt.HasValue)
        {
            if (current?.RevokedAt != null)
            {
                var tokensToRevoke = await _db.RefreshTokenRecords
                    .Where(t => t.UserId == current.UserId && t.RevokedAt == null)
                    .ToListAsync(cancellationToken);
                foreach (var t in tokensToRevoke) t.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });
        }

        var user = await _users.FindByIdAsync(current.UserId);
        if (user == null) return Unauthorized(new { message = "Tài khoản không còn tồn tại." });
        var response = await CreateTokenPairAsync(user, cancellationToken);
        current.RevokedAt = DateTime.UtcNow;
        current.ReplacedByTokenHash = HashToken(response.RefreshToken);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("auth/revoke")]
    public async Task<IActionResult> RevokeToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var hash = HashToken(request.RefreshToken);
        var token = await _db.RefreshTokenRecords.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token != null && !token.RevokedAt.HasValue)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [HttpPost("auth/password/forgot")]
    public async Task<IActionResult> ForgotPassword(PasswordResetRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user != null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var resetUrl = $"{Request.Scheme}://{Request.Host}/account/resetpassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            await _email.SendPasswordResetAsync(user.Email!, user.FullName, resetUrl);
        }
        return Ok(new { message = "Nếu email thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi." });
    }

    [HttpPost("auth/password/reset")]
    public async Task<IActionResult> ResetPassword(PasswordResetConfirmRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user == null) return BadRequest(new { message = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." });
        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        return result.Succeeded
            ? Ok(new { message = "Đã đổi mật khẩu. Bạn có thể đăng nhập." })
            : BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
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

    [HttpGet("customer/orders"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> MyOrders(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return Ok(orders.Select(ToOrderDto));
    }

    [HttpGet("customer/orders/{id:int}"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> MyOrder(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var order = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);
        return order == null ? NotFound() : Ok(ToOrderDto(order));
    }

    [HttpGet("cart")]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        return Ok(cart);
    }

    [HttpPost("cart/items")]
    public async Task<IActionResult> AddCartItem(CartAddItemRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var guestToken = EnsureGuestCartToken();
        try { return Ok(await _carts.AddAsync(CurrentUserId(), guestToken, request, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("cart/items")]
    public async Task<IActionResult> SetCartItemQuantity(CartLineRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        try { return Ok(await _carts.SetQuantityAsync(CurrentUserId(), EnsureGuestCartToken(), request, cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("cart/items/{productId:int}")]
    public async Task<IActionResult> RemoveCartItem(int productId, [FromQuery] int? variantId, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var cart = await _carts.RemoveAsync(CurrentUserId(), EnsureGuestCartToken(), productId, variantId, cancellationToken);
        return Ok(cart);
    }

    [HttpPost("cart/merge"), Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> MergeCart(CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var userId = CurrentUserId();
        var state = await _carts.MergeGuestCartAsync(userId!, Request.Cookies[GuestCartCookie], cancellationToken);
        Response.Cookies.Delete(GuestCartCookie, new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax, Secure = Request.IsHttps });
        return Ok(state);
    }

    [HttpPost("vouchers/validate")]
    public async Task<IActionResult> ValidateVoucher(VoucherValidationRequest request, CancellationToken cancellationToken)
    {
        var cart = await _carts.GetAsync(CurrentUserId(), Request.Cookies[GuestCartCookie], cancellationToken);
        var voucher = await _orders.ValidateVoucherAsync(request.Code, cart.SubTotal);
        if (voucher == null) return Ok(new VoucherValidationResponse { Message = "Mã ưu đãi không hợp lệ hoặc chưa đủ điều kiện áp dụng." });
        var discount = voucher.DiscountPercent.HasValue
            ? cart.SubTotal * voucher.DiscountPercent.Value / 100m
            : voucher.DiscountAmount ?? 0m;
        if (voucher.MaxDiscountAmount.HasValue) discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
        return Ok(new VoucherValidationResponse { Valid = true, DiscountAmount = Math.Min(cart.SubTotal, discount), Message = $"Áp dụng thành công mã {voucher.Code}." });
    }

    [HttpPost("orders")]
    public async Task<IActionResult> PlaceOrder(CustomerOrderRequest request, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var userId = CurrentUserId();
        var guestToken = Request.Cookies[GuestCartCookie];
        var cart = await _carts.GetAsync(userId, guestToken, cancellationToken);
        if (cart.Items.Count == 0) return BadRequest(new { message = "Giỏ hàng của bạn đang trống." });
        if (cart.Items.Any(i => !i.IsAvailable)) return BadRequest(new { message = "Có sản phẩm đã hết hàng hoặc thay đổi tồn kho. Vui lòng cập nhật giỏ hàng." });

        var checkout = new CheckoutRequestDto
        {
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            ShippingAddress = request.ShippingAddress.Trim(),
            City = request.City?.Trim(),
            OrderNotes = request.OrderNotes?.Trim(),
            VoucherCode = request.VoucherCode?.Trim(),
            PaymentMethod = request.PaymentMethod,
            IsGiftWrapping = request.IsGiftWrapping,
            GiftMessage = request.GiftMessage?.Trim(),
            Items = cart.Items.Select(i => new CartItemDto { ProductId = i.ProductId, VariantId = i.VariantId, Quantity = i.Quantity }).ToList()
        };
        var (success, error, order) = await _orders.PlaceOrderAsync(checkout, userId);
        if (!success || order == null) return BadRequest(new { message = error ?? "Không thể hoàn tất đơn hàng." });

        await _carts.ClearAsync(userId, guestToken, cancellationToken);
        string? trackingToken = null;
        string? trackingUrl = null;
        if (order.UserId == null)
        {
            trackingToken = _guestOrderProtector.Protect(order.OrderCode);
            trackingUrl = Url?.Action("Success", "Checkout", new { token = trackingToken }, Request.Scheme)
                ?? $"/checkout/success?token={trackingToken}";
        }
        try
        {
            await _email.SendOrderConfirmationAsync(order, trackingUrl);
        }
        catch
        {
            _logger?.LogWarning("Order {OrderCode} was saved, but its confirmation email could not be sent.", order.OrderCode);
        }
        var targetAction = order.PaymentMethod == PaymentMethod.BankTransfer ? "VietQR" : "Success";
        var response = new GuestOrderResponse
        {
            Order = ToOrderDto(order),
            TrackingToken = trackingToken,
            SuccessUrl = order.UserId == null
                ? (Url?.Action(targetAction, "Checkout", new { token = trackingToken }, Request.Scheme) ?? $"/checkout/{targetAction.ToLowerInvariant()}?token={trackingToken}")
                : (Url?.Action(targetAction, "Checkout", new { orderCode = order.OrderCode }, Request.Scheme) ?? $"/checkout/{targetAction.ToLowerInvariant()}?orderCode={order.OrderCode}")
        };
        return Ok(response);
    }

    [HttpGet("orders/guest")]
    public async Task<IActionResult> TrackGuestOrder([FromQuery] string token)
    {
        string code;
        try { code = _guestOrderProtector.Unprotect(token); }
        catch { return NotFound(); }
        var order = await _orders.GetOrderByCodeAsync(code);
        return order == null || order.UserId != null ? NotFound() : Ok(ToOrderDto(order));
    }

    [HttpGet("products/{productId:int}/reviews")]
    public async Task<IActionResult> ProductReviews(int productId, CancellationToken cancellationToken)
    {
        var reviews = await _db.Reviews.AsNoTracking().Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync(cancellationToken);
        var response = new ProductReviewsResponse
        {
            RatingCount = reviews.Count,
            RatingAverage = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1),
            Items = reviews.Select(r => new ProductReviewDto
            {
                Id = r.Id,
                UserName = r.UserName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                ImageUrls = ParseImageNames(r.ImageUrls).Select(name => $"/reviews/photo?reviewId={r.Id}&fileName={Uri.EscapeDataString(name)}").ToList()
            }).ToList()
        };
        return Ok(response);
    }

    [HttpPost("products/{productId:int}/reviews"), Authorize(Roles = "Customer", Policy = "CustomerApi")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> CreateReview(int productId, [FromForm] int rating, [FromForm] string comment,
        [FromForm] List<IFormFile>? images, CancellationToken cancellationToken)
    {
        if (!await ValidateCookieWriteAsync()) return BadRequest(new { message = "Phiên gửi biểu mẫu đã hết hạn. Vui lòng tải lại trang." });
        var user = await _users.GetUserAsync(User);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (user == null || product == null) return NotFound();
        comment = (comment ?? string.Empty).Trim();
        if (rating is < 1 or > 5 || comment.Length is < 5 or > 2000)
            return BadRequest(new { message = "Đánh giá cần có từ 1 đến 5 sao và nội dung từ 5 đến 2.000 ký tự." });
        var purchased = await _db.Orders.AnyAsync(o => o.UserId == user.Id && o.OrderStatus == OrderStatus.Delivered && o.Items.Any(i => i.ProductId == productId), cancellationToken);
        if (!purchased) return Forbid();
        if (await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == user.Id, cancellationToken))
            return Conflict(new { message = "Bạn đã gửi đánh giá cho sản phẩm này." });

        images ??= new List<IFormFile>();
        if (images.Count(f => f.Length > 0) > 3 || images.Any(f => f.Length > 5 * 1024 * 1024))
            return BadRequest(new { message = "Mỗi đánh giá nhận tối đa 3 ảnh, mỗi ảnh tối đa 5 MB." });

        var root = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Storage:RootPath"];
        if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(AppContext.BaseDirectory, "App_Data");
        var imageDirectory = Path.Combine(root, "ReviewImages");
        Directory.CreateDirectory(imageDirectory);
        var saved = new List<string>();
        try
        {
            foreach (var upload in images.Where(f => f.Length > 0))
            {
                var extension = await DetectImageExtensionAsync(upload, cancellationToken);
                if (extension == null) throw new InvalidOperationException("Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP hợp lệ.");
                var safeName = $"{Guid.NewGuid():N}{extension}";
                await using var output = System.IO.File.Create(Path.Combine(imageDirectory, safeName));
                await upload.CopyToAsync(output, cancellationToken);
                saved.Add(safeName);
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                UserName = user.FullName,
                Rating = rating,
                Comment = comment,
                IsApproved = false,
                ImageUrls = JsonSerializer.Serialize(saved)
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Đánh giá đã được gửi và sẽ hiển thị sau khi cửa hàng duyệt." });
        }
        catch (InvalidOperationException ex)
        {
            DeleteSavedImages(imageDirectory, saved);
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            DeleteSavedImages(imageDirectory, saved);
            return StatusCode(500, new { message = "Chưa gửi được đánh giá. Vui lòng thử lại." });
        }
    }

    private async Task<ApiTokenResponse> CreateTokenPairAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var (accessToken, accessExpiry) = _jwt.CreateAccessToken(user, roles);
        var refresh = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var refreshExpiry = DateTime.UtcNow.AddDays(30);
        _db.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            UserId = user.Id,
            TokenHash = HashToken(refresh),
            ExpiresAt = refreshExpiry
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new ApiTokenResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiry,
            RefreshToken = refresh,
            RefreshTokenExpiresAtUtc = refreshExpiry,
            Customer = ToProfile(user)
        };
    }

    private async Task MergeGuestCartAsync(string userId, CancellationToken cancellationToken)
    {
        var token = Request.Cookies[GuestCartCookie];
        await _carts.MergeGuestCartAsync(userId, token, cancellationToken);
        Response.Cookies.Delete(GuestCartCookie, new CookieOptions { Path = "/", SameSite = SameSiteMode.Lax, Secure = Request.IsHttps });
    }

    private string EnsureGuestCartToken()
    {
        var existing = Request.Cookies[GuestCartCookie];
        if (!string.IsNullOrWhiteSpace(existing) && existing.Length <= 128) return existing;
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(GuestCartCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            MaxAge = GuestCartLifetime
        });
        return token;
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

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

    private static CustomerOrderDto ToOrderDto(Order order) => new()
    {
        Id = order.Id,
        OrderCode = order.OrderCode,
        CreatedAt = order.CreatedAt,
        OrderStatus = order.OrderStatus.ToString(),
        PaymentStatus = order.PaymentStatus.ToString(),
        PaymentMethod = order.PaymentMethod.ToString(),
        SubTotal = order.SubTotal,
        DiscountAmount = order.DiscountAmount,
        ShippingFee = order.ShippingFee,
        TotalAmount = order.TotalAmount,
        IsGiftWrapping = order.IsGiftWrapping,
        GiftWrappingFee = order.GiftWrappingFee,
        GiftMessage = order.GiftMessage,
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        CustomerEmail = order.CustomerEmail,
        ShippingAddress = order.ShippingAddress,
        City = order.City,
        TrackingNumber = order.TrackingNumber,
        Items = order.Items.Select(i => new CustomerOrderLineDto
        {
            ProductId = i.ProductId,
            VariantId = i.VariantId,
            ProductName = i.ProductName,
            VariantLabel = i.VariantLabel,
            ProductImageUrl = i.ProductImageUrl,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static List<string> ParseImageNames(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new List<string>(); }
        catch (JsonException) { return new List<string>(); }
    }

    private static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024) return null;
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header, cancellationToken);
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (read >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
            header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') return ".webp";
        return null;
    }

    private static void DeleteSavedImages(string directory, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var path = Path.Combine(directory, name);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
