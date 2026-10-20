using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Contracts;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[Microsoft.AspNetCore.Mvc.Route("api")]
public sealed class AuthController(RaceDayDbContext db, JwtTokenService tokens) : ApiControllerBase
{
    private static readonly PasswordHasher Hasher = new();
    private static readonly string DummyPasswordHash = Hasher.Hash("not-a-real-account-password");

    /// <summary>Creates a Participant account without accepting a client-supplied role.</summary>
    [AllowAnonymous, HttpPost("auth/register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{10,128}$"))
            return BadRequest(new { message = "Password must be at least 10 characters and include upper-case, lower-case, numeric, and symbol characters." });
        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) return Conflict(new { message = "An account with this email already exists." });
        var user = new User { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, PasswordHash = Hasher.Hash(request.Password), Role = "Participant" };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        var response = new UserResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.Role);
        return Created("/api/users/me", response);
    }

    /// <summary>Validates credentials and returns a signed bearer token.</summary>
    [AllowAnonymous, HttpPost("auth/login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email.Trim(), cancellationToken);
        var passwordMatches = Hasher.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || !passwordMatches) return Unauthorized(new { message = "Email or password is incorrect." });
        var (token, expiresAt) = tokens.Create(user.UserID, user.Email, user.Role);
        return Ok(new { token, tokenType = "Bearer", expiresAt });
    }

    /// <summary>Returns the authenticated user's own profile.</summary>
    [Authorize, HttpGet("users/me")]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserID == CurrentUserId, cancellationToken);
        return user is null ? NotFound() : Ok(new UserResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.Role));
    }

    /// <summary>Updates only the authenticated user's editable profile fields.</summary>
    [Authorize, HttpPut("users/me")]
    public async Task<ActionResult<UserResponse>> UpdateMe(ProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserID == CurrentUserId, cancellationToken);
        if (user is null) return NotFound();
        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(u => u.UserID != user.UserID && u.Email == email, cancellationToken)) return Conflict(new { message = "An account with this email already exists." });
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim(); user.Email = email;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new UserResponse(user.UserID, user.FirstName, user.LastName, user.Email, user.Role));
    }
}
