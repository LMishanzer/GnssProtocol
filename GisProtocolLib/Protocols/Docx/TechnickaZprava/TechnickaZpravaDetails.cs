using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx.TechnickaZprava;

public class TechnickaZpravaDetails : IDocxDetails
{
    public List<Measurement> Measurements { get; set; } = [];
    public List<Coordinates> AggregatedPositions { get; set; } = [];
    public string? UzemiTextBox { get; set; }
    public string? Okres { get; set; }
    public string? OutputDocxPathTextBox { get; set; }
    public required FormDetails FormDetails { get; set; }
}