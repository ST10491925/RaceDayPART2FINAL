# RaceDay Part 2 Requirements Tracker

| Requirement | Implementation | Test or verification | Evidence | Status |
|---|---|---|---|---|
| Use approved Part 1 schema and routes | `RaceDay.API/Models`, `Data/RaceDayDbContext.cs`, controllers | Compare mappings and routes to `RACEDAY.SQL` and endpoint plan | Source files | Implemented; build pending |
| Correct ERD cardinalities | `ERD RACEDAY Part 2.png` | Visual review against SQL unique constraints | Corrected diagram | Implemented; review before replacing Part 1 diagram |
| ASP.NET Core API and xUnit solution | `RaceDay.sln`, `RaceDay.API`, `RaceDay.Tests` | `dotnet build RaceDay.sln` | Build output | Added; restore blocked by unavailable NuGet DNS |
| EF Core Code First model and migration | `RaceDayDbContext.cs`, `Migrations/20261011210000_InitialCreate.cs` | Apply to a clean SQL Server database and compare with Part 1 schema | SQL Server schema screenshot | Migration authored; not applied in this workspace |
| Participant registration and secure login | `AuthController.cs`, `PasswordHasher.cs`, `JwtTokenService.cs` | Registration, duplicate, weak-password, valid/invalid login tests | Test output / Swagger | Implemented; tests not run |
| Role and ownership authorization | Controllers and JWT bearer configuration | Anonymous, wrong-role, wrong-owner tests | Test output / Swagger | Implemented; tests not run |
| Approved event/category/enrolment/result/profile/route/weather endpoints | `Controllers/` | Endpoint success and failure matrix | Swagger requests | Implemented; runtime verification pending |
| Swagger/OpenAPI | `Program.cs`, `Security/OpenApiBearerTransformers.cs` | Exercise public and secured routes in `/swagger` | Screenshots | Implemented; runtime verification pending |
| GitHub Actions restore/build/test workflow | `.github/workflows/dotnet.yml` | Run workflow on GitHub | Green workflow screenshot | Implemented; remote run pending |
| README setup and API guide | `README.md` | Follow from a clean clone | README | Drafted; clean-clone verification pending |
| Unlisted demonstration video | External recording | Open link in private browser | Video link | Pending recording and upload |
| At least 20 meaningful commits in actual repository | Real repository history | Review commit log | GitHub history | Pending: supplied ZIP has no `.git` history |

## Restore limitation

NuGet restore was attempted from this workspace but failed because `api.nuget.org` could not be resolved. The solution has not been built or tested here. No successful build, test, database, CI, or video evidence is claimed.

## Source-of-truth decisions

Assessor feedback says the Part 1 SQL and endpoint plan are strong and the ERD has incorrect cardinalities. The implementation therefore follows the SQL for the schema, including zero-or-one result per enrolment, zero-or-one route per event, and weather linked to events. It follows the API plan's JWT response. Registration creates Participants only; Organiser creation is an administrator-only hidden-password setup command.
