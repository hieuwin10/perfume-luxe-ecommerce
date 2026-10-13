using Perfume.Application.DTOs;

namespace Perfume.Application.Interfaces;

public interface ISystemSettingService
{
    Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(SystemSettingsDto settings, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> TestSmtpConnectionAsync(string recipientEmail, CancellationToken cancellationToken = default);
}
