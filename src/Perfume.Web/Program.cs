using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Perfume.Infrastructure;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Web.Middlewares;
using Perfume.Web.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/accessdenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, BearerTokenHandler>("Bearer", _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerApi", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, "Bearer");
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Customer");
    });
    options.AddPolicy("StoreManagerApi", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, "Bearer");
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "StoreManager");
    });
});

var dataPath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(Path.Combine(dataPath, "DataProtection-Keys"));
Directory.CreateDirectory(Path.Combine(dataPath, "ReviewImages"));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, "DataProtection-Keys")))
    .SetApplicationName("PerfumeLuxe");
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<PerfumeDbContext>();
    var databaseCreated = await db.Database.EnsureCreatedAsync();
    await DatabaseSchemaUpgrader.ApplyAsync(db, builder.Configuration, logger, databaseCreated);

    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    foreach (var role in new[] { "Admin", "StoreManager", "WarehouseStaff", "Concierge", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var created = await roleManager.CreateAsync(new AppRole { Name = role, Description = role });
            if (!created.Succeeded) logger.LogError("Could not create role {Role}.", role);
        }
    }

    var adminEmail = builder.Configuration["InitialAdmin:Email"];
    var adminPassword = builder.Configuration["InitialAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new AppUser { UserName = adminEmail, Email = adminEmail, FullName = "System Administrator", EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            else logger.LogError("Initial administrator was not created; check password policy and local environment settings.");
        }
        else
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            await userManager.ResetPasswordAsync(existingAdmin, token, adminPassword);
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                await userManager.AddToRoleAsync(existingAdmin, "Admin");
            }
        }
    }
}

app.UseMiddleware<SystemExceptionLoggingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com https://www.google-analytics.com https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
            "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com data:; " +
            "img-src 'self' https: data: blob:; " +
            "connect-src 'self' https://www.google-analytics.com https://region1.google-analytics.com https://images.unsplash.com https://*.google-analytics.com; " +
            "frame-ancestors 'self';");
    }
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("Server");
    await next();
});
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000,immutable");
    }
});
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase) &&
        context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        var bearer = await context.AuthenticateAsync("Bearer");
        if (!bearer.Succeeded || bearer.Principal == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.User = bearer.Principal;
    }

    await next();
});
app.UseAuthorization();
app.UseMiddleware<MaintenanceMiddleware>();
app.UseSession();
app.MapControllerRoute("productDetail", "nuoc-hoa/{slug}", new { controller = "Catalog", action = "Detail" });
app.MapControllerRoute("catalogFilter", "bo-suu-tap", new { controller = "Catalog", action = "Index" });
app.MapControllerRoute("blogDetail", "blog/{slug}", new { controller = "Blog", action = "Detail" });
app.MapControllerRoute("sitemap", "sitemap.xml", new { controller = "Seo", action = "Sitemap" });
app.MapControllerRoute("robots", "robots.txt", new { controller = "Seo", action = "Robots" });
app.MapControllerRoute("llms", "llms.txt", new { controller = "Seo", action = "LlmsTxt" });
app.MapControllerRoute("llmsWellKnown", ".well-known/llms.txt", new { controller = "Seo", action = "LlmsTxt" });
app.MapControllerRoute("llmsFull", "llms-full.txt", new { controller = "Seo", action = "LlmsFullTxt" });
app.MapControllerRoute("areas", "{area:exists}/{controller=AdminDashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
