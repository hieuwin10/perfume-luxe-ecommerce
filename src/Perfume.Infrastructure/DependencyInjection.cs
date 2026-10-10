using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Perfume.Application.Interfaces;
using Perfume.Infrastructure.Data;
using Perfume.Infrastructure.Identity;
using Perfume.Infrastructure.Services;

namespace Perfume.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost,1433;Database=PerfumeDb;User Id=sa;Password=PerfumePass@2026!;TrustServerCertificate=True;";

        services.AddDbContext<PerfumeDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(PerfumeDbContext).Assembly.FullName)));

        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<PerfumeDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IDatabaseConsoleService, DatabaseConsoleService>();
        services.AddScoped<IPerfumeCatalogService, PerfumeCatalogService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IBlogService, BlogService>();

        return services;
    }
}
