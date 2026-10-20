using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

[AllowAnonymous, Microsoft.AspNetCore.Mvc.Route("api/events/{eventId:int}")]
public sealed class EventInfoController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Returns the route information stored for a public event.</summary>
    [HttpGet("route")]
    public async Task<ActionResult<RouteResponse>> RouteInfo(int eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(x => x.EventID == eventId, ct)) return NotFound();
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(x => x.EventID == eventId, ct);
        return route is null ? NotFound() : Ok(new RouteResponse(route.RouteID, route.EventID, route.RouteName, route.Distance, route.Elevation, route.RouteURL));
    }

    /// <summary>Returns stored weather records for a public event.</summary>
    [HttpGet("weather")]
    public async Task<ActionResult<IEnumerable<WeatherResponse>>> WeatherInfo(int eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(x => x.EventID == eventId, ct)) return NotFound();
        return Ok(await db.Weather.AsNoTracking().Where(x => x.EventID == eventId).OrderByDescending(x => x.RecordedAt).Select(x => new WeatherResponse(x.WeatherID, x.EventID, x.Temperature, x.WeatherCondition, x.WindSpeed, x.RecordedAt)).ToListAsync(ct));
    }
}
