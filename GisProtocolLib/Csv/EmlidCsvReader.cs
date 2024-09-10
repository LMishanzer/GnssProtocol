using System.Globalization;
using CsvHelper;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class EmlidCsvReader : BaseCsvReader, ICsvReader
{
    protected override string MainColumnName => "Name";
    
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss.f 'UTC'zzz";

    protected override Measurement NextMeasurement(bool isGlobal, CsvReader csvReader)
    {
        csvReader.TryGetField<string>("Averaging start", out var averagingStart);
        csvReader.TryGetField<string>("Averaging end", out var averagingEnd);

        DateTime.TryParseExact(averagingStart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStart);
        DateTime.TryParseExact(averagingEnd, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeEnd);

        var position = new Measurement
        {
            Name = GetTrimmedField(csvReader, "Name"),
            Longitude = GetNumberField<decimal>(csvReader, isGlobal ? "Longitude" : "Easting", -1),
            Latitude = GetNumberField<decimal>(csvReader, isGlobal ? "Latitude" : "Northing", -1),
            Height = GetNumberField<decimal>(csvReader, isGlobal ? "Ellipsoidal height" : "Elevation", -1),
            AntennaHeight = GetNumberField<decimal>(csvReader, "Antenna height", -1),
            TimeStart = timeStart,
            TimeEnd = timeEnd,
            Pdop = GetNumberField<decimal>(csvReader, "PDOP", -1),
            AccuracyY = GetNumberField<decimal>(csvReader, "Easting RMS", -1),
            AccuracyX = GetNumberField<decimal>(csvReader, "Northing RMS", -1),
            AccuracyZ = GetNumberField<decimal>(csvReader, "Elevation RMS", -1),
            SolutionStatus = GetTrimmedField(csvReader, "Solution status"),
            Code = GetTrimmedField(csvReader, "Code"),
            Metoda = GetTrimmedField(csvReader, "Mount point"),
            GpsSatellites = GetNumberField(csvReader, "GPS Satellites", -1),
            GlonassSatellites = GetNumberField(csvReader, "GLONASS Satellites", -1),
            GalileoSatellites = GetNumberField(csvReader, "Galileo Satellites", -1),
            BeidouSatellites = GetNumberField(csvReader, "BeiDou Satellites", -1),
            QzssSatellites = GetNumberField(csvReader, "QZSS Satellites", -1),
            Description = GetTrimmedField(csvReader, "Description")
        };

        position.Longitude = Math.Abs(position.Longitude);
        position.Latitude = Math.Abs(position.Latitude);

        return position;

        // return position.Validate() ? position : null;
    }
    
    private static string GetTrimmedField(CsvReader csvReader, string fieldName)
    {
        csvReader.TryGetField<string>(fieldName, out var result);
        return result?.Trim() ?? string.Empty;
    }
    
    private static T GetNumberField<T>(CsvReader csvReader, string fieldName, T defaultValue = default) where T: struct
    {
        if (csvReader.TryGetField<T?>(fieldName, out var result))
        {
            return result ?? defaultValue;
        }
        return defaultValue;
    }
}