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

        // A "/augs" output line, e.g. "Duration: 3". The label MUST be at the very start of the
        // line (after an optional chat timestamp like "[12:34:56] "), which is only ever true of
        // our own "/augs" output. When another player copies their augs into a chat channel it
        // arrives wrapped — 'Someone says, "Creature: 10, ..."' / '[General] Someone says, "..."'
        // / 'Someone tells you, "..."' — so the label is no longer at the start and is ignored.
        // Without this anchor the old \b match scraped those pasted counts as our own.
        private static readonly Regex LineRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?(Creature|Item|Life|War|Void|Duration|Specialization|Melee|Missile):\s*([\d,]+)\b");

        // The decoration the server prints around the block — a rule and the section title. There's
        // nothing to parse in these; they're recognised only so they can be suppressed along with
        // the counts, which is why they're claimed solely while a reply we asked for is arriving.
        // A bare rule is far too generic a shape to touch at any other time.
        private static readonly Regex ChromeRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?(?:-{3,}|Advanced Augmentation Levels:|Use /aug info for per-level effect details\.)\s*$",
            RegexOptions.IgnoreCase);

        // When we last issued "/augs" (UtcNow). Drives the throttle below. Set on login too (see
        // PluginCore), so the tab won't immediately re-pull if you open it right after logging in.
        private static DateTime LastRefresh = DateTime.MinValue;

        // Minimum spacing between auto-refreshes. The Conquest augs tab re-pulls while it's on
        // screen (see RefreshIfStale, called each tick from UpdateConquestAugmentations) — but no
        // more often than this, so it never spams "/augs". The manual Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

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

        // Zero the previous character's aug counts on login (called from PluginCore.Init). Refresh()
        // also runs on login and repopulates from "/augs", but it updates counts in place without
        // zeroing first — so an aug the new character has at 0 (and which "/augs" may omit) would
        // otherwise keep the prior character's count.
        public static void Init()
        {
            foreach (var a in All) a.Count = 0;
            LastRefresh = DateTime.MinValue;
            Request.Clear();
        }

        // Marks the window in which a reply to our own "/augs" is expected, so the chat handler
        // can suppress it without touching a "/augs" you typed yourself.
        private static readonly ChatRequest Request = new ChatRequest();

        // Ask the server to reprint the aug block so we can reparse it. Only meaningful on
        // Conquest — the only server with these augs. Returns true when the command actually went
        // out, which the view uses to acknowledge it on the Refresh button.
        public static bool Refresh()
        {
            if (!Server.IsConquest) return false;

            LastRefresh = DateTime.UtcNow;
            Request.Sent();
            Util.Command("/augs");

            return true;
        }

        // Refresh only if it's been at least RefreshThrottle since the last pull. The view calls
        // this every tick while the augs tab is visible, so coming back to the tab shows current
        // aug counts on its own — immediately if it's been a while, and at most once per throttle
        // window while you sit on it — without a manual Refresh and without hammering the server.
        public static bool RefreshIfStale()
        {
            if (!Server.IsConquest) return false;
            if (DateTime.UtcNow - LastRefresh < RefreshThrottle) return false;

            return Refresh();
        }

        // True when this chat line is a "/augs" aug line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
                && (LineRegex.IsMatch(text) || (Request.Awaiting && ChromeRegex.IsMatch(text)));
        }

        // Forwarded from PluginCore's chat handler: parse one aug line and store its count.
        // Returns true when the line answers a "/augs" the plugin issued, which is what makes it
        // eligible for suppression.
        public static bool NoteChat(string text)
        {
            if (text == null) return false;

            // Only reached behind Matches, so a line that isn't a count is the block's decoration:
            // nothing to store, but still ours to suppress.
            Match m = LineRegex.Match(text);
            if (!m.Success) return Request.Awaiting;

            ConquestAugmentation aug = Get(m.Groups[1].Value);
            if (aug != null && int.TryParse(m.Groups[2].Value.Replace(",", ""), out int count)) { aug.Count = count; }

            return Request.Awaiting;
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

        // Base stat_Mod_Val of the top-tier spells the Life aug effects land on, read out of the
        // Conquest world DB (ace_world.spell). The server adds/subtracts raw MULTIPLIER POINTS; the
        // three percentages below divide through by these bases so they read as felt effect — "how
        // much less damage / more regen do I actually get" — rather than as multiplier points, which
        // are close to uninterpretable in a tooltip.
        //   Incantation of <element> Protection Self       0.32  damage multiplier, i.e. you take 32%
        //   Incantation of Regeneration Self               2.45  (Rejuvenation and Mana Renewal
        //                                                         Self are 2.45 as well)
        //   <element> Vulnerability Other VI               2.50  damage multiplier on the target
        //
        // The protection and regen bases are the level-VIII (Incantation) values. Lower spell tiers
        // have smaller bases — Regeneration Self I is 1.1 — so anyone buffed below Incantation gets
        // MORE relative benefit than shown. These are the conservative endgame numbers.
        //
        // The vuln base is the level-VI value, corroborated twice: Conquest-ACE pins its rend cap to
        // it (WorldObject_Weapon.MaxRendingMod = 2.5f, commented "equivalent to level 6 vuln") and
        // the community wiki gives Vulnerability Other VI as +150% damage taken. If Conquest's top
        // vuln is an Incantation tier with a bigger base, VulnPercent overstates and this constant
        // is the only thing to change.
        //
        // The protection cap and the protection base being the same 0.32 is not a coincidence: the
        // aug subtracts from the multiplier, so at the cap it reaches 0.00 and nullifies that damage
        // type outright. ResistPercent therefore asymptotes to 100%.
        private const double BaseProtection = 0.32;
        private const double BaseRegen = 2.45;
        private const double BaseVuln = 2.50;

        // Damage-reduction gained on Protection Self, as a percentage of damage that used to get
        // through. Mirrors Conquest-ACE EnchantmentManager.GetLifeAugProtectRating: linear 0.3%/aug
        // for the first 10, then exponential decay onto the remaining headroom, clamped at 32.
        //
        // Those four constants are PropertyManager reads (life_aug_prot_*), so this mirrors the
        // server's defaults rather than a guarantee — a retuned shard drifts from it silently. The
        // shape is confirmed against the author's own worked examples at EnchantmentManager.cs:429
        // (400 augs 24%, 600 augs 28%, 1000+ 32%), which are raw points, i.e. pre-division here.
        private double ResistPercent()
        {
            const double MaxBonus = 0.32, LinearRate = 0.003, Tuning = 0.0034597;
            const int LinearCap = 10;

            if (Count <= 0) return 0.0;

            double bonus = Count <= LinearCap
                ? Count * LinearRate
                : (LinearCap * LinearRate)
                  + (MaxBonus - LinearCap * LinearRate) * (1.0 - Math.Pow(1.0 - Tuning, Count - LinearCap));

            return Math.Min(bonus, MaxBonus) * 100.0;
        }

        // Extra damage your vulnerabilities amplify, as a percentage over an unaugmented vuln.
        // EnchantmentManager.BuildEntry, MagicSchool.LifeMagic, HARMFUL, same StatModKeys 64-70 the
        // protections use: the aug adds Count * 0.01 onto the vuln's damage multiplier.
        //
        // Nothing like the protection curve on those same keys — this side is flat, linear and
        // uncapped, with neither the 10-aug linear phase nor the exponential decay, so it outruns
        // every other Life effect at high counts (100 augs is +1.0 multiplier points here against
        // the protection side's +0.06). Cast at a target, so unlike the rest of the Life line this
        // one is not self-targeted and applies to whatever you're fighting.
        private double VulnPercent()
        {
            return ResistPercent();
        }

        // Extra health/stamina/mana regeneration, as a percentage over having the top self buff up
        // unaugmented. EnchantmentManager.BuildEntry adds Count * 0.10 onto the spell's multiplier,
        // which GetRegenerationMod multiplies into the tick at Creature_Vitals.cs:144 — so the aug
        // takes 2.45 to 2.45 + 0.1N. Works out to a flat ~4.08%/aug, and it does not taper: there is
        // no cap anywhere on this path (the server even carries a "// cap rate?" TODO above the
        // line). Self-cast only — the Other variants of these spells get no aug bonus at all.
        private double RegenPercent()
        {
            if (Count <= 0) return 0.0;

            return Count * 0.10;
        }

        // Short, human-readable description of this aug's effect at its current Count. Numbers come
        // from the Conquest-ACE `live` source, not the in-game aug text. Kept to one short line —
        // the Conquest tab renders these in a narrow column. Shown even at Count 0 so players can
        // preview each aug.
        public string Effect()
        {
            switch (Name)
            {
                // EnchantmentManager.BuildEntry, MagicSchool.CreatureEnchantment: beneficial and
                // self-targeted adds Count to the spell's StatModValue, harmful subtracts it — so
                // buffs and debuffs alike land Count points harder. Fellowship spells are excluded.
                case "Creature":       return $"+{Count} to creature buffs, -{Count} to debuffs";
                case "Item":           return $"+{Count}% attack/melee, +{Count * 0.5:0.#} blood/spirit, +{Count} AL";

                // EnchantmentManager.BuildEntry, MagicSchool.LifeMagic, beneficial + self-targeted:
                //   StatModKey 64-70 (Protections)                 -> -= ResistPercent, multiplicative
                //   everything else (Regeneration/Rejuvenation/    -> += Count * 0.10, added onto a
                //     Mana Renewal)                                    MULTIPLIER
                // ...and on the harmful side, StatModKey 64-70 (Vulnerabilities) -> += Count * 0.01.
                // StatModKey 0 (BodyArmorValue, i.e. Armor Self) takes a flat += Count of AL, left
                // off the line: the Item aug already advertises AL, and a point per aug is the least
                // interesting thing Life does.
                //
                // Surges take that same += Count * 0.10, but land on PropertyInt ratings that
                // GetAdditiveMod casts with (int) per enchantment — the fraction is thrown away, so
                // only whole points ever apply, one per 10 augs. Integer-divided here to match
                // rather than printing a 2.5 the game will never hand out.
                //
                // prot / vuln / regen are all felt effect against the matching unaugmented spell
                // (see the Base* constants), so they read as comparable percentages; surge rating is
                // a flat count and needs no baseline. The rending bonus is left off — it needs no
                // spell cast and only pays out on a rending weapon, so it doesn't belong on the
                // same line.
                case "Life":           return $"+{ResistPercent():0.##}% prot, +{VulnPercent():0.##}% vuln, +{RegenPercent():0.##} regen, +{Count / 10} surge rating";
                case "War":            return $"+{Count * 2}% war magic potency";
                case "Void":           return $"+{Count * 1.5:0.##}% void magic potency";
                // DamageEvent: a flat +Count onto BaseDamage, plus 1.5% crit damage per aug
                // (luminanceAugmentCritDamageMultiplier, hardcoded 0.015). Both are dropped in PvP
                // when pvp_disable_custom_augs is set. The PvP-only evasion reduction is omitted.
                case "Melee":          return $"+{Count} melee damage, +{Count * 1.5:0.#}% crit damage";
                case "Missile":        return $"+{Count} missile damage, +{Count * 1.5:0.#}% crit damage";
                case "Duration":       return $"+{Count * 5}% spell duration";
                case "Specialization": return $"+{Count} skill spec cap (now {70 + Count})";
                default:               return "";
            }
        }
    }
}
