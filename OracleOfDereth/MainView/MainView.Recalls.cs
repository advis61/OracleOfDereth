using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList RecallsList { get; private set; }

        private void InitRecalls()
        {
            RecallsList = (HudList)view["RecallsList"];
            RecallsList.Click += RecallsList_Click;
            RecallsList.ClearRows();
        }

        private void DisposeRecalls()
        {
            RecallsList.Click -= RecallsList_Click;
        }

        public void UpdateRecalls()
        {
            UpdateRecallsList();
        }

        private void UpdateRecallsList()
        {
            List<Recall> recalls = Recall.Recalls.ToList();

            for (int x = 0; x < recalls.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= RecallsList.RowCount)
                {
                    row = RecallsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                }
                else
                {
                    row = RecallsList[x];
                }

                // Update
                Recall recall = recalls[x];
                AssignImage((HudPictureBox)row[0], recall.IsComplete());
                ((HudStaticText)row[1]).Text = recall.Name;

                if (recall.IsComplete())
                {
                    ((HudStaticText)row[2]).Text = "completed";
                }
                else
                {
                    ((HudStaticText)row[2]).Text = "-";
                }

                ((HudStaticText)row[3]).Text = recall.SpellId.ToString();
            }
        }

        private void RecallsList_Click(object sender, int row, int col)
        {
            int spellId = int.Parse(((HudStaticText)RecallsList[row][3]).Text);

            Recall recall = Recall.Recalls.FirstOrDefault(x => x.SpellId == spellId);
            if (recall == null) { return; }

            // Quest URL
            if (col == 0 && recall.Url.Length > 0)
            {
                Util.Think($"{recall.Name}: {recall.Url}");
                Util.ClipboardCopy(recall.Url);
            }

            // Quest Hint
            if (col == 1 && recall.Hint.Length > 0)
            {
                Util.Think($"{recall.Name} Recall: {recall.Hint}");
            }

            // Debug
            if (col >= 2)
            {
                Util.Chat($"Name:{recall.Name} SpellId:{recall.SpellId}", Util.ColorPink);
            }
        }
    }
}
