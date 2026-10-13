using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════
// PART 3A: CUSTOMER ACCOUNT & IDENTITY SERVICE
// ═══════════════════════════════════════════════════════════════════════════

public sealed class CustomerAccountService : ICustomerAccountService
{
    private readonly PerfumeDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();
    private readonly ILoyaltyTierService _loyaltyService;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneRegex = new(
        @"^[0-9+\-\s()]{8,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public CustomerAccountService(PerfumeDbContext db, ILoyaltyTierService? loyaltyService = null)
    {
        _db = db;
        _loyaltyService = loyaltyService ?? new LoyaltyTierService(db);
    }

    public bool ValidatePasswordPolicy(string? password, out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Mật khẩu không được để trống.");
            return false;
        }

        if (password.Length < 8)
            errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
        if (password.Length > 100)
            errors.Add("Mật khẩu không được vượt quá 100 ký tự.");
        if (!password.Any(char.IsUpper))
            errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).");
        if (!password.Any(char.IsLower))
            errors.Add("Mật khẩu phải chứa ít nhất 1 chữ cái thường (a-z).");
        if (!password.Any(char.IsDigit))
            errors.Add("Mật khẩu phải chứa ít nhất 1 chữ số (0-9).");
        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.");

        return errors.Count == 0;
    }

    public async Task<AccountOperationResultDto> RegisterCustomerAsync(
        CustomerRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AccountOperationResultDto();
        if (request == null)
        {
            result.ErrorMessage = "Yêu cầu đăng ký không hợp lệ.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var fullName = (request.FullName ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (fullName.Length < 2 || fullName.Length > 100)
            result.ValidationErrors.Add("Họ tên phải từ 2 đến 100 ký tự.");

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !EmailRegex.IsMatch(email))
            result.ValidationErrors.Add("Địa chỉ email không hợp lệ.");

        if (!ValidatePasswordPolicy(request.Password, out var passwordErrors))
            result.ValidationErrors.AddRange(passwordErrors);

        if (request.Password != request.ConfirmPassword)
            result.ValidationErrors.Add("Mật khẩu xác nhận không khớp.");

        if (result.ValidationErrors.Count > 0)
        {
            result.ErrorMessage = string.Join(" ", result.ValidationErrors);
            return result;
        }

        var normalizedEmail = email.ToUpperInvariant();
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.Email == email, cancellationToken);
        if (existingUser != null)
        {
            result.ErrorMessage = "Email này đã được sử dụng.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = true,
            FullName = fullName,
            Tier = UserTier.Bronze,
            LoyaltyPoints = 0,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        // Ensure "Customer" role exists and assign it
        var customerRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == "CUSTOMER" || r.Name == "Customer", cancellationToken);
        if (customerRole == null)
        {
            customerRole = new AppRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Customer",
                NormalizedName = "CUSTOMER",
                Description = "Khách hàng thành viên PERFUME LUXE"
            };
            _db.Roles.Add(customerRole);
        }

        _db.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = user.Id,
            RoleId = customerRole.Id
        });

        // Create initial UserLoyalty record
        var loyalty = new UserLoyalty
        {
            UserId = user.Id,
            Points = 0,
            Tier = UserTier.Bronze,
            TotalSpent = 0m
        };
        _db.UserLoyalties.Add(loyalty);

        await _db.SaveChangesAsync(cancellationToken);

        result.Success = true;
        result.Profile = new CustomerProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Tier = user.Tier.ToString(),
            LoyaltyPoints = user.LoyaltyPoints
        };
        return result;
    }

    public async Task<AccountOperationResultDto> AuthenticateCustomerAsync(
        CustomerLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AccountOperationResultDto();
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            result.ErrorMessage = "Vui lòng nhập email và mật khẩu.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var normalizedEmail = email.ToUpperInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail || u.Email == email, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            result.ErrorMessage = "Email hoặc mật khẩu không đúng.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            result.ErrorMessage = "Email hoặc mật khẩu không đúng.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var loyaltyStatus = await _loyaltyService.GetOrSyncUserLoyaltyAsync(user.Id, cancellationToken);

        result.Success = true;
        result.Profile = new CustomerProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Tier = loyaltyStatus.CurrentTier.ToString(),
            LoyaltyPoints = loyaltyStatus.LoyaltyPoints
        };
        return result;
    }

    public async Task<CustomerProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return null;

        var loyaltyStatus = await _loyaltyService.GetOrSyncUserLoyaltyAsync(user.Id, cancellationToken);

        return new CustomerProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Tier = loyaltyStatus.CurrentTier.ToString(),
            LoyaltyPoints = loyaltyStatus.LoyaltyPoints
        };
    }

    public async Task<AccountOperationResultDto> UpdateProfileAsync(
        string userId,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AccountOperationResultDto();
        if (string.IsNullOrWhiteSpace(userId))
        {
            result.ErrorMessage = "Không tìm thấy người dùng.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            result.ErrorMessage = "Không tìm thấy người dùng.";
            result.ValidationErrors.Add(result.ErrorMessage);
            return result;
        }

        var fullName = (request?.FullName ?? string.Empty).Trim();
        var phone = string.IsNullOrWhiteSpace(request?.PhoneNumber) ? null : request.PhoneNumber.Trim();
        var address = string.IsNullOrWhiteSpace(request?.Address) ? null : request.Address.Trim();

        if (fullName.Length < 2 || fullName.Length > 100)
            result.ValidationErrors.Add("Họ tên phải từ 2 đến 100 ký tự.");

        if (phone != null && (phone.Length > 30 || !PhoneRegex.IsMatch(phone)))
            result.ValidationErrors.Add("Số điện thoại không hợp lệ.");

        if (address != null && address.Length > 500)
            result.ValidationErrors.Add("Địa chỉ không được vượt quá 500 ký tự.");

        if (result.ValidationErrors.Count > 0)
        {
            result.ErrorMessage = string.Join(" ", result.ValidationErrors);
            return result;
        }

        user.FullName = fullName;
        user.PhoneNumber = phone;
        user.Address = address;

        await _db.SaveChangesAsync(cancellationToken);

        var loyaltyStatus = await _loyaltyService.GetOrSyncUserLoyaltyAsync(user.Id, cancellationToken);

        result.Success = true;
        result.Profile = new CustomerProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Tier = loyaltyStatus.CurrentTier.ToString(),
            LoyaltyPoints = loyaltyStatus.LoyaltyPoints
        };
        return result;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PART 3B: AUTOMATIC VIP LOYALTY TIER ENGINE SERVICE
// ═══════════════════════════════════════════════════════════════════════════

public sealed class LoyaltyTierService : ILoyaltyTierService
{
    public const decimal SilverThreshold = 5_000_000m;
    public const decimal GoldThreshold = 15_000_000m;
    public const decimal DiamondThreshold = 30_000_000m;
    public const decimal PointsUnitAmount = 10_000m;

    private readonly PerfumeDbContext _db;

    public LoyaltyTierService(PerfumeDbContext db)
    {
        _db = db;
    }

    public UserTier EvaluateTier(decimal cumulativeSpent)
    {
        if (cumulativeSpent >= DiamondThreshold) return UserTier.Diamond;
        if (cumulativeSpent >= GoldThreshold) return UserTier.Gold;
        if (cumulativeSpent >= SilverThreshold) return UserTier.Silver;
        return UserTier.Bronze;
    }

    public decimal GetDiscountPercent(UserTier tier) => tier switch
    {
        UserTier.Diamond => 12m,
        UserTier.Gold => 7m,
        UserTier.Silver => 3m,
        _ => 0m
    };

    public int CalculatePoints(decimal cumulativeSpent)
    {
        if (cumulativeSpent <= 0m) return 0;
        return (int)Math.Floor(cumulativeSpent / PointsUnitAmount);
    }

    public UserTier? GetNextTier(UserTier currentTier) => currentTier switch
    {
        UserTier.Bronze => UserTier.Silver,
        UserTier.Silver => UserTier.Gold,
        UserTier.Gold => UserTier.Diamond,
        UserTier.Diamond => null,
        _ => UserTier.Silver
    };

