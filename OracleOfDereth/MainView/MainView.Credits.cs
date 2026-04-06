using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList CreditsList { get; private set; }
        public HudButton CreditsRefresh { get; private set; }

        private void InitCredits()
        {
            CreditsRefresh = (HudButton)view["CreditsRefresh"];
            CreditsRefresh.Hit += QuestFlagsRefresh_Hit;

            CreditsList = (HudList)view["CreditsList"];
            CreditsList.Click += CreditsList_Click;
            CreditsList.ClearRows();
        }

        private void DisposeCredits()
        {
            CreditsList.Click -= CreditsList_Click;
            CreditsRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateCredits()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateCreditsList();
        }

        private void UpdateCreditsList()
        {
            List<CreditQuest> creditQuests = CreditQuest.CreditQuests.ToList();

            for (int x = 0; x < creditQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= CreditsList.RowCount) {
                    row = CreditsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = CreditsList[x];
                }

                // Update
                CreditQuest creditQuest = creditQuests[x];
                QuestFlag.QuestFlags.TryGetValue(creditQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], creditQuest.IsComplete());
                ((HudStaticText)row[1]).Text = creditQuest.Name;

                if(creditQuest.IsComplete()) {
                    ((HudStaticText)row[2]).Text = "completed";
                } else {
                    ((HudStaticText)row[2]).Text = "ready";
                }

                ((HudStaticText)row[3]).Text = creditQuest.Flag;
            }
        }


        private void CreditsList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)CreditsList[row][3]).Text;

            CreditQuest creditQuest = CreditQuest.CreditQuests.FirstOrDefault(x => x.Flag == flag);
            if (creditQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && creditQuest.Url.Length > 0) {
                Util.Think($"{creditQuest.Name}: {creditQuest.Url}");
                Util.ClipboardCopy(creditQuest.Url);
            }

            // Quest Hint
            if (col == 1 && creditQuest.Hint.Length > 0) {
                Util.Think($"{creditQuest.Name}: {creditQuest.Hint}");
            }

            // Quest Flag
            if (col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
