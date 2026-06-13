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
        // Items
        public HudStaticText ItemsText { get; private set; }
        public HudCheckBox ItemsAddSelected { get; private set; }
        public HudButton ItemsAdd { get; private set; }
        public HudButton ItemsAddAll { get; private set; }
        public HudButton ItemsClear { get; private set; }
        public HudButton ItemsClipboard { get; private set; }
        public HudButton ItemsExportText { get; private set; }
        public HudButton ItemsExportCsv { get; private set; }
        public HudButton ItemsExportJson { get; private set; }
        public HudTextBox ItemsFilterText { get; private set; }
        public HudCheckBox ItemsFilterWeapons { get; private set; }
        public HudCheckBox ItemsFilterArmor { get; private set; }
        public HudCheckBox ItemsFilterJewelry { get; private set; }
        public HudCheckBox ItemsFilterCloaks { get; private set; }
        public HudCheckBox ItemsFilterSummons { get; private set; }
        public HudCheckBox ItemsFilterClothing { get; private set; }
        public HudCheckBox ItemsFilterAetheria { get; private set; }
        public HudCheckBox ItemsFilterSalvage { get; private set; }
        public HudCheckBox ItemsFilterOther { get; private set; }
        public HudFixedLayout ItemsListSortComplete { get; private set; }
        public HudPictureBox ItemsListSortCompleteIcon { get; private set; }
        public HudStaticText ItemsListSortName { get; private set; }
        public HudStaticText ItemsListSortCol1 { get; private set; }
        public HudStaticText ItemsListSortCol2 { get; private set; }
        public HudStaticText ItemsListSortCol3 { get; private set; }
        public HudStaticText ItemsListSortCol4 { get; private set; }
        public HudList ItemsList { get; private set; }

        private void InitItems()
        {
            Item.OnItemsListChanged = () => UpdateItemsList();
            Item.OnQueueFinished = () => { ItemsAddAll.Text = "Add All"; UpdateItemsList(); };

            ItemsText = (HudStaticText)view["ItemsText"];
            ItemsText.FontHeight = 10;

            ItemsAdd = (HudButton)view["ItemsAdd"];
            ItemsAdd.Hit += ItemsAdd_Hit;

            ItemsAddAll = (HudButton)view["ItemsAddAll"];
            ItemsAddAll.Hit += ItemsAddAll_Hit;

            ItemsClear = (HudButton)view["ItemsClear"];
            ItemsClear.Hit += ItemsClear_Hit;

            ItemsExportText = (HudButton)view["ItemsExportText"];
            ItemsExportText.Hit += ItemsExportText_Hit;

            ItemsExportCsv = (HudButton)view["ItemsExportCsv"];
            ItemsExportCsv.Hit += ItemsExportCsv_Hit;

            ItemsExportJson = (HudButton)view["ItemsExportJson"];
            ItemsExportJson.Hit += ItemsExportJson_Hit;

            ItemsClipboard = (HudButton)view["ItemsClipboard"];
            ItemsClipboard.Hit += ItemsClipboard_Hit;

            ItemsAddSelected = (HudCheckBox)view["ItemsAddSelected"];
            ItemsAddSelected.Change += ItemsAddSelected_Change;

            ItemsFilterText = (HudTextBox)view["ItemsFilterText"];
            ItemsFilterText.Change += ItemsFilter_Change;

            ItemsFilterWeapons = (HudCheckBox)view["ItemsFilterWeapons"];
            ItemsFilterWeapons.Change += ItemsFilter_Change;
            ItemsFilterArmor = (HudCheckBox)view["ItemsFilterArmor"];
            ItemsFilterArmor.Change += ItemsFilter_Change;
            ItemsFilterClothing = (HudCheckBox)view["ItemsFilterClothing"];
            ItemsFilterClothing.Change += ItemsFilter_Change;
            ItemsFilterJewelry = (HudCheckBox)view["ItemsFilterJewelry"];
            ItemsFilterJewelry.Change += ItemsFilter_Change;
            ItemsFilterCloaks = (HudCheckBox)view["ItemsFilterCloaks"];
            ItemsFilterCloaks.Change += ItemsFilter_Change;
            ItemsFilterSummons = (HudCheckBox)view["ItemsFilterSummons"];
            ItemsFilterSummons.Change += ItemsFilter_Change;
            ItemsFilterAetheria = (HudCheckBox)view["ItemsFilterAetheria"];
            ItemsFilterAetheria.Change += ItemsFilter_Change;
            ItemsFilterSalvage = (HudCheckBox)view["ItemsFilterSalvage"];
            ItemsFilterSalvage.Change += ItemsFilter_Change;
            ItemsFilterOther = (HudCheckBox)view["ItemsFilterOther"];
            ItemsFilterOther.Change += ItemsFilter_Change;

            ItemsListSortCompleteIcon = new HudPictureBox();
            ItemsListSortCompleteIcon.Image = IconSort;
            ItemsListSortComplete = (HudFixedLayout)view["ItemsListSortComplete"];
            ItemsListSortComplete.AddControl(ItemsListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
            ItemsListSortCompleteIcon.Hit += ItemsListSortComplete_Click;

            ItemsListSortName = (HudStaticText)view["ItemsListSortName"];
            ItemsListSortName.Hit += ItemsListSortName_Click;

            ItemsListSortCol1 = (HudStaticText)view["ItemsListSortCol1"];
            ItemsListSortCol1.Hit += ItemsListSortCol1_Click;

            ItemsListSortCol2 = (HudStaticText)view["ItemsListSortCol2"];
            ItemsListSortCol2.Hit += ItemsListSortCol2_Click;

            ItemsListSortCol3 = (HudStaticText)view["ItemsListSortCol3"];
            ItemsListSortCol3.Hit += ItemsListSortCol3_Click;

            ItemsListSortCol4 = (HudStaticText)view["ItemsListSortCol4"];
            ItemsListSortCol4.Hit += ItemsListSortCol4_Click;

            ItemsList = (HudList)view["ItemsList"];
            ItemsList.Click += ItemsList_Click;
            ItemsList.ClearRows();
        }

        private void DisposeItems()
        {
            Item.OnItemsListChanged = null;
            Item.OnQueueFinished = null;
            ItemsAddSelected.Change -= ItemsAddSelected_Change;
            ItemsAdd.Hit -= ItemsAdd_Hit;
            ItemsAddAll.Hit -= ItemsAddAll_Hit;
            ItemsClear.Hit -= ItemsClear_Hit;
            ItemsExportText.Hit -= ItemsExportText_Hit;
            ItemsExportCsv.Hit -= ItemsExportCsv_Hit;
            ItemsExportJson.Hit -= ItemsExportJson_Hit;
            ItemsClipboard.Hit -= ItemsClipboard_Hit;
            ItemsFilterText.Change -= ItemsFilter_Change;
            ItemsFilterWeapons.Change -= ItemsFilter_Change;
            ItemsFilterArmor.Change -= ItemsFilter_Change;
            ItemsFilterClothing.Change -= ItemsFilter_Change;
            ItemsFilterJewelry.Change -= ItemsFilter_Change;
            ItemsFilterCloaks.Change -= ItemsFilter_Change;
            ItemsFilterSummons.Change -= ItemsFilter_Change;
            ItemsFilterAetheria.Change -= ItemsFilter_Change;
            ItemsFilterSalvage.Change -= ItemsFilter_Change;
            ItemsFilterOther.Change -= ItemsFilter_Change;
            ItemsList.Click -= ItemsList_Click;
            ItemsListSortCompleteIcon.Hit -= ItemsListSortComplete_Click;
            ItemsListSortName.Hit -= ItemsListSortName_Click;
            ItemsListSortCol1.Hit -= ItemsListSortCol1_Click;
            ItemsListSortCol2.Hit -= ItemsListSortCol2_Click;
            ItemsListSortCol3.Hit -= ItemsListSortCol3_Click;
            ItemsListSortCol4.Hit -= ItemsListSortCol4_Click;
        }

        public void UpdateItems()
        {
            UpdateItemsList();
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

        private bool MatchesFilter(Item t, string[] terms)
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
                case 0: return ItemsFilterWeapons.Checked;
                case 1: return ItemsFilterArmor.Checked;
                case 2: return ItemsFilterJewelry.Checked;
                case 3: return ItemsFilterCloaks.Checked;
                case 4: return ItemsFilterSummons.Checked;
                case 5: return ItemsFilterAetheria.Checked;
                case 6: return ItemsFilterSalvage.Checked;
                case 7: return ItemsFilterClothing.Checked;
                default: return ItemsFilterOther.Checked;
            }
        }

        public void UpdateItemsList()
        {
            string filterText = ItemsFilterText?.Text?.Trim() ?? "";
            string[] filterTerms = filterText.Length > 0 ? filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[0];

            List<Item> items = Item.Items.Where(t => IsCategoryVisible(t.SortCategory)).Where(t => MatchesFilter(t, filterTerms)).ToList();

            for (int x = 0; x < items.Count; x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= ItemsList.RowCount) { row = ItemsList.AddRow(); } else { row = ItemsList[x]; }

                Item item = items[x];

                AssignImage((HudPictureBox)row[0], IconNotComplete);
                AssignImage((HudPictureBox)row[1], item.Icon);
                ((HudStaticText)row[2]).Text = item.Name;
                ((HudStaticText)row[3]).Text = item.SummaryCol1;
                ((HudStaticText)row[4]).Text = item.SummaryCol2;
                ((HudStaticText)row[5]).Text = item.SummaryCol3;
                ((HudStaticText)row[6]).Text = item.SummaryCol4;
                ((HudStaticText)row[7]).Text = item.Id.ToString();

                // Dim the row while it's still waiting on its appraisal.
                SetRowLoading(row, !item.IsIdentified);
            }

            while (ItemsList.RowCount > items.Count) { ItemsList.RemoveRow(ItemsList.RowCount - 1); }

            ItemsText.Text = ItemsStatusText(items.Count);
        }

        // Dim grey for rows still waiting on their appraisal details.
        private static readonly Color ColorLoading = Color.FromArgb(255, 150, 150, 150);

        // Tint the row's text columns (Name..Details) grey while loading, or reset
        // to the default colour once the item's details are filled in.
        private void SetRowLoading(HudList.HudListRowAccessor row, bool loading)
        {
            for (int col = 2; col <= 6; col++)
            {
                if (loading) ((HudStaticText)row[col]).TextColor = ColorLoading;
                else ((HudStaticText)row[col]).ResetTextColor();
            }
        }

        // One consistent format regardless of filters: total, plus optional
        // "(X shown)" when a filter hides some and "(N identifying)" while ids load.
        private string ItemsStatusText(int shownCount)
        {
            int total = Item.Items.Count;
            int identifying = Item.QueueCount;

            var notes = new List<string>();
            if (shownCount != total) notes.Add($"{shownCount} shown");
            if (identifying > 0) notes.Add($"{identifying} identifying");

            string status = $"Items: {total}";
            if (notes.Count > 0) status += " (" + string.Join(", ", notes) + ")";
            return status;
        }

        private void ItemsAddSelected_Change(object sender, EventArgs e)
        {
            Item.AutoAddEnabled = ItemsAddSelected.Checked;
        }

        private void ItemsFilter_Change(object sender, EventArgs e)
        {
            UpdateItemsList();
        }

        private void ItemsAdd_Hit(object sender, EventArgs e)
        {
            Item.RequestAdd(Target.CurrentTargetId);
            UpdateItemsList();
        }

        private void ItemsAddAll_Hit(object sender, EventArgs e)
        {
            if (Item.IsProcessingQueue)
            {
                Item.CancelQueue();
                ItemsAddAll.Text = "Add All";
                UpdateItemsList();
                return;
            }

            ItemsAddAll.Text = "Adding...";
            Item.AddAll();
            if (Item.QueueCount == 0) { ItemsAddAll.Text = "Add All"; }
            UpdateItemsList();
        }

        private void ItemsClear_Hit(object sender, EventArgs e)
        {
            Item.Clear();
            UpdateItemsList();
        }

        private void ItemsExportText_Hit(object sender, EventArgs e)
        {
            string path = Item.ExportToText();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {Item.Items.Count} items to {path}");
        }

        private void ItemsExportCsv_Hit(object sender, EventArgs e)
        {
            string path = Item.ExportToCsv();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {Item.Items.Count} items to {path}");
        }

        private void ItemsExportJson_Hit(object sender, EventArgs e)
        {
            string path = Item.ExportToJson();
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {Item.Items.Count} items to {path}");
        }

        private void ItemsClipboard_Hit(object sender, EventArgs e)
        {
            string text = string.Join("\n", Item.Items.Select(t => t.Description));
            Util.ClipboardCopy(text);
            Util.Chat($"Copied {Item.Items.Count} items to clipboard");
        }

        private void ItemsList_Click(object sender, int row, int col)
        {
            int id = int.Parse(((HudStaticText)ItemsList[row][7]).Text);

            if (col == 0)
            {
                Item.Remove(id);
                UpdateItemsList();
            }
            else
            {
                CoreManager.Current.Actions.SelectItem(id);
                //Item item = Item.Items.FirstOrDefault(t => t.Id == id);
                //if (item != null) { Util.Chat(item.Description); }
            }
        }

        private void ItemsListSortComplete_Click(object sender, EventArgs e)
        {
            ItemsListSortName_Click(sender, e);
        }

        private void ItemsListSortName_Click(object sender, EventArgs e)
        {
            if (Item.CurrentSortType == Item.SortType.NameAscending)
                Item.Sort(Item.SortType.NameDescending);
            else
                Item.Sort(Item.SortType.NameAscending);

            UpdateItemsList();
        }

        private void ItemsListSortCol1_Click(object sender, EventArgs e)
        {
            if (Item.CurrentSortType == Item.SortType.Col1Ascending)
                Item.Sort(Item.SortType.Col1Descending);
            else
                Item.Sort(Item.SortType.Col1Ascending);

            UpdateItemsList();
        }

        private void ItemsListSortCol2_Click(object sender, EventArgs e)
        {
            if (Item.CurrentSortType == Item.SortType.Col2Ascending)
                Item.Sort(Item.SortType.Col2Descending);
            else
                Item.Sort(Item.SortType.Col2Ascending);

            UpdateItemsList();
        }

        private void ItemsListSortCol3_Click(object sender, EventArgs e)
        {
            if (Item.CurrentSortType == Item.SortType.Col3Ascending)
                Item.Sort(Item.SortType.Col3Descending);
            else
                Item.Sort(Item.SortType.Col3Ascending);

            UpdateItemsList();
        }

        private void ItemsListSortCol4_Click(object sender, EventArgs e)
        {
            if (Item.CurrentSortType == Item.SortType.Col4Ascending)
                Item.Sort(Item.SortType.Col4Descending);
            else
                Item.Sort(Item.SortType.Col4Ascending);

            UpdateItemsList();
        }
    }
}
