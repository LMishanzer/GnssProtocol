using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public abstract class BaseCsvReader
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
                csvData.UnreadMeasurementNames.Add(unreadMeasurement);
        }

        return csvData;
    }

    protected abstract Measurement? NextMeasurement(bool isGlobal, CsvReader csvReader);

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