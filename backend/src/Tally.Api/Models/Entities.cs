using Microsoft.AspNetCore.Identity;

namespace Tally.Api.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}

// shared across all users, admins manage these
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#5B6B7A";
    public bool IsArchived { get; set; }
}

public enum PaymentMethod
{
    Card = 0,
    Cash = 1,
    BankTransfer = 2,
    Other = 3
}

public class Expense
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Budget
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Limit { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
