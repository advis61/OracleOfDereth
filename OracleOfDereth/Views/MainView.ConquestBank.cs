using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText ConquestBankText { get; private set; }
        public HudStaticText ConquestBankName { get; private set; }
        public HudStaticText ConquestBankValue { get; private set; }
        public HudList ConquestBankList { get; private set; }
        public HudButton ConquestBankRefresh { get; private set; }

        private void InitConquestBank()
        {
            ConquestBankText = (HudStaticText)view["ConquestBankText"];
            ConquestBankText.FontHeight = 10;
            ConquestBankName = (HudStaticText)view["ConquestBankName"];
            ConquestBankValue = (HudStaticText)view["ConquestBankValue"];
            ConquestBankList = (HudList)view["ConquestBankList"];
            ConquestBankRefresh = (HudButton)view["ConquestBankRefresh"];
            ConquestBankRefresh.Hit += ConquestBankRefresh_Hit;
            ConquestBankList.ClearRows();
        }

        private void DisposeConquestBank()
        {
            ConquestBankRefresh.Hit -= ConquestBankRefresh_Hit;
        }

        public void UpdateConquestBank()
        {
            // Bank balances only exist on Conquest. Off-server, show "None" and hide the list,
            // refresh button, and column headers.
            bool available = Server.IsConquest;

            ConquestBankName.Visible = available;
            ConquestBankValue.Visible = available;
            ConquestBankList.Visible = available;
            ConquestBankRefresh.Visible = available;

            if (!available)
            {
                ConquestBankText.Text = "None";
                return;
            }

            // Lazy-load the first time the tab is shown instead of refreshing on login.
            if (!ConquestBank.Ran) { ConquestBank.Refresh(); }

            ConquestBankText.Text = "Bank Balances";
            UpdateConquestBankList();
        }

        private void UpdateConquestBankList()
        {
            List<ConquestBank> balances = ConquestBank.All.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();

            for (int x = 0; x < balances.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestBankList.RowCount)
                    ? ConquestBankList.AddRow()
                    : ConquestBankList[x];

                ((HudStaticText)row[0]).Text = balances[x].Name;
                ((HudStaticText)row[1]).Text = balances[x].Value;
            }

            while (ConquestBankList.RowCount > balances.Count)
            {
                ConquestBankList.RemoveRow(ConquestBankList.RowCount - 1);
            }
        }

        // Reissues "/b" so the server reprints balances, which the chat handler reparses into
        // ConquestBank. The list refreshes on the next tick.
        private void ConquestBankRefresh_Hit(object sender, EventArgs e)
        {
            ConquestBank.Refresh();
        }
    }
}
