namespace RaceDay.API.Models;

public sealed class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public string ResultStatus { get; set; } = "Finished";
    public Enrolment Enrolment { get; set; } = null!;
}
