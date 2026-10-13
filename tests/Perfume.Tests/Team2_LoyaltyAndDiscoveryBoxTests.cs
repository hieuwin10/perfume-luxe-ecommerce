using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;
using Xunit;

namespace Perfume.Tests;

public class Team2_LoyaltyAndDiscoveryBoxTests
{
    private static PerfumeDbContext CreateIsolatedDbContext()
    {
        var options = new DbContextOptionsBuilder<PerfumeDbContext>()
            .UseInMemoryDatabase(databaseName: $"Team2_PerfumeTestDb_{Guid.NewGuid():N}")
            .Options;

        return new PerfumeDbContext(options);
    }

    private static async Task<List<Product>> SeedLuxuryPerfumesAsync(PerfumeDbContext db, int count = 6)
    {
        var brand = new Brand
        {
            Name = "Maison Francis Kurkdjian",
            Slug = $"mfk-{Guid.NewGuid():N}"[..12],
            Country = "France",
            IsFeatured = true
        };
        var category = new Category
        {
            Name = "Niche Extrait",
            Slug = $"niche-{Guid.NewGuid():N}"[..12],
            IsFeatured = true
        };
        db.Brands.Add(brand);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var basePrices = new decimal[] { 3_500_000m, 5_000_000m, 6_500_000m, 8_000_000m, 10_000_000m, 12_500_000m, 4_200_000m, 7_500_000m };
        var products = new List<Product>();

        for (int i = 0; i < count; i++)
        {
            var p = new Product
            {
                Name = $"Tuyệt Phẩm Hương Niche #{i + 1}",
                Slug = $"tuyet-pham-niche-{i + 1}-{Guid.NewGuid():N}"[..24],
                Sku = $"SKU-T2-{i + 1}-{Guid.NewGuid():N}"[..16],
                BrandId = brand.Id,
                CategoryId = category.Id,
                BasePrice = basePrices[i % basePrices.Length],
                IsInStock = true,
                Concentration = Concentration.Parfum,
                Gender = GenderTarget.Unisex
            };
            products.Add(p);
            db.Products.Add(p);
        }

        await db.SaveChangesAsync();
        return products;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PART 3: CUSTOMER ACCOUNT & AUTOMATIC VIP LOYALTY TIER ENGINE (TC-T2-001..050)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "TC-T2-001: RegisterCustomerAsync creates AppUser, Customer role, and Bronze UserLoyalty")]
    public async Task TC_T2_001_RegisterCustomer_ValidInput_CreatesUserRoleAndBronzeLoyalty()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var result = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Nguyễn Hoàng Gia Bảo",
            Email = "giabao.vip@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Profile);
        Assert.Equal("Nguyễn Hoàng Gia Bảo", result.Profile!.FullName);
        Assert.Equal("giabao.vip@parfumluxe.vn", result.Profile.Email);
        Assert.Equal("Bronze", result.Profile.Tier);
        Assert.Equal(0, result.Profile.LoyaltyPoints);

        var loyalty = await db.UserLoyalties.FirstOrDefaultAsync(l => l.UserId == result.Profile.Id);
        Assert.NotNull(loyalty);
        Assert.Equal(UserTier.Bronze, loyalty!.Tier);
        Assert.Equal(0m, loyalty.TotalSpent);
        Assert.Equal(0, loyalty.Points);

        var userRole = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == result.Profile.Id);
        Assert.NotNull(userRole);
    }

    [Fact(DisplayName = "TC-T2-002: RegisterCustomerAsync rejects duplicate email case-insensitively")]
    public async Task TC_T2_002_RegisterCustomer_DuplicateEmail_ReturnsValidationError()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var first = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Khách Hàng Đầu Tiên",
            Email = "vipmember@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });
        Assert.True(first.Success);

        var duplicate = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Khách Hàng Thứ Hai",
            Email = "VIPMEMBER@PARFUMLUXE.VN",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        Assert.False(duplicate.Success);
        Assert.Contains("Email này đã được sử dụng", duplicate.ErrorMessage);
    }

    [Theory(DisplayName = "TC-T2-003..006: RegisterCustomerAsync rejects invalid FullName boundaries")]
    [InlineData("", "TC-T2-003: Empty FullName")]
    [InlineData("   ", "TC-T2-004: Whitespace-only FullName")]
    [InlineData("A", "TC-T2-005: Single-char FullName (< 2 chars)")]
    [InlineData("NguyenVanA_012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", "TC-T2-006: 101-char FullName (> 100 chars)")]
    public async Task TC_T2_003_to_006_RegisterCustomer_InvalidFullNameBoundaries_Rejected(string invalidName, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var result = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = invalidName,
            Email = "testname@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        Assert.False(result.Success, scenarioNote);
        Assert.NotEmpty(result.ValidationErrors);
    }

    [Theory(DisplayName = "TC-T2-007..009: RegisterCustomerAsync rejects invalid Email formats")]
    [InlineData("", "TC-T2-007: Empty Email")]
    [InlineData("plainaddress-without-at-symbol", "TC-T2-008: Missing @ symbol")]
    [InlineData("missingdomain@", "TC-T2-009: Missing domain part")]
    public async Task TC_T2_007_to_009_RegisterCustomer_InvalidEmail_Rejected(string invalidEmail, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var result = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Trần Minh Quân",
            Email = invalidEmail,
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        Assert.False(result.Success, scenarioNote);
    }

    [Theory(DisplayName = "TC-T2-010..014: ValidatePasswordPolicy enforces complexity rules")]
    [InlineData("Short1!", false, "TC-T2-010: Less than 8 characters")]
    [InlineData("alllowercase123!", false, "TC-T2-011: Missing uppercase letter")]
    [InlineData("ALLUPPERCASE123!", false, "TC-T2-012: Missing lowercase letter")]
    [InlineData("NoDigitsPassword!", false, "TC-T2-013: Missing numeric digit")]
    [InlineData("NoSpecialChar2026", false, "TC-T2-014: Missing special character")]
    public void TC_T2_010_to_014_ValidatePasswordPolicy_EnforcesSecurityRules(string password, bool expectedValid, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var isValid = accountService.ValidatePasswordPolicy(password, out var errors);

        Assert.Equal(expectedValid, isValid);
        Assert.True(errors.Count > 0, scenarioNote);
    }

    [Fact(DisplayName = "TC-T2-015: RegisterCustomerAsync rejects mismatched ConfirmPassword")]
    public async Task TC_T2_015_RegisterCustomer_PasswordMismatch_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var result = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Lê Hoàng Châu",
            Email = "hoangchau@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "DifferentPassword@2026"
        });

        Assert.False(result.Success);
        Assert.Contains("Mật khẩu xác nhận không khớp", result.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-016: AuthenticateCustomerAsync succeeds with valid credentials and syncs loyalty")]
    public async Task TC_T2_016_AuthenticateCustomer_ValidCredentials_ReturnsSyncedProfile()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var reg = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Phạm Quốc Vương",
            Email = "quocvuong@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        db.Orders.Add(new Order
        {
            OrderCode = "ORD-LOGIN-SYNC-01",
            UserId = reg.Profile!.Id,
            CustomerName = "Phạm Quốc Vương",
            CustomerPhone = "0901234567",
            CustomerEmail = "quocvuong@parfumluxe.vn",
            ShippingAddress = "88 Tràng Tiền, Hà Nội",
            TotalAmount = 16_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed
        });
        await db.SaveChangesAsync();

        var loginResult = await accountService.AuthenticateCustomerAsync(new CustomerLoginRequest
        {
            Email = "QUOCVUONG@parfumluxe.vn",
            Password = "RoyalPassword@2026"
        });

        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Profile);
        Assert.Equal("Gold", loginResult.Profile!.Tier);
        Assert.Equal(1600, loginResult.Profile.LoyaltyPoints);
    }

    [Fact(DisplayName = "TC-T2-017: AuthenticateCustomerAsync rejects wrong password or unknown email")]
    public async Task TC_T2_017_AuthenticateCustomer_WrongPasswordOrUnknownEmail_Fails()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Đặng Thu Thảo",
            Email = "thuthao@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        var wrongPass = await accountService.AuthenticateCustomerAsync(new CustomerLoginRequest
        {
            Email = "thuthao@parfumluxe.vn",
            Password = "WrongPassword@999"
        });
        var unknownUser = await accountService.AuthenticateCustomerAsync(new CustomerLoginRequest
        {
            Email = "nobody@parfumluxe.vn",
            Password = "RoyalPassword@2026"
        });

        Assert.False(wrongPass.Success);
        Assert.False(unknownUser.Success);
    }

    [Fact(DisplayName = "TC-T2-018: UpdateProfileAsync trims and updates FullName, PhoneNumber, and Address")]
    public async Task TC_T2_018_UpdateProfile_ValidFields_TrimsAndPersists()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var reg = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Vũ Minh Khôi",
            Email = "minhkhoi@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        var update = await accountService.UpdateProfileAsync(reg.Profile!.Id, new CustomerProfileUpdateRequest
        {
            FullName = "  Vũ Minh Khôi VIP  ",
            PhoneNumber = " 0988123456 ",
            Address = "  Biệt thự Vinhomes Riverside, Long Biên, Hà Nội  "
        });

        Assert.True(update.Success);
        Assert.Equal("Vũ Minh Khôi VIP", update.Profile!.FullName);
        Assert.Equal("0988123456", update.Profile.PhoneNumber);
        Assert.Equal("Biệt thự Vinhomes Riverside, Long Biên, Hà Nội", update.Profile.Address);
    }

    [Fact(DisplayName = "TC-T2-019: UpdateProfileAsync rejects invalid phone number or overlong address")]
    public async Task TC_T2_019_UpdateProfile_InvalidPhoneOrLongAddress_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var accountService = new CustomerAccountService(db);

        var reg = await accountService.RegisterCustomerAsync(new CustomerRegisterRequest
        {
            FullName = "Hoàng Yến Nhi",
            Email = "yennhi@parfumluxe.vn",
            Password = "RoyalPassword@2026",
            ConfirmPassword = "RoyalPassword@2026"
        });

        var badPhone = await accountService.UpdateProfileAsync(reg.Profile!.Id, new CustomerProfileUpdateRequest
        {
            FullName = "Hoàng Yến Nhi",
            PhoneNumber = "INVALID_PHONE_ABC",
            Address = new string('A', 501)
        });

        Assert.False(badPhone.Success);
        Assert.Equal(2, badPhone.ValidationErrors.Count);
    }

    // ─── EXACT LOYALTY TIER BOUNDARY VALUE TESTS (TC-T2-020 to TC-T2-027) ────

    [Theory(DisplayName = "TC-T2-020..027: EvaluateTier & GetDiscountPercent exact boundary values")]
    [InlineData(0, UserTier.Bronze, 0, "TC-T2-020: 0₫ -> Bronze (0%)")]
    [InlineData(4_999_999, UserTier.Bronze, 0, "TC-T2-021: 4,999,999₫ -> Bronze (0%)")]
    [InlineData(5_000_000, UserTier.Silver, 3, "TC-T2-022: 5,000,000₫ -> Silver (3%)")]
    [InlineData(14_999_999, UserTier.Silver, 3, "TC-T2-023: 14,999,999₫ -> Silver (3%)")]
    [InlineData(15_000_000, UserTier.Gold, 7, "TC-T2-024: 15,000,000₫ -> Gold (7%)")]
    [InlineData(29_999_999, UserTier.Gold, 7, "TC-T2-025: 29,999,999₫ -> Gold (7%)")]
    [InlineData(30_000_000, UserTier.Diamond, 12, "TC-T2-026: 30,000,000₫ -> Diamond (12%)")]
    [InlineData(100_000_000, UserTier.Diamond, 12, "TC-T2-027: 100,000,000₫ -> Diamond (12%)")]
    public void TC_T2_020_to_027_LoyaltyTier_ExactBoundaryValues_EvaluatedCorrectly(
        decimal spent,
        UserTier expectedTier,
        decimal expectedDiscountPercent,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);

        var actualTier = loyaltyService.EvaluateTier(spent);
        var actualDiscount = loyaltyService.GetDiscountPercent(actualTier);

        Assert.True(actualTier == expectedTier, scenarioNote);
        Assert.Equal(expectedDiscountPercent, actualDiscount);
    }

    // ─── LOYALTY POINTS CALCULATION TESTS (TC-T2-028 to TC-T2-034) ───────────

    [Theory(DisplayName = "TC-T2-028..034: CalculatePoints 1 point per 10,000₫ boundary checks")]
    [InlineData(-50_000, 0, "TC-T2-028: Negative spend -> 0 points")]
    [InlineData(0, 0, "TC-T2-029: 0₫ -> 0 points")]
    [InlineData(9_999, 0, "TC-T2-030: 9,999₫ -> 0 points (floor rounding)")]
    [InlineData(10_000, 1, "TC-T2-031: 10,000₫ -> 1 point")]
    [InlineData(4_999_999, 499, "TC-T2-032: 4,999,999₫ -> 499 points")]
    [InlineData(15_000_000, 1_500, "TC-T2-033: 15,000,000₫ -> 1,500 points")]
    [InlineData(100_000_000, 10_000, "TC-T2-034: 100,000,000₫ -> 10,000 points")]
    public void TC_T2_028_to_034_CalculatePoints_FloorDivisionBy10000_Exact(
        decimal spent,
        int expectedPoints,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);

        var actualPoints = loyaltyService.CalculatePoints(spent);
        Assert.True(actualPoints == expectedPoints, scenarioNote);
    }

    // ─── PROGRESS % & AMOUNT TO NEXT TIER TESTS (TC-T2-035 to TC-T2-041) ─────

    [Theory(DisplayName = "TC-T2-035..041: CalculateProgressPercent & CalculateAmountToNextTier across all tiers")]
    [InlineData(0, 0, 5_000_000, "TC-T2-035: 0₫ -> 0% progress, 5,000,000₫ to Silver")]
    [InlineData(2_500_000, 50, 2_500_000, "TC-T2-036: 2,500,000₫ -> 50% progress in Bronze, 2,500,000₫ to Silver")]
    [InlineData(5_000_000, 0, 10_000_000, "TC-T2-037: 5,000,000₫ -> 0% progress in Silver, 10,000,000₫ to Gold")]
    [InlineData(10_000_000, 50, 5_000_000, "TC-T2-038: 10,000,000₫ -> 50% progress in Silver, 5,000,000₫ to Gold")]
    [InlineData(15_000_000, 0, 15_000_000, "TC-T2-039: 15,000,000₫ -> 0% progress in Gold, 15,000,000₫ to Diamond")]
    [InlineData(22_500_000, 50, 7_500_000, "TC-T2-040: 22,500,000₫ -> 50% progress in Gold, 7,500,000₫ to Diamond")]
    [InlineData(30_000_000, 100, 0, "TC-T2-041: 30,000,000₫ -> 100% progress in Diamond, 0₫ to next tier")]
    public void TC_T2_035_to_041_ProgressAndAmountToNextTier_ComputedAccurately(
        decimal spent,
        decimal expectedProgress,
        decimal expectedAmountToNext,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);

        var progress = loyaltyService.CalculateProgressPercent(spent);
        var amountToNext = loyaltyService.CalculateAmountToNextTier(spent);

        Assert.True(progress == expectedProgress, scenarioNote);
        Assert.Equal(expectedAmountToNext, amountToNext);
    }

    // ─── ORDER FILTERING & VIP DISCOUNT APPLICATION TESTS (TC-T2-042 to TC-T2-050) ─

    [Fact(DisplayName = "TC-T2-042: GetOrSyncUserLoyaltyAsync counts Delivered and Completed orders only, excluding Cancelled/Pending/Refunded")]
    public async Task TC_T2_042_GetOrSyncUserLoyalty_ExcludesCancelledPendingFailedRefundedOrders()
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);
        var userId = "user-loyalty-filter-042";

        db.Users.Add(new AppUser { Id = userId, UserName = "vip042@luxe.vn", Email = "vip042@luxe.vn", FullName = "VIP 042" });

        db.Orders.AddRange(
            // Qualifying 1: Delivered + COD Pending -> 4,000,000₫
            new Order { OrderCode = "ORD-042-1", UserId = userId, TotalAmount = 4_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Pending },
            // Qualifying 2: Confirmed + BankTransfer Completed -> 3,500,000₫
            new Order { OrderCode = "ORD-042-2", UserId = userId, TotalAmount = 3_500_000m, OrderStatus = OrderStatus.Confirmed, PaymentStatus = PaymentStatus.Completed },
            // Excluded 1: Cancelled even though PaymentStatus.Completed -> 20,000,000₫
            new Order { OrderCode = "ORD-042-3", UserId = userId, TotalAmount = 20_000_000m, OrderStatus = OrderStatus.Cancelled, PaymentStatus = PaymentStatus.Completed },
            // Excluded 2: Pending + Pending -> 10,000,000₫
            new Order { OrderCode = "ORD-042-4", UserId = userId, TotalAmount = 10_000_000m, OrderStatus = OrderStatus.Pending, PaymentStatus = PaymentStatus.Pending },
            // Excluded 3: Delivered + Refunded -> 8,000,000₫
            new Order { OrderCode = "ORD-042-5", UserId = userId, TotalAmount = 8_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Refunded },
            // Excluded 4: Soft-deleted Delivered order -> 15,000,000₫
            new Order { OrderCode = "ORD-042-6", UserId = userId, TotalAmount = 15_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Completed, IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var status = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);

        Assert.Equal(2, status.QualifyingOrderCount);
        Assert.Equal(7_500_000m, status.TotalSpent);
        Assert.Equal(UserTier.Silver, status.CurrentTier);
        Assert.Equal(3m, status.DiscountPercent);
        Assert.Equal(750, status.LoyaltyPoints);
        Assert.Equal(7_500_000m, status.AmountToNextTier);
    }

    [Fact(DisplayName = "TC-T2-043: Multi-order progression upgrades user automatically Bronze -> Silver -> Gold -> Diamond")]
    public async Task TC_T2_043_MultiOrderProgression_UpgradesBronzeToSilverToGoldToDiamond()
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);
        var userId = "user-progression-043";
        db.Users.Add(new AppUser { Id = userId, UserName = "prog@luxe.vn", Email = "prog@luxe.vn", FullName = "Progression User" });
        await db.SaveChangesAsync();

        // Initial: Bronze
        var s0 = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);
        Assert.Equal(UserTier.Bronze, s0.CurrentTier);

        // Add 6,000,000₫ order -> Silver
        db.Orders.Add(new Order { OrderCode = "P-1", UserId = userId, TotalAmount = 6_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Completed });
        await db.SaveChangesAsync();
        var s1 = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);
        Assert.Equal(UserTier.Silver, s1.CurrentTier);

        // Add 10,000,000₫ order (total 16,000,000₫) -> Gold
        db.Orders.Add(new Order { OrderCode = "P-2", UserId = userId, TotalAmount = 10_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Completed });
        await db.SaveChangesAsync();
        var s2 = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);
        Assert.Equal(UserTier.Gold, s2.CurrentTier);

        // Add 15,000,000₫ order (total 31,000,000₫) -> Diamond
        db.Orders.Add(new Order { OrderCode = "P-3", UserId = userId, TotalAmount = 15_000_000m, OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Completed });
        await db.SaveChangesAsync();
        var s3 = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);
        Assert.Equal(UserTier.Diamond, s3.CurrentTier);
        Assert.Null(s3.NextTier);
        Assert.Equal(0m, s3.AmountToNextTier);
        Assert.Equal(100m, s3.ProgressPercent);
    }

    [Theory(DisplayName = "TC-T2-044..047: ApplyVipLoyaltyToOrderAsync calculates VIP discount, voucher, gift wrap, and shipping")]
    [InlineData(0, 4_000_000, 0, false, UserTier.Bronze, 0, 0, 4_000_000, "TC-T2-044: Bronze 0% discount on 4M order")]
    [InlineData(6_000_000, 5_000_000, 200_000, true, UserTier.Silver, 150_000, 50_000, 4_700_000, "TC-T2-045: Silver 3% (150k) + 200k voucher + 50k gift wrap on 5M order")]
    [InlineData(18_000_000, 10_000_000, 500_000, true, UserTier.Gold, 700_000, 50_000, 8_850_000, "TC-T2-046: Gold 7% (700k) + 500k voucher + 50k gift wrap on 10M order")]
    [InlineData(35_000_000, 10_000_000, 1_000_000, false, UserTier.Diamond, 1_200_000, 0, 7_800_000, "TC-T2-047: Diamond 12% (1.2M) + 1M voucher on 10M order")]
    public async Task TC_T2_044_to_047_ApplyVipLoyaltyToOrder_CalculatesDiscountsAndFeesCorrectly(
        decimal historicalSpend,
        decimal newOrderSubTotal,
        decimal voucherDiscount,
        bool isGiftWrap,
        UserTier expectedTier,
        decimal expectedVipDiscount,
        decimal expectedGiftFee,
        decimal expectedFinalTotal,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);
        var userId = $"user-{Guid.NewGuid():N}";

        if (historicalSpend > 0)
        {
            db.Orders.Add(new Order
            {
                OrderCode = $"HIST-{Guid.NewGuid():N}"[..16],
                UserId = userId,
                TotalAmount = historicalSpend,
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Completed
            });
            await db.SaveChangesAsync();
        }

        var order = new Order
        {
            OrderCode = $"NEW-{Guid.NewGuid():N}"[..16],
            UserId = userId,
            SubTotal = newOrderSubTotal,
            DiscountAmount = voucherDiscount,
            IsGiftWrapping = isGiftWrap
        };

        await loyaltyService.ApplyVipLoyaltyToOrderAsync(order);

        Assert.True(order.AppliedLoyaltyTier == expectedTier, scenarioNote);
        Assert.Equal(expectedVipDiscount, order.LoyaltyDiscountAmount);
        Assert.Equal(expectedGiftFee, order.GiftWrappingFee);
        Assert.Equal(expectedFinalTotal, order.TotalAmount);
    }

    [Fact(DisplayName = "TC-T2-048: CalculateOrderVipPricingAsync adds 35,000₫ shipping fee when SubTotal < 2,000,000₫")]
    public async Task TC_T2_048_CalculateOrderVipPricing_SubTotalUnder2Million_Adds35kShippingFee()
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);

        var preview = await loyaltyService.CalculateOrderVipPricingAsync(null, subTotal: 1_500_000m, voucherDiscount: 0m, isGiftWrapping: false);

        Assert.Equal(35_000m, preview.ShippingFee);
        Assert.Equal(1_535_000m, preview.FinalTotalAmount);
    }

    [Fact(DisplayName = "TC-T2-049: CalculateOrderVipPricingAsync caps combined voucher + VIP discount so TotalAmount never goes negative")]
    public async Task TC_T2_049_CalculateOrderVipPricing_DiscountNeverExceedsSubTotal()
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);
        var userId = "diamond-cap-user";
        db.UserLoyalties.Add(new UserLoyalty { UserId = userId, TotalSpent = 50_000_000m, Tier = UserTier.Diamond, Points = 5000 });
        await db.SaveChangesAsync();

        var preview = await loyaltyService.CalculateOrderVipPricingAsync(userId, subTotal: 2_000_000m, voucherDiscount: 1_900_000m, isGiftWrapping: false);

        Assert.Equal(100_000m, preview.VipDiscountAmount);
        Assert.Equal(0m, preview.FinalTotalAmount);
    }

    [Fact(DisplayName = "TC-T2-050: GetOrSyncUserLoyaltyAsync handles guest/empty userId gracefully with Bronze defaults")]
    public async Task TC_T2_050_GetOrSyncUserLoyalty_EmptyUserId_ReturnsBronzeDefaults()
    {
        using var db = CreateIsolatedDbContext();
        var loyaltyService = new LoyaltyTierService(db);

        var status = await loyaltyService.GetOrSyncUserLoyaltyAsync("");

        Assert.Equal(UserTier.Bronze, status.CurrentTier);
        Assert.Equal(UserTier.Silver, status.NextTier);
        Assert.Equal(0m, status.TotalSpent);
        Assert.Equal(5_000_000m, status.AmountToNextTier);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PART 4: 3-STEP DISCOVERY GIFT BOX BUILDER (TC-T2-051..100)
    // ═══════════════════════════════════════════════════════════════════════════

    // ─── STEP 1: BOX STYLE SELECTION & PRICING (TC-T2-051 to TC-T2-060) ──────

    [Fact(DisplayName = "TC-T2-051: GetAvailableBoxStyles returns exactly 3 luxury styles with configured prices")]
    public void TC_T2_051_GetAvailableBoxStyles_ReturnsThreeLuxuryOptions()
    {
        using var db = CreateIsolatedDbContext();
        var boxService = new DiscoveryBoxService(db);

        var styles = boxService.GetAvailableBoxStyles();

        Assert.Equal(3, styles.Count);
        Assert.Contains(styles, s => s.Code == "ClassicWood" && s.Price == 250_000m);
        Assert.Contains(styles, s => s.Code == "RoyalVelvet" && s.Price == 200_000m);
        Assert.Contains(styles, s => s.Code == "ArtisanLeather" && s.Price == 180_000m);
    }

    [Theory(DisplayName = "TC-T2-052..054: Step 1 Box Style pricing applied accurately for ClassicWood, RoyalVelvet, ArtisanLeather")]
    [InlineData("ClassicWood", "Hộp Gỗ Sơn Mài Hoàng Gia", 250_000, "TC-T2-052: ClassicWood = 250,000₫")]
    [InlineData("RoyalVelvet", "Hộp Nhung Đen Huyền Bí", 200_000, "TC-T2-053: RoyalVelvet = 200,000₫")]
    [InlineData("ArtisanLeather", "Túi Da Thủ Công Viền Vàng", 180_000, "TC-T2-054: ArtisanLeather = 180,000₫")]
    public async Task TC_T2_052_to_054_ValidateBoxStyle_ValidStyle_AppliesExactBoxPrice(
        string styleCode,
        string expectedName,
        decimal expectedBoxPrice,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = styleCode,
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.True(validation.IsValid, scenarioNote);
        Assert.Equal(expectedBoxPrice, validation.Configuration!.BoxPrice);
        Assert.Equal(expectedName, validation.Configuration.BoxStyleName);
    }

    [Theory(DisplayName = "TC-T2-055..060: Step 1 Box Style rejects invalid, empty, or case-mismatched codes")]
    [InlineData("", "TC-T2-055: Empty BoxStyleCode")]
    [InlineData("   ", "TC-T2-056: Whitespace BoxStyleCode")]
    [InlineData("PlasticBox", "TC-T2-057: Unknown style PlasticBox")]
    [InlineData("classicwood", "TC-T2-058: Lowercase classicwood")]
    [InlineData("ROYALVELVET", "TC-T2-059: Uppercase ROYALVELVET")]
    [InlineData("CardboardPaper", "TC-T2-060: Unsupported CardboardPaper")]
    public async Task TC_T2_055_to_060_ValidateBoxStyle_InvalidStyle_Rejected(string invalidStyle, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = invalidStyle,
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid, scenarioNote);
        Assert.Contains("Kiểu hộp quà không hợp lệ", validation.ErrorMessage);
    }

    // ─── STEP 2: SAMPLE VIAL SELECTION & BOUNDARIES (TC-T2-061 to TC-T2-080) ─

    [Theory(DisplayName = "TC-T2-061..069: Step 2 Vial count boundary validation (0,1,2 rejected; 3,4,5 accepted; 6,7,10 rejected)")]
    [InlineData(0, false, "TC-T2-061: 0 vials rejected")]
    [InlineData(1, false, "TC-T2-062: 1 vial rejected")]
    [InlineData(2, false, "TC-T2-063: 2 vials rejected (lower boundary - 1)")]
    [InlineData(3, true, "TC-T2-064: 3 vials accepted (min boundary)")]
    [InlineData(4, true, "TC-T2-065: 4 vials accepted (mid boundary)")]
    [InlineData(5, true, "TC-T2-066: 5 vials accepted (max boundary)")]
    [InlineData(6, false, "TC-T2-067: 6 vials rejected (upper boundary + 1)")]
    [InlineData(7, false, "TC-T2-068: 7 vials rejected")]
    [InlineData(10, false, "TC-T2-069: 10 vials rejected")]
    public async Task TC_T2_061_to_069_ValidateVialCount_BoundariesEnforced(
        int vialCount,
        bool expectedValid,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 10);
        var boxService = new DiscoveryBoxService(db);

        var request = new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            Vials = products.Take(vialCount).Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        };

        var validation = await boxService.ValidateAndBuildConfigurationAsync(request);
        Assert.True(validation.IsValid == expectedValid, scenarioNote);
    }

    [Theory(DisplayName = "TC-T2-070..075: Step 2 Sample Vial pricing for 2ml and 5ml across product BasePrices")]
    [InlineData(3_000_000, 2, 120_000, "TC-T2-070: 2ml of 3M perfume -> min floor 120,000₫ (3% = 90k < 120k)")]
    [InlineData(5_000_000, 2, 150_000, "TC-T2-071: 2ml of 5M perfume -> 3% = 150,000₫")]
    [InlineData(6_500_000, 2, 195_000, "TC-T2-072: 2ml of 6.5M perfume -> 3% = 195,000₫")]
    [InlineData(3_500_000, 5, 250_000, "TC-T2-073: 5ml of 3.5M perfume -> min floor 250,000₫ (6% = 210k < 250k)")]
    [InlineData(5_000_000, 5, 300_000, "TC-T2-074: 5ml of 5M perfume -> 6% = 300,000₫")]
    [InlineData(10_000_000, 5, 600_000, "TC-T2-075: 5ml of 10M perfume -> 6% = 600,000₫")]
    public void TC_T2_070_to_075_CalculateSampleVialPrice_2mlAnd5mlFormula_Exact(
        decimal basePrice,
        int volumeMl,
        decimal expectedVialPrice,
        string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var boxService = new DiscoveryBoxService(db);

        var actualPrice = boxService.CalculateSampleVialPrice(basePrice, volumeMl);
        Assert.True(actualPrice == expectedVialPrice, scenarioNote);
    }

    [Theory(DisplayName = "TC-T2-076..078: Step 2 rejects unsupported vial volumes (0ml, 1ml, 10ml, 100ml)")]
    [InlineData(0, "TC-T2-076: 0ml rejected")]
    [InlineData(1, "TC-T2-077: 1ml rejected")]
    [InlineData(10, "TC-T2-078: 10ml rejected in 2ml/5ml builder")]
    public async Task TC_T2_076_to_078_ValidateVialVolume_UnsupportedVolume_Rejected(int invalidVolume, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "RoyalVelvet",
            Vials = new List<DiscoveryVialSelectionDto>
            {
                new() { ProductId = products[0].Id, VolumeMl = 2 },
                new() { ProductId = products[1].Id, VolumeMl = 5 },
                new() { ProductId = products[2].Id, VolumeMl = invalidVolume }
            }
        });

        Assert.False(validation.IsValid, scenarioNote);
        Assert.Contains("Chỉ chấp nhận 2ml hoặc 5ml", validation.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-079: Step 2 rejects out-of-stock Product in Discovery Box")]
    public async Task TC_T2_079_ValidateVials_OutOfStockProduct_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        products[1].IsInStock = false;
        await db.SaveChangesAsync();

        var boxService = new DiscoveryBoxService(db);
        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid);
        Assert.Contains("hết hàng", validation.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-080: Step 2 rejects non-existent or soft-deleted ProductId")]
    public async Task TC_T2_080_ValidateVials_NonExistentOrDeletedProduct_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        products[2].IsDeleted = true;
        await db.SaveChangesAsync();

        var boxService = new DiscoveryBoxService(db);
        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            Vials = new List<DiscoveryVialSelectionDto>
            {
                new() { ProductId = products[0].Id, VolumeMl = 2 },
                new() { ProductId = products[1].Id, VolumeMl = 2 },
                new() { ProductId = products[2].Id, VolumeMl = 2 }
            }
        });

        Assert.False(validation.IsValid);
        Assert.Contains("không tồn tại hoặc đã bị xóa", validation.ErrorMessage);
    }

    // ─── STEP 3: GOLD-FOIL GREETING CARD & TYPOGRAPHY (TC-T2-081 to TC-T2-090) ─

    [Theory(DisplayName = "TC-T2-081..083: Step 3 accepts all 3 luxury fonts (Playfair Display, Dancing Script, Cormorant Garamond)")]
    [InlineData("Playfair Display", "TC-T2-081: Playfair Display font accepted")]
    [InlineData("Dancing Script", "TC-T2-082: Dancing Script font accepted")]
    [InlineData("Cormorant Garamond", "TC-T2-083: Cormorant Garamond font accepted")]
    public async Task TC_T2_081_to_083_ValidateGreetingCard_AllowedFonts_Accepted(string allowedFont, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "RoyalVelvet",
            GreetingMessage = "Chúc mừng kỷ niệm ngày cưới ấm áp và ngọt ngào!",
            GreetingFont = allowedFont,
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.True(validation.IsValid, scenarioNote);
        Assert.Equal(allowedFont, validation.Configuration!.GreetingFont);
    }

    [Theory(DisplayName = "TC-T2-084..086: Step 3 rejects unsupported fonts")]
    [InlineData("Comic Sans MS", "TC-T2-084: Comic Sans MS rejected")]
    [InlineData("Arial", "TC-T2-085: Arial rejected")]
    [InlineData("Papyrus", "TC-T2-086: Papyrus rejected")]
    public async Task TC_T2_084_to_086_ValidateGreetingCard_UnsupportedFonts_Rejected(string invalidFont, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "RoyalVelvet",
            GreetingMessage = "Gửi tặng người thương",
            GreetingFont = invalidFont,
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid, scenarioNote);
        Assert.Contains("không được hỗ trợ", validation.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-087: Step 3 GreetingMessage exact 250-character boundary is accepted and trimmed")]
    public async Task TC_T2_087_ValidateGreetingMessage_Exact250Chars_Accepted()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var exact250 = new string('G', 250);
        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            GreetingMessage = $"   {exact250}   ",
            GreetingFont = "Playfair Display",
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.True(validation.IsValid);
        Assert.Equal(250, validation.Configuration!.GreetingMessage!.Length);
    }

    [Fact(DisplayName = "TC-T2-088: Step 3 GreetingMessage 251-character boundary is rejected")]
    public async Task TC_T2_088_ValidateGreetingMessage_251Chars_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var over250 = new string('X', 251);
        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            GreetingMessage = over250,
            GreetingFont = "Playfair Display",
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid);
        Assert.Contains("không được vượt quá 250 ký tự", validation.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-089: Step 3 Empty or whitespace GreetingFont defaults to Playfair Display")]
    public async Task TC_T2_089_ValidateGreetingFont_Blank_DefaultsToPlayfairDisplay()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ArtisanLeather",
            GreetingMessage = null,
            GreetingFont = "   ",
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.True(validation.IsValid);
        Assert.Equal("Playfair Display", validation.Configuration!.GreetingFont);
    }

    [Fact(DisplayName = "TC-T2-090: Step 3 RecipientName exceeding 100 characters is rejected")]
    public async Task TC_T2_090_ValidateRecipientName_Over100Chars_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "ClassicWood",
            RecipientName = new string('R', 101),
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid);
        Assert.Contains("Tên người nhận không được vượt quá 100 ký tự", validation.ErrorMessage);
    }

    // ─── STEP 4: CART & ORDER PERSISTENCE WITH CUSTOMBOXCONFIGJSON (TC-T2-091..100) ─

    [Fact(DisplayName = "TC-T2-091: AddDiscoveryBoxToCartAsync saves IsCustomBox=true, CustomBoxConfigJson, and CustomUnitPrice for authenticated user")]
    public async Task TC_T2_091_AddDiscoveryBoxToCart_AuthenticatedUser_PersistsCustomBoxFields()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 4);
        var boxService = new DiscoveryBoxService(db);

        var (success, error, cartItem) = await boxService.AddDiscoveryBoxToCartAsync(
            userId: "vip-cart-user-091",
            guestToken: null,
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ClassicWood", // 250,000₫
                RecipientName = "Nàng Thơ Của Anh",
                GreetingMessage = "Món quà hương thơm tinh tế dành tặng em!",
                GreetingFont = "Dancing Script",
                Quantity = 2,
                Vials = new List<DiscoveryVialSelectionDto>
                {
                    new() { ProductId = products[0].Id, VolumeMl = 2 }, // 3.5M -> 120,000₫
                    new() { ProductId = products[1].Id, VolumeMl = 5 }, // 5.0M -> 300,000₫
                    new() { ProductId = products[2].Id, VolumeMl = 5 }, // 6.5M -> 390,000₫
                    new() { ProductId = products[3].Id, VolumeMl = 2 }  // 8.0M -> 240,000₫
                }
            });

        Assert.True(success, error);
        Assert.NotNull(cartItem);
        Assert.True(cartItem!.IsCustomBox);
        Assert.Equal(2, cartItem.Quantity);
        // Box 250k + (120k + 300k + 390k + 240k = 1,050k) = 1,300,000₫
        Assert.Equal(1_300_000m, cartItem.CustomUnitPrice);
        Assert.NotNull(cartItem.CustomBoxConfigJson);

        using var doc = JsonDocument.Parse(cartItem.CustomBoxConfigJson!);
        Assert.Equal("ClassicWood", doc.RootElement.GetProperty("boxStyleCode").GetString());
        Assert.Equal("Dancing Script", doc.RootElement.GetProperty("greetingFont").GetString());
        Assert.Equal(4, doc.RootElement.GetProperty("vials").GetArrayLength());
    }

    [Fact(DisplayName = "TC-T2-092: AddDiscoveryBoxToCartAsync saves custom box for anonymous guestToken")]
    public async Task TC_T2_092_AddDiscoveryBoxToCart_GuestToken_PersistsHashedOwnerCart()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var (success, error, cartItem) = await boxService.AddDiscoveryBoxToCartAsync(
            userId: null,
            guestToken: "guest-session-token-999",
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ArtisanLeather", // 180,000₫
                Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
            });

        Assert.True(success, error);
        Assert.NotNull(cartItem);
        Assert.True(cartItem!.IsCustomBox);
        var cart = await db.ShoppingCarts.FirstOrDefaultAsync(c => c.Id == cartItem.ShoppingCartId);
        Assert.NotNull(cart);
        Assert.StartsWith("guest:", cart!.OwnerKey);
    }

    [Fact(DisplayName = "TC-T2-093: AddDiscoveryBoxToCartAsync fails when both userId and guestToken are missing")]
    public async Task TC_T2_093_AddDiscoveryBoxToCart_MissingUserAndGuestToken_Fails()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var (success, error, cartItem) = await boxService.AddDiscoveryBoxToCartAsync(
            userId: null,
            guestToken: "   ",
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ClassicWood",
                Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
            });

        Assert.False(success);
        Assert.Null(cartItem);
        Assert.Contains("Không xác định được phiên giỏ hàng", error);
    }

    [Theory(DisplayName = "TC-T2-094..095: ValidateAndBuildConfigurationAsync rejects Quantity < 1 or > 20")]
    [InlineData(0, "TC-T2-094: Quantity = 0 rejected")]
    [InlineData(21, "TC-T2-095: Quantity = 21 rejected")]
    public async Task TC_T2_094_to_095_ValidateDiscoveryBox_InvalidQuantity_Rejected(int invalidQty, string scenarioNote)
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var validation = await boxService.ValidateAndBuildConfigurationAsync(new DiscoveryBoxBuildRequestDto
        {
            BoxStyleCode = "RoyalVelvet",
            Quantity = invalidQty,
            Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
        });

        Assert.False(validation.IsValid, scenarioNote);
        Assert.Contains("Số lượng hộp quà phải từ 1 đến 20", validation.ErrorMessage);
    }

    [Fact(DisplayName = "TC-T2-096: PlaceDiscoveryBoxOrderAsync persists Order and OrderItem with IsCustomBox=true and CustomBoxConfigJson")]
    public async Task TC_T2_096_PlaceDiscoveryBoxOrder_ValidOrder_PersistsOrderItemCustomBox()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 5);
        var boxService = new DiscoveryBoxService(db);

        var (success, error, order) = await boxService.PlaceDiscoveryBoxOrderAsync(
            userId: null,
            new CheckoutRequestDto
            {
                CustomerName = "Trần Hoàng Anh",
                CustomerPhone = "0912345678",
                CustomerEmail = "hoanganh@parfumluxe.vn",
                ShippingAddress = "120 Nguyễn Huệ, Quận 1, TP.HCM",
                PaymentMethod = PaymentMethod.BankTransfer,
                IsGiftWrapping = true
            },
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "RoyalVelvet", // 200,000₫
                RecipientName = "Trần Hoàng Anh",
                GreetingMessage = "Tự thưởng bản thân bộ 5 mùi Niche đỉnh cao!",
                GreetingFont = "Cormorant Garamond",
                Quantity = 1,
                Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 5 }).ToList()
            });

        Assert.True(success, error);
        Assert.NotNull(order);
        Assert.Single(order!.Items);

        var item = order.Items.First();
        Assert.True(item.IsCustomBox);
        Assert.NotEmpty(item.CustomBoxConfigJson!);
        // 5 vials of 5ml: 3.5M->250k, 5M->300k, 6.5M->390k, 8M->480k, 10M->600k => sum = 2,020,000₫ + 200,000₫ box = 2,220,000₫
        Assert.Equal(2_220_000m, item.UnitPrice);
        Assert.Equal(2_220_000m, order.SubTotal);
        Assert.Equal(0m, order.ShippingFee); // >= 2M free shipping
        Assert.Equal(50_000m, order.GiftWrappingFee);
        Assert.Equal(2_270_000m, order.TotalAmount);
    }

    [Fact(DisplayName = "TC-T2-097: PlaceDiscoveryBoxOrderAsync applies Diamond VIP 12% discount automatically on Custom Discovery Box")]
    public async Task TC_T2_097_PlaceDiscoveryBoxOrder_DiamondMember_Applies12PercentVipDiscount()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var userId = "diamond-dbox-user-097";
        db.UserLoyalties.Add(new UserLoyalty { UserId = userId, TotalSpent = 40_000_000m, Tier = UserTier.Diamond, Points = 4000 });
        await db.SaveChangesAsync();

        var boxService = new DiscoveryBoxService(db);
        var (success, error, order) = await boxService.PlaceDiscoveryBoxOrderAsync(
            userId: userId,
            new CheckoutRequestDto
            {
                CustomerName = "Nữ Hoàng Kim Cương",
                CustomerPhone = "0999888777",
                CustomerEmail = "diamondqueen@parfumluxe.vn",
                ShippingAddress = "Landmark 81 Penthouse, TP.HCM",
                IsGiftWrapping = false
            },
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ClassicWood", // 250,000₫
                Quantity = 2,
                Vials = new List<DiscoveryVialSelectionDto>
                {
                    new() { ProductId = products[0].Id, VolumeMl = 5 }, // 3.5M -> 250k
                    new() { ProductId = products[1].Id, VolumeMl = 5 }, // 5.0M -> 300k
                    new() { ProductId = products[2].Id, VolumeMl = 5 }  // 6.5M -> 390k
                }
            });

        // 1 box = 250k + 250k + 300k + 390k = 1,190,000₫. Quantity 2 => SubTotal = 2,380,000₫
        // Diamond 12% of 2,380,000₫ = 285,600₫. FinalTotal = 2,094,400₫
        Assert.True(success, error);
        Assert.Equal(UserTier.Diamond, order!.AppliedLoyaltyTier);
        Assert.Equal(2_380_000m, order.SubTotal);
        Assert.Equal(285_600m, order.LoyaltyDiscountAmount);
        Assert.Equal(2_094_400m, order.TotalAmount);
    }

    [Fact(DisplayName = "TC-T2-098: PlaceDiscoveryBoxOrderAsync rejects missing checkout recipient info")]
    public async Task TC_T2_098_PlaceDiscoveryBoxOrder_MissingCustomerInfo_Rejected()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 3);
        var boxService = new DiscoveryBoxService(db);

        var (success, error, order) = await boxService.PlaceDiscoveryBoxOrderAsync(
            userId: null,
            new CheckoutRequestDto
            {
                CustomerName = "",
                CustomerPhone = "",
                CustomerEmail = "",
                ShippingAddress = ""
            },
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ClassicWood",
                Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 2 }).ToList()
            });

        Assert.False(success);
        Assert.Null(order);
        Assert.Contains("Vui lòng nhập đầy đủ thông tin người nhận hàng", error);
    }

    [Fact(DisplayName = "TC-T2-099: Delivering a Discovery Box order automatically upgrades Customer Loyalty Tier")]
    public async Task TC_T2_099_DeliveringDiscoveryBoxOrder_UpgradesUserLoyaltyTier()
    {
        using var db = CreateIsolatedDbContext();
        var products = await SeedLuxuryPerfumesAsync(db, 5);
        var loyaltyService = new LoyaltyTierService(db);
        var boxService = new DiscoveryBoxService(db, loyaltyService);

        var userId = "upgrade-via-dbox-099";
        db.Orders.Add(new Order
        {
            OrderCode = "PREV-4M-099",
            UserId = userId,
            TotalAmount = 4_000_000m,
            OrderStatus = OrderStatus.Delivered,
            PaymentStatus = PaymentStatus.Completed
        });
        await db.SaveChangesAsync();

        var (_, _, dboxOrder) = await boxService.PlaceDiscoveryBoxOrderAsync(
            userId: userId,
            new CheckoutRequestDto
            {
                CustomerName = "Khách Sắp Lên Hạng Bạc",
                CustomerPhone = "0933445566",
                CustomerEmail = "silverupgrade@parfumluxe.vn",
                ShippingAddress = "Quận 3, TP.HCM"
            },
            new DiscoveryBoxBuildRequestDto
            {
                BoxStyleCode = "ClassicWood",
                Quantity = 1,
                Vials = products.Select(p => new DiscoveryVialSelectionDto { ProductId = p.Id, VolumeMl = 5 }).ToList()
            });

        dboxOrder!.OrderStatus = OrderStatus.Delivered;
        dboxOrder.PaymentStatus = PaymentStatus.Completed;
        await db.SaveChangesAsync();

        var synced = await loyaltyService.GetOrSyncUserLoyaltyAsync(userId);
        Assert.Equal(UserTier.Silver, synced.CurrentTier);
        Assert.True(synced.TotalSpent >= 5_000_000m);
    }

    [Fact(DisplayName = "TC-T2-100: GetAllowedFonts returns exact 3 luxury typography options")]
    public void TC_T2_100_GetAllowedFonts_ReturnsThreeLuxuryTypographyOptions()
    {
        using var db = CreateIsolatedDbContext();
        var boxService = new DiscoveryBoxService(db);

        var fonts = boxService.GetAllowedFonts();

        Assert.Equal(3, fonts.Count);
        Assert.Equal(new[] { "Playfair Display", "Dancing Script", "Cormorant Garamond" }, fonts);
    }
}
