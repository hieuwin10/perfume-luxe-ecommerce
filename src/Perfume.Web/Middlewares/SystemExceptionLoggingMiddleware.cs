using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Middlewares;

public class SystemExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SystemExceptionLoggingMiddleware> _logger;

    public SystemExceptionLoggingMiddleware(RequestDelegate _next, ILogger<SystemExceptionLoggingMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var errorCode = $"ERR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
            _logger.LogError(ex, "Unhandled system exception captured [{ErrorCode}] at {Path}", errorCode, context.Request.Path);

            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetService<PerfumeDbContext>();
                if (db != null)
                {
                    var log = new SystemErrorLog
                    {
                        ErrorCode = errorCode,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        RequestPath = context.Request.Path.Value,
                        HttpMethod = context.Request.Method,
                        ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers["User-Agent"].ToString(),
                        StatusCode = 500,
                        ActorUserId = context.User.Identity?.Name,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.SystemErrorLogs.Add(log);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to persist SystemErrorLog to database.");
            }

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    var json = JsonSerializer.Serialize(new
                    {
                        errorCode,
                        status = 500,
                        error = "Internal Server Error",
                        message = "Đã xảy ra sự cố kỹ thuật máy chủ. Vui lòng gửi mã lỗi này cho Ban Quản Trị để được hỗ trợ tức thì."
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
    <title>Sự Cố Kỹ Thuật | PERFUME LUXE</title>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css"" />
    <style>
        body {{
            margin: 0; padding: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center;
            background: #09090b; color: #f4f4f5; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            text-align: center; box-sizing: border-box;
        }}
        .card {{
            background: #111116; border: 1px solid rgba(239, 68, 68, 0.35); border-radius: 20px;
            padding: 48px 32px; max-width: 580px; width: 90%; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.85);
        }}
        .err-icon {{ font-size: 3rem; color: #ef4444; margin-bottom: 16px; }}
        h1 {{ font-size: 1.5rem; margin: 0 0 12px; color: #fff; }}
        p {{ color: #a1a1aa; line-height: 1.6; font-size: 0.95rem; margin-bottom: 24px; }}
        .code-box {{
            background: rgba(0,0,0,0.5); border: 1px dashed rgba(239,68,68,0.5); padding: 12px 18px;
            border-radius: 8px; font-family: monospace; font-size: 1.1rem; color: #f87171; letter-spacing: 1px;
            margin-bottom: 24px; display: inline-block;
        }}
        .btn {{
            display: inline-block; padding: 10px 24px; border-radius: 8px; font-size: 0.88rem; font-weight: 700;
            background: linear-gradient(135deg, #FFE082 0%, #D4AF37 50%, #AA7C11 100%); color: #000; text-decoration: none;
        }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""err-icon""><i class=""fa-solid fa-triangle-exclamation""></i></div>
        <h1>Đã Xảy Ra Sự Cố Máy Chủ</h1>
        <p>Hệ thống đã tự động ghi nhận nhật ký lỗi kỹ thuật. Vui lòng cung cấp mã tham chiếu dưới đây khi liên hệ hỗ trợ:</p>
        <div class=""code-box"">{errorCode}</div>
        <div>
            <a href=""/"" class=""btn""><i class=""fa-solid fa-house"" style=""margin-right: 6px;""></i>Về Trang Chủ</a>
        </div>
    </div>
</body>
</html>";
                await context.Response.WriteAsync(html);
            }
        }
    }
}
