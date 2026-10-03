using Dalamud.Interface.ImGuiNotification;

namespace EventyCN;

public static class Utils
{
    public static IEnumerable<DateTime> EachDay(DateTime start, DateTime end)
    {
        for(var day = start.Date; day.Date <= end.Date; day = day.AddDays(1))
            yield return new DateTime(day.Year, day.Month, day.Day);
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Dalamud.Utility.Util.OpenLink(url);
        }
        catch
        {
            Plugin.Notification.AddNotification(new Notification { Content = "无法打开链接", Type = NotificationType.Error });
        }
    }
}
