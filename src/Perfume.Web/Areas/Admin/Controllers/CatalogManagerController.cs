using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,StoreManager")]
public class CatalogManagerController : Controller
{
    private readonly PerfumeDbContext _db;

    public CatalogManagerController(PerfumeDbContext db)
    {
        _db = db;
    }

    // ─── BRANDS ───────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Brands()
    {
        var brands = await _db.Brands
            .Include(b => b.Products)
            .OrderBy(b => b.Name)
            .ToListAsync();
        return View(brands);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBrand(int id, string name, string? country, string? logoUrl, string? description, bool isFeatured)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Vui lòng nhập tên thương hiệu.";
            return RedirectToAction(nameof(Brands));
        }

        var cleanName = name.Trim();
        var slug = cleanName.ToLowerInvariant().Replace(" ", "-").Replace("'", "");

        if (id == 0)
        {
            if (await _db.Brands.AnyAsync(b => b.Slug == slug))
                slug = $"{slug}-{DateTime.UtcNow.Ticks % 1000}";

            var brand = new Brand
            {
                Name = cleanName,
                Slug = slug,
                Country = country?.Trim(),
                LogoUrl = logoUrl?.Trim(),
                Description = description?.Trim(),
                IsFeatured = isFeatured,
                CreatedAt = DateTime.UtcNow
            };
            _db.Brands.Add(brand);
            TempData["Success"] = $"Đã thêm thương hiệu '{cleanName}'!";
        }
        else
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            brand.Name = cleanName;
            brand.Country = country?.Trim();
            brand.LogoUrl = logoUrl?.Trim();
            brand.Description = description?.Trim();
            brand.IsFeatured = isFeatured;
            brand.UpdatedAt = DateTime.UtcNow;
            TempData["Success"] = $"Đã cập nhật thương hiệu '{cleanName}'!";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Brands));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        var brand = await _db.Brands.Include(b => b.Products).FirstOrDefaultAsync(b => b.Id == id);
        if (brand == null) return NotFound();
        if (brand.Products.Count > 0)
        {
            TempData["Error"] = $"Không thể xóa thương hiệu '{brand.Name}' vì đang có {brand.Products.Count} sản phẩm liên kết.";
            return RedirectToAction(nameof(Brands));
        }

        brand.IsDeleted = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa thương hiệu '{brand.Name}'!";
        return RedirectToAction(nameof(Brands));
    }

    // ─── FRAGRANCE FAMILIES ───────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> FragranceFamilies()
    {
        var families = await _db.FragranceFamilies
            .Include(f => f.Products)
            .OrderBy(f => f.Name)
            .ToListAsync();
        return View(families);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFragranceFamily(int id, string name, string? colorHex, ScentFamily scentType, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Vui lòng nhập tên nhóm hương.";
            return RedirectToAction(nameof(FragranceFamilies));
        }

        var cleanName = name.Trim();
        var slug = cleanName.ToLowerInvariant().Replace(" ", "-");

        if (id == 0)
        {
            if (await _db.FragranceFamilies.AnyAsync(f => f.Slug == slug))
                slug = $"{slug}-{DateTime.UtcNow.Ticks % 1000}";

            var fam = new FragranceFamily
            {
                Name = cleanName,
                Slug = slug,
                ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#D4AF37" : colorHex.Trim(),
                ScentType = scentType,
                Description = description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.FragranceFamilies.Add(fam);
            TempData["Success"] = $"Đã thêm nhóm hương '{cleanName}'!";
        }
        else
        {
            var fam = await _db.FragranceFamilies.FindAsync(id);
            if (fam == null) return NotFound();
            fam.Name = cleanName;
            fam.ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#D4AF37" : colorHex.Trim();
            fam.ScentType = scentType;
            fam.Description = description?.Trim();
            fam.UpdatedAt = DateTime.UtcNow;
            TempData["Success"] = $"Đã cập nhật nhóm hương '{cleanName}'!";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(FragranceFamilies));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFragranceFamily(int id)
    {
        var fam = await _db.FragranceFamilies.Include(f => f.Products).FirstOrDefaultAsync(f => f.Id == id);
        if (fam == null) return NotFound();
        if (fam.Products.Count > 0)
        {
            TempData["Error"] = $"Không thể xóa nhóm hương '{fam.Name}' vì đang có {fam.Products.Count} sản phẩm liên kết.";
            return RedirectToAction(nameof(FragranceFamilies));
        }

        fam.IsDeleted = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa nhóm hương '{fam.Name}'!";
        return RedirectToAction(nameof(FragranceFamilies));
    }

    // ─── FRAGRANCE NOTES ──────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> FragranceNotes(string? search)
    {
        var query = _db.FragranceNotes
            .Include(n => n.ProductNotes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(n => n.Name.Contains(search));

        var notes = await query
            .OrderByDescending(n => n.ProductNotes.Count)
            .ThenBy(n => n.Name)
            .ToListAsync();

        ViewBag.Search = search;
        return View(notes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFragranceNote(int id, string name, string? imageUrl, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Vui lòng nhập tên nốt hương.";
            return RedirectToAction(nameof(FragranceNotes));
        }

        var cleanName = name.Trim();
        var slug = cleanName.ToLowerInvariant().Replace(" ", "-");

        if (id == 0)
        {
            if (await _db.FragranceNotes.AnyAsync(n => n.Name == cleanName))
            {
                TempData["Error"] = $"Nốt hương '{cleanName}' đã tồn tại.";
                return RedirectToAction(nameof(FragranceNotes));
            }

            var note = new FragranceNote
            {
                Name = cleanName,
                Slug = slug,
                ImageUrl = imageUrl?.Trim(),
                Description = description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.FragranceNotes.Add(note);
            TempData["Success"] = $"Đã thêm nốt hương '{cleanName}'!";
        }
        else
        {
            var note = await _db.FragranceNotes.FindAsync(id);
            if (note == null) return NotFound();
            note.Name = cleanName;
            note.ImageUrl = imageUrl?.Trim();
            note.Description = description?.Trim();
            note.UpdatedAt = DateTime.UtcNow;
            TempData["Success"] = $"Đã cập nhật nốt hương '{cleanName}'!";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(FragranceNotes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFragranceNote(int id)
    {
        var note = await _db.FragranceNotes.Include(n => n.ProductNotes).FirstOrDefaultAsync(n => n.Id == id);
        if (note == null) return NotFound();
        if (note.ProductNotes.Count > 0)
        {
            TempData["Error"] = $"Nốt hương '{note.Name}' đang được {note.ProductNotes.Count} chai nước hoa sử dụng, không thể xóa.";
            return RedirectToAction(nameof(FragranceNotes));
        }

        note.IsDeleted = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa nốt hương '{note.Name}'!";
        return RedirectToAction(nameof(FragranceNotes));
    }
}
