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

/// <summary>
/// Service quan ly tinh nang tuy bien hop qua trai nghiem mui huong Discovery Box (3-5 ong mau, in thiep ep kim).
/// </summary>
public sealed class DiscoveryBoxService : IDiscoveryBoxService
{
    // ==========================================
    // 1. HANG SO VA CAU HINH KIEU HOP QUY CHUAN
    // ==========================================
    public const int MinVialCount = 3;
    public const int MaxVialCount = 5;
    public const int MaxGreetingMessageLength = 250;

    // Danh sach cac kieu hop qua cao cap duoc phep chon
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

    // Danh sach phong cach font chu in loi chuc tren thiep ep kim
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

    // ==========================================
    // 2. TINH GIA ONG MAU CHIET THEO DUNG TICH
    // ==========================================

    // Tinh gia ong mau dua tren gia goc chai fullbox va he so dung tich (2ml hoac 5ml)
    public decimal CalculateSampleVialPrice(decimal productBasePrice, int volumeMl)
    {
        if (volumeMl != 2 && volumeMl != 5)
            throw new ArgumentOutOfRangeException(nameof(volumeMl), "Dung tích ống mẫu chỉ hỗ trợ 2ml hoặc 5ml.");

        var safeBasePrice = Math.Max(0m, productBasePrice);
        if (volumeMl == 2)
        {
            // 2ml tuong duong 3% gia goc chai fullbox, toi thieu 120.000 VND
            var proportional = Math.Round((safeBasePrice * 0.03m) / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
            return Math.Max(120_000m, proportional);
        }
        else
        {
            // 5ml tuong duong 6% gia goc chai fullbox, toi thieu 250.000 VND
            var proportional = Math.Round((safeBasePrice * 0.06m) / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
            return Math.Max(250_000m, proportional);
        }
    }

    // ==========================================
    // 3. KIEM TRA VA DUNG CAU HINH HOP QUA
    // ==========================================

    // Kiem tra hop le: kieu hop, so luong 3-5 ong, dung tich, do dai loi chuc va tinh tong tien hop
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

        // Buoc 1: Kiem tra kieu hop qua
        var styleCode = (request.BoxStyleCode ?? string.Empty).Trim();
        var boxStyle = BoxStyles.FirstOrDefault(s => string.Equals(s.Code, styleCode, StringComparison.Ordinal));
        if (boxStyle == null)
        {
            result.Errors.Add("Kiểu hộp quà không hợp lệ. Vui lòng chọn ClassicWood, RoyalVelvet hoặc ArtisanLeather.");
        }

        // Buoc 2: Kiem tra cac ong mau (3 den 5 ong, 2ml hoac 5ml)
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

        // Buoc 3: Kiem tra loi chuc tren thiep ep kim va font chu
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

        // Xac thuc cac san pham duoc chon ton tai trong DB va con hang
        var productIds = vials.Select(v => v.ProductId).Distinct().ToList();
        var productsDict = await _db.Products
            .AsNoTracking()
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

        // Chuyen cau hinh sang chuoi JSON de luu tru trong Cart / Order
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

    // ==========================================
    // 4. THEM HOP QUA VAO GIO HANG (CART)
    // ==========================================

    // Them hop qua vao gio hang cua thanh vien hoac khach vang lai
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

    // ==========================================
    // 5. DAT HANG TRUC TIEP DISCOVERY BOX
    // ==========================================

    // Tao don hang rieng cho Discovery Box kem ap dung uu dai VIP
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

    // ==========================================
    // 6. HAM PHU TRO DINH DANH GIO HANG (SHA-256)
    // ==========================================

    // Tao khoa nhan dien gio hang: user:{userId} hoac guest:{sha256}
    private static string? GetOwnerKey(string? userId, string? guestToken)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return "user:" + userId;
        if (string.IsNullOrWhiteSpace(guestToken)) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(guestToken));
        return "guest:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
