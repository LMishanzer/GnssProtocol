using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GisProtocolLib;
using GisProtocolLib.Csv;
using GisProtocolLib.Models;
using Newtonsoft.Json;
using ComboBoxItem = Avalonia.Controls.ComboBoxItem;

namespace PointAverager.Views;

public partial class MainWindow : Window
{
    private readonly LocalDatabase _localDatabase;
    private Details _details = new();
    
    public MainWindow()
    {
        InitializeComponent();
        InputPathTextBox.AddHandler(DragDrop.DropEvent, OnFileDrop);
        InputPathTextBox.AddHandler(DragDrop.DragEnterEvent, OnDrag);
        
        _localDatabase = new LocalDatabase();
        Init();
        RestoreState();
    }

    private async void Init()
    {
        await _localDatabase.Init();
        
        foreach (var uzemi in _localDatabase.GetAllUzemiList()) 
            KatastralniUzemi.Items.Add(uzemi);
    }

    private static readonly string[] CsvPatterns = ["*.csv"];
    
    public async void OnInputButtonClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new FilePickerFileType[]
            {
                new("CSV Files")
                {
                    Patterns = CsvPatterns
                }
            }
        });
        
        if (files.Any())
        {
            InputPathTextBox.Text = files[0].Path.LocalPath;
        }
    }
    
    public async void OnOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new []
            {
                FilePickerFileTypes.TextPlain, 
            },
            DefaultExtension = ".txt",
            ShowOverwritePrompt = true,
            SuggestedFileName = "output.txt"
        });
        
        if (file != null)
        {
            OutputPathTextBox.Text = file.Path.LocalPath;
        }
    }
    
    public async void OnDocxOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = ".docx",
            ShowOverwritePrompt = true,
            SuggestedFileName = "protokol.docx"
        });
        
        if (file != null)
        {
            OutputDocxPathTextBox.Text = file.Path.LocalPath;
        }
    }

    private int _precision;

    public async void Process(object sender, RoutedEventArgs e)
    {
        SaveState();
        
        var filePath = InputPathTextBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Info.Text = "No input file";
            
            return;
        }

        try
        {
            ProcessButton.IsEnabled = false;
            var isGlobal = CoordinatesType.SelectionBoxItem?.ToString() == "Globální";
            _precision = (int?) PrecisionInput.Value ?? 2;

            var typTechnologie = (TypTechnologie.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
            ICsvReader csvReader = typTechnologie switch
            {
                "EMLID" => new EmlidCsvReader(),
                "NIVEL Point" => new NivelCsvReader(),
                _ => throw new Exception("Neznámý typ technologie")
            };

            var measurements = await csvReader.ReadData(filePath, isGlobal);

            var (aggregatedPositions, differences) = PositionsHelper.AggregatePositions(measurements);
            var textToWrite = CreateProtocol(measurements, aggregatedPositions, differences);

            var outputFile = OutputPathTextBox.Text ?? string.Empty;

            await File.WriteAllTextAsync(outputFile, textToWrite);
            
            CreateDocxProtocol(measurements);

            Info.Text = "Úspěšně dokončeno";
        }
        catch (Exception ex)
        {
            Info.Text = $"Vznikla chyba: {ex.Message}";
        }
        finally
        {
            ProcessButton.IsEnabled = true;
        }
    }

    private const string StateFileName = "state.json";
    
    private async void SaveState()
    {
        var state = new FormState
        {
            TypTechnologieIndex = TypTechnologie.SelectedIndex,
            InputFile = InputPathTextBox.Text,
            OutputFile = OutputPathTextBox.Text,
            OutputDocxFile = OutputDocxPathTextBox.Text,
            Precision = (int?) PrecisionInput.Value,
            CoordinatesTypeIndex = CoordinatesType.SelectedIndex,
            PouzitaStaniceIndex = PouzitaStanice.SelectedIndex,
            Sensor = _details.Sensor,
            TransSoft = _details.TransSoft,
            PolSoft = _details.PolSoft,
            Projection = _details.Projection,
            GeoModel = _details.GeoModel,
            RealizationFrom = _details.RealizationFrom,
            Lokalita = _details.Lokalita,
            Zhotovitel = _details.Zhotovitel,
            Zpracoval = _details.Zpracoval,
            Prijimace = _details.Prijemace,
            Vyrobce = _details.Vyrobce,
            Typ = _details.Typ,
            Cislo = _details.Cislo,
            Anteny = _details.Anteny,
            PristupovyBod = _details.PristupovyBod,
            IntervalZaznamu = _details.IntervalZaznamu,
            ElevacniMaska = _details.ElevacniMaska,
            VyskaAnteny = _details.VyskaAnteny,
            PocetZameneniBodu = _details.PocetZameneniBodu,
            ZpracovatelskyProgram = _details.ZpracovatelskyProgram,
            SouradniceNepripojeny = _details.SouradniceNepripojeny,
            KontrolaPripojeni = _details.KontrolaPripojeni,
            TransformacniPostup = _details.TransformacniPostup,
            TransformaceZpracovatelskyProgram = _details.TransformaceZpracovatelskyProgram,
            Poznamky = _details.Poznamky
        };
        
        await File.WriteAllTextAsync(StateFileName, JsonConvert.SerializeObject(state));
    }

    private async void RestoreState()
    {
        if (!File.Exists(StateFileName))
            return;
        
        var text = await File.ReadAllTextAsync(StateFileName);
        var state = JsonConvert.DeserializeObject<FormState>(text);
        
        if (state == null)
            return;

        TypTechnologie.SelectedIndex = state.TypTechnologieIndex;
        InputPathTextBox.Text = state.InputFile;
        OutputPathTextBox.Text = state.OutputFile;
        OutputDocxPathTextBox.Text = state.OutputDocxFile;
        PrecisionInput.Value = state.Precision;
        CoordinatesType.SelectedIndex = state.CoordinatesTypeIndex ?? 0;
        PouzitaStanice.SelectedIndex = state.PouzitaStaniceIndex ?? 0;
        _details.Sensor = state.Sensor;
        _details.TransSoft = state.TransSoft;
        _details.PolSoft = state.PolSoft;
        _details.Projection = state.Projection;
        _details.GeoModel = state.GeoModel;
        _details.RealizationFrom = state.RealizationFrom;
        _details.Lokalita = state.Lokalita;
        _details.Zhotovitel = state.Zhotovitel;
        _details.Zpracoval = state.Zpracoval;
        _details.Prijemace = state.Prijimace;
        _details.Vyrobce = state.Vyrobce;
        _details.Typ = state.Typ;
        _details.Cislo = state.Cislo;
        _details.Anteny = state.Anteny;
        _details.PristupovyBod = state.PristupovyBod;
        _details.IntervalZaznamu = state.IntervalZaznamu;
        _details.ElevacniMaska = state.ElevacniMaska;
        _details.VyskaAnteny = state.VyskaAnteny;
        _details.PocetZameneniBodu = state.PocetZameneniBodu;
        _details.ZpracovatelskyProgram = state.ZpracovatelskyProgram;
        _details.SouradniceNepripojeny = state.SouradniceNepripojeny;
        _details.KontrolaPripojeni = state.KontrolaPripojeni;
        _details.TransformacniPostup = state.TransformacniPostup;
        _details.TransformaceZpracovatelskyProgram = state.TransformaceZpracovatelskyProgram;
        _details.Poznamky = state.Poznamky;
    }

    

    private string CreateProtocol(List<Measurement> measurements, List<Coordinates> averagedCoordinates, List<MeasurementDifference> differences)
    {
        const int padConst = 30;
        List<string> pointsHeader = ["Bod č.", "Y", "X", "H(orto)", "Výška výtyčky", "Datum Čas (H:M:S)", "Počet epoch", "RTK řešení", "GDOP", "PDOP", "Počet satelitů", "Kód", "Síť"];

        var pointsValues = measurements.Select(MeasurementSelector);
        
        var protocol = 
$"""
*Protokol GNSS měření*

GNSS Senzor: {_details.Sensor}
Software pro transformaci mezi ETRS89 a S-JTSK pomocí zpřesněné globální transformace: {_details.TransSoft}
Polní software: {_details.PolSoft}
Projekce: {_details.Projection}
Model geoidu: {_details.GeoModel}
Firma: {_details.Zhotovitel}
Měřil: {_details.Zpracoval}

Pro výpočet S-JTSK souřadnic a Bpv výšek byla použitá zpřesněná globální transformace mezi ETRS89 a S-JTSK, realizace od {_details.RealizationFrom}.


*Měření*
----------
{string.Join(string.Empty, pointsHeader.Select(p => p.PadLeft(padConst)))}

{string.Join(Environment.NewLine, pointsValues.Select(p => string.Join("", p.Select(s => s.PadLeft(padConst)))))}

*Souřadnice*
------------------------
{string.Join(string.Empty, new[]{"Bod č.", "Y", "X", "H(orto)", "Kód"}.Select(s => s.PadLeft(padConst)))}

{string.Join(Environment.NewLine, averagedCoordinates.Select(c => 
    $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
    $"{Math.Round(c.Height, _precision),padConst}{c.Code,padConst}"))}

*Porovnání měření*
------------------------
{string.Join(string.Empty, new[]{"Bod č.", "dY", "dX", "dZ", "dM", "delta čas (H:M:S)"}.Select(s => s.PadLeft(padConst)))}

{string.Join(Environment.NewLine, differences.Select(c => 
    $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
    $"{Math.Round(c.Height, _precision),padConst}{Math.Round(c.Distance, _precision),padConst}{c.DeltaTime.ToString("g").Split('.')[0],padConst}"))}
    
""";
        return protocol;
    }

    private void CreateDocxProtocol(List<Measurement> measurements)
    {
        var measurementTime = measurements.MaxBy(m => m.TimeEnd)!.TimeEnd;
        var maxPdop = measurements.MaxBy(m => m.Pdop)?.Pdop;

        var minIntervalTicks = long.MaxValue;

        foreach (var measurement1 in measurements)
        {
            foreach (var measurement2 in measurements)
            {
                if (measurement1 != measurement2 && minIntervalTicks > Math.Abs(measurement1.TimeEnd.Ticks - measurement2.TimeEnd.Ticks))
                    minIntervalTicks = Math.Abs(measurement1.TimeEnd.Ticks - measurement2.TimeEnd.Ticks);
            }
        }

        var minInterval = TimeSpan.FromTicks(minIntervalTicks);

        var docxDict = new Dictionary<string, string>
        {
            { "{lokalita}", _details.Lokalita ?? string.Empty },
            { "{katastralniUzemi}", KatastralniUzemi.SelectedItem?.ToString() ?? string.Empty },
            { "{okres}", Okres.Text ?? string.Empty },
            { "{zhotovitel}", _details.Zhotovitel ?? string.Empty },
            { "{vypracoval}", _details.Zpracoval ?? string.Empty },
            { "{dne}", DateTime.Now.ToString("dd.MM.yyyy") },
            { "{prijimace}", _details.Prijemace ?? string.Empty },
            { "{vyrobce}", _details.Vyrobce ?? string.Empty },
            { "{typ}", _details.Typ ?? string.Empty },
            { "{cislo}", _details.Cislo ?? string.Empty },
            { "{anteny}", _details.Anteny ?? string.Empty },
            { "{zamereniDatum}", measurementTime.ToString("dd.MM.yyyy") },
            { "{metoda}", measurements.FirstOrDefault()?.Metoda ?? string.Empty },
            { "{sit}", (PouzitaStanice.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty },
            { "{pristupovyBod}", _details.PristupovyBod ?? string.Empty },
            { "{interval}", _details.IntervalZaznamu ?? string.Empty },
            { "{elevacniMaska}", _details.ElevacniMaska ?? string.Empty },
            { "{vyskaAntenyVztazena}", _details.VyskaAnteny ?? string.Empty },
            { "{minimalniDoba}", $"{minInterval.Seconds}s" },
            { "{maxPdop}", maxPdop?.ToString() ?? string.Empty },
            { "{nejmensiPocet}", _details.PocetZameneniBodu ?? string.Empty },
            { "{zpracovatelskyProgram}", _details.ZpracovatelskyProgram ?? string.Empty },
            { "{souradnicePripojeny}", _details.SouradniceNepripojeny ?? string.Empty },
            { "{kontrolaPripojeni}", _details.KontrolaPripojeni ?? string.Empty },
            { "{transformacniPristup}", _details.TransformacniPostup ?? string.Empty },
            { "{transformaceZpracovatelskyProgram}", _details.TransformaceZpracovatelskyProgram ?? string.Empty },
            { "{poznamky}", _details.Poznamky ?? string.Empty }
        };
        
        const string fileName = "protokol.docx";
        var outputFileName = OutputDocxPathTextBox.Text;

        if (string.IsNullOrWhiteSpace(outputFileName))
            throw new Exception("Zadejte výstupní soubory.");
        
        File.Copy(Path.Combine("Resources", fileName), outputFileName, true);

        using var doc = WordprocessingDocument.Open(outputFileName, true);
        var mainPart = doc.MainDocumentPart;
        var body = mainPart?.Document.Body;
        
        if (body == null)
            return;

        foreach (var (key, value) in docxDict)
        {
            foreach (var text in body.Descendants<Text>())
            {
                if (!text.Text.Contains(key)) 
                    continue;
                var lines = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        
                text.Text = "";

                for (var i = 0; i < lines.Length; i++)
                {
                    text.InsertBeforeSelf(new Text(lines[i]));
                    
                    if (i < lines.Length - 1)
                        text.InsertBeforeSelf(new Break());
                }

                text.Remove();
            }
        }
    }
    
    private List<string> MeasurementSelector(Measurement measurement)
    {
        var diff = measurement.TimeEnd - measurement.TimeStart;
        var pdopSign = "";

        if (measurement.Pdop > 7)
            pdopSign = "*";
        if (measurement.Pdop > 40)
            pdopSign = "#";

        var randDecimal = (decimal) Random.Shared.Next(2, 7);
        var delta = randDecimal / 100;
        var gdop = measurement.Pdop + delta;
        
        return
        [
            measurement.Name,
            Math.Round(measurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
            measurement.AntennaHeight.ToString(CultureInfo.InvariantCulture),
            measurement.TimeEnd.ToString("s").Replace("T", ""),
            diff.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            measurement.SolutionStatus,
            gdop.ToString(CultureInfo.InvariantCulture),
            $"{pdopSign}{measurement.Pdop}",
            measurement.SatellitesCount.ToString(),
            $"{measurement.Code} {measurement.Description}".Trim(),
            measurement.Metoda
        ];
    }

    public void ChangeAccuracy(object sender, SelectionChangedEventArgs e)
    {
        if (PrecisionInput == null)
            return;
        
        var item = e.AddedItems[0] as ComboBoxItem;
        
        if (item?.Content?.ToString() == "Lokální")
        {
            PrecisionInput.Minimum = 2;
            PrecisionInput.Maximum = 3;
            PrecisionInput.Value = 2;
            
            return;
        }
        
        PrecisionInput.Minimum = 7;
        PrecisionInput.Maximum = 10;
        PrecisionInput.Value = 9;
    }
    
    private void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (sender is not TextBox textBox || !e.Data.Contains(DataFormats.FileNames)) 
            return;
        
        var files = e.Data.GetFiles()?.Select(f => f.Name).ToList();

        // Assuming you want to set the text of the TextBox with the path of the first dropped file
        if (files?.Count > 0)
        {
            textBox.Text = files[0];
        }
    }
    
    private void OnDrag(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.Handled = true;
            
        }
    }

    private void UzemiOnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        if (e.AddedItems[0] is not string item)
            return;
        
        var okres = _localDatabase.GetOkresByUzemi(item);
        Okres.Text = okres;
    }

    private async void OpenDetailsClick(object? _1, RoutedEventArgs _2)
    {
        var detailsDialog = new DetailsWindow(_details);
        var details = await detailsDialog.ShowDialog<Details?>(this);
        
        if (details != null)
        {
            _details = details;
        }
    }
}