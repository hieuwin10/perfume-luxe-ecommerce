namespace Perfume.Domain.Entities;

public class Voucher : BaseEntity
{
    public string Code { get; set; } = string.Empty; // LUXE20, NOEL2026...
    public string Description { get; set; } = string.Empty;
    public decimal? DiscountPercent { get; set; } // e.g. 10%
    public decimal? DiscountAmount { get; set; } // e.g. 100.000₫
    public decimal MinOrderAmount { get; set; } = 0;
    public decimal? MaxDiscountAmount { get; set; }
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; } = 0;
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
    public bool IsActive { get; set; } = true;
}
