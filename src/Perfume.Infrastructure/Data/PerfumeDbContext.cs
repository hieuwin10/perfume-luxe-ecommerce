using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Perfume.Domain.Entities;
using Perfume.Infrastructure.Identity;

namespace Perfume.Infrastructure.Data;

public class PerfumeDbContext : IdentityDbContext<AppUser, AppRole, string>
{
    public PerfumeDbContext(DbContextOptions<PerfumeDbContext> options) : base(options)
    {
    }

    // Catalog DbSets
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FragranceFamily> FragranceFamilies => Set<FragranceFamily>();
    public DbSet<FragranceNote> FragranceNotes => Set<FragranceNote>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductFragranceNote> ProductFragranceNotes => Set<ProductFragranceNote>();
    public DbSet<ProductImage> Images => Set<ProductImage>();

    // Ecommerce DbSets
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<DatabaseAuditLog> DatabaseAuditLogs => Set<DatabaseAuditLog>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<UserLoyalty> UserLoyalties => Set<UserLoyalty>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables for clean schema
        builder.Entity<AppUser>(entity => entity.ToTable("Users"));
        builder.Entity<AppRole>(entity => entity.ToTable("Roles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity => entity.ToTable("UserRoles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity => entity.ToTable("UserClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity => entity.ToTable("UserLogins"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity => entity.ToTable("RoleClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity => entity.ToTable("UserTokens"));

        // Brand
        builder.Entity<Brand>(entity =>
        {
            entity.HasIndex(b => b.Slug).IsUnique();
            entity.Property(b => b.Name).HasMaxLength(150).IsRequired();
            entity.HasQueryFilter(b => !b.IsDeleted);
        });

        // Category
        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(150).IsRequired();
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        // FragranceFamily
        builder.Entity<FragranceFamily>(entity =>
        {
            entity.HasIndex(f => f.Slug).IsUnique();
            entity.Property(f => f.Name).HasMaxLength(150).IsRequired();
            entity.HasQueryFilter(f => !f.IsDeleted);
        });

        // FragranceNote
        builder.Entity<FragranceNote>(entity =>
        {
            entity.HasIndex(n => n.Slug).IsUnique();
            entity.Property(n => n.Name).HasMaxLength(150).IsRequired();
            entity.HasQueryFilter(n => !n.IsDeleted);
        });

        // Product
        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(255).IsRequired();
            entity.Property(p => p.BasePrice).HasPrecision(18, 2);

            entity.HasOne(p => p.Brand)
                  .WithMany(b => b.Products)
                  .HasForeignKey(p => p.BrandId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.FragranceFamily)
                  .WithMany(f => f.Products)
                  .HasForeignKey(p => p.FragranceFamilyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // ProductVariant
        builder.Entity<ProductVariant>(entity =>
        {
            entity.Property(pv => pv.OriginalPrice).HasPrecision(18, 2);
            entity.Property(pv => pv.SalePrice).HasPrecision(18, 2);

            entity.HasOne(pv => pv.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(pv => pv.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(pv => !pv.IsDeleted);
        });

        // ProductFragranceNote (Many-to-Many linking with NoteLevel)
        builder.Entity<ProductFragranceNote>(entity =>
        {
            entity.HasKey(pfn => new { pfn.ProductId, pfn.FragranceNoteId, pfn.NoteLevel });

            entity.HasOne(pfn => pfn.Product)
                  .WithMany(p => p.FragranceNotes)
                  .HasForeignKey(pfn => pfn.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pfn => pfn.FragranceNote)
                  .WithMany(fn => fn.ProductNotes)
                  .HasForeignKey(pfn => pfn.FragranceNoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductImage
        builder.Entity<ProductImage>(entity =>
        {
            entity.HasOne(pi => pi.Product)
                  .WithMany(p => p.Images)
                  .HasForeignKey(pi => pi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(pi => !pi.IsDeleted);
        });

        // Order & OrderItem
        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.Property(o => o.SubTotal).HasPrecision(18, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            entity.Property(o => o.ShippingFee).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.HasQueryFilter(o => !o.IsDeleted);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(oi => !oi.IsDeleted);
        });

        // Voucher
        builder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(v => v.Code).IsUnique();
            entity.Property(v => v.DiscountPercent).HasPrecision(5, 2);
            entity.Property(v => v.DiscountAmount).HasPrecision(18, 2);
            entity.Property(v => v.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(v => v.MaxDiscountAmount).HasPrecision(18, 2);
            entity.HasQueryFilter(v => !v.IsDeleted);
        });

        // Review
        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.Product)
                  .WithMany(p => p.Reviews)
                  .HasForeignKey(r => r.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        // Blog
        builder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(b => b.Slug).IsUnique();
            entity.HasOne(b => b.Category)
                  .WithMany(c => c.Posts)
                  .HasForeignKey(b => b.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(b => !b.IsDeleted);
        });

        builder.Entity<BlogCategory>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.HasQueryFilter(c => !c.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
