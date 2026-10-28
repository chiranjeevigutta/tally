using System.ComponentModel.DataAnnotations;
using Tally.Api.Models;

namespace Tally.Api.Dtos;

// Auth
public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, StringLength(60, MinimumLength = 2)] string DisplayName);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record UserDto(string Id, string Email, string DisplayName, string Currency, IReadOnlyList<string> Roles);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, UserDto User);

// Categories
public record CategoryDto(int Id, string Name, string Color, bool IsArchived);

public record SaveCategoryRequest(
    [Required, StringLength(60, MinimumLength = 2)] string Name,
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a hex value like #2F6B4F.")] string Color,
    bool IsArchived = false);

// Expenses
public record ExpenseDto(
    int Id, decimal Amount, DateOnly Date, string Description, string? Notes,
    PaymentMethod PaymentMethod, int CategoryId, string CategoryName, string CategoryColor);

public record SaveExpenseRequest(
    [Range(0.01, 1_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")] decimal Amount,
    [Required] DateOnly Date,
    [Required, StringLength(140, MinimumLength = 1)] string Description,
    [StringLength(500)] string? Notes,
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")] int CategoryId,
    PaymentMethod PaymentMethod = PaymentMethod.Card);

public class ExpenseQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    // date, amount or description
    public string Sort { get; set; } = "date";
    public bool Desc { get; set; } = true;
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, decimal TotalAmount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// Budgets
public record BudgetLineDto(int? BudgetId, int CategoryId, string CategoryName, string CategoryColor, decimal Limit, decimal Spent)
{
    public decimal Remaining => Limit - Spent;
    public double PercentUsed => Limit <= 0 ? 0 : Math.Round((double)(Spent / Limit) * 100, 1);
}

public record SetBudgetRequest(int Year, [Range(1, 12)] int Month, [Range(1, int.MaxValue)] int CategoryId, [Range(0, 1_000_000)] decimal Limit);

public record CopyBudgetsRequest(int Year, [Range(1, 12)] int Month);

// Dashboard
public record CategorySpendDto(int CategoryId, string Name, string Color, decimal Spent, decimal Budget);

public record MonthTotalDto(int Year, int Month, decimal Spent, decimal Budget);

public record DashboardDto(
    int Year, int Month,
    decimal TotalSpent, decimal TotalBudget, int TransactionCount,
    decimal DailyAverage, decimal ProjectedSpend,
    decimal PreviousMonthSpent,
    IReadOnlyList<CategorySpendDto> ByCategory,
    IReadOnlyList<MonthTotalDto> Trend,
    IReadOnlyList<ExpenseDto> Recent);

// Admin
public record AdminUserDto(string Id, string Email, string DisplayName, IReadOnlyList<string> Roles, int ExpenseCount, decimal TotalSpent, DateTime CreatedAtUtc);

public record SetRoleRequest([Required] string Role);
