using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Perfume.Web.Services;

public interface IDeviceDetectionService
{
    bool IsMobile();
    bool IsTablet();
    bool IsDesktop();
    string GetDeviceType();
}

public sealed partial class DeviceDetectionService(IHttpContextAccessor httpContextAccessor) : IDeviceDetectionService
{
    public bool IsMobile()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return false;

        // 1. Kiem tra W3C Client Hint hien dai (Sec-CH-UA-Mobile: ?1)
        if (context.Request.Headers.TryGetValue("Sec-CH-UA-Mobile", out var chMobile))
        {
            if (chMobile.ToString().Contains("?1"))
            {
                return true;
            }
        }

        // 2. Fallback sang User-Agent
        var ua = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua)) return false;

        // Neu la Tablet thi khong phai Mobile phone
        if (IsTabletInternal(ua)) return false;

        return MobileRegex().IsMatch(ua);
    }

    public bool IsTablet()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return false;

        var ua = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(ua)) return false;

        return IsTabletInternal(ua);
    }

    public bool IsDesktop()
    {
        return !IsMobile() && !IsTablet();
    }

    public string GetDeviceType()
    {
        if (IsMobile()) return "Mobile";
        if (IsTablet()) return "Tablet";
        return "Desktop";
    }

    private static bool IsTabletInternal(string ua)
    {
        if (TabletRegex().IsMatch(ua)) return true;

        // Android tablet thuong co "Android" trong UA nhung khong co "Mobile"
        if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase) &&
            !ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    [GeneratedRegex(@"iphone|ipod|android.*mobile|windows phone|blackberry|bb10|mobile|opera mini|iemobile", RegexOptions.IgnoreCase)]
    private static partial Regex MobileRegex();

    [GeneratedRegex(@"ipad|tablet|playbook|silk|kindle", RegexOptions.IgnoreCase)]
    private static partial Regex TabletRegex();
}

