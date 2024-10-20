using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Csv.Writing;

public abstract class BaseCsvWriter : ICsvWriter
{
    protected abstract string[] GetHeaders(bool isGlobal);
    
    public async Task WriteData(string outputFilePath, IEnumerable<ReducedMeasurement> reducedMeasurements, bool isGlobal, string delimiter = ",")
    {
        await using var fileStream = new StreamWriter(outputFilePath);

        await using var csvWriter = new CsvWriter(fileStream, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter
        });
        
        var headers = GetHeaders(isGlobal);

        foreach (var header in headers)
        {
            csvWriter.WriteField(header);
        }
        
        await csvWriter.NextRecordAsync();

        foreach (var reducedMeasurement in reducedMeasurements)
        {
            csvWriter.WriteField(reducedMeasurement.Name);
            csvWriter.WriteField(reducedMeasurement.Longitude);
            csvWriter.WriteField(reducedMeasurement.Latitude);
            csvWriter.WriteField(reducedMeasurement.Height);
            csvWriter.WriteField(reducedMeasurement.Code);
            
            await csvWriter.NextRecordAsync();
        }
    }
}