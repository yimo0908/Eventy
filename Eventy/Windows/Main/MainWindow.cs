using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace Eventy.Windows.Main;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private static readonly string[] DayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    private static readonly string[] MonthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    private static readonly int[] NumDaysPerMonth = [31,28,31,30,31,30,31,31,30,31,30,31];

    private static float LongestMonthWidth;
    private static readonly float[] MonthWidths = new float[12];
    private DateTime CurrentDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    private readonly uint DarkGrey;
    public readonly Queue<(uint Normal, uint Opacity)> Colors;

    public MainWindow(Plugin plugin) : base("Eventy##Eventy")
    {
        Plugin = plugin;

        Flags = ImGuiWindowFlags.AlwaysAutoResize;

        Colors = new Queue<(uint Normal, uint Opacity)>(
        [
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedGreen), Helper.Vec4ToUintColor(ImGuiColors.ParsedGreen with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedBlue), Helper.Vec4ToUintColor(ImGuiColors.ParsedBlue with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedPurple), Helper.Vec4ToUintColor(ImGuiColors.ParsedPurple with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedOrange), Helper.Vec4ToUintColor(ImGuiColors.ParsedOrange with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedPink), Helper.Vec4ToUintColor(ImGuiColors.ParsedPink with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.ParsedGold), Helper.Vec4ToUintColor(ImGuiColors.ParsedGold with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.DPSRed), Helper.Vec4ToUintColor(ImGuiColors.DPSRed with {W = 0.5f})),
            (Helper.Vec4ToUintColor(ImGuiColors.DalamudYellow), Helper.Vec4ToUintColor(ImGuiColors.DalamudYellow with {W = 0.5f})),
        ]);

        DarkGrey = Helper.Vec4ToUintColor(ImGuiColors.DalamudGrey3);

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            Click = _ => { Plugin.OpenConfig(); }
        });
    }

    public void Dispose() { }

    private Vector2 FieldSize = new(60, 60);
    public override void Draw()
    {
        var drawlist = ImGui.GetWindowDrawList();
        FieldSize = new Vector2(60, 60) * ImGuiHelpers.GlobalScale;
        var sampleWidth = ImGui.CalcTextSize("00").X;

        if (LongestMonthWidth == 0.0f)
        {
            for (var i = 0; i < 12; i++)
            {
                var mw = ImGui.CalcTextSize(MonthNames[i]).X;

                MonthWidths[i] = mw;
                LongestMonthWidth = Math.Max(LongestMonthWidth, mw);
            }
        }

        var style = ImGui.GetStyle();
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.One);

        const string arrowLeft = "<";
        const string arrowRight = ">";
        var arrowLeftWidth = ImGui.CalcTextSize(arrowLeft).X;
        var arrowRightWidth = ImGui.CalcTextSize(arrowRight).X;

        var yearString = $"{CurrentDate.Year}";
        var yearPartWidth = arrowLeftWidth + arrowRightWidth + ImGui.CalcTextSize(yearString).X;

        using (ImRaii.PushId(1234))
        {
            if (ImGui.SmallButton(arrowLeft))
                CurrentDate = CurrentDate.AddMonths(-1);

            ImGui.SameLine();

            var color = ImGui.GetColorU32(style.Colors[(int)ImGuiCol.Text]);
            var monthWidth = MonthWidths[CurrentDate.Month - 1];
            var pos = ImGui.GetCursorScreenPos();
            pos = pos with { X = pos.X + ((LongestMonthWidth - monthWidth) * 0.5f) };

            drawlist.AddText(pos, color, MonthNames[CurrentDate.Month - 1]);

            ImGui.SameLine(0, LongestMonthWidth + (style.ItemSpacing.X * 2));

            if (ImGui.SmallButton(arrowRight))
                CurrentDate = CurrentDate.AddMonths(1);
        }

        const string todayString = "Today";
        var todayWidth = ImGui.CalcTextSize(todayString).X + ImGui.GetStyle().FramePadding.X * 2;
        var centerOffset = (ImGui.GetWindowWidth() - todayWidth) * 0.5f;
        ImGui.SameLine(centerOffset);
        if (ImGui.SmallButton(todayString))
            CurrentDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

        ImGui.SameLine(ImGui.GetWindowWidth() - yearPartWidth - style.WindowPadding.X - (style.ItemSpacing.X * 3.0f));

        using (ImRaii.PushId(1235))
        {
            if (ImGui.SmallButton(arrowLeft))
                CurrentDate = CurrentDate.AddYears(-1);

            ImGui.SameLine();
            ImGui.Text($"{CurrentDate.Year}");
            ImGui.SameLine();

            if (ImGui.SmallButton(arrowRight))
                CurrentDate = CurrentDate.AddYears(1);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var maxDayOfCurMonth = NumDaysPerMonth[CurrentDate.Month - 1];
        if (maxDayOfCurMonth == 28)
        {
            var year = CurrentDate.Year;
            var bis = ((year % 4) == 0) && ((year % 100) != 0 || (year % 400) == 0);
            if (bis)
                maxDayOfCurMonth = 29;
        }

        var currentDay = DateTime.Now;
        var dayOfWeek = (int)new DateTime(CurrentDate.Year, CurrentDate.Month, 1).DayOfWeek;
        for (var dw = 0; dw < 7; dw++)
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0,0)))
            using (ImRaii.Group())
            {
                using var textColor = ImRaii.PushColor(ImGuiCol.Text, CalculateTextColor(true), dw == 0);

                ImGui.Text($"{(dw == 0 ? "" : " ")}{DayNames[dw]}");
                if (dw == 0)
                    ImGui.Separator();
                else
                    ImGui.Spacing();

                // Use dayOfWeek for spacing
                var curDay = dw - dayOfWeek;
                for (var row = 0; row < 7; row++)
                {
                    var cday = curDay + (7 * row);
                    if (cday - dw > maxDayOfCurMonth)
                        continue;

                    var lastMonth = cday < 0;
                    var currentMonth = cday >= 0 && cday < maxDayOfCurMonth;
                    var day = currentMonth
                                  ? new DateTime(CurrentDate.Year, CurrentDate.Month, cday + 1)
                                  : new DateTime(CurrentDate.Year, CurrentDate.Month, lastMonth ? 1 : maxDayOfCurMonth).AddDays(cday + 1 - (lastMonth ? 1 : maxDayOfCurMonth));

                    var eventDay = Plugin.Events.TryGetValue(day.Ticks, out var array);

                    var pos = ImGui.GetCursorScreenPos();
                    var isCurrentDay = day.Date == currentDay.Date;
                    CreateSquare(dw, row, drawlist, eventDay, array, currentMonth, isCurrentDay);

                    var text = string.Format(cday < 9 ? " {0}" : "{0}", day.Day);
                    var textWidth = ImGui.CalcTextSize(text).X;
                    var spacing = 5.0f * ImGuiHelpers.GlobalScale;

                    pos = pos with { X = pos.X + spacing + ((sampleWidth - textWidth) * 0.5f) };
                    var color = Helper.Vec4ToUintColor(dw != 0 ? ImGuiColors.DalamudGrey with {W = currentMonth ? 1 : 0.5f} : CalculateTextColor(currentMonth));
                    drawlist.AddText(pos, color, text);
                }

                if (dw == 0)
                    ImGui.Separator();
            }

            if (dw != 6)
                ImGui.SameLine((FieldSize.X * (dw + 1)) + ImGui.GetStyle().ItemSpacing.X);
        }
    }

    private static Vector4 CalculateTextColor(bool currentMonth)
    {
        var textColor = ImGuiColors.DalamudGrey with {W = currentMonth ? 1 : 0.5f};
        var l = (textColor.X + textColor.Y + textColor.Z) * 0.33334f;
        return new Vector4(l * 2.0f > 1 ? 1 : l * 2.0f, l * .5f, l * .5f, textColor.W);
    }

    private bool CreateSquare(int row, int col, ImDrawListPtr drawList, bool isEvent = false, ParsedEvent[]? events = null, bool currentMonth = true, bool currentDay = false)
    {
        var min = ImGui.GetCursorScreenPos();
        var max = new Vector2(min.X + FieldSize.X, min.Y + FieldSize.Y);
        var size = max - min;

        using var pushedId = ImRaii.PushId($"{row}{col}");
        ImGui.Dummy(size);
        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var hovered = ImGui.IsItemHovered();

        ImGui.SetCursorScreenPos(min);

        var specialDay = new ParsedEvent();
        if (events != null)
            specialDay = events.FirstOrDefault(ev => ev.Special);

        var isSpecial = !string.IsNullOrEmpty(specialDay.Name);
        DrawRect(min, max, isSpecial ? specialDay.Opacity : 0, isSpecial ? specialDay.Color : DarkGrey, drawList);
        if (currentDay)
        {
            var thickness = 3.0f;
            var halfThickness = new Vector2(thickness / 2);
            drawList.AddRect(min + halfThickness, max - halfThickness, Helper.Vec4ToUintColor(ImGuiColors.ParsedOrange), 0.0f, 0, thickness);
        }

        if (isSpecial && hovered)
        {
            using var textColor = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
            ImGui.SetTooltip($"{specialDay.Name}\n{specialDay.Begin.AddHours(8):f} - {specialDay.End.AddHours(1):f}");
        }

        if (isEvent && events != null)
        {
            foreach (var ev in events.Where(ev => !ev.Special).Where(ev => !ev.IsPvP || Plugin.Configuration.ShowPvP))
            {
                var spacing = ev.Spacing * ImGuiHelpers.GlobalScale;
                var lineMin = min with { Y = min.Y + spacing };
                var lineMax = max with { Y = min.Y + spacing + (5.0f * ImGuiHelpers.GlobalScale) };

                drawList.AddRectFilled(lineMin, lineMax, currentMonth ? ev.Color : ev.Opacity);

                ImGui.SetCursorScreenPos(lineMin);
                if (ImGui.InvisibleButton($"##event{ev.Id}", lineMax - lineMin) && ev.Url != "")
                    Utils.OpenUrl(ev.Url.Replace("//eu", $"//{Plugin.Configuration.Subdomain.ToValue()}"));

                if (ImGui.IsItemHovered())
                {
                    using var textColor = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                    ImGui.SetTooltip($"{ev.Name}\n{ev.Begin.AddHours(8):f} - {ev.End.AddHours(1):f}");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
            }
        }

        ImGui.SetCursorScreenPos(min);
        ImGui.Dummy(size);

        return clicked;
    }

    private static void DrawRect(Vector2 min, Vector2 max, uint fillColor, uint borderColor, ImDrawListPtr drawList)
    {
        drawList.AddRectFilled(min, max, fillColor);
        drawList.AddRect(min, max, borderColor);
    }
}
