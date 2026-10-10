using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Controllers;

public class ReviewsController : Controller
{
    private const int MaxImageCount = 3;
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private readonly PerfumeDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ReviewsController(PerfumeDbContext db, UserManager<AppUser> users, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _db = db;
        _users = users;
        _environment = environment;
        _configuration = configuration;
    }

    [Authorize(Roles = "Customer")]
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> Create(int productId, int rating, string comment, List<IFormFile>? images)
    {
        var user = await _users.GetUserAsync(User);
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (user == null || product == null) return NotFound();
        if (rating is < 1 or > 5 || string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 2000)
        {
            TempData["ReviewError"] = "Đánh giá cần có số sao từ 1 đến 5 và nội dung tối đa 2.000 ký tự.";
            return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
        }

        var purchased = await _db.Orders.AnyAsync(o => o.UserId == user.Id && o.OrderStatus == OrderStatus.Delivered &&
            o.Items.Any(i => i.ProductId == productId));
        if (!purchased)
        {
            TempData["ReviewError"] = "Chỉ khách đã mua và nhận sản phẩm mới có thể đánh giá.";
            return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
        }
        if (await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == user.Id))
        {
            TempData["ReviewError"] = "Bạn đã gửi đánh giá cho sản phẩm này.";
            return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
        }

        images ??= new();
        if (images.Count(f => f != null && f.Length > 0) > MaxImageCount ||
            images.Any(f => f != null && f.Length > MaxImageBytes))
        {
            TempData["ReviewError"] = "Mỗi đánh giá nhận tối đa 3 ảnh, mỗi ảnh tối đa 5 MB.";
            return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
        }

        var root = _configuration["Storage:RootPath"] ?? Path.Combine(_environment.ContentRootPath, "App_Data");
        var imageDirectory = Path.Combine(root, "ReviewImages");
        Directory.CreateDirectory(imageDirectory);
        var saved = new List<string>();
        try
        {
            foreach (var upload in images.Where(f => f != null && f.Length > 0))
            {
                var (extension, contentType) = await DetectImageAsync(upload);
                if (extension == null || contentType == null)
                {
                    foreach (var name in saved)
                    {
                        var invalidPath = Path.Combine(imageDirectory, name);
                        if (System.IO.File.Exists(invalidPath)) System.IO.File.Delete(invalidPath);
                    }
                    TempData["ReviewError"] = "Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP hợp lệ.";
                    return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
                }
                var safeName = $"{Guid.NewGuid():N}{extension}";
                await using var output = System.IO.File.Create(Path.Combine(imageDirectory, safeName));
                await upload.CopyToAsync(output);
                saved.Add(safeName);
            }

            _db.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = user.Id,
                UserName = user.FullName,
                Rating = rating,
                Comment = comment.Trim(),
                IsApproved = false,
                ImageUrls = JsonSerializer.Serialize(saved)
            });
            await _db.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đánh giá đã được gửi và sẽ hiển thị sau khi cửa hàng duyệt.";
        }
        catch
        {
            foreach (var name in saved)
            {
                var fullPath = Path.Combine(imageDirectory, name);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            TempData["ReviewError"] = "Chưa gửi được đánh giá. Vui lòng thử lại.";
        }

        return RedirectToAction("Detail", "Catalog", new { slug = product.Slug });
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int reviewId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)) return NotFound();
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null || (!review.IsApproved && !User.IsInRole("Admin") && !User.IsInRole("StoreManager"))) return NotFound();
        List<string> names;
        try { names = JsonSerializer.Deserialize<List<string>>(review.ImageUrls ?? "[]") ?? new(); }
        catch { return NotFound(); }
        if (!names.Contains(fileName, StringComparer.Ordinal)) return NotFound();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
        if (contentType == null) return NotFound();
        var root = _configuration["Storage:RootPath"] ?? Path.Combine(_environment.ContentRootPath, "App_Data");
        var path = Path.Combine(root, "ReviewImages", fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = review.IsApproved ? "public,max-age=3600" : "no-store";
        return PhysicalFile(path, contentType);
    }

    private static async Task<(string? Extension, string? ContentType)> DetectImageAsync(IFormFile upload)
    {
        if (upload.Length <= 0 || upload.Length > MaxImageBytes) return (null, null);
        var header = new byte[12];
        await using var stream = upload.OpenReadStream();
        var read = await stream.ReadAsync(header);
        var jpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var png = read >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                   header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
        if (jpeg) return (".jpg", "image/jpeg");
        if (png) return (".png", "image/png");
        if (webp) return (".webp", "image/webp");
        return (null, null);
    }
}