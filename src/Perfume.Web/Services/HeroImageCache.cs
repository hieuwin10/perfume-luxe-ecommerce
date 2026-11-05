namespace Perfume.Web.Services;

/// <summary>
/// Cache chuỗi Base64 Data-URI cho ảnh Hero Mobile nhằm tối ưu triệt để LCP (Largest Contentful Paint).
/// Trình duyệt di động nhận dữ liệu ảnh ngay trong dòng HTML đầu tiên mà không cần mở thêm kết nối HTTP.
/// </summary>
public static class HeroImageCache
{
    private static string? _mobileDataUri;
    private static readonly object _lock = new();

    public static string GetMobileDataUri(string? webRootPath = null)
    {
        if (_mobileDataUri != null) return _mobileDataUri;

        lock (_lock)
        {
            if (_mobileDataUri != null) return _mobileDataUri;

            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(webRootPath))
            {
                candidates.Add(Path.Combine(webRootPath, "images", "hero", "hero-mobile.webp"));
            }

            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "hero", "hero-mobile.webp"));
            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "hero", "hero-mobile.webp"));

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(path);
                        _mobileDataUri = $"data:image/webp;base64,{Convert.ToBase64String(bytes)}";
                        return _mobileDataUri;
                    }
                    catch
                    {
                        // Bo qua va thu tiep candidate khac
                    }
                }
            }

            _mobileDataUri = string.Empty;
            return _mobileDataUri;
        }
    }
}
