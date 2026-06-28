using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList CustomQuestsList { get; private set; }
        public HudButton CustomQuestsRefresh { get; private set; }

        private void InitCustomQuests()
        {
            CustomQuestsRefresh = (HudButton)view["CustomQuestsRefresh"];
            CustomQuestsRefresh.Hit += QuestFlagsRefresh_Hit;

            CustomQuestsList = (HudList)view["CustomQuestsList"];
            CustomQuestsList.Click += CustomQuestsList_Click;
            CustomQuestsList.ClearRows();
        }

        private void DisposeCustomQuests()
        {
            CustomQuestsList.Click -= CustomQuestsList_Click;
            CustomQuestsRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateCustomQuests()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateCustomQuestsList();
        }

        private void UpdateCustomQuestsList()
        {
            List<CustomQuest> customQuests = CustomQuest.CustomQuests.ToList();

            for (int x = 0; x < customQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= CustomQuestsList.RowCount)
                {
                    row = CustomQuestsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                }
                else
                {
                    row = CustomQuestsList[x];
                }

                // Update
                CustomQuest customQuest = customQuests[x];
                QuestFlag.QuestFlags.TryGetValue(customQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], customQuest.IsComplete());
                ((HudStaticText)row[1]).Text = customQuest.Name;

                if (questFlag == null)
                {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                }
                else
                {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[4]).Text = customQuest.Flag;
            }

            // Trim stale rows (e.g. if the list shrinks)
            while (CustomQuestsList.RowCount > customQuests.Count) { CustomQuestsList.RemoveRow(CustomQuestsList.RowCount - 1); }
        }

        private void CustomQuestsList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)CustomQuestsList[row][4]).Text;

            CustomQuest customQuest = CustomQuest.CustomQuests.FirstOrDefault(x => x.Flag == flag);
            if (customQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && customQuest.Url.Length > 0)
            {
                Util.ThinkQuestUrl($"{customQuest.Name}: {customQuest.Url}", customQuest.Url);
            }

            // Quest Hint
            if (col == 1 && customQuest.Hint.Length > 0)
            {
                Util.ThinkQuestDirections($"{customQuest.Name}: {customQuest.Hint}", customQuest.Hint);
            }

            // Quest Flag
            if (col >= 2)
            {
                if (questFlag == null)
                {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                }
                else
                {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