    public decimal CalculateProgressPercent(decimal cumulativeSpent)
    {
        if (cumulativeSpent <= 0m) return 0m;
        if (cumulativeSpent >= DiamondThreshold) return 100m;

        if (cumulativeSpent < SilverThreshold)
        {
            return Math.Clamp(Math.Round((cumulativeSpent / SilverThreshold) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
        }

        if (cumulativeSpent < GoldThreshold)
        {
            var span = GoldThreshold - SilverThreshold; // 10,000,000
            return Math.Clamp(Math.Round(((cumulativeSpent - SilverThreshold) / span) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
        }

        // Gold toward Diamond
        var goldSpan = DiamondThreshold - GoldThreshold; // 15,000,000
        return Math.Clamp(Math.Round(((cumulativeSpent - GoldThreshold) / goldSpan) * 100m, 2, MidpointRounding.AwayFromZero), 0m, 100m);
    }

    public decimal CalculateAmountToNextTier(decimal cumulativeSpent)
    {
        var sanitized = Math.Max(0m, cumulativeSpent);
        if (sanitized >= DiamondThreshold) return 0m;
        if (sanitized >= GoldThreshold) return DiamondThreshold - sanitized;
        if (sanitized >= SilverThreshold) return GoldThreshold - sanitized;
        return SilverThreshold - sanitized;
    }

    public async Task<LoyaltyStatusDto> GetOrSyncUserLoyaltyAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new LoyaltyStatusDto
            {
                UserId = string.Empty,
                CurrentTier = UserTier.Bronze,
                TierName = UserTier.Bronze.ToString(),
                NextTier = UserTier.Silver,
                NextTierName = UserTier.Silver.ToString(),
                TotalSpent = 0m,
                LoyaltyPoints = 0,
                DiscountPercent = 0m,
                ProgressPercent = 0m,
                AmountToNextTier = SilverThreshold,
                QualifyingOrderCount = 0
            };
        }

        var userOrders = await _db.Orders
            .Where(o => o.UserId == userId && !o.IsDeleted)
            .ToListAsync(cancellationToken);

        var qualifyingOrders = userOrders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled
                     && o.PaymentStatus != PaymentStatus.Failed
                     && o.PaymentStatus != PaymentStatus.Refunded
                     && (o.OrderStatus == OrderStatus.Delivered || o.PaymentStatus == PaymentStatus.Completed))
            .ToList();

        var loyaltyRecord = await _db.UserLoyalties
            .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        decimal cumulativeSpent;
        if (userOrders.Count > 0)
        {
            cumulativeSpent = qualifyingOrders.Sum(o => o.TotalAmount);
        }
        else
        {
            cumulativeSpent = loyaltyRecord?.TotalSpent ?? 0m;
        }

        var evaluatedTier = EvaluateTier(cumulativeSpent);
        var calculatedPoints = CalculatePoints(cumulativeSpent);
        var nextTier = GetNextTier(evaluatedTier);

        if (loyaltyRecord == null)
        {
            loyaltyRecord = new UserLoyalty
            {
                UserId = userId,
                TotalSpent = cumulativeSpent,
                Tier = evaluatedTier,
                Points = calculatedPoints
            };
            _db.UserLoyalties.Add(loyaltyRecord);
        }
        else
        {
            loyaltyRecord.TotalSpent = cumulativeSpent;
            loyaltyRecord.Tier = evaluatedTier;
            loyaltyRecord.Points = calculatedPoints;
        }

        var appUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (appUser != null)
        {
            appUser.Tier = evaluatedTier;
            appUser.LoyaltyPoints = calculatedPoints;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new LoyaltyStatusDto
        {
            UserId = userId,
            CurrentTier = evaluatedTier,
            TierName = evaluatedTier.ToString(),
            NextTier = nextTier,
            NextTierName = nextTier?.ToString(),
            TotalSpent = cumulativeSpent,
            LoyaltyPoints = calculatedPoints,
            DiscountPercent = GetDiscountPercent(evaluatedTier),
            ProgressPercent = CalculateProgressPercent(cumulativeSpent),
            AmountToNextTier = CalculateAmountToNextTier(cumulativeSpent),
            QualifyingOrderCount = qualifyingOrders.Count
        };
    }

    public async Task<OrderVipDiscountPreviewDto> CalculateOrderVipPricingAsync(
        string? userId,
        decimal subTotal,
        decimal voucherDiscount = 0m,
        bool isGiftWrapping = false,
        CancellationToken cancellationToken = default)
    {
        var safeSubTotal = Math.Max(0m, subTotal);
        var safeVoucherDiscount = Math.Clamp(voucherDiscount, 0m, safeSubTotal);

        UserTier appliedTier = UserTier.Bronze;
        decimal vipDiscountPercent = 0m;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var status = await GetOrSyncUserLoyaltyAsync(userId, cancellationToken);
            appliedTier = status.CurrentTier;
            vipDiscountPercent = status.DiscountPercent;
        }

        var vipDiscountAmount = Math.Round(safeSubTotal * (vipDiscountPercent / 100m), 0, MidpointRounding.AwayFromZero);
        if (vipDiscountAmount + safeVoucherDiscount > safeSubTotal)
        {
            vipDiscountAmount = Math.Max(0m, safeSubTotal - safeVoucherDiscount);
        }

        var giftWrappingFee = isGiftWrapping ? 50_000m : 0m;
        var shippingFee = safeSubTotal == 0m ? 0m : (safeSubTotal >= 2_000_000m ? 0m : 35_000m);
        var finalTotal = Math.Max(0m, safeSubTotal - safeVoucherDiscount - vipDiscountAmount + shippingFee + giftWrappingFee);

        return new OrderVipDiscountPreviewDto
        {
            UserId = userId,
            AppliedTier = appliedTier,
            VipDiscountPercent = vipDiscountPercent,
            SubTotal = safeSubTotal,
            VipDiscountAmount = vipDiscountAmount,
            VoucherDiscountAmount = safeVoucherDiscount,
            ShippingFee = shippingFee,
            GiftWrappingFee = giftWrappingFee,
            FinalTotalAmount = finalTotal,
            EarnedPointsOnCompletion = CalculatePoints(finalTotal)
        };
    }

    public async Task<Order> ApplyVipLoyaltyToOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));

        if (order.SubTotal <= 0m && order.Items.Count > 0)
        {
            order.SubTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        }

        var preview = await CalculateOrderVipPricingAsync(
            order.UserId,
            order.SubTotal,
            order.DiscountAmount,
            order.IsGiftWrapping,
            cancellationToken);

        order.AppliedLoyaltyTier = preview.AppliedTier;
        order.LoyaltyDiscountAmount = preview.VipDiscountAmount;
        order.GiftWrappingFee = preview.GiftWrappingFee;
        order.ShippingFee = preview.ShippingFee;
        order.TotalAmount = preview.FinalTotalAmount;

        return order;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PART 4: 3-STEP DISCOVERY GIFT BOX BUILDER SERVICE
