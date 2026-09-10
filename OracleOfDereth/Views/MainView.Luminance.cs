using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText LuminanceText { get; private set; }
        public HudList LuminanceList { get; private set; }
        public HudButton LuminanceRefresh { get; private set; }

        private void InitLuminance()
        {
            LuminanceRefresh = (HudButton)view["LuminanceRefresh"];
            LuminanceRefresh.Hit += QuestFlagsRefresh_Hit;

            LuminanceText = (HudStaticText)view["LuminanceText"];
            LuminanceText.FontHeight = 10;

            LuminanceList = (HudList)view["LuminanceList"];
            LuminanceList.Click += LuminanceList_Click;
            LuminanceList.ClearRows();
        }

        private void DisposeLuminance()
        {
            LuminanceList.Click -= LuminanceList_Click;
            LuminanceRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateLuminance()
        {
            UpdateLuminanceList();
            UpdateLuminanceText();
        }

        private void UpdateLuminanceList()
        {
            List<Augmentation> augmentations = Augmentation.LuminanceAugmentations();

            for (int x = 0; x < augmentations.Count(); x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= LuminanceList.RowCount) {
                    row = LuminanceList.AddRow();

                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                } else {
                    row = LuminanceList[x];
                }

                // Update
                Augmentation augmentation = augmentations[x];
                if (augmentation.Name == "Blank") { continue; }
                if (augmentation.Id == 0) { SetText(row, 2, augmentation.Name); continue; }

                AssignImage((HudPictureBox)row[0], augmentation.IsComplete());
                SetText(row, 1, augmentation.Text());
                SetText(row, 2, augmentation.Name);
                SetText(row, 3, augmentation.Effect);
                //SetText(row, 3, $"{augmentation.LuminanceSpent():N0}");
                SetText(row, 4, augmentation.CostText());
                SetText(row, 5, augmentation.Id.ToString());
            }
        }

        void LuminanceList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)LuminanceList[row][5]).Text;
            if (text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int id = int.Parse(text);

            Augmentation augmentation = Augmentation.LuminanceAugmentations().FirstOrDefault(x => x.Id == id);
            if (augmentation == null) { return; }

            // Quest URL
            if (col == 0 && augmentation.Url.Length > 0) {
                Util.ThinkQuestUrl($"{augmentation.Name}: {augmentation.Url}", augmentation.Url);
            }

            // Quest Hint
            if (col > 0 && augmentation.Hint.Length > 0) {
                Util.ThinkQuestDirections($"{augmentation.Name}: {augmentation.Hint}", augmentation.Hint);
            }
        }

        private void UpdateLuminanceText()
        {
            LuminanceText.Text = $"{Augmentation.TotalLuminanceSpent():N0} spent / {Augmentation.TotalLuminance():N0} ({Augmentation.TotalLuminancePercentage()}% complete, {Augmentation.TotalLuminanceRemaining():N0} to max)";
        }
    }
}
