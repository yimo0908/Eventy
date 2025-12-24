using System.Collections.Frozen;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Eventy.Attributes;
using Eventy.Windows.Config;
using Eventy.Windows.Main;
using Newtonsoft.Json;

namespace Eventy;

public class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static INotificationManager Notification { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;

    public static Configuration Configuration { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("Eventy");
    public ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }

    private readonly PluginCommandManager<Plugin> CommandManager;
    public readonly ServerBar ServerBar;

    public FrozenDictionary<long, ParsedEvent[]> Events = FrozenDictionary<long, ParsedEvent[]>.Empty;

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

    [Command("/eventy")]
    [HelpMessage("Opens the event calender")]
    public void OpenMainCommand(string _, string __)
    {
        MainWindow.Toggle();
    }

    [Command("/eventyconf")]
    [HelpMessage("Opens the event calender")]
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

    private async Task LoadEvents()
    {
        var dict = new Dictionary<long, ParsedEvent[]>();
        try
        {
            var storedJson = "";
            var file = new FileInfo(Path.Combine(PluginInterface.ConfigDirectory.FullName, "events.json"));
            if (file.Exists)
            {
                using var reader = new StreamReader(file.FullName);
                storedJson = reader.ReadToEnd();
            }

            var response = await Updater.GetEvents(0);

            // Check if the response is valid and if it was different to the stored version
            if (response != string.Empty && response != storedJson)
            {
                storedJson = response;

                await using var reader = new StreamWriter(file.FullName);
                await reader.WriteAsync(storedJson);
            }

            var events = JsonConvert.DeserializeObject<Event[]>(storedJson, new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ssZ",
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            })!;

            foreach (var ev in events.OrderBy(ev => ev.Begin))
            {
                // Get colors and put it at the end of queue
                var color = MainWindow.Colors.Dequeue();
                MainWindow.Colors.Enqueue(color);

                var eventDay = new ParsedEvent
                {
                    Id = ev.Id,

                    // Translate English event names to Chinese for display
                    Name = EventTranslator.Translate(ev.Name),
                    Begin = ev.Begin,
                    End = ev.End,
                    Special = ev.Special,
                    IsPvP = ev.IsPvP,
                    Url = ev.Url,
                    Color = color.Normal,
                    Opacity = color.Opacity,

                    Spacing = 20.0f // initial spacing
                };

                foreach (var (day, idx) in Utils.EachDay(ev.Begin, ev.End).WithIndex())
                {
                    eventDay.IsFirst = idx == 0;
                    if (!dict.TryAdd(day.Ticks, [eventDay]))
                    {
                        var entries = dict[day.Ticks];

                        // We have a max of 50.0f spacing, so we wrap back around if we go above it
                        if (eventDay.IsFirst)
                        {
                            // Check if space above is free else set spacing to +10.0f of current
                            foreach (var (entry, iidx) in entries.WithIndex())
                            {
                                var spacing = 20.0f + (10.0f * iidx);
                                if (entry.Spacing > spacing)
                                {
                                    eventDay.Spacing = spacing;
                                    break;
                                }

                                eventDay.Spacing = entry.Spacing + 10.0f;
                            }
                        }
                        dict[day.Ticks] = entries.Append(eventDay).ToArray();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to parse");
        }

        Events = dict.ToFrozenDictionary();
    }

    private void DrawUi() => WindowSystem.Draw();
    public void OpenMain() => MainWindow.Toggle();
    public void OpenConfig() => ConfigWindow.Toggle();
}

public class Event
{
    [JsonProperty("id")]
    public long Id;

    [JsonProperty("name")]
    public string Name = "";

    [JsonProperty("begin")]
    public DateTime Begin;

    [JsonProperty("end")]
    public DateTime End;

    [JsonProperty("special")]
    public bool Special;

    [JsonProperty("url")]
    public string Url = "";

    [JsonProperty("pvp")]
    public bool IsPvP;

    [JsonConstructor]
    public Event() {}
}

public struct ParsedEvent
{
    public long Id;

    public string Name = "";
    public DateTime Begin = DateTime.UnixEpoch;
    public DateTime End = DateTime.UnixEpoch;
    public bool Special = false;
    public bool IsPvP = false;
    public string Url = "";
    public uint Color = 0;
    public uint Opacity = 0;
    public float Spacing = 0;

    public bool IsFirst = false;

    public ParsedEvent() {}
}
