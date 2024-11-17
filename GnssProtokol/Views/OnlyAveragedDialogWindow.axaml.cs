using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.Messages;
using GisProtocolLib.Protocols;

namespace GnssProtokol.Views;

public partial class OnlyAveragedDialogWindow : Window
{
    private readonly ProtocolData _protocolData;

    public OnlyAveragedDialogWindow(ProtocolData protocolData)
    {
        _protocolData = protocolData;
        InitializeComponent();
    }
    
    public OnlyAveragedDialogWindow() => throw new Exception();

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
            SuggestedFileName = "seznam_souradnic_bodu_s_prumerem.txt"
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
            _protocolData.OnlyAveragedPoints = true;

            var unreadMeasurements = await ProtocolProcessor.ProcessProtocol(_protocolData);

            var statusString = StatusMessageHandler.GetStatus(unreadMeasurements.Names);

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