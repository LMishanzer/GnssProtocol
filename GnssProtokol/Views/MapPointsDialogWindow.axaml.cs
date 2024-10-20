using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.CommonModels;
using GisProtocolLib.Messages;
using GisProtocolLib.Protocols;

namespace GnssProtokol.Views;

public partial class MapPointsDialogWindow : Window
{
    private readonly ProtocolData _protocolData;

    public MapPointsDialogWindow(ProtocolData protocolData)
    {
        _protocolData = protocolData;
        
        InitializeComponent();
    }

    public async void OnOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = [ new FilePickerFileType("CSV file")
            {
                MimeTypes = ["text/csv"]
            }],
            DefaultExtension = ".csv",
            ShowOverwritePrompt = true,
            SuggestedFileName = "seznam_souradnic_surovych_mereni.csv"
        });

        if (file == null) 
            return;
        
        OutputFilePath.Text = file.Path.LocalPath;
    }
    
    private async void Process(object? sender, RoutedEventArgs e)
    {
        var outputFilePath = OutputFilePath.Text ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            Info.Text = "Zadejte výsledný soubor";
            return;
        }

        try
        {
            _protocolData.OutputFilePath = outputFilePath;
            _protocolData.OnlyAveragedPoints = true;
            
            var isGlobal = _protocolData.IsGlobal();
            var csvReader = _protocolData.GetCsvReader();
            var csvWriter = _protocolData.GetCsvWriter();
            var csvData = await csvReader.ReadData(_protocolData.SourceFilePath, isGlobal, _protocolData.CsvDelimiter);

            var outputData = csvData.Measurements.Select(m => new ReducedMeasurement
            {
                Name = m.Name,
                Longitude = m.Longitude,
                Latitude = m.Latitude,
                Height = m.Height,
                Code = m.Code
            });
            
            await csvWriter.WriteData(outputFilePath, outputData, isGlobal, _protocolData.CsvDelimiter);

            var statusString = StatusMessageHandler.GetStatus(csvData.UnreadMeasurements.Names);
            
            Info.Text = statusString;
        }
        catch (Exception ex)
        {
            Info.Text = StatusMessageHandler.GetErrorString(ex);
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}