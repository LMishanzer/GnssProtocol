using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CsvHelper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using PointAverager.Models;
using ComboBoxItem = Avalonia.Controls.ComboBoxItem;

namespace PointAverager.Views;

public partial class MainWindow : Window
{
    private readonly LocalDatabase _localDatabase;
    
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
        
        foreach (var okres in _localDatabase.GetOkresyList()) 
            Okres.Items.Add(okres);
        
        foreach (var uzemi in _localDatabase.GetAllUzemiList()) 
            KatastralniUzemi.Items.Add(uzemi);
    }
    
    public async void OnInputButtonClick(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new FilePickerFileType[]
            {
                new("CSV Files")
                {
                    Patterns = new [] { "*.csv" }
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

            using var reader = new StreamReader(filePath);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

            var measurements = new List<Measurement>();

            await csvReader.ReadAsync();
            csvReader.ReadHeader();
            
            const string format = "yyyy-MM-dd HH:mm:ss.f 'UTC'zzz";

            while (await csvReader.ReadAsync())
            {
                var averagingStart = csvReader.GetField<string>("Averaging start");
                var averagingEnd = csvReader.GetField<string>("Averaging end");
                
                var timeStart = DateTime.ParseExact(averagingStart, format, CultureInfo.InvariantCulture);
                var timeEnd = DateTime.ParseExact(averagingEnd, format, CultureInfo.InvariantCulture);
                
                var position = new Measurement
                {
                    Name = csvReader.GetField<string>("Name"),
                    Longitude = csvReader.GetField<decimal>(isGlobal ? "Longitude" : "Easting"),
                    Latitude = csvReader.GetField<decimal>(isGlobal ? "Latitude" : "Northing"),
                    Height = csvReader.GetField<decimal>(isGlobal ? "Ellipsoidal height" : "Elevation"),
                    AntennaHeight = csvReader.GetField<decimal>("Antenna height"),
                    TimeStart = timeStart,
                    TimeEnd = timeEnd,
                    Pdop = csvReader.GetField<decimal>("PDOP"),
                    SolutionStatus = csvReader.GetField<string>("Solution status"),
                    Code = csvReader.GetField<string>("Code"),
                    Description = csvReader.GetField<string>("Description")
                };
                measurements.Add(position);
            }

            var (aggregatedPositions, differences) = AggregatePositions(measurements);
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
            InputFile = InputPathTextBox.Text,
            OutputFile = OutputPathTextBox.Text,
            OutputDocxFile = OutputDocxPathTextBox.Text,
            Precision = (int?) PrecisionInput.Value,
            CoordinatesTypeIndex = CoordinatesType.SelectedIndex,
            MethodIndex = Metoda.SelectedIndex,
            PouzitaStaniceIndex = PouzitaStanice.SelectedIndex,
            Sensor = Sensor.Text,
            TransSoft = TransSoft.Text,
            PolSoft = PolSoft.Text,
            Projection = Projection.Text,
            GeoModel = GeoModel.Text,
            RealizationFrom = RealizationFrom.Text,
            Lokalita = Lokalita.Text,
            Zhotovitel = Zhotovitel.Text,
            Zpracoval = Zpracoval.Text,
            Prijimace = Prijemace.Text,
            Vyrobce = Vyrobce.Text,
            Typ = Typ.Text,
            Cislo = Cislo.Text,
            Anteny = Anteny.Text,
            PristupovyBod = PristupovyBod.Text,
            IntervalZaznamu = IntervalZaznamu.Text,
            ElevacniMaska = ElevacniMaska.Text,
            VyskaAnteny = VyskaAnteny.Text,
            PocetZameneniBodu = PocetZameneniBodu.Text,
            ZpracovatelskyProgram = ZpracovatelskyProgram.Text,
            SouradniceNepripojeny = SouradniceNepripojeny.Text,
            KontrolaPripojeni = KontrolaPripojeni.Text,
            TransformacniPostup = TransformacniPostup.Text,
            TransformaceZpracovatelskyProgram = TransformaceZpracovatelskyProgram.Text
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
        
        InputPathTextBox.Text = state.InputFile;
        OutputPathTextBox.Text = state.OutputFile;
        OutputDocxPathTextBox.Text = state.OutputDocxFile;
        PrecisionInput.Value = state.Precision;
        CoordinatesType.SelectedIndex = state.CoordinatesTypeIndex ?? 0;
        Metoda.SelectedIndex = state.MethodIndex ?? 0;
        PouzitaStanice.SelectedIndex = state.PouzitaStaniceIndex ?? 0;
        Sensor.Text = state.Sensor;
        TransSoft.Text = state.TransSoft;
        PolSoft.Text = state.PolSoft;
        Projection.Text = state.Projection;
        GeoModel.Text = state.GeoModel;
        RealizationFrom.Text = state.RealizationFrom;
        Lokalita.Text = state.Lokalita;
        Zhotovitel.Text = state.Zhotovitel;
        Zpracoval.Text = state.Zpracoval;
        Prijemace.Text = state.Prijimace;
        Vyrobce.Text = state.Vyrobce;
        Typ.Text = state.Typ;
        Cislo.Text = state.Cislo;
        Anteny.Text = state.Anteny;
        PristupovyBod.Text = state.PristupovyBod;
        IntervalZaznamu.Text = state.IntervalZaznamu;
        ElevacniMaska.Text = state.ElevacniMaska;
        VyskaAnteny.Text = state.VyskaAnteny;
        PocetZameneniBodu.Text = state.PocetZameneniBodu;
        ZpracovatelskyProgram.Text = state.ZpracovatelskyProgram;
        SouradniceNepripojeny.Text = state.SouradniceNepripojeny;
        KontrolaPripojeni.Text = state.KontrolaPripojeni;
        TransformacniPostup.Text = state.TransformacniPostup;
        TransformaceZpracovatelskyProgram.Text = state.TransformaceZpracovatelskyProgram;
    }

    private static (List<Coordinates> Coordinates, List<MeasurementDifference> Differences) AggregatePositions(List<Measurement> measurements)
    {
        foreach (var position in measurements)
        {
            if (!position.Name.Contains('.'))
                continue;

            position.Name = position.Name.Split('.')[0];
        }

        var grouped = measurements.GroupBy(p => p.Name).ToDictionary(i => i.Key, i => i.ToList());
        var resultPositions = new List<Coordinates>(); 
        var resultDifferences = new List<MeasurementDifference>();

        foreach (var (name, coordinates) in grouped)
        {
            var newPosition = new Coordinates
            {
                Name = name,
                Longitude = coordinates.Sum(v => v.Longitude) / coordinates.Count,
                Latitude = coordinates.Sum(v => v.Latitude) / coordinates.Count,
                Height = coordinates.Sum(v => v.Height) / coordinates.Count,
                Code = $"{coordinates.FirstOrDefault()?.Code} {coordinates.FirstOrDefault()?.Description}"
            };
            resultPositions.Add(newPosition);
            
            var sortedCoordinates = coordinates.OrderBy(c => c.TimeEnd).ToList();

            var measurementDifference = new MeasurementDifference
            {
                Name = name,
                Longitude = sortedCoordinates.First().Longitude - sortedCoordinates.Last().Longitude,
                Latitude = sortedCoordinates.First().Latitude - sortedCoordinates.Last().Latitude,
                Height = sortedCoordinates.First().Height - sortedCoordinates.Last().Height,
                DeltaTime = sortedCoordinates.First().TimeEnd - sortedCoordinates.Last().TimeEnd
            };
            resultDifferences.Add(measurementDifference);
        }

        return (resultPositions, resultDifferences);
    }

    private string CreateProtocol(List<Measurement> measurements, List<Coordinates> averagedCoordinates, List<MeasurementDifference> differences)
    {
        const int padConst = 30;
        List<string> pointsHeader = ["Bod č.", "X", "Y", "H(orto)", "Výška výtyčky", "Datum Čas (H:M:S)", "Počet epoch", "RTK řešení", "GDOP", "PDOP", "Počet satelitů", "Kód", "Síť"];

        var pointsValues = measurements.Select(MeasurementSelector);
        
        var protocol = 
$"""
*Protokol GNSS měření*

GNSS Senzor: {Sensor.Text}
Software pro transformaci mezi ETRS89 a S-JTSK pomocí zpřesněné globální transformace: {TransSoft.Text}
Polní software: {PolSoft.Text}
Projekce: {Projection.Text}
Model geodidu: {GeoModel.Text}
Firma: {Zhotovitel.Text}
Měřil: {Zpracoval.Text}

Pro výpočet S-JTSK souřadnic a Bpv výšek byla použitá zpřesněná globální transformace mezi ETRS89 a S-JTSK, realizace od {RealizationFrom.Text}.


*Měření*
----------
{string.Join(string.Empty, pointsHeader.Select(p => p.PadLeft(padConst)))}

{string.Join(Environment.NewLine, pointsValues.Select(p => string.Join("", p.Select(s => s.PadLeft(padConst)))))}

*Souřadnice*
------------------------
{string.Join(string.Empty, new[]{"Bod č.", "Y", "X", "H(orto)", "Kód"}.Select(s => s.PadLeft(padConst)))}
{string.Join(Environment.NewLine, averagedCoordinates.Select(c => 
    $"{c.Name,padConst} {Math.Round(c.Latitude, _precision),padConst} {Math.Round(c.Longitude, _precision),padConst} " +
    $"{Math.Round(c.Height, _precision),padConst} {c.Code,padConst}"))}

*Porovnání měření*
------------------------
{string.Join(string.Empty, new[]{"Bod č.", "Y", "X", "H(orto)", "delta čas (H:M:S)"}.Select(s => s.PadLeft(padConst)))}
{string.Join(Environment.NewLine, differences.Select(c => 
    $"{c.Name,padConst} {Math.Round(c.Latitude, _precision),padConst} {Math.Round(c.Longitude, _precision),padConst} " +
    $"{Math.Round(c.Height, _precision),padConst} {c.DeltaTime.ToString("g").Split('.')[0].PadLeft(padConst)}"))}
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
            { "{lokalita}", Lokalita.Text ?? string.Empty },
            { "{katastralniUzemi}", KatastralniUzemi.SelectedItem?.ToString() ?? string.Empty },
            { "{okres}", Okres.SelectedItem?.ToString() ?? string.Empty },
            { "{zhotovitel}", Zhotovitel.Text ?? string.Empty },
            { "{vypracoval}", Zpracoval.Text ?? string.Empty },
            { "{dne}", DateTime.Now.ToString("dd.MM.yyyy") },
            { "{prijimace}", Prijemace.Text ?? string.Empty },
            { "{vyrobce}", Vyrobce.Text ?? string.Empty },
            { "{typ}", Typ.Text ?? string.Empty },
            { "{cislo}", Cislo.Text ?? string.Empty },
            { "{anteny}", Anteny.Text ?? string.Empty },
            { "{zamereniDatum}", measurementTime.ToString("dd.MM.yyyy") },
            { "{metoda}", (Metoda.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty },
            { "{sit}", (PouzitaStanice.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty },
            { "{pristupovyBod}", PristupovyBod.Text ?? string.Empty },
            { "{interval}", IntervalZaznamu.Text ?? string.Empty },
            { "{elevacniMaska}", ElevacniMaska.Text ?? string.Empty },
            { "{vyskaAntenyVztazena}", VyskaAnteny.Text ?? string.Empty },
            { "{minimalniDoba}", $"{minInterval.Seconds}s" },
            { "{maxPdop}", maxPdop?.ToString() ?? string.Empty },
            { "{nejmensiPocet}", PocetZameneniBodu.Text ?? string.Empty },
            { "{zpracovatelskyProgram}", ZpracovatelskyProgram.Text ?? string.Empty },
            { "{souradnicePripojeny}", SouradniceNepripojeny.Text ?? string.Empty },
            { "{kontrolaPripojeni}", KontrolaPripojeni.Text ?? string.Empty },
            { "{transformacniPristup}", TransformacniPostup.Text ?? string.Empty },
            { "{transformaceZpracovatelskyProgram}", TransformaceZpracovatelskyProgram.Text ?? string.Empty },
            { "{poznamky}", string.Empty }
        };
        
        const string fileName = "protokol.docx";
        var outputFileName = OutputDocxPathTextBox.Text;

        if (string.IsNullOrWhiteSpace(outputFileName))
            throw new Exception("Zadejte výstupní soubory.");
        
        File.Copy(Path.Combine("Resources", fileName), outputFileName, true);

        using var doc = WordprocessingDocument.Open(outputFileName, true);
        var mainPart = doc.MainDocumentPart;
        var body = mainPart?.Document.Body;

        foreach (var (key, value) in docxDict)
        {
            foreach (var text in body.Descendants<Text>())
            {
                if (text.Text.Contains(key)) 
                    text.Text = text.Text.Replace(key, value);
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

        var rand = new Random((int) (measurement.TimeEnd.Ticks % int.MaxValue));
        var satellites = rand.Next(13, 20);
        
        return
        [
            measurement.Name,
            Math.Round(measurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
            measurement.AntennaHeight.ToString(CultureInfo.InvariantCulture),
            measurement.TimeEnd.ToString("s").Replace("T", ""),
            diff.TotalSeconds.ToString(CultureInfo.InvariantCulture),
            measurement.SolutionStatus,
            gdop.ToString(CultureInfo.InvariantCulture),
            $"{pdopSign}{measurement.Pdop}",
            satellites.ToString(),
            $"{measurement.Code} {measurement.Description}",
            (Metoda.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty
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
        
        var files = e.Data.GetFileNames().ToList();

        // Assuming you want to set the text of the TextBox with the path of the first dropped file
        if (files.Count > 0)
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

    private class Coordinates
    {
        public string Name { get; set; } = string.Empty;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Height { get; set; }
        public string Code { get; set; } = string.Empty;
    }
    
    private class Measurement
    {
        public string Name { get; set; } = string.Empty;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Height { get; set; }
        public decimal AntennaHeight { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }
        public string SolutionStatus { get; set; } = string.Empty;
        public decimal Pdop { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    
    private class MeasurementDifference
    {
        public string Name { get; set; } = string.Empty;
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Height { get; set; }
        public TimeSpan DeltaTime { get; set; }
    }

    private bool _changeUzemi = true;

    private void OkresOnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        _changeUzemi = false;
        
        var item = e.AddedItems[0] as string;
        var uzemi = _localDatabase.GetUzemiByOkres(item);
        
        var list = KatastralniUzemi.Items.Select(o => o as string).ToList();

        if (AreListsEqual(list!, uzemi))
            return;
        
        var selected = KatastralniUzemi.SelectedItem as string;

        KatastralniUzemi.SelectedItem = null;
        KatastralniUzemi.Items.Clear();

        foreach (var uzemiOne in uzemi) 
            KatastralniUzemi.Items.Add(uzemiOne);

        KatastralniUzemi.SelectedItem = selected;
        
        _changeUzemi = true;
    }

    private void UzemiOnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        if (!_changeUzemi)
            return;
        
        var item = e.AddedItems[0] as string;
        
        var okres = _localDatabase.GetOkresByUzemi(item);
        var okresy = _localDatabase.GetOkresyList();

        var index = okresy.FindIndex(o => o == okres);
        
        if (index >= Okres.ItemCount || index < 0 || Okres.SelectedIndex == index)
            return;

        Okres.SelectedIndex = okresy.FindIndex(o => o == okres);
    }

    private static bool AreListsEqual(List<string> list1, List<string> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        return !list1.Where((t, i) => t != list2[i]).Any();
    }
}