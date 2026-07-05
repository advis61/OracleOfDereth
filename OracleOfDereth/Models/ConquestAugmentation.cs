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

        // Per-aug luminance pricing inputs, matching Conquest-ACE's aug-gem emote record:
        //   LumBase    <- emote.Amount        (the tier-1 base luminance cost, i.e. cost at count 0)
        //   LumPercent <- emote.Percent / 100 (the per-level step within a tier)
        // LumBase = emote.Amount and LumPercent = emote.Percent/100, both per-gem (they vary — a
        // single shared percent never fit all augs). Percents are dev-provided.
        //   War/Void/Melee/Missile 0.425%  Duration 0.625%  Creature 0.3825%
        //   Item 0.6825%  Life 0.95%  Specialization 2%
        public double LumBase { get; }
        public double LumPercent { get; }

        private ConquestAugmentation(string name, int coinCost, double lumBase, double lumPercent)
        {
            Name = name;
            CoinCost = coinCost;
            LumBase = lumBase;
            LumPercent = lumPercent;
        }

        // Registry, in "/augs" output order.  Args: coinCost, lumBase (emote.Amount), lumPercent.
        public static readonly List<ConquestAugmentation> All = new List<ConquestAugmentation>
        {
            new ConquestAugmentation("Creature", 25, 2_500_000, 0.003825),   // confirmed: count 25 = 6,229,500
            new ConquestAugmentation("Item", 100, 3_000_000, 0.006825),      // confirmed: count 5 = 3,102,375
            new ConquestAugmentation("Life", 75, 2_500_000, 0.0095),         // dev-confirmed 0.95%
            new ConquestAugmentation("War", 50, 1_750_000, 0.00425),         // dev-provided 0.425%
            new ConquestAugmentation("Void", 50, 1_800_000, 0.00425),        // dev-provided 0.425%
            new ConquestAugmentation("Duration", 30, 1_400_000, 0.00625),    // dev-provided 0.625%
            new ConquestAugmentation("Specialization", 125, 3_000_000, 0.02),    // dev-provided 2%
            new ConquestAugmentation("Melee", 50, 1_750_000, 0.00425),       // confirmed: count 1 = 1,757,438
            new ConquestAugmentation("Missile", 50, 1_750_000, 0.00425),     // assumed = Melee (same base), unconfirmed
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

        // Luminance cost of the NEXT purchase of this aug, mirroring the Conquest-ACE `live` branch
        // EmoteManager_AugGems.CalculateTieredAugmentationCost. The tier structure (boundaries and
        // multipliers) is hardcoded server-side and shared by all augs; only LumBase (emote.Amount)
        // and LumPercent (emote.Percent) vary per gem. Cost is linear within a tier:
        //   tierBase * (1 + positionInTier * LumPercent)
        // Tier bases (as multiples of LumBase): 1x (0-14), 2.4x (15-29), 4.8x (30-59),
        // 12x (60-64), 30x (65+). The 65+ tier is the live-branch addition. No cap. Server casts
        // the total to long, so we truncate too.
        public long NextLuminanceCost()
        {
            long idx = Count;
            double tierBase;
            long pos;

            if (idx >= 65)      { tierBase = LumBase * 30.0; pos = idx - 65; }
            else if (idx >= 60) { tierBase = LumBase * 12.0; pos = idx - 60; }
            else if (idx >= 30) { tierBase = LumBase * 4.8;  pos = idx - 30; }
            else if (idx >= 15) { tierBase = LumBase * 2.4;  pos = idx - 15; }
            else                { tierBase = LumBase;        pos = idx; }

            return (long)(tierBase * (1.0 + (pos * LumPercent)));
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

        // Short, human-readable description of this aug's effect at its current Count, from the
        // in-game aug descriptions. Shown even at Count 0 so players can preview each aug.
        public string Effect()
        {
            switch (Name)
            {
                case "Creature":       return $"+{Count} effective level";
                case "Item":           return $"+{Count}% attack/melee, +{Count * 0.5:0.#} blood/spirit drinker";
                case "Life":
                    double resist = Math.Min(Count, 10) * 0.3 + Math.Max(Count - 10, 0) * 0.1;
                    return $"+{resist:0.#}% resist, +{Count} heal/revit, +{Count * 0.1:0.#} surge";
                case "War":            return $"+{Count * 2}% war magic potency";
                case "Void":           return $"+{Count * 2.5:0.#}% void magic potency";
                case "Melee":          return $"+{Count} damage with melee weapons";
                case "Missile":        return $"+{Count} damage with missile weapons";
                case "Duration":       return $"+{Count * 5}% spell duration";
                case "Specialization": return $"+{Count} skill spec cap (now {70 + Count})";
                default:               return "";
            }
        }
    }
}
