namespace RaceDay.API.Models;

public sealed class EventCategory
{
    public int EventCategoryID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
