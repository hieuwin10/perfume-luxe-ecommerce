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
public sealed class ApiV1ExtensionsController : ControllerBase
{
    private readonly PerfumeDbContext _db;
    private readonly IFlashSaleService _flashSaleService;

    private static readonly HashSet<string> AllowedBoxStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClassicWood",
        "RoyalVelvet",
        "ArtisanLeather"
    };

    public ApiV1ExtensionsController(PerfumeDbContext db, IFlashSaleService flashSaleService)
    {
        _db = db;
        _flashSaleService = flashSaleService;
    }

    // ─── 1. GET /api/v1/flashsales/active ─────────────────────────────────────
    [HttpGet("api/v1/flashsales/active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveFlashSale(CancellationToken cancellationToken = default)
    {
        var active = await _flashSaleService.GetActiveFlashSaleAsync(cancellationToken);
        if (active == null)
        {
            return NotFound(new { message = "Hiện không có chiến dịch Flash Sale nào đang diễn ra." });
        }

        var remainingSeconds = (long)Math.Max(0, (active.EndTime - DateTime.UtcNow).TotalSeconds);

        return Ok(new FlashSaleActiveApiResponse
        {
            Id = active.Id,
            Name = active.Name,
            Description = active.Description,
            StartTime = active.StartTime,
            EndTime = active.EndTime,
            IsActive = active.IsActive,
            IsOngoing = active.IsOngoing,
            RemainingSeconds = remainingSeconds,
            BannerUrl = active.BannerUrl,
            Items = active.Items
        });
    }

    // ─── 2. GET /api/v1/loyalty/status ────────────────────────────────────────
    [HttpGet("api/v1/loyalty/status")]
    [Authorize(Policy = "CustomerApi")]
    public async Task<IActionResult> GetLoyaltyStatus(CancellationToken cancellationToken = default)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để tra cứu hạng thành viên VIP." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "Không tìm thấy định danh tài khoản hợp lệ." });
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var loyalty = await _db.UserLoyalties.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        if (user == null && loyalty == null)
        {
            return Unauthorized(new { message = "Tài khoản không tồn tại trong hệ thống." });
        }

        var deliveredSpent = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.OrderStatus == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var totalSpent = loyalty != null && loyalty.TotalSpent > deliveredSpent
            ? loyalty.TotalSpent
            : deliveredSpent;

        var computedTier = CalculateTierFromSpent(totalSpent);
        var storedTier = loyalty?.Tier ?? user?.Tier ?? UserTier.Bronze;
        var effectiveTier = storedTier > computedTier ? storedTier : computedTier;

        var (discountPercent, nextTier, amountToNextTier) = GetTierProgress(effectiveTier, totalSpent);
        var points = loyalty?.Points > 0
            ? loyalty.Points
            : (user?.LoyaltyPoints > 0 ? user.LoyaltyPoints : (int)(totalSpent / 100_000m));

        return Ok(new LoyaltyStatusApiResponse
        {
            UserId = userId,
            Tier = effectiveTier.ToString(),
            TotalSpent = totalSpent,
            DiscountPercent = discountPercent,
            Points = points,
            NextTier = nextTier,
            AmountToNextTier = amountToNextTier
        });
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

    // ─── 4. GET /api/v1/orders/track?orderCode={code} ─────────────────────────
    [HttpGet("api/v1/orders/track")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackOrder(
        [FromQuery] string? orderCode,
        [FromQuery] string? phone = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return BadRequest(new { message = "Vui lòng cung cấp mã đơn hàng (orderCode) để tra cứu." });
        }

        var normalizedCode = orderCode.Trim().ToUpperInvariant();
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == normalizedCode, cancellationToken);

        if (order == null)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng với mã '{orderCode.Trim()}'." });
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            var orderCleanPhone = new string(order.CustomerPhone.Where(char.IsDigit).ToArray());
            bool phoneMatches = orderCleanPhone == cleanPhone ||
                (orderCleanPhone.Length >= 6 && cleanPhone.Length >= 6 &&
                 (orderCleanPhone.EndsWith(cleanPhone) || cleanPhone.EndsWith(orderCleanPhone)));

            if (!phoneMatches)
            {
                return BadRequest(new { message = "Số điện thoại xác thực không khớp với đơn hàng." });
            }
        }

        var timeline = BuildOrderTimeline(order);

        return Ok(new OrderTrackApiResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            OrderStatus = order.OrderStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            CustomerName = order.CustomerName,
            ShippingAddress = order.ShippingAddress,
            City = order.City,
            TrackingNumber = order.TrackingNumber,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            ShippingFee = order.ShippingFee,
            IsGiftWrapping = order.IsGiftWrapping,
            GiftWrappingFee = order.GiftWrappingFee,
            GiftMessage = order.GiftMessage,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new CustomerOrderLineDto
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                ProductName = i.ProductName,
                VariantLabel = i.VariantLabel,
                ProductImageUrl = i.ProductImageUrl,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            Timeline = timeline
        });
    }

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

    // ─── 6. OPENAPI 3.0.1 & SWAGGER UI ENDPOINTS ──────────────────────────────
    [HttpGet("swagger/v1/swagger.json")]
    [AllowAnonymous]
    public IActionResult GetOpenApiSpec()
    {
        var spec = new Dictionary<string, object?>
        {
            ["openapi"] = "3.0.1",
            ["info"] = new Dictionary<string, object?>
            {
                ["title"] = "PERFUME LUXE - Haute Parfumerie RESTful API",
                ["description"] = "Tài liệu đặc tả RESTful API v1 cho hệ thống thương mại điện tử & ERP nước hoa cao cấp PERFUME LUXE.",
                ["version"] = "v1"
            },
            ["servers"] = new object[]
            {
                new Dictionary<string, object?> { ["url"] = "/", ["description"] = "Perfume Luxe API v1 Server" }
            },
            ["paths"] = new Dictionary<string, object?>
            {
                ["/api/v1/catalog/products"] = CreatePathItem("get", "Catalog", "Lấy danh sách nước hoa có phân trang & bộ lọc đa chiều", false, "200"),
                ["/api/v1/catalog/products/{slug}"] = CreatePathItem("get", "Catalog", "Lấy chi tiết chai nước hoa theo đường dẫn slug", false, "200", "404"),
                ["/api/v1/auth/register"] = CreatePathItem("post", "Auth", "Đăng ký tài khoản khách hàng mới", false, "200", "400", "409"),
                ["/api/v1/auth/login"] = CreatePathItem("post", "Auth", "Đăng nhập phiên khách hàng", false, "200", "401"),
                ["/api/v1/auth/token"] = CreatePathItem("post", "Auth", "Cấp cặp JWT Access Token & Refresh Token", false, "200", "401"),
                ["/api/v1/auth/refresh"] = CreatePathItem("post", "Auth", "Làm mới JWT Access Token bằng Refresh Token", false, "200", "401"),
                ["/api/v1/customer/profile"] = CreatePathItem("get", "Customer", "Xem thông tin hồ sơ cá nhân & hạng VIP", true, "200", "401"),
                ["/api/v1/customer/orders"] = CreatePathItem("get", "Customer", "Danh sách đơn hàng của tài khoản đang đăng nhập", true, "200", "401"),
                ["/api/v1/cart"] = CreatePathItem("get", "Cart", "Xem trạng thái giỏ hàng hiện tại", false, "200"),
                ["/api/v1/cart/items"] = CreatePathItem("post", "Cart", "Thêm sản phẩm nước hoa vào giỏ hàng", false, "200", "400", "404"),
                ["/api/v1/vouchers/validate"] = CreatePathItem("post", "Vouchers", "Kiểm tra tính hợp lệ và mức giảm của mã ưu đãi Voucher", false, "200"),
                ["/api/v1/orders"] = CreatePathItem("post", "Orders", "Đặt đơn hàng mới (hỗ trợ gói quà, lời chúc, VietQR, COD)", false, "200", "400"),
                ["/api/v1/orders/track"] = CreatePathItem("get", "Orders", "Tra cứu hành trình đơn hàng theo mã đơn (orderCode)", false, "200", "400", "404"),
                ["/api/v1/flashsales/active"] = CreatePathItem("get", "FlashSales", "Lấy chiến dịch Flash Sale đang hoạt động kèm bộ đếm ngược", false, "200", "404"),
                ["/api/v1/loyalty/status"] = CreatePathItem("get", "Loyalty", "Tra cứu hạng VIP (Bronze/Silver/Gold/Diamond), điểm thưởng & tiến trình nâng hạng", true, "200", "401"),
                ["/api/v1/discovery-box/validate-and-add"] = CreatePathItem("post", "DiscoveryBox", "Xác thực & tính giá hộp quà Discovery Box (3-5 ống chiết 2ml/5ml)", false, "200", "400"),
                ["/api/v1/experience/tasting-booking"] = CreatePathItem("post", "Experience", "Đặt lịch hẹn thử hương Private Tasting tại Boutique", false, "201", "400"),
                ["/api/v1/back-in-stock/subscribe"] = CreatePathItem("post", "Experience", "Đăng ký nhận thông báo khi nước hoa về hàng trở lại", false, "200", "400")
            },
            ["components"] = new Dictionary<string, object?>
            {
                ["securitySchemes"] = new Dictionary<string, object?>
                {
                    ["Bearer"] = new Dictionary<string, object?>
                    {
                        ["type"] = "http",
                        ["scheme"] = "bearer",
                        ["bearerFormat"] = "JWT",
                        ["description"] = "Nhập JWT Bearer token theo định dạng: Bearer {token}"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(spec, new JsonSerializerOptions { WriteIndented = true });
        return Content(json, "application/json; charset=utf-8");
    }

    [HttpGet("swagger")]
    [HttpGet("swagger/index.html")]
    [AllowAnonymous]
    public IActionResult GetSwaggerUi()
    {
        const string html = """
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>PERFUME LUXE - Swagger UI (OpenAPI 3.0.1)</title>
    <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5.11.0/swagger-ui.css" />
    <style>
        body { margin: 0; background: #0d0d11; color: #f5f5f7; }
        .topbar { background: #14141b !important; border-bottom: 1px solid rgba(212,175,55,0.3); }
    </style>
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="https://unpkg.com/swagger-ui-dist@5.11.0/swagger-ui-bundle.js"></script>
    <script>
        window.onload = function() {
            window.ui = SwaggerUIBundle({
                url: "/swagger/v1/swagger.json",
                dom_id: '#swagger-ui',
                deepLinking: true,
                tryItOutEnabled: true,
                persistAuthorization: true
            });
        };
    </script>
</body>
</html>
""";
        return Content(html, "text/html; charset=utf-8");
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────
    public static UserTier CalculateTierFromSpent(decimal totalSpent)
    {
        if (totalSpent >= 30_000_000m) return UserTier.Diamond;
        if (totalSpent >= 15_000_000m) return UserTier.Gold;
        if (totalSpent >= 5_000_000m) return UserTier.Silver;
        return UserTier.Bronze;
    }

    public static (int DiscountPercent, string NextTier, decimal AmountToNextTier) GetTierProgress(UserTier tier, decimal totalSpent)
    {
        return tier switch
        {
            UserTier.Diamond => (12, "Max", 0m),
            UserTier.Gold => (7, "Diamond", Math.Max(0m, 30_000_000m - totalSpent)),
            UserTier.Silver => (3, "Gold", Math.Max(0m, 15_000_000m - totalSpent)),
            _ => (0, "Silver", Math.Max(0m, 5_000_000m - totalSpent))
        };
    }

    private static List<OrderTimelineStepDto> BuildOrderTimeline(Order order)
    {
        var steps = new List<OrderTimelineStepDto>
        {
            new()
            {
                Stage = "Pending",
                Title = "Đặt hàng thành công",
                Completed = true,
                Timestamp = order.CreatedAt
            },
            new()
            {
                Stage = "Confirmed",
                Title = "Đã xác nhận & đóng gói Haute Parfumerie",
                Completed = order.OrderStatus is OrderStatus.Confirmed or OrderStatus.Shipping or OrderStatus.Delivered,
                Timestamp = order.UpdatedAt ?? order.CreatedAt
            },
            new()
            {
                Stage = "Shipping",
                Title = "Đang vận chuyển chuyên biệt",
                Completed = order.OrderStatus is OrderStatus.Shipping or OrderStatus.Delivered,
                Timestamp = order.UpdatedAt
            },
            new()
            {
                Stage = "Delivered",
                Title = "Giao hàng hoàn tất",
                Completed = order.OrderStatus == OrderStatus.Delivered,
                Timestamp = order.UpdatedAt
            }
        };

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            steps.Add(new OrderTimelineStepDto
            {
                Stage = "Cancelled",
                Title = "Đơn hàng đã hủy",
                Completed = true,
                Timestamp = order.UpdatedAt ?? DateTime.UtcNow
            });
        }

        return steps;
    }

    private static Dictionary<string, object?> CreatePathItem(
        string method,
        string tag,
        string summary,
        bool requiresAuth,
        params string[] statusCodes)
    {
        var responses = new Dictionary<string, object?>();
        foreach (var code in statusCodes)
        {
            responses[code] = new Dictionary<string, object?>
            {
                ["description"] = $"HTTP {code} Response"
            };
        }

        var operation = new Dictionary<string, object?>
        {
            ["tags"] = new[] { tag },
            ["summary"] = summary,
            ["responses"] = responses
        };

        if (requiresAuth)
        {
            operation["security"] = new object[]
            {
                new Dictionary<string, object?> { ["Bearer"] = Array.Empty<string>() }
            };
        }

        return new Dictionary<string, object?>
        {
            [method] = operation
        };
    }
}

// ─── API v1 EXTENSION DTOs ────────────────────────────────────────────────────

public sealed class FlashSaleActiveApiResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsOngoing { get; set; }
    public long RemainingSeconds { get; set; }
    public string? BannerUrl { get; set; }
    public List<FlashSaleItemDto> Items { get; set; } = new();
}

public sealed class LoyaltyStatusApiResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Tier { get; set; } = "Bronze";
    public decimal TotalSpent { get; set; }
    public int DiscountPercent { get; set; }
    public int Points { get; set; }
    public string NextTier { get; set; } = "Silver";
    public decimal AmountToNextTier { get; set; }
}

public sealed class DiscoveryBoxApiRequest
{
    public string BoxStyle { get; set; } = "ClassicWood";
    public string? GreetingMessage { get; set; }
    public List<DiscoveryVialApiSelectionDto> Vials { get; set; } = new();
}

public sealed class DiscoveryVialApiSelectionDto
{
    public int ProductId { get; set; }
    public string VialSize { get; set; } = "2ml";
}

public sealed class DiscoveryBoxApiResponse
{
    public bool Valid { get; set; }
    public string BoxStyle { get; set; } = "ClassicWood";
    public int VialCount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? GreetingMessage { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class OrderTrackApiResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public bool IsGiftWrapping { get; set; }
    public decimal GiftWrappingFee { get; set; }
    public string? GiftMessage { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CustomerOrderLineDto> Items { get; set; } = new();
    public List<OrderTimelineStepDto> Timeline { get; set; } = new();
}

public sealed class OrderTimelineStepDto
{
    public string Stage { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTime? Timestamp { get; set; }
}

public sealed class TastingBookingApiRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BoutiqueLocation { get; set; } = "Flagship 88 Tràng Tiền, Hà Nội";
    public DateTime BookingDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);
    public string TimeSlot { get; set; } = "14:00 - 15:30";
    public int GuestCount { get; set; } = 1;
    public string? PreferredScentFamilies { get; set; }
    public string? Notes { get; set; }
}

public sealed class TastingBookingApiResponse
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BoutiqueLocation { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public string? PreferredScentFamilies { get; set; }
    public string Status { get; set; } = "Pending";
}
