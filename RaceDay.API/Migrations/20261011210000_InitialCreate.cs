using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.API.Data;

namespace RaceDay.API.Migrations;

[DbContext(typeof(RaceDayDbContext))]
[Migration("20261011210000_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        CREATE TABLE [Users] (
            [UserID] int IDENTITY(1,1) NOT NULL,
            [FirstName] varchar(50) NOT NULL,
            [LastName] varchar(50) NOT NULL,
            [Email] varchar(100) NOT NULL,
            [PasswordHash] varchar(255) NOT NULL,
            [Role] varchar(20) NOT NULL,
            CONSTRAINT [PK_Users] PRIMARY KEY ([UserID]),
            CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('Organiser', 'Participant'))
        );
        CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

        CREATE TABLE [Events] (
            [EventID] int IDENTITY(1,1) NOT NULL,
            [OrganiserID] int NOT NULL,
            [EventName] varchar(150) NOT NULL,
            [EventDate] date NOT NULL,
            [Location] varchar(150) NOT NULL,
            [Description] varchar(500) NULL,
            CONSTRAINT [PK_Events] PRIMARY KEY ([EventID]),
            CONSTRAINT [FK_Events_Users] FOREIGN KEY ([OrganiserID]) REFERENCES [Users] ([UserID])
        );

        CREATE TABLE [Categories] (
            [CategoryID] int IDENTITY(1,1) NOT NULL,
            [CategoryName] varchar(100) NOT NULL,
            [Distance] decimal(6,2) NOT NULL,
            [Description] varchar(300) NULL,
            CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID]),
            CONSTRAINT [CK_Categories_Distance] CHECK ([Distance] > 0)
        );

        CREATE TABLE [Routes] (
            [RouteID] int IDENTITY(1,1) NOT NULL,
            [EventID] int NOT NULL,
            [RouteName] varchar(150) NOT NULL,
            [Distance] decimal(6,2) NOT NULL,
            [Elevation] decimal(8,2) NULL,
            [RouteURL] varchar(500) NULL,
            CONSTRAINT [PK_Routes] PRIMARY KEY ([RouteID]),
            CONSTRAINT [FK_Routes_Events] FOREIGN KEY ([EventID]) REFERENCES [Events] ([EventID]),
            CONSTRAINT [CK_Routes_Distance] CHECK ([Distance] > 0),
            CONSTRAINT [CK_Routes_Elevation] CHECK ([Elevation] IS NULL OR [Elevation] >= 0)
        );
        CREATE UNIQUE INDEX [IX_Routes_EventID] ON [Routes] ([EventID]);

        CREATE TABLE [Weather] (
            [WeatherID] int IDENTITY(1,1) NOT NULL,
            [EventID] int NOT NULL,
            [Temperature] decimal(5,2) NULL,
            [WeatherCondition] varchar(100) NULL,
            [WindSpeed] decimal(6,2) NULL,
            [RecordedAt] datetime2 NOT NULL CONSTRAINT [DF_Weather_RecordedAt] DEFAULT (SYSDATETIME()),
            CONSTRAINT [PK_Weather] PRIMARY KEY ([WeatherID]),
            CONSTRAINT [FK_Weather_Events] FOREIGN KEY ([EventID]) REFERENCES [Events] ([EventID]),
            CONSTRAINT [CK_Weather_WindSpeed] CHECK ([WindSpeed] IS NULL OR [WindSpeed] >= 0)
        );

        CREATE TABLE [EventCategories] (
            [EventCategoryID] int IDENTITY(1,1) NOT NULL,
            [EventID] int NOT NULL,
            [CategoryID] int NOT NULL,
            [EntryFee] decimal(10,2) NOT NULL,
            [MaxParticipants] int NULL,
            CONSTRAINT [PK_EventCategories] PRIMARY KEY ([EventCategoryID]),
            CONSTRAINT [FK_EventCategories_Events] FOREIGN KEY ([EventID]) REFERENCES [Events] ([EventID]),
            CONSTRAINT [FK_EventCategories_Categories] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]),
            CONSTRAINT [CK_EventCategories_EntryFee] CHECK ([EntryFee] >= 0),
            CONSTRAINT [CK_EventCategories_MaxParticipants] CHECK ([MaxParticipants] IS NULL OR [MaxParticipants] > 0)
        );
        CREATE UNIQUE INDEX [IX_EventCategories_EventID_CategoryID] ON [EventCategories] ([EventID], [CategoryID]);

        CREATE TABLE [Enrolments] (
            [EnrolmentID] int IDENTITY(1,1) NOT NULL,
            [ParticipantID] int NOT NULL,
            [EventCategoryID] int NOT NULL,
            [EnrolmentDate] datetime2 NOT NULL CONSTRAINT [DF_Enrolments_EnrolmentDate] DEFAULT (SYSDATETIME()),
            [Status] varchar(20) NOT NULL CONSTRAINT [DF_Enrolments_Status] DEFAULT ('Active'),
            CONSTRAINT [PK_Enrolments] PRIMARY KEY ([EnrolmentID]),
            CONSTRAINT [FK_Enrolments_Users] FOREIGN KEY ([ParticipantID]) REFERENCES [Users] ([UserID]),
            CONSTRAINT [FK_Enrolments_EventCategories] FOREIGN KEY ([EventCategoryID]) REFERENCES [EventCategories] ([EventCategoryID]),
            CONSTRAINT [CK_Enrolments_Status] CHECK ([Status] IN ('Active', 'Cancelled', 'Completed'))
        );
        CREATE UNIQUE INDEX [IX_Enrolments_ParticipantID_EventCategoryID] ON [Enrolments] ([ParticipantID], [EventCategoryID]);

        CREATE TABLE [Results] (
            [ResultID] int IDENTITY(1,1) NOT NULL,
            [EnrolmentID] int NOT NULL,
            [FinishTime] time NULL,
            [OverallPosition] int NULL,
            [ResultStatus] varchar(20) NOT NULL CONSTRAINT [DF_Results_ResultStatus] DEFAULT ('Finished'),
            CONSTRAINT [PK_Results] PRIMARY KEY ([ResultID]),
            CONSTRAINT [FK_Results_Enrolments] FOREIGN KEY ([EnrolmentID]) REFERENCES [Enrolments] ([EnrolmentID]),
            CONSTRAINT [CK_Results_Position] CHECK ([OverallPosition] IS NULL OR [OverallPosition] > 0),
            CONSTRAINT [CK_Results_Status] CHECK ([ResultStatus] IN ('Finished', 'Did Not Finish', 'Disqualified'))
        );
        CREATE UNIQUE INDEX [IX_Results_EnrolmentID] ON [Results] ([EnrolmentID]);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DROP TABLE [Results];
        DROP TABLE [Enrolments];
        DROP TABLE [EventCategories];
        DROP TABLE [Weather];
        DROP TABLE [Routes];
        DROP TABLE [Categories];
        DROP TABLE [Events];
        DROP TABLE [Users];
        """);
}
