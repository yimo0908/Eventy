using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace EventyCN.Windows;

public static class Helper
{
    /// <summary>
    /// An unformatted version for ImGui.TextWrapped
    /// </summary>
    /// <param name="text">text to display</param>
    public static void TextWrapped(string text)
    {
        using (ImRaii.TextWrapPos(0.0f))
            ImGui.TextUnformatted(text);
    }

    /// <summary>
    /// An unformatted version for ImGui.TextWrapped with color
    /// </summary>
    /// <param name="color">color to be used</param>
    /// <param name="text">text to display</param>
    public static void WrappedTextWithColor(Vector4 color, string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            TextWrapped(text);
    }

    private static float Saturate(float f) => f < 0.0f ? 0.0f : f > 1.0f ? 1.0f : f;
    private static uint FloatToUintSat(float val) => (uint) ((Saturate(val) * 255.0f) + 0.5f);

    public static uint Vec4ToUintColor(Vector4 i)
    {
        var o = FloatToUintSat(i.X) << 0;
        o |= FloatToUintSat(i.Y) << 8;
        o |= FloatToUintSat(i.Z) << 16;
        o |= FloatToUintSat(i.W) << 24;

        return o;
    }

    /// <summary>
    /// 将十六进制颜色字符串 (如 "#D98481") 转换为 ImGui 使用的 uint 颜色值
    /// </summary>
    /// <param name="hex">十六进制颜色字符串，如 "#D98481"</param>
    /// <param name="alpha">透明度 0.0~1.0，默认 1.0</param>
    /// <returns>uint 颜色值 (ABGR 格式)</returns>
    public static uint HexToUint(string hex, float alpha = 1.0f)
    {
        if (string.IsNullOrEmpty(hex))
            return 0;

        hex = hex.TrimStart('#');
        if (hex.Length < 6)
            return 0;

        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        var a = (byte)(Saturate(alpha) * 255);

        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }
}
