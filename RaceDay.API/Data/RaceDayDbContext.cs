using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using Route = RaceDay.API.Models.Route;

namespace RaceDay.API.Data;

public sealed class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Weather> Weather => Set<Weather>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(e => {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID); e.Property(x => x.UserID).UseIdentityColumn();
            e.Property(x => x.FirstName).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsUnicode(false).IsRequired(); e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsUnicode(false).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsUnicode(false).IsRequired();
        });
        model.Entity<Event>(e => {
            e.ToTable("Events"); e.HasKey(x => x.EventID); e.Property(x => x.EventID).UseIdentityColumn();
            e.Property(x => x.EventName).HasMaxLength(150).IsUnicode(false).IsRequired(); e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsUnicode(false).IsRequired(); e.Property(x => x.Description).HasMaxLength(500).IsUnicode(false);
            e.HasOne(x => x.Organiser).WithMany(x => x.OrganisedEvents).HasForeignKey(x => x.OrganiserID).HasConstraintName("FK_Events_Users").OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Category>(e => {
            e.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_Distance", "[Distance] > 0"));
            e.HasKey(x => x.CategoryID); e.Property(x => x.CategoryID).UseIdentityColumn();
            e.Property(x => x.CategoryName).HasMaxLength(100).IsUnicode(false).IsRequired(); e.Property(x => x.Distance).HasPrecision(6, 2);
            e.Property(x => x.Description).HasMaxLength(300).IsUnicode(false);
        });
        model.Entity<EventCategory>(e => {
            e.ToTable("EventCategories", t => {
                t.HasCheckConstraint("CK_EventCategories_EntryFee", "[EntryFee] >= 0");
                t.HasCheckConstraint("CK_EventCategories_MaxParticipants", "[MaxParticipants] IS NULL OR [MaxParticipants] > 0");
            });
            e.HasKey(x => x.EventCategoryID); e.Property(x => x.EventCategoryID).UseIdentityColumn();
            e.Property(x => x.EntryFee).HasPrecision(10, 2); e.HasIndex(x => new { x.EventID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Event).WithMany(x => x.EventCategories).HasForeignKey(x => x.EventID).HasConstraintName("FK_EventCategories_Events").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.EventCategories).HasForeignKey(x => x.CategoryID).HasConstraintName("FK_EventCategories_Categories").OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Enrolment>(e => {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Active', 'Cancelled', 'Completed')"));
            e.HasKey(x => x.EnrolmentID); e.Property(x => x.EnrolmentID).UseIdentityColumn();
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Active").IsRequired();
            e.HasIndex(x => new { x.ParticipantID, x.EventCategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(x => x.Enrolments).HasForeignKey(x => x.ParticipantID).HasConstraintName("FK_Enrolments_Users").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EventCategory).WithMany(x => x.Enrolments).HasForeignKey(x => x.EventCategoryID).HasConstraintName("FK_Enrolments_EventCategories").OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Result>(e => {
            e.ToTable("Results", t => {
                t.HasCheckConstraint("CK_Results_Position", "[OverallPosition] IS NULL OR [OverallPosition] > 0");
                t.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'Did Not Finish', 'Disqualified')");
            });
            e.HasKey(x => x.ResultID); e.Property(x => x.ResultID).UseIdentityColumn(); e.HasIndex(x => x.EnrolmentID).IsUnique();
            e.Property(x => x.FinishTime).HasColumnType("time"); e.Property(x => x.ResultStatus).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Finished").IsRequired();
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrolmentID).HasConstraintName("FK_Results_Enrolments").OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Route>(e => {
            e.ToTable("Routes", t => {
                t.HasCheckConstraint("CK_Routes_Distance", "[Distance] > 0");
                t.HasCheckConstraint("CK_Routes_Elevation", "[Elevation] IS NULL OR [Elevation] >= 0");
            });
            e.HasKey(x => x.RouteID); e.Property(x => x.RouteID).UseIdentityColumn(); e.HasIndex(x => x.EventID).IsUnique();
            e.Property(x => x.RouteName).HasMaxLength(150).IsUnicode(false).IsRequired(); e.Property(x => x.Distance).HasPrecision(6, 2);
            e.Property(x => x.Elevation).HasPrecision(8, 2); e.Property(x => x.RouteURL).HasMaxLength(500).IsUnicode(false);
            e.HasOne(x => x.Event).WithOne(x => x.Route).HasForeignKey<Route>(x => x.EventID).HasConstraintName("FK_Routes_Events").OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Weather>(e => {
            e.ToTable("Weather", t => t.HasCheckConstraint("CK_Weather_WindSpeed", "[WindSpeed] IS NULL OR [WindSpeed] >= 0"));
            e.HasKey(x => x.WeatherID); e.Property(x => x.WeatherID).UseIdentityColumn();
            e.Property(x => x.Temperature).HasPrecision(5, 2); e.Property(x => x.WeatherCondition).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.WindSpeed).HasPrecision(6, 2); e.Property(x => x.RecordedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Event).WithMany(x => x.Weather).HasForeignKey(x => x.EventID).HasConstraintName("FK_Weather_Events").OnDelete(DeleteBehavior.Restrict);
        });
    }
}
