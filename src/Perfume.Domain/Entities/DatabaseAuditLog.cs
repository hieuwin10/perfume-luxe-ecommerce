namespace Perfume.Domain.Entities;

public class DatabaseAuditLog : BaseEntity
{
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? CommandHash { get; set; }
    public bool Succeeded { get; set; }
    public int? RowsAffected { get; set; }
    public long ElapsedMilliseconds { get; set; }
}