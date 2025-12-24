using System.Text.RegularExpressions;

namespace Eventy
{
    public static class EventTranslator
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Heavensturn", "降神节" },
            { "Valentione's Day", "恋人节" },
            { "Little Ladies' Day", "女儿节" },
            { "Hatching-tide", "彩蛋狩猎" },
            { "The Make It Rain Campaign", "金蝶嘉年华" },
            { "Moonfire Faire", "红莲节" },
            { "The Rising", "新生庆典" },
            { "All Saints Wake", "守护天节" },
            { "Starlight Celebration", "星芒节" },
            { "The Moogle Treasure", "莫古莫古★大收集" },
            { "Maintenance", "维护" },
            // Add more known mappings here
        };

        public static string Translate(string english)
        {
            if (string.IsNullOrWhiteSpace(english)) return english;

            var trimmed = english.Trim();

            // Handle patterns like "PVP Series 9" or "PVP Series 10"
            var m = Regex.Match(trimmed, @"\bPVP\s*Series\s*(\d+)\b", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return $"PVP第{m.Groups[1].Value}赛季";
            }

            // Handle patterns like "Liveletter 89" or "Live Letter 90" (allow optional space)
            m = Regex.Match(trimmed, @"\bLive\s*letter\s*(\d+)\b", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return $"第{m.Groups[1].Value}回制作人来信";
            }

            if (Map.TryGetValue(trimmed, out var zh)) return zh;

            // Handle cases like "Event Name (Region)"
            var idx = trimmed.IndexOf('(');
            if (idx > 0)
            {
                var key = trimmed.Substring(0, idx).Trim();
                if (Map.TryGetValue(key, out zh)) return zh;
            }

            // Try substring match for known keys
            foreach (var kv in Map)
            {
                if (trimmed.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }

            // Not found, return original English name
            return english;
        }
    }
}
