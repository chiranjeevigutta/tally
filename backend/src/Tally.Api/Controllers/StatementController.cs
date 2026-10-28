using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

// Server-rendered (MVC + Razor view) monthly statement. The React app fetches
// this HTML with the user's token and opens it in a new window for printing.
[Authorize]
[Route("statement")]
public class StatementController(IExpenseService expenses, IBudgetService budgets) : Controller
{
    [HttpGet("{year:int}/{month:range(1,12)}")]
    public async Task<IActionResult> Monthly(int year, int month)
    {
        var userId = User.GetUserId();
        var (start, end) = BudgetService.MonthRange(year, month);

        var model = new StatementViewModel
        {
            DisplayName = User.Identity?.Name ?? "",
            Year = year,
            Month = month,
            Expenses = await expenses.GetRangeAsync(userId, start, end),
            Budgets = (await budgets.GetMonthAsync(userId, year, month))
                .Where(b => b.Limit > 0 || b.Spent > 0)
                .ToList()
        };

        return View(model);
    }
}
