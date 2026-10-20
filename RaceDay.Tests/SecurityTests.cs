using Microsoft.Extensions.Configuration;
using RaceDay.API.Security;

namespace RaceDay.Tests;

public sealed class SecurityTests
{
    [Fact]
    public void PasswordHash_IsNotPlainText_AndVerifiesOnlyCorrectPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("RaceDay!Pass123");
        Assert.NotEqual("RaceDay!Pass123", hash);
        Assert.StartsWith("pbkdf2-sha256$", hash);
        Assert.True(hasher.Verify("RaceDay!Pass123", hash));
        Assert.False(hasher.Verify("Wrong!Pass123", hash));
    }

    [Fact]
    public void JwtToken_RoundTripsIdentity_AndRejectsTampering()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "local-test-signing-key-that-is-at-least-32-bytes"
        }).Build();
        var service = new JwtTokenService(configuration);
        var (token, expiry) = service.Create(42, "person@example.test", "Participant");

        var principal = service.Validate(token);
        Assert.NotNull(principal);
        Assert.Equal("42", principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("Participant", principal.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value);
        Assert.True(expiry > DateTime.UtcNow);
        var parts = token.Split('.');
        var tamperedSignature = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        Assert.Null(service.Validate($"{parts[0]}.{parts[1]}.{tamperedSignature}"));
    }
}
