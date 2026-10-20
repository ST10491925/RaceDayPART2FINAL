# RaceDay PROG6212 Part 2

RaceDay is a REST API for managing running and walking events, categories, participant enrolments and race results. This Part 2 solution builds on the approved Part 1 SQL and endpoint plan. The SQL schema is the implementation reference because assessor feedback identified incorrect cardinalities in the submitted ERD. A corrected Part 2 ERD is included alongside the original.

## Roles

- **Public:** register as a Participant, log in, browse events/categories, and view event route and weather information.
- **Participant:** manage their own profile, enrol in event categories, view and cancel their own enrolments, and view their own results.
- **Organiser:** manage events they own, manage categories and event-category links, view enrolments for their own events, and create or edit results for those events.

Registration creates Participant accounts only. Organiser accounts must be provisioned by an authorised database administrator; there is no public role-selection field or role-grant endpoint.

## Solution layout

```text
RaceDay.sln
RaceDay.API/       ASP.NET Core Web API, EF Core models, SQL Server DbContext, controllers
RaceDay.Tests/     xUnit unit and HTTP integration tests
DOCS/              Approved Part 1 ERD, SQL, and API endpoint plan
.github/workflows/ Build and test workflow
```

## Requirements

- .NET SDK 10
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 (optional) or the .NET CLI

## Configure and run

The development connection string is in `RaceDay.API/appsettings.json` and targets local SQL Server LocalDB. Change it through user secrets or an environment variable for another SQL Server. Do not commit real credentials.

`RaceDay.API/appsettings.Development.json` contains a development-only JWT signing key so the API starts without extra setup. Override it with your own private value of at least 32 characters for any shared or deployed environment. In PowerShell:

```powershell
$env:Jwt__SigningKey = "replace-with-a-private-random-value-at-least-32-characters"
```

Restore, build and run. When the API starts in the Development environment it applies the EF Core migration to the database automatically, so no separate database step is needed:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project RaceDay.API --launch-profile https
```

To apply the migration manually instead, install the EF CLI once and run `dotnet ef database update --project RaceDay.API`.

Provision the first Organiser through the administrator-only console setup command. It applies migrations, prompts for the account name and a hidden password, and does not expose role selection through registration:

```powershell
dotnet run --project RaceDay.API -- --provision-organiser organiser@example.com
```

For local demo records, open `database/DevSampleData.sql`, set `@OrganiserEmail` to the account you just created, and run it against the migrated `RaceDayDB`. It adds sample events, categories, routes, and weather records without dropping or recreating the database.

Swagger UI is available at `/swagger` in Development. The OpenAPI document is at `/openapi/v1.json`.
In VS Code, start the **Launch RaceDay API and Swagger** configuration to build and run the API and open Swagger automatically on `http://localhost:5229`. This separate port avoids colliding with the standard launch profiles on port 5228. If Swagger reports that it cannot fetch the OpenAPI document, check the API output for startup errors.

## Database model

The EF Core model maps to the Part 1 tables: `Users`, `Events`, `Categories`, `EventCategories`, `Enrolments`, `Results`, `Routes`, and `Weather`. It preserves the SQL keys, lengths, defaults, checks, and unique constraints. In particular:

- `EventCategories` links reusable global `Categories` to `Events` and uniquely constrains each event/category pair.
- A participant can enrol once per event category.
- An enrolment has zero or one result.
- An event has zero or one route and can have many weather records.
- Events and weather are linked through `Weather.EventID`.

The submitted SQL file drops and recreates `RaceDayDB`; do not run it against a database that contains data you need. The API uses EF Core migrations for its Code First database setup.

## Authentication and authorization

`POST /api/auth/register` creates a Participant account. Passwords are stored as PBKDF2-SHA256 hashes, never as plaintext. `POST /api/auth/login` returns a signed JWT bearer token that expires after two hours. The signing key is configuration-only and must not be committed. The server reads the user ID and role from the validated token; client-supplied identity and role values are not trusted. The approved endpoint plan has no logout route; a client ends its local session by deleting its token.

