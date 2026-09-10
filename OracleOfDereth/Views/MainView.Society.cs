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
            if (!QuestState.HasRequestedRefresh) { QuestFlag.Refresh(); }
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
            int index = 0;

            if (name == "None")
            {
                UpdateStatusRow(index++, "Requires", "Level 180+");
                UpdateStatusRow(index++, "Start with", "Investigating the Societies quest");
            }

            if (name != "None")
            {
                int value = Society.GetRankValue();
                int max = Society.GetRankMax();
                int dailyLimit = Society.GetDailyLimit();
                int ribbonsToday = Society.GetRibbonsToday();
                int ribbonsToNext = Society.GetRibbonsToNextRank();
                string nextRankName = Society.GetNextRankName();

                // Value can briefly exceed the cap (95-100 etc); never show more than max.
                UpdateStatusRow(index++, "Rank Progress", $"{Math.Min(Society.GetRankProgress(), max)} / {max} ribbons");

                // Master (1001+) trades ribbons for tokens with no per-day cap (the
                // .es exchange branch never touches the daily counter), so the count
                // and limit are both meaningless at that rank.
                if (value > 1000)
                {
                    UpdateStatusRow(index++, "Ribbons Today", "Unlimited");
                }
                else
                {
                    UpdateStatusRow(index++, "Ribbons Today", $"{ribbonsToday} / {dailyLimit}");
                }

                if (ribbonsToNext > 0)
                {
                    UpdateStatusRow(index++, "Ribbons to " + nextRankName, ribbonsToNext.ToString());
                }

                // Status / what to do next
                string status = GetSocietyStatusText(value);
                if (status.Length > 0)
                {
                    UpdateStatusRow(index++, "Status", status);
                }
            }
            while (SocietyStatusList.RowCount > index) { SocietyStatusList.RemoveRow(SocietyStatusList.RowCount - 1); }
        }

        private void UpdateStatusRow(int index, string key, string value)
        {
            var row = index < SocietyStatusList.RowCount ? SocietyStatusList[index] : SocietyStatusList.AddRow();
            SetText(row, 0, key);
            SetText(row, 1, value);
        }

        // Ribbons are turned in to raise rank until the cap (test range) is hit.
        // At the cap you cannot turn in ribbons and must take the rank test;
        // after passing you return to be promoted, then ribbons reopen for the
        // next tier. Breakpoints per society NPC weenie 38232 emote script.
        private string GetSocietyStatusText(int value)
        {
            if (value >= 1001) return "Turn in 50 ribbons for Trade Token";
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
            List<SocietyQuest> societyQuests = SocietyQuest.VisibleQuests();

            // Rows map 1:1 to the visible quests (never skip a row) so row index == list index
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
                    SetText(row, 1, "");
                    SetText(row, 2, "");
                    SetText(row, 3, "");
                    SetText(row, 4, "");
                    SetText(row, 5, "");
                    continue;
                }

                if (societyQuest.IsHeader()) {
                    AssignImage((HudPictureBox)row[0], 0);
                    SetText(row, 1, societyQuest.Name.Replace("Rank: ", ""));
                    SetText(row, 2, "");
                    SetText(row, 3, "");
                    SetText(row, 4, "");
                    SetText(row, 5, "");
                    continue;
                }

                // Quest row
                QuestFlag.QuestFlags.TryGetValue(societyQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], societyQuest.IsComplete());
                SetText(row, 1, societyQuest.Name);
                SetText(row, 2, societyQuest.Area);

                if (societyQuest.IsRankTest()) {
                    SetText(row, 3, societyQuest.IsComplete() ? "completed" : "ready");
                    SetText(row, 4, "");
                } else if (questFlag == null) {
                    SetText(row, 3, "ready");
                    SetText(row, 4, "");
                } else if (societyQuest.IsOneTime()) {
                    SetText(row, 3, "completed");
                    SetText(row, 4, "");
                } else {
                    SetText(row, 3, questFlag.NextAvailable());
                    SetText(row, 4, $"{questFlag.Solves}");
                }

                SetText(row, 5, societyQuest.Flag);
            }

            // Filtering can shrink the visible count (e.g. after joining a society);
            // drop any leftover trailing rows so they don't linger.
            while (SocietyList.RowCount > societyQuests.Count) { SocietyList.RemoveRow(SocietyList.RowCount - 1); }
        }

        void SocietyList_Click(object sender, int row, int col)
        {
            List<SocietyQuest> societyQuests = SocietyQuest.VisibleQuests();
            if (row < 0 || row >= societyQuests.Count) { return; }

            SocietyQuest societyQuest = societyQuests[row];
            if (!societyQuest.IsQuest()) { return; }

            QuestFlag.QuestFlags.TryGetValue(societyQuest.Flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && societyQuest.Url.Length > 0) {
                Util.ThinkQuestUrl($"{societyQuest.Name}: {societyQuest.Url}", societyQuest.Url);
            }

            // Quest Hint
            if ((col == 1 || col == 2) && societyQuest.Hint.Length > 0) {
                Util.ThinkQuestDirections($"{societyQuest.Name}: {societyQuest.Hint}", societyQuest.Hint);
            }

            // Quest Flag
            if (col >= 3) {
                if (societyQuest.IsRankTest()) {
                    string status = societyQuest.IsComplete() ? "Completed" : "Not completed";
                    Util.Chat($"{societyQuest.Name}: {status}", Util.ColorPink);
                } else if (questFlag == null) {
                    Util.Chat($"{societyQuest.Flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
