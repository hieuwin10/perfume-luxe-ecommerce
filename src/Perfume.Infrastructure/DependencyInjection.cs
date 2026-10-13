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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required. Configure local secrets or environment variables.");

        services.AddDbContext<PerfumeDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(PerfumeDbContext).Assembly.FullName)));
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<PerfumeDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IDatabaseConsoleService, DatabaseConsoleService>();
        services.AddScoped<IPerfumeCatalogService, PerfumeCatalogService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerCartService, CustomerCartService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IOrderEmailSender, SmtpOrderEmailSender>();
        services.AddScoped<IFlashSaleService, FlashSaleService>();
        return services;
    }
}
