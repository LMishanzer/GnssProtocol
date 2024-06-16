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
            var averagingStart = csvReader.GetField<string>("StartLokální čas");
            var averagingEnd = csvReader.GetField<string>("EndLokální čas");
                
            var timeStart = DateTime.ParseExact(averagingStart, format, CultureInfo.InvariantCulture);
            var timeEnd = DateTime.ParseExact(averagingEnd, format, CultureInfo.InvariantCulture);
                
            var position = new Measurement
            {
                Name = csvReader.GetField<string>("Název"),
                Height = csvReader.GetField<decimal?>(isGlobal ? "H" : "Z") ?? 0,
                AntennaHeight = csvReader.GetField<decimal?>("Ant H") ?? 0,
                TimeStart = timeStart,
                TimeEnd = timeEnd,
                Pdop = csvReader.GetField<decimal?>("PDOP") ?? 0,
                SolutionStatus = csvReader.GetField<string>("Status").Trim(),
                Metoda = csvReader.GetField<string>("MountPoint").Trim(),
                SharedSats = csvReader.GetField<int>("Sdílet Sate"),
                Code = csvReader.GetField<string>("Popis").Trim()
            };

            if (isGlobal)
            {
                var zemDelka = csvReader.GetField<string>("Zem. délka");
                var zemSirka = csvReader.GetField<string>("Zem. šířka");
                position.Longitude = CoordinatesToDegrees(zemDelka);
                position.Latitude = CoordinatesToDegrees(zemSirka);
            }
            else
            {
                position.Longitude = csvReader.GetField<decimal?>("Y") ?? 0;
                position.Latitude = csvReader.GetField<decimal?>("X") ?? 0;
            }

            position.Longitude = Math.Abs(position.Longitude);
            position.Latitude = Math.Abs(position.Latitude);
            
            if (position.AntennaHeight != 0)
                measurements.Add(position);
        }

        return measurements;
    }

    private static decimal CoordinatesToDegrees(string coordinate)
    {
        var coordinateParts = coordinate.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => decimal.TryParse(s, out var result) ? result : 0)
            .ToList();

        if (coordinateParts.Count != 3)
            return 0;

        return coordinateParts[0] + coordinateParts[1] / 60 + coordinateParts[2] / 3600;
    }
}