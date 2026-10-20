namespace RaceDay.API.Models;

public sealed class Weather
{
    public int WeatherID { get; set; }
    public int EventID { get; set; }
    public decimal? Temperature { get; set; }
    public string? WeatherCondition { get; set; }
    public decimal? WindSpeed { get; set; }
    public DateTime RecordedAt { get; set; }
    public Event Event { get; set; } = null!;
}
