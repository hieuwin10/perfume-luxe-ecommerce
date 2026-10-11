using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public sealed class CustomerCartService : ICustomerCartService
{
    private readonly PerfumeDbContext _db;
    private static readonly TimeSpan GuestCartLifetime = TimeSpan.FromDays(30);

    public CustomerCartService(PerfumeDbContext db) => _db = db;

    public async Task<CartStateDto> GetAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var ownerKey = GetOwnerKey(userId, guestToken);
        if (ownerKey == null) return new CartStateDto();
        var cart = await _db.ShoppingCarts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (cart == null) return new CartStateDto();
        return await BuildStateAsync(cart, cancellationToken);
    }

    public async Task<CartStateDto> AddAsync(string? userId, string? guestToken, CartAddItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity is < 1 or > 50) throw new InvalidOperationException("Số lượng phải từ 1 đến 50.");
        var product = await _db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == request.ProductSlug && p.IsInStock, cancellationToken);
        if (product == null) throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng bán.");
        if (product.Variants.Count == 0 && request.VariantId.HasValue)
            throw new KeyNotFoundException("Sản phẩm này không có phiên bản đã chọn.");

        ProductVariant? variant = null;
        if (product.Variants.Count > 0)
        {
            variant = request.VariantId.HasValue
                ? product.Variants.FirstOrDefault(v => v.Id == request.VariantId.Value)
                : product.Variants.FirstOrDefault(v => v.IsDefault) ?? product.Variants.FirstOrDefault();
            if (variant == null) throw new KeyNotFoundException("Phiên bản sản phẩm không hợp lệ.");
            if (variant.StockQuantity <= 0) throw new InvalidOperationException("Phiên bản này đã hết hàng.");
        }

        var cart = await GetOrCreateCartAsync(userId, guestToken, cancellationToken);
        var line = cart.Items.FirstOrDefault(i => i.ProductId == product.Id && i.VariantId == variant?.Id);
        var nextQuantity = (line?.Quantity ?? 0) + request.Quantity;
        // Products without variants expose only an in-stock flag, not a stock count.
        // Keep the per-line safety limit at 50 while the order service revalidates availability.
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
        cart.ExpiresAt = DateTime.UtcNow.Add(GuestCartLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildStateAsync(cart, cancellationToken);
    }

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
                account.Items.Add(new ShoppingCartItem { ProductId = guestLine.ProductId, VariantId = guestLine.VariantId, Quantity = Math.Min(guestLine.Quantity, Math.Min(stock, 50)) });
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

    public async Task ClearAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null) return;
        _db.ShoppingCartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ShoppingCart?> FindCartAsync(string? userId, string? guestToken, CancellationToken cancellationToken)
    {
        var ownerKey = GetOwnerKey(userId, guestToken);
        if (ownerKey == null) return null;
        return await _db.ShoppingCarts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.OwnerKey == ownerKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

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

    private async Task<CartStateDto> BuildStateAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        var state = new CartStateDto();
        foreach (var line in cart.Items)
        {
            var product = await _db.Products.Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
            if (product == null) continue;
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

    private async Task<int> GetStockAsync(int productId, int? variantId, CancellationToken cancellationToken)
    {
        if (variantId.HasValue)
            return await _db.ProductVariants.Where(v => v.Id == variantId.Value && v.ProductId == productId)
                .Select(v => v.StockQuantity).FirstOrDefaultAsync(cancellationToken);
        return await _db.Products.Where(p => p.Id == productId && p.IsInStock).Select(_ => 50).FirstOrDefaultAsync(cancellationToken);
    }

    private static string? GetOwnerKey(string? userId, string? guestToken)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return "user:" + userId;
        if (string.IsNullOrWhiteSpace(guestToken)) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(guestToken));
        return "guest:" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
