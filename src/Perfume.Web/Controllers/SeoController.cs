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
            "Allow: /llms.txt",
            "Allow: /.well-known/llms.txt",
            "Allow: /llms-full.txt",
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

        return Content(content, "text/plain; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public IActionResult LlmsTxt()
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var sb = new StringBuilder();
        sb.AppendLine("# PERFUME LUXE");
        sb.AppendLine();
        sb.AppendLine("> PERFUME LUXE là nhà phân phối và bán lẻ nước hoa Niche & Haute Parfumerie chính hãng hàng đầu tại Việt Nam, ủy quyền các nhà hương danh giá như Roja Parfums, Maison Francis Kurkdjian, Kilian Paris, Creed, Tom Ford Private Blend, Le Labo, Byredo.");
        sb.AppendLine();
        sb.AppendLine("## Danh Mục & Dịch Vụ");
        sb.AppendLine($"- [Trang Chủ]({baseUrl}/): Cửa hàng trực tuyến nước hoa xa xỉ chính hãng.");
        sb.AppendLine($"- [Bộ Sưu Tập Nước Hoa]({baseUrl}/bo-suu-tap): Danh mục nước hoa Niche, chiết 10ml và fullbox 100ml.");
        sb.AppendLine($"- [Hộp Quà Discovery Box]({baseUrl}/discovery-box): Bộ mẫu thử 3-5 mùi hương cao cấp tự chọn.");
        sb.AppendLine($"- [AI Trắc Nghiệm Tìm Mùi]({baseUrl}/catalog/quiz): Công cụ AI gợi ý mùi hương định danh cá nhân.");
        sb.AppendLine($"- [So Sánh Mùi Hương]({baseUrl}/so-sanh): Bảng so sánh nốt hương và độ lưu hương trực quan.");
        sb.AppendLine($"- [Đặt Lịch Thử Mùi VIP]({baseUrl}/dat-lich-trai-nghiem): Dịch vụ thử mùi 1:1 riêng tư với chuyên gia.");
        sb.AppendLine($"- [Tạp Chí Mùi Hương]({baseUrl}/blog): Kiến thức và cẩm nang phân tầng hương nước hoa.");
        sb.AppendLine($"- [Hệ Thống Flagship Boutique]({baseUrl}/boutique): Danh sách showroom tại Hà Nội và TP.HCM.");
        sb.AppendLine($"- [Về Chúng Tôi]({baseUrl}/ve-chung-toi): Tuyên ngôn di sản và sứ mệnh của PERFUME LUXE.");
        sb.AppendLine($"- [Chính Sách Đổi Trả & Bảo Hành]({baseUrl}/chinh-sach-doi-tra): Cam kết 100% xuất xứ và chính sách 7 ngày.");
        sb.AppendLine();
        sb.AppendLine("## Flagship Boutiques");
        sb.AppendLine("- Hà Nội: 12 Tràng Tiền, Quận Hoàn Kiếm, Hà Nội | Hotline: 024.3828.9999");
        sb.AppendLine("- TP. Hồ Chí Minh: 68 Đồng Khởi, Quận 1, TP. Hồ Chí Minh | Hotline: 028.3822.8888");
        sb.AppendLine("- Hotline VIP Concierge: 024.3828.9999");
        sb.AppendLine("- Email: concierge@perfumeluxe.vn");
        sb.AppendLine();
        sb.AppendLine("## Optional");
        sb.AppendLine($"- [llms-full.txt]({baseUrl}/llms-full.txt): Toàn bộ thông tin mở rộng dành cho LLM và AI Agents.");

        return Content(sb.ToString(), "text/markdown; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet]
    [ResponseCache(Duration = 3600)]
    public IActionResult LlmsFullTxt()
    {
        var baseUrl = SeoUrlBuilder.GetSiteBaseUrl(Request, _configuration);
        var sb = new StringBuilder();
        sb.AppendLine("# PERFUME LUXE - Báo Cáo Toàn Diện Cho AI Agents");
        sb.AppendLine();
        sb.AppendLine("> Tài liệu chi tiết mô tả hệ thống danh mục, sản phẩm nước hoa niche, chính sách bảo hành, cam kết chính hãng và hệ thống cửa hàng PERFUME LUXE.");
        sb.AppendLine();
        sb.AppendLine("## Danh Mục Sản Phẩm Chính");
        sb.AppendLine($"- [Bộ Sưu Tập Nước Hoa Niche]({baseUrl}/bo-suu-tap): Tuyển tập Roja Parfums, Creed Aventus, MFK Baccarat Rouge 540, Kilian Angels' Share, Tom Ford Lost Cherry.");
        sb.AppendLine($"- [Discovery Box]({baseUrl}/discovery-box): Bộ sưu tập thử mùi 3 ống hoặc 5 ống 2ml tùy chọn.");
        sb.AppendLine($"- [Trắc Nghiệm Mùi Hương AI]({baseUrl}/catalog/quiz): Bộ câu hỏi thông minh giúp xác định phong cách mùi hương phù hợp.");
        sb.AppendLine();
        sb.AppendLine("## Chính Sách & Cam Kết");
        sb.AppendLine("- 100% Chính Hãng: Hoàn tiền 200% nếu phát hiện hàng giả.");
        sb.AppendLine("- Đổi Trả Miễn Phí: Trong vòng 7 ngày nếu còn nguyên tem niêm phong.");
        sb.AppendLine("- Giao Hàng Hỏa Tốc: Nội thành 2 giờ trong bao bì sang trọng.");
        sb.AppendLine();
        sb.AppendLine("## Flagship Boutiques");
        sb.AppendLine("- Hà Nội: 12 Tràng Tiền, Hoàn Kiếm, Hà Nội.");
        sb.AppendLine("- TP.HCM: 68 Đồng Khởi, Quận 1, TP. Hồ Chí Minh.");
        sb.AppendLine($"- Website chính thức: {baseUrl}/");

        return Content(sb.ToString(), "text/markdown; charset=utf-8", Encoding.UTF8);
    }
}
