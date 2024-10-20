using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx;

public class DocxProtocolHelper
{
    private readonly DocxDetails _docxDetails;

    public DocxProtocolHelper(DocxDetails docxDetails)
    {
        _docxDetails = docxDetails;
    }

    public void CreateProtocol(List<Measurement> measurements)
    {
        var measurementTime = measurements.MaxBy(m => m.TimeEnd)?.TimeEnd;
        var maxPdop = measurements.MaxBy(m => m.Pdop)?.Pdop;

        var minInterval = GetMinInterval(measurements);

        var docxDict = GetDictionary(measurements, measurementTime, minInterval, maxPdop);

        const string fileName = "protokol.docx";
        var outputFileName = _docxDetails.OutputDocxPathTextBox;

        if (string.IsNullOrWhiteSpace(outputFileName))
            throw new Exception("Zadejte výstupní soubory.");

        File.Copy(Path.Combine("Resources", fileName), outputFileName, true);

        using var doc = WordprocessingDocument.Open(outputFileName, true);
        var mainPart = doc.MainDocumentPart;
        var body = mainPart?.Document.Body;

        if (body == null)
            return;

        ReplaceFields(docxDict, body);
    }

    private static TimeSpan GetMinInterval(List<Measurement> measurements)
    {
        // select the minimal observation time
        var minIntervalTicks = measurements.Select(measurement => Math.Abs(measurement.TimeEnd.Ticks - measurement.TimeStart.Ticks)).Prepend(long.MaxValue).Min();

        var minInterval = TimeSpan.FromTicks(minIntervalTicks);
        var oneSecond = TimeSpan.FromSeconds(1);
        
        if (minInterval < oneSecond)
            minInterval = oneSecond;
        
        return minInterval;
    }

    private static void ReplaceFields(Dictionary<string, string> docxDict, Body body)
    {
        foreach (var (key, value) in docxDict)
        {
            foreach (var text in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                if (!text.Text.Contains(key))
                    continue;
                
                var lines = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                text.Text = "";

                for (var i = 0; i < lines.Length; i++)
                {
                    text.InsertBeforeSelf(new DocumentFormat.OpenXml.Wordprocessing.Text(lines[i]));

                    if (i < lines.Length - 1)
                        text.InsertBeforeSelf(new Break());
                }

                text.Remove();
            }
        }
    }

    private Dictionary<string, string> GetDictionary(List<Measurement> measurements, DateTime? measurementTime, TimeSpan minInterval, decimal? maxPdop) =>
        new()
        {
            { "{lokalita}", _docxDetails.Lokalita ?? string.Empty },
            { "{katastralniUzemi}", _docxDetails.UzemiTextBox ?? string.Empty },
            { "{okres}", _docxDetails.Okres ?? string.Empty },
            { "{zhotovitel}", _docxDetails.FormDetails.Zhotovitel ?? string.Empty },
            { "{vypracoval}", _docxDetails.FormDetails.Zpracoval ?? string.Empty },
            { "{dne}", DateTime.Now.ToString("dd.MM.yyyy") },
            { "{prijimace}", _docxDetails.FormDetails.Prijemace ?? string.Empty },
            { "{vyrobce}", _docxDetails.FormDetails.Vyrobce ?? string.Empty },
            { "{typ}", _docxDetails.FormDetails.Typ ?? string.Empty },
            { "{cislo}", _docxDetails.FormDetails.Cislo ?? string.Empty },
            { "{anteny}", _docxDetails.FormDetails.Anteny ?? string.Empty },
            { "{zamereniDatum}", measurementTime?.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{metoda}", measurements.FirstOrDefault()?.Metoda ?? string.Empty },
            { "{sit}", _docxDetails.FormDetails.PouzitaStanice ?? string.Empty },
            { "{pristupovyBod}", _docxDetails.FormDetails.PristupovyBod ?? string.Empty },
            { "{interval}", _docxDetails.FormDetails.IntervalZaznamu ?? string.Empty },
            { "{elevacniMaska}", _docxDetails.FormDetails.ElevacniMaska ?? string.Empty },
            { "{vyskaAntenyVztazena}", _docxDetails.FormDetails.VyskaAnteny ?? string.Empty },
            { "{minimalniDoba}", $"{minInterval.Seconds}s" },
            { "{maxPdop}", maxPdop?.ToString() ?? string.Empty },
            { "{nejmensiPocet}", _docxDetails.FormDetails.PocetZameneniBodu ?? string.Empty },
            { "{zpracovatelskyProgram}", _docxDetails.FormDetails.ZpracovatelskyProgram ?? string.Empty },
            { "{souradnicePripojeny}", _docxDetails.FormDetails.SouradniceNepripojeny ?? string.Empty },
            { "{kontrolaPripojeni}", _docxDetails.FormDetails.KontrolaPripojeni ?? string.Empty },
            { "{transformacniPristup}", _docxDetails.FormDetails.TransformacniPostup ?? string.Empty },
            { "{transformaceZpracovatelskyProgram}", _docxDetails.FormDetails.TransformaceZpracovatelskyProgram ?? string.Empty },
            { "{poznamky}", _docxDetails.Poznamky ?? string.Empty }
        };
}