namespace Perfume.Web.Seo;

public static class SeoUrlBuilder
{
    public static string GetSiteBaseUrl(HttpRequest? request, IConfiguration configuration)
    {
        var configured = configuration["Seo:SiteBaseUrl"]?.Trim();
        if (Uri.TryCreate(configured, UriKind.Absolute, out var siteUri) &&
            (siteUri.Scheme == Uri.UriSchemeHttp || siteUri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(siteUri.UserInfo))
        {
            return siteUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        if (request == null) return "https://perfumeluxe.vn";
        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
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
}