Organiser event, event-category, enrolment, and result operations check ownership against the authenticated Organiser. Participant enrolment and result routes filter to the authenticated Participant. Organisers cannot edit or remove another Organiser's events or event categories.

## API routes

| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register a Participant |
| POST | `/api/auth/login` | Public | Obtain a JWT bearer token |
| GET, PUT | `/api/users/me` | Participant or Organiser | Read or update the signed-in profile |
| GET | `/api/events` | Public | Browse events |
| GET | `/api/events/{id}` | Public | Read event details |
| POST, PUT, DELETE | `/api/events[/{id}]` | Organiser | Create or manage owned events |
| GET | `/api/categories` | Public | Browse categories |
| GET | `/api/categories/{id}` | Public | Read a category |
| POST, PUT, DELETE | `/api/categories[/{id}]` | Organiser | Manage the shared category catalog |
| GET | `/api/events/{eventId}/categories` | Public | List categories offered by an event |
| POST | `/api/events/{eventId}/categories` | Organiser | Add a category to an owned event |
| DELETE | `/api/events/{eventId}/categories/{categoryId}` | Organiser | Remove a category from an owned event |
| POST | `/api/enrolments` | Participant | Enter an event category |
| GET | `/api/enrolments/my` | Participant | List own enrolments |
| GET, DELETE | `/api/enrolments/{id}` | Participant; GET also Organiser | Read an allowed enrolment or cancel own enrolment |
| GET | `/api/events/{eventId}/enrolments` | Organiser | List enrolments for an owned event |
| POST, PUT | `/api/results[/{id}]` | Organiser | Record or edit a result for an owned event |
| GET | `/api/results/my` | Participant | List own results |
| GET | `/api/events/{eventId}/results` | Organiser | List results for an owned event |
| GET | `/api/events/{eventId}/route` | Public | View route information |
| GET | `/api/events/{eventId}/weather` | Public | View weather records |

Expected responses include 200, 201, 204, 400, 401, 403, 404, and 409. Unexpected server errors return 500 through ASP.NET Core's problem-details handler.

## Tests

Run the full suite with:

```powershell
dotnet test RaceDay.sln --configuration Release
```

The tests cover password hashing, token signing and validation, anonymous access to protected routes, public event access, registration (including that a client cannot choose the Organiser role), duplicate email, weak passwords, login success and failure, Organiser event create, update and delete, ownership checks, rejection of the wrong role on Organiser and Participant routes, Participant enrolment with the enrolment recorded, duplicate enrolment rejection, and Organiser result capture with the Participant viewing their own result. HTTP tests use a separate in-memory test database and do not use the development database.

## Continuous integration

GitHub Actions restores, builds, and tests the solution for pushes and pull requests targeting `main` or `part-2`. Add a screenshot of a successful run here after the workflow runs in the GitHub repository:
 <img width="686" height="189" alt="image" src="https://github.com/user-attachments/assets/cd5774b1-2e9a-4601-9169-b89cbd469d37" />



## Part 1 references

- [Submitted Part 1 ERD](DOCS/ERD%20RACEDAY.png)
- [Corrected Part 2 ERD](DOCS/ERD%20RACEDAY%20Part%202.png)
- [Approved SQL schema](DOCS/RACEDAY.SQL)
- [Approved API endpoint plan](DOCS/RaceDay%20API%20Endpoint%20PlaN%20Part%201FA.pdf)
- [Part 2 requirements and evidence tracker](DOCS/Part2_Requirements_Tracker.md)

## Demonstration video

Unlisted video link: **pending recording and upload**. Add the viewable YouTube link after recording the API, authentication and ownership checks, Swagger, tests, database, and CI workflow.

## Submission work still required

- Review the corrected Part 2 ERD and replace the submitted Part 1 ERD in the final submission if approved.
- Run the workflow on GitHub and add its successful CI screenshot.
- Record and link the unlisted demonstration video.
- Preserve the required meaningful commits in the actual Git repository. The supplied ZIP is a source snapshot and does not contain Git history.
