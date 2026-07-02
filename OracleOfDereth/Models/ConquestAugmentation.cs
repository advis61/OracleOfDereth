using System;
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

        // Flat coin cost to buy one of this aug. Unlike the luminance cost (below), the coin price
        // is per-aug-type and not in the Conquest-ACE source (it's the gem's vendor price, held in
        // the world DB), so these are hand-entered. Creature/Item confirmed; the rest are a 250
        // placeholder until the real vendor values are known.
        public int CoinCost { get; }

        private ConquestAugmentation(string name, int coinCost) { Name = name; CoinCost = coinCost; }

        // Registry, in "/augs" output order.
        public static readonly List<ConquestAugmentation> All = new List<ConquestAugmentation>
        {
            new ConquestAugmentation("Creature", 25),
            new ConquestAugmentation("Item", 100),
            new ConquestAugmentation("Life", 250),
            new ConquestAugmentation("War", 250),
            new ConquestAugmentation("Void", 250),
            new ConquestAugmentation("Duration", 250),
            new ConquestAugmentation("Specialization", 250),
            new ConquestAugmentation("Melee", 250),
            new ConquestAugmentation("Missile", 250),
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

        // Clipboard summary, e.g. "Creature: 10, Item: 5, Missile: 34, Total: 233". Omits zero augs.
        public static string Summary()
        {
            var parts = All.Where(a => a.Count > 0).Select(a => $"{a.Name}: {a.Count}").ToList();
            parts.Add($"Total Augs: {Total}");
            return string.Join(", ", parts);
        }

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

        // Luminance cost of the NEXT purchase of this aug, from Conquest-ACE
        // EmoteManager_AugGems.CalculateTieredAugmentationCost. Same formula for every aug; the only
        // variable is this aug's current Count. Base 2.5M, +5% per step within a tier, tier bases
        // step up at counts 15/30/60 (2.5M -> 6M -> 12M -> 30M). No cap.
        private const double LumBaseCost = 2_500_000;
        private const double LumPercentStep = 0.05;

        public long NextLuminanceCost()
        {
            long idx = Count;
            double tierBase;
            long pos;

            if (idx >= 60)      { tierBase = LumBaseCost * 12.0; pos = idx - 60; }
            else if (idx >= 30) { tierBase = LumBaseCost * 4.8;  pos = idx - 30; }
            else if (idx >= 15) { tierBase = LumBaseCost * 2.4;  pos = idx - 15; }
            else                { tierBase = LumBaseCost;        pos = idx; }

            return (long)(tierBase * (1.0 + pos * LumPercentStep));
        }

        // "25 coins, 2.5M lum" — the price of the next purchase of this aug.
        public string NextCostText()
        {
            return $"{CoinCost} coins, {FormatLum(NextLuminanceCost())} lum";
        }

        // Abbreviated millions: 2,500,000 -> "2.5M", 6,000,000 -> "6M", 4,250,000 -> "4.25M".
        private static string FormatLum(long lum)
        {
            return (lum / 1_000_000.0).ToString("0.##") + "M";
        }

        // Short, human-readable per-aug effect for the current Count (the aug's level):
        //  - Creature: +1 effective spell level per point.
        //  - Item: +1 armor level / attack / melee-D and +0.5 spirit & blood drinker per point.
        //  - Life: resists scale +0.3%/point up to 10, then +0.1%/point; +1 heal/revitalize and
        //    +0.1 surge per point.
        //  - War/Void/Melee/Missile: +1 damage per point.
        //  - Duration: +5% spell duration per point.
        //  - Specialization: raises the 70 specialization-credit cap by +Count.
        public string Effect()
        {
            //if (Count <= 0) return "";

            switch (Name)
            {
                case "Creature":       return $"+{Count} effective level";
                case "Item":           return $"+{Count}% attack/melee, +{Count * 0.5:0.#} blood/spirit drinker";
                case "Life":
                    double resist = Math.Min(Count, 10) * 0.3 + Math.Max(Count - 10, 0) * 0.1;
                    return $"+{resist:0.#}% resist, +{Count} heal/revit, +{Count * 0.1:0.#} surge";
                case "War":            return $"+{Count} damage with war magic";
                case "Void":           return $"+{Count} damage with void magic";
                case "Melee":          return $"+{Count} damage with melee weapons";
                case "Missile":        return $"+{Count} damage with missile weapons";
                case "Duration":       return $"+{Count * 5}% spell duration";
                case "Specialization": return $"+{Count} skill spec cap (now {70 + Count})";
                default:               return "";
            }
        }
    }
}
