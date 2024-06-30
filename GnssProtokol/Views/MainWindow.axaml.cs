using System;
using System.Collections.Generic;
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
            FileTypeChoices = new[]
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
            var isGlobal = _details.CoordinatesType == "Globální";
            _precision = _details.PrecisionInput ?? 2;

            var typTechnologie = (TypTechnologie.SelectedItem as ComboBoxItem)?.Content as string ?? string.Empty;
            ICsvReader csvReader = typTechnologie switch
            {
                "EMLID" => new EmlidCsvReader(),
                "NIVEL Point" => new NivelCsvReader(),
                _ => throw new Exception("Neznámý typ technologie")
            };

            var delimiterValue = (Delimiter.SelectedItem as ComboBoxItem)?.Content as string;
            
            var csvData = await csvReader.ReadData(filePath, isGlobal, delimiterValue ?? ",");
            var measurements = csvData.Measurements;

            var (aggregatedPositions, differences) = PositionsHelper.AggregatePositions(measurements);

            var protocolHelper = new ProtocolHelper(_details, _precision);
            var textToWrite = protocolHelper.CreateProtocol(measurements, aggregatedPositions, differences);

            var outputFile = OutputPathTextBox.Text ?? string.Empty;

            await File.WriteAllTextAsync(outputFile, textToWrite);

            CreateDocxProtocol(measurements);

            Info.Text = GetStatus(csvData.UnreadMeasurementNames);
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

    private static string GetStatus(List<string> unusedMeasurementNames)
    {
        const string successMessage = "Úspěšně dokončeno";
        const string unusedMeasurementsMessage = "Vynechané body:";
        const int maxLines = 10;

        if (unusedMeasurementNames.Count == 0)
            return successMessage;

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(successMessage);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(unusedMeasurementsMessage);

        foreach (var unusedMeasurement in unusedMeasurementNames.Take(maxLines))
        {
            stringBuilder.AppendLine(unusedMeasurement);
        }

        if (unusedMeasurementNames.Count > maxLines)
            stringBuilder.AppendLine("...");

        return stringBuilder.ToString();
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
            Precision = _details.PrecisionInput,
            CoordinatesTypeIndex = _details.CoordinatesTypeIndex,
            PouzitaStaniceIndex = _details.PouzitaStaniceIndex,
            Sensor = _details.Sensor,
            TransSoft = _details.TransSoft,
            PolSoft = _details.PolSoft,
            Projection = _details.Projection,
            GeoModel = _details.GeoModel,
            RealizationFrom = _details.RealizationFrom,
            Lokalita = Lokalita.Text,
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
            Poznamky = Poznamky.Text
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
        Lokalita.Text = state.Lokalita;
        Poznamky.Text = state.Poznamky;
        _details.PrecisionInput = state.Precision;
        _details.CoordinatesTypeIndex = state.CoordinatesTypeIndex ?? 0;
        _details.PouzitaStaniceIndex = state.PouzitaStaniceIndex ?? 0;
        _details.Sensor = state.Sensor;
        _details.TransSoft = state.TransSoft;
        _details.PolSoft = state.PolSoft;
        _details.Projection = state.Projection;
        _details.GeoModel = state.GeoModel;
        _details.RealizationFrom = state.RealizationFrom;
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
    }

    private void CreateDocxProtocol(List<Measurement> measurements)
    {
        var measurementTime = measurements.MaxBy(m => m.TimeEnd)?.TimeEnd;
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
            { "{zamereniDatum}", measurementTime?.ToString("dd.MM.yyyy") ?? string.Empty },
            { "{metoda}", measurements.FirstOrDefault()?.Metoda ?? string.Empty },
            { "{sit}", _details.PouzitaStanice ?? string.Empty },
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
            { "{poznamky}", Poznamky.Text ?? string.Empty }
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