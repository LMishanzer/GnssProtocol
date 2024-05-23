using System.Globalization;
using CsvHelper;
using GisProtocolLib.Models;

namespace GisProtocolLib.Csv;

public class NivelCsvReader : ICsvReader
{
    public async Task<List<Measurement>> ReadData(string filePath, bool isGlobal)
    {
        using var reader = new StreamReader(filePath);
        using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

        var measurements = new List<Measurement>();

        await csvReader.ReadAsync();
        csvReader.ReadHeader();
            
        const string format = "yyyy-MM-dd HH:mm:ss.f";

        while (await csvReader.ReadAsync())
        {
            var averagingStart = csvReader.GetField<string>("StartLocal time");
            var averagingEnd = csvReader.GetField<string>("EndLocal time");
                
            var timeStart = DateTime.ParseExact(averagingStart, format, CultureInfo.InvariantCulture);
            var timeEnd = DateTime.ParseExact(averagingEnd, format, CultureInfo.InvariantCulture);
                
            var position = new Measurement
            {
                Name = csvReader.GetField<string>("Name"),
                Longitude = csvReader.GetField<decimal>(isGlobal ? "Lon" : "E"),
                Latitude = csvReader.GetField<decimal>(isGlobal ? "Lat" : "N"),
                Height = csvReader.GetField<decimal>(isGlobal ? "H" : "Z"),
                AntennaHeight = csvReader.GetField<decimal>("AntH"),
                TimeStart = timeStart,
                TimeEnd = timeEnd,
                Pdop = csvReader.GetField<decimal>("PDOP"),
                SolutionStatus = csvReader.GetField<string>("Status").Trim(),
                Metoda = csvReader.GetField<string>("VRS Name").Trim(),
                SharedSats = csvReader.GetField<int>("Shared Sats"),
                Code = csvReader.GetField<string>("Desc").Trim()
            };
            measurements.Add(position);
        }

        return measurements;
    }
}