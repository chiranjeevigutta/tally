using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tally.Api.Data;
using Tally.Api.Dtos;
using Tally.Api.Models;

namespace Tally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public Task<List<CategoryDto>> GetAll([FromQuery] bool includeArchived = false) =>
        db.Categories.AsNoTracking()
            .Where(c => includeArchived || !c.IsArchived)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Color, c.IsArchived))
            .ToListAsync();

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request)
    {
        if (await NameTaken(request.Name, null))
            return Conflict(new ProblemDetails { Title = "Category exists", Detail = $"A category named \"{request.Name}\" already exists." });

        var category = new Category { Name = request.Name.Trim(), Color = request.Color.ToUpperInvariant() };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Ok(new CategoryDto(category.Id, category.Name, category.Color, category.IsArchived));
    }

    // categories are archived instead of deleted so old expenses keep their category
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CategoryDto>> Update(int id, SaveCategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await NameTaken(request.Name, id))
            return Conflict(new ProblemDetails { Title = "Category exists", Detail = $"A category named \"{request.Name}\" already exists." });

        category.Name = request.Name.Trim();
        category.Color = request.Color.ToUpperInvariant();
        category.IsArchived = request.IsArchived;
        await db.SaveChangesAsync();
        return Ok(new CategoryDto(category.Id, category.Name, category.Color, category.IsArchived));
    }

    private Task<bool> NameTaken(string name, int? exceptId)
    {
        var normalized = name.Trim().ToLower();
        return db.Categories.AnyAsync(c => c.Name.ToLower() == normalized && c.Id != exceptId);
    }
}
