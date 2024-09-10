using CsvHelper.Configuration.Attributes;

namespace GisProtocolLib.Conversion.Models;

public class MeasurementReduced
{
    [Index(0)]
    public string Name { get; set; } = string.Empty;
    
    [Index(1)]
    public decimal Easting { get; set; }
    
    [Index(2)]
    public decimal Northing { get; set; }
    
    [Index(3)]
    public decimal Elevation { get; set; }
    
    [Index(4)]
    public string Code { get; set; } = string.Empty;
}