using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Models;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin")]
public class AdminController(AppDbContext db, UserManager<AppUser> users) : ControllerBase
{
    [HttpGet("users")]
    public async Task<List<AdminUserDto>> GetUsers()
    {
        var allUsers = await db.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();

        var stats = (await db.Expenses.AsNoTracking().Select(e => new { e.UserId, e.Amount }).ToListAsync())
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Total: g.Sum(x => x.Amount)));

        var roleLookup = (await (from ur in db.UserRoles
                                 join r in db.Roles on ur.RoleId equals r.Id
                                 select new { ur.UserId, r.Name }).ToListAsync())
            .ToLookup(x => x.UserId, x => x.Name!);

        return allUsers.Select(u =>
        {
            var s = stats.GetValueOrDefault(u.Id);
            return new AdminUserDto(u.Id, u.Email!, u.DisplayName, roleLookup[u.Id].ToList(), s.Count, s.Total, u.CreatedAtUtc);
        }).ToList();
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> SetRole(string id, SetRoleRequest request)
    {
        if (request.Role is not (Roles.Admin or Roles.User))
            return BadRequest(new ProblemDetails { Title = "Unknown role", Detail = "Role must be Admin or User." });

        if (id == User.GetUserId())
            return BadRequest(new ProblemDetails { Title = "Not allowed", Detail = "You can't change your own role." });

        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var current = await users.GetRolesAsync(user);
        await users.RemoveFromRolesAsync(user, current);
        await users.AddToRoleAsync(user, request.Role);
        return NoContent();
    }
}