// ═══════════════════════════════════════════════════════════════════════════

public sealed class DiscoveryBoxService : IDiscoveryBoxService
{
    public const int MinVialCount = 3;
    public const int MaxVialCount = 5;
    public const int MaxGreetingMessageLength = 250;

    private static readonly IReadOnlyList<DiscoveryBoxStyleOptionDto> BoxStyles = new List<DiscoveryBoxStyleOptionDto>
    {
        new()
        {
            Code = "ClassicWood",
            Name = "Hộp Gỗ Sơn Mài Hoàng Gia",
            Description = "Chế tác từ gỗ óc chó phủ sơn mài bóng, lót lụa tơ tằm vàng kim.",
            Price = 250_000m
        },
        new()
        {
            Code = "RoyalVelvet",
            Name = "Hộp Nhung Đen Huyền Bí",
            Description = "Hộp nhung đen tuyền thắt nơ ruy-băng lụa mạ vàng 24K.",
            Price = 200_000m
        },
        new()
        {
            Code = "ArtisanLeather",
            Name = "Túi Da Thủ Công Viền Vàng",
            Description = "Bao da thủ công khâu tay tinh xảo, tiện lợi mang theo du lịch.",
            Price = 180_000m
        }
    };

    private static readonly IReadOnlyList<string> AllowedFonts = new List<string>
    {
        "Playfair Display",
        "Dancing Script",
        "Cormorant Garamond"
    };

