using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GisProtocolLib.CommonModels;
using GisProtocolLib.Csv.Models;
using ValidationException = GisProtocolLib.Exceptions.ValidationException;

namespace GisProtocolLib.Csv.Reading;

public abstract class BaseCsvReader : ICsvReader
{
    protected abstract string MainColumnName { get; }
    
    public async Task<CsvData> ReadData(string filePath, bool isGlobal, string delimiter = ",")
    {
        using var reader = new StreamReader(filePath);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter
        });

        var csvData = new CsvData();

        await csvReader.ReadAsync();
        csvReader.ReadHeader();

        var validationResult = ValidateCsvHeader(csvReader.HeaderRecord, isGlobal);

        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.ErrorMessage ?? "CSV soubor není validní");

        while (await csvReader.ReadAsync())
        {
            var position = NextMeasurement(isGlobal, csvReader);

            if (position != null)
            {
                csvData.Measurements.Add(position);
                continue;
            }
            
            var unreadMeasurement = TryGetMeasurementName(csvReader, MainColumnName);
            
            if (!string.IsNullOrWhiteSpace(unreadMeasurement))
                csvData.UnreadMeasurements.Add(unreadMeasurement);
        }

        return csvData;
    }

    protected abstract Measurement? NextMeasurement(bool isGlobal, CsvReader csvReader);
    
    protected abstract ValidationResult ValidateCsvHeader(string[] csvHeader, bool isGlobal);

    private static string? TryGetMeasurementName(CsvReader csvReader, string name)
    {
        try
        {
            return csvReader.GetField<string>(name);
        }
        catch (Exception)
        {
            return null;
        }
    }
}