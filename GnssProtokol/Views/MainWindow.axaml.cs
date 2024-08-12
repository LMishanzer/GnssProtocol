using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib;
using GisProtocolLib.Csv;
using GisProtocolLib.Models;
using GisProtocolLib.Protocols.Docx;
using GisProtocolLib.Protocols.Text;
using GisProtocolLib.State;
using ComboBoxItem = Avalonia.Controls.ComboBoxItem;

namespace GnssProtokol.Views;

public partial class MainWindow : Window
{
    private readonly LocalDatabase _localDatabase;
    private FormDetails _formDetails = new();
    private bool _useSamePath;

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
            FileTypeFilter =
            [
                new("CSV Files")
                {
                    Patterns = CsvPatterns
                }
            ]
        });

        if (!files.Any()) 
            return;
        
        InputPathTextBox.Text = files[0].Path.LocalPath;
        
        if (_useSamePath)
        {
            OutputPathTextBox.Text = GetOutputFileName("txt");
            OutputDocxPathTextBox.Text = GetOutputFileName("docx");
        }
    }

    public async void OnOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices =
            [
                FilePickerFileTypes.TextPlain
            ],
            DefaultExtension = ".txt",
            ShowOverwritePrompt = true,
            SuggestedFileName = "protokol.txt"
        });

        if (file == null) 
            return;
        
        OutputPathTextBox.Text = file.Path.LocalPath;
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
            var isGlobal = _formDetails.CoordinatesType == "Globální";
            _precision = _formDetails.PrecisionInput ?? 2;

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

            var protocolHelper = new TextProtocolHelper(_formDetails, _precision);
            var textToWrite = protocolHelper.CreateProtocol(measurements, aggregatedPositions, differences);

            var outputFile = OutputPathTextBox.Text ?? string.Empty;

            await File.WriteAllTextAsync(outputFile, textToWrite, Encoding.UTF8);

            var docxProtocolHelper = new DocxProtocolHelper(new DocxDetails
            {
                FormDetails = _formDetails,
                Lokalita = Lokalita.Text,
                Okres = Okres.Text,
                OutputDocxPathTextBox = OutputDocxPathTextBox.Text,
                Poznamky = Poznamky.Text,
                UzemiTextBox = UzemiTextBox.Text
            });

            docxProtocolHelper.CreateProtocol(measurements);

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

    private async void SaveState()
    {
        var state = new FormState
        {
            TypTechnologieIndex = TypTechnologie.SelectedIndex,
            InputFile = InputPathTextBox.Text,
            OutputFile = OutputPathTextBox.Text,
            OutputDocxFile = OutputDocxPathTextBox.Text,
            Precision = _formDetails.PrecisionInput,
            CoordinatesTypeIndex = _formDetails.CoordinatesTypeIndex,
            PouzitaStaniceIndex = _formDetails.PouzitaStaniceIndex,
            Sensor = _formDetails.Sensor,
            TransSoft = _formDetails.TransSoft,
            PolSoft = _formDetails.PolSoft,
            Projection = _formDetails.Projection,
            GeoModel = _formDetails.GeoModel,
            RealizationFrom = _formDetails.RealizationFrom,
            Lokalita = Lokalita.Text,
            Zhotovitel = _formDetails.Zhotovitel,
            Zpracoval = _formDetails.Zpracoval,
            Prijimace = _formDetails.Prijemace,
            Vyrobce = _formDetails.Vyrobce,
            Typ = _formDetails.Typ,
            Cislo = _formDetails.Cislo,
            Anteny = _formDetails.Anteny,
            PristupovyBod = _formDetails.PristupovyBod,
            IntervalZaznamu = _formDetails.IntervalZaznamu,
            ElevacniMaska = _formDetails.ElevacniMaska,
            VyskaAnteny = _formDetails.VyskaAnteny,
            PocetZameneniBodu = _formDetails.PocetZameneniBodu,
            ZpracovatelskyProgram = _formDetails.ZpracovatelskyProgram,
            SouradniceNepripojeny = _formDetails.SouradniceNepripojeny,
            KontrolaPripojeni = _formDetails.KontrolaPripojeni,
            TransformacniPostup = _formDetails.TransformacniPostup,
            TransformaceZpracovatelskyProgram = _formDetails.TransformaceZpracovatelskyProgram,
            Poznamky = Poznamky.Text
        };

        await StateManager.SaveState(state: state);
    }

    private async void RestoreState()
    {
        var state = await StateManager.RestoreState();

        TypTechnologie.SelectedIndex = state.TypTechnologieIndex;
        InputPathTextBox.Text = state.InputFile;
        OutputPathTextBox.Text = state.OutputFile;
        OutputDocxPathTextBox.Text = state.OutputDocxFile;
        Lokalita.Text = state.Lokalita;
        Poznamky.Text = state.Poznamky;
        _formDetails.PrecisionInput = state.Precision;
        _formDetails.CoordinatesTypeIndex = state.CoordinatesTypeIndex ?? 0;
        _formDetails.PouzitaStaniceIndex = state.PouzitaStaniceIndex ?? 0;
        _formDetails.Sensor = state.Sensor;
        _formDetails.TransSoft = state.TransSoft;
        _formDetails.PolSoft = state.PolSoft;
        _formDetails.Projection = state.Projection;
        _formDetails.GeoModel = state.GeoModel;
        _formDetails.RealizationFrom = state.RealizationFrom;
        _formDetails.Zhotovitel = state.Zhotovitel;
        _formDetails.Zpracoval = state.Zpracoval;
        _formDetails.Prijemace = state.Prijimace;
        _formDetails.Vyrobce = state.Vyrobce;
        _formDetails.Typ = state.Typ;
        _formDetails.Cislo = state.Cislo;
        _formDetails.Anteny = state.Anteny;
        _formDetails.PristupovyBod = state.PristupovyBod;
        _formDetails.IntervalZaznamu = state.IntervalZaznamu;
        _formDetails.ElevacniMaska = state.ElevacniMaska;
        _formDetails.VyskaAnteny = state.VyskaAnteny;
        _formDetails.PocetZameneniBodu = state.PocetZameneniBodu;
        _formDetails.ZpracovatelskyProgram = state.ZpracovatelskyProgram;
        _formDetails.SouradniceNepripojeny = state.SouradniceNepripojeny;
        _formDetails.KontrolaPripojeni = state.KontrolaPripojeni;
        _formDetails.TransformacniPostup = state.TransformacniPostup;
        _formDetails.TransformaceZpracovatelskyProgram = state.TransformaceZpracovatelskyProgram;
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
        var detailsDialog = new DetailsWindow(_formDetails);
        var details = await detailsDialog.ShowDialog<FormDetails?>(this);

        if (details != null)
        {
            _formDetails = details;
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

    private void ToggleFileNameSwitch(object? _, RoutedEventArgs _1)
    {
        var isChecked = UseSameName.IsChecked ?? false;
        _useSamePath = isChecked;
        
        if (isChecked)
        {
            TxtPathForm.IsEnabled = false;
            DocxPathForm.IsEnabled = false;
            
            OutputPathTextBox.Text = GetOutputFileName("txt");
            OutputDocxPathTextBox.Text = GetOutputFileName("docx");
        }
        else
        {
            TxtPathForm.IsEnabled = true;
            DocxPathForm.IsEnabled = true;
        }
    }

    private string? GetOutputFileName(string fileType)
    {
        if (InputPathTextBox.Text?.EndsWith(".csv") ?? false)
            return Regex.Replace(InputPathTextBox.Text, "csv$", fileType);

        return InputPathTextBox.Text;
    }
}