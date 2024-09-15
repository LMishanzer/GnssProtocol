using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.Conversion;

namespace GnssProtokol.Views;

public partial class ImportWindow : Window
{
    private readonly Converter _converter = new();
    
    public ImportWindow()
    {
        InitializeComponent();
    }

    private static readonly string[] CsvPatterns = ["*.csv"];

    private async void OnSelectImportFileButtonClick(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV Files")
                {
                    Patterns = CsvPatterns
                }
            ]
        });

        if (result.Count <= 0)
            return;
        
        ImportFileTextBox.Text = result[0].Path.AbsolutePath;
    }
    
    private async void OnSelectExportFileButtonClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = ".csv",
            ShowOverwritePrompt = true,
            SuggestedFileName = "emlid.csv"
        });

        if (file is null)
            return;
        
        ExportFileTextBox.Text = file.Path.AbsolutePath;
    }

    private async void ProcessImport_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ImportFileTextBox.Text == null || ExportFileTextBox.Text == null)
        {
            Info.Text = "Vyberte soubory pro import a export";
            return;
        }

        try
        {
            await _converter.ConvertAsync(ImportFileTextBox.Text, ExportFileTextBox.Text);
            Info.Text = "Import byl proveden úspěšně";
        }
        catch (Exception ex)
        {
            Info.Text = ex.ToString();
        }
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e) => Close();
}