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

        // Whether "/b" has been issued yet. Lets the Bank tab lazy-refresh the first time it's
        // shown instead of spamming "/b" on login (mirrors QuestFlag.MyQuestsRan).
        public static bool Ran = false;

        // A "/b" output line, e.g. "[BANK] Pyreals: 1,039,678,533" or
        // "[BANK] Daily Transfer: 0 / 8,020,000 (+20,000 enlightenment)". The value is the rest
        // of the line; header lines with no value (e.g. "[BANK] Your balances:") don't match.
        private static readonly Regex LineRegex = new Regex(@"\[BANK\]\s+(.+?):\s*(.*\S)\s*$");

        // The server labels event-token currencies as "Event Tokens [Dragon Coins] (...)";
        // show just the token name, e.g. "Dragon Coins (...)".
        private static readonly Regex EventTokenRegex = new Regex(@"Event Tokens \[(.+?)\]");

        // One MMD trade note is 250,000 pyreals.
        private const int PyrealsPerMmd = 250_000;

        public static ConquestBank Get(string name) => All.FirstOrDefault(b => b.Name == name);

        // Ask the server to reprint balances so we can reparse them. Conquest-only.
        public static void Refresh()
        {
            if (!Server.IsConquest) return;
            Ran = true;
            Util.Command("/b");
        }

        // True when this chat line is a "/b" balance line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching other servers' bank chat.
        public static bool Matches(string text)
        {
            return text != null && Server.IsConquest && LineRegex.IsMatch(text);
        }

        // Forwarded from PluginCore's chat handler: parse one balance line and store it,
        // updating the existing entry for that currency or appending a new one.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            Match m = LineRegex.Match(text);
            if (!m.Success) return;

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
        }
    }
}
