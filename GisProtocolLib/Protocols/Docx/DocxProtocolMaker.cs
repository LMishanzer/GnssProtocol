using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Protocols.Docx;

public abstract class DocxProtocolMaker<T> where T : class, IDocxDetails
{
    protected readonly T ProtocolDocxDetails;
    protected abstract string ProtocolFileName { get; }

    protected DocxProtocolMaker(T protocolDocxDetails)
    {
        ProtocolDocxDetails = protocolDocxDetails;
    }

    public void CreateProtocol(List<Measurement> measurements, List<Coordinates> aggregatedPositions)
    {
        ProtocolDocxDetails.Measurements = measurements;
        ProtocolDocxDetails.AggregatedPositions = aggregatedPositions;

        var docxDict = GetDictionary();

        var fileName = ProtocolFileName;
        var outputFileName = ProtocolDocxDetails.OutputDocxPathTextBox;

        if (string.IsNullOrWhiteSpace(outputFileName))
            throw new Exception("Zadejte výstupní soubory.");

        File.Copy(Path.Combine("Resources", fileName), outputFileName, true);

        using var document = WordprocessingDocument.Open(outputFileName, true);

        ReplaceFields(docxDict, document);
    }

    protected virtual void ReplaceFields(Dictionary<string, string> docxDict, WordprocessingDocument document)
    {
        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document.Body;

        if (mainPart == null || body == null)
            return;

        var descendants = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().ToList();

        foreach (var (key, value) in docxDict)
        {
            foreach (var text in descendants)
            {
                var lines = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                
                if (text.Text.Equals(key))
                {
                    text.Text = "";

                    for (var i = 0; i < lines.Length; i++)
                    {
                        text.InsertBeforeSelf(new DocumentFormat.OpenXml.Wordprocessing.Text(lines[i])
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                        });

                        if (i < lines.Length - 1)
                            text.InsertBeforeSelf(new Break());
                    }

                    text.Remove();
                }
                else if (text.Text.Contains(key))
                {
                    text.Text = text.Text.Replace(key, lines.First());
                    OpenXmlLeafElement currentText = text;
                
                    if (lines.Length > 1)
                    {
                        var breakElement = new Break();
                        currentText.InsertAfterSelf(breakElement);
                        currentText = breakElement;
                    }

                    for (var i = 1; i < lines.Length; i++)
                    {
                        var newText = new DocumentFormat.OpenXml.Wordprocessing.Text(lines[i])
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                        };
                        
                        currentText.InsertAfterSelf(newText);
                        currentText = newText;

                        if (i < lines.Length - 1)
                        {
                            var breakElement = new Break();
                            currentText.InsertAfterSelf(breakElement);
                            currentText = breakElement;
                        }
                    }                    
                } 
            }
        }

        mainPart.Document.Save();
    }

    protected abstract Dictionary<string, string> GetDictionary();
}