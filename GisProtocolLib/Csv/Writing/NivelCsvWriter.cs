namespace GisProtocolLib.Csv.Writing;

public class NivelCsvWriter : BaseCsvWriter
{
    protected override string[] GetHeaders(bool isGlobal) => isGlobal ? ["Název", "Zem. délka", "Zem. šířka", "H", "Popis"] : ["Název", "Y", "X", "Z", "Popis"];
}