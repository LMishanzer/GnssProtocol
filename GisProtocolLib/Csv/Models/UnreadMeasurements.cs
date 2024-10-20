namespace GisProtocolLib.Csv.Models;

public class UnreadMeasurements
{
    public List<string> Names { get; } = [];

    public void Add(string measurementName) => Names.Add(measurementName);
}