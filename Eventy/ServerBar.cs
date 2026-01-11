using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace Eventy;

public class ServerBar
{
    private readonly Plugin Plugin;
    private readonly IDtrBarEntry? DtrEntry;

    private long LastRefresh = Environment.TickCount64;

    public ServerBar(Plugin plugin)
    {
        Plugin = plugin;

        if (Plugin.DtrBar.Get("Eventy") is not { } entry)
            return;

        DtrEntry = entry;

        DtrEntry.Text = "We all like events...";
        DtrEntry.Shown = false;
        DtrEntry.OnClick += OnClick;

        Plugin.Framework.Update += UpdateDtrBar;
    }

    public void Dispose()
    {
        if (DtrEntry == null)
            return;

        Plugin.Framework.Update -= UpdateDtrBar;
        DtrEntry.OnClick -= OnClick;
        DtrEntry.Remove();
    }

    public void Refresh()
    {
        LastRefresh = 0;
    }

    private void UpdateDtrBar(IFramework framework)
    {
        if (!Plugin.Configuration.ShowDtrEntry)
        {
            UpdateVisibility(false);
            return;
        }

        // Only refresh every 5s
        if (Environment.TickCount64 - 5000 < LastRefresh)
            return;
        LastRefresh = Environment.TickCount64;

        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        var ok = Plugin.Events.TryGetValue(date.Ticks, out var events);
        events = events?.Where(ev => !Plugin.Configuration.CompletedEvents.Contains(ev.Id) || Plugin.Configuration.ShowCompletedEvents).Where(ev => !ev.IsPvP || Plugin.Configuration.ShowPvP).ToArray();

        if (Plugin.Configuration.HideForZeroEvents && (!ok || events?.Length == 0))
        {
            UpdateVisibility(false);
            return;
        }

        UpdateVisibility(true);
        UpdateBarString(ok, events);
    }

    private void UpdateBarString(bool ok, ParsedEvent[]? events)
    {
        var text = $"{(!Plugin.Configuration.UseShortVersion ? "No Events" : $"{(char) SeIconChar.Clock} 0")}";
        DtrEntry!.Tooltip = null;

        if (ok)
        {
            text = $"{(!Plugin.Configuration.UseShortVersion ? "Ongoing Events: " : (char) SeIconChar.Clock)} {events!.Length}";

            var tooltip = new SeStringBuilder();
            foreach (var ev in events)
            {
                tooltip.AddText($"{ev.Name}\n");
                tooltip.AddUiForeground(ev.Special ? $"{ev.Begin.AddHours(8):f} - {ev.End.AddHours(1):f}" : $"{ev.Begin.AddHours(8):D} - {ev.End.AddHours(1):D}", 58);
                tooltip.AddText("\n");
            }

            DtrEntry!.Tooltip = tooltip.BuiltString;
        }

        DtrEntry!.Text = text;
    }

    private void UpdateVisibility(bool shown)
    {
        if (DtrEntry!.Shown != shown)
            DtrEntry!.Shown = shown;
    }

    private void OnClick(DtrInteractionEvent data)
    {
        Plugin.OpenMain();
    }
}
