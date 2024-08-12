namespace GisProtocolLib.Models;

public class Measurement
{
    public string Name { get; set; } = string.Empty;
    public string PointName { get; set; } = string.Empty;
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public decimal Height { get; init; }
    public decimal AntennaHeight { get; init; }
    public DateTime TimeStart { get; init; }
    public DateTime TimeEnd { get; init; }
    public string SolutionStatus { get; init; } = string.Empty;
    public decimal Pdop { get; init; }
    public decimal AccuracyY { get; set; }
    public decimal AccuracyX { get; set; }
    public decimal AccuracyZ { get; set; }
    public string Code { get; init; } = string.Empty;
    public string Metoda { get; set; } = string.Empty;
    public int GpsSatellites { get; set; }
    public int GlonassSatellites { get; set; }
    public int GalileoSatellites { get; set; }
    public int BeidouSatellites { get; set; }
    public int QzssSatellites { get; set; }
    public int SharedSats { get; set; }
    public string Description { get; init; } = string.Empty;

    public int SatellitesCount => Math.Max(SharedSats, GpsSatellites + GlonassSatellites + GalileoSatellites + BeidouSatellites + QzssSatellites);

    public bool Validate()
    {
        var originalLongitude = Longitude;
        var originalLatitude = Latitude;
        
        Longitude = Math.Abs(Longitude);
        Latitude = Math.Abs(Latitude);
        
        return originalLongitude != -1 &&
               originalLatitude != -1 &&
               Height != -1 &&
               AntennaHeight != -1 &&
               Pdop != -1 &&
               AccuracyY != -1 &&
               AccuracyX != -1 &&
               AccuracyZ != -1;
    }
}