using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api/categories")]
public sealed class CategoriesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Lists the shared public category catalog.</summary>
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> List(CancellationToken ct) =>
        Ok(await db.Categories.AsNoTracking().OrderBy(c => c.Distance).Select(c => new CategoryResponse(c.CategoryID, c.CategoryName, c.Distance, c.Description)).ToListAsync(ct));

    /// <summary>Returns one category from the shared catalog.</summary>
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Get(int id, CancellationToken ct)
    {
        var c = await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.CategoryID == id, ct);
        return c is null ? NotFound() : Ok(new CategoryResponse(c.CategoryID, c.CategoryName, c.Distance, c.Description));
    }

    /// <summary>Adds a category to the shared catalog.</summary>
    [Authorize(Roles = "Organiser"), HttpPost]
    public async Task<IActionResult> Create(CategoryRequest request, CancellationToken ct)
    {
        var c = new Category { CategoryName = request.CategoryName.Trim(), Distance = request.Distance, Description = request.Description?.Trim() };
        db.Categories.Add(c); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = c.CategoryID }, new CategoryResponse(c.CategoryID, c.CategoryName, c.Distance, c.Description));
    }

    /// <summary>Updates a category in the shared catalog.</summary>
    [Authorize(Roles = "Organiser"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request, CancellationToken ct)
    {
        var c = await db.Categories.SingleOrDefaultAsync(x => x.CategoryID == id, ct); if (c is null) return NotFound();
        c.CategoryName = request.CategoryName.Trim(); c.Distance = request.Distance; c.Description = request.Description?.Trim();
        await db.SaveChangesAsync(ct); return Ok(new CategoryResponse(c.CategoryID, c.CategoryName, c.Distance, c.Description));
    }

    /// <summary>Deletes a category only when it is not assigned to an event.</summary>
    [Authorize(Roles = "Organiser"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var c = await db.Categories.SingleOrDefaultAsync(x => x.CategoryID == id, ct); if (c is null) return NotFound();
        if (await db.EventCategories.AnyAsync(ec => ec.CategoryID == id, ct)) return Conflict(new { message = "A category assigned to an event cannot be deleted." });
        db.Categories.Remove(c); await db.SaveChangesAsync(ct); return NoContent();
    }
}
