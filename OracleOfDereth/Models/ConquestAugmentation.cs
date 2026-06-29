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

        // Whether "/augs" has been issued yet. Lets the Custom Augs tab lazy-refresh the first
        // time it's shown instead of running on login (mirrors QuestFlag.MyQuestsRan / ConquestBank).
        public static bool Ran = false;

        public static ConquestAugmentation Get(string name) => All.FirstOrDefault(a => a.Name == name);

        // Spell-duration luminance aug count; each adds +5% to void DoT duration.
        public static int DurationCount => Get("Duration")?.Count ?? 0;

        // Sum of every advanced aug level.
        public static int Total => All.Sum(a => a.Count);

        // Ask the server to reprint the aug block so we can reparse it. Only meaningful on
        // Conquest — the only server with these augs.
        public static void Refresh()
        {
            if (!Server.IsConquest) return;
            Ran = true;
            Util.Command("/augs");
        }

        // True when this chat line is a "/augs" aug line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
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

        // Human-readable per-aug effect for the current Count (the aug's level; character level is
        // not a factor). Derived from the Conquest-ACE server source:
        //  - Creature/Item/Life add +Count to that school's spell effectiveness.
        //  - War/Void raise stacking priority for that school (shown simply as "+Count ... spells").
        //  - Duration is +5% spell duration per level (duration *= 1 + Count*0.05).
        //  - Melee/Missile add +Count flat weapon damage.
        //  - Specialization has no implemented gameplay effect in the server source yet.
        public string Effect()
        {
            if (Count <= 0) return "";

            switch (Name)
            {
                case "Creature": return $"+{Count} to your creature spells";
                case "Item":     return $"+{Count} to your item spells";
                case "Life":     return $"+{Count} to your life spells";
                case "War":      return $"+{Count} to your war spells";
                case "Void":     return $"+{Count} to your void spells";
                case "Duration": return $"+{Count * 5}% spell duration";
                case "Melee":    return $"+{Count} melee damage";
                case "Missile":  return $"+{Count} missile damage";
                default:         return ""; // Specialization: no implemented effect
            }
        }
    }
}
