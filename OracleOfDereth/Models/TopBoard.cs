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
    // and each of those prints a header line followed by one ranked line per player, e.g.:
    //   Top 25 Players by Quest Bonus:
    //   1: 5,682 - Stannonkor
    //   2: 5,543 - Raen
    // This class issues those commands, parses the blocks, and stores each leaderboard
    // separately for display (Server -> Top tab, one sub-tab per board). Mirrors ConquestBank /
    // ConquestFship / ConquestAugmentation. Conquest-only.
    public class TopBoard
    {
        // The "/top <Key>" argument, and the sub-tab label. The label also names this board's
        // controls in mainView.xml ("Top" + Label + "List", etc.) — see MainView.Top.
        public string Key { get; }
        public string Label { get; }

        // The metric as the server names it in the header line ("Quest Bonus", "Banked
        // Luminance"). Seeded with our best guess so the tab reads sensibly before the first
        // refresh, then overwritten with the server's own wording once we've seen a block. Also
        // the fallback that attributes a block we didn't ask for (you typed "/top lum" yourself)
        // to the right board — an exact match only, so a wrong guess just means that block is
        // ignored until the tab has fetched once.
        public string Title { get; private set; }

        // The players of the current block, in arrival order. Rebuilt in full on each block.
        // Read Sorted() to display them — the server splits a block across several chat messages
        // and they don't necessarily arrive in rank order.
        public readonly List<TopPlayer> Players = new List<TopPlayer>();

        // The players in rank order. Applied at display time rather than on insert so a block
        // that's still arriving reads correctly too.
        public List<TopPlayer> Sorted() => Players.OrderBy(p => p.Rank).ToList();

        // When we last issued this board's command (UtcNow). Drives the throttle below.
        private DateTime LastRefresh = DateTime.MinValue;

        private TopBoard(string key, string label, string title)
        {
            Key = key;
            Label = label;
            Title = title;
        }

        public string Command => $"/top {Key}";

        // The eight leaderboards, alphabetically by label — which is also the sub-tab order in
        // mainView.xml, and what makes the open sub-tab's index an index into this list. Keep the
        // two in sync. The keys are the server's own "/top" arguments and don't all match the
        // label ("/top qb" is the Quests board).
        public static readonly List<TopBoard> All = new List<TopBoard>
        {
            new TopBoard("augs",   "Augs",       "Augmentations"),
            new TopBoard("bank",   "Bank",       "Banked Pyreals"),
            new TopBoard("deaths", "Deaths",     "Deaths"),
            new TopBoard("enl",    "Enlightens", "Enlightenment"),
            new TopBoard("level",  "Level",      "Level"),
            new TopBoard("lum",    "Luminance",  "Banked Luminance"),
            new TopBoard("qb",     "Quests",     "Quest Bonus"),
            new TopBoard("titles", "Titles",     "Titles"),
        };

        // Minimum spacing between auto-refreshes. The open sub-tab pulls its board while it's on
        // screen (see RefreshIfStale, called each tick from UpdateTop) — but no more often than
        // this, so it never spams "/top". The manual Refresh button ignores it.
        private static readonly TimeSpan RefreshThrottle = TimeSpan.FromMinutes(5);

        // Clear the previous character's boards on login (called from PluginCore.Init). The
        // standings are server-wide rather than character-specific, but a character switch can
        // also be a server switch — and off Conquest these must not linger. Resetting
        // LastRefresh lets each tab pull fresh data immediately when it's next opened.
        public static void Init()
        {
            foreach (TopBoard board in All)
            {
                board.Players.Clear();
                board.LastRefresh = DateTime.MinValue;
            }

            Pending = null;
            Receiving = null;
        }

        // Ask the server to reprint this leaderboard so we can reparse it. Conquest-only.
        public void Refresh()
        {
            if (!Server.IsConquest) return;

            LastRefresh = DateTime.UtcNow;
            Pending = this;
            PendingAt = DateTime.UtcNow;
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

        // The board whose command we've issued but whose header hasn't come back yet. The reply
        // itself doesn't echo the "/top" argument, so this is what ties a block to the board that
        // asked for it; the header wording is only a fallback (see Title).
        private static TopBoard Pending;
        private static DateTime PendingAt = DateTime.MinValue;

        // How long a request stays claimable. If the server never answers (a mistyped or removed
        // leaderboard), the claim expires instead of misattributing whatever block shows up next.
        private static readonly TimeSpan PendingWindow = TimeSpan.FromSeconds(15);

        // The board currently accumulating rows: set by a header line, cleared once the promised
        // number of players have arrived. Entry lines are only claimed while this is set, which is
        // what keeps the loose "N: value - name" shape from scraping unrelated chat.
        private static TopBoard Receiving;
        private static int ReceivingExpected;

        // The line that begins a block, e.g. "Top 25 Players by Quest Bonus:". Anchored so the
        // phrase must lead the line (after optional chat-timestamp / "[TOP]:" tags), so a pasted
        // 'Someone says, "Top 25 Players by..."' can't wipe a board. Groups: 1=count, 2=metric.
        private static readonly Regex HeaderRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*Top\s+(\d+)\s+Players?\s+by\s+(.+?):\s*$");

        // One ranked line, e.g. "1: 5,682 - Stannonkor". Groups: 1=rank, 2=value, 3=name.
        private static readonly Regex EntryRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*(\d+):\s+([\d,]+)\s+-\s+(.+?)\s*$");

        // True when this chat line is part of a "/top" block — lets PluginCore route only the
        // relevant lines here. Gated to Conquest, and entry lines only count while a block is
        // actually in flight.
        public static bool Matches(string text)
        {
            if (text == null || !Server.IsConquest) return false;

            return HeaderRegex.IsMatch(text) || (Receiving != null && EntryRegex.IsMatch(text));
        }

        // Forwarded from PluginCore's chat handler: a header opens a block on the board it
        // belongs to (clearing that board's previous standings); each following ranked line is
        // parsed and appended until the header's promised count is reached.
        public static void NoteChat(string text)
        {
            if (text == null) return;

            Match header = HeaderRegex.Match(text);
            if (header.Success) { BeginBlock(header); return; }

            if (Receiving == null) return;

            Match entry = EntryRegex.Match(text);
            if (!entry.Success) return;

            Receiving.Players.Add(new TopPlayer(
                int.TryParse(entry.Groups[1].Value, out int rank) ? rank : Receiving.Players.Count + 1,
                entry.Groups[2].Value.Trim(),
                entry.Groups[3].Value.Trim()));

            if (Receiving.Players.Count >= ReceivingExpected) { Receiving = null; }
        }

        private static void BeginBlock(Match header)
        {
            string title = header.Groups[2].Value.Trim();
            TopBoard board = TakePending() ?? ByTitle(title);

            // A block for a metric we can't place (an unrequested "/top <something new>"): drop it
            // rather than filing it under the wrong board.
            if (board == null) { Receiving = null; return; }

            board.Title = title;
            board.Players.Clear();

            Receiving = board;
            ReceivingExpected = int.TryParse(header.Groups[1].Value, out int count) ? count : int.MaxValue;
        }

        // The board we asked for, if the request is still fresh. Claimed once either way, so a
        // second block can't also land on it.
        private static TopBoard TakePending()
        {
            TopBoard pending = Pending;
            Pending = null;

            if (pending == null) return null;
            return (DateTime.UtcNow - PendingAt < PendingWindow) ? pending : null;
        }

        private static TopBoard ByTitle(string title)
        {
            return All.FirstOrDefault(b => string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase));
        }
    }
}
