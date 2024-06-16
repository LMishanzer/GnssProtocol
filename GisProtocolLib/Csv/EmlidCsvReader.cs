using System.Globalization;
using CsvHelper;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class EmlidCsvReader : ICsvReader
{
    public async Task<List<Measurement>> ReadData(string filePath, bool isGlobal)
    {
        using var reader = new StreamReader(filePath);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

        var measurements = new List<Measurement>();

        await csvReader.ReadAsync();
        csvReader.ReadHeader();
            
        const string format = "yyyy-MM-dd HH:mm:ss.f 'UTC'zzz";

        while (await csvReader.ReadAsync())
        {
            var averagingStart = csvReader.GetField<string>("Averaging start");
            var averagingEnd = csvReader.GetField<string>("Averaging end");
                
            var timeStart = DateTime.ParseExact(averagingStart, format, CultureInfo.InvariantCulture);
            var timeEnd = DateTime.ParseExact(averagingEnd, format, CultureInfo.InvariantCulture);
                
            var position = new Measurement
            {
                Name = csvReader.GetField<string>("Name"),
                Longitude = csvReader.GetField<decimal?>(isGlobal ? "Longitude" : "Easting") ?? 0,
                Latitude = csvReader.GetField<decimal?>(isGlobal ? "Latitude" : "Northing") ?? 0,
                Height = csvReader.GetField<decimal?>(isGlobal ? "Ellipsoidal height" : "Elevation") ?? 0,
                AntennaHeight = csvReader.GetField<decimal?>("Antenna height") ?? 0,
                TimeStart = timeStart,
                TimeEnd = timeEnd,
                Pdop = csvReader.GetField<decimal?>("PDOP") ?? 0,
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
            
            if (position.AntennaHeight != 0)
                measurements.Add(position);
        }

        return measurements;
    }
}