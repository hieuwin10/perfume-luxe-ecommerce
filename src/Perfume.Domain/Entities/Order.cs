using Perfume.Domain.Enums;

namespace Perfume.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty; // PF-20261010-XXXX
    public string? UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? OrderNotes { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal LoyaltyDiscountAmount { get; set; } = 0;
    public UserTier? AppliedLoyaltyTier { get; set; }
    public decimal ShippingFee { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    public string? VoucherCode { get; set; }
    public string? TrackingNumber { get; set; }

    public bool IsGiftWrapping { get; set; } = false;
    public decimal GiftWrappingFee { get; set; } = 0;
    public string? GiftMessage { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
