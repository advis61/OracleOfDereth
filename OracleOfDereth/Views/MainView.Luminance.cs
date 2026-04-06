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
                if (augmentation.Id == 0) { ((HudStaticText)row[2]).Text = augmentation.Name; continue; }

                AssignImage((HudPictureBox)row[0], augmentation.IsComplete());
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[2]).Text = augmentation.Name;
                ((HudStaticText)row[3]).Text = augmentation.Effect;
                //((HudStaticText)row[3]).Text = $"{augmentation.LuminanceSpent():N0}";
                ((HudStaticText)row[4]).Text = augmentation.CostText();
                ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
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
                Util.Think($"{augmentation.Name}: {augmentation.Url}");
                Util.ClipboardCopy(augmentation.Url);
            }

            // Quest Hint
            if (col > 0 && augmentation.Hint.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Hint}");
            }
        }

        private void UpdateLuminanceText()
        {
            LuminanceText.Text = $"{Augmentation.TotalLuminanceSpent():N0} spent / {Augmentation.TotalLuminance():N0} ({Augmentation.TotalLuminancePercentage()}% complete, {Augmentation.TotalLuminanceRemaining():N0} to max)";
        }
    }
}
