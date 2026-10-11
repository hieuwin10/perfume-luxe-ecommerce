using System.ComponentModel.DataAnnotations;

namespace Perfume.Application.DTOs;

public sealed class CustomerProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string Tier { get; set; } = "Bronze";
    public int LoyaltyPoints { get; set; }
}

public sealed class CustomerProfileUpdateRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }
    [StringLength(500)]
    public string? Address { get; set; }
}

public sealed class CustomerRegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class CustomerLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class PasswordResetRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class PasswordResetConfirmRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Token { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ApiTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public CustomerProfileDto Customer { get; set; } = new();
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class CartAddItemRequest
{
    [Required]
    public string ProductSlug { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    [Range(1, 50)]
    public int Quantity { get; set; } = 1;
}

public sealed class CartLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    [Range(0, 50)]
    public int Quantity { get; set; }
}

public sealed class CartStateDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public int TotalCount { get; set; }
}

public sealed class VoucherValidationRequest
{
    [Required, StringLength(80)]
    public string Code { get; set; } = string.Empty;
}

public sealed class VoucherValidationResponse
{
    public bool Valid { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class CustomerOrderDto
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? TrackingNumber { get; set; }
    public List<CustomerOrderLineDto> Items { get; set; } = new();
}

public sealed class CustomerOrderLineDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}

public sealed class CustomerOrderRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;
    [Required, Phone, StringLength(32)]
    public string CustomerPhone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    public string CustomerEmail { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 5)]
    public string ShippingAddress { get; set; } = string.Empty;
    [StringLength(120)]
    public string? City { get; set; }
    [StringLength(1000)]
    public string? OrderNotes { get; set; }
    [StringLength(80)]
    public string? VoucherCode { get; set; }
}

public sealed class GuestOrderResponse
{
    public CustomerOrderDto Order { get; set; } = new();
    public string? TrackingToken { get; set; }
    public string? SuccessUrl { get; set; }
}

public sealed class ReviewCreateRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }
    [Required, StringLength(2000, MinimumLength = 5)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class ProductReviewsResponse
{
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public List<ProductReviewDto> Items { get; set; } = new();
}

public sealed class ProductReviewDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
