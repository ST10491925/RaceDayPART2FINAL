using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api")]
public sealed class EnrolmentsController(RaceDayDbContext db) : ApiControllerBase
{
    private static IQueryable<Enrolment> Expanded(RaceDayDbContext db) => db.Enrolments.Include(x => x.EventCategory).ThenInclude(x => x.Event).Include(x => x.EventCategory).ThenInclude(x => x.Category);
    private static EnrolmentResponse View(Enrolment e) => new(e.EnrolmentID, e.ParticipantID, e.EventCategoryID, e.EnrolmentDate, e.Status, e.EventCategory.Event.EventName, e.EventCategory.Category.CategoryName);

    /// <summary>Enrols the authenticated Participant in an available event category.</summary>
    [Authorize(Roles = "Participant"), HttpPost("enrolments")]
    public async Task<IActionResult> Create(EnrolmentRequest request, CancellationToken ct)
    {
        var ec = await db.EventCategories.Include(x => x.Event).SingleOrDefaultAsync(x => x.EventCategoryID == request.EventCategoryId, ct);
        if (ec is null) return NotFound(new { message = "Event category not found." });
        if (await db.Enrolments.AnyAsync(x => x.ParticipantID == CurrentUserId && x.EventCategoryID == ec.EventCategoryID, ct)) return Conflict(new { message = "You are already enrolled in this event category." });
        if (ec.MaxParticipants is int limit && await db.Enrolments.CountAsync(x => x.EventCategoryID == ec.EventCategoryID && x.Status == "Active", ct) >= limit) return Conflict(new { message = "This event category has reached its participant limit." });
        var enrolment = new Enrolment { ParticipantID = CurrentUserId, EventCategoryID = ec.EventCategoryID, EnrolmentDate = DateTime.UtcNow, Status = "Active" };
        db.Enrolments.Add(enrolment); await db.SaveChangesAsync(ct);
        await db.Entry(enrolment).Reference(x => x.EventCategory).LoadAsync(ct);
        await db.Entry(enrolment.EventCategory).Reference(x => x.Category).LoadAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = enrolment.EnrolmentID }, new EnrolmentResponse(enrolment.EnrolmentID, enrolment.ParticipantID, enrolment.EventCategoryID, enrolment.EnrolmentDate, enrolment.Status, ec.Event.EventName, enrolment.EventCategory.Category.CategoryName));
    }

    /// <summary>Lists the authenticated Participant's enrolments.</summary>
    [Authorize(Roles = "Participant"), HttpGet("enrolments/my")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> Mine(CancellationToken ct) =>
        Ok(await Expanded(db).AsNoTracking().Where(x => x.ParticipantID == CurrentUserId).OrderByDescending(x => x.EnrolmentDate).Select(x => new EnrolmentResponse(x.EnrolmentID, x.ParticipantID, x.EventCategoryID, x.EnrolmentDate, x.Status, x.EventCategory.Event.EventName, x.EventCategory.Category.CategoryName)).ToListAsync(ct));

    /// <summary>Returns an enrolment only to its Participant or the event's Organiser.</summary>
    [Authorize(Roles = "Participant,Organiser"), HttpGet("enrolments/{id:int}")]
    public async Task<ActionResult<EnrolmentResponse>> Get(int id, CancellationToken ct)
    {
        var e = await Expanded(db).AsNoTracking().SingleOrDefaultAsync(x => x.EnrolmentID == id, ct);
        if (e is null) return NotFound();
        if (User.IsInRole("Participant") && e.ParticipantID != CurrentUserId) return Forbid();
        if (User.IsInRole("Organiser") && e.EventCategory.Event.OrganiserID != CurrentUserId) return Forbid();
        return Ok(View(e));
    }

    /// <summary>Cancels the authenticated Participant's own enrolment.</summary>
    [Authorize(Roles = "Participant"), HttpDelete("enrolments/{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var e = await db.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentID == id, ct);
        if (e is null) return NotFound(); if (e.ParticipantID != CurrentUserId) return Forbid();
        if (e.Status == "Cancelled") return NoContent();
        e.Status = "Cancelled"; await db.SaveChangesAsync(ct); return NoContent();
    }

    /// <summary>Lists enrolments for an event owned by the authenticated Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpGet("events/{eventId:int}/enrolments")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> ForEvent(int eventId, CancellationToken ct)
    {
        var e = await db.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventID == eventId, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        return Ok(await Expanded(db).AsNoTracking().Where(x => x.EventCategory.EventID == eventId).OrderBy(x => x.EnrolmentID).Select(x => new EnrolmentResponse(x.EnrolmentID, x.ParticipantID, x.EventCategoryID, x.EnrolmentDate, x.Status, x.EventCategory.Event.EventName, x.EventCategory.Category.CategoryName)).ToListAsync(ct));
    }
}
