using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList FacilityList { get; private set; }
        public HudButton FacilityRefresh { get; private set; }

        private void InitFacility()
        {
            FacilityRefresh = (HudButton)view["FacilityRefresh"];
            FacilityRefresh.Hit += QuestFlagsRefresh_Hit;

            FacilityList = (HudList)view["FacilityList"];
            FacilityList.Click += FacilityList_Click;
            FacilityList.ClearRows();
        }

        private void DisposeFacility()
        {
            FacilityList.Click -= FacilityList_Click;
            FacilityRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateFacility()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateFacilityList();
        }

        private void UpdateFacilityList()
        {
            List<FacilityQuest> facilityQuests = FacilityQuest.FacilityQuests.ToList();

            for (int x = 0; x < facilityQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= FacilityList.RowCount)
                {
                    row = FacilityList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                }
                else
                {
                    row = FacilityList[x];
                }

                // Update
                FacilityQuest facilityQuest = facilityQuests[x];

                AssignImage((HudPictureBox)row[0], facilityQuest.IsComplete());
                ((HudStaticText)row[1]).Text = facilityQuest.Name;

                ((HudStaticText)row[2]).Text = facilityQuest.Level.ToString();

                if (facilityQuest.IsComplete())
                {
                    ((HudStaticText)row[3]).Text = "completed";
                }
                else
                {
                    ((HudStaticText)row[3]).Text = "ready";
                }

                ((HudStaticText)row[4]).Text = facilityQuest.Flag;
            }
        }

        private void FacilityList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)FacilityList[row][4]).Text;

            FacilityQuest facilityQuest = FacilityQuest.FacilityQuests.FirstOrDefault(x => x.Flag == flag);
            if (facilityQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFacility);

            // Quest URL
            if (col == 0 && facilityQuest.Url.Length > 0)
            {
                Util.Think($"{facilityQuest.Name}: {facilityQuest.Url}");
                Util.ClipboardCopy(facilityQuest.Url);
            }

            // Quest Hint
            if (col == 1 && facilityQuest.Hint.Length > 0)
            {
                Util.Think($"{facilityQuest.Name}: {facilityQuest.Hint}");
            }

            // Quest Facility
            if (col >= 2)
            {
                if (questFacility == null)
                {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                }
                else
                {
                    Util.Chat($"{questFacility.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
