using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The Conquest server's "/top" leaderboards, one per metric. "/top" with no argument prints
    // the available ones:
    //   [TOP] Specify a leaderboard: /top qb, /top level, /top enl, /top bank, /top lum,
    //         /top augs, /top deaths, or /top titles
    // and each of those prints a header line and one ranked line per player, e.g.:
    //   Top 25 Players by Quest Bonus:
    //   1: 5,682 - Stannonkor
    //   2: 5,543 - Raen
    // This class issues those commands, parses the blocks, and stores each leaderboard
    // separately for display (Server -> Top tab, one sub-tab per board). Mirrors ConquestBank /
    // ConquestFship / ConquestAugmentation. Conquest-only.
    public class TopBoard
    {
        // The "/top <Key>" argument, and the sub-tab label. The label also names this board's
        // controls in mainView.xml ("Top" + Label + "List", etc.) — see MainView.Top. Everything
        // shown on a tab comes from Label; nothing is read out of the server's own wording.
        public string Key { get; }
        public string Label { get; }

        // This board's chat command, e.g. "/top qb".
        public readonly string Command;

        // The metric this board's header line names — the "Quest Bonus" of "Top 25 Players by
        // Quest Bonus:". Hardcoded and never updated from what the server prints; nothing on a tab
        // reads it. Its only job is to attribute a block we didn't ask for (you typed "/top lum"
        // yourself) to the right board. Confirmed for augs / lum / qb; the rest are best guesses,
        // and a wrong one only means a hand-typed command for that board isn't picked up.
        private readonly string Header;

        // The players of the current block, in arrival order. Rebuilt in full on each block.
        // Read Sorted() to display them — the server splits a block across several chat messages
        // and they don't necessarily arrive in rank order.
        public readonly List<TopPlayer> Players = new List<TopPlayer>();

        // The players in rank order. Applied at display time rather than on insert so a block
        // that's still arriving reads correctly too.
        public List<TopPlayer> Sorted() => Players.OrderBy(p => p.Rank).ToList();

        // When this board's command last went out (UtcNow). Drives the throttle below.
        private DateTime LastRefresh = DateTime.MinValue;

        private TopBoard(string key, string label, string header)
        {
            Key = key;
            Label = label;
            Header = header;
            Command = $"/top {key}";
        }

        // The eight leaderboards, alphabetically by label — which is also the sub-tab order in
        // mainView.xml, and what makes the open sub-tab's index an index into this list. Keep the
        // two in sync. The keys are the server's own "/top" arguments and don't all match the
        // label ("/top qb" is the Quests board).
        public static readonly List<TopBoard> All = new List<TopBoard>
        {
            //            key       label (shown)  header wording (attribution only)
            new TopBoard("augs",   "Augs",       "Total Augmentations"),
            new TopBoard("deaths", "Deaths",     "Deaths"),
            new TopBoard("enl",    "Enlightens", "Enlightenment"),
            new TopBoard("level",  "Levels",     "Level"),
            new TopBoard("lum",    "Luminance",  "Banked Luminance"),
            new TopBoard("bank",   "Pyreals",    "Banked Pyreals"),
            new TopBoard("qb",     "Quests",     "Quest Bonus"),
            new TopBoard("titles", "Titles",     "Titles"),
        };

        // Minimum spacing between auto-refreshes. The open sub-tab pulls its board while it's on
        // screen (see RefreshIfStale, called each tick from UpdateTop) — but no more often than
        // this, so it never spams "/top". The manual Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

        // Drop everything held from the previous character on login (called from PluginCore.Init).
        // The standings are server-wide rather than character-specific, but a character switch can
        // also be a server switch, and off Conquest they must not linger. Resetting LastRefresh
        // lets each tab pull fresh data immediately when it's next opened.
        public static void Init()
        {
            foreach (TopBoard board in All)
            {
                board.Players.Clear();
                board.LastRefresh = DateTime.MinValue;
            }

            EndBlock();
            Orphans.Clear();
        }

        // Ask the server to reprint this leaderboard so we can reparse it. Conquest-only.
        public void Refresh()
        {
            if (!Server.IsConquest) return;

            // One block at a time. A reply carries nothing that ties it back to the command that
            // asked for it, so two in-flight "/top" replies would interleave and land on the
            // wrong boards — easy to trigger by clicking through the sub-tabs, since each one
            // pulls on arrival. Skipping rather than queueing is enough: LastRefresh is only
            // stamped once the command actually goes out, so the tab's per-tick RefreshIfStale
            // reissues it as soon as the current block finishes.
            if (Active() != null) return;

            LastRefresh = DateTime.UtcNow;
            OpenBlock(this);
            Util.Command(Command);
        }

        // Refresh only if it's been at least RefreshThrottle since this board's last pull. The
        // view calls this every tick for the open sub-tab, so coming back to it shows current
        // standings on its own — immediately if it's been a while, and at most once per throttle
        // window while you sit on it — without a manual Refresh and without hammering the server.
        public void RefreshIfStale()
        {
            if (!Server.IsConquest) return;
            if (DateTime.UtcNow - LastRefresh < RefreshThrottle) return;
            Refresh();
        }

        // ---- Parsing ------------------------------------------------------------------------
        //
        // The server sends a block as several chat messages and they do NOT arrive in order — the
        // header can land in the middle of the ranked lines, or after all of them:
        //     9: 310 - Grief
        //     ...
        //     Top 25 Players by Total Augmentations:
        //     1: 370 - Slayer
        //     ...
        // So the header can't delimit a block. What does is the command that went out: collection
        // opens when Refresh issues one, and every ranked line that arrives while it's open
        // belongs to that board. The header only says how many players to expect — plus, for a
        // block we didn't ask for, which board it belongs to (see Header).

        // The board currently collecting, and when collection opened. Null once the block is
        // complete, or once CollectWindow has passed without one.
        private static TopBoard Collecting;
        private static DateTime CollectingAt = DateTime.MinValue;

        // How many players the header promised. Unknown until it arrives, which is why it can't
        // be the only thing that ends a block.
        private static int CollectingExpected = int.MaxValue;

        // Whether this block has dropped the board's previous standings yet. Deferred to the
        // first line that actually arrives, so a request the server never answers leaves the old
        // standings on screen rather than blanking the tab.
        private static bool CollectingCleared;

        // How long a block stays open. Generous enough for the server to spread one across
        // several messages, short enough that an unanswered request can't swallow an unrelated
        // block minutes later.
        private static readonly TimeSpan CollectWindow = TimeSpan.FromSeconds(15);

        // Ranked lines that arrived with nothing collecting — you typed "/top lum" yourself and
        // the rows beat the header that says which board they belong to. Held briefly and folded
        // in when that header lands. A block we requested never needs these: collection is
        // already open before any of its lines arrive.
        private static readonly List<TopPlayer> Orphans = new List<TopPlayer>();
        private static DateTime OrphansAt = DateTime.MinValue;
        private static readonly TimeSpan OrphanWindow = TimeSpan.FromSeconds(10);
        private const int OrphanLimit = 100;

        // The line that names a block, e.g. "Top 25 Players by Quest Bonus:". Anchored so the
        // phrase must lead the line (after optional chat-timestamp / "[TOP]:" tags), so a pasted
        // 'Someone says, "Top 25 Players by..."' can't disturb a board. Groups: 1=count, 2=metric.
        private static readonly Regex HeaderRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*Top\s+(\d+)\s+Players?\s+by\s+(.+?):\s*$");

        // One ranked line, e.g. "1: 5,682 - Stannonkor". Groups: 1=rank, 2=value, 3=name. Same
        // anchoring: a quoted 'Someone says, "1: 5,682 - Bob"' doesn't start with the rank, so it
        // can't inject a row.
        private static readonly Regex EntryRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*(\d+):\s+([\d,]+)\s+-\s+(.+?)\s*$");

        // True when this chat line is part of a "/top" block — lets PluginCore route only the
        // relevant lines here. Gated to Conquest.
        public static bool Matches(string text)
        {
            if (text == null || !Server.IsConquest) return false;

            return HeaderRegex.IsMatch(text) || EntryRegex.IsMatch(text);
        }

        // Forwarded from PluginCore's chat handler.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            Match header = HeaderRegex.Match(text);
            if (header.Success) { NoteHeader(header); return; }

            Match entry = EntryRegex.Match(text);
            if (entry.Success) { NoteEntry(entry); }
        }

        // The header says how big the block is, and — for one we didn't ask for — which board it
        // belongs to.
        private static void NoteHeader(Match header)
        {
            // The board whose command we issued is the strongest signal, since the server is
            // answering it. Only when nothing is collecting (you typed "/top ..." yourself) do we
            // go by the header wording, which may be a board we can't place: leave everything
            // alone rather than filing it under the wrong one.
            TopBoard requested = Active();
            TopBoard board = requested ?? ByHeader(header.Groups[2].Value.Trim());
            if (board == null) return;

            OpenBlock(board);
            ClearOnce();

            CollectingExpected = int.TryParse(header.Groups[1].Value, out int count) ? count : int.MaxValue;

            // Buffered rows belong to this block only if this header is what opened it. A block we
            // requested was already collecting its own rows directly, so anything still buffered
            // came from something else — drop it rather than mixing another board's standings in.
            if (requested == null) { AdoptOrphans(); } else { Orphans.Clear(); }

            CloseIfComplete();
        }

        private static void NoteEntry(Match entry)
        {
            if (!int.TryParse(entry.Groups[1].Value, out int rank)) return;

            TopPlayer player = new TopPlayer(rank, entry.Groups[2].Value.Trim(), entry.Groups[3].Value.Trim());

            if (Active() == null) { HoldOrphan(player); return; }

            ClearOnce();
            Add(player);
            CloseIfComplete();
        }

        private static TopBoard ByHeader(string metric)
        {
            return All.FirstOrDefault(b => string.Equals(b.Header, metric, StringComparison.OrdinalIgnoreCase));
        }

        // Begin collecting for this board, unless we already are — a second request must not
        // restart the block and discard what has already come in.
        private static void OpenBlock(TopBoard board)
        {
            if (Active() == board) return;

            Collecting = board;
            CollectingAt = DateTime.UtcNow;
            CollectingExpected = int.MaxValue;
            CollectingCleared = false;
        }

        private static void EndBlock()
        {
            Collecting = null;
            CollectingExpected = int.MaxValue;
            CollectingCleared = false;
        }

        // The board collecting right now, or null if nothing is or the window has passed.
        private static TopBoard Active()
        {
            if (Collecting == null) return null;
            return (DateTime.UtcNow - CollectingAt < CollectWindow) ? Collecting : null;
        }

        private static void ClearOnce()
        {
            if (CollectingCleared) return;

            Collecting.Players.Clear();
            CollectingCleared = true;
        }

        // Ranks are unique within a block, so this drops a repeated line without dropping players
        // who tie on value — and keeps a repeat from counting twice toward the expected total.
        private static void Add(TopPlayer player)
        {
            if (Collecting.Players.Any(p => p.Rank == player.Rank)) return;
            Collecting.Players.Add(player);
        }

        private static void CloseIfComplete()
        {
            if (Collecting.Players.Count >= CollectingExpected) { EndBlock(); }
        }

        private static void AdoptOrphans()
        {
            if (DateTime.UtcNow - OrphansAt <= OrphanWindow)
            {
                foreach (TopPlayer player in Orphans) { Add(player); }
            }

            Orphans.Clear();
        }

        private static void HoldOrphan(TopPlayer player)
        {
            if (DateTime.UtcNow - OrphansAt > OrphanWindow) { Orphans.Clear(); }

            OrphansAt = DateTime.UtcNow;
            if (Orphans.Count < OrphanLimit) { Orphans.Add(player); }
        }
    }
}
