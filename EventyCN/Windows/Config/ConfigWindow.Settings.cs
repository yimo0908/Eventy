using Dalamud.Interface.Utility;

namespace EventyCN.Windows.Config;

public partial class ConfigWindow
{
    private void Settings()
    {
        using var tabItem = ImRaii.TabItem("设置");
        if (!tabItem.Success)
            return;

        var changed = false;

        changed |= ImGui.Checkbox("显示服务器栏", ref Plugin.Configuration.ShowDtrEntry);
        changed |= ImGui.Checkbox("使用简短版本", ref Plugin.Configuration.UseShortVersion);
        changed |= ImGui.Checkbox("无活动时隐藏", ref Plugin.Configuration.HideForZeroEvents);

        if (changed)
        {
            Plugin.Configuration.Save();
            Plugin.ServerBar.Refresh();
        }
    }
}
