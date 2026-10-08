using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        private HudStaticText ArenaHeading;
        private HudButton ArenaRefresh;
        private HudStaticText[] ArenaHeaders;
        private HudList ArenaList;
        private string ArenaHeadingText;
        private string ArenaSelectedName;
        private static readonly List<int> ArenaColumns = new List<int> { 0, 1, 2, 3, 4, 5, 6 };

        private void InitArena()
        {
            ArenaHeading = (HudStaticText)view["TopArenaText"];
            ArenaRefresh = (HudButton)view["TopArenaRefresh"];
            ArenaHeaders = new[] { "Rank", "Name", "Points", "Matches", "Wins", "Draws", "Losses" }
                .Select(name => (HudStaticText)view["TopArena" + name]).ToArray();
            foreach (var header in ArenaHeaders.Skip(2))
                header.TextAlignment = VirindiViewService.WriteTextFormats.Center;
            ArenaList = (HudList)view["TopArenaList"];
            ArenaHeadingText = ArenaHeading.Text;
            ArenaHeading.FontHeight = 10;
            ArenaRefresh.Hit += ArenaRefresh_Hit;
            ArenaList.Click += ArenaList_Click;
            ArenaList.ClearRows();
        }

        private void DisposeArena()
        {
            if (ArenaRefresh != null) ArenaRefresh.Hit -= ArenaRefresh_Hit;
            if (ArenaList != null) ArenaList.Click -= ArenaList_Click;
            ArenaHeading = null;
            ArenaRefresh = null;
            ArenaHeaders = null;
            ArenaList = null;
        }

        private void UpdateArena()
        {
            // Arena is appended after the existing /top tabs, without entering TopPages.
            if (TopViewNotebook.CurrentTab != TopBoard.All.Count) return;
            bool available = Server.IsConquest;
            ArenaRefresh.Visible = available;
            ArenaList.Visible = available;
            foreach (var header in ArenaHeaders) header.Visible = available;
            SetText(ArenaHeading, available ? ArenaHeadingText : "None");
            if (!available) return;
            if (view.Visible && ArenaBoard.RefreshIfStale()) FlashButton(ArenaRefresh);
            UpdateArenaList();
        }

        private void UpdateArenaList()
        {
            var players = ArenaBoard.Sorted();
            for (int x = 0; x < players.Count; x++)
            {
                var row = x >= ArenaList.RowCount ? ArenaList.AddRow() : ArenaList[x];
                SetText(row, 0, $"{players[x].Rank}");
                SetText(row, 1, players[x].Name);
                for (int metric = 0; metric < 5; metric++)
                {
                    ((HudStaticText)row[metric + 2]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    SetText(row, metric + 2, players[x].Metrics[metric]);
                }
                AssignSelected(row, players[x].Name == ArenaSelectedName, ArenaColumns);
            }
            while (ArenaList.RowCount > players.Count) ArenaList.RemoveRow(ArenaList.RowCount - 1);
        }

        private void ArenaRefresh_Hit(object sender, EventArgs e)
        {
            if (ArenaBoard.Refresh()) FlashButton(ArenaRefresh);
        }

        private void ArenaList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= ArenaList.RowCount) return;
            string name = ((HudStaticText)ArenaList[row][1]).Text;
            if (string.IsNullOrWhiteSpace(name)) return;
            ArenaSelectedName = name;
            UpdateArenaList();
            if (name.EndsWith(" (You)", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - " (You)".Length);
            Util.Command("/who " + name);
        }
    }
}
