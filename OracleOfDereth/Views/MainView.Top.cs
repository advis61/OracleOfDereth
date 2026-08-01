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
            public HudStaticText Heading;
            public string HeadingText;      // fixed, read off the control once
            public HudButton Refresh;
            public HudStaticText[] Headers; // the column headers, only ever shown or hidden together
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
                    Heading = (HudStaticText)view[$"Top{board.Label}Text"],
                    Refresh = (HudButton)view[$"Top{board.Label}Refresh"],
                    Headers = new[]
                    {
                        (HudStaticText)view[$"Top{board.Label}Rank"],
                        (HudStaticText)view[$"Top{board.Label}Name"],
                        (HudStaticText)view[$"Top{board.Label}Value"],
                    },
                    List = (HudList)view[$"Top{board.Label}List"],
                };

                // A tab's own wording — the heading and the header above the value column — is set
                // on the controls in mainView.xml, so it sits with the rest of the layout and a tab
                // can name its metric however it reads best ("QB" on the strip, "Quest Bonus"
                // inside). Nothing here rewrites it, and nothing reads the server's wording either.
                // The heading is kept because UpdateTop swaps it for "None" off-server and has to
                // put it back.
                page.HeadingText = page.Heading.Text;
                page.Heading.FontHeight = 10;

                page.Refresh.Hit += TopRefresh_Hit;
                page.List.ClearRows();

                TopPages.Add(page);
            }
        }

        private void DisposeTop()
        {
            foreach (TopPage page in TopPages) { page.Refresh.Hit -= TopRefresh_Hit; }

            // Drop the references so nothing here outlives the view being disposed.
            TopPages.Clear();
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
            page.List.Visible = available;
            foreach (HudStaticText header in page.Headers) { header.Visible = available; }

            if (!available)
            {
                SetText(page.Heading, "None");
                return;
            }

            // While the tab is actually on screen, keep this board current on its own (throttled
            // inside RefreshIfStale), so opening it shows standings without hitting Refresh. The
            // view.Visible gate matters because Update() still ticks this method while the plugin
            // window is closed — we don't want to issue "/top" then. When a pull does go out, the
            // Refresh button says so — the heading stays put.
            if (view.Visible && page.Board.RefreshIfStale()) { FlashButton(page.Refresh); }

            SetText(page.Heading, page.HeadingText);

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
            if (page == null) { return; }

            page.Board.Refresh();
            FlashButton(page.Refresh);
        }
    }
}
