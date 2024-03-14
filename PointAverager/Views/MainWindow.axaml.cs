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

namespace PointAverager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        InputPathTextBox.AddHandler(DragDrop.DropEvent, OnFileDrop);
        InputPathTextBox.AddHandler(DragDrop.DragEnterEvent, OnDrag);
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

    private int _precision;

    public async void Process(object sender, RoutedEventArgs e)
    {
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

            Info.Text = "Successfully completed";
        }
        catch (Exception ex)
        {
            Info.Text = $"Error occurred: {ex.Message}";
        }
        finally
        {
            ProcessButton.IsEnabled = true;
        }
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
        const int padConst = 20;
        List<string> pointsHeader = ["Bod č.", "Y", "X", "H(orto)", "Výška výtyčky", "Datum Čas (H:M:S)", "Počet epoch", "RTK řešení", "PDOP", "Kód"];

        var pointsValues = measurements.Select(MeasurementSelector);
        
        var protocol = 
$"""
*Protokol GNSS měření*

GNSS Senzor: {Sensor.Text}
Software pro transformaci mezi ETRS89 a S-JTSK pomocí zpřesněné globální transformace: {TransSoft.Text}
Polní software: {PolSoft.Text}
Projekce: {Projection.Text}
Model geodidu: {GeoModel.Text}

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

    private List<string> MeasurementSelector(Measurement measurement)
    {
        var diff = measurement.TimeEnd - measurement.TimeStart;
        var pdopSign = "";

        if (measurement.Pdop > 7)
            pdopSign = "*";
        if (measurement.Pdop > 40)
            pdopSign = "#";
        
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
            $"{pdopSign}{measurement.Pdop}",
            $"{measurement.Code} {measurement.Description}"
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
}