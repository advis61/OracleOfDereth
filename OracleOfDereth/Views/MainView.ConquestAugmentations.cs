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
        public HudStaticText ConquestAugsCost { get; private set; }
        public HudStaticText ConquestAugsEffect { get; private set; }
        public HudList ConquestAugsList { get; private set; }
        public HudButton ConquestAugsRefresh { get; private set; }
        public HudButton ConquestAugsCopy { get; private set; }

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
            ConquestAugsCost = (HudStaticText)view["ConquestAugsCost"];
            ConquestAugsEffect = (HudStaticText)view["ConquestAugsEffect"];
            ConquestAugsList = (HudList)view["ConquestAugsList"];
            ConquestAugsRefresh = (HudButton)view["ConquestAugsRefresh"];
            ConquestAugsRefresh.Hit += ConquestAugsRefresh_Hit;
            ConquestAugsCopy = (HudButton)view["ConquestAugsCopy"];
            ConquestAugsCopy.Hit += ConquestAugsCopy_Hit;
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
            ConquestAugsCopy.Hit -= ConquestAugsCopy_Hit;
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
            ConquestAugsCost.Visible = available;
            ConquestAugsEffect.Visible = available;
            ConquestAugsList.Visible = available;
            ConquestAugsRefresh.Visible = available;
            ConquestAugsCopy.Visible = available;
            ConquestBonusText.Visible = available;
            ConquestBonusName.Visible = available;
            ConquestBonusValue.Visible = available;
            ConquestBonusList.Visible = available;

            if (!available)
            {
                ConquestAugsText.Text = "None";
                return;
            }

            // While the tab is actually on screen, keep the augs and bonuses current on their own
            // (throttled inside RefreshIfStale). Coming back to the tab shows fresh data without
            // hitting Refresh. The view.Visible gate matters because Update() still ticks this
            // method while the plugin window is closed — we don't want to pull them then.
            if (view.Visible)
            {
                // Both share the one Refresh button, so either pull going out acknowledges on it.
                // Not short-circuited — both still need the chance to pull.
                bool pulled = ConquestAugmentation.RefreshIfStale();
                pulled |= ConquestBonus.RefreshIfStale();
                if (pulled) { FlashButton(ConquestAugsRefresh); }
            }

            ConquestBonusText.Text = "XP Bonuses";

            UpdateConquestSummaryList();
            UpdateConquestAugsList();
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

        private void UpdateConquestAugsList()
        {
            List<ConquestAugmentation> augs = ConquestAugmentation.All;

            // The total lives in the big title at the top of the tab.
            ConquestAugsText.Text = $"Conquest Augs: {ConquestAugmentation.Total}";

            for (int x = 0; x < augs.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestAugsList.RowCount)
                    ? ConquestAugsList.AddRow()
                    : ConquestAugsList[x];

                ((HudStaticText)row[0]).Text = augs[x].Name;
                ((HudStaticText)row[1]).Text = augs[x].Count.ToString();
                ((HudStaticText)row[2]).Text = augs[x].Effect();
                ((HudStaticText)row[3]).Text = augs[x].NextCostText();
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
            FlashButton(ConquestAugsRefresh);
        }

        // Thinks the aug summary (to self, or fellowship/alliance with Shift/Alt) and copies it.
        // Appends the Quest Bonus line from "/bonus", e.g. "Quest Bonus: 14.18% (1,418 quests)".
        private void ConquestAugsCopy_Hit(object sender, EventArgs e)
        {
            string summary = ConquestAugmentation.Summary();

            ConquestBonus quest = ConquestBonus.Get("Quest");
            if (quest != null && quest.Value.Length > 0) { summary += $", Quest Bonus: {quest.Value}"; }

            Util.Think(summary);
            Util.ClipboardCopy(summary);
        }
    }
}
