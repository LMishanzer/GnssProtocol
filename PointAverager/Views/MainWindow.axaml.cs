using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            SuggestedFileName = "protokol.txt"
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
        Info.Text = string.Empty;
            
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
        const int padConst = 15;
        List<string> pointsHeader = ["Bod č.", "Y", "X", "Z", "PDOP", "Síť", "Počet satelitů", "Antena vyska", "Datum", "Zacatek mereni", "Doba mereni [s]", "Kod bodu"];

        var pointsValues = measurements.Select(MeasurementSelector);
        
        var protocol = 
$"""
--------------------------------------
PROTOKOL GNSS (RTK) MERENI
--------------------------------------
 Firma:   AZIMUT CZ s.r.o., s.r.o.
          Hrdlořezská 21/31
          190 00  Praha 9

 Zakazka: carha20240220
 Meril:   
 Datum:   01.11.2014

 Pristroj: Trimble Geo7X, fw: 4.95,  vyr. c.: Geo7X00000
 Trimble General Survey SW: 2.80
 Verze protokolu: 4.95
 Souradnicovy system:  Pouzit transformacni modul zpresnene globalni transformace Trimble 2018 verze 1.0 schvaleny CUZK pro mereni od 1.1.2018
 Zona:  Krovak_2018
 Soubor rovinne dotransformace:  KG2018



Vertikalni transformace
-------------------------

Model kvazigeoidu: CR2005


-------------------------
POUZITE A MERENE BODY
-------------------------

{string.Join(string.Empty, pointsHeader.Select(p => p.PadLeft(padConst)))}

{string.Join(Environment.NewLine, pointsValues.Select(p => string.Join("", p.Select(s => s.PadLeft(padConst)))))}

 * Bod meren na: 1 VRS = Trimble VRS NOW CZ
                 2     = TOPNET
                 3 RTK = CZEPOS RTK a RTK3;		3 RTK3-MSM = CZEPOS RTK3-MSM;
                 3 PRS = CZEPOS RTK-PRS;			3 FKP = CZEPOS RTK-FKP;
                 3 MAX = CZEPOS VRS3-MAX;			3 iMAX = CZEPOS VRS3-iMAX;
                 3 MAXG = CZEPOS VRS3-MAX-GG;		3 iMAXG = CZEPOS VRS3-iMAX-GG; 
                 3 CMR = CZEPOS VRS3-iMAX-GG_CMR;	3 CMR+ = CZEPOS VRS3-iMAX-GG_CMR+; 
                 4     = GEOORBIT 
                 5     = ostatni 
 ** Vyska anteny merena od: FC = fazoveho centra; SZ = spodku zavitu; SN = stredu narazniku
 Hodnoty PDOP oznacene * jsou mimo nastavenou toleranci: 7.00
 Hodnoty s RMS oznacene # jsou mimo nastavenou toleranci: 40.00
 Body oznacene ! NoFix ! pred cislem bodu nebyly pri mereni Fixovany!

-------------------------
PRUMEROVANI BODU
-------------------------
{string.Join(string.Empty, new[]{"Cislo bodu", "Y", "X", "Z", "dY", "dX", "dZ"}.Select(s => s.PadLeft(padConst)))}
    
{Prumerovani(measurements, averagedCoordinates)}

-------------------------
ZPRUMEROVANE BODY
-------------------------

{string.Join(string.Empty, new[]{"Cislo bodu", "Y", "X", "Z", "Kod"}.Select(s => s.PadLeft(padConst)))}

{string.Join(Environment.NewLine, averagedCoordinates.Select(c => 
            $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
            $"{Math.Round(c.Height, _precision),padConst}{c.Code,padConst}"))}

-------------------------
MAX ODCHYLKY OD PRUMERU
-------------------------

{string.Join(string.Empty, new[]{"Bod č.", "dY", "dX", "dZ", "dM", "delta čas"}.Select(s => s.PadLeft(padConst)))}

{string.Join(Environment.NewLine, differences.Select(c => 
    $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
    $"{Math.Round(c.Height, _precision),padConst}{Math.Round(c.Distance, _precision),padConst}{c.DeltaTime.ToString("g").Split('.')[0],padConst}"))}
    
""";
        
        
        
        return protocol;
    }

    private string Prumerovani(List<Measurement> measurements, List<Coordinates> averagedCoordinates)
    {
        const int padLeft = 15;
        var stringBuilder = new StringBuilder();
        
        foreach (var averagedCoordinate in averagedCoordinates)
        {
            var currentMeasurements = measurements
                .Where(m => m.Name.StartsWith($"{averagedCoordinate.Name}.") || m.Name.Equals(averagedCoordinate.Name, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(m => m.Name)
                .ToList();
            
            if (currentMeasurements.Count <= 1)
                continue;

            stringBuilder.Append(Environment.NewLine);

            foreach (var currentMeasurement in currentMeasurements)
            {
                List<string> diff = [
                    currentMeasurement.Name,
                    Math.Round(currentMeasurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Longitude - averagedCoordinate.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Latitude - averagedCoordinate.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Height - averagedCoordinate.Height, _precision).ToString(CultureInfo.InvariantCulture)
                ];

                stringBuilder.Append(string.Join("", diff.Select(d => d.PadLeft(padLeft))));
                stringBuilder.Append(Environment.NewLine);
            }

            stringBuilder.Append(new string('-', padLeft * 7));
            stringBuilder.Append(Environment.NewLine);

            List<string> summary =
            [
                averagedCoordinate.Name,
                Math.Round(averagedCoordinate.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                Math.Round(averagedCoordinate.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                Math.Round(averagedCoordinate.Height, _precision).ToString(CultureInfo.InvariantCulture)
            ];

            stringBuilder.Append(string.Join("", summary.Select(s => s.PadLeft(padLeft))));
            stringBuilder.Append(Environment.NewLine);
        }

        return stringBuilder.ToString();
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
            { "{katastralniUzemi}", UzemiTextBox.Text ?? string.Empty },
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
        var pdopSign = string.Empty;

        if (measurement.Pdop > 7)
            pdopSign = "*";
        if (measurement.Pdop > 40)
            pdopSign = "#";
        
        return
        [
            measurement.Name,
            Math.Round(measurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
            $"{pdopSign}{measurement.Pdop}",
            measurement.Metoda,
            measurement.SatellitesCount.ToString(),
            measurement.AntennaHeight.ToString(CultureInfo.InvariantCulture),
            measurement.TimeStart.ToString("dd.MM"),
            measurement.TimeStart.ToString("hh:mm"),
            (measurement.TimeEnd - measurement.TimeStart).TotalSeconds.ToString(CultureInfo.InvariantCulture),
            $"{measurement.Code} {measurement.Description}".Trim()
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

    private async void OpenDetailsClick(object? _1, RoutedEventArgs _2)
    {
        var detailsDialog = new DetailsWindow(_details);
        var details = await detailsDialog.ShowDialog<Details?>(this);
        
        if (details != null)
        {
            _details = details;
        }
    }

    private void InputTextBox_KeyUp(object? _1, KeyEventArgs keyEvent)
    {
        if (keyEvent.Key is Key.Enter or Key.Down or Key.Up)
        {
            _triggerSelectionChanged = true;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(UzemiTextBox.Text))
        {
            SuggestionsListBox.IsVisible = false;
            return;
        }
        
        SuggestionsListBox.IsVisible = true;
        var filteredUzemi = _localDatabase.FilterUzemi(UzemiTextBox.Text);
        
        SuggestionsListBox.Items.Clear();

        foreach (var uzemi in filteredUzemi)
        {
            SuggestionsListBox.Items.Add(uzemi);
        }
        
        SuggestionsListBox.IsVisible = SuggestionsListBox.Items.Any();
    }

    private bool _triggerSelectionChanged = true;
    
    private void SuggestionsListBox_SelectionChanged(object _1, SelectionChangedEventArgs _2)
    {
        if (!_triggerSelectionChanged)
            return;
        
        if (SuggestionsListBox.SelectedItem is not string selectedText) 
            return;
        
        UzemiTextBox.Text = selectedText;
        SuggestionsListBox.IsVisible = false;
        
        var okres = _localDatabase.GetOkresByUzemi(selectedText);
        Okres.Text = okres;
    }
    
    private void InputTextBox_KeyDown(object _, KeyEventArgs keyEvent)
    {
        if (!SuggestionsListBox.IsVisible) 
            return;

        if (keyEvent.Key is Key.Enter or Key.Down or Key.Up)
            _triggerSelectionChanged = false;

        switch (keyEvent.Key)
        {
            case Key.Down:
            {
                if (SuggestionsListBox.SelectedIndex < SuggestionsListBox.ItemCount - 1)
                {
                    SuggestionsListBox.SelectedIndex++;
                    keyEvent.Handled = true;
                }

                break;
            }
            case Key.Up:
            {
                if (SuggestionsListBox.SelectedIndex > 0)
                {
                    SuggestionsListBox.SelectedIndex--;
                    keyEvent.Handled = true;
                }

                break;
            }
            case Key.Enter:
            {
                if (SuggestionsListBox.SelectedItem is string selectedText)
                {
                    UzemiTextBox.Text = selectedText;
                    SuggestionsListBox.IsVisible = false;
                    keyEvent.Handled = true;
                    var okres = _localDatabase.GetOkresByUzemi(selectedText);
                    Okres.Text = okres;
                }

                break;
            }
        }
    }
}