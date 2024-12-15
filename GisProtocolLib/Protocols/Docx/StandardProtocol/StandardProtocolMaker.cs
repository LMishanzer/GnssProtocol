namespace GisProtocolLib.Protocols.Docx.StandardProtocol;

public class StandardProtocolMaker : DocxProtocolMaker<ProtocolDocxDetails>
{
    protected override string ProtocolFileName => "protokol.docx";

    public StandardProtocolMaker(ProtocolDocxDetails protocolDocxDetails) : base(protocolDocxDetails) { }

    protected override Dictionary<string, string> GetDictionary() =>
        new()
        {
            { "{lokalita}", ProtocolDocxDetails.Lokalita ?? string.Empty },
            { "{katastralniUzemi}", ProtocolDocxDetails.UzemiTextBox ?? string.Empty },
            { "{okres}", ProtocolDocxDetails.Okres ?? string.Empty },
            { "{zhotovitel}", ProtocolDocxDetails.FormDetails.Zhotovitel ?? string.Empty },
            { "{vypracoval}", ProtocolDocxDetails.FormDetails.Zpracoval ?? string.Empty },
            { "{dne}", DateTime.Now.ToString("dd.MM.yyyy") },
            { "{prijimace}", ProtocolDocxDetails.FormDetails.Prijemace ?? string.Empty },
            { "{vyrobce}", ProtocolDocxDetails.FormDetails.Vyrobce ?? string.Empty },
            { "{typ}", ProtocolDocxDetails.FormDetails.Typ ?? string.Empty },
            { "{cislo}", ProtocolDocxDetails.FormDetails.Cislo ?? string.Empty },
            { "{anteny}", ProtocolDocxDetails.FormDetails.Anteny ?? string.Empty },
            { "{zamereniDatum}", ProtocolDocxDetails.MeasurementTime?.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{metoda}", ProtocolDocxDetails.Measurements.FirstOrDefault()?.Metoda ?? string.Empty },
            { "{sit}", ProtocolDocxDetails.FormDetails.PouzitaStanice ?? string.Empty },
            { "{pristupovyBod}", ProtocolDocxDetails.FormDetails.PristupovyBod ?? string.Empty },
            { "{interval}", ProtocolDocxDetails.FormDetails.IntervalZaznamu ?? string.Empty },
            { "{elevacniMaska}", ProtocolDocxDetails.FormDetails.ElevacniMaska ?? string.Empty },
            { "{vyskaAntenyVztazena}", ProtocolDocxDetails.FormDetails.VyskaAnteny ?? string.Empty },
            { "{minimalniDoba}", $"{ProtocolDocxDetails.MinInterval.Seconds}s" },
            { "{maxPdop}", ProtocolDocxDetails.MaxPdop?.ToString() ?? string.Empty },
            { "{nejmensiPocet}", ProtocolDocxDetails.FormDetails.PocetZameneniBodu ?? string.Empty },
            { "{zpracovatelskyProgram}", ProtocolDocxDetails.FormDetails.ZpracovatelskyProgram ?? string.Empty },
            { "{souradnicePripojeny}", ProtocolDocxDetails.FormDetails.SouradniceNepripojeny ?? string.Empty },
            { "{kontrolaPripojeni}", ProtocolDocxDetails.FormDetails.KontrolaPripojeni ?? string.Empty },
            { "{transformacniPristup}", ProtocolDocxDetails.FormDetails.TransformacniPostup ?? string.Empty },
            { "{transformaceZpracovatelskyProgram}", ProtocolDocxDetails.FormDetails.TransformaceZpracovatelskyProgram ?? string.Empty },
            { "{poznamky}", ProtocolDocxDetails.Poznamky ?? string.Empty }
        };
}