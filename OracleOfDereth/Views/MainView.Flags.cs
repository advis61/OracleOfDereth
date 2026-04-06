using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList FlagsList { get; private set; }
        public HudButton FlagsRefresh { get; private set; }

        private void InitFlags()
        {
            FlagsRefresh = (HudButton)view["FlagsRefresh"];
            FlagsRefresh.Hit += QuestFlagsRefresh_Hit;

            FlagsList = (HudList)view["FlagsList"];
            FlagsList.Click += FlagsList_Click;
            FlagsList.ClearRows();
        }

        private void DisposeFlags()
        {
            FlagsList.Click -= FlagsList_Click;
            FlagsRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateFlags()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateFlagsList();
        }

        private void UpdateFlagsList()
        {
            List<FlagQuest> flagQuests = FlagQuest.FlagQuests.ToList();

            for (int x = 0; x < flagQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= FlagsList.RowCount) {
                    row = FlagsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = FlagsList[x];
                }

                // Update
                FlagQuest flagQuest = flagQuests[x];

                AssignImage((HudPictureBox)row[0], flagQuest.IsComplete());
                ((HudStaticText)row[1]).Text = flagQuest.Name;

                if (flagQuest.IsComplete())
                {
                    ((HudStaticText)row[2]).Text = "completed";
                }
                else
                {
                    ((HudStaticText)row[2]).Text = "ready";
                }

                ((HudStaticText)row[3]).Text = flagQuest.Flag;
            }
        }

        private void FlagsList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)FlagsList[row][3]).Text;

            FlagQuest flagQuest = FlagQuest.FlagQuests.FirstOrDefault(x => x.Flag == flag);
            if (flagQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && flagQuest.Url.Length > 0)
            {
                Util.Think($"{flagQuest.Name}: {flagQuest.Url}");
                Util.ClipboardCopy(flagQuest.Url);
            }

            // Quest Hint
            if (col == 1 && flagQuest.Hint.Length > 0)
            {
                Util.Think($"{flagQuest.Name}: {flagQuest.Hint}");
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
