using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GisProtocolLib.Conversion;

namespace GnssProtokol.Views;

public partial class ImportWindow : Window
{
    private readonly ImportConverter _importConverter = new();
    
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

        var absolutePath = result[0].Path.LocalPath;
        
        ImportFileTextBox.Text = absolutePath;
        ExportFileTextBox.Text = absolutePath.Replace(result[0].Name, "export.csv");
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
        
        ExportFileTextBox.Text = file.Path.LocalPath;
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
            var delimiter = (Delimiter.SelectedItem as ComboBoxItem)?.Content as string ?? ",";
            await _importConverter.ConvertAsync(ImportFileTextBox.Text, ExportFileTextBox.Text, delimiter);
            Info.Text = "Import byl proveden úspěšně";
        }
        catch (Exception ex)
        {
            Info.Text = $"Import spadl na chybu:{Environment.NewLine}{ex}";
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}