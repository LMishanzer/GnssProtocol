using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx.StandardProtocol;

public class ProtocolDocxDetails : IDocxDetails
{
    public List<Measurement> Measurements { get; set; } = [];
    public List<Coordinates> AggregatedPositions { get; set; } = [];
    public DateTime? MeasurementTime { get; set; }
    public TimeSpan MinInterval { get; set; }
    public decimal? MaxPdop { get; set; }
    public string? Lokalita { get; set; }
    public string? UzemiTextBox { get; set; }
    public string? Okres { get; set; }
    public string? Poznamky { get; set; }
    public string? OutputDocxPathTextBox { get; set; }
    public required FormDetails FormDetails { get; set; }
}