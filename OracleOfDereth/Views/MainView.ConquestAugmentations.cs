using Decal.Adapter;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText ConquestAugsText { get; private set; }
        public HudStaticText ConquestAugsName { get; private set; }
        public HudStaticText ConquestAugsLevel { get; private set; }
        public HudList ConquestAugsList { get; private set; }
        public HudButton ConquestAugsRefresh { get; private set; }

        private void InitConquestAugmentations()
        {
            ConquestAugsText = (HudStaticText)view["ConquestAugsText"];
            ConquestAugsText.FontHeight = 10;
            ConquestAugsName = (HudStaticText)view["ConquestAugsName"];
            ConquestAugsLevel = (HudStaticText)view["ConquestAugsLevel"];
            ConquestAugsList = (HudList)view["ConquestAugsList"];
            ConquestAugsRefresh = (HudButton)view["ConquestAugsRefresh"];
            ConquestAugsRefresh.Hit += ConquestAugsRefresh_Hit;
            ConquestAugsList.ClearRows();
        }

        private void DisposeConquestAugmentations()
        {
            ConquestAugsRefresh.Hit -= ConquestAugsRefresh_Hit;
        }

        public void UpdateConquestAugmentations()
        {
            // The advanced augs only exist on Conquest. Off-server, show "None" and hide the
            // list, refresh button, and column headers.
            bool available = CoreManager.Current.CharacterFilter.Server == "Conquest";

            ConquestAugsName.Visible = available;
            ConquestAugsLevel.Visible = available;
            ConquestAugsList.Visible = available;
            ConquestAugsRefresh.Visible = available;

            if (!available)
            {
                ConquestAugsText.Text = "None";
                return;
            }

            UpdateConquestAugsList();
        }

        private void UpdateConquestAugsList()
        {
            List<ConquestAugmentation> augs = ConquestAugmentation.All;

            ConquestAugsText.Text = $"Total Conquest Augs: {ConquestAugmentation.Total}";

            for (int x = 0; x < augs.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestAugsList.RowCount)
                    ? ConquestAugsList.AddRow()
                    : ConquestAugsList[x];

                ((HudStaticText)row[0]).Text = augs[x].Name;
                ((HudStaticText)row[1]).Text = augs[x].Count.ToString();
            }

            while (ConquestAugsList.RowCount > augs.Count)
            {
                ConquestAugsList.RemoveRow(ConquestAugsList.RowCount - 1);
            }
        }

        // Reissues "/augs" so the server reprints the levels, which the chat handler reparses
        // into ConquestAugmentation. The list refreshes on the next tick.
        private void ConquestAugsRefresh_Hit(object sender, EventArgs e)
        {
            ConquestAugmentation.Refresh();
        }
    }
}
