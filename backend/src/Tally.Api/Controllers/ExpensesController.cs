using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public class ExpensesController(IExpenseService expenses) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<ExpenseDto>> Search([FromQuery] ExpenseQuery query) =>
        expenses.SearchAsync(User.GetUserId(), query);

    [HttpGet("{id:int}")]
    public Task<ExpenseDto> Get(int id) => expenses.GetAsync(User.GetUserId(), id);

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(SaveExpenseRequest request)
    {
        var created = await expenses.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public Task<ExpenseDto> Update(int id, SaveExpenseRequest request) =>
        expenses.UpdateAsync(User.GetUserId(), id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await expenses.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
