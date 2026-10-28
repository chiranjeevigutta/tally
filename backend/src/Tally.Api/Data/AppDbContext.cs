using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tally.Api.Models;

namespace Tally.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(60).IsRequired();
            e.Property(c => c.Color).HasMaxLength(9).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        builder.Entity<Expense>(e =>
        {
            // sqlite can't sum or sort decimals, store as double
            e.Property(x => x.Amount).HasConversion<double>();
            e.Property(x => x.Description).HasMaxLength(140).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne(x => x.User).WithMany(u => u.Expenses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Budget>(e =>
        {
            e.Property(x => x.Limit).HasConversion<double>();
            e.HasIndex(x => new { x.UserId, x.CategoryId, x.Year, x.Month }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.Budgets).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
