using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Models;

namespace Tally.Tests;

// in-memory sqlite so the tests hit the same provider as the real app
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Context { get; }

    public const string Alice = "user-alice";
    public const string Bob = "user-bob";
    public int Groceries { get; }
    public int Transport { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();

        Context.Users.AddRange(
            new AppUser { Id = Alice, UserName = "alice@test.dev", Email = "alice@test.dev", DisplayName = "Alice" },
            new AppUser { Id = Bob, UserName = "bob@test.dev", Email = "bob@test.dev", DisplayName = "Bob" });

        var groceries = new Category { Name = "Groceries", Color = "#2A8C8C" };
        var transport = new Category { Name = "Transport", Color = "#5B6B7A" };
        Context.Categories.AddRange(groceries, transport);
        Context.SaveChanges();

        Groceries = groceries.Id;
        Transport = transport.Id;
    }

    public void AddExpense(string userId, int categoryId, decimal amount, DateOnly date, string description = "Test")
    {
        Context.Expenses.Add(new Expense { UserId = userId, CategoryId = categoryId, Amount = amount, Date = date, Description = description });
        Context.SaveChanges();
    }

    public void AddBudget(string userId, int categoryId, int year, int month, decimal limit)
    {
        Context.Budgets.Add(new Budget { UserId = userId, CategoryId = categoryId, Year = year, Month = month, Limit = limit });
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
