using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api/events")]
public sealed class EventsController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Lists public event details.</summary>
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponse>>> List(CancellationToken ct) =>
        Ok(await db.Events.AsNoTracking().OrderBy(e => e.EventDate).Select(e => new EventResponse(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.Location, e.Description)).ToListAsync(ct));

    /// <summary>Returns one public event, or 404 when it does not exist.</summary>
    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<ActionResult<EventResponse>> Get(int id, CancellationToken ct)
    {
        var e = await db.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventID == id, ct);
        return e is null ? NotFound() : Ok(new EventResponse(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.Location, e.Description));
    }

    /// <summary>Creates an event owned by the authenticated Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpPost]
    public async Task<IActionResult> Create(EventRequest request, CancellationToken ct)
    {
        var e = new Event { OrganiserID = CurrentUserId, EventName = request.EventName.Trim(), EventDate = request.EventDate, Location = request.Location.Trim(), Description = request.Description?.Trim() };
        db.Events.Add(e); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = e.EventID }, new EventResponse(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.Location, e.Description));
    }

    /// <summary>Updates an event owned by the authenticated Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventRequest request, CancellationToken ct)
    {
        var e = await db.Events.SingleOrDefaultAsync(x => x.EventID == id, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        e.EventName = request.EventName.Trim(); e.EventDate = request.EventDate; e.Location = request.Location.Trim(); e.Description = request.Description?.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(new EventResponse(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.Location, e.Description));
    }

    /// <summary>Deletes an owned event only when it has no dependent records.</summary>
    [Authorize(Roles = "Organiser"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await db.Events.SingleOrDefaultAsync(x => x.EventID == id, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        if (await db.EventCategories.AnyAsync(x => x.EventID == id, ct) || await db.Routes.AnyAsync(x => x.EventID == id, ct) || await db.Weather.AnyAsync(x => x.EventID == id, ct))
            return Conflict(new { message = "An event with categories, route, or weather records cannot be deleted." });
        db.Events.Remove(e); await db.SaveChangesAsync(ct); return NoContent();
    }

    /// <summary>Lists the categories offered by a public event.</summary>
    [AllowAnonymous, HttpGet("{eventId:int}/categories")]
    public async Task<ActionResult<IEnumerable<EventCategoryResponse>>> ListCategories(int eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId, ct)) return NotFound();
        return Ok(await db.EventCategories.AsNoTracking().Where(ec => ec.EventID == eventId).Select(ec => new EventCategoryResponse(ec.EventCategoryID, ec.EventID, ec.CategoryID, ec.Category.CategoryName, ec.Category.Distance, ec.EntryFee, ec.MaxParticipants)).ToListAsync(ct));
    }

    /// <summary>Adds an existing shared category to an event owned by the Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpPost("{eventId:int}/categories")]
    public async Task<IActionResult> AddCategory(int eventId, EventCategoryRequest request, CancellationToken ct)
    {
        var e = await db.Events.SingleOrDefaultAsync(x => x.EventID == eventId, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        if (!await db.Categories.AnyAsync(c => c.CategoryID == request.CategoryId, ct)) return NotFound(new { message = "Category not found." });
        if (await db.EventCategories.AnyAsync(ec => ec.EventID == eventId && ec.CategoryID == request.CategoryId, ct)) return Conflict(new { message = "This category is already available for the event." });
        var ec = new EventCategory { EventID = eventId, CategoryID = request.CategoryId, EntryFee = request.EntryFee, MaxParticipants = request.MaxParticipants };
        db.EventCategories.Add(ec); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ListCategories), new { eventId }, new { eventCategoryId = ec.EventCategoryID, eventId, categoryId = ec.CategoryID, ec.EntryFee, ec.MaxParticipants });
    }

    /// <summary>Removes a category from an owned event when there are no enrolments.</summary>
    [Authorize(Roles = "Organiser"), HttpDelete("{eventId:int}/categories/{categoryId:int}")]
    public async Task<IActionResult> RemoveCategory(int eventId, int categoryId, CancellationToken ct)
    {
        var e = await db.Events.SingleOrDefaultAsync(x => x.EventID == eventId, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        var ec = await db.EventCategories.SingleOrDefaultAsync(x => x.EventID == eventId && x.CategoryID == categoryId, ct);
        if (ec is null) return NotFound();
        if (await db.Enrolments.AnyAsync(x => x.EventCategoryID == ec.EventCategoryID, ct)) return Conflict(new { message = "A category with enrolments cannot be removed from an event." });
        db.EventCategories.Remove(ec); await db.SaveChangesAsync(ct); return NoContent();
    }
}
