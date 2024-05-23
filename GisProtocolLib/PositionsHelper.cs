using GisProtocolLib.Models;

namespace GisProtocolLib;

public class PositionsHelper
{
    public static (List<Coordinates> Coordinates, List<MeasurementDifference> Differences) AggregatePositions(List<Measurement> measurements)
    {
        foreach (var position in measurements)
        {
            if (!position.Name.Contains('.'))
                continue;

            position.Name = position.Name.Split('.')[0];
        }

        var grouped = measurements.GroupBy(p => p.Name).ToDictionary(i => i.Key, i => i.ToList());
        var resultPositions = new List<Coordinates>(); 
        var resultDifferences = new List<MeasurementDifference>();

        foreach (var (name, coordinates) in grouped)
        {
            var newPosition = new Coordinates
            {
                Name = name,
                Longitude = coordinates.Sum(v => v.Longitude) / coordinates.Count,
                Latitude = coordinates.Sum(v => v.Latitude) / coordinates.Count,
                Height = coordinates.Sum(v => v.Height) / coordinates.Count,
                Code = $"{coordinates.FirstOrDefault()?.Code} {coordinates.FirstOrDefault()?.Description}"
            };
            resultPositions.Add(newPosition);
            
            var sortedCoordinates = coordinates.OrderBy(c => c.TimeEnd).ToList();

            var measurementDifference = new MeasurementDifference
            {
                Name = name,
                Longitude = Math.Abs(sortedCoordinates.First().Longitude - sortedCoordinates.Last().Longitude),
                Latitude = Math.Abs(sortedCoordinates.First().Latitude - sortedCoordinates.Last().Latitude),
                Height = Math.Abs(sortedCoordinates.First().Height - sortedCoordinates.Last().Height),
                Distance = GetDistance(sortedCoordinates.First(),  sortedCoordinates.Last()),
                DeltaTime = (sortedCoordinates.First().TimeEnd - sortedCoordinates.Last().TimeEnd).Duration()
            };
            
            if (measurementDifference.DeltaTime != TimeSpan.Zero)
                resultDifferences.Add(measurementDifference);
        }

        return (resultPositions, resultDifferences);
    }

    private static decimal GetDistance(Measurement point1, Measurement point2)
    {
        var diff1 = (double) (point1.Longitude - point2.Longitude);
        var diff2 = (double) (point1.Latitude - point2.Latitude);
        var diff3 = (double) (point1.Height - point2.Height);
        
        return (decimal) Math.Sqrt(Math.Pow(diff1, 2) + Math.Pow(diff2, 2) + Math.Pow(diff3, 2));
    }
}