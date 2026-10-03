using System.Collections.Frozen;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EventyCN.Attributes;
using EventyCN.Windows;
using EventyCN.Windows.Config;
using EventyCN.Windows.Main;
using Newtonsoft.Json;

namespace EventyCN;

public class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static INotificationManager Notification { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;

    public static Configuration Configuration { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("EventyCN");
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }

    private readonly PluginCommandManager<Plugin> CommandManager;
    public readonly ServerBar ServerBar;

    public FrozenDictionary<long, ParsedEvent[]> Events = FrozenDictionary<long, ParsedEvent[]>.Empty;

    private readonly HashSet<(int Year, int Month)> _loadedMonths = [];
    private readonly object _loadLock = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager = new PluginCommandManager<Plugin>(this, Commands);
        ServerBar = new ServerBar(this);

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi += OpenMain;

        Task.Run(async () => await LoadEvents());
    }

    [Command("/eventycn")]
    [HelpMessage("打开活动日历")]
    public void OpenMainCommand(string _, string __)
    {
        MainWindow.Toggle();
    }

    [Command("/eventycnconf")]
    [HelpMessage("打开活动日历设置")]
    public void OpenSettingsCommand(string _, string __)
    {
        ConfigWindow.Toggle();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMain;

        CommandManager.Dispose();
        ServerBar.Dispose();
    }

    /// <summary>
    /// 初始化加载：请求当前月 ± 2 月，共 5 个月的数据
    /// </summary>
    private async Task LoadEvents()
    {
        var now = DateTime.Now;
        for (var offset = -2; offset <= 2; offset++)
        {
            var d = now.AddMonths(offset);
            await EnsureMonthLoaded(d.Year, d.Month);
        }
    }

    /// <summary>
    /// 确保指定月份的数据已加载，如果尚未加载则请求 API
    /// </summary>
    public async Task EnsureMonthLoaded(int year, int month)
    {
        lock (_loadLock)
        {
            if (_loadedMonths.Contains((year, month)))
                return;
            _loadedMonths.Add((year, month));
        }

        var json = await Updater.GetEventsCn(year, month);
        if (string.IsNullOrEmpty(json))
            return;

        CnApiResponse? resp;
        try
        {
            resp = JsonConvert.DeserializeObject<CnApiResponse>(json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "解析活动数据失败");
            return;
        }

        if (resp?.Code != 10000 || resp.Data == null)
            return;

        // 过滤掉 type == 1（版本类）的事件
        var events = resp.Data.Where(e => e.Type != 1).OrderBy(e => e.BeginTime).ToArray();
        if (events.Length == 0)
            return;

        lock (_loadLock)
        {
            // 将当前 Events 转为可修改的字典
            var dict = new Dictionary<long, ParsedEvent[]>(Events);

            // 收集所有已存在的事件 ID，避免跨月活动重复添加
            var existingIds = new HashSet<long>();
            foreach (var arr in dict.Values)
            {
                foreach (var e in arr)
                    existingIds.Add(e.Id);
            }

            foreach (var ev in events)
            {
                // 如果该事件已经被前一个月的数据添加过，跳过
                if (existingIds.Contains(ev.Id))
                    continue;

                var begin = DateTimeOffset.FromUnixTimeSeconds(ev.BeginTime).LocalDateTime;
                var end = DateTimeOffset.FromUnixTimeSeconds(ev.EndTime).LocalDateTime;

                var color = Helper.HexToUint(ev.Color);
                var opacity = Helper.HexToUint(ev.Color, 0.5f);

                var eventDay = new ParsedEvent
                {
                    Id = ev.Id,
                    Name = ev.Name,
                    Begin = begin,
                    End = end,
                    Url = ev.Url,
                    Color = color,
                    Opacity = opacity,
                    Spacing = 17.0f
                };

                existingIds.Add(ev.Id);

                foreach (var (idx, day) in Utils.EachDay(begin, end).Index())
                {
                    eventDay.IsFirst = idx == 0;
                    if (!dict.TryAdd(day.Ticks, [eventDay]))
                    {
                        var entries = dict[day.Ticks];

                        if (eventDay.IsFirst)
                        {
                            while (eventDay.Spacing < 80.0f)
                            {
                                if (entries.All(e => (int)e.Spacing != (int)eventDay.Spacing))
                                    break;

                                eventDay.Spacing += 10.0f;
                            }
                        }

                        dict[day.Ticks] = entries.Append(eventDay).ToArray();
                    }
                }
            }

            Events = dict.ToFrozenDictionary();
        }

        ServerBar.Refresh();
    }

    private void DrawUi() => WindowSystem.Draw();
    public void OpenMain() => MainWindow.Toggle();
    public void OpenConfig() => ConfigWindow.Toggle();
}

public class CnApiResponse
{
    [JsonProperty("code")]
    public int Code;

    [JsonProperty("msg")]
    public string Msg = "";

    [JsonProperty("data")]
    public CnEvent[] Data = [];
}

public class CnEvent
{
    [JsonProperty("id")]
    public long Id;

    [JsonProperty("name")]
    public string Name = "";

    [JsonProperty("url")]
    public string Url = "";

    [JsonProperty("begin_time")]
    public long BeginTime;

    [JsonProperty("end_time")]
    public long EndTime;

    [JsonProperty("color")]
    public string Color = "";

    [JsonProperty("type")]
    public int Type;

    [JsonProperty("weight")]
    public int Weight;

    [JsonProperty("daoyu_sw")]
    public int DaoyuSw;

    [JsonProperty("banner_url")]
    public string? BannerUrl;

    public CnEvent() {}
}

public struct ParsedEvent
{
    public long Id;

    public string Name = "";
    public DateTime Begin = DateTime.UnixEpoch;
    public DateTime End = DateTime.UnixEpoch;
    public string Url = "";
    public uint Color = 0;
    public uint Opacity = 0;
    public float Spacing = 0;

    public bool IsFirst = false;

    public ParsedEvent() {}
}
