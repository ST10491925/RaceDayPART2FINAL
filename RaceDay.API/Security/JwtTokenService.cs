using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RaceDay.API.Security;

public sealed class JwtTokenService(IConfiguration configuration)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Configure Jwt:SigningKey with a secret of at least 32 characters."));
    public const string Issuer = "RaceDay.API";
    public const string Audience = "RaceDay.Client";

    public (string Token, DateTime ExpiresAt) Create(int userId, string email, string role)
    {
        if (_key.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
        var now = DateTime.UtcNow;
        var expires = now.AddHours(2);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        var jwt = new JwtSecurityToken(Issuer, Audience, claims, now, expires, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = Issuer,
                ValidateAudience = true, ValidAudience = Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
            };
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out _) ||
                principal.FindFirst(ClaimTypes.Email) is null ||
                principal.FindFirst(ClaimTypes.Role)?.Value is not ("Organiser" or "Participant")) return null;
            return principal;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException) { return null; }
    }
}
