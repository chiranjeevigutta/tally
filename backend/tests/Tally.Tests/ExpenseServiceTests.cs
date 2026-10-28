using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Tests;

public class ExpenseServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly ExpenseService _service;

    public ExpenseServiceTests() => _service = new ExpenseService(_db.Context);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_rounds_amount_and_returns_category()
    {
        var request = new SaveExpenseRequest(12.345m, new DateOnly(2026, 3, 4), "  Bread  ", null, _db.Groceries);

        var created = await _service.CreateAsync(TestDb.Alice, request);

        Assert.Equal(12.35m, created.Amount);
        Assert.Equal("Bread", created.Description);
        Assert.Equal("Groceries", created.CategoryName);
    }

    [Fact]
    public async Task Search_only_returns_the_callers_expenses()
    {
        _db.AddExpense(TestDb.Alice, _db.Groceries, 10m, new DateOnly(2026, 3, 1));
        _db.AddExpense(TestDb.Alice, _db.Transport, 5m, new DateOnly(2026, 3, 2));
        _db.AddExpense(TestDb.Bob, _db.Groceries, 99m, new DateOnly(2026, 3, 1));

        var result = await _service.SearchAsync(TestDb.Alice, new ExpenseQuery());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(15m, result.TotalAmount);
    }

    [Fact]
    public async Task Search_filters_by_text_category_and_dates()
    {
        _db.AddExpense(TestDb.Alice, _db.Groceries, 20m, new DateOnly(2026, 2, 28), "Farmers market");
        _db.AddExpense(TestDb.Alice, _db.Groceries, 30m, new DateOnly(2026, 3, 10), "Farmers market");
        _db.AddExpense(TestDb.Alice, _db.Transport, 40m, new DateOnly(2026, 3, 11), "Gas");

        var result = await _service.SearchAsync(TestDb.Alice, new ExpenseQuery
        {
            Search = "farmers",
            CategoryId = _db.Groceries,
            From = new DateOnly(2026, 3, 1),
            To = new DateOnly(2026, 3, 31)
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(30m, item.Amount);
    }

    [Fact]
    public async Task Search_pages_and_sorts_by_amount()
    {
        for (var i = 1; i <= 5; i++)
            _db.AddExpense(TestDb.Alice, _db.Groceries, i * 10m, new DateOnly(2026, 3, i));

        var page = await _service.SearchAsync(TestDb.Alice, new ExpenseQuery { Sort = "amount", Desc = true, Page = 2, PageSize = 2 });

        Assert.Equal(3, page.TotalPages);
        Assert.Equal(new[] { 30m, 20m }, page.Items.Select(i => i.Amount));
    }

    [Fact]
    public async Task Cannot_update_or_delete_someone_elses_expense()
    {
        _db.AddExpense(TestDb.Bob, _db.Groceries, 10m, new DateOnly(2026, 3, 1));
        var bobsId = _db.Context.Expenses.Single().Id;
        var request = new SaveExpenseRequest(1m, new DateOnly(2026, 3, 1), "Hacked", null, _db.Groceries);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(TestDb.Alice, bobsId, request));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(TestDb.Alice, bobsId));
    }

    [Fact]
    public async Task Create_rejects_archived_category()
    {
        _db.Context.Categories.Find(_db.Transport)!.IsArchived = true;
        await _db.Context.SaveChangesAsync();

        var request = new SaveExpenseRequest(10m, new DateOnly(2026, 3, 1), "Bus", null, _db.Transport);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(TestDb.Alice, request));
    }
}
