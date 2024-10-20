using System.Text;

namespace GisProtocolLib.Messages;

public static class StatusMessageHandler
{
    public static string GetStatus(List<string> unusedMeasurementNames)
    {
        const string successMessage = "Úspěšně dokončeno";
        const string unusedMeasurementsMessage = "Vynechané body:";
        const int maxLines = 10;

        if (unusedMeasurementNames.Count == 0)
            return successMessage;

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine(successMessage);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(unusedMeasurementsMessage);

        foreach (var unusedMeasurement in unusedMeasurementNames.Take(maxLines))
        {
            stringBuilder.AppendLine(unusedMeasurement);
        }

        if (unusedMeasurementNames.Count > maxLines)
            stringBuilder.AppendLine("...");

        return stringBuilder.ToString();
    }

    public static string GetErrorString(Exception exception) => $"Vznikla chyba: {Environment.NewLine}{exception.Message}";
}