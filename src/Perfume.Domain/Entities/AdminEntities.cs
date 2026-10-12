namespace Perfume.Domain.Entities;

public class HeroBanner : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? TargetUrl { get; set; } = "/bo-suu-tap";
    public string? ButtonText { get; set; } = "Khám Phá Ngay";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class StockAdjustment : BaseEntity
{
    public int VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public int OldQuantity { get; set; }
    public int AdjustedQuantity { get; set; } // +10 or -2
    public int NewQuantity { get; set; }

    public decimal? UnitCost { get; set; } // Giá vốn nhập kho
    public string Reason { get; set; } = "Nhập hàng mới"; // Nhập hàng mới, Trả hàng, Kiểm kê sai lệch...
    public string? Note { get; set; }
    public string? PerformedByUserId { get; set; }
    public string? PerformedByName { get; set; }
}

public class PrivateTastingBooking : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BoutiqueLocation { get; set; } = "Flagship Tràng Tiền, Hà Nội"; // Tràng Tiền or Đồng Khởi
    public DateTime BookingDate { get; set; }
    public string TimeSlot { get; set; } = "14:00 - 15:30";
    public int GuestCount { get; set; } = 1;
    public string? PreferredScentFamilies { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
}

public class BackInStockAlert : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? VariantId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public bool IsNotified { get; set; } = false;
    public DateTime? NotifiedAt { get; set; }
}
