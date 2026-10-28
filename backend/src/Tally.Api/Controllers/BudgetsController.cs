using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public class BudgetsController(IBudgetService budgets) : ControllerBase
{
    [HttpGet("{year:int}/{month:range(1,12)}")]
    public Task<List<BudgetLineDto>> GetMonth(int year, int month) =>
        budgets.GetMonthAsync(User.GetUserId(), year, month);

    [HttpPut]
    public Task<BudgetLineDto> Set(SetBudgetRequest request) =>
        budgets.SetAsync(User.GetUserId(), request);

    [HttpPost("copy-previous")]
    public async Task<IActionResult> CopyPrevious(CopyBudgetsRequest request)
    {
        var copied = await budgets.CopyFromPreviousMonthAsync(User.GetUserId(), request.Year, request.Month);
        return Ok(new { copied });
    }
}
