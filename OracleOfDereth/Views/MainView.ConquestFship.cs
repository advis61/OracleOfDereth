using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText ConquestFshipText { get; private set; } // top-left: current fellowship
        public HudFixedLayout ConquestFshipSort { get; private set; } // sortable-list indicator slot
        private HudPictureBox ConquestFshipSortIcon;
        public HudStaticText ConquestFshipName { get; private set; }
        public HudStaticText ConquestFshipMembers { get; private set; }
        public HudStaticText ConquestFshipLocation { get; private set; }
        public HudList ConquestFshipList { get; private set; }
        public HudButton ConquestFshipRefresh { get; private set; }
        public HudButton ConquestFshipJoin { get; private set; }
        public HudButton ConquestFshipQuit { get; private set; }

        // Column indices for ConquestFshipList (see mainView.xml). Columns start at the left edge;
        // the sort-indicator glyph floats in the header row (ConquestFshipSort), not in a column.
        private const int FshipColName = 0;
        private const int FshipColMembers = 1;
        private const int FshipColLocation = 2;
        private const int FshipColLeader = 3; // hidden; holds the leader name for Join

        // The leader of the currently selected (highlighted) fellowship, "" when none.
        private string fshipSelectedLeader = "";

        private void InitConquestFship()
        {
            ConquestFshipText = (HudStaticText)view["ConquestFshipText"];
            ConquestFshipText.FontHeight = 10;

            // Sortable-list indicator glyph, in the list's leading icon-column header slot.
            ConquestFshipSortIcon = new HudPictureBox();
            ConquestFshipSortIcon.Image = IconSort;
            ConquestFshipSort = (HudFixedLayout)view["ConquestFshipSort"];
            ConquestFshipSort.AddControl(ConquestFshipSortIcon, new Rectangle(0, 0, 16, 16));
            ConquestFshipSortIcon.Hit += ConquestFshipName_Click;

            ConquestFshipName = (HudStaticText)view["ConquestFshipName"];
            ConquestFshipName.Hit += ConquestFshipName_Click;
            ConquestFshipMembers = (HudStaticText)view["ConquestFshipMembers"];
            ConquestFshipMembers.Hit += ConquestFshipMembers_Click;
            ConquestFshipLocation = (HudStaticText)view["ConquestFshipLocation"];
            ConquestFshipLocation.Hit += ConquestFshipLocation_Click;

            ConquestFshipRefresh = (HudButton)view["ConquestFshipRefresh"];
            ConquestFshipRefresh.Hit += ConquestFshipRefresh_Hit;
            ConquestFshipJoin = (HudButton)view["ConquestFshipJoin"];
            ConquestFshipJoin.Hit += ConquestFshipJoin_Hit;
            ConquestFshipJoin.Visible = false;
            ConquestFshipQuit = (HudButton)view["ConquestFshipQuit"];
            ConquestFshipQuit.Hit += ConquestFshipQuit_Hit;
            ConquestFshipQuit.Visible = false;

            ConquestFshipList = (HudList)view["ConquestFshipList"];
            ConquestFshipList.Click += ConquestFshipList_Click;
            ConquestFshipList.ClearRows();
        }

        private void DisposeConquestFship()
        {
            ConquestFshipSortIcon.Hit -= ConquestFshipName_Click;
            ConquestFshipName.Hit -= ConquestFshipName_Click;
            ConquestFshipMembers.Hit -= ConquestFshipMembers_Click;
            ConquestFshipLocation.Hit -= ConquestFshipLocation_Click;
            ConquestFshipRefresh.Hit -= ConquestFshipRefresh_Hit;
            ConquestFshipJoin.Hit -= ConquestFshipJoin_Hit;
            ConquestFshipQuit.Hit -= ConquestFshipQuit_Hit;
            ConquestFshipList.Click -= ConquestFshipList_Click;
        }

        public void UpdateConquestFship()
        {
            // The recruiting list only exists on Conquest. Off-server, show "None" and hide the
            // list, buttons, and column headers.
            bool available = Server.IsConquest;

            ConquestFshipSort.Visible = available;
            ConquestFshipName.Visible = available;
            ConquestFshipMembers.Visible = available;
            ConquestFshipLocation.Visible = available;
            ConquestFshipList.Visible = available;
            ConquestFshipRefresh.Visible = available;

            if (!available)
            {
                ConquestFshipText.Text = "None";
                ConquestFshipJoin.Visible = false;
                ConquestFshipQuit.Visible = false;
                return;
            }

            // Lazy-load the first time the tab is shown instead of refreshing on login.
            if (!ConquestFship.Ran) { ConquestFship.Refresh(); }

            ConquestFshipText.Text = Fellowship.IsInFellowship()
                ? $"Fellowship: {Fellowship.Name()}"
                : "No Fellowship";

            UpdateConquestFshipList();
            RefreshFshipButtons();
        }

        private void UpdateConquestFshipList()
        {
            List<ConquestFship> fships = ConquestFship.Sorted();
            List<int> columns = new List<int> { FshipColName, FshipColMembers, FshipColLocation };

            for (int x = 0; x < fships.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestFshipList.RowCount)
                    ? ConquestFshipList.AddRow()
                    : ConquestFshipList[x];

                bool selected = !string.IsNullOrEmpty(fshipSelectedLeader)
                    && fships[x].Leader == fshipSelectedLeader;
                AssignSelected(row, selected, columns);

                ((HudStaticText)row[FshipColName]).Text = fships[x].Name;
                ((HudStaticText)row[FshipColMembers]).Text = fships[x].Members;
                ((HudStaticText)row[FshipColLocation]).Text = fships[x].Location;
                ((HudStaticText)row[FshipColLeader]).Text = fships[x].Leader;
            }

            while (ConquestFshipList.RowCount > fships.Count)
            {
                ConquestFshipList.RemoveRow(ConquestFshipList.RowCount - 1);
            }
        }

        // Join shows only with a valid selection; Quit only while in a fellowship. Also drops a
        // stale selection whose fellowship has left the recruiting list.
        private void RefreshFshipButtons()
        {
            bool hasSelection = !string.IsNullOrEmpty(fshipSelectedLeader)
                && ConquestFship.All.Any(f => f.Leader == fshipSelectedLeader);
            if (!hasSelection) { fshipSelectedLeader = ""; }

            bool inFellowship = Fellowship.IsInFellowship();

            // Join and Quit share the same spot (only one shows at a time): Join to pick a
            // fellowship when you have none, Quit once you're in one.
            ConquestFshipJoin.Visible = hasSelection && !inFellowship;
            ConquestFshipQuit.Visible = inFellowship;
        }

        // Reissues "/fship list" so the server reprints the recruiting fellowships, which the chat
        // handler reparses into ConquestFship. The list refreshes on the next tick.
        private void ConquestFshipRefresh_Hit(object sender, EventArgs e)
        {
            ConquestFship.Refresh();
        }

        // Select the clicked fellowship: highlight its row and reveal the Join button.
        private void ConquestFshipList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= ConquestFshipList.RowCount) { return; }

            fshipSelectedLeader = ((HudStaticText)ConquestFshipList[row][FshipColLeader]).Text;
            UpdateConquestFshipList();
            RefreshFshipButtons();
        }

        // Join the selected fellowship by telling its leader "xp" (the server's join mechanism).
        private void ConquestFshipJoin_Hit(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fshipSelectedLeader)) { return; }

            Util.Command($"/tell {fshipSelectedLeader}, xp");
            Util.Chat($"Asking {fshipSelectedLeader} to join their fellowship...", Util.ColorPink);
        }

        private void ConquestFshipQuit_Hit(object sender, EventArgs e)
        {
            Fellowship.Quit();
        }

        private void ConquestFshipName_Click(object sender, EventArgs e)
        {
            ConquestFship.CurrentSort = (ConquestFship.CurrentSort == ConquestFship.SortType.NameAsc)
                ? ConquestFship.SortType.NameDesc
                : ConquestFship.SortType.NameAsc;
            UpdateConquestFshipList();
        }

        private void ConquestFshipMembers_Click(object sender, EventArgs e)
        {
            ConquestFship.CurrentSort = (ConquestFship.CurrentSort == ConquestFship.SortType.MembersAsc)
                ? ConquestFship.SortType.MembersDesc
                : ConquestFship.SortType.MembersAsc;
            UpdateConquestFshipList();
        }

        private void ConquestFshipLocation_Click(object sender, EventArgs e)
        {
            ConquestFship.CurrentSort = (ConquestFship.CurrentSort == ConquestFship.SortType.LocationAsc)
                ? ConquestFship.SortType.LocationDesc
                : ConquestFship.SortType.LocationAsc;
            UpdateConquestFshipList();
        }
    }
}
