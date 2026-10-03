using System.Net.Http;
using System.Threading.Tasks;

namespace EventyCN;

public static class Updater
{
    private const string BaseUrl = "https://apiff14risingstones.web.sdo.com/api/home/active/calendar/getActiveCalendarMonth";
    private static readonly HttpClient Client = new();

    static Updater()
    {
        Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        Client.DefaultRequestHeaders.Add("Referer", "https://ff14risingstones.web.sdo.com/");

        Client.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 获取指定月份的活动日历数据
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份 (1-12)</param>
    /// <returns>API 返回的 JSON 字符串</returns>
    public static async Task<string> GetEventsCn(int year, int month)
    {
        try
        {
            var url = $"{BaseUrl}?month={year}-{month:D2}";
            var response = await Client.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "获取活动数据失败");
            return string.Empty;
        }
    }
}
