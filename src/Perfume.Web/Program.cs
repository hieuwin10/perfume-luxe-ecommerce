using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Perfume.Infrastructure;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Infrastructure Services (EF Core, Identity, Redis, Dapper DB Console)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Configure Identity Cookie Paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/accessdenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

// 3. Add MVC and Web API
builder.Services.AddControllersWithViews();

// 3. Add Distributed Memory Cache & Session for Cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 4. Ensure Database schema on startup (creates tables automatically without hardcoded seed data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<PerfumeDbContext>();
        await db.Database.EnsureCreatedAsync();
        logger.LogInformation(">>> Cơ sở dữ liệu SQL Server đã được kiểm tra & tạo bảng thành công! <<<");

        // Seed Roles & Default Users
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        string[] roles = ["Admin", "StoreManager", "Customer"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role, Description = role });
        }

        // Admin account
        if (await userManager.FindByEmailAsync("admin@perfumeluxe.com") == null)
        {
            var admin = new AppUser { UserName = "admin@perfumeluxe.com", Email = "admin@perfumeluxe.com", FullName = "Super Admin", EmailConfirmed = true };
            var r = await userManager.CreateAsync(admin, "Admin@2026!");
            if (r.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Store Manager account
        if (await userManager.FindByEmailAsync("manager@perfumeluxe.com") == null)
        {
            var mgr = new AppUser { UserName = "manager@perfumeluxe.com", Email = "manager@perfumeluxe.com", FullName = "Cửa Hàng Trưởng", EmailConfirmed = true };
            var r = await userManager.CreateAsync(mgr, "Manager@2026!");
            if (r.Succeeded) await userManager.AddToRoleAsync(mgr, "StoreManager");
        }

        // Demo Customer account
        if (await userManager.FindByEmailAsync("khachhang@gmail.com") == null)
        {
            var cust = new AppUser { UserName = "khachhang@gmail.com", Email = "khachhang@gmail.com", FullName = "Khách Hàng Demo", EmailConfirmed = true };
            var r = await userManager.CreateAsync(cust, "Customer@2026!");
            if (r.Succeeded) await userManager.AddToRoleAsync(cust, "Customer");
        }

        logger.LogInformation(">>> Roles & tài khoản mặc định đã được kiểm tra & tạo thành công! <<<");
    }
    catch (Exception ex)
    {
        logger.LogWarning("Không thể kết nối SQL Server tự động lúc này ({Message}). Hãy chắc chắn container SQL Server đang chạy qua 'docker compose -f docker-compose.infra.yml up -d'.", ex.Message);
    }
}

// 5. Configure HTTP request pipeline
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

// 6. Custom SEO & Friendly URLs
app.MapControllerRoute(
    name: "productDetail",
    pattern: "nuoc-hoa/{slug}",
    defaults: new { controller = "Catalog", action = "Detail" });

app.MapControllerRoute(
    name: "catalogFilter",
    pattern: "bo-suu-tap",
    defaults: new { controller = "Catalog", action = "Index" });

app.MapControllerRoute(
    name: "blogDetail",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Detail" });

app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Seo", action = "Sitemap" });

app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Seo", action = "Robots" });

// 7. Area Route (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// 8. Default MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
