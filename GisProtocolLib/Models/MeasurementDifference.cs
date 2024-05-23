namespace GisProtocolLib.Models;

public class MeasurementDifference
{
    public string Name { get; init; } = string.Empty;
    public decimal Longitude { get; init; }
    public decimal Latitude { get; init; }
    public decimal Height { get; init; }
    public decimal Distance { get; set; }
    public TimeSpan DeltaTime { get; init; }
}