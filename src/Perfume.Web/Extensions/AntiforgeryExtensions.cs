using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Perfume.Web.Extensions;

/// <summary>
/// Tiện ích hỗ trợ kiểm tra token chống giả mạo yêu cầu (Antiforgery) và xác thực Bearer token.
/// </summary>
public static class AntiforgeryExtensions
{
    /// <summary>
    /// Kiểm tra hợp lệ cookie write request hoặc bỏ qua nếu request mang Bearer token.
    /// </summary>
    public static async Task<bool> ValidateCookieWriteAsync(this IAntiforgery antiforgery, HttpContext httpContext)
    {
        if (httpContext.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra hợp lệ cookie write request từ ControllerBase.
    /// </summary>
    public static Task<bool> ValidateCookieWriteAsync(this ControllerBase controller, IAntiforgery antiforgery)
    {
        return antiforgery.ValidateCookieWriteAsync(controller.HttpContext);
    }
}
