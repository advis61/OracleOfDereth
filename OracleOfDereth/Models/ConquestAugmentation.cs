using Decal.Adapter;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's "advanced augmentation" levels. These live in the 9000+ custom
    // PropertyInt64 range server-side (e.g. LumAugDurationCount = 9016) and are never networked
    // to the stock client, so they can't be read via GetCharProperty. The only client-visible
    // source is the "/augs" command, which prints one "Label: N" line per aug. This class issues
    // that command, parses the lines, and stores each aug's count separately. Refreshed on login
    // (Conquest only), from the Conquest tab's Refresh button, and whenever the player runs
    // "/augs" themselves. TargetSpell.Duration() reads DurationCount for the void-DoT scaling.
    public class ConquestAugmentation
    {
        public string Name { get; }
        public int Count { get; set; }

        private ConquestAugmentation(string name) { Name = name; }

        // Registry, in "/augs" output order.
        public static readonly List<ConquestAugmentation> All = new List<ConquestAugmentation>
        {
            new ConquestAugmentation("Creature"),
            new ConquestAugmentation("Item"),
            new ConquestAugmentation("Life"),
            new ConquestAugmentation("War"),
            new ConquestAugmentation("Void"),
            new ConquestAugmentation("Duration"),
            new ConquestAugmentation("Specialization"),
            new ConquestAugmentation("Melee"),
            new ConquestAugmentation("Missile"),
        };

        // A "/augs" output line, e.g. "Duration: 3" (the label set keeps this from matching
        // unrelated chat; it tolerates a leading chat timestamp).
        private static readonly Regex LineRegex = new Regex(
            @"\b(Creature|Item|Life|War|Void|Duration|Specialization|Melee|Missile):\s*([\d,]+)\b");

        public static ConquestAugmentation Get(string name) => All.FirstOrDefault(a => a.Name == name);

        // Spell-duration luminance aug count; each adds +5% to void DoT duration.
        public static int DurationCount => Get("Duration")?.Count ?? 0;

        // Sum of every advanced aug level.
        public static int Total => All.Sum(a => a.Count);

        // Ask the server to reprint the aug block so we can reparse it. Only meaningful on
        // Conquest — the only server with these augs.
        public static void Refresh()
        {
            if (CoreManager.Current.CharacterFilter.Server != "Conquest") return;
            Util.Command("/augs");
        }

        // True when this chat line is a "/augs" aug line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null
                && CoreManager.Current.CharacterFilter.Server == "Conquest"
                && LineRegex.IsMatch(text);
        }

        // Forwarded from PluginCore's chat handler: parse one aug line and store its count.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            Match m = LineRegex.Match(text);
            if (!m.Success) return;

            ConquestAugmentation aug = Get(m.Groups[1].Value);
            if (aug != null && int.TryParse(m.Groups[2].Value.Replace(",", ""), out int count)) { aug.Count = count; }
        }
    }
}
