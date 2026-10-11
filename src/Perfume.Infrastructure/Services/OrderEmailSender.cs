using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfume.Domain.Entities;

namespace Perfume.Infrastructure.Services;

public interface IOrderEmailSender
{
    Task SendOrderConfirmationAsync(Order order, string? trackingUrl);
    Task SendPasswordResetAsync(string email, string fullName, string resetUrl);
}

public sealed class SmtpOrderEmailSender : IOrderEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpOrderEmailSender> _logger;

    public SmtpOrderEmailSender(IConfiguration configuration, ILogger<SmtpOrderEmailSender> logger)
    { _configuration = configuration; _logger = logger; }

    public async Task SendOrderConfirmationAsync(Order order, string? trackingUrl)
    {
        if (!bool.TryParse(_configuration["Smtp:Enabled"], out var enabled) || !enabled) return;
        try
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SMTP host is not configured.");
            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;
            var fromAddress = _configuration["Smtp:FromEmail"] ?? "noreply@perfumeluxe.local";
            var fromName = _configuration["Smtp:FromName"] ?? "PERFUME LUXE";
            var body = $"Xin chào {order.CustomerName},\n\nPerfume Luxe đã nhận đơn {order.OrderCode}. Tổng tiền: {order.TotalAmount:N0} ₫. Phương thức thanh toán: COD.\nCửa hàng sẽ cập nhật đã thu tiền sau khi xác nhận COD.\n" +
                       (string.IsNullOrWhiteSpace(trackingUrl) ? string.Empty : $"\nTheo dõi đơn hàng an toàn tại: {trackingUrl}\n") +
                       "\nCảm ơn quý khách đã mua sắm tại PERFUME LUXE.";
            using var message = new MailMessage(new MailAddress(fromAddress, fromName), new MailAddress(order.CustomerEmail))
            { Subject = $"PERFUME LUXE - Xác nhận đơn hàng {order.OrderCode}", Body = body, IsBodyHtml = false };
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 5000
            };
            var username = _configuration["Smtp:UserName"];
            if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);
            await client.SendMailAsync(message);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Order {OrderCode} was saved but its confirmation email failed.", order.OrderCode); }
    }

    public async Task SendPasswordResetAsync(string email, string fullName, string resetUrl)
    {
        if (!bool.TryParse(_configuration["Smtp:Enabled"], out var enabled) || !enabled) return;
        try
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SMTP host is not configured.");
            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;
            var fromAddress = _configuration["Smtp:FromEmail"] ?? "noreply@perfumeluxe.local";
            var fromName = _configuration["Smtp:FromName"] ?? "PERFUME LUXE";
            using var message = new MailMessage(new MailAddress(fromAddress, fromName), new MailAddress(email))
            {
                Subject = "PERFUME LUXE - Đặt lại mật khẩu",
                Body = $"Xin chào {fullName},\n\nBạn vừa yêu cầu đặt lại mật khẩu. Mở liên kết dưới đây để tạo mật khẩu mới (liên kết chỉ dùng một lần):\n\n{resetUrl}\n\nNếu bạn không yêu cầu, hãy bỏ qua email này.",
                IsBodyHtml = false
            };
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) && ssl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 5000
            };
            var username = _configuration["Smtp:UserName"];
            if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);
            await client.SendMailAsync(message);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Password reset email could not be sent."); }
    }
}
