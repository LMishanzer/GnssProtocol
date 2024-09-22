namespace GisProtocolLib.Extensions;

public static class TimeSpanExtensions
{
    public static string ToLongCzechFormat(this TimeSpan timeSpan)
    {
        var days = timeSpan.Days;
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        var seconds = timeSpan.Seconds;

        var dayPart = days == 1 ? "den" : days < 5 ? "dny" : "dnů";
        var hourPart = hours == 1 ? "hodin" : hours < 5 ? "hodiny" : "hodin";
        var minutePart = minutes == 1 ? "minuta" : minutes < 5 ? "minuty" : "minut";
        var secondPart = seconds == 1 ? "vteřina" : seconds < 5 ? "vteřiny" : "vteřin";

        return $"{days} {dayPart} {hours} {hourPart} {minutes} {minutePart} {seconds} {secondPart}";
    }
    
    public static string ToShortCzechFormat(this TimeSpan timeSpan)
    {
        var days = timeSpan.Days;
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        var seconds = timeSpan.Seconds;

        var dayPart = days > 0 ? $"{days}d " : "";
        var hourPart = hours > 0 ? $"{hours.ToString().PadLeft(2, '0')}h " : "";
        var minutePart = minutes > 0 ? $"{minutes.ToString().PadLeft(2, '0')}m " : "";
        var secondPart = $"{seconds.ToString().PadLeft(2, '0')}s";
        return $"{dayPart}{hourPart}{minutePart}{secondPart}";
    }
}