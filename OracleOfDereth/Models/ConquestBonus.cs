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
        // "PK Dungeon Bonus: 0.00%". The label set keeps this from matching unrelated chat; it
        // tolerates a leading chat timestamp. Group 1 = type, group 2 = the rest of the line.
        private static readonly Regex LineRegex = new Regex(
            @"\b(Quest|Enlightenment|PK Dungeon|Augmentation|Equipment|Total) Bonus:\s*(.+\S)\s*$");

        // Whether "/bonus" has been issued yet. Lets the Conquest tab lazy-refresh the first time
        // it's shown instead of running on login (mirrors ConquestAugmentation.Ran / ConquestBank).
        public static bool Ran = false;

        public static ConquestBonus Get(string name) => All.FirstOrDefault(b => b.Name == name);

        // Ask the server to reprint the bonus block so we can reparse it. Only meaningful on
        // Conquest — the only server with these bonuses.
        public static void Refresh()
        {
            if (!Server.IsConquest) return;
            Ran = true;
            Util.Command("/bonus");
        }

        // True when this chat line is a "/bonus" bonus line — lets PluginCore route only the
        // relevant lines here. Gated to Conquest to avoid matching stray chat.
        public static bool Matches(string text)
        {
            return text != null && Server.IsConquest && LineRegex.IsMatch(text);
        }

        // Forwarded from PluginCore's chat handler: parse one bonus line and store its value.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            Match m = LineRegex.Match(text);
            if (!m.Success) return;

            ConquestBonus entry = Get(m.Groups[1].Value);
            if (entry != null) { entry.Value = m.Groups[2].Value.Trim(); }
        }
    }
}
