using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // Character Level + XP-to-next-level summary (single row), shown below the augs list.
        public HudStaticText ConquestCharacterText { get; private set; } // big "Character" section label
        public HudList ConquestSummaryList { get; private set; }

        public HudStaticText ConquestAugsText { get; private set; } // off-server "None" indicator only
        public HudStaticText ConquestAugsName { get; private set; }
        public HudStaticText ConquestAugsLevel { get; private set; }
        public HudStaticText ConquestAugsEffect { get; private set; }
        public HudList ConquestAugsList { get; private set; }
        public HudButton ConquestAugsRefresh { get; private set; }

        // XP Bonuses ("/bonus"), shown in a list below the augs list on the same tab.
        public HudStaticText ConquestBonusText { get; private set; }
        public HudStaticText ConquestBonusName { get; private set; }
        public HudStaticText ConquestBonusValue { get; private set; }
        public HudList ConquestBonusList { get; private set; }

        private void InitConquestAugmentations()
        {
            ConquestSummaryList = (HudList)view["ConquestSummaryList"];
            ConquestSummaryList.ClearRows();

            ConquestCharacterText = (HudStaticText)view["ConquestCharacterText"];
            ConquestCharacterText.FontHeight = 10;

            ConquestAugsText = (HudStaticText)view["ConquestAugsText"];
            ConquestAugsText.FontHeight = 10;
            ConquestAugsName = (HudStaticText)view["ConquestAugsName"];
            ConquestAugsLevel = (HudStaticText)view["ConquestAugsLevel"];
            ConquestAugsEffect = (HudStaticText)view["ConquestAugsEffect"];
            ConquestAugsList = (HudList)view["ConquestAugsList"];
            ConquestAugsRefresh = (HudButton)view["ConquestAugsRefresh"];
            ConquestAugsRefresh.Hit += ConquestAugsRefresh_Hit;
            ConquestAugsList.ClearRows();

            ConquestBonusText = (HudStaticText)view["ConquestBonusText"];
            ConquestBonusText.FontHeight = 10;
            ConquestBonusName = (HudStaticText)view["ConquestBonusName"];
            ConquestBonusValue = (HudStaticText)view["ConquestBonusValue"];
            ConquestBonusList = (HudList)view["ConquestBonusList"];
            ConquestBonusList.ClearRows();
        }

        private void DisposeConquestAugmentations()
        {
            ConquestAugsRefresh.Hit -= ConquestAugsRefresh_Hit;
        }

        public void UpdateConquestAugmentations()
        {
            // The advanced augs/bonuses only exist on Conquest. Off-server, show "None" and hide
            // the summary, both lists, the refresh button, and the column headers.
            bool available = Server.IsConquest;

            ConquestAugsText.Visible = true; // big title on-server ("Total Custom Augs"), "None" off-server
            ConquestSummaryList.Visible = available;
            ConquestCharacterText.Visible = available;
            ConquestAugsName.Visible = available;
            ConquestAugsLevel.Visible = available;
            ConquestAugsEffect.Visible = available;
            ConquestAugsList.Visible = available;
            ConquestAugsRefresh.Visible = available;
            ConquestBonusText.Visible = available;
            ConquestBonusName.Visible = available;
            ConquestBonusValue.Visible = available;
            ConquestBonusList.Visible = available;

            if (!available)
            {
                ConquestAugsText.Text = "None";
                return;
            }

            // Lazy-load the first time the tab is shown instead of refreshing on login.
            if (!ConquestAugmentation.Ran) { ConquestAugmentation.Refresh(); }
            if (!ConquestBonus.Ran) { ConquestBonus.Refresh(); }

            ConquestBonusText.Text = "XP Bonuses";

            UpdateConquestSummaryList();
            UpdateConquestAugsList();
            UpdateConquestBonusList();
        }

        private void UpdateConquestSummaryList()
        {
            HudList.HudListRowAccessor row = (ConquestSummaryList.RowCount == 0)
                ? ConquestSummaryList.AddRow()
                : ConquestSummaryList[0];

            ((HudStaticText)row[0]).Text = "Level";
            ((HudStaticText)row[1]).Text = CharacterXp.LevelSummary();

            while (ConquestSummaryList.RowCount > 1)
            {
                ConquestSummaryList.RemoveRow(ConquestSummaryList.RowCount - 1);
            }
        }

        private void UpdateConquestAugsList()
        {
            List<ConquestAugmentation> augs = ConquestAugmentation.All;

            // The total lives in the big title at the top of the tab.
            ConquestAugsText.Text = $"Custom Augs: {ConquestAugmentation.Total}";

            for (int x = 0; x < augs.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestAugsList.RowCount)
                    ? ConquestAugsList.AddRow()
                    : ConquestAugsList[x];

                ((HudStaticText)row[0]).Text = augs[x].Name;
                ((HudStaticText)row[1]).Text = augs[x].Count.ToString();
                ((HudStaticText)row[2]).Text = augs[x].Effect();
            }

            while (ConquestAugsList.RowCount > augs.Count)
            {
                ConquestAugsList.RemoveRow(ConquestAugsList.RowCount - 1);
            }
        }

        private void UpdateConquestBonusList()
        {
            List<ConquestBonus> bonuses = ConquestBonus.All;

            for (int x = 0; x < bonuses.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestBonusList.RowCount)
                    ? ConquestBonusList.AddRow()
                    : ConquestBonusList[x];

                ((HudStaticText)row[0]).Text = bonuses[x].Name;
                ((HudStaticText)row[1]).Text = bonuses[x].Value;
            }

            while (ConquestBonusList.RowCount > bonuses.Count)
            {
                ConquestBonusList.RemoveRow(ConquestBonusList.RowCount - 1);
            }
        }

        // Reissues "/augs" and "/bonus" so the server reprints both, which the chat handler
        // reparses into ConquestAugmentation / ConquestBonus. The lists refresh on the next tick.
        private void ConquestAugsRefresh_Hit(object sender, EventArgs e)
        {
            ConquestAugmentation.Refresh();
            ConquestBonus.Refresh();
        }
    }
}
