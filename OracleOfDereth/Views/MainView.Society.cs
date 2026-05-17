using System;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText SocietyText { get; private set; }
        public HudButton SocietyRefresh { get; private set; }
        public HudList SocietyInitiationList { get; private set; }
        public HudList SocietyStatusList { get; private set; }
        public HudList SocietyInitiateList { get; private set; }
        public HudList SocietyKnightList { get; private set; }
        public HudList SocietyLordList { get; private set; }
        public HudList SocietyMasterList { get; private set; }

        private void InitSociety()
        {
            SocietyText = (HudStaticText)view["SocietyText"];
            SocietyText.FontHeight = 10;

            SocietyRefresh = (HudButton)view["SocietyRefresh"];
            SocietyRefresh.Hit += QuestFlagsRefresh_Hit;

            SocietyInitiationList = (HudList)view["SocietyInitiationList"];
            SocietyInitiationList.ClearRows();

            SocietyStatusList = (HudList)view["SocietyStatusList"];
            SocietyStatusList.ClearRows();

            SocietyInitiateList = (HudList)view["SocietyInitiateList"];
            SocietyInitiateList.ClearRows();

            SocietyKnightList = (HudList)view["SocietyKnightList"];
            SocietyKnightList.ClearRows();

            SocietyLordList = (HudList)view["SocietyLordList"];
            SocietyLordList.ClearRows();

            SocietyMasterList = (HudList)view["SocietyMasterList"];
            SocietyMasterList.ClearRows();
        }

        private void DisposeSociety()
        {
            SocietyRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateSociety()
        {
            UpdateSocietyStatus();
        }

        private void UpdateSocietyStatus()
        {
            string name = Society.GetSocietyName();
            string rankName = Society.GetRankName();

            // Header
            if (name == "None")
            {
                SocietyText.Text = "No Society";
            }
            else
            {
                SocietyText.Text = $"{name} - {rankName}";
            }

            // Status key-value list
            SocietyStatusList.ClearRows();

            if (name != "None")
            {
                int value = Society.GetRankValue();
                int max = Society.GetRankMax();
                int dailyLimit = Society.GetDailyLimit();
                int ribbonsToday = Society.GetRibbonsToday();
                int ribbonsToNext = Society.GetRibbonsToNextRank();
                string nextRankName = Society.GetNextRankName();

                AddStatusRow("Rank Progress", $"{value} / {max} ribbons");
                AddStatusRow("Ribbons Today", $"{ribbonsToday} / {dailyLimit}");

                if (ribbonsToNext > 0)
                {
                    AddStatusRow("Ribbons to " + nextRankName, ribbonsToNext.ToString());
                }

                // Status / what to do next
                string status = GetSocietyStatusText(value, rankName);
                if (status.Length > 0)
                {
                    AddStatusRow("Status", status);
                }
            }
        }

        private void AddStatusRow(string key, string value)
        {
            HudList.HudListRowAccessor row = SocietyStatusList.AddRow();
            ((HudStaticText)row[0]).Text = key;
            ((HudStaticText)row[1]).Text = value;
        }

        private string GetSocietyStatusText(int value, string rankName)
        {
            if (value >= 1001) return "Grand Master - Trade 50 ribbons for Trade Token";
            if (value >= 998) return "Master rank achieved";
            if (value >= 995) return "Take the Master Test";
            if (value >= 601) return "Turn in ribbons or take Master Test at 995";
            if (value >= 301) return "Turn in ribbons or take Lord Test at 595";
            if (value >= 101) return "Turn in ribbons or take Knight Test at 295";
            if (value >= 1) return "Turn in ribbons or take Adept Test at 95";
            return "";
        }
    }
}
