using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's enlightenment augmentations, printed by the "/enl augs" command as one
    // "<Name>: <count> (<max> max)" line per aug under an "Enlightenment Augmentations:" header.
    // This class issues that command, keeps the lines it prints, and hands them to the Server ->
    // Augs tab for display beneath the advanced augs. Mirrors ConquestAugmentation / ConquestBonus.
    // Conquest-only. Like "/bonus", "/enl augs" is NOT refreshed on login — it lazy-loads the first
    // time the tab is shown, and on the tab's Refresh button.
    //
    // No registry of known augs and no interpretation of the values: rows are whatever the server
    // printed, in the order it printed them, so augs added or recapped server-side show up without
    // a plugin change.
    public class ConquestEnlAugmentation
    {
        public string Name { get; }

        // Exactly as the server printed it, e.g. "0 (4 max)" — what the list shows.
        public string Value { get; }

        // The leading count off the same line, pulled out only so the levels can be totalled the
        // way the advanced augs are. The row itself still displays Value verbatim.
        public int Count { get; }

        // What one more level of this aug costs. Not in the "/enl augs" output — the prices are
        // hand-entered below, like ConquestAugmentation's coin costs. An aug that isn't in the
        // table shows nothing rather than a wrong price, so a new one added server-side still
        // gets its row.
        public string Cost => Costs.TryGetValue(Name, out string cost) ? cost : "";

        private const string Pristine = "1 Pristine Token of Enlightenment";
        private const string TwoTokens = "2 Token of Enlightenment";
        private const string FourTokens = "4 Token of Enlightenment";

        private static readonly Dictionary<string, string> Costs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cleave", Pristine },
            { "Arrow Split", Pristine },
            { "Spell Chain", Pristine },
            { "Aetheria Surge", Pristine },
            { "Void Contagion", Pristine },

            { "Damage", TwoTokens },
            { "Damage Reduction", TwoTokens },
            { "Crit Damage", TwoTokens },
            { "Crit Damage Reduction", TwoTokens },
            { "Imbue", TwoTokens },
            { "Salvage", TwoTokens },

            { "Skill Credits", FourTokens },
            { "Stamina Benediction", FourTokens },
            { "Mana Benediction", FourTokens },
        };

        private ConquestEnlAugmentation(string name, string value, int count)
        {
            Name = name;
            Value = value;
            Count = count;
        }

        // Rebuilt from scratch each time the block's header arrives, so a reprint replaces the
        // previous rows instead of appending to them.
        public static List<ConquestEnlAugmentation> All { get; private set; } = new List<ConquestEnlAugmentation>();

        // Sum of every enlightenment aug level, for the section header. Mirrors
        // ConquestAugmentation.Total.
        public static int Total => All.Sum(a => a.Count);

        // An "/enl augs" output line, e.g. "Crit Damage: 0 (4 max)". The label MUST be at the very
        // start of the line (after an optional bracketed chat tag, as every sibling parser allows),
        // which is only ever true of our own "/enl augs" output — when another player copies theirs
        // into a channel it arrives wrapped, 'Someone says, "Cleave: 1 (1 max)"', so the label is
        // no longer at the start and is ignored. The trailing "(<n> max)" is required as well:
        // with no fixed list of aug names to match against, that tail is what separates these from
        // the bare "<word>: <number>" shape ordinary chat throws off.
        //
        // Group 1 = name, group 2 = the value exactly as printed, group 3 = the count inside it.
        private static readonly Regex LineRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?([A-Za-z][A-Za-z '\-]*?):\s*(([\d,]+)\s*\(\s*[\d,]+\s+max\s*\))\s*$");

        // The block's header. Nothing to display, but it marks the start of a fresh block — see
        // NoteChat.
        private static readonly Regex ChromeRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?Enlightenment Augmentations:\s*$");

        // When we last issued "/enl augs" (UtcNow). Drives the throttle below.
        private static DateTime LastRefresh = DateTime.MinValue;

        // Minimum spacing between auto-refreshes. The Augs tab re-pulls while it's on screen (see
        // RefreshIfStale, called each tick from UpdateConquestAugmentations) — but no more often
        // than this, so it never spams "/enl augs". The manual Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

        // Drop the previous character's rows on login (called from PluginCore.Init) — same
        // stale-across-switch issue as ConquestAugmentation.
        public static void Init()
        {
            All = new List<ConquestEnlAugmentation>();
            LastRefresh = DateTime.MinValue;
            Request.Clear();
        }

        // Marks the window in which a reply to our own "/enl augs" is expected, so the chat handler
        // can suppress it without touching an "/enl augs" you typed yourself.
        private static readonly ChatRequest Request = new ChatRequest();

        // Ask the server to reprint the block so we can recapture it. Only meaningful on Conquest —
        // the only server with these augs. Returns true when the command actually went out, which
        // the view uses to acknowledge it on the Refresh button.
        public static bool Refresh()
        {
            if (!Server.IsConquest) return false;

            LastRefresh = DateTime.UtcNow;
            Request.Sent();
            Util.Command("/enl augs");

            return true;
        }

        // Refresh only if it's been at least RefreshThrottle since the last pull. The view calls
        // this every tick while the Augs tab is visible, so coming back to the tab shows current
        // counts on its own — immediately if it's been a while, and at most once per throttle
        // window while you sit on it — without a manual Refresh and without hammering the server.
        public static bool RefreshIfStale()
        {
            if (!Server.IsConquest) return false;
            if (DateTime.UtcNow - LastRefresh < RefreshThrottle) return false;

            return Refresh();
        }

        // True when this chat line is part of an "/enl augs" block — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
                && (LineRegex.IsMatch(text) || ChromeRegex.IsMatch(text));
        }

        // Forwarded from PluginCore's chat handler: capture one line of the block. Returns true
        // when the line answers an "/enl augs" the plugin issued, which is what makes it eligible
        // for suppression — an "/enl augs" you typed still populates the tab, and still prints.
        public static bool NoteChat(string text)
        {
            if (text == null) return false;

            Match m = LineRegex.Match(text);

            // Only reached behind Matches, so a line that isn't an aug is the block's header: the
            // rows that follow are a fresh printing, so drop what's there and collect them anew.
            if (!m.Success)
            {
                All = new List<ConquestEnlAugmentation>();
                return Request.Awaiting;
            }

            // A count that somehow doesn't parse still gets its row — it just doesn't add to the
            // total, which beats dropping the line.
            int.TryParse(m.Groups[3].Value.Replace(",", ""), out int count);

            All.Add(new ConquestEnlAugmentation(m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim(), count));

            return Request.Awaiting;
        }
    }
}
