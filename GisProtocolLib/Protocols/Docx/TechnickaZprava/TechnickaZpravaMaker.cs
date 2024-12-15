using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GisProtocolLib.ImageGeneration;
using GisProtocolLib.Protocols.Text;
using NonVisualGraphicFrameDrawingProperties = DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace GisProtocolLib.Protocols.Docx.TechnickaZprava;

public class TechnickaZpravaMaker(TechnickaZpravaDetails protocolDocxDetails) : DocxProtocolMaker<TechnickaZpravaDetails>(protocolDocxDetails)
{
    private readonly TextProtocolMaker _textProtocolMaker = new(protocolDocxDetails.FormDetails, protocolDocxDetails.FormDetails.PrecisionInput ?? 2);

    protected override string ProtocolFileName => "technicka_zprava.docx";

    protected override Dictionary<string, string> GetDictionary() =>
        new()
        {
            { "{firma}", ProtocolDocxDetails.FormDetails.Zhotovitel ?? string.Empty },
            { "{zpracoval}", ProtocolDocxDetails.FormDetails.Zpracoval ?? string.Empty },
            { "{katastralniUzemi}", ProtocolDocxDetails.UzemiTextBox ?? string.Empty },
            { "{okres}", ProtocolDocxDetails.Okres ?? string.Empty },

            { "{prijimace}", ProtocolDocxDetails.FormDetails.Prijemace ?? string.Empty },
            { "{vyrobce}", ProtocolDocxDetails.FormDetails.Vyrobce ?? string.Empty },
            { "{typ}", ProtocolDocxDetails.FormDetails.Typ ?? string.Empty },
            { "{cislo}", ProtocolDocxDetails.FormDetails.Cislo ?? string.Empty },
            { "{anteny}", ProtocolDocxDetails.FormDetails.Anteny ?? string.Empty },
            { "{zpracovatelskyProgram}", ProtocolDocxDetails.FormDetails.ZpracovatelskyProgram ?? string.Empty },
            { "souradnicePripojeny", ProtocolDocxDetails.FormDetails.SouradniceNepripojeny ?? string.Empty },
            { "kontrolaPripojeni", ProtocolDocxDetails.FormDetails.KontrolaPripojeni ?? string.Empty },

            { "{mereni}", _textProtocolMaker.AllMeasurementsProtocol(ProtocolDocxDetails.Measurements, 10) },
            { "{prumerovaniBodu}", _textProtocolMaker.PrumerovaniBodu(ProtocolDocxDetails.Measurements, ProtocolDocxDetails.AggregatedPositions, 11) },
            { "{zprumerovaneBody}", _textProtocolMaker.VysledneSouradnice(ProtocolDocxDetails.AggregatedPositions, true) },

            { "{vsechnyBody}", _textProtocolMaker.VysledneSouradnice(ProtocolDocxDetails.AggregatedPositions) },

            { "{datum}", DateTime.Now.ToString("dd.MM.yyyy") }
        };

    protected override void ReplaceFields(Dictionary<string, string> docxDict, WordprocessingDocument document)
    {
        base.ReplaceFields(docxDict, document);

        var mainPart = document.MainDocumentPart;
        var body = mainPart?.Document.Body;

        if (mainPart == null || body == null)
            return;

        const string placeholderText = "{schematicImage}";

        using var schematicImage = new SchematicImageGenerator().GenerateImage(ProtocolDocxDetails.Measurements);

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                if (!text.Text.Contains(placeholderText))
                    continue;

                text.Text = text.Text.Replace(placeholderText, "");

                var imagePart = mainPart.AddImagePart(ImagePartType.Png);

                imagePart.FeedData(schematicImage.ImageStream);

                AddImageToBody(paragraph, mainPart.GetIdOfPart(imagePart));
            }
        }

        mainPart.Document.Save();
    }

    private static void AddImageToBody(Paragraph paragraph, string relationshipId)
    {
        // Define the reference of the image.
        var element =
            new Drawing(
                new Inline(
                    new Extent() { Cx = 4950000L, Cy = 3960000L },
                    new EffectExtent()
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DocProperties()
                    {
                        Id = (UInt32Value)1U,
                        Name = "Picture 1"
                    },
                    new NonVisualGraphicFrameDrawingProperties(
                        new GraphicFrameLocks() { NoChangeAspect = true }),
                    new Graphic(
                        new GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties()
                                        {
                                            Id = (UInt32Value)0U,
                                            Name = "New Bitmap Image.jpg"
                                        },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new Blip(
                                            new BlipExtensionList(
                                                new BlipExtension()
                                                {
                                                    Uri =
                                                        "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                })
                                        )
                                        {
                                            Embed = relationshipId,
                                            CompressionState =
                                                BlipCompressionValues.Print
                                        },
                                        new Stretch(
                                            new FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new Transform2D(
                                            new Offset() { X = 0L, Y = 0L },
                                            new Extents() { Cx = 990000L, Cy = 792000L }),
                                        new PresetGeometry(
                                                new AdjustValueList()
                                            )
                                            { Preset = ShapeTypeValues.Rectangle }))
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                {
                    DistanceFromTop = (UInt32Value)0U,
                    DistanceFromBottom = (UInt32Value)0U,
                    DistanceFromLeft = (UInt32Value)0U,
                    DistanceFromRight = (UInt32Value)0U,
                    EditId = "50D07946"
                });

        paragraph.AppendChild(new Run(element));
    }
}