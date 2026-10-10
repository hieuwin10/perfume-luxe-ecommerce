using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Web.Extensions;

namespace Perfume.Web.Controllers;

public class CartController : Controller
{
    private const string CartSessionKey = "PERFUME_CART_SESSION";
    private readonly IPerfumeCatalogService _catalogService;

    public CartController(IPerfumeCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    private List<CartItemDto> GetCart()
    {
        return HttpContext.Session.GetObject<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
    }

    private void SaveCart(List<CartItemDto> cart)
    {
        HttpContext.Session.SetObject(CartSessionKey, cart);
    }

    [HttpGet]
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpGet]
    public IActionResult GetCartState()
    {
        var cart = GetCart();
        var totalAmount = cart.Sum(i => i.TotalPrice);
        var totalCount = cart.Sum(i => i.Quantity);
        return Json(new { items = cart, totalAmount, totalCount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        var product = await _catalogService.GetProductBySlugAsync(req.ProductSlug);
        if (product == null) return NotFound();

        var cart = GetCart();
        var variant = product.Variants.FirstOrDefault(v => v.Id == req.VariantId) 
                      ?? product.Variants.FirstOrDefault();

        var existingItem = cart.FirstOrDefault(i => i.ProductId == product.Id && i.VariantId == (variant?.Id));
        decimal price = variant != null && variant.SalePrice > 0 ? variant.SalePrice : product.BasePrice;
        string variantLabel = variant?.VolumeLabel ?? "Fullbox";

        if (existingItem != null)
        {
            existingItem.Quantity += Math.Max(1, req.Quantity);
        }
        else
        {
            cart.Add(new CartItemDto
            {
                ProductId = product.Id,
                VariantId = variant?.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                ProductImageUrl = product.MainImageUrl,
                VariantLabel = variantLabel,
                UnitPrice = price,
                Quantity = Math.Max(1, req.Quantity)
            });
        }

        SaveCart(cart);
        var totalAmount = cart.Sum(i => i.TotalPrice);
        var totalCount = cart.Sum(i => i.Quantity);

        return Json(new { success = true, items = cart, totalAmount, totalCount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity([FromBody] UpdateCartQuantityRequest req)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(i => i.ProductId == req.ProductId && i.VariantId == req.VariantId);
        if (item != null)
        {
            if (req.Quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = req.Quantity;
            }
            SaveCart(cart);
        }

        var totalAmount = cart.Sum(i => i.TotalPrice);
        var totalCount = cart.Sum(i => i.Quantity);
        return Json(new { success = true, items = cart, totalAmount, totalCount });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveItem([FromBody] RemoveCartItemRequest req)
    {
        var cart = GetCart();
        cart.RemoveAll(i => i.ProductId == req.ProductId && i.VariantId == req.VariantId);
        SaveCart(cart);

        var totalAmount = cart.Sum(i => i.TotalPrice);
        var totalCount = cart.Sum(i => i.Quantity);
        return Json(new { success = true, items = cart, totalAmount, totalCount });
    }
}

public class AddToCartRequest
{
    public string ProductSlug { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartQuantityRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int Quantity { get; set; }
}

public class RemoveCartItemRequest
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
}
