using System.Globalization;
using CsvHelper;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class NivelCsvReader : BaseCsvReader, ICsvReader
{
    protected override string MainColumnName => "Název";
    
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss.f";

    protected override Measurement? NextMeasurement(bool isGlobal, CsvReader csvReader)
    {
        var averagingStart = csvReader.GetField<string>("StartLokální čas");
        var averagingEnd = csvReader.GetField<string>("EndLokální čas");

        var timeStartOk = DateTime.TryParseExact(averagingStart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStart);
        var timeEndOk = DateTime.TryParseExact(averagingEnd, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeEnd);

        if (!timeStartOk || !timeEndOk)
            return null;

        var position = new Measurement
        {
            Name = csvReader.GetField<string>("Název"),
            Height = csvReader.GetField<decimal?>(isGlobal ? "H" : "Z") ?? -1,
            AntennaHeight = csvReader.GetField<decimal?>("Ant H") ?? -1,
            TimeStart = timeStart,
            TimeEnd = timeEnd,
            Pdop = csvReader.GetField<decimal?>("PDOP") ?? -1,
            AccuracyZ = csvReader.GetField<decimal?>("VRMS") ?? -1,
            SolutionStatus = csvReader.GetField<string>("Status").Trim(),
            Metoda = csvReader.GetField<string>("MountPoint").Trim(),
            SharedSats = csvReader.GetField<int>("Sdílet Sate"),
            Code = csvReader.GetField<string>("Popis").Trim()
        };

        var accuracy = csvReader.GetField<decimal?>("HRMS") ?? -1;

        position.AccuracyY = accuracy;
        position.AccuracyX = accuracy;

        if (isGlobal)
        {
            var zemDelka = csvReader.GetField<string>("Zem. délka");
            var zemSirka = csvReader.GetField<string>("Zem. šířka");
            position.Longitude = CoordinatesToDegrees(zemDelka);
            position.Latitude = CoordinatesToDegrees(zemSirka);
        }
        else
        {
            position.Longitude = GetDecimal(csvReader, "Y") ?? -1;
            position.Latitude = GetDecimal(csvReader, "X") ?? -1;
        }

        position.Longitude = Math.Abs(position.Longitude);
        position.Latitude = Math.Abs(position.Latitude);

        return position.Validate() ? position : null;
    }

    private static decimal? GetDecimal(CsvReader csvReader, string columnName)
    {
        var value = csvReader.GetField<string>(columnName);
        var beautifiedValue = value.Trim().Replace(",", ".");

        return decimal.TryParse(beautifiedValue, out var parsed) ? parsed : null;
    }

    private static decimal CoordinatesToDegrees(string coordinate)
    {
        var coordinateParts = coordinate.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => decimal.TryParse(s, out var result) ? result : 0)
            .ToList();

        if (coordinateParts.Count != 3)
            return -1;

        return coordinateParts[0] + coordinateParts[1] / 60 + coordinateParts[2] / 3600;
    }
}