using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerOperationTransformer>();
});
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay"))
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var signingKeyBytes = System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"] ?? "");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtTokenService.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtTokenService.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

if (args.Length == 2 && args[0] == "--provision-organiser")
{
    var provisioningApp = builder.Build();
    using var scope = provisioningApp.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    await db.Database.MigrateAsync();
    var email = args[1].Trim();
    if (email.Length > 100 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        throw new InvalidOperationException("A valid email address of at most 100 characters is required.");
    if (await db.Users.AnyAsync(user => user.Email == email))
        throw new InvalidOperationException("An account with that email already exists.");
    Console.Write("Organiser first name: ");
    var firstName = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Organiser last name: ");
    var lastName = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Set a password (hidden): ");
    var password = ReadHiddenPassword();
    if (firstName.Length is < 1 or > 50 || lastName.Length is < 1 or > 50 ||
        !System.Text.RegularExpressions.Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{10,128}$"))
        throw new InvalidOperationException("Names must be 1-50 characters; the password must be 10-128 characters and include upper-case, lower-case, numeric, and symbol characters.");
    db.Users.Add(new RaceDay.API.Models.User
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        PasswordHash = new PasswordHasher().Hash(password),
        Role = "Organiser"
    });
    await db.SaveChangesAsync();
    Console.WriteLine($"Organiser account provisioned: {email}");
    return;
}

var app = builder.Build();

var signingKey = app.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || System.Text.Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set Jwt:SigningKey to a private value of at least 32 bytes before starting the API.");

if (app.Environment.IsDevelopment())
{
    using (var migrationScope = app.Services.CreateScope())
    {
        var migrationDb = migrationScope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        if (migrationDb.Database.IsRelational()) migrationDb.Database.Migrate();
    }
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "RaceDay API v1"));
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static string ReadHiddenPassword()
{
    var value = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
        if (key.Key == ConsoleKey.Backspace && value.Length > 0) { value.Length--; continue; }
        if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }
}

public partial class Program { }
