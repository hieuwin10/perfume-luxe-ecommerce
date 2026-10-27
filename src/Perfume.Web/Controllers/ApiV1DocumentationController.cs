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
public sealed class ApiV1DocumentationController : ControllerBase
{
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
                ["/api/v1/auth/revoke"] = CreatePathItem("post", "Auth", "Thu hồi refresh token", false, "204"),
                ["/api/v1/auth/logout"] = CreatePathItem("post", "Auth", "Đăng xuất phiên cookie", true, "204", "401"),
                ["/api/v1/auth/password/forgot"] = CreatePathItem("post", "Auth", "Gửi hướng dẫn đặt lại mật khẩu", false, "200"),
                ["/api/v1/auth/password/reset"] = CreatePathItem("post", "Auth", "Đặt lại mật khẩu bằng token", false, "200", "400"),
                ["/api/v1/customer/profile"] = MergePathItems(
                    CreatePathItem("get", "Customer", "Xem thông tin hồ sơ cá nhân & hạng VIP", true, "200", "401"),
                    CreatePathItem("put", "Customer", "Cập nhật hồ sơ cá nhân", true, "200", "400", "401")),
                ["/api/v1/customer/orders"] = CreatePathItem("get", "Customer", "Danh sách đơn hàng của tài khoản đang đăng nhập", true, "200", "401"),
                ["/api/v1/customer/orders/{id}"] = CreatePathItem("get", "Customer", "Xem chi tiết đơn thuộc tài khoản đang đăng nhập", true, "200", "401", "404"),
                ["/api/v1/cart"] = CreatePathItem("get", "Cart", "Xem trạng thái giỏ hàng hiện tại", false, "200"),
                ["/api/v1/cart/items"] = MergePathItems(
                    CreatePathItem("post", "Cart", "Thêm sản phẩm nước hoa vào giỏ hàng", false, "200", "400", "404"),
                    CreatePathItem("put", "Cart", "Cập nhật số lượng sản phẩm trong giỏ", false, "200", "400")),
                ["/api/v1/cart/items/{productId}"] = CreatePathItem("delete", "Cart", "Xóa sản phẩm khỏi giỏ", false, "200"),
                ["/api/v1/cart/merge"] = CreatePathItem("post", "Cart", "Gộp giỏ khách vãng lai sau khi đăng nhập", true, "200", "401"),
                ["/api/v1/vouchers/validate"] = CreatePathItem("post", "Vouchers", "Kiểm tra tính hợp lệ và mức giảm của mã ưu đãi Voucher", false, "200"),
                ["/api/v1/orders"] = CreatePathItem("post", "Orders", "Đặt đơn hàng mới (hỗ trợ gói quà, lời chúc, VietQR, COD)", false, "200", "400"),
                ["/api/v1/orders/guest"] = CreatePathItem("get", "Orders", "Theo dõi đơn khách vãng lai bằng token", false, "200", "404"),
                ["/api/v1/orders/track"] = CreatePathItem("get", "Orders", "Tra cứu hành trình đơn hàng theo mã đơn (orderCode)", false, "200", "400", "404"),
                ["/api/v1/products/{productId}/reviews"] = MergePathItems(
                    CreatePathItem("get", "Reviews", "Xem đánh giá đã được duyệt", false, "200"),
                    CreatePathItem("post", "Reviews", "Gửi đánh giá sản phẩm đã nhận", true, "200", "400", "403")),
                ["/api/v1/store-manager/dashboard"] = CreatePathItem("get", "StoreManager", "Thống kê doanh thu, đơn hàng, tồn kho và voucher", true, "200", "401", "403"),
                ["/api/v1/store-manager/orders"] = CreatePathItem("get", "StoreManager", "Lọc và phân trang đơn hàng", true, "200", "401", "403"),
                ["/api/v1/store-manager/orders/{id}"] = CreatePathItem("get", "StoreManager", "Xem chi tiết đơn hàng", true, "200", "404"),
                ["/api/v1/store-manager/orders/{id}/status"] = CreatePathItem("put", "StoreManager", "Cập nhật trạng thái đơn và hoàn kho khi hủy", true, "200", "401", "403", "409"),
                ["/api/v1/store-manager/orders/{id}/confirm-payment"] = CreatePathItem("post", "StoreManager", "Xác nhận đã thu COD hoặc đối soát chuyển khoản", true, "200", "401", "403", "409"),
                ["/api/v1/store-manager/products"] = MergePathItems(
                    CreatePathItem("get", "StoreManager", "Tìm kiếm và phân trang sản phẩm", true, "200", "401", "403"),
                    CreatePathItem("post", "StoreManager", "Tạo sản phẩm với phiên bản đầu tiên", true, "201", "400", "409")),
                ["/api/v1/store-manager/products/{id}"] = MergePathItems(
                    CreatePathItem("get", "StoreManager", "Xem sản phẩm và các phiên bản", true, "200", "404"),
                    CreatePathItem("put", "StoreManager", "Cập nhật thông tin sản phẩm", true, "200", "400", "404")),
                ["/api/v1/store-manager/products/{id}/availability"] = CreatePathItem("put", "StoreManager", "Cập nhật giá cơ sở và trạng thái bán", true, "200", "409"),
                ["/api/v1/store-manager/products/{productId}/variants"] = CreatePathItem("post", "StoreManager", "Thêm phiên bản và giá bán", true, "201", "400", "409"),
                ["/api/v1/store-manager/variants/{variantId}"] = MergePathItems(
                    CreatePathItem("put", "StoreManager", "Cập nhật phiên bản sản phẩm", true, "200", "400", "409"),
                    CreatePathItem("delete", "StoreManager", "Ẩn phiên bản không còn được sử dụng", true, "204", "404", "409")),
                ["/api/v1/store-manager/inventory"] = CreatePathItem("get", "StoreManager", "Xem và lọc tồn kho theo ngưỡng", true, "200"),
                ["/api/v1/store-manager/inventory/{variantId}"] = CreatePathItem("put", "StoreManager", "Điều chỉnh tồn kho phiên bản", true, "200", "400", "404"),
                ["/api/v1/store-manager/vouchers"] = MergePathItems(
                    CreatePathItem("get", "StoreManager", "Danh sách voucher", true, "200"),
                    CreatePathItem("post", "StoreManager", "Tạo voucher", true, "201", "400", "409")),
                ["/api/v1/store-manager/vouchers/{id}"] = CreatePathItem("put", "StoreManager", "Cập nhật voucher", true, "200", "400", "404", "409"),
                ["/api/v1/store-manager/reviews"] = CreatePathItem("get", "StoreManager", "Danh sách đánh giá và trạng thái duyệt", true, "200"),
                ["/api/v1/store-manager/reviews/{reviewId}/moderation"] = CreatePathItem("put", "StoreManager", "Duyệt hoặc từ chối đánh giá", true, "200", "404"),
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

// ─── HELPERS ──────────────────────────────────────────────────────────────
    private static Dictionary<string, object?> MergePathItems(params Dictionary<string, object?>[] items) =>
        items.SelectMany(item => item).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
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
