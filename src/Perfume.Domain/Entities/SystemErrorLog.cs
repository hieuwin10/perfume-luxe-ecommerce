using System;

namespace Perfume.Domain.Entities;

public class SystemErrorLog : BaseEntity
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public int StatusCode { get; set; } = 500;
    public string? ActorUserId { get; set; }
}
