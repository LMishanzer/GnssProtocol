using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using GisProtocolLib.Conversion.Models;

namespace GisProtocolLib.Conversion;

public class ImportConverter
{
    public async Task ConvertAsync(string inputFile, string outputFile, string delimiter = ",")
    {
        if (delimiter is not ("," or ";"))
            throw new ApplicationException("Invalid delimiter");
        
        using var inputFs = new StreamReader(inputFile);
        await using var outputFs = new StreamWriter(outputFile);

        using var csvReader = new CsvReader(inputFs, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            BadDataFound = null
        });

        await using var csvWriter = new CsvWriter(outputFs, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ","
        });

        csvWriter.WriteHeader<MeasurementReduced>();

        while (await csvReader.ReadAsync())
        {
            await csvWriter.NextRecordAsync();
            var withElevation = csvReader.ColumnCount == 5;
            var measurement = GetMeasurement(csvReader, withElevation);
            csvWriter.WriteRecord(measurement);
        }
    }

    private static MeasurementReduced GetMeasurement(CsvReader csvReader, bool withElevation)
    {
        var counter = 0;

        var measurement = new MeasurementReduced
        {
            Name = csvReader[counter++],
            Easting = -Math.Abs(decimal.Parse(csvReader[counter++], CultureInfo.InvariantCulture)),
            Northing = -Math.Abs(decimal.Parse(csvReader[counter++], CultureInfo.InvariantCulture)),
            Elevation = withElevation ? decimal.Parse(csvReader[counter++], CultureInfo.InvariantCulture) : 0,
            Code = csvReader[counter]
        };

        var firstTwenty = measurement.Code[..Math.Min(20, measurement.Code.Length)];

        measurement.Code = RemoveDiacriticsAndPunctuation(firstTwenty);
        measurement.Code = Regex.Replace(measurement.Code, @"\s{2,}", " ").Trim();

        return measurement;
    }
    
    private static string RemoveDiacriticsAndPunctuation(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalizedString = text.Normalize(NormalizationForm.FormD);

        var stringBuilder = new StringBuilder();
        foreach (var c in normalizedString)
        {
            if (char.IsPunctuation(c))
            {
                stringBuilder.Append(' ');
                continue;
            }
            
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}