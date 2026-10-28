using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboard, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get([FromQuery] int? year, [FromQuery] int? month)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var y = year ?? today.Year;
        var m = month is >= 1 and <= 12 ? month.Value : today.Month;
        return dashboard.GetAsync(User.GetUserId(), y, m, today);
    }
}