    private readonly PerfumeDbContext _db;
    private readonly ILoyaltyTierService _loyaltyService;

    public DiscoveryBoxService(PerfumeDbContext db, ILoyaltyTierService? loyaltyService = null)
    {
        _db = db;
        _loyaltyService = loyaltyService ?? new LoyaltyTierService(db);
    }

    public IReadOnlyList<DiscoveryBoxStyleOptionDto> GetAvailableBoxStyles() => BoxStyles;

    public IReadOnlyList<string> GetAllowedFonts() => AllowedFonts;

    public decimal CalculateSampleVialPrice(decimal productBasePrice, int volumeMl)
    {
        if (volumeMl != 2 && volumeMl != 5)
            throw new ArgumentOutOfRangeException(nameof(volumeMl), "Dung tích ống mẫu chỉ hỗ trợ 2ml hoặc 5ml.");

        var safeBasePrice = Math.Max(0m, productBasePrice);
        if (volumeMl == 2)
        {
            var proportional = Math.Round((safeBasePrice * 0.03m) / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
            return Math.Max(120_000m, proportional);
        }
        else
        {
            var proportional = Math.Round((safeBasePrice * 0.06m) / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
            return Math.Max(250_000m, proportional);
        }
    }

    public async Task<DiscoveryBoxValidationResultDto> ValidateAndBuildConfigurationAsync(
        DiscoveryBoxBuildRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = new DiscoveryBoxValidationResultDto();

        if (request == null)
        {
            result.Errors.Add("Yêu cầu tạo hộp quà Discovery Box không được để trống.");
            result.ErrorMessage = result.Errors[0];
            return result;
        }

        // Step 1: Validate Box Style
        var styleCode = (request.BoxStyleCode ?? string.Empty).Trim();
        var boxStyle = BoxStyles.FirstOrDefault(s => string.Equals(s.Code, styleCode, StringComparison.Ordinal));
        if (boxStyle == null)
        {
            result.Errors.Add("Kiểu hộp quà không hợp lệ. Vui lòng chọn ClassicWood, RoyalVelvet hoặc ArtisanLeather.");
        }

        // Step 2: Validate Sample Vials (3 to 5 vials, 2ml or 5ml, valid & in-stock products)
        var vials = request.Vials ?? new List<DiscoveryVialSelectionDto>();
        if (vials.Count < MinVialCount)
        {
            result.Errors.Add($"Hộp Discovery Box yêu cầu chọn tối thiểu {MinVialCount} ống mẫu (hiện tại: {vials.Count}).");
        }
        else if (vials.Count > MaxVialCount)
        {
            result.Errors.Add($"Hộp Discovery Box chỉ cho phép tối đa {MaxVialCount} ống mẫu (hiện tại: {vials.Count}).");
        }

        foreach (var vial in vials)
        {
            if (vial.VolumeMl != 2 && vial.VolumeMl != 5)
            {
                result.Errors.Add($"Dung tích ống mẫu {vial.VolumeMl}ml không hợp lệ. Chỉ chấp nhận 2ml hoặc 5ml.");
            }
            if (vial.ProductId <= 0)
            {
                result.Errors.Add("Mã sản phẩm nước hoa không hợp lệ.");
            }
        }

        // Step 3: Validate Greeting Card & Typography
        var trimmedMessage = string.IsNullOrWhiteSpace(request.GreetingMessage)
            ? null
            : request.GreetingMessage.Trim();
        if (trimmedMessage != null && trimmedMessage.Length > MaxGreetingMessageLength)
        {
            result.Errors.Add($"Lời chúc trên thiệp ép kim không được vượt quá {MaxGreetingMessageLength} ký tự (hiện tại: {trimmedMessage.Length}).");
        }

        var trimmedRecipient = string.IsNullOrWhiteSpace(request.RecipientName)
            ? null
            : request.RecipientName.Trim();
        if (trimmedRecipient != null && trimmedRecipient.Length > 100)
        {
            result.Errors.Add("Tên người nhận không được vượt quá 100 ký tự.");
        }

        var font = string.IsNullOrWhiteSpace(request.GreetingFont)
            ? "Playfair Display"
            : request.GreetingFont.Trim();
        if (!AllowedFonts.Contains(font, StringComparer.Ordinal))
        {
            result.Errors.Add($"Font chữ '{font}' không được hỗ trợ. Vui lòng chọn Playfair Display, Dancing Script hoặc Cormorant Garamond.");
        }

        if (request.Quantity < 1 || request.Quantity > 20)
        {
            result.Errors.Add("Số lượng hộp quà phải từ 1 đến 20.");
        }

        if (result.Errors.Count > 0)
        {
            result.ErrorMessage = string.Join(" ", result.Errors);
            return result;
        }

        // Verify products in DB
        var productIds = vials.Select(v => v.ProductId).Distinct().ToList();
        var productsDict = await _db.Products
            .Include(p => p.Brand)
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var resolvedVials = new List<DiscoveryVialResolvedItemDto>();
        for (int i = 0; i < vials.Count; i++)
        {
            var selection = vials[i];
            if (!productsDict.TryGetValue(selection.ProductId, out var product))
            {
                result.Errors.Add($"Sản phẩm ID #{selection.ProductId} không tồn tại hoặc đã bị xóa.");
                continue;
            }

            if (!product.IsInStock)
            {
                result.Errors.Add($"Sản phẩm '{product.Name}' hiện đang hết hàng.");
                continue;
            }

            var vialPrice = CalculateSampleVialPrice(product.BasePrice, selection.VolumeMl);
            resolvedVials.Add(new DiscoveryVialResolvedItemDto
            {
                SlotIndex = i + 1,
                ProductId = product.Id,
                ProductName = product.Name,
                BrandName = product.Brand?.Name ?? "PERFUME LUXE",
                VolumeMl = selection.VolumeMl,
                ProductBasePrice = product.BasePrice,
                VialPrice = vialPrice
            });
        }

        if (result.Errors.Count > 0)
        {
            result.ErrorMessage = string.Join(" ", result.Errors);
            return result;
        }

        var vialsTotal = resolvedVials.Sum(v => v.VialPrice);
        var totalBoxUnitPrice = boxStyle!.Price + vialsTotal;

        var config = new DiscoveryBoxConfigurationDto
        {
            BoxStyleCode = boxStyle.Code,
            BoxStyleName = boxStyle.Name,
            BoxPrice = boxStyle.Price,
            Vials = resolvedVials,
            VialsTotalPrice = vialsTotal,
            TotalBoxUnitPrice = totalBoxUnitPrice,
            RecipientName = trimmedRecipient,
            GreetingMessage = trimmedMessage,
            GreetingFont = font
        };

        config.ConfigJson = JsonSerializer.Serialize(new
        {
            boxStyleCode = config.BoxStyleCode,
            boxStyleName = config.BoxStyleName,
            boxPrice = config.BoxPrice,
            vialsTotalPrice = config.VialsTotalPrice,
            totalBoxUnitPrice = config.TotalBoxUnitPrice,
            recipientName = config.RecipientName,
            greetingMessage = config.GreetingMessage,
            greetingFont = config.GreetingFont,
            vials = config.Vials.Select(v => new
            {
                slot = v.SlotIndex,
                productId = v.ProductId,
                productName = v.ProductName,
                brandName = v.BrandName,
                volumeMl = v.VolumeMl,
                vialPrice = v.VialPrice
            })
        });

        result.IsValid = true;
        result.Configuration = config;
        return result;
    }

    public async Task<(bool Success, string? Error, ShoppingCartItem? CartItem)> AddDiscoveryBoxToCartAsync(
        string? userId,
        string? guestToken,
        DiscoveryBoxBuildRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var ownerKey = GetOwnerKey(userId, guestToken);
        if (ownerKey == null)
        {
            return (false, "Không xác định được phiên giỏ hàng của khách hàng.", null);
        }

        var validation = await ValidateAndBuildConfigurationAsync(request, cancellationToken);
        if (!validation.IsValid || validation.Configuration == null)
        {
            return (false, validation.ErrorMessage, null);
        }

        var config = validation.Configuration;

        var cart = await _db.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey, cancellationToken);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                OwnerKey = ownerKey,
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            _db.ShoppingCarts.Add(cart);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            cart.ExpiresAt = DateTime.UtcNow.AddDays(30);
        }

        var cartItem = new ShoppingCartItem
        {
            ShoppingCartId = cart.Id,
            ProductId = config.Vials[0].ProductId,
            VariantId = null,
            Quantity = request.Quantity,
            IsCustomBox = true,
            CustomBoxConfigJson = config.ConfigJson,
            CustomUnitPrice = config.TotalBoxUnitPrice
        };

        cart.Items.Add(cartItem);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, cartItem);
    }

    public async Task<(bool Success, string? Error, Order? Order)> PlaceDiscoveryBoxOrderAsync(
        string? userId,
        CheckoutRequestDto checkoutRequest,
        DiscoveryBoxBuildRequestDto boxRequest,
        CancellationToken cancellationToken = default)
    {
        if (checkoutRequest == null ||
            string.IsNullOrWhiteSpace(checkoutRequest.CustomerName) ||
            string.IsNullOrWhiteSpace(checkoutRequest.CustomerPhone) ||
            string.IsNullOrWhiteSpace(checkoutRequest.CustomerEmail) ||
            string.IsNullOrWhiteSpace(checkoutRequest.ShippingAddress))
        {
            return (false, "Vui lòng nhập đầy đủ thông tin người nhận hàng.", null);
        }

        var validation = await ValidateAndBuildConfigurationAsync(boxRequest, cancellationToken);
        if (!validation.IsValid || validation.Configuration == null)
        {
            return (false, validation.ErrorMessage, null);
        }

        var config = validation.Configuration;
        var now = DateTime.UtcNow;

        var order = new Order
        {
            OrderCode = $"DBOX-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            CustomerName = checkoutRequest.CustomerName.Trim(),
            CustomerPhone = checkoutRequest.CustomerPhone.Trim(),
            CustomerEmail = checkoutRequest.CustomerEmail.Trim(),
            ShippingAddress = checkoutRequest.ShippingAddress.Trim(),
            City = checkoutRequest.City?.Trim(),
            OrderNotes = checkoutRequest.OrderNotes?.Trim(),
            PaymentMethod = checkoutRequest.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            OrderStatus = OrderStatus.Pending,
            IsGiftWrapping = checkoutRequest.IsGiftWrapping,
            GiftMessage = checkoutRequest.GiftMessage?.Trim() ?? config.GreetingMessage
        };

        var vialSummary = string.Join(" + ", config.Vials.Select(v => $"{v.ProductName} ({v.VolumeMl}ml)"));
        var orderItem = new OrderItem
        {
            ProductId = config.Vials[0].ProductId,
            VariantId = null,
            ProductName = $"Discovery Box ({config.BoxStyleName})",
            VariantLabel = $"{config.Vials.Count} ống mẫu: {vialSummary}",
            UnitPrice = config.TotalBoxUnitPrice,
            Quantity = boxRequest.Quantity,
            IsCustomBox = true,
            CustomBoxConfigJson = config.ConfigJson
        };

        order.Items.Add(orderItem);
        order.SubTotal = orderItem.UnitPrice * orderItem.Quantity;

        await _loyaltyService.ApplyVipLoyaltyToOrderAsync(order, cancellationToken);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, order);
    }

    private static string? GetOwnerKey(string? userId, string? guestToken)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return "user:" + userId;
        if (string.IsNullOrWhiteSpace(guestToken)) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(guestToken));
        return "guest:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
