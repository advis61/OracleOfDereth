using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's public "looking for members" fellowship list, shown via the
    // "/fship list" command. That command prints a header line followed by one line per
    // recruiting fellowship, e.g.:
    //   [FSHIP]: Fellowships looking for members (tell any member 'xp' to join):
    //     - Awoooooo (Leader: Bluchewbarry) [11/14] @ Viamont Staging Area v3
    // This class issues the command, parses the lines, and stores each fellowship for display
    // (Server -> Fship tab). Mirrors ConquestBank / ConquestAugmentation. Conquest-only.
    // Clicking a row's Join cell tells that fellowship's leader "xp" to join (see
    // MainView.ConquestFship). Named to match the ConquestBank sibling (a single recruiting
    // fellowship, with a static All registry).
    public class ConquestFship
    {
        public string Name { get; }
        public string Leader { get; }
        public string Members { get; }   // e.g. "11/14"
        public string Location { get; }  // "" when the fellowship prints no location

        // Current member count parsed from Members ("11/14" -> 11), for the Size sort.
        public int MemberCount
        {
            get
            {
                int slash = Members.IndexOf('/');
                string head = slash >= 0 ? Members.Substring(0, slash) : Members;
                return int.TryParse(head.Trim(), out int n) ? n : 0;
            }
        }

        private ConquestFship(string name, string leader, string members, string location)
        {
            Name = name;
            Leader = leader;
            Members = members;
            Location = location;
        }

        // Rebuilt in full on each "/fship list" (the recruiting set changes constantly), in the
        // server's print order. The header line clears it to begin a fresh block.
        public static readonly List<ConquestFship> All = new List<ConquestFship>();

        // Whether "/fship list" has been issued yet. Lets the Fship tab lazy-refresh the first
        // time it's shown instead of running on login (mirrors ConquestBank.Ran).
        public static bool Ran = false;

        // Column sort, applied as a display-time view (see Sorted) rather than reordering All,
        // so it survives the full rebuild that happens on every "/fship list" refresh.
        public enum SortType { NameAsc, NameDesc, MembersAsc, MembersDesc, LocationAsc, LocationDesc }
        public static SortType CurrentSort = SortType.NameAsc;

        // All fellowships ordered by the current column sort (Name is the tiebreaker throughout).
        public static List<ConquestFship> Sorted()
        {
            StringComparer c = StringComparer.OrdinalIgnoreCase;
            switch (CurrentSort)
            {
                case SortType.NameDesc:     return All.OrderByDescending(f => f.Name, c).ToList();
                case SortType.MembersAsc:   return All.OrderBy(f => f.MemberCount).ThenBy(f => f.Name, c).ToList();
                case SortType.MembersDesc:  return All.OrderByDescending(f => f.MemberCount).ThenBy(f => f.Name, c).ToList();
                case SortType.LocationAsc:  return All.OrderBy(f => f.Location, c).ThenBy(f => f.Name, c).ToList();
                case SortType.LocationDesc: return All.OrderByDescending(f => f.Location, c).ThenBy(f => f.Name, c).ToList();
                case SortType.NameAsc:
                default:                    return All.OrderBy(f => f.Name, c).ToList();
            }
        }

        // The header line that begins a "/fship list" block; seeing it clears the previous set.
        private static readonly Regex HeaderRegex = new Regex(@"Fellowships looking for members");

        // One fellowship line, e.g. "  - Olthoi (Leader: Tank) [13/14] @ Pheraion's Sanctum v3".
        // The trailing " @ <location>" is optional. Not anchored at the start, so a leading chat
        // timestamp is tolerated. Groups: 1=name, 2=leader, 3=members "n/n", 4=location.
        private static readonly Regex LineRegex = new Regex(
            @"-\s+(.+?)\s+\(Leader:\s+(.+?)\)\s+\[(\d+/\d+)\](?:\s+@\s+(.+?))?\s*$");

        // Ask the server to reprint the recruiting list so we can reparse it. Conquest-only.
        public static void Refresh()
        {
            if (!Server.IsConquest) return;
            Ran = true;
            Util.Command("/fship list");
        }

        // True when this chat line is part of a "/fship list" block (header or a fellowship row) —
        // lets PluginCore route only the relevant lines here. Gated to Conquest.
        public static bool Matches(string text)
        {
            return text != null
                && Server.IsConquest
                && (HeaderRegex.IsMatch(text) || LineRegex.IsMatch(text));
        }

        // Forwarded from PluginCore's chat handler: the header clears the set to begin a fresh
        // block; each subsequent fellowship line is parsed and appended.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            if (HeaderRegex.IsMatch(text)) { All.Clear(); return; }

            Match m = LineRegex.Match(text);
            if (!m.Success) return;

            All.Add(new ConquestFship(
                m.Groups[1].Value.Trim(),
                m.Groups[2].Value.Trim(),
                m.Groups[3].Value.Trim(),
                m.Groups[4].Success ? m.Groups[4].Value.Trim() : ""));
        }
    }
}
