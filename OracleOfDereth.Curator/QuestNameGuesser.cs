using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth.Curator
{
    internal static class QuestNameGuesser
    {
        private static readonly string[] Suffixes =
        {
            "onfirstcompletion", "firstcompletion", "participation", "completedinamonth",
            "collectedinamonth", "completion", "completed", "complete", "monthly",
            "counter", "timer", "wait", "found", "pickup", "turnin"
        };

        private static readonly string[] Words =
        {
            "achievement", "redistribution", "elemental", "legendary", "treasure",
            "invasive", "knowledge", "contract", "attributes", "facility", "society",
            "killtask", "species", "greater", "lesser", "slayer", "dragon", "golem",
            "anekshay", "sawato", "master", "reward", "event", "quest", "acid",
            "hope", "kill", "task", "first", "gem", "map", "red", "drag", "of", "lc"
        };

        private static readonly Dictionary<string, string> Display =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["anekshay"] = "A'nekshay",
                ["killtask"] = "Kill Task",
                ["drag"] = "Dragon",
                ["lc"] = "LC",
                ["of"] = "of"
            };

        public static bool IsUsefulName(string value)
        {
            string text = (value ?? "").Trim();
            return text.Length > 2 && text != "-1" &&
                !string.Equals(text, "quest timer", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(text, "timer", StringComparison.OrdinalIgnoreCase);
        }

        public static string Guess(string flag)
        {
            string remaining = new string((flag ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            string suffix = Suffixes.FirstOrDefault(s => remaining.EndsWith(s, StringComparison.Ordinal));
            if (suffix != null) remaining = remaining.Substring(0, remaining.Length - suffix.Length);

            var parts = new List<string>();
            while (remaining.Length > 0)
            {
                string word = Words.OrderByDescending(w => w.Length)
                    .FirstOrDefault(w => remaining.StartsWith(w, StringComparison.Ordinal));
                if (word == null) return "";
                parts.Add(Display.TryGetValue(word, out string display)
                    ? display
                    : char.ToUpperInvariant(word[0]) + word.Substring(1));
                remaining = remaining.Substring(word.Length);
            }
            return parts.Count >= 2 ? string.Join(" ", parts) : "";
        }
    }
}
