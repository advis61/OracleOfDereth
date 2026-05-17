using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText SocietyText { get; private set; }
        public HudButton SocietyRefresh { get; private set; }
        public HudList SocietyStatusList { get; private set; }
        public HudList SocietyList { get; private set; }

        private void InitSociety()
        {
            SocietyText = (HudStaticText)view["SocietyText"];
            SocietyText.FontHeight = 10;

            SocietyRefresh = (HudButton)view["SocietyRefresh"];
            SocietyRefresh.Hit += QuestFlagsRefresh_Hit;

            SocietyStatusList = (HudList)view["SocietyStatusList"];
            SocietyStatusList.ClearRows();

            SocietyList = (HudList)view["SocietyList"];
            SocietyList.Click += SocietyList_Click;
            SocietyList.ClearRows();
        }

        private void DisposeSociety()
        {
            SocietyList.Click -= SocietyList_Click;
            SocietyRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateSociety()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateSocietyStatus();
            UpdateSocietyList();
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

                // Value can briefly exceed the cap (95-100 etc); never show more than max
                AddStatusRow("Rank Progress", $"{Math.Min(value, max)} / {max} ribbons");
                AddStatusRow("Ribbons Today", $"{ribbonsToday} / {dailyLimit}");

                if (ribbonsToNext > 0)
                {
                    AddStatusRow("Ribbons to " + nextRankName, ribbonsToNext.ToString());
                }

                // Status / what to do next
                string status = GetSocietyStatusText(value);
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

        // Ribbons are turned in to raise rank until the cap (test range) is hit.
        // At the cap you cannot turn in ribbons and must take the rank test;
        // after passing you return to be promoted, then ribbons reopen for the
        // next tier. Breakpoints per society NPC weenie 38232 emote script.
        private string GetSocietyStatusText(int value)
        {
            if (value >= 1001) return "Grand Master - Trade 50 ribbons for Trade Token";
            if (value >= 998) return "Return to be promoted to Master";
            if (value >= 995) return "Cap reached - Take the Master Test";
            if (value >= 601) return "Turn in ribbons - Master Test at 995";
            if (value >= 598) return "Return to be promoted to Lord";
            if (value >= 595) return "Cap reached - Take the Lord Test";
            if (value >= 301) return "Turn in ribbons - Lord Test at 595";
            if (value >= 298) return "Return to be promoted to Knight";
            if (value >= 295) return "Cap reached - Take the Knight Test";
            if (value >= 101) return "Turn in ribbons - Knight Test at 295";
            if (value >= 98) return "Return to be promoted to Adept";
            if (value >= 95) return "Cap reached - Take the Adept Test";
            if (value >= 1) return "Turn in ribbons - Adept Test at 95";
            return "";
        }

        private void UpdateSocietyList()
        {
            List<SocietyQuest> societyQuests = SocietyQuest.SocietyQuests.ToList();

            // Rows map 1:1 to SocietyQuests (never skip a row) so row index == list index
            for (int x = 0; x < societyQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= SocietyList.RowCount) {
                    row = SocietyList.AddRow();

                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[4]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = SocietyList[x];
                }

                SocietyQuest societyQuest = societyQuests[x];

                if (societyQuest.IsBlank()) {
                    AssignImage((HudPictureBox)row[0], 0);
                    ((HudStaticText)row[1]).Text = "";
                    ((HudStaticText)row[2]).Text = "";
                    ((HudStaticText)row[3]).Text = "";
                    ((HudStaticText)row[4]).Text = "";
                    ((HudStaticText)row[5]).Text = "";
                    continue;
                }

                if (societyQuest.IsHeader()) {
                    bool reached = societyQuest.RankReached();
                    AssignImage((HudPictureBox)row[0], reached);
                    ((HudStaticText)row[1]).Text = societyQuest.Name;
                    ((HudStaticText)row[2]).Text = "";
                    ((HudStaticText)row[3]).Text = reached ? "completed" : "";
                    ((HudStaticText)row[4]).Text = "";
                    ((HudStaticText)row[5]).Text = "";
                    continue;
                }

                // Repeatable quest row
                QuestFlag.QuestFlags.TryGetValue(societyQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], societyQuest.IsComplete());
                ((HudStaticText)row[1]).Text = societyQuest.Name;
                ((HudStaticText)row[2]).Text = societyQuest.Area;

                if (questFlag == null) {
                    ((HudStaticText)row[3]).Text = "ready";
                    ((HudStaticText)row[4]).Text = "";
                } else {
                    ((HudStaticText)row[3]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[4]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[5]).Text = societyQuest.Flag;
            }
        }

        void SocietyList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= SocietyQuest.SocietyQuests.Count) { return; }

            SocietyQuest societyQuest = SocietyQuest.SocietyQuests[row];
            if (!societyQuest.IsQuest()) { return; }

            QuestFlag.QuestFlags.TryGetValue(societyQuest.Flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && societyQuest.Url.Length > 0) {
                Util.Think($"{societyQuest.Name}: {societyQuest.Url}");
                Util.ClipboardCopy(societyQuest.Url);
            }

            // Quest Hint
            if ((col == 1 || col == 2) && societyQuest.Hint.Length > 0) {
                Util.Think($"{societyQuest.Name}: {societyQuest.Hint}");
            }

            // Quest Flag
            if (col >= 3) {
                if (questFlag == null) {
                    Util.Chat($"{societyQuest.Flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
