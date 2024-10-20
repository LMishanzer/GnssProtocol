using System.Globalization;
using CsvHelper;
using GisProtocolLib.CommonModels;

namespace GisProtocolLib.Csv.Reading;

public class EmlidCsvReader : BaseCsvReader
{
    protected override string MainColumnName => "Name";

    private const string DateFormat = "yyyy-MM-dd HH:mm:ss.f 'UTC'zzz";

    private readonly List<string> _mandatoryAttributes =
    [
        "Name", "Antenna height", "Averaging start", "Averaging end", "PDOP", "Easting RMS", "Northing RMS", "Elevation RMS", "Code", "Mount point", "GPS Satellites", 
        "GLONASS Satellites", "Galileo Satellites", "BeiDou Satellites", "QZSS Satellites"
    ];

    protected override Measurement? NextMeasurement(bool isGlobal, CsvReader csvReader)
    {
        var startRead = csvReader.TryGetField<string>("Averaging start", out var averagingStart);
        startRead &= DateTime.TryParseExact(averagingStart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStart);

        var endRead = csvReader.TryGetField<string>("Averaging end", out var averagingEnd);
        endRead &= DateTime.TryParseExact(averagingEnd, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeEnd);

        if (!startRead || !endRead)
            return null;

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

        return position.Validate() ? position : null;
    }

    protected override ValidationResult ValidateCsvHeader(string[] csvHeader, bool isGlobal)
    {
        List<string> otherColumns = isGlobal ? ["Longitude", "Latitude", "Ellipsoidal height"] : ["Easting", "Northing", "Elevation"];
        var errorMessages = new List<string>();

        foreach (var mandatoryColumn in _mandatoryAttributes.Union(otherColumns))
        {
            if (csvHeader.Contains(mandatoryColumn))
                continue;

            
            errorMessages.Add($"CSV soubor neobsahuje povinný sloupec {mandatoryColumn}.");
        }

        if (errorMessages.Count <= 0)
            return new ValidationResult
            {
                IsValid = true
            };
        
        errorMessages.Add("Vytvoření protokolu je pozastaveno.");

        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = string.Join(Environment.NewLine, errorMessages)
        };
    }


    private static string GetTrimmedField(CsvReader csvReader, string fieldName)
    {
        csvReader.TryGetField<string>(fieldName, out var result);
        return result?.Trim() ?? string.Empty;
    }

    private static T GetNumberField<T>(CsvReader csvReader, string fieldName, T defaultValue = default) where T : struct
    {
        if (csvReader.TryGetField<T?>(fieldName, out var result))
        {
            return result ?? defaultValue;
        }

        return defaultValue;
    }
}