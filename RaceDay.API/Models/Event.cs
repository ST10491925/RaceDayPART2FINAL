namespace RaceDay.API.Models;

public sealed class Event
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public User Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public Route? Route { get; set; }
    public ICollection<Weather> Weather { get; set; } = new List<Weather>();
}
