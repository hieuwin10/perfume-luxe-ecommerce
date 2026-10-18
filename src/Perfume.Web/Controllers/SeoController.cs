using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Web.Seo;

namespace Perfume.Web.Controllers;

public class SeoController : Controller
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private readonly IPerfumeCatalogService _catalogService;
    private readonly IBlogService _blogService;
    private readonly IConfiguration _configuration;

    public SeoController(IPerfumeCatalogService catalogService, IBlogService blogService, IConfiguration? configuration = null)
    {
        _catalogService = catalogService;
        _blogService = blogService;
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken = default)
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var entries = new List<(string Url, DateTime? LastModified)>();

        Add("/");
        Add("/bo-suu-tap");
        Add("/discovery-box");
        Add("/so-sanh");
        Add("/catalog/quiz");
        Add("/dat-lich-trai-nghiem");
        Add("/boutique");
        Add("/ve-chung-toi");
        Add("/chinh-sach-doi-tra");
        Add("/huong-dan-mua-hang");
        Add("/privacy");
        Add("/blog");

        const int pageSize = 1000;
        var productPage = 1;
        while (true)
        {
            var (products, total) = await _catalogService.GetProductsAsync(new ProductFilterParams
            {
                Page = productPage,
                PageSize = pageSize,
                SortBy = "newest"
            });

            foreach (var product in products)
                Add($"/nuoc-hoa/{Uri.EscapeDataString(product.Slug)}", product.UpdatedAt ?? product.CreatedAt);

            if (products.Count == 0 || productPage * pageSize >= total) break;
            productPage++;
        }

        var categories = await _blogService.GetBlogCategoriesAsync();
        foreach (var category in categories)
        {
            Add($"/blog?category={Uri.EscapeDataString(category.Slug)}");
        }

        var blogPage = 1;
        while (true)
        {
            var (posts, totalPosts) = await _blogService.GetPostsPagedAsync(null, blogPage, pageSize);
            foreach (var post in posts)
                Add($"/blog/{Uri.EscapeDataString(post.Slug)}", post.UpdatedAt ?? post.CreatedAt);

            if (posts.Count == 0 || blogPage * pageSize >= totalPosts) break;
            blogPage++;
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset",
                entries.Select(entry => new XElement(SitemapNamespace + "url",
                    new XElement(SitemapNamespace + "loc", entry.Url),
                    entry.LastModified.HasValue
                        ? new XElement(SitemapNamespace + "lastmod", DateTime.SpecifyKind(entry.LastModified.Value, DateTimeKind.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        : null))));

        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);

        void Add(string path, DateTime? lastModified = null) => entries.Add(($"{baseUrl}{path}", lastModified));
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var content = string.Join('\n', new[]
        {
            "User-agent: *",
            "Disallow: /admin",
            "Disallow: /account",
            "Disallow: /cart",
            "Disallow: /checkout",
            "Disallow: /api",
            "Disallow: /swagger",
            "Disallow: /tra-cuu-don-hang",
            "Disallow: /order-tracking",
            "Allow: /",
            string.Empty,
            "User-agent: GPTBot",
            "Allow: /",
            "User-agent: PerplexityBot",
            "Allow: /",
            "User-agent: ClaudeBot",
            "Allow: /",
            "User-agent: Applebot-Extended",
            "Allow: /",
            "User-agent: Bytespider",
            "Allow: /",
            "User-agent: CCBot",
            "Allow: /",
            "User-agent: Google-Extended",
            "Allow: /",
            string.Empty,
            $"Sitemap: {baseUrl}/sitemap.xml",
            string.Empty
        });

        return Content(content, "text/plain", Encoding.UTF8);
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public IActionResult LlmsTxt()
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var sb = new StringBuilder();
        sb.AppendLine("# PERFUME LUXE - Haute Parfumerie & Niche Fragrances");
        sb.AppendLine();
        sb.AppendLine("> PERFUME LUXE là nhà phân phối và bán lẻ nước hoa Niche & Haute Parfumerie chính hãng hàng đầu tại Việt Nam, độc quyền và ủy quyền các nhà hương danh giá như Roja Parfums, Maison Francis Kurkdjian, Kilian Paris, Creed, Tom Ford Private Blend, Le Labo, Frederic Malle, Byredo, Diptyque, Clive Christian.");
        sb.AppendLine();
        sb.AppendLine("## Flagship Boutiques");
        sb.AppendLine("- Hà Nội: 12 Tràng Tiền, Quận Hoàn Kiếm, Hà Nội | Hotline: 024.3828.9999");
        sb.AppendLine("- TP. Hồ Chí Minh: 68 Đồng Khởi, Quận 1, TP. Hồ Chí Minh | Hotline: 028.3822.8888");
        sb.AppendLine("- Hotline VIP Concierge: 024.3828.9999");
        sb.AppendLine("- Email: concierge@perfumeluxe.vn");
        sb.AppendLine();
        sb.AppendLine("## Cam Kết & Dịch Vụ Độc Quyền");
        sb.AppendLine("- 100% Chính Hãng: Sản phẩm nhập khẩu nguyên bản với tem chống giả bảo chứng và cam kết bồi hoàn 200% nếu phát hiện không chuẩn.");
        sb.AppendLine("- Trải Nghiệm Thử Mùi VIP (Concierge Tasting): Buổi thẩm hương 1:1 trong phòng VIP riêng tư với chuyên gia mùi hương.");
        sb.AppendLine("- Hộp Khám Phá (Discovery Box): Bộ mẫu thử cao cấp do khách hàng tự chọn 3-5 hương thơm.");
        sb.AppendLine("- Giao Hàng Hỏa Tốc: 2H nội thành Hà Nội & TP.HCM trong bao bì nhung hộp cứng sang trọng.");
        sb.AppendLine();
        sb.AppendLine("## Liên Kết Chính");
        sb.AppendLine($"- Trang chủ: {baseUrl}/");
        sb.AppendLine($"- Bộ sưu tập: {baseUrl}/bo-suu-tap");
        sb.AppendLine($"- Discovery Box: {baseUrl}/discovery-box");
        sb.AppendLine($"- So sánh nước hoa: {baseUrl}/so-sanh");
        sb.AppendLine($"- Tìm kiếm hương thơm qua AI Quiz: {baseUrl}/catalog/quiz");
        sb.AppendLine($"- Đặt lịch thử mùi VIP: {baseUrl}/dat-lich-trai-nghiem");
        sb.AppendLine($"- Hệ thống Boutique: {baseUrl}/boutique");
        sb.AppendLine($"- Kiến thức mùi hương: {baseUrl}/blog");
        sb.AppendLine($"- Về chúng tôi: {baseUrl}/ve-chung-toi");
        sb.AppendLine($"- Chính sách đổi trả: {baseUrl}/chinh-sach-doi-tra");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
