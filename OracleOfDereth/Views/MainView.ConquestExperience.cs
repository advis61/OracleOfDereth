using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // Character Level + XP-to-next-level summary (two rows).
        public HudStaticText ConquestCharacterText { get; private set; } // big "Character" section label
        public HudList ConquestSummaryList { get; private set; }

        // XP Bonuses ("/bonus").
        public HudStaticText ConquestBonusText { get; private set; }
        public HudStaticText ConquestBonusName { get; private set; }
        public HudStaticText ConquestBonusValue { get; private set; }
        public HudList ConquestBonusList { get; private set; }

        public HudButton ConquestExperienceRefresh { get; private set; }

        private void InitConquestExperience()
        {
            ConquestCharacterText = (HudStaticText)view["ConquestCharacterText"];
            ConquestCharacterText.FontHeight = 10;

            ConquestExperienceRefresh = (HudButton)view["ConquestExperienceRefresh"];
            ConquestExperienceRefresh.Hit += ConquestExperienceRefresh_Hit;

            ConquestSummaryList = (HudList)view["ConquestSummaryList"];
            ConquestSummaryList.ClearRows();

            ConquestBonusText = (HudStaticText)view["ConquestBonusText"];
            ConquestBonusText.FontHeight = 10;
            ConquestBonusName = (HudStaticText)view["ConquestBonusName"];
            ConquestBonusValue = (HudStaticText)view["ConquestBonusValue"];
            ConquestBonusList = (HudList)view["ConquestBonusList"];
            ConquestBonusList.ClearRows();
        }

        private void DisposeConquestExperience()
        {
            ConquestExperienceRefresh.Hit -= ConquestExperienceRefresh_Hit;
        }

        public void UpdateConquestExperience()
        {
            // Same rule as the Augs tab: this data only exists on Conquest. Off-server the section
            // label carries the "None" so the tab isn't blank, and everything else hides.
            bool available = Server.IsConquest;

            ConquestCharacterText.Visible = true;
            ConquestSummaryList.Visible = available;
            ConquestBonusText.Visible = available;
            ConquestBonusName.Visible = available;
            ConquestBonusValue.Visible = available;
            ConquestBonusList.Visible = available;
            ConquestExperienceRefresh.Visible = available;

            if (!available)
            {
                ConquestCharacterText.Text = "None";
                return;
            }

            ConquestCharacterText.Text = "Character";
            ConquestBonusText.Text = "XP Bonuses";

            // While the tab is on screen, keep the bonuses current on their own (throttled inside
            // RefreshIfStale). Coming back to the tab shows fresh data without hitting Refresh. The
            // view.Visible gate matters because Update() still ticks this method while the plugin
            // window is closed — we don't want to pull them then.
            if (view.Visible && ConquestBonus.RefreshIfStale()) { FlashButton(ConquestExperienceRefresh); }

            UpdateConquestSummaryList();
            UpdateConquestBonusList();
        }

        private void UpdateConquestSummaryList()
        {
            while (ConquestSummaryList.RowCount < 2) { ConquestSummaryList.AddRow(); }

            HudList.HudListRowAccessor level = ConquestSummaryList[0];
            ((HudStaticText)level[0]).Text = CharacterXp.LevelLabel();
            ((HudStaticText)level[1]).Text = CharacterXp.ProgressText();

            HudList.HudListRowAccessor enl = ConquestSummaryList[1];
            ((HudStaticText)enl[0]).Text = CharacterXp.EnlightenmentLabel();
            ((HudStaticText)enl[1]).Text = CharacterXp.EnlightenmentProgressText();

            while (ConquestSummaryList.RowCount > 2)
            {
                ConquestSummaryList.RemoveRow(ConquestSummaryList.RowCount - 1);
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

        // Reissues "/bonus" so the server reprints the block, which the chat handler reparses into
        // ConquestBonus. The list refreshes on the next tick.
        private void ConquestExperienceRefresh_Hit(object sender, EventArgs e)
        {
            ConquestBonus.Refresh();
            FlashButton(ConquestExperienceRefresh);
        }
    }
}
