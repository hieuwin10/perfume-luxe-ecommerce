using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Perfume.Infrastructure;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
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
builder.Services.AddAuthorization(options => options.AddPolicy("CustomerApi", policy =>
{
    policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, "Bearer");
    policy.RequireAuthenticatedUser();
}));

var dataPath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(Path.Combine(dataPath, "DataProtection-Keys"));
Directory.CreateDirectory(Path.Combine(dataPath, "ReviewImages"));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, "DataProtection-Keys")))
    .SetApplicationName("PerfumeLuxe");
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute("productDetail", "nuoc-hoa/{slug}", new { controller = "Catalog", action = "Detail" });
app.MapControllerRoute("catalogFilter", "bo-suu-tap", new { controller = "Catalog", action = "Index" });
app.MapControllerRoute("blogDetail", "blog/{slug}", new { controller = "Blog", action = "Detail" });
app.MapControllerRoute("sitemap", "sitemap.xml", new { controller = "Seo", action = "Sitemap" });
app.MapControllerRoute("robots", "robots.txt", new { controller = "Seo", action = "Robots" });
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
