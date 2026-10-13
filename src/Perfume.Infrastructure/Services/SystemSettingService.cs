using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class SystemSettingService : ISystemSettingService
{
    private const string SettingsKey = "Store_Configuration";
    private readonly PerfumeDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemSettingService> _logger;

    public SystemSettingService(PerfumeDbContext db, IConfiguration configuration, ILogger<SystemSettingService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var record = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, cancellationToken);
        if (record != null && !string.IsNullOrWhiteSpace(record.Value))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<SystemSettingsDto>(record.Value);
                if (parsed != null) return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize SystemSettings from database; using defaults.");
            }
        }

        // Defaults fallback from configuration or hardcoded
        var fallback = new SystemSettingsDto
        {
            StoreName = _configuration["Store:Name"] ?? "PERFUME LUXE",
            StoreSlogan = _configuration["Store:Slogan"] ?? "Haute Parfumerie & Niche Fragrances",
            Hotline = _configuration["Store:Hotline"] ?? "1900 8888",
            ContactEmail = _configuration["Store:ContactEmail"] ?? "concierge@perfumeluxe.vn",
            BoutiqueAddress = _configuration["Store:Address"] ?? "88 Đồng Khởi, Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            WorkingHours = "08:00 - 22:00 (Tất cả các ngày trong tuần)",
            FreeShippingThreshold = 2_000_000m,
            DefaultShippingFee = 35_000m,
            SmtpEnabled = bool.TryParse(_configuration["Smtp:Enabled"], out var sEnabled) ? sEnabled : true,
            SmtpHost = _configuration["Smtp:Host"] ?? "localhost",
            SmtpPort = int.TryParse(_configuration["Smtp:Port"], out var sPort) ? sPort : 1025,
            SmtpEnableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var sSsl) && sSsl,
            SmtpFromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@perfumeluxe.local",
            SmtpFromName = _configuration["Smtp:FromName"] ?? "PERFUME LUXE",
            SmtpUserName = _configuration["Smtp:UserName"],
            SmtpPassword = _configuration["Smtp:Password"]
        };
        return fallback;
    }

    public async Task SaveSettingsAsync(SystemSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        var record = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == SettingsKey, cancellationToken);
        if (record == null)
        {
            record = new SystemSetting
            {
                Key = SettingsKey,
                Value = json,
                Description = "General Store & Operational Configuration"
            };
            _db.SystemSettings.Add(record);
        }
        else
        {
            record.Value = json;
            record.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message)> TestSmtpConnectionAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        if (!settings.SmtpEnabled)
        {
            return (false, "Dịch vụ gửi email (SMTP) hiện đang bị tắt trong cấu hình.");
        }

        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 7000
            };

            if (!string.IsNullOrWhiteSpace(settings.SmtpUserName))
            {
                client.Credentials = new NetworkCredential(settings.SmtpUserName, settings.SmtpPassword);
            }

            var mail = new MailMessage(new MailAddress(settings.SmtpFromEmail, settings.SmtpFromName), new MailAddress(recipientEmail))
            {
                Subject = $"[PERFUME LUXE] Thử nghiệm kết nối SMTP thành công ({DateTime.UtcNow:HH:mm:ss})",
                Body = $"Xin chào,\n\nĐây là email kiểm tra kết nối từ hệ thống quản trị PERFUME LUXE.\n\nCấu hình đang áp dụng:\n- Host: {settings.SmtpHost}\n- Port: {settings.SmtpPort}\n- SSL: {settings.SmtpEnableSsl}\n- Sender: {settings.SmtpFromEmail}\n\nHệ thống đã sẵn sàng gửi thông báo hóa đơn và xác nhận đơn hàng tự động.",
                IsBodyHtml = false
            };

            await client.SendMailAsync(mail, cancellationToken);
            return (true, $"Đã gửi email thử nghiệm thành công tới {recipientEmail} qua {settings.SmtpHost}:{settings.SmtpPort}!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test failed when sending to {Recipient}", recipientEmail);
            return (false, $"Lỗi kết nối SMTP: {ex.Message}");
        }
    }
}
