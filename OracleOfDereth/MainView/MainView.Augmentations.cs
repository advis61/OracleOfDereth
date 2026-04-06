using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList AugmentationsQuestsList { get; private set; }
        public HudList AugmentationsList { get; private set; }
        public HudButton AugmentationsRefresh { get; private set; }

        private void InitAugmentations()
        {
            AugmentationsRefresh = (HudButton)view["AugmentationsRefresh"];
            AugmentationsRefresh.Hit += QuestFlagsRefresh_Hit;

            AugmentationsQuestsList = (HudList)view["AugmentationsQuestsList"];
            AugmentationsQuestsList.Click += AugmentationsQuestsList_Click;
            AugmentationsQuestsList.ClearRows();

            AugmentationsList = (HudList)view["AugmentationsList"];
            AugmentationsList.Click += AugmentationsList_Click;
            AugmentationsList.ClearRows();
        }

        private void DisposeAugmentations()
        {
            AugmentationsQuestsList.Click -= AugmentationsQuestsList_Click;
            AugmentationsList.Click -= AugmentationsList_Click;
            AugmentationsRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateAugmentations()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateAugmentationQuestsList();
            UpdateAugmentationsList();
        }

        private void UpdateAugmentationsList()
        {
            List<Augmentation> augmentations = Augmentation.XPAugmentations().ToList();

            // Add or update rows
            for (int x = 0; x < augmentations.Count(); x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= AugmentationsList.RowCount) {
                    row = AugmentationsList.AddRow();

                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                } else {
                    row = AugmentationsList[x];
                }

                // Update
                Augmentation augmentation = augmentations[x];
                if (augmentation.Name == "Blank") { continue; }
                if (augmentation.Id == 0) { ((HudStaticText)row[2]).Text = augmentation.Name; continue; }

                AssignImage((HudPictureBox)row[0], augmentation.IsComplete());
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[2]).Text = augmentation.Name;
                ((HudStaticText)row[3]).Text = augmentation.Effect;
                ((HudStaticText)row[4]).Text = augmentation.CostText();
                ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
            }
        }

        private void AugmentationsList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)AugmentationsList[row][5]).Text;
            if(text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int id = int.Parse(text);

            Augmentation augmentation = Augmentation.XPAugmentations().FirstOrDefault(x => x.Id == id);
            if(augmentation == null) { return; }

            // Quest URL
            if (col == 0 && augmentation.Url.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Url}");
                Util.ClipboardCopy(augmentation.Url);
            }

            // Quest Hint
            if (col > 0 && augmentation.Hint.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Hint}");
            }
        }

        private void UpdateAugmentationQuestsList()
        {
            List<AugQuest> augQuests = AugQuest.AugQuests.ToList();

            for (int x = 0; x < augQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= AugmentationsQuestsList.RowCount) {
                    row = AugmentationsQuestsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = AugmentationsQuestsList[x];
                }

                // Update
                AugQuest augQuest = augQuests[x];
                QuestFlag.QuestFlags.TryGetValue(augQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], augQuest.IsComplete());
                ((HudStaticText)row[1]).Text = augQuest.Name;

                if (questFlag == null) {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                } else {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[4]).Text = augQuest.Flag;
            }
        }

        private void AugmentationsQuestsList_Click(object sender, int row, int col) {
            string flag = ((HudStaticText)AugmentationsQuestsList[row][4]).Text;

            AugQuest augQuest = AugQuest.AugQuests.FirstOrDefault(x => x.Flag == flag);
            if(augQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && augQuest.Url.Length > 0) {
                Util.Think($"{augQuest.Name}: {augQuest.Url}");
                Util.ClipboardCopy(augQuest.Url);
            }

            // Quest Hint
            if (col == 1 && augQuest.Hint.Length > 0) {
                Util.Think($"{augQuest.Name}: {augQuest.Hint}");
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
