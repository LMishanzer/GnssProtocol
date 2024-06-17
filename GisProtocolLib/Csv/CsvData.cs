using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class CsvData
{
    public List<Measurement> Measurements { get; set; } = [];
    public List<string> UnreadMeasurementNames { get; set; } = [];
}