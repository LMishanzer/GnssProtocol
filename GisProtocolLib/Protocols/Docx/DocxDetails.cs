using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx;

public class DocxDetails
{
    public string? Lokalita { get; set; }
    public string? UzemiTextBox { get; set; }
    public string? Okres { get; set; }
    public string? Poznamky { get; set; }
    public string? OutputDocxPathTextBox { get; set; }
    public required FormDetails FormDetails { get; set; }
}