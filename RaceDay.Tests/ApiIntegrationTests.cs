using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.Tests;

public sealed class ApiIntegrationTests : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _client;
    private readonly RaceDayApiFactory _factory;

    public ApiIntegrationTests(RaceDayApiFactory factory) { _factory = factory; _client = factory.CreateClient(); }

    [Fact]
    public async Task ProtectedProfile_RejectsAnonymousRequest()
    {
        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEvents_ReturnsSuccessWithoutLogin()
    {
        var response = await _client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Registration_CreatesParticipantWithHashedPassword_AndRejectsDuplicate()
    {
        var payload = new { firstName = "Jamie", lastName = "Runner", email = "jamie@example.test", password = "RaceDay!Pass123" };
        var first = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == payload.email);
            Assert.Equal("Participant", user.Role);
            Assert.NotEqual(payload.password, user.PasswordHash);
            Assert.True(new PasswordHasher().Verify(payload.password, user.PasswordHash));
        }
        var duplicate = await _client.PostAsJsonAsync("/api/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Registration_RejectsWeakPassword()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { firstName = "Jamie", lastName = "Runner", email = "weak@example.test", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_IsForbiddenFromOrganiserEventCreation()
    {
        var email = $"participant-{Guid.NewGuid():N}@example.test";
        var credentials = new { firstName = "Casey", lastName = "Runner", email, password = "RaceDay!Pass123" };
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/auth/register", credentials)).StatusCode);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = credentials.password });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events") { Content = JsonContent.Create(new { eventName = "Not allowed", eventDate = "2026-11-01", location = "Durban", description = (string?)null }) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var first = new RaceDay.API.Models.User { FirstName = "Owner", LastName = "One", Email = $"owner-{Guid.NewGuid():N}@example.test", PasswordHash = "unused", Role = "Organiser" };
        var second = new RaceDay.API.Models.User { FirstName = "Owner", LastName = "Two", Email = $"owner-{Guid.NewGuid():N}@example.test", PasswordHash = "unused", Role = "Organiser" };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Users.AddRange(first, second);
            await db.SaveChangesAsync();
            db.Events.Add(new RaceDay.API.Models.Event { OrganiserID = second.UserID, EventName = "Protected Event", EventDate = new DateOnly(2026, 11, 1), Location = "Durban" });
            await db.SaveChangesAsync();
        }
        var eventId = await GetEventId("Protected Event");
        var tokens = _factory.Services.GetRequiredService<JwtTokenService>();
        var token = tokens.Create(first.UserID, first.Email, "Organiser").Token;
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/events/{eventId}") { Content = JsonContent.Create(new { eventName = "Changed", eventDate = "2026-11-01", location = "Durban", description = (string?)null }) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_RejectsIncorrectPassword()
    {
        var email = $"wrong-password-{Guid.NewGuid():N}@example.test";
        await _client.PostAsJsonAsync("/api/auth/register", new { firstName = "Taylor", lastName = "Runner", email, password = "RaceDay!Pass123" });
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDay!Wrong123" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registration_IgnoresClientSuppliedRole_AndCreatesParticipant()
    {
        var email = $"role-{Guid.NewGuid():N}@example.test";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { firstName = "Sam", lastName = "Runner", email, password = "RaceDay!Pass123", role = "Organiser" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Users.SingleAsync(x => x.Email == email);
        Assert.Equal("Participant", user.Role);
    }

    [Fact]
    public async Task Login_ReturnsBearerToken_ForValidCredentials()
    {
        var email = $"login-{Guid.NewGuid():N}@example.test";
        await _client.PostAsJsonAsync("/api/auth/register", new { firstName = "Lee", lastName = "Runner", email, password = "RaceDay!Pass123" });
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDay!Pass123" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Bearer", body.TokenType);
    }

    [Fact]
    public async Task EventCreation_RejectsAnonymousRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/events", new { eventName = "Anon", eventDate = "2026-11-01", location = "Durban" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_AndAnyoneCanViewIt()
    {
        var organiser = await CreateUser("Organiser");
        using var create = Authed(HttpMethod.Post, "/api/events", organiser.Token, new { eventName = "Durban 10km", eventDate = "2026-11-15", location = "Durban Beachfront", description = "Coastal run" });
        var created = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var eventId = await GetEventId("Durban 10km");
        var view = await _client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnEvent()
    {
        var organiser = await CreateUser("Organiser");
        var seeded = await SeedEvent(organiser.Id, "Delete Me Event");
        using var update = Authed(HttpMethod.Put, $"/api/events/{seeded}", organiser.Token, new { eventName = "Renamed Event", eventDate = "2026-12-01", location = "Pietermaritzburg" });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(update)).StatusCode);
        using var delete = Authed(HttpMethod.Delete, $"/api/events/{seeded}", organiser.Token);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delete)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/events/{seeded}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded_ThenDuplicateIsRejected()
    {
        var organiser = await CreateUser("Organiser");
        var participant = await CreateUser("Participant");
        var eventCategoryId = await SeedEventCategory(organiser.Id, "Enrol Event");
        using var enrol = Authed(HttpMethod.Post, "/api/enrolments", participant.Token, new { eventCategoryId });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(enrol)).StatusCode);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var saved = await db.Enrolments.SingleAsync(x => x.ParticipantID == participant.Id && x.EventCategoryID == eventCategoryId);
            Assert.Equal("Active", saved.Status);
        }
        using var duplicate = Authed(HttpMethod.Post, "/api/enrolments", participant.Token, new { eventCategoryId });
        Assert.Equal(HttpStatusCode.Conflict, (await _client.SendAsync(duplicate)).StatusCode);
    }

    [Fact]
    public async Task Organiser_IsForbiddenFromEnrolling_AndAnonymousIsUnauthorized()
    {
        var organiser = await CreateUser("Organiser");
        var eventCategoryId = await SeedEventCategory(organiser.Id, "Role Check Event");
        using var enrol = Authed(HttpMethod.Post, "/api/enrolments", organiser.Token, new { eventCategoryId });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(enrol)).StatusCode);
        var anonymous = await _client.PostAsJsonAsync("/api/enrolments", new { eventCategoryId });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsEnrolment()
    {
        var organiser = await CreateUser("Organiser");
        var owner = await CreateUser("Participant");
        var other = await CreateUser("Participant");
        var enrolmentId = await SeedEnrolment(organiser.Id, owner.Id, "Privacy Event");
        using var request = Authed(HttpMethod.Get, $"/api/enrolments/{enrolmentId}", other.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Participant_IsForbiddenFromRecordingResults()
    {
        var participant = await CreateUser("Participant");
        using var request = Authed(HttpMethod.Post, "/api/results", participant.Token, new { enrolmentId = 1, finishTime = "01:00:00", overallPosition = 1, resultStatus = "Finished" });
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantCanViewOwnResult()
    {
        var organiser = await CreateUser("Organiser");
        var participant = await CreateUser("Participant");
        var enrolmentId = await SeedEnrolment(organiser.Id, participant.Id, "Results Event");
        using var record = Authed(HttpMethod.Post, "/api/results", organiser.Token, new { enrolmentId, finishTime = "00:45:30", overallPosition = 1, resultStatus = "Finished" });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(record)).StatusCode);
        using var mine = Authed(HttpMethod.Get, "/api/results/my", participant.Token);
        var response = await _client.SendAsync(mine);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(1, items.GetArrayLength());
    }

    private async Task<(int Id, string Token)> CreateUser(string role)
    {
        var user = new User { FirstName = "Test", LastName = role, Email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test", PasswordHash = "unused", Role = role };
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<JwtTokenService>().Create(user.UserID, user.Email, role).Token;
        return (user.UserID, token);
    }

    private async Task<int> SeedEvent(int organiserId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var entity = new Event { OrganiserID = organiserId, EventName = name, EventDate = new DateOnly(2026, 11, 1), Location = "Durban" };
        db.Events.Add(entity);
        await db.SaveChangesAsync();
        return entity.EventID;
    }

    private async Task<int> SeedEventCategory(int organiserId, string eventName)
    {
        var eventId = await SeedEvent(organiserId, eventName);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var category = new Category { CategoryName = $"10km {Guid.NewGuid():N}", Distance = 10m };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        var eventCategory = new EventCategory { EventID = eventId, CategoryID = category.CategoryID, EntryFee = 150m };
        db.EventCategories.Add(eventCategory);
        await db.SaveChangesAsync();
        return eventCategory.EventCategoryID;
    }

    private async Task<int> SeedEnrolment(int organiserId, int participantId, string eventName)
    {
        var eventCategoryId = await SeedEventCategory(organiserId, eventName);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var enrolment = new Enrolment { ParticipantID = participantId, EventCategoryID = eventCategoryId, EnrolmentDate = DateTime.UtcNow, Status = "Active" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        return enrolment.EnrolmentID;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<int> GetEventId(string name)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Events.Where(x => x.EventName == name).Select(x => x.EventID).SingleAsync();
    }

    private sealed record TokenResponse(string Token, string TokenType, DateTime ExpiresAt);
}

public sealed class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-bytes",
            ["ConnectionStrings:RaceDay"] = "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayTests;Trusted_Connection=True"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
