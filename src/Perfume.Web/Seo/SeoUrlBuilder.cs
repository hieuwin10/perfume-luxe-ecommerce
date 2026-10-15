using System.Net;

namespace Perfume.Web.Seo;

public static class SeoUrlBuilder
{
    public static string GetSiteBaseUrl(HttpRequest? request, IConfiguration configuration)
    {
        var configured = configuration["Seo:SiteBaseUrl"]?.Trim();
        if (Uri.TryCreate(configured, UriKind.Absolute, out var siteUri) &&
            (siteUri.Scheme == Uri.UriSchemeHttp || siteUri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(siteUri.UserInfo) &&
            (siteUri.Scheme == Uri.UriSchemeHttps || IsLocalHost(siteUri.Host)))
        {
            return siteUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        if (request == null) return "https://perfumeluxe.vn";
        if (!request.Host.HasValue || Uri.CheckHostName(request.Host.Host) == UriHostNameType.Unknown)
            return "https://perfumeluxe.vn";

        var localHost = IsLocalHost(request.Host.Host);
        var scheme = localHost && request.Scheme is "http" or "https" ? request.Scheme : Uri.UriSchemeHttps;
        var port = request.Host.Port;
        if (port == (scheme == Uri.UriSchemeHttps ? 443 : 80)) port = null;
        return new UriBuilder(scheme, request.Host.Host, port ?? -1).Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    public static string AbsoluteUrl(HttpRequest? request, IConfiguration configuration, string pathAndQuery)
    {
        if (Uri.TryCreate(pathAndQuery, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.AbsoluteUri;
        }

        var baseUrl = GetSiteBaseUrl(request, configuration);
        var path = pathAndQuery.StartsWith('/') ? pathAndQuery : $"/{pathAndQuery}";
        return $"{baseUrl}{path}";
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
}
