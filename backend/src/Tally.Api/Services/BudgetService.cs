using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Models;

namespace Tally.Api.Services;

public interface IBudgetService
{
    Task<List<BudgetLineDto>> GetMonthAsync(string userId, int year, int month);
    Task<BudgetLineDto> SetAsync(string userId, SetBudgetRequest request);
    Task<int> CopyFromPreviousMonthAsync(string userId, int year, int month);
}

public class BudgetService(AppDbContext db) : IBudgetService
{
    public async Task<List<BudgetLineDto>> GetMonthAsync(string userId, int year, int month)
    {
        var (start, end) = MonthRange(year, month);

        var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var budgets = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToDictionaryAsync(b => b.CategoryId);
        var spent = (await db.Expenses.AsNoTracking()
                .Where(e => e.UserId == userId && e.Date >= start && e.Date <= end)
                .Select(e => new { e.CategoryId, e.Amount })
                .ToListAsync())
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        return categories
            // hide archived categories unless they still have a budget or spending this month
            .Where(c => !c.IsArchived || budgets.ContainsKey(c.Id) || spent.ContainsKey(c.Id))
            .Select(c =>
            {
                budgets.TryGetValue(c.Id, out var budget);
                return new BudgetLineDto(budget?.Id, c.Id, c.Name, c.Color, budget?.Limit ?? 0, spent.GetValueOrDefault(c.Id));
            })
            .ToList();
    }

    public async Task<BudgetLineDto> SetAsync(string userId, SetBudgetRequest request)
    {
        if (request.Year is < 2000 or > 2100) throw new BadRequestException("Year is out of range.");
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            throw new NotFoundException($"Category {request.CategoryId} was not found.");

        var budget = await db.Budgets.FirstOrDefaultAsync(b =>
            b.UserId == userId && b.CategoryId == request.CategoryId && b.Year == request.Year && b.Month == request.Month);

        if (budget is null)
        {
            budget = new Budget { UserId = userId, CategoryId = request.CategoryId, Year = request.Year, Month = request.Month };
            db.Budgets.Add(budget);
        }
        budget.Limit = Math.Round(request.Limit, 2);
        await db.SaveChangesAsync();

        var lines = await GetMonthAsync(userId, request.Year, request.Month);
        return lines.First(l => l.CategoryId == request.CategoryId);
    }

    public async Task<int> CopyFromPreviousMonthAsync(string userId, int year, int month)
    {
        var previous = new DateOnly(year, month, 1).AddMonths(-1);
        var source = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == previous.Year && b.Month == previous.Month)
            .ToListAsync();
        if (source.Count == 0) throw new BadRequestException("There are no budgets in the previous month to copy.");

        var existing = await db.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .ToDictionaryAsync(b => b.CategoryId);

        foreach (var b in source)
        {
            if (existing.TryGetValue(b.CategoryId, out var current))
                current.Limit = b.Limit;
            else
                db.Budgets.Add(new Budget { UserId = userId, CategoryId = b.CategoryId, Year = year, Month = month, Limit = b.Limit });
        }

        await db.SaveChangesAsync();
        return source.Count;
    }

    internal static (DateOnly Start, DateOnly End) MonthRange(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }
}
