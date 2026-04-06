using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText JohnLabel { get; private set; }
        public HudStaticText JohnText { get; private set; }

        public HudFixedLayout JohnListSortComplete { get; private set; }
        public HudPictureBox JohnListSortCompleteIcon { get; private set; }

        public HudStaticText JohnListSortName { get; private set; }
        public HudStaticText JohnListSortReady { get; private set; }
        public HudStaticText JohnListSortSolves { get; private set; }

        public HudList JohnList { get; private set; }
        public HudButton JohnRefresh { get; private set; }

        private void InitJohn()
        {
            JohnText = (HudStaticText)view["JohnText"];
            JohnText.FontHeight = 10;

            JohnRefresh = (HudButton)view["JohnRefresh"];
            JohnRefresh.Hit += QuestFlagsRefresh_Hit;

            JohnList = (HudList)view["JohnList"];
            JohnList.Click += JohnList_Click;
            JohnList.ClearRows();

            JohnListSortCompleteIcon = new HudPictureBox();
            JohnListSortCompleteIcon.Image = IconSort;
            JohnListSortComplete = (HudFixedLayout)view["JohnListSortComplete"];
            JohnListSortComplete.AddControl(JohnListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
            JohnListSortCompleteIcon.Hit += JohnListSortComplete_Click;

            JohnListSortName = (HudStaticText)view["JohnListSortName"];
            JohnListSortName.Hit += JohnListSortName_Click;

            JohnListSortReady = (HudStaticText)view["JohnListSortReady"];
            JohnListSortReady.Hit += JohnListSortReady_Click;

            JohnListSortSolves = (HudStaticText)view["JohnListSortSolves"];
            JohnListSortSolves.Hit += JohnListSortSolves_Click;
        }

        private void DisposeJohn()
        {
            JohnList.Click -= JohnList_Click;
            JohnListSortCompleteIcon.Hit -= JohnListSortComplete_Click;
            JohnListSortName.Hit -= JohnListSortName_Click;
            JohnListSortReady.Hit -= JohnListSortReady_Click;
            JohnListSortSolves.Hit -= JohnListSortSolves_Click;
            JohnRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateJohn()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateJohnList();
        }

        private void UpdateJohnList()
        {
            List<JohnQuest> johnQuests = JohnQuest.JohnQuests.ToList();
            int completed = 0;

            for (int x = 0; x < johnQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= JohnList.RowCount) {
                    row = JohnList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = JohnList[x];
                }

                // Update
                JohnQuest johnQuest = johnQuests[x];
                QuestFlag.QuestFlags.TryGetValue(johnQuest.Flag, out QuestFlag questFlag);

                bool complete = johnQuest.IsComplete();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                ((HudStaticText)row[1]).Text = johnQuest.Name;

                if (questFlag == null) {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                } else {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[4]).Text = johnQuest.Flag;
            }

            // Update Text
            JohnText.Text = $"Legendary John Quests: {completed} completed";
        }

        void JohnList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)JohnList[row][4]).Text;

            JohnQuest johnQuest = JohnQuest.JohnQuests.FirstOrDefault(x => x.Flag == flag);
            if(johnQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && johnQuest.Url.Length > 0) {
                Util.Think($"{johnQuest.Name}: {johnQuest.Url}");
                Util.ClipboardCopy(johnQuest.Url);
            }

            // Quest Hint
            if (col == 1 && johnQuest.Hint.Length > 0) {
                Util.Think($"{johnQuest.Name}: {johnQuest.Hint}");
            }

            // Quest Flag
            if(col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
        void JohnListSortComplete_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.CompleteAscending)
            {
                JohnQuest.Sort(JohnQuest.SortType.CompleteDescending);
            }
            else
            {
                JohnQuest.Sort(JohnQuest.SortType.CompleteAscending);
            }

            UpdateJohnList();
        }

        void JohnListSortName_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.NameAscending) {
                JohnQuest.Sort(JohnQuest.SortType.NameDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.NameAscending);
            }

            UpdateJohnList();
        }

        void JohnListSortReady_Click(object sender, EventArgs e)
        {
            if(JohnQuest.CurrentSortType == JohnQuest.SortType.ReadyAscending) {
                JohnQuest.Sort(JohnQuest.SortType.ReadyDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.ReadyAscending);
            }

            UpdateJohnList();
        }

        void JohnListSortSolves_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.SolvesAscending) {
                JohnQuest.Sort(JohnQuest.SortType.SolvesDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.SolvesAscending);
            }

            UpdateJohnList();
        }
    }
}
