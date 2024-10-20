using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Csv.Models;

public class CsvData
{
    public List<Measurement> Measurements { get; set; } = [];
    public UnreadMeasurements UnreadMeasurements { get; set; } = new();
}