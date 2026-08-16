using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText ConquestAugsText { get; private set; } // off-server "None" indicator only
        public HudStaticText ConquestAugsName { get; private set; }
        public HudStaticText ConquestAugsLevel { get; private set; }
        public HudStaticText ConquestAugsCost { get; private set; }
        public HudStaticText ConquestAugsEffect { get; private set; }
        public HudList ConquestAugsList { get; private set; }
        public HudButton ConquestAugsRefresh { get; private set; }
        public HudButton ConquestAugsCopy { get; private set; }

        // Enlightenment augs ("/enl augs"), listed below the advanced augs on the same tab.
        public HudStaticText ConquestEnlAugsText { get; private set; }
        public HudStaticText ConquestEnlAugsName { get; private set; }
        public HudStaticText ConquestEnlAugsValue { get; private set; }
        public HudStaticText ConquestEnlAugsCost { get; private set; }
        public HudList ConquestEnlAugsList { get; private set; }

        private void InitConquestAugmentations()
        {
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

            ConquestEnlAugsText = (HudStaticText)view["ConquestEnlAugsText"];
            ConquestEnlAugsText.FontHeight = 10;
            ConquestEnlAugsName = (HudStaticText)view["ConquestEnlAugsName"];
            ConquestEnlAugsValue = (HudStaticText)view["ConquestEnlAugsValue"];
            ConquestEnlAugsCost = (HudStaticText)view["ConquestEnlAugsCost"];
            ConquestEnlAugsList = (HudList)view["ConquestEnlAugsList"];
            ConquestEnlAugsList.ClearRows();
        }

        private void DisposeConquestAugmentations()
        {
            ConquestAugsRefresh.Hit -= ConquestAugsRefresh_Hit;
            ConquestAugsCopy.Hit -= ConquestAugsCopy_Hit;
        }

        public void UpdateConquestAugmentations()
        {
            // The advanced augs only exist on Conquest. Off-server, show "None" and hide the list,
            // the buttons, and the column headers.
            bool available = Server.IsConquest;

            ConquestAugsText.Visible = true; // big title on-server ("Total Custom Augs"), "None" off-server
            ConquestAugsName.Visible = available;
            ConquestAugsLevel.Visible = available;
            ConquestAugsCost.Visible = available;
            ConquestAugsEffect.Visible = available;
            ConquestAugsList.Visible = available;
            ConquestAugsRefresh.Visible = available;
            ConquestAugsCopy.Visible = available;
            ConquestEnlAugsText.Visible = available;
            ConquestEnlAugsName.Visible = available;
            ConquestEnlAugsValue.Visible = available;
            ConquestEnlAugsCost.Visible = available;
            ConquestEnlAugsList.Visible = available;

            if (!available)
            {
                ConquestAugsText.Text = "None";
                return;
            }

            // While the tab is actually on screen, keep both aug lists current on their own
            // (throttled inside RefreshIfStale). Coming back to the tab shows fresh data without
            // hitting Refresh. The view.Visible gate matters because Update() still ticks this
            // method while the plugin window is closed — we don't want to pull them then.
            if (view.Visible)
            {
                // Both share the one Refresh button, so either pull going out acknowledges on it.
                // Not short-circuited — both still need the chance to pull.
                bool pulled = ConquestAugmentation.RefreshIfStale();
                pulled |= ConquestEnlAugmentation.RefreshIfStale();
                if (pulled) { FlashButton(ConquestAugsRefresh); }
            }

            UpdateConquestAugsList();
            UpdateConquestEnlAugsList();
        }

        private void UpdateConquestEnlAugsList()
        {
            List<ConquestEnlAugmentation> augs = ConquestEnlAugmentation.All;

            // The total lives in the section header, the same way the advanced augs' does.
            ConquestEnlAugsText.Text = $"Enlightenment Augs: {ConquestEnlAugmentation.Total}";

            for (int x = 0; x < augs.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestEnlAugsList.RowCount)
                    ? ConquestEnlAugsList.AddRow()
                    : ConquestEnlAugsList[x];

                ((HudStaticText)row[0]).Text = augs[x].Name;
                ((HudStaticText)row[1]).Text = augs[x].Value;
                ((HudStaticText)row[2]).Text = augs[x].Cost;
            }

            while (ConquestEnlAugsList.RowCount > augs.Count)
            {
                ConquestEnlAugsList.RemoveRow(ConquestEnlAugsList.RowCount - 1);
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

        // Reissues "/augs" and "/enl augs" so the server reprints both blocks, which the chat
        // handler recaptures into ConquestAugmentation / ConquestEnlAugmentation. The lists refresh
        // on the next tick. "/bonus" belongs to the Experience tab's own Refresh.
        private void ConquestAugsRefresh_Hit(object sender, EventArgs e)
        {
            ConquestAugmentation.Refresh();
            ConquestEnlAugmentation.Refresh();
            FlashButton(ConquestAugsRefresh);
        }

        // Thinks the aug summary (to self, or fellowship/alliance with Shift/Alt) and copies it.
        // Appends the Quest Bonus line from "/bonus", e.g. "Quest Bonus: 14.18% (1,418 quests)" —
        // whatever the Experience tab last pulled, and omitted entirely until it has pulled once.
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
