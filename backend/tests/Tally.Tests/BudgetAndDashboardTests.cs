using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Tests;

public class BudgetServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly BudgetService _service;

    public BudgetServiceTests() => _service = new BudgetService(_db.Context);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Month_view_compares_budget_with_spending()
    {
        _db.AddBudget(TestDb.Alice, _db.Groceries, 2026, 3, 200m);
        _db.AddExpense(TestDb.Alice, _db.Groceries, 150m, new DateOnly(2026, 3, 5));
        _db.AddExpense(TestDb.Alice, _db.Groceries, 80m, new DateOnly(2026, 3, 20));
        _db.AddExpense(TestDb.Alice, _db.Groceries, 999m, new DateOnly(2026, 4, 1)); // next month, ignored

        var lines = await _service.GetMonthAsync(TestDb.Alice, 2026, 3);
        var groceries = lines.Single(l => l.CategoryId == _db.Groceries);

        Assert.Equal(230m, groceries.Spent);
        Assert.Equal(-30m, groceries.Remaining);
        Assert.Equal(115.0, groceries.PercentUsed);
    }

    [Fact]
    public async Task Set_creates_then_updates_the_same_budget()
    {
        await _service.SetAsync(TestDb.Alice, new SetBudgetRequest(2026, 3, _db.Groceries, 100m));
        await _service.SetAsync(TestDb.Alice, new SetBudgetRequest(2026, 3, _db.Groceries, 250m));

        var budget = Assert.Single(_db.Context.Budgets);
        Assert.Equal(250m, budget.Limit);
    }

    [Fact]
    public async Task Copy_previous_month_carries_limits_forward()
    {
        _db.AddBudget(TestDb.Alice, _db.Groceries, 2025, 12, 300m);
        _db.AddBudget(TestDb.Alice, _db.Transport, 2025, 12, 80m);

        var copied = await _service.CopyFromPreviousMonthAsync(TestDb.Alice, 2026, 1);

        Assert.Equal(2, copied);
        Assert.Equal(2, _db.Context.Budgets.Count(b => b.Year == 2026 && b.Month == 1));
    }

    [Fact]
    public async Task Copy_previous_month_fails_when_there_is_nothing_to_copy()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CopyFromPreviousMonthAsync(TestDb.Alice, 2026, 1));
    }
}

public class DashboardServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly DashboardService _service;

    public DashboardServiceTests() => _service = new DashboardService(_db.Context);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Summarizes_the_month_and_builds_a_six_month_trend()
    {
        _db.AddBudget(TestDb.Alice, _db.Groceries, 2026, 3, 400m);
        _db.AddExpense(TestDb.Alice, _db.Groceries, 100m, new DateOnly(2026, 3, 2));
        _db.AddExpense(TestDb.Alice, _db.Transport, 50m, new DateOnly(2026, 3, 9));
        _db.AddExpense(TestDb.Alice, _db.Groceries, 70m, new DateOnly(2026, 2, 14));
        _db.AddExpense(TestDb.Bob, _db.Groceries, 500m, new DateOnly(2026, 3, 2));

        var result = await _service.GetAsync(TestDb.Alice, 2026, 3, today: new DateOnly(2026, 3, 10));

        Assert.Equal(150m, result.TotalSpent);
        Assert.Equal(400m, result.TotalBudget);
        Assert.Equal(2, result.TransactionCount);
        Assert.Equal(15m, result.DailyAverage);          // 150 over 10 days
        Assert.Equal(465m, result.ProjectedSpend);        // 15 * 31 days in March
        Assert.Equal(70m, result.PreviousMonthSpent);
        Assert.Equal(6, result.Trend.Count);
        Assert.Equal("Groceries", result.ByCategory[0].Name);
    }

    [Fact]
    public async Task Past_months_use_actual_total_as_projection()
    {
        _db.AddExpense(TestDb.Alice, _db.Groceries, 310m, new DateOnly(2026, 1, 15));

        var result = await _service.GetAsync(TestDb.Alice, 2026, 1, today: new DateOnly(2026, 3, 10));

        Assert.Equal(10m, result.DailyAverage);
        Assert.Equal(310m, result.ProjectedSpend);
    }
}
