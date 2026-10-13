namespace Perfume.Application.DTOs;

public class SystemSettingsDto
{
    public string StoreName { get; set; } = "PERFUME LUXE";
    public string StoreSlogan { get; set; } = "Haute Parfumerie & Niche Fragrances";
    public string Hotline { get; set; } = "1900 8888";
    public string ContactEmail { get; set; } = "concierge@perfumeluxe.vn";
    public string BoutiqueAddress { get; set; } = "88 Đồng Khởi, Bến Nghé, Quận 1, TP. Hồ Chí Minh";
    public string WorkingHours { get; set; } = "08:00 - 22:00 (Tất cả các ngày trong tuần)";
    public decimal FreeShippingThreshold { get; set; } = 2000000;
    public decimal DefaultShippingFee { get; set; } = 35000;
    public bool SmtpEnabled { get; set; } = true;
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public bool SmtpEnableSsl { get; set; } = false;
    public string SmtpFromEmail { get; set; } = "noreply@perfumeluxe.local";
    public string SmtpFromName { get; set; } = "PERFUME LUXE";
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }

    // Enterprise Maintenance Mode Settings
    public bool IsMaintenanceMode { get; set; } = false;
    public string MaintenanceMessage { get; set; } = "PerfumeLuxe đang nâng cấp trải nghiệm Hoàng Gia. Hệ thống sẽ sớm mở lại.";
    public string? ExpectedEndTime { get; set; }
    public string? AllowedBypassIps { get; set; }
}
