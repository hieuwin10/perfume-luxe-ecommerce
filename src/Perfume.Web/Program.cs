using Microsoft.EntityFrameworkCore;
using Perfume.Infrastructure;
using Perfume.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Infrastructure Services (EF Core, Identity, Redis, Dapper DB Console)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Add MVC and Web API
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
