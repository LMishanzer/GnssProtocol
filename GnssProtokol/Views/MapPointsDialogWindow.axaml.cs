using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.CommonModels;
using GisProtocolLib.Messages;
using GisProtocolLib.Protocols;
using GisProtocolLib.Protocols.Docx.StandardProtocol;
using GisProtocolLib.Protocols.Text;

namespace GnssProtokol.Views;

public partial class MapPointsDialogWindow : Window
{
    private readonly ProtocolData<ProtocolDocxDetails> _protocolData;

    public MapPointsDialogWindow(ProtocolData<ProtocolDocxDetails> protocolData)
    {
        _protocolData = protocolData;
        
        InitializeComponent();
    }

    public MapPointsDialogWindow() => throw new Exception();

    public async void OnOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            // FileTypeChoices = [ new FilePickerFileType("CSV file")
            // {
            //     MimeTypes = ["text/csv"]
            // }],
            FileTypeChoices = 
            [
                FilePickerFileTypes.TextPlain
            ],
            DefaultExtension = ".txt",
            ShowOverwritePrompt = true,
            SuggestedFileName = "seznam_souradnic_surovych_mereni.txt"
        });

        if (file == null) 
            return;
        
        OutputFilePath.Text = file.Path.LocalPath;
    }
    
    private async void Process(object? sender, RoutedEventArgs e)
    {
        Info.Text = string.Empty;
        var outputFilePath = OutputFilePath.Text ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            Info.Text = "Zadejte výsledný soubor";
            return;
        }

        try
        {
            ProcessButton.IsEnabled = false;

            _protocolData.OutputFilePath = outputFilePath;

            var isGlobal = _protocolData.IsGlobal();
            var csvReader = _protocolData.GetCsvReader();
            // var csvWriter = _protocolData.GetCsvWriter();
            var csvData = await csvReader.ReadData(_protocolData.SourceFilePath, isGlobal, _protocolData.CsvDelimiter);
            var protocolProcessor = new TextProtocolMaker(_protocolData.FormDetails, _protocolData.GetPrecision());

            var outputData = csvData.Measurements.Select(m => new ReducedMeasurement
            {
                Name = m.Name,
                Longitude = m.Longitude,
                Latitude = m.Latitude,
                Height = m.Height,
                Code = m.Code
            });
            
            var protocol = protocolProcessor.OnlyMappedPoints(outputData);
            
            await File.WriteAllTextAsync(outputFilePath, protocol);

            // await csvWriter.WriteData(outputFilePath, outputData, isGlobal, _protocolData.CsvDelimiter);

            var statusString = StatusMessageHandler.GetStatus(csvData.UnreadMeasurements.Names);

            Info.Text = statusString;
        }
        catch (Exception ex)
        {
            Info.Text = StatusMessageHandler.GetErrorString(ex);
        }
        finally
        {
            ProcessButton.IsEnabled = true;
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}