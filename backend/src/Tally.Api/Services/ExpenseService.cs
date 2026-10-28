using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Models;

namespace Tally.Api.Services;

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> SearchAsync(string userId, ExpenseQuery query);
    Task<List<ExpenseDto>> GetRangeAsync(string userId, DateOnly from, DateOnly to);
    Task<ExpenseDto> GetAsync(string userId, int id);
    Task<ExpenseDto> CreateAsync(string userId, SaveExpenseRequest request);
    Task<ExpenseDto> UpdateAsync(string userId, int id, SaveExpenseRequest request);
    Task DeleteAsync(string userId, int id);
}

public class ExpenseService(AppDbContext db) : IExpenseService
{
    public async Task<PagedResult<ExpenseDto>> SearchAsync(string userId, ExpenseQuery query)
    {
        var q = db.Expenses.AsNoTracking().Where(e => e.UserId == userId);

        if (query.From is { } from) q = q.Where(e => e.Date >= from);
        if (query.To is { } to) q = q.Where(e => e.Date <= to);
        if (query.CategoryId is { } categoryId) q = q.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(e => e.Description.ToLower().Contains(term) || (e.Notes != null && e.Notes.ToLower().Contains(term)));
        }

        var amounts = await q.Select(e => e.Amount).ToListAsync();
        var total = amounts.Count;

        q = (query.Sort.ToLowerInvariant(), query.Desc) switch
        {
            ("amount", true) => q.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Id),
            ("amount", false) => q.OrderBy(e => e.Amount).ThenBy(e => e.Id),
            ("description", true) => q.OrderByDescending(e => e.Description),
            ("description", false) => q.OrderBy(e => e.Description),
            (_, false) => q.OrderBy(e => e.Date).ThenBy(e => e.Id),
            _ => q.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync();

        return new PagedResult<ExpenseDto>(items, query.Page, query.PageSize, total, amounts.Sum());
    }

    public Task<List<ExpenseDto>> GetRangeAsync(string userId, DateOnly from, DateOnly to) =>
        db.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.Date >= from && e.Date <= to)
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .Select(ToDto)
            .ToListAsync();

    public async Task<ExpenseDto> GetAsync(string userId, int id) =>
        await db.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId && e.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync()
        ?? throw new NotFoundException($"Expense {id} was not found.");

    public async Task<ExpenseDto> CreateAsync(string userId, SaveExpenseRequest request)
    {
        await EnsureCategoryAsync(request.CategoryId);

        var expense = new Expense { UserId = userId };
        Apply(expense, request);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        return await GetAsync(userId, expense.Id);
    }

    public async Task<ExpenseDto> UpdateAsync(string userId, int id, SaveExpenseRequest request)
    {
        // filtering by user id here means you get a 404 for someone else's expense, not a 403
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId)
            ?? throw new NotFoundException($"Expense {id} was not found.");

        await EnsureCategoryAsync(request.CategoryId);
        Apply(expense, request);
        await db.SaveChangesAsync();

        return await GetAsync(userId, id);
    }

    public async Task DeleteAsync(string userId, int id)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId)
            ?? throw new NotFoundException($"Expense {id} was not found.");

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();
    }

    private async Task EnsureCategoryAsync(int categoryId)
    {
        var exists = await db.Categories.AnyAsync(c => c.Id == categoryId && !c.IsArchived);
        if (!exists) throw new BadRequestException("Choose an active category.");
    }

    private static void Apply(Expense expense, SaveExpenseRequest request)
    {
        expense.Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        expense.Date = request.Date;
        expense.Description = request.Description.Trim();
        expense.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        expense.CategoryId = request.CategoryId;
        expense.PaymentMethod = request.PaymentMethod;
    }

    internal static readonly System.Linq.Expressions.Expression<Func<Expense, ExpenseDto>> ToDto = e =>
        new ExpenseDto(e.Id, e.Amount, e.Date, e.Description, e.Notes, e.PaymentMethod,
            e.CategoryId, e.Category!.Name, e.Category!.Color);
}
