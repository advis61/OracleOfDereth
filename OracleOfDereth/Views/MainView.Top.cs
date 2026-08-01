using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // The eight "/top" leaderboards are identical apart from which board they show, so they're
        // wired up from TopBoard.All rather than as eight near-identical blocks of code. Each
        // board's controls are named "Top" + its Label + a suffix in mainView.xml, and the sub-tab
        // order there matches TopBoard.All — that's what makes TopViewNotebook.CurrentTab an index
        // into TopPages.
        private class TopPage
        {
            public TopBoard Board;
            public HudStaticText Text;
            public HudButton Refresh;
            public HudStaticText RankHeader;
            public HudStaticText ValueHeader;
            public HudStaticText NameHeader;
            public HudList List;
        }

        private readonly List<TopPage> TopPages = new List<TopPage>();

        // Column indices for every Top*List (see mainView.xml).
        private const int TopColRank = 0;
        private const int TopColName = 1;
        private const int TopColValue = 2;

        private void InitTop()
        {
            foreach (TopBoard board in TopBoard.All)
            {
                TopPage page = new TopPage
                {
                    Board = board,
                    Text = (HudStaticText)view[$"Top{board.Label}Text"],
                    Refresh = (HudButton)view[$"Top{board.Label}Refresh"],
                    RankHeader = (HudStaticText)view[$"Top{board.Label}Rank"],
                    ValueHeader = (HudStaticText)view[$"Top{board.Label}Value"],
                    NameHeader = (HudStaticText)view[$"Top{board.Label}Name"],
                    List = (HudList)view[$"Top{board.Label}List"],
                };

                // Headings read from the board's label rather than the metric name the server
                // prints, so a tab always describes itself the same way you selected it. The value
                // column header is set once and never changes; the heading is only seeded here,
                // since UpdateTop swaps it for the loading / off-server states.
                page.Text.Text = $"Top Players by {board.Label}";
                page.ValueHeader.Text = board.Label;

                page.Text.FontHeight = 10;
                page.Refresh.Hit += TopRefresh_Hit;
                page.List.ClearRows();

                TopPages.Add(page);
            }
        }

        private void DisposeTop()
        {
            foreach (TopPage page in TopPages) { page.Refresh.Hit -= TopRefresh_Hit; }
        }

        public void UpdateTop()
        {
            // Only the open sub-tab is on screen, so only it is worth painting — the others are
            // brought up to date when they're selected (the tab change ticks Update()).
            TopPage page = CurrentTopPage();
            if (page == null) { return; }

            // The leaderboards only exist on Conquest. Off-server, show "None" and hide the list
            // and its column headers, same as the Bank / Fship / Augs tabs.
            bool available = Server.IsConquest;

            page.Refresh.Visible = available;
            page.RankHeader.Visible = available;
            page.ValueHeader.Visible = available;
            page.NameHeader.Visible = available;
            page.List.Visible = available;

            if (!available)
            {
                page.Text.Text = "None";
                return;
            }

            // While the tab is actually on screen, keep this board current on its own (throttled
            // inside RefreshIfStale), so opening it shows standings without hitting Refresh. The
            // view.Visible gate matters because Update() still ticks this method while the plugin
            // window is closed — we don't want to issue "/top" then.
            if (view.Visible) { page.Board.RefreshIfStale(); }

            // The heading is fixed (set in InitTop) except while the first block is still on its
            // way, when it says so instead of sitting over an empty list.
            page.Text.Text = page.Board.Players.Count > 0
                ? $"Top Players by {page.Board.Label}"
                : $"Loading {page.Board.Command}...";

            UpdateTopList(page);
        }

        private TopPage CurrentTopPage()
        {
            int tab = TopViewNotebook.CurrentTab;
            return (tab >= 0 && tab < TopPages.Count) ? TopPages[tab] : null;
        }

        private void UpdateTopList(TopPage page)
        {
            List<TopPlayer> players = page.Board.Sorted();

            for (int x = 0; x < players.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= page.List.RowCount)
                    ? page.List.AddRow()
                    : page.List[x];

                SetText(row, TopColRank, $"{players[x].Rank}");
                SetText(row, TopColName, players[x].Name);
                SetText(row, TopColValue, players[x].Value);
            }

            // Trim stale rows (a shorter board than the one previously shown here).
            while (page.List.RowCount > players.Count) { page.List.RemoveRow(page.List.RowCount - 1); }
        }

        // Reissues this board's "/top" command so the server reprints it; the chat handler
        // reparses the block and the list refreshes on the next tick.
        private void TopRefresh_Hit(object sender, EventArgs e)
        {
            TopPage page = TopPages.FirstOrDefault(p => ReferenceEquals(p.Refresh, sender));
            page?.Board.Refresh();
        }
    }
}
