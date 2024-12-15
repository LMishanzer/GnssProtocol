namespace GisProtocolLib.CommonModels;

public class Coordinates
{
    public string Name { get; init; } = string.Empty;
    public decimal Longitude { get; init; }
    public decimal Latitude { get; init; }
    public decimal Height { get; init; }
    public string Code { get; init; } = string.Empty;
    public bool IsAveraged { get; init; }
}