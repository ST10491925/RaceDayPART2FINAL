namespace RaceDay.API.Models;

public sealed class Route
{
    public int RouteID { get; set; }
    public int EventID { get; set; }
    public string RouteName { get; set; } = "";
    public decimal Distance { get; set; }
    public decimal? Elevation { get; set; }
    public string? RouteURL { get; set; }
    public Event Event { get; set; } = null!;
}
