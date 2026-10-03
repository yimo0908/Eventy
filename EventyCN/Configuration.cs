using Dalamud.Configuration;

namespace EventyCN;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public bool ShowDtrEntry = true;
    public bool UseShortVersion = false;
    public bool HideForZeroEvents = false;

    public bool ShowCompletedEvents = false;
    public HashSet<long> CompletedEvents = [];

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
