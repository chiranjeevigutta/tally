using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Dtos;

namespace Tally.Api.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(string userId, int year, int month, DateOnly today);
}

public class DashboardService(AppDbContext db) : IDashboardService
{
    private const int TrendMonths = 6;

    public async Task<DashboardDto> GetAsync(string userId, int year, int month, DateOnly today)
    {
        var (start, end) = BudgetService.MonthRange(year, month);
        var trendStart = start.AddMonths(-(TrendMonths - 1));

        // one query for the whole 6 month window, then group in memory
        var expenses = await db.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= trendStart && e.Date <= end)
            .Select(e => new { e.CategoryId, e.Amount, e.Date })
            .ToListAsync();

        var budgets = await db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId &&
                        (b.Year * 100 + b.Month) >= (trendStart.Year * 100 + trendStart.Month) &&
                        (b.Year * 100 + b.Month) <= (year * 100 + month))
            .Select(b => new { b.CategoryId, b.Year, b.Month, b.Limit })
            .ToListAsync();

        var categories = await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id);

        var thisMonth = expenses.Where(e => e.Date >= start).ToList();
        var thisMonthBudgets = budgets.Where(b => b.Year == year && b.Month == month).ToDictionary(b => b.CategoryId, b => b.Limit);
        var totalSpent = thisMonth.Sum(e => e.Amount);

        var byCategory = thisMonth.Select(e => e.CategoryId)
            .Union(thisMonthBudgets.Keys)
            .Where(categories.ContainsKey)
            .Select(id => new CategorySpendDto(
                id, categories[id].Name, categories[id].Color,
                thisMonth.Where(e => e.CategoryId == id).Sum(e => e.Amount),
                thisMonthBudgets.GetValueOrDefault(id)))
            .OrderByDescending(c => c.Spent)
            .ToList();

        var trend = Enumerable.Range(0, TrendMonths)
            .Select(i => trendStart.AddMonths(i))
            .Select(m => new MonthTotalDto(
                m.Year, m.Month,
                expenses.Where(e => e.Date.Year == m.Year && e.Date.Month == m.Month).Sum(e => e.Amount),
                budgets.Where(b => b.Year == m.Year && b.Month == m.Month).Sum(b => b.Limit)))
            .ToList();

        // for the current month only count days so far, past months use the full month
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var isCurrentMonth = today.Year == year && today.Month == month;
        var daysElapsed = isCurrentMonth ? today.Day : daysInMonth;
        var dailyAverage = daysElapsed == 0 ? 0 : Math.Round(totalSpent / daysElapsed, 2);
        var projected = isCurrentMonth ? Math.Round(dailyAverage * daysInMonth, 2) : totalSpent;

        var recent = await db.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= start && e.Date <= end)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
            .Take(6)
            .Select(ExpenseService.ToDto)
            .ToListAsync();

        return new DashboardDto(
            year, month,
            totalSpent, thisMonthBudgets.Values.Sum(), thisMonth.Count,
            dailyAverage, projected,
            trend.Count >= 2 ? trend[^2].Spent : 0,
            byCategory, trend, recent);
    }
}
