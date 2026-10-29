using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

// ============================================================================
// DICH VU QUAN LY GIO HANG (CUSTOMER CART SERVICE)
// Xu ly toan bo logic gio hang: khach vang lai (guest), thanh vien (user),
// gop gio hang khi dang nhap va kiem tra ton kho thoi gian thuc.
// ============================================================================
public sealed class CustomerCartService : ICustomerCartService
{
    private readonly PerfumeDbContext _db;
    // Thoi gian luu tru gio hang khach vang lai mac dinh la 30 ngay
    private static readonly TimeSpan GuestCartLifetime = TimeSpan.FromDays(30);

    public CustomerCartService(PerfumeDbContext db) => _db = db;

    // ------------------------------------------------------------------------
    // 1. LAY TRANG THAI GIO HANG HIEN TAI
    // Tim gio hang theo ownerKey (user ID hoac hash guest token), kiem tra han dung
    // ------------------------------------------------------------------------
    public async Task<CartStateDto> GetAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var ownerKey = GetOwnerKey(userId, guestToken);
        if (ownerKey == null) return new CartStateDto();

        var cart = await _db.ShoppingCarts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (cart == null) return new CartStateDto();

        return await BuildStateAsync(cart, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 2. THEM SAN PHAM VAO GIO HANG
    // Kiem tra ton kho, gioi han so luong toi da 50 sp va tao dong item moi
    // ------------------------------------------------------------------------
    public async Task<CartStateDto> AddAsync(string? userId, string? guestToken, CartAddItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity is < 1 or > 50) throw new InvalidOperationException("Số lượng phải từ 1 đến 50.");

        // Tim san pham con dang kinh doanh
        var product = await _db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == request.ProductSlug && p.IsInStock, cancellationToken);
        if (product == null) throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng bán.");
        if (product.Variants.Count == 0 && request.VariantId.HasValue)
            throw new KeyNotFoundException("Sản phẩm này không có phiên bản đã chọn.");

        // Xac dinh phien ban variant hop le
        ProductVariant? variant = null;
        if (product.Variants.Count > 0)
        {
            variant = request.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value)
                : product.Variants.FirstOrDefault(v => v.IsDefault) ?? product.Variants.FirstOrDefault();
            if (variant == null) throw new KeyNotFoundException("Phiên bản sản phẩm không hợp lệ.");
            if (variant.StockQuantity <= 0) throw new InvalidOperationException("Phiên bản này đã hết hàng.");
        }

        // Lay hoac tao moi gio hang
        var cart = await GetOrCreateCartAsync(userId, guestToken, cancellationToken);
        var line = cart.Items.FirstOrDefault(i => i.ProductId == product.Id && i.VariantId == variant?.Id);
        var nextQuantity = (line?.Quantity ?? 0) + request.Quantity;

        // Kiem tra gioi han ton kho an toan
        var stock = variant?.StockQuantity ?? 50;
        if (nextQuantity > Math.Min(stock, 50)) throw new InvalidOperationException($"Chỉ còn tối đa {Math.Min(stock, 50)} sản phẩm khả dụng.");

        if (line == null)
        {
            cart.Items.Add(new ShoppingCartItem { ProductId = product.Id, VariantId = variant?.Id, Quantity = request.Quantity });
        }
        else
        {
            line.Quantity = nextQuantity;
        }

        // Gia han thoi gian song cua gio hang
        cart.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(cart, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 3. CAP NHAT SO LUONG MON HANG TRONG GIO
    // Neu so luong = 0 thi tu dong xoa khoi gio hang
    // ------------------------------------------------------------------------
    public async Task<CartStateDto> SetQuantityAsync(string? userId, string? guestToken, CartLineRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity is < 0 or > 50) throw new InvalidOperationException("Số lượng phải từ 0 đến 50.");

        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        var item = cart?.Items.FirstOrDefault(i => i.ProductId == request.ProductId && i.VariantId == request.VariantId);
        if (cart == null || item == null) return cart == null ? new CartStateDto() : await BuildStateAsync(cart, cancellationToken);

        if (request.Quantity == 0)
        {
            _db.ShoppingCartItems.Remove(item);
        }
        else
        {
            if (request.Quantity > 50) throw new InvalidOperationException("Số lượng mỗi phiên bản không được vượt quá 50.");
            var stock = await GetStockAsync(item.ProductId, item.VariantId, cancellationToken);
            if (request.Quantity > stock) throw new InvalidOperationException($"Chỉ còn {stock} sản phẩm khả dụng.");
            item.Quantity = request.Quantity;
        }

        cart.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(cart, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 4. XOA MON HANG KHOI GIO HANG
    // ------------------------------------------------------------------------
    public async Task<CartStateDto> RemoveAsync(string? userId, string? guestToken, int productId, int? variantId, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null) return new CartStateDto();

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.VariantId == variantId);
        if (item != null) _db.ShoppingCartItems.Remove(item);

        cart.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(cart, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 5. GOP GIO HANG KHACH VANG LAI VAO TAI KHOAN THANH VIEN KHI DANG NHAP
    // Chuyen toan bo san pham tu guest cart sang user cart, lam sach guest cart
    // ------------------------------------------------------------------------
    public async Task<CartStateDto> MergeGuestCartAsync(string userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var guestKey = GetOwnerKey(null, guestToken);
        if (guestKey == null) return await GetAsync(userId, null, cancellationToken);

        var guest = await _db.ShoppingCarts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == guestKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
        var account = await GetOrCreateCartAsync(userId, null, cancellationToken);
        if (guest == null || guest.Items.Count == 0) return await BuildStateAsync(account, cancellationToken);

        foreach (var guestLine in guest.Items)
        {
            var stock = await GetStockAsync(guestLine.ProductId, guestLine.VariantId, cancellationToken);
            if (stock <= 0) continue;

            var accountLine = account.Items.FirstOrDefault(i => i.ProductId == guestLine.ProductId && i.VariantId == guestLine.VariantId);
            if (accountLine == null)
            {
                account.Items.Add(new ShoppingCartItem
                {
                    ProductId = guestLine.ProductId,
                    VariantId = guestLine.VariantId,
                    Quantity = Math.Min(guestLine.Quantity, Math.Min(stock, 50))
                });
            }
            else
            {
                accountLine.Quantity = Math.Min(accountLine.Quantity + guestLine.Quantity, Math.Min(stock, 50));
            }
        }

        guest.Items.Clear();
        guest.ExpiresAt = DateTime.UtcNow;
        account.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(account, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 6. XOA SACH GIO HANG SAU KHI DAT HANG THANH CONG
    // ------------------------------------------------------------------------
    public async Task ClearAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null) return;

        _db.ShoppingCartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 7. HAM HELPER: TIM GIO HANG THEO OWNER KEY
    // ------------------------------------------------------------------------
    private async Task<ShoppingCart?> FindCartAsync(string? userId, string? guestToken, CancellationToken cancellationToken)
    {
        var ownerKey = GetOwnerKey(userId, guestToken);
        if (ownerKey == null) return null;

        return await _db.ShoppingCarts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 8. HAM HELPER: LAY HOAC TAO MOI GIO HANG
    // ------------------------------------------------------------------------
    private async Task<ShoppingCart> GetOrCreateCartAsync(string? userId, string? guestToken, CancellationToken cancellationToken)
    {
        var ownerKey = GetOwnerKey(userId, guestToken) ?? throw new InvalidOperationException("Không xác định được giỏ hàng.");
        var cart = await _db.ShoppingCarts.Include(c => c.Items).FirstOrDefaultAsync(c => c.OwnerKey == ownerKey, cancellationToken);
        if (cart == null)
        {
            cart = new ShoppingCart
            {
                OwnerKey = ownerKey,
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime)
            };
            _db.ShoppingCarts.Add(cart);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            cart.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
            if (!string.IsNullOrWhiteSpace(userId)) cart.UserId = userId;
        }
        return cart;
    }

    // ------------------------------------------------------------------------
    // 9. HAM HELPER: DUNG DTO TRANG THAI GIO HANG (BATCH LOADING - TRIET TIEU N+1)
    // Gom toan bo product IDs va query trong 1 lan duy nhat de toi uu database
    // ------------------------------------------------------------------------
    private async Task<CartStateDto> BuildStateAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var state = new CartStateDto();
        if (cart.Items.Count == 0) return state;

        // Truy van tat ca san pham trong gio bang 1 cau lenh don (khong theo doi change tracker)
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var line in cart.Items)
        {
            if (!productsDict.TryGetValue(line.ProductId, out var product)) continue;

            var variant = line.VariantId.HasValue ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId.Value) : null;
            var hasVariants = product.Variants.Count > 0;
            var stock = hasVariants ? variant?.StockQuantity ?? 0 : product.IsInStock ? 50 : 0;
            var price = variant == null ? product.BasePrice : variant.SalePrice > 0 ? variant.SalePrice : variant.OriginalPrice;
            if (price <= 0) price = product.BasePrice;

            state.Items.Add(new CartItemDto
            {
                ProductId = product.Id,
                VariantId = variant?.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                ProductImageUrl = product.MainImageUrl,
                VariantLabel = variant?.VolumeLabel ?? "Fullbox",
                UnitPrice = price,
                Quantity = line.Quantity,
                StockQuantity = stock,
                IsAvailable = product.IsInStock && (!hasVariants || (variant != null && stock >= line.Quantity))
            });
        }

        state.SubTotal = state.Items.Sum(i => i.TotalPrice);
        state.TotalCount = state.Items.Sum(i => i.Quantity);
        return state;
    }

    // ------------------------------------------------------------------------
    // 10. HAM HELPER: LAY SO LUONG TON KHO CUA PHIEN BAN
    // ------------------------------------------------------------------------
    private async Task<int> GetStockAsync(int productId, int? variantId, CancellationToken cancellationToken)
    {
        if (variantId.HasValue)
            return await _db.ProductVariants.AsNoTracking()
                .Where(v => v.Id == variantId.Value && v.ProductId == productId)
                .Select(v => v.StockQuantity).FirstOrDefaultAsync(cancellationToken);

        return await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId && p.IsInStock).Select(_ => 50).FirstOrDefaultAsync(cancellationToken);
    }

    // ------------------------------------------------------------------------
    // 11. HAM HELPER: TAO OWNER KEY DINH DANH GIO HANG
    // Neu la thanh vien: 'user:{userId}', neu la khach: 'guest:{sha256_hash}'
    // ------------------------------------------------------------------------
    private static string? GetOwnerKey(string? userId, string? guestToken)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return "user:" + userId;
        if (string.IsNullOrWhiteSpace(guestToken)) return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(guestToken));
        return "guest:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
