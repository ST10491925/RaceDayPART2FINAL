using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api")]
public sealed class ResultsController(RaceDayDbContext db) : ApiControllerBase
{
    private static IQueryable<Enrolment> Expanded(RaceDayDbContext db) => db.Enrolments.Include(x => x.Participant).Include(x => x.EventCategory).ThenInclude(x => x.Event).Include(x => x.EventCategory).ThenInclude(x => x.Category).Include(x => x.Result);
    private static ResultResponse View(Result r) => new(r.ResultID, r.EnrolmentID, r.FinishTime, r.OverallPosition, r.ResultStatus, $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}", r.Enrolment.EventCategory.Event.EventName, r.Enrolment.EventCategory.Category.CategoryName);

    /// <summary>Records one result for an enrolment in an event owned by the Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpPost("results")]
    public async Task<IActionResult> Create(ResultRequest request, CancellationToken ct)
    {
        var enrolment = await Expanded(db).SingleOrDefaultAsync(x => x.EnrolmentID == request.EnrolmentId, ct);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.EventCategory.Event.OrganiserID != CurrentUserId) return Forbid();
        if (enrolment.Result is not null) return Conflict(new { message = "A result already exists for this enrolment." });
        var result = new Result { EnrolmentID = enrolment.EnrolmentID, FinishTime = request.FinishTime, OverallPosition = request.OverallPosition, ResultStatus = request.ResultStatus };
        db.Results.Add(result); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetEventResults), new { eventId = enrolment.EventCategory.EventID }, new ResultResponse(result.ResultID, result.EnrolmentID, result.FinishTime, result.OverallPosition, result.ResultStatus, $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}", enrolment.EventCategory.Event.EventName, enrolment.EventCategory.Category.CategoryName));
    }

    /// <summary>Updates a result for an event owned by the Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpPut("results/{id:int}")]
    public async Task<IActionResult> Update(int id, ResultUpdateRequest request, CancellationToken ct)
    {
        var result = await db.Results.Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Event).Include(x => x.Enrolment).ThenInclude(x => x.Participant).Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Category).SingleOrDefaultAsync(x => x.ResultID == id, ct);
        if (result is null) return NotFound(); if (result.Enrolment.EventCategory.Event.OrganiserID != CurrentUserId) return Forbid();
        result.FinishTime = request.FinishTime; result.OverallPosition = request.OverallPosition; result.ResultStatus = request.ResultStatus;
        await db.SaveChangesAsync(ct); return Ok(View(result));
    }

    /// <summary>Lists only results linked to the authenticated Participant.</summary>
    [Authorize(Roles = "Participant"), HttpGet("results/my")]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> Mine(CancellationToken ct)
    {
        var rows = await db.Results.AsNoTracking().Include(x => x.Enrolment).ThenInclude(x => x.Participant).Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Event).Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Category).Where(x => x.Enrolment.ParticipantID == CurrentUserId).ToListAsync(ct);
        return Ok(rows.Select(View));
    }

    /// <summary>Lists results for an event owned by the authenticated Organiser.</summary>
    [Authorize(Roles = "Organiser"), HttpGet("events/{eventId:int}/results")]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetEventResults(int eventId, CancellationToken ct)
    {
        var e = await db.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventID == eventId, ct);
        if (e is null) return NotFound(); if (e.OrganiserID != CurrentUserId) return Forbid();
        var rows = await db.Results.AsNoTracking().Include(x => x.Enrolment).ThenInclude(x => x.Participant).Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Event).Include(x => x.Enrolment).ThenInclude(x => x.EventCategory).ThenInclude(x => x.Category).Where(x => x.Enrolment.EventCategory.EventID == eventId).OrderBy(x => x.OverallPosition).ToListAsync(ct);
        return Ok(rows.Select(View));
    }
}
