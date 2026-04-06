using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // Trade
        public HudStaticText TradeText { get; private set; }
        public HudCheckBox TradeAddSelected { get; private set; }
        public HudButton TradeAdd { get; private set; }
        public HudButton TradeAddAll { get; private set; }
        public HudButton TradeClear { get; private set; }
        public HudButton TradeClipboard { get; private set; }
        public HudButton TradeExportText { get; private set; }
        public HudButton TradeExportCsv { get; private set; }
        public HudButton TradeExportJson { get; private set; }
        public HudTextBox TradeFilterText { get; private set; }
        public HudCheckBox TradeFilterWeapons { get; private set; }
        public HudCheckBox TradeFilterArmor { get; private set; }
        public HudCheckBox TradeFilterJewelry { get; private set; }
        public HudCheckBox TradeFilterCloaks { get; private set; }
        public HudCheckBox TradeFilterSummons { get; private set; }
        public HudCheckBox TradeFilterClothing { get; private set; }
        public HudCheckBox TradeFilterAetheria { get; private set; }
        public HudCheckBox TradeFilterSalvage { get; private set; }
        public HudCheckBox TradeFilterOther { get; private set; }
        public HudFixedLayout TradeListSortComplete { get; private set; }
        public HudPictureBox TradeListSortCompleteIcon { get; private set; }
        public HudStaticText TradeListSortName { get; private set; }
        public HudStaticText TradeListSortCol1 { get; private set; }
        public HudStaticText TradeListSortCol2 { get; private set; }
        public HudStaticText TradeListSortCol3 { get; private set; }
        public HudStaticText TradeListSortCol4 { get; private set; }
        public HudList TradeList { get; private set; }

        private void InitTrade()
        {
            TradeItem.OnTradeListChanged = () => UpdateTradeList();
            TradeItem.OnQueueFinished = () => { TradeAddAll.Text = "Add All"; UpdateTradeList(); };

            TradeText = (HudStaticText)view["TradeText"];
            TradeText.FontHeight = 10;

            TradeAdd = (HudButton)view["TradeAdd"];
            TradeAdd.Hit += TradeAdd_Hit;

            TradeAddAll = (HudButton)view["TradeAddAll"];
            TradeAddAll.Hit += TradeAddAll_Hit;

            TradeClear = (HudButton)view["TradeClear"];
            TradeClear.Hit += TradeClear_Hit;

            TradeExportText = (HudButton)view["TradeExportText"];
            TradeExportText.Hit += TradeExportText_Hit;

            TradeExportCsv = (HudButton)view["TradeExportCsv"];
            TradeExportCsv.Hit += TradeExportCsv_Hit;

            TradeExportJson = (HudButton)view["TradeExportJson"];
            TradeExportJson.Hit += TradeExportJson_Hit;

            TradeClipboard = (HudButton)view["TradeClipboard"];
            TradeClipboard.Hit += TradeClipboard_Hit;

            TradeAddSelected = (HudCheckBox)view["TradeAddSelected"];
            TradeAddSelected.Change += TradeAddSelected_Change;

            TradeFilterText = (HudTextBox)view["TradeFilterText"];
            TradeFilterText.Change += TradeFilter_Change;

            TradeFilterWeapons = (HudCheckBox)view["TradeFilterWeapons"];
            TradeFilterWeapons.Change += TradeFilter_Change;
            TradeFilterArmor = (HudCheckBox)view["TradeFilterArmor"];
            TradeFilterArmor.Change += TradeFilter_Change;
            TradeFilterClothing = (HudCheckBox)view["TradeFilterClothing"];
            TradeFilterClothing.Change += TradeFilter_Change;
            TradeFilterJewelry = (HudCheckBox)view["TradeFilterJewelry"];
            TradeFilterJewelry.Change += TradeFilter_Change;
            TradeFilterCloaks = (HudCheckBox)view["TradeFilterCloaks"];
            TradeFilterCloaks.Change += TradeFilter_Change;
            TradeFilterSummons = (HudCheckBox)view["TradeFilterSummons"];
            TradeFilterSummons.Change += TradeFilter_Change;
            TradeFilterAetheria = (HudCheckBox)view["TradeFilterAetheria"];
            TradeFilterAetheria.Change += TradeFilter_Change;
            TradeFilterSalvage = (HudCheckBox)view["TradeFilterSalvage"];
            TradeFilterSalvage.Change += TradeFilter_Change;
            TradeFilterOther = (HudCheckBox)view["TradeFilterOther"];
            TradeFilterOther.Change += TradeFilter_Change;

            TradeListSortCompleteIcon = new HudPictureBox();
            TradeListSortCompleteIcon.Image = IconSort;
            TradeListSortComplete = (HudFixedLayout)view["TradeListSortComplete"];
            TradeListSortComplete.AddControl(TradeListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
            TradeListSortCompleteIcon.Hit += TradeListSortComplete_Click;

            TradeListSortName = (HudStaticText)view["TradeListSortName"];
            TradeListSortName.Hit += TradeListSortName_Click;

            TradeListSortCol1 = (HudStaticText)view["TradeListSortCol1"];
            TradeListSortCol1.Hit += TradeListSortCol1_Click;

            TradeListSortCol2 = (HudStaticText)view["TradeListSortCol2"];
            TradeListSortCol2.Hit += TradeListSortCol2_Click;

            TradeListSortCol3 = (HudStaticText)view["TradeListSortCol3"];
            TradeListSortCol3.Hit += TradeListSortCol3_Click;

            TradeListSortCol4 = (HudStaticText)view["TradeListSortCol4"];
            TradeListSortCol4.Hit += TradeListSortCol4_Click;

            TradeList = (HudList)view["TradeList"];
            TradeList.Click += TradeList_Click;
            TradeList.ClearRows();
        }

        private void DisposeTrade()
        {
            TradeItem.OnTradeListChanged = null;
            TradeItem.OnQueueFinished = null;
            TradeAddSelected.Change -= TradeAddSelected_Change;
            TradeAdd.Hit -= TradeAdd_Hit;
            TradeAddAll.Hit -= TradeAddAll_Hit;
            TradeClear.Hit -= TradeClear_Hit;
            TradeExportText.Hit -= TradeExportText_Hit;
            TradeExportCsv.Hit -= TradeExportCsv_Hit;
            TradeExportJson.Hit -= TradeExportJson_Hit;
            TradeClipboard.Hit -= TradeClipboard_Hit;
            TradeFilterText.Change -= TradeFilter_Change;
            TradeFilterWeapons.Change -= TradeFilter_Change;
            TradeFilterArmor.Change -= TradeFilter_Change;
            TradeFilterClothing.Change -= TradeFilter_Change;
            TradeFilterJewelry.Change -= TradeFilter_Change;
            TradeFilterCloaks.Change -= TradeFilter_Change;
            TradeFilterSummons.Change -= TradeFilter_Change;
            TradeFilterAetheria.Change -= TradeFilter_Change;
            TradeFilterSalvage.Change -= TradeFilter_Change;
            TradeFilterOther.Change -= TradeFilter_Change;
            TradeList.Click -= TradeList_Click;
            TradeListSortCompleteIcon.Hit -= TradeListSortComplete_Click;
            TradeListSortName.Hit -= TradeListSortName_Click;
            TradeListSortCol1.Hit -= TradeListSortCol1_Click;
            TradeListSortCol2.Hit -= TradeListSortCol2_Click;
            TradeListSortCol3.Hit -= TradeListSortCol3_Click;
            TradeListSortCol4.Hit -= TradeListSortCol4_Click;
        }

        public void UpdateTrade()
        {
            UpdateTradeList();
        }

        private int CountOccurrences(string text, string term)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += term.Length;
            }
            return count;
        }

        private bool MatchesFilter(TradeItem t, string[] terms)
        {
            if (terms.Length == 0) return true;
            string combined = $"{t.Name} {t.SummaryCol1} {t.SummaryCol2} {t.SummaryCol3} {t.SummaryCol4}";
            foreach (string term in terms)
            {
                int requiredCount = 1;
                string word = term;
                int starIndex = term.LastIndexOf('*');
                if (starIndex > 0 && int.TryParse(term.Substring(starIndex + 1), out int n))
                {
                    word = term.Substring(0, starIndex);
                    requiredCount = n;
                }
                else if (term.Contains(".*"))
                {
                    string[] parts = term.Split(new[] { ".*" }, StringSplitOptions.RemoveEmptyEntries);
                    requiredCount = parts.Length;
                    word = parts[0];
                    bool allSame = true;
                    foreach (string p in parts) { if (!p.Equals(parts[0], StringComparison.OrdinalIgnoreCase)) { allSame = false; break; } }
                    if (!allSame)
                    {
                        bool allFound = true;
                        foreach (string p in parts) { if (combined.IndexOf(p, StringComparison.OrdinalIgnoreCase) < 0) { allFound = false; break; } }
                        if (!allFound) return false;
                        continue;
                    }
                }
                if (CountOccurrences(combined, word) < requiredCount) return false;
            }
            return true;
        }

        private bool IsCategoryVisible(int sortCategory)
        {
            switch (sortCategory)
            {
                case 0: return TradeFilterWeapons.Checked;
                case 1: return TradeFilterArmor.Checked;
                case 2: return TradeFilterJewelry.Checked;
                case 3: return TradeFilterCloaks.Checked;
                case 4: return TradeFilterSummons.Checked;
                case 5: return TradeFilterAetheria.Checked;
                case 6: return TradeFilterSalvage.Checked;
                case 7: return TradeFilterClothing.Checked;
                default: return TradeFilterOther.Checked;
            }
        }

        public void UpdateTradeList()
        {
            string filterText = TradeFilterText?.Text?.Trim() ?? "";
            string[] filterTerms = filterText.Length > 0 ? filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];
            List<TradeItem> items = TradeItem.TradeItems
                .Where(t => IsCategoryVisible(t.SortCategory))
                .Where(t => MatchesFilter(t, filterTerms))
                .ToList();

            for (int x = 0; x < items.Count; x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= TradeList.RowCount) { row = TradeList.AddRow(); } else { row = TradeList[x]; }

                TradeItem item = items[x];

                AssignImage((HudPictureBox)row[0], IconNotComplete);
                AssignImage((HudPictureBox)row[1], item.Icon);
                ((HudStaticText)row[2]).Text = item.Name;
                ((HudStaticText)row[3]).Text = item.SummaryCol1;
                ((HudStaticText)row[4]).Text = item.SummaryCol2;
                ((HudStaticText)row[5]).Text = item.SummaryCol3;
                ((HudStaticText)row[6]).Text = item.SummaryCol4;
                ((HudStaticText)row[7]).Text = item.Id.ToString();
            }

            while (TradeList.RowCount > items.Count) { TradeList.RemoveRow(TradeList.RowCount - 1); }

            if (items.Count == TradeItem.TradeItems.Count)
                TradeText.Text = TradeItem.StatusText();
            else
                TradeText.Text = $"Trade Items: {items.Count} shown / {TradeItem.TradeItems.Count} total";
        }

        private void TradeAddSelected_Change(object sender, EventArgs e)
        {
            TradeItem.AutoAddEnabled = TradeAddSelected.Checked;
        }

        private void TradeFilter_Change(object sender, EventArgs e)
        {
            UpdateTradeList();
        }

        private void TradeAdd_Hit(object sender, EventArgs e)
        {
            TradeItem.RequestAdd(Target.CurrentTargetId);
            UpdateTradeList();
        }

        private void TradeAddAll_Hit(object sender, EventArgs e)
        {
            if (TradeItem.IsProcessingQueue)
            {
                TradeItem.CancelQueue();
                TradeAddAll.Text = "Add All";
                UpdateTradeList();
                return;
            }

            TradeAddAll.Text = "Adding...";
            TradeItem.AddAll();
            if (TradeItem.QueueCount == 0) { TradeAddAll.Text = "Add All"; }
            UpdateTradeList();
        }

        private void TradeClear_Hit(object sender, EventArgs e)
        {
            TradeItem.Clear();
            UpdateTradeList();
        }

        private void TradeExportText_Hit(object sender, EventArgs e)
        {
            string path = TradeItem.ExportToText();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {TradeItem.TradeItems.Count} items to {path}");
        }

        private void TradeExportCsv_Hit(object sender, EventArgs e)
        {
            string path = TradeItem.ExportToCsv();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {TradeItem.TradeItems.Count} items to {path}");
        }

        private void TradeExportJson_Hit(object sender, EventArgs e)
        {
            string path = TradeItem.ExportToJson();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {TradeItem.TradeItems.Count} items to {path}");
        }

        private void TradeClipboard_Hit(object sender, EventArgs e)
        {
            string text = string.Join("\n", TradeItem.TradeItems.Select(t => t.Description));
            Util.ClipboardCopy(text);
            Util.Chat($"Copied {TradeItem.TradeItems.Count} items to clipboard");
        }

        private void TradeList_Click(object sender, int row, int col)
        {
            int id = int.Parse(((HudStaticText)TradeList[row][7]).Text);

            if (col == 0)
            {
                TradeItem.Remove(id);
                UpdateTradeList();
            }
            else
            {
                CoreManager.Current.Actions.SelectItem(id);
                TradeItem item = TradeItem.TradeItems.FirstOrDefault(t => t.Id == id);
                if (item != null) { Util.Chat(item.Description); }
            }
        }

        private void TradeListSortComplete_Click(object sender, EventArgs e)
        {
            TradeListSortName_Click(sender, e);
        }

        private void TradeListSortName_Click(object sender, EventArgs e)
        {
            if (TradeItem.CurrentSortType == TradeItem.SortType.NameAscending)
                TradeItem.Sort(TradeItem.SortType.NameDescending);
            else
                TradeItem.Sort(TradeItem.SortType.NameAscending);
            UpdateTradeList();
        }

        private void TradeListSortCol1_Click(object sender, EventArgs e)
        {
            if (TradeItem.CurrentSortType == TradeItem.SortType.Col1Ascending)
                TradeItem.Sort(TradeItem.SortType.Col1Descending);
            else
                TradeItem.Sort(TradeItem.SortType.Col1Ascending);
            UpdateTradeList();
        }

        private void TradeListSortCol2_Click(object sender, EventArgs e)
        {
            if (TradeItem.CurrentSortType == TradeItem.SortType.Col2Ascending)
                TradeItem.Sort(TradeItem.SortType.Col2Descending);
            else
                TradeItem.Sort(TradeItem.SortType.Col2Ascending);
            UpdateTradeList();
        }

        private void TradeListSortCol3_Click(object sender, EventArgs e)
        {
            if (TradeItem.CurrentSortType == TradeItem.SortType.Col3Ascending)
                TradeItem.Sort(TradeItem.SortType.Col3Descending);
            else
                TradeItem.Sort(TradeItem.SortType.Col3Ascending);
            UpdateTradeList();
        }

        private void TradeListSortCol4_Click(object sender, EventArgs e)
        {
            if (TradeItem.CurrentSortType == TradeItem.SortType.Col4Ascending)
                TradeItem.Sort(TradeItem.SortType.Col4Descending);
            else
                TradeItem.Sort(TradeItem.SortType.Col4Ascending);
            UpdateTradeList();
        }
    }
}
