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
