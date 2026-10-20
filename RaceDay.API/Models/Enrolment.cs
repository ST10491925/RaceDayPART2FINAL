namespace RaceDay.API.Models;

public sealed class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventCategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "Active";
    public User Participant { get; set; } = null!;
    public EventCategory EventCategory { get; set; } = null!;
    public Result? Result { get; set; }
}
