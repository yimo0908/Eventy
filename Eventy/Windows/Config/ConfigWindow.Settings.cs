using Dalamud.Interface.Utility;

namespace Eventy.Windows.Config;

public partial class ConfigWindow
{
    private void Settings()
    {
        using var tabItem = ImRaii.TabItem("设置");
        if (!tabItem.Success)
            return;

        var changed = false;

        changed |= ImGui.Checkbox("显示在服务器信息栏", ref Plugin.Configuration.ShowDtrEntry);
        changed |= ImGui.Checkbox("使用简洁显示版本", ref Plugin.Configuration.UseShortVersion);
        changed |= ImGui.Checkbox("无事件时隐藏", ref Plugin.Configuration.HideForZeroEvents);
        changed |= ImGui.Checkbox("显示PVP赛季", ref Plugin.Configuration.ShowPvP);

        ImGuiHelpers.ScaledDummy(20.0f);

        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 5.0f);
        using var combo = ImRaii.Combo("Subdomain To Use", Plugin.Configuration.Subdomain.ToName());
        if (combo.Success)
        {
            foreach (var sub in Enum.GetValues<Subdomain>())
            {
                if (ImGui.Selectable(sub.ToName(), sub == Plugin.Configuration.Subdomain))
                {
                    changed = true;
                    Plugin.Configuration.Subdomain = sub;
                }
            }
        }

        if (changed)
        {
            Plugin.Configuration.Save();
            Plugin.ServerBar.Refresh();
        }
    }
}
