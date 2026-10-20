using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Contracts;

public sealed record RegisterRequest([param: Required, MaxLength(50)] string FirstName, [param: Required, MaxLength(50)] string LastName, [param: Required, EmailAddress, MaxLength(100)] string Email, [param: Required, MinLength(10), MaxLength(128)] string Password);
public sealed record LoginRequest([param: Required, EmailAddress] string Email, [param: Required] string Password);
public sealed record ProfileUpdateRequest([param: Required, MaxLength(50)] string FirstName, [param: Required, MaxLength(50)] string LastName, [param: Required, EmailAddress, MaxLength(100)] string Email);
public sealed record EventRequest([param: Required, MaxLength(150)] string EventName, DateOnly EventDate, [param: Required, MaxLength(150)] string Location, [param: MaxLength(500)] string? Description);
public sealed record CategoryRequest([param: Required, MaxLength(100)] string CategoryName, [param: Range(typeof(decimal), "0.01", "9999.99")] decimal Distance, [param: MaxLength(300)] string? Description);
public sealed record EventCategoryRequest(int CategoryId, [param: Range(typeof(decimal), "0", "99999999.99")] decimal EntryFee, [param: Range(1, int.MaxValue)] int? MaxParticipants);
public sealed record EnrolmentRequest(int EventCategoryId);
public sealed record ResultRequest(int EnrolmentId, TimeSpan? FinishTime, [param: Range(1, int.MaxValue)] int? OverallPosition, [param: RegularExpression("^(Finished|Did Not Finish|Disqualified)$")] string ResultStatus = "Finished");
public sealed record ResultUpdateRequest(TimeSpan? FinishTime, [param: Range(1, int.MaxValue)] int? OverallPosition, [param: RegularExpression("^(Finished|Did Not Finish|Disqualified)$")] string ResultStatus);

public sealed record UserResponse(int UserID, string FirstName, string LastName, string Email, string Role);
public sealed record EventResponse(int EventID, int OrganiserID, string EventName, DateOnly EventDate, string Location, string? Description);
public sealed record CategoryResponse(int CategoryID, string CategoryName, decimal Distance, string? Description);
public sealed record EventCategoryResponse(int EventCategoryID, int EventID, int CategoryID, string CategoryName, decimal Distance, decimal EntryFee, int? MaxParticipants);
public sealed record EnrolmentResponse(int EnrolmentID, int ParticipantID, int EventCategoryID, DateTime EnrolmentDate, string Status, string EventName, string CategoryName);
public sealed record ResultResponse(int ResultID, int EnrolmentID, TimeSpan? FinishTime, int? OverallPosition, string ResultStatus, string ParticipantName, string EventName, string CategoryName);
public sealed record RouteResponse(int RouteID, int EventID, string RouteName, decimal Distance, decimal? Elevation, string? RouteURL);
public sealed record WeatherResponse(int WeatherID, int EventID, decimal? Temperature, string? WeatherCondition, decimal? WindSpeed, DateTime RecordedAt);
