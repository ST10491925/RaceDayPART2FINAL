
USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* 1. Users */
CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

/* 2. Events */
CREATE TABLE Events
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NULL,
    CONSTRAINT FK_Events_Users
        FOREIGN KEY (OrganiserID) REFERENCES Users(UserID)
);
GO

/* 3. Categories */
CREATE TABLE Categories
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    Description VARCHAR(300) NULL,
    CONSTRAINT CK_Categories_Distance CHECK (Distance > 0)
);
GO

/* 4. EventCategories - resolves Event M:N Category */
CREATE TABLE EventCategories
(
    EventCategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    MaxParticipants INT NULL,
    CONSTRAINT FK_EventCategories_Events
        FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT FK_EventCategories_Categories
        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID),
    CONSTRAINT UQ_EventCategories_Event_Category
        UNIQUE (EventID, CategoryID),
    CONSTRAINT CK_EventCategories_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_MaxParticipants
        CHECK (MaxParticipants IS NULL OR MaxParticipants > 0)
);
GO

/* 5. Enrolments */
CREATE TABLE Enrolments
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventCategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    CONSTRAINT FK_Enrolments_Users
        FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    CONSTRAINT FK_Enrolments_EventCategories
        FOREIGN KEY (EventCategoryID) REFERENCES EventCategories(EventCategoryID),
    CONSTRAINT UQ_Enrolments_Participant_EventCategory
        UNIQUE (ParticipantID, EventCategoryID),
    CONSTRAINT CK_Enrolments_Status
        CHECK (Status IN ('Active', 'Cancelled', 'Completed'))
);
GO

/* 6. Results */
CREATE TABLE Results
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    OverallPosition INT NULL,
    ResultStatus VARCHAR(20) NOT NULL DEFAULT 'Finished',
    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    CONSTRAINT CK_Results_Position
        CHECK (OverallPosition IS NULL OR OverallPosition > 0),
    CONSTRAINT CK_Results_Status
        CHECK (ResultStatus IN ('Finished', 'Did Not Finish', 'Disqualified'))
);
GO

/* 7. Routes */
CREATE TABLE Routes
(
    RouteID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL UNIQUE,
    RouteName VARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    Elevation DECIMAL(8,2) NULL,
    RouteURL VARCHAR(500) NULL,
    CONSTRAINT FK_Routes_Events
        FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT CK_Routes_Distance CHECK (Distance > 0),
    CONSTRAINT CK_Routes_Elevation CHECK (Elevation IS NULL OR Elevation >= 0)
);
GO

/* 8. Weather */
CREATE TABLE Weather
(
    WeatherID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Temperature DECIMAL(5,2) NULL,
    WeatherCondition VARCHAR(100) NULL,
    WindSpeed DECIMAL(6,2) NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Weather_Events
        FOREIGN KEY (EventID) REFERENCES Events(EventID),
    CONSTRAINT CK_Weather_WindSpeed CHECK (WindSpeed IS NULL OR WindSpeed >= 0)
);
GO

/* SAMPLE DATA  */

/* Users */
INSERT INTO Users (FirstName, LastName, Email, PasswordHash, Role)
VALUES
('Sipho', 'Dlamini', 'sipho.organiser@example.com', 'HASH_PLACEHOLDER_1', 'Organiser'),
('Sarah', 'Naidoo', 'sarah.participant@example.com', 'HASH_PLACEHOLDER_2', 'Participant'),
('John', 'Mthembu', 'john.participant@example.com', 'HASH_PLACEHOLDER_3', 'Participant'),
('Ayesha', 'Khan', 'ayesha.participant@example.com', 'HASH_PLACEHOLDER_4', 'Participant');
GO

/* Events */
INSERT INTO Events (OrganiserID, EventName, EventDate, Location, Description)
VALUES
(1, 'Durban Summer Road Run', '2026-11-15', 'Durban, KwaZulu-Natal',
 'A community road running event with multiple distances.'),
(1, 'Durban Charity Walk', '2026-12-06', 'Durban, KwaZulu-Natal',
 'A community charity walking event.');
GO

/* Categories */
INSERT INTO Categories (CategoryName, Distance, Description)
VALUES
('5 KM Walk', 5.00, 'Five kilometre walking category.'),
('10 KM Run', 10.00, 'Ten kilometre road running category.'),
('21 KM Half Marathon', 21.10, 'Half marathon category.'),
('42 KM Marathon', 42.20, 'Marathon category.');
GO

/* EventCategories */
INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants)
VALUES
(1, 2, 150.00, 1000),
(1, 3, 250.00, 800),
(1, 4, 400.00, 500),
(2, 1, 80.00, 600);
GO

/* Routes */
INSERT INTO Routes (EventID, RouteName, Distance, Elevation, RouteURL)
VALUES
(1, 'Durban Summer Road Route', 42.20, 320.00, 'https://example.com/routes/durban-summer'),
(2, 'Durban Charity Walk Route', 5.00, 65.00, 'https://example.com/routes/charity-walk');
GO

/* Weather */
INSERT INTO Weather (EventID, Temperature, WeatherCondition, WindSpeed, RecordedAt)
VALUES
(1, 24.50, 'Partly Cloudy', 12.00, '2026-11-15T06:00:00'),
(2, 23.00, 'Sunny', 8.00, '2026-12-06T07:00:00');
GO

/* Enrolments */
INSERT INTO Enrolments (ParticipantID, EventCategoryID, EnrolmentDate, Status)
VALUES
(2, 1, '2026-09-01T09:00:00', 'Active'),
(3, 2, '2026-09-02T10:30:00', 'Active'),
(4, 3, '2026-09-03T11:15:00', 'Active');
GO

/* Results */
INSERT INTO Results (EnrolmentID, FinishTime, OverallPosition, ResultStatus)
VALUES
(1, '00:58:21', 42, 'Finished'),
(2, '00:49:35', 18, 'Finished');
GO

/*  VERIFICATION QUERIES  */

SELECT * FROM Users;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM EventCategories;
SELECT * FROM Enrolments;
SELECT * FROM Results;
SELECT * FROM Routes;
SELECT * FROM Weather;
GO

/* Relationship check */
SELECT
    u.FirstName + ' ' + u.LastName AS Participant,
    e.EventName,
    c.CategoryName,
    en.Status AS EnrolmentStatus,
    r.FinishTime,
    r.OverallPosition
FROM Enrolments en
INNER JOIN Users u ON en.ParticipantID = u.UserID
INNER JOIN EventCategories ec ON en.EventCategoryID = ec.EventCategoryID
INNER JOIN Events e ON ec.EventID = e.EventID
INNER JOIN Categories c ON ec.CategoryID = c.CategoryID
LEFT JOIN Results r ON en.EnrolmentID = r.EnrolmentID;
GO
