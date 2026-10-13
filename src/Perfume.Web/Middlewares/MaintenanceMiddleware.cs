using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;

namespace Perfume.Web.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(RequestDelegate _next, ILogger<MaintenanceMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Luôn cho phép static files và route đăng nhập/đăng xuất để Admin có thể đăng nhập
        if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/images/") ||
            path.StartsWith("/lib/") || path.Equals("/favicon.ico") ||
            path.StartsWith("/account/login") || path.StartsWith("/account/logout") ||
            path.StartsWith("/account/accessdenied"))
        {
            await _next(context);
            return;
        }

        // Lấy cấu hình hệ thống từ ISystemSettingService
        var settingService = context.RequestServices.GetService<ISystemSettingService>();
        if (settingService == null)
        {
            await _next(context);
            return;
        }

        SystemSettingsDto settings;
        try
        {
            settings = await settingService.GetSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch SystemSettings in MaintenanceMiddleware; bypassing.");
            await _next(context);
            return;
        }

        if (!settings.IsMaintenanceMode)
        {
            await _next(context);
            return;
        }

        // 1. ADMIN BYPASS: Cho phép tài khoản Admin đi qua tự do
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Admin"))
        {
            await _next(context);
            return;
        }

        // 2. IP BYPASS: Cho phép các IP trong danh sách trắng đi qua
        if (!string.IsNullOrWhiteSpace(settings.AllowedBypassIps))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var allowedIps = settings.AllowedBypassIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (clientIp != null && allowedIps.Contains(clientIp, StringComparer.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        // 3. CHẶN VÀ TRẢ VỀ 503 SERVICE UNAVAILABLE
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var json = JsonSerializer.Serialize(new
            {
                status = 503,
                error = "Service Unavailable",
                message = settings.MaintenanceMessage,
                expectedEndTime = settings.ExpectedEndTime
            });
            await context.Response.WriteAsync(json);
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        var html = $@"<!DOCTYPE html>
<html lang=""vi"" data-theme=""dark"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Hệ Thống Đang Nâng Cấp | PERFUME LUXE</title>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css"" />
    <style>
        body {{
            margin: 0; padding: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center;
            background: #09090b; color: #f4f4f5; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            text-align: center; box-sizing: border-box;
        }}
        .card {{
            background: #111116; border: 1px solid rgba(212, 175, 55, 0.35); border-radius: 20px;
            padding: 48px 32px; max-width: 580px; width: 90%; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.85);
        }}
        .logo {{
            font-size: 1.4rem; font-weight: 900; letter-spacing: 4px;
            background: linear-gradient(135deg, #FFE082 0%, #D4AF37 50%, #AA7C11 100%);
            -webkit-background-clip: text; -webkit-text-fill-color: transparent; margin-bottom: 20px;
        }}
        .crown {{ font-size: 3rem; color: #D4AF37; margin-bottom: 16px; }}
        h1 {{ font-size: 1.6rem; margin: 0 0 12px; color: #fff; }}
        p {{ color: #a1a1aa; line-height: 1.6; font-size: 0.95rem; margin-bottom: 24px; }}
        .badge {{
            display: inline-block; background: rgba(212,175,55,0.12); border: 1px solid rgba(212,175,55,0.3);
            color: #D4AF37; padding: 6px 16px; border-radius: 99px; font-size: 0.82rem; font-weight: 700; margin-bottom: 24px;
        }}
        .admin-link {{
            color: #71717a; text-decoration: none; font-size: 0.78rem; transition: color 0.2s;
        }}
        .admin-link:hover {{ color: #D4AF37; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""crown""><i class=""fa-solid fa-crown""></i></div>
        <div class=""logo"">PERFUME LUXE</div>
        <div class=""badge""><i class=""fa-solid fa-screwdriver-wrench"" style=""margin-right: 6px;""></i>BẢO TRÌ NÂNG CẤP ĐỊNH KỲ</div>
        <h1>Hệ Thống Đang Nâng Cấp</h1>
        <p>{settings.MaintenanceMessage}</p>
        {(string.IsNullOrWhiteSpace(settings.ExpectedEndTime) ? "" : $"<p style=\"color: #D4AF37; font-weight: 600;\"><i class=\"fa-regular fa-clock\" style=\"margin-right: 6px;\"></i>Dự kiến hoàn tất: {settings.ExpectedEndTime}</p>")}
        <div style=""margin-top: 32px;"">
            <a href=""/account/login"" class=""admin-link""><i class=""fa-solid fa-lock"" style=""margin-right: 4px;""></i>Đăng nhập Quản Trị Viên (Admin Bypass)</a>
        </div>
    </div>
</body>
</html>";
        await context.Response.WriteAsync(html);
    }
}
