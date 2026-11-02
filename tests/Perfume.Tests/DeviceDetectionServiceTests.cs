using Microsoft.AspNetCore.Http;
using Perfume.Web.Services;
using Xunit;

namespace Perfume.Tests;

public sealed class DeviceDetectionServiceTests
{
    private static DeviceDetectionService CreateService(Action<DefaultHttpContext> configure)
    {
        var context = new DefaultHttpContext();
        configure(context);
        var accessor = new HttpContextAccessor { HttpContext = context };
        return new DeviceDetectionService(accessor);
    }

    [Fact]
    public void IsMobile_ReturnsTrue_WhenClientHintIsMobile()
    {
        var service = CreateService(ctx =>
        {
            ctx.Request.Headers["Sec-CH-UA-Mobile"] = "?1";
            ctx.Request.Headers.UserAgent = "Mozilla/5.0 (Linux; Android 14; SM-F946B) AppleWebKit/537.36";
        });

        Assert.True(service.IsMobile());
        Assert.False(service.IsDesktop());
        Assert.Equal("Mobile", service.GetDeviceType());
    }

    [Fact]
    public void IsDesktop_ReturnsTrue_WhenClientHintIsDesktop()
    {
        var service = CreateService(ctx =>
        {
            ctx.Request.Headers["Sec-CH-UA-Mobile"] = "?0";
            ctx.Request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";
        });

        Assert.True(service.IsDesktop());
        Assert.False(service.IsMobile());
        Assert.Equal("Desktop", service.GetDeviceType());
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-S928B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-F936B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Mobile Safari/537.36")] // Galaxy Z Fold Folded
    public void IsMobile_ReturnsTrue_ForMobileUserAgents(string userAgent)
    {
        var service = CreateService(ctx =>
        {
            ctx.Request.Headers.UserAgent = userAgent;
        });

        Assert.True(service.IsMobile());
        Assert.False(service.IsDesktop());
    }

    [Theory]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-X906B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36")] // Android Tablet (no Mobile)
    public void IsTablet_ReturnsTrue_ForTabletUserAgents(string userAgent)
    {
        var service = CreateService(ctx =>
        {
            ctx.Request.Headers.UserAgent = userAgent;
        });

        Assert.True(service.IsTablet());
        Assert.False(service.IsMobile());
        Assert.Equal("Tablet", service.GetDeviceType());
    }

    [Fact]
    public void Service_HandlesEmptyContext_Gracefully()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var service = new DeviceDetectionService(accessor);

        Assert.False(service.IsMobile());
        Assert.False(service.IsTablet());
        Assert.True(service.IsDesktop());
        Assert.Equal("Desktop", service.GetDeviceType());
    }
}

