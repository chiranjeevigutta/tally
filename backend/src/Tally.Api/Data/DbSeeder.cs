using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tally.Api.Models;

namespace Tally.Api.Data;

public static class DbSeeder
{
    public const string AdminEmail = "admin@tally.dev";
    public const string AdminPassword = "Admin123!";
    public const string DemoEmail = "demo@tally.dev";
    public const string DemoPassword = "Demo123!";

    private static readonly (string Name, string Color)[] DefaultCategories =
    [
        ("Housing", "#2F6B4F"),
        ("Groceries", "#2A8C8C"),
        ("Dining out", "#E0A526"),
        ("Transport", "#5B6B7A"),
        ("Utilities", "#7A4E8C"),
        ("Health", "#C25B7A"),
        ("Entertainment", "#4C7BD9"),
        ("Shopping", "#9A6B3F"),
    ];

    public static async Task SeedAsync(IServiceProvider services, DateOnly? today = null)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();

        foreach (var role in new[] { Roles.Admin, Roles.User })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(DefaultCategories.Select(c => new Category { Name = c.Name, Color = c.Color }));
            await db.SaveChangesAsync();
        }

        await EnsureUserAsync(users, AdminEmail, AdminPassword, "Avery Admin", Roles.Admin);
        var demo = await EnsureUserAsync(users, DemoEmail, DemoPassword, "Chiranjeevi Gutta", Roles.User);

        if (!await db.Expenses.AnyAsync(e => e.UserId == demo.Id))
            await SeedDemoSpendingAsync(db, demo.Id, today ?? DateOnly.FromDateTime(DateTime.Today));
    }

    private static async Task<AppUser> EnsureUserAsync(UserManager<AppUser> users, string email, string password, string name, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is not null) return user;

        user = new AppUser { UserName = email, Email = email, DisplayName = name, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Seeding failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, role);
        return user;
    }

    // 6 months of fake spending for the demo account (fixed seed so it looks the same every time)
    private static async Task SeedDemoSpendingAsync(AppDbContext db, string userId, DateOnly today)
    {
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        var rng = new Random(42);
        var expenses = new List<Expense>();
        var budgets = new List<Budget>();

        var plan = new (string Cat, decimal Budget, (string Desc, decimal Min, decimal Max, int PerMonth)[] Items)[]
        {
            ("Housing", 1450m, new[] { ("Rent", 1400m, 1400m, 1) }),
            ("Groceries", 520m, new[] { ("Whole Foods", 45m, 120m, 3), ("Trader Joe's", 30m, 85m, 3), ("Farmers market", 15m, 40m, 1) }),
            ("Dining out", 260m, new[] { ("Coffee", 4m, 7m, 8), ("Lunch with team", 14m, 28m, 4), ("Dinner out", 35m, 90m, 2) }),
            ("Transport", 180m, new[] { ("Gas", 38m, 55m, 2), ("Transit pass", 50m, 50m, 1), ("Rideshare", 12m, 30m, 2) }),
            ("Utilities", 210m, new[] { ("Electric bill", 70m, 120m, 1), ("Internet", 65m, 65m, 1), ("Phone plan", 45m, 45m, 1) }),
            ("Health", 90m, new[] { ("Gym membership", 40m, 40m, 1), ("Pharmacy", 8m, 35m, 1) }),
            ("Entertainment", 120m, new[] { ("Streaming", 15.99m, 15.99m, 1), ("Movie night", 18m, 32m, 1), ("Concert tickets", 45m, 110m, 0) }),
            ("Shopping", 200m, new[] { ("Clothing", 30m, 140m, 1), ("Household supplies", 12m, 45m, 2), ("Books", 10m, 30m, 1) }),
        };

        for (var offset = 5; offset >= 0; offset--)
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-offset);
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var lastDay = offset == 0 ? today.Day : daysInMonth;

            foreach (var (cat, budget, items) in plan)
            {
                var categoryId = categories[cat];
                budgets.Add(new Budget { UserId = userId, CategoryId = categoryId, Year = monthStart.Year, Month = monthStart.Month, Limit = budget });

                foreach (var (desc, min, max, perMonth) in items)
                {
                    var count = perMonth + (perMonth != 1 && rng.NextDouble() < 0.3 ? 1 : 0);
                    for (var i = 0; i < count; i++)
                    {
                        var day = desc == "Rent" ? 1 : rng.Next(1, daysInMonth + 1);
                        if (day > lastDay) continue;
                        var amount = Math.Round(min + (decimal)rng.NextDouble() * (max - min), 2);
                        expenses.Add(new Expense
                        {
                            UserId = userId,
                            CategoryId = categoryId,
                            Amount = amount,
                            Date = monthStart.AddDays(day - 1),
                            Description = desc,
                            PaymentMethod = desc is "Rent" or "Internet" or "Phone plan" or "Electric bill"
                                ? PaymentMethod.BankTransfer
                                : rng.NextDouble() < 0.15 ? PaymentMethod.Cash : PaymentMethod.Card,
                        });
                    }
                }
            }
        }

        db.Budgets.AddRange(budgets);
        db.Expenses.AddRange(expenses);
        await db.SaveChangesAsync();
    }
}
