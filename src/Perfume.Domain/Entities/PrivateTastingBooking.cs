namespace Perfume.Domain.Entities;

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
