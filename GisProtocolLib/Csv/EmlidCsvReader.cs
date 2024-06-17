using System.Globalization;
using CsvHelper;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class EmlidCsvReader : BaseCsvReader, ICsvReader
{
    protected override string MainColumnName => "Name";
    
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss.f 'UTC'zzz";

    protected override Measurement? NextMeasurement(bool isGlobal, CsvReader csvReader)
    {
        var averagingStart = csvReader.GetField<string>("Averaging start");
        var averagingEnd = csvReader.GetField<string>("Averaging end");

        var timeStartOk = DateTime.TryParseExact(averagingStart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeStart);
        var timeEndOk = DateTime.TryParseExact(averagingEnd, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeEnd);

        if (!timeStartOk || !timeEndOk)
            return null;

        var position = new Measurement
        {
            Name = csvReader.GetField<string>("Name"),
            Longitude = csvReader.GetField<decimal?>(isGlobal ? "Longitude" : "Easting") ?? -1,
            Latitude = csvReader.GetField<decimal?>(isGlobal ? "Latitude" : "Northing") ?? -1,
            Height = csvReader.GetField<decimal?>(isGlobal ? "Ellipsoidal height" : "Elevation") ?? -1,
            AntennaHeight = csvReader.GetField<decimal?>("Antenna height") ?? -1,
            TimeStart = timeStart,
            TimeEnd = timeEnd,
            Pdop = csvReader.GetField<decimal?>("PDOP") ?? -1,
            AccuracyY = csvReader.GetField<decimal?>("Easting RMS") ?? -1,
            AccuracyX = csvReader.GetField<decimal?>("Northing RMS") ?? -1,
            AccuracyZ = csvReader.GetField<decimal?>("Elevation RMS") ?? -1,
            SolutionStatus = csvReader.GetField<string>("Solution status").Trim(),
            Code = csvReader.GetField<string>("Code").Trim(),
            Metoda = csvReader.GetField<string>("Mount point").Trim(),
            GpsSatellites = csvReader.GetField<int?>("GPS Satellites") ?? 0,
            GlonassSatellites = csvReader.GetField<int?>("GLONASS Satellites") ?? 0,
            GalileoSatellites = csvReader.GetField<int?>("Galileo Satellites") ?? 0,
            BeidouSatellites = csvReader.GetField<int?>("BeiDou Satellites") ?? 0,
            QzssSatellites = csvReader.GetField<int?>("QZSS Satellites") ?? 0,
            Description = csvReader.GetField<string>("Description").Trim()
        };

        position.Longitude = Math.Abs(position.Longitude);
        position.Latitude = Math.Abs(position.Latitude);

        return position.Validate() ? position : null;
    }
}