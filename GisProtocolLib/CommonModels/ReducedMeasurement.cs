namespace GisProtocolLib.CommonModels;

public class ReducedMeasurement
{
    public string Name { get; set; } = string.Empty;
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public decimal Height { get; init; }
    public string Code { get; init; } = string.Empty;
}