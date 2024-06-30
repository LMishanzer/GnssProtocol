using System.Globalization;
using System.Text;
using GisProtocolLib.Models;

namespace GisProtocolLib;

public class ProtocolHelper
{
    private readonly Details _details;
    private readonly int _precision;

    public ProtocolHelper(Details details, int precision)
    {
        _details = details;
        _precision = precision;
    }
    
    public string CreateProtocol(List<Measurement> measurements, List<Coordinates> averagedCoordinates, List<MeasurementDifference> differences)
    {
        const int tablePadConst = 13;
        const int padConst = 16;
        List<string> pointsHeaderFirstLine =  ["Bod č.", "Y", "X", "Z", "PDOP", "Přesnost", "Přesnost", "Přesnost", "Síť", "Počet",    "Anténa", "Datum", "Začátek", "Doba",   "Kod"];
        List<string> pointsHeaderSecondLine = ["",       "",  "",  "",  "",      "Y",       "X",        "Z",        "",    "satelitů", "výška",  "",      "měření",  "měření", "bodu"];

        var pointsValues = measurements.Select(measurement => MeasurementSelector(measurement, tablePadConst));

        var protocol =
            $"""
             --------------------------------------
             PROTOKOL GNSS (RTK) MĚŘENÍ
             --------------------------------------

             GNSS Senzor: {_details.Sensor}
             Software pro transformaci mezi ETRS89 a S-JTSK pomocí zpřesněné globální transformace: {_details.TransSoft}
             Polní software: {_details.PolSoft}
             Projekce: {_details.Projection}
             Model geoidu: {_details.GeoModel}
             Firma: {_details.Zhotovitel}
             Měřil: {_details.Zpracoval}

             Pro výpočet S-JTSK souřadnic a Bpv výšek byla použitá zpřesněná globální transformace mezi ETRS89 a S-JTSK, realizace od {_details.RealizationFrom}.

             -------------------------
             POUŽITÉ A MĚŘENÉ BODY
             -------------------------

             {string.Join(string.Empty, pointsHeaderFirstLine.Select(p => p.PadLeft(tablePadConst)))}
             {string.Join(string.Empty, pointsHeaderSecondLine.Select(p => p.PadLeft(tablePadConst)))}

             {string.Join(Environment.NewLine, pointsValues.Select(p => string.Join("", p.Select(s => s.PadLeft(tablePadConst)))))}

             -------------------------
             PRŮMĚROVÁNÍ BODŮ
             -------------------------

             {string.Join(string.Empty, new[] { "Číslo bodu", "Y", "X", "Z", "dY", "dX", "dZ" }.Select(s => s.PadLeft(padConst)))}
                 
             {Prumerovani(measurements, averagedCoordinates, padConst)}

             -------------------------
             ZPRŮMĚROVANÉ BODY
             -------------------------

             {string.Join(string.Empty, new[] { "Číslo bodu", "Y", "X", "Z", "Kod" }.Select(s => s.PadLeft(padConst)))}

             {string.Join(Environment.NewLine, averagedCoordinates.Select(c =>
                 $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
                 $"{Math.Round(c.Height, _precision),padConst}{c.Code,padConst}"))}

             -------------------------
             MAX ODCHÝLKY OD PRŮMERU
             -------------------------

             {string.Join(string.Empty, new[] { "Číslo bodu", "dY", "dX", "dZ", "dM", "delta čas" }.Select(s => s.PadLeft(padConst)))}

             {string.Join(Environment.NewLine, differences.Select(c =>
                 $"{c.Name,padConst}{Math.Round(c.Longitude, _precision),padConst}{Math.Round(c.Latitude, _precision),padConst}" +
                 $"{Math.Round(c.Height, _precision),padConst}{Math.Round(c.Distance, _precision),padConst}{c.DeltaTime.ToString("g").Split('.')[0],padConst}"))}
                 
             """;

        return protocol;
    }
    
    private string Prumerovani(List<Measurement> measurements, List<Coordinates> averagedCoordinates, int padConst)
    {
        var stringBuilder = new StringBuilder();

        foreach (var averagedCoordinate in averagedCoordinates)
        {
            var currentMeasurements = measurements
                .Where(m => m.Name.StartsWith($"{averagedCoordinate.Name}.") || m.Name.Equals(averagedCoordinate.Name, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(m => m.Name)
                .ToList();

            if (currentMeasurements.Count <= 1)
                continue;

            stringBuilder.Append(Environment.NewLine);

            foreach (var currentMeasurement in currentMeasurements)
            {
                List<string> diff =
                [
                    currentMeasurement.Name,
                    Math.Round(currentMeasurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Longitude - averagedCoordinate.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Latitude - averagedCoordinate.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                    Math.Round(currentMeasurement.Height - averagedCoordinate.Height, _precision).ToString(CultureInfo.InvariantCulture)
                ];

                stringBuilder.Append(string.Join("", diff.Select(d => d.PadLeft(padConst))));
                stringBuilder.Append(Environment.NewLine);
            }

            stringBuilder.Append(new string('-', padConst * 7));
            stringBuilder.Append(Environment.NewLine);

            var timeDiff = (currentMeasurements.Last().TimeEnd - currentMeasurements.First().TimeEnd).Duration();

            List<string> summary =
            [
                averagedCoordinate.Name,
                Math.Round(averagedCoordinate.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
                Math.Round(averagedCoordinate.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
                Math.Round(averagedCoordinate.Height, _precision).ToString(CultureInfo.InvariantCulture),
                $"    Čas.odstup: {TimeSpanToString(timeDiff)}"
            ];

            stringBuilder.Append(string.Join("", summary.Select(s => s.PadLeft(padConst))));
            stringBuilder.Append(Environment.NewLine);
        }

        return stringBuilder.ToString();
    }
    
    private static string TimeSpanToString(TimeSpan timeSpan) => $"{timeSpan.Days} dnů {timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    
    private List<string> MeasurementSelector(Measurement measurement, int padConst)
    {
        var pdopSign = string.Empty;

        if (measurement.Pdop > 7)
            pdopSign = "*";
        if (measurement.Pdop > 40)
            pdopSign = "#";

        var sit = measurement.Metoda;
        
        if (measurement.Metoda.Length >= padConst)
        {
            sit = measurement.Metoda[..(padConst - 3)];
            sit = $"{sit}..";
        }

        return
        [
            measurement.Name,
            Math.Round(measurement.Longitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Latitude, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.Height, _precision).ToString(CultureInfo.InvariantCulture),
            $"{pdopSign}{Math.Round(measurement.Pdop, _precision)}",
            Math.Round(measurement.AccuracyY, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.AccuracyX, _precision).ToString(CultureInfo.InvariantCulture),
            Math.Round(measurement.AccuracyZ, _precision).ToString(CultureInfo.InvariantCulture),
            sit,
            measurement.SatellitesCount.ToString(),
            measurement.AntennaHeight.ToString(CultureInfo.InvariantCulture),
            measurement.TimeStart.ToString("dd.MM"),
            measurement.TimeStart.ToString("hh:mm"),
            (measurement.TimeEnd - measurement.TimeStart).TotalSeconds.ToString(CultureInfo.InvariantCulture),
            $"{measurement.Code} {measurement.Description}".Trim()
        ];
    }
}