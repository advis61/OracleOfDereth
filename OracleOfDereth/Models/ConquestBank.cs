using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's bank balances, shown via the "/b" command which prints one
    // "[BANK] <label>: <value>" line per balance. This class issues that command, parses the
    // lines, and stores each balance separately for display (Server -> Bank tab). Mirrors
    // ConquestAugmentation. Conquest-only. The unrelated Bank class handles withdraw / bank
    // support detection for trades; this is purely the balance readout.
    public class ConquestBank
    {
        public string Name { get; }
        public string Value { get; set; }

        private ConquestBank(string name, string value) { Name = name; Value = value; }

        // Parsed balances, keyed by name (updated in place across refreshes). Stored in parse
        // order, but displayed sorted alphabetically (see MainView.ConquestBank) since the
        // server's "/b" output order can drift between calls.
        public static readonly List<ConquestBank> All = new List<ConquestBank>();

        // When we last issued "/b" (UtcNow). Drives the throttle below.
        private static DateTime LastRefresh = DateTime.MinValue;

        // Minimum spacing between auto-refreshes. The Bank tab pulls fresh balances while it's on
        // screen (see RefreshIfStale, called each tick from UpdateConquestBank) — but no more often
        // than this, so it never spams "/b". The manual Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

        // A "/b" output line, e.g. "[BANK] Pyreals: 1,039,678,533" or
        // "[BANK] Daily Transfer: 0 / 8,020,000 (+20,000 enlightenment)". The value is the rest
        // of the line; header lines with no value (e.g. "[BANK] Your balances:") don't match.
        // The "[BANK]" tag MUST be at the start of the line (after an optional chat timestamp like
        // "[12:34:56] "), which is only ever true of our own "/b" output. When another player copies
        // their balances into a chat channel it arrives wrapped — 'Someone says, "[BANK] Pyreals:
        // 999"' — so the tag is no longer at the start and is ignored, instead of overwriting our
        // own balances with theirs.
        private static readonly Regex LineRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?\[BANK\]\s+(.+?):\s*(.*\S)\s*$");

        // The block's heading, "[BANK] Your balances:" — a labelled line with no value, which is
        // exactly what LineRegex declines to match. Nothing to parse; recognised only so it can be
        // suppressed with the balances, and only while a reply we asked for is arriving. Requiring
        // the trailing colon keeps it clear of "[BANK] Withdrew ...", which you should always see.
        private static readonly Regex ChromeRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?\[BANK\]\s+[^:]*:\s*$");

        // The server labels event-token currencies as "Event Tokens [Dragon Coins] (...)";
        // show just the token name, e.g. "Dragon Coins (...)".
        private static readonly Regex EventTokenRegex = new Regex(@"Event Tokens \[(.+?)\]");

        // One MMD trade note is 250,000 pyreals.
        private const int PyrealsPerMmd = 250_000;

        public static ConquestBank Get(string name) => All.FirstOrDefault(b => b.Name == name);

        // Clear the previous character's balances on login (called from PluginCore.Init). All +
        // LastRefresh are static and otherwise only updated while the Bank tab is visible (throttled
        // to 5 min), so without this a new character sees the prior character's balances — including
        // character-specific lines like Daily Transfer — until the throttle expires. Resetting
        // LastRefresh lets the tab pull fresh data immediately when it's next opened.
        public static void Init()
        {
            All.Clear();
            LastRefresh = DateTime.MinValue;
            Request.Clear();
        }

        // Marks the window in which a reply to our own "/b" is expected, so the chat handler can
        // suppress it without touching a "/b" you typed yourself.
        private static readonly ChatRequest Request = new ChatRequest();

        // Ask the server to reprint balances so we can reparse them. Conquest-only. Returns true
        // when the command actually went out, which the view uses to acknowledge it on the Refresh
        // button.
        public static bool Refresh()
        {
            if (!Server.IsConquest) return false;

            LastRefresh = DateTime.UtcNow;
            Request.Sent();
            Util.Command("/b");

            return true;
        }

        // Refresh only if it's been at least RefreshThrottle since the last pull. The view calls
        // this every tick while the Bank tab is visible, so coming back to the tab shows current
        // balances on its own — immediately if it's been a while, and at most once per throttle
        // window while you sit on it — without a manual Refresh and without hammering the server.
        public static bool RefreshIfStale()
        {
            if (!Server.IsConquest) return false;
            if (DateTime.UtcNow - LastRefresh < RefreshThrottle) return false;

            return Refresh();
        }

        // True when this chat line is a "/b" balance line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching other servers' bank chat.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
                && (LineRegex.IsMatch(text) || (Request.Awaiting && ChromeRegex.IsMatch(text)));
        }

        // Forwarded from PluginCore's chat handler: parse one balance line and store it,
        // updating the existing entry for that currency or appending a new one. Returns true when
        // the line answers a "/b" the plugin issued, which is what makes it eligible for
        // suppression.
        public static bool NoteChat(string text)
        {
            if (text == null) return false;

            // Only reached behind Matches, so a line that isn't a balance is the block's heading:
            // nothing to store, but still ours to suppress.
            Match m = LineRegex.Match(text);
            if (!m.Success) return Request.Awaiting;

            string name = EventTokenRegex.Replace(m.Groups[1].Value.Trim(), "$1");
            string value = m.Groups[2].Value.Trim();

            // Annotate the pyreal balance with its MMD-trade-note equivalent, e.g.
            // "1,039,678,533 (4,158 MMDs)".
            if (name == "Pyreals" && long.TryParse(value.Replace(",", ""), out long pyreals))
            {
                value += $" ({pyreals / PyrealsPerMmd:N0} MMDs)";
            }

            ConquestBank entry = Get(name);
            if (entry != null) { entry.Value = value; }
            else { All.Add(new ConquestBank(name, value)); }

            return Request.Awaiting;
        }
    }
}
