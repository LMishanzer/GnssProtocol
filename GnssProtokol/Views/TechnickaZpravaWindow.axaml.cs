using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.Messages;
using GisProtocolLib.Protocols;
using GisProtocolLib.Protocols.Docx.TechnickaZprava;

namespace GnssProtokol.Views;

public partial class TechnickaZpravaWindow : Window
{
    private readonly ProtocolData<TechnickaZpravaDetails> _protocolData;

    public TechnickaZpravaWindow(ProtocolData<TechnickaZpravaDetails> protocolData)
    {
        _protocolData = protocolData;
        InitializeComponent();
    }
    
    public TechnickaZpravaWindow() => throw new Exception();

    public async void OnOutputButtonClick(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = ".docx",
            ShowOverwritePrompt = true,
            SuggestedFileName = "test.docx"
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
            _protocolData.ProtocolDocxDetails.OutputDocxPathTextBox = outputFilePath;

            var unreadMeasurements = await ProtocolProcessor<TechnickaZpravaDetails>.ProcessProtocol(_protocolData);
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