using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx;

public interface IDocxDetails
{
    public List<Measurement> Measurements { get; set; }
    public List<Coordinates> AggregatedPositions { get; set; }
    public string? OutputDocxPathTextBox { get; set; }
    public FormDetails FormDetails { get; set; }
}