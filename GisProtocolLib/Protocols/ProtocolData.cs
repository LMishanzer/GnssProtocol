using GisProtocolLib.CommonModels;
using GisProtocolLib.Csv.Reading;
using GisProtocolLib.Protocols.Docx;

namespace GisProtocolLib.Protocols;

public class ProtocolData<T> where T : class, IDocxDetails
{
    public required FormDetails FormDetails { get; set; }
    public required string TechnologyType { get; set; }
    public required string CsvDelimiter { get; set; }
    public required string SourceFilePath { get; set; }
    public required string OutputFilePath { get; set; }
    public bool FitForA4 { get; set; }
    public required T ProtocolDocxDetails { get; set; }
    public required ProtocolType ProtocolType { get; set; }

    public bool IsGlobal() => FormDetails.CoordinatesType == "Globální";

    public int GetPrecision() => FormDetails.PrecisionInput ?? 2;

    public ICsvReader GetCsvReader() => TechnologyType switch
    {
        "EMLID" => new EmlidCsvReader(),
        "NIVEL Point" => new NivelCsvReader(),
        _ => throw new Exception("Neznámý typ technologie")
    };
}