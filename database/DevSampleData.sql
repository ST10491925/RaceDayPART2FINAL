/* Optional local demo data for an already migrated RaceDayDB.
   Set @OrganiserEmail to an Organiser account created with --provision-organiser.
   This script is additive and does not drop or recreate the database. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @OrganiserEmail varchar(100) = 'replace-with-your-organiser-email@example.test';
DECLARE @OrganiserID int;
SELECT @OrganiserID = UserID FROM Users WHERE Email = @OrganiserEmail AND Role = 'Organiser';
IF @OrganiserID IS NULL
    THROW 50001, 'Create an Organiser account and set @OrganiserEmail before running the demo data script.', 1;

IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = '5 KM Walk')
    INSERT INTO Categories (CategoryName, Distance, Description) VALUES ('5 KM Walk', 5.00, 'Five kilometre walking category.');
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = '10 KM Run')
    INSERT INTO Categories (CategoryName, Distance, Description) VALUES ('10 KM Run', 10.00, 'Ten kilometre road running category.');
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = '21 KM Half Marathon')
    INSERT INTO Categories (CategoryName, Distance, Description) VALUES ('21 KM Half Marathon', 21.10, 'Half marathon category.');
IF NOT EXISTS (SELECT 1 FROM Categories WHERE CategoryName = '42 KM Marathon')
    INSERT INTO Categories (CategoryName, Distance, Description) VALUES ('42 KM Marathon', 42.20, 'Marathon category.');

IF NOT EXISTS (SELECT 1 FROM Events WHERE OrganiserID = @OrganiserID AND EventName = 'Durban Summer Road Run')
    INSERT INTO Events (OrganiserID, EventName, EventDate, Location, Description)
    VALUES (@OrganiserID, 'Durban Summer Road Run', '2026-11-15', 'Durban, KwaZulu-Natal', 'A community road running event with multiple distances.');
IF NOT EXISTS (SELECT 1 FROM Events WHERE OrganiserID = @OrganiserID AND EventName = 'Durban Charity Walk')
    INSERT INTO Events (OrganiserID, EventName, EventDate, Location, Description)
    VALUES (@OrganiserID, 'Durban Charity Walk', '2026-12-06', 'Durban, KwaZulu-Natal', 'A community charity walking event.');

DECLARE @RunEventID int = (SELECT EventID FROM Events WHERE OrganiserID = @OrganiserID AND EventName = 'Durban Summer Road Run');
DECLARE @WalkEventID int = (SELECT EventID FROM Events WHERE OrganiserID = @OrganiserID AND EventName = 'Durban Charity Walk');
DECLARE @FiveKmID int = (SELECT CategoryID FROM Categories WHERE CategoryName = '5 KM Walk');
DECLARE @TenKmID int = (SELECT CategoryID FROM Categories WHERE CategoryName = '10 KM Run');
DECLARE @HalfID int = (SELECT CategoryID FROM Categories WHERE CategoryName = '21 KM Half Marathon');
DECLARE @FullID int = (SELECT CategoryID FROM Categories WHERE CategoryName = '42 KM Marathon');

IF NOT EXISTS (SELECT 1 FROM EventCategories WHERE EventID = @RunEventID AND CategoryID = @TenKmID)
    INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants) VALUES (@RunEventID, @TenKmID, 150.00, 1000);
IF NOT EXISTS (SELECT 1 FROM EventCategories WHERE EventID = @RunEventID AND CategoryID = @HalfID)
    INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants) VALUES (@RunEventID, @HalfID, 250.00, 800);
IF NOT EXISTS (SELECT 1 FROM EventCategories WHERE EventID = @RunEventID AND CategoryID = @FullID)
    INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants) VALUES (@RunEventID, @FullID, 400.00, 500);
IF NOT EXISTS (SELECT 1 FROM EventCategories WHERE EventID = @WalkEventID AND CategoryID = @FiveKmID)
    INSERT INTO EventCategories (EventID, CategoryID, EntryFee, MaxParticipants) VALUES (@WalkEventID, @FiveKmID, 80.00, 600);

IF NOT EXISTS (SELECT 1 FROM Routes WHERE EventID = @RunEventID)
    INSERT INTO Routes (EventID, RouteName, Distance, Elevation, RouteURL)
    VALUES (@RunEventID, 'Durban Summer Road Route', 42.20, 320.00, 'https://example.com/routes/durban-summer');
IF NOT EXISTS (SELECT 1 FROM Routes WHERE EventID = @WalkEventID)
    INSERT INTO Routes (EventID, RouteName, Distance, Elevation, RouteURL)
    VALUES (@WalkEventID, 'Durban Charity Walk Route', 5.00, 65.00, 'https://example.com/routes/charity-walk');
IF NOT EXISTS (SELECT 1 FROM Weather WHERE EventID = @RunEventID AND RecordedAt = '2026-11-15T06:00:00')
    INSERT INTO Weather (EventID, Temperature, WeatherCondition, WindSpeed, RecordedAt)
    VALUES (@RunEventID, 24.50, 'Partly Cloudy', 12.00, '2026-11-15T06:00:00');
IF NOT EXISTS (SELECT 1 FROM Weather WHERE EventID = @WalkEventID AND RecordedAt = '2026-12-06T07:00:00')
    INSERT INTO Weather (EventID, Temperature, WeatherCondition, WindSpeed, RecordedAt)
    VALUES (@WalkEventID, 23.00, 'Sunny', 8.00, '2026-12-06T07:00:00');

COMMIT TRANSACTION;
