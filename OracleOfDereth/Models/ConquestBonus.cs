using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's XP bonus breakdown, printed by the "/bonus" command as one
    // "<Type> Bonus: <percent>% (<detail>)" line per source under a "=== XP Bonuses ===" header.
    // This class issues that command, parses the lines, and stores each bonus separately for
    // display on the Conquest (Custom) tab beneath the augs list. Mirrors ConquestAugmentation /
    // ConquestBank. Conquest-only. Unlike "/augs", "/bonus" is NOT refreshed on login — it lazy-
    // loads the first time the tab is shown, and on the tab's Refresh button.
    public class ConquestBonus
    {
        public string Name { get; }
        public string Value { get; set; } = "";

        private ConquestBonus(string name) { Name = name; }

        // Registry, in "/bonus" output order.
        public static readonly List<ConquestBonus> All = new List<ConquestBonus>
        {
            new ConquestBonus("Quest"),
            new ConquestBonus("Enlightenment"),
            new ConquestBonus("PK Dungeon"),
            new ConquestBonus("Augmentation"),
            new ConquestBonus("Equipment"),
            new ConquestBonus("Total"),
        };

        // A "/bonus" output line, e.g. "Quest Bonus: 14.18% (1,418 quests)" or
        // "PK Dungeon Bonus: 0.00%". The label MUST be at the start of the line (after an optional
        // chat timestamp like "[12:34:56] "), which is only ever true of our own "/bonus" output.
        // When another player copies their bonuses into chat it arrives wrapped — 'Someone says,
        // "Quest Bonus: 99.99%"' — so the label is no longer at the start and is ignored, instead
        // of overwriting our own bonuses. Group 1 = type, group 2 = the rest of the line.
        private static readonly Regex LineRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?(Quest|Enlightenment|PK Dungeon|Augmentation|Equipment|Total) Bonus:\s*(.+\S)\s*$");

        // The block's title line, e.g. "=== XP Bonuses ===". Nothing to parse; recognised only so
        // it can be suppressed with the values, and only while a reply we asked for is arriving.
        private static readonly Regex ChromeRegex = new Regex(
            @"^\s*(?:\[[^\]]*\]\s*)?={2,}\s*XP Bonuses\s*={2,}\s*$");

        // When we last issued "/bonus" (UtcNow). Drives the throttle below.
        private static DateTime LastRefresh = DateTime.MinValue;

        // Minimum spacing between auto-refreshes. The Conquest augs tab re-pulls the bonuses while
        // it's on screen (see RefreshIfStale, called each tick from UpdateConquestAugmentations) —
        // but no more often than this, so it never spams "/bonus". The Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

        public static ConquestBonus Get(string name) => All.FirstOrDefault(b => b.Name == name);

        // Clear the previous character's bonus values on login (called from PluginCore.Init) — same
        // stale-across-switch issue as ConquestBank. All is a fixed list, so blank each entry's value
        // rather than clearing the list. Resetting LastRefresh lets the tab re-pull on next open.
        public static void Init()
        {
            foreach (var b in All) b.Value = "";
            LastRefresh = DateTime.MinValue;
            Request.Clear();
        }

        // Marks the window in which a reply to our own "/bonus" is expected, so the chat handler
        // can suppress it without touching a "/bonus" you typed yourself.
        private static readonly ChatRequest Request = new ChatRequest();

        // Ask the server to reprint the bonus block so we can reparse it. Only meaningful on
        // Conquest — the only server with these bonuses. Returns true when the command actually
        // went out, which the view uses to acknowledge it on the Refresh button.
        public static bool Refresh()
        {
            if (!Server.IsConquest) return false;

            LastRefresh = DateTime.UtcNow;
            Request.Sent();
            Util.Command("/bonus");

            return true;
        }

        // Refresh only if it's been at least RefreshThrottle since the last pull. The view calls
        // this every tick while the augs tab is visible, so coming back to the tab shows current
        // bonuses on its own — immediately if it's been a while, and at most once per throttle
        // window while you sit on it — without a manual Refresh and without hammering the server.
        public static bool RefreshIfStale()
        {
            if (!Server.IsConquest) return false;
            if (DateTime.UtcNow - LastRefresh < RefreshThrottle) return false;

            return Refresh();
        }

        // True when this chat line is a "/bonus" bonus line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
                && (LineRegex.IsMatch(text) || (Request.Awaiting && ChromeRegex.IsMatch(text)));
        }

        // Forwarded from PluginCore's chat handler: parse one bonus line and store its value.
        // Returns true when the line answers a "/bonus" the plugin issued, which is what makes it
        // eligible for suppression.
        public static bool NoteChat(string text)
        {
            if (text == null) return false;

            // Only reached behind Matches, so a line that isn't a bonus is the block's title:
            // nothing to store, but still ours to suppress.
            Match m = LineRegex.Match(text);
            if (!m.Success) return Request.Awaiting;

            ConquestBonus entry = Get(m.Groups[1].Value);
            if (entry != null) { entry.Value = m.Groups[2].Value.Trim(); }

            return Request.Awaiting;
        }
    }
}
