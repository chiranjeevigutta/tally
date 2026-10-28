using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(IExpenseService expenses, IReportService reports) : ControllerBase
{
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("expenses.xlsx")]
    public async Task<IActionResult> Excel([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (!ValidRange(from, to, out var problem)) return problem!;

        var data = await expenses.GetRangeAsync(User.GetUserId(), from, to);
        var bytes = reports.BuildExcel(data, from, to);
        return File(bytes, XlsxType, $"tally-expenses-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
    }

    [HttpGet("expenses.pdf")]
    public async Task<IActionResult> Pdf([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (!ValidRange(from, to, out var problem)) return problem!;

        var data = await expenses.GetRangeAsync(User.GetUserId(), from, to);
        var name = User.Identity?.Name ?? "you";
        var bytes = reports.BuildPdf(data, from, to, name);
        return File(bytes, "application/pdf", $"tally-expenses-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }

    private bool ValidRange(DateOnly from, DateOnly to, out IActionResult? problem)
    {
        problem = null;
        if (from == default || to == default)
            problem = BadRequest(new ProblemDetails { Title = "Missing dates", Detail = "Pass both from and to as yyyy-MM-dd." });
        else if (from > to)
            problem = BadRequest(new ProblemDetails { Title = "Invalid range", Detail = "The start date must be on or before the end date." });
        else if (to.DayNumber - from.DayNumber > 366 * 2)
            problem = BadRequest(new ProblemDetails { Title = "Range too large", Detail = "Reports can cover up to two years." });
        return problem is null;
    }
}
