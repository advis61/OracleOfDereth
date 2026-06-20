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
        // The inventory-backed list shown on the Items tab. (TradeView drives ItemList.Trade.)
        private ItemList InventoryList => ItemList.Inventory;

        // Set while Reset clears all the filter controls, so each control's Change event
        // doesn't trigger a refresh/re-request — we do one refresh at the end instead.
        private bool suppressItemsFilter = false;

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
        public HudButton ItemsFilterReset { get; private set; }
        public HudCheckBox ItemsFilterWeapons { get; private set; }
        public HudCheckBox ItemsFilterArmor { get; private set; }
        public HudCheckBox ItemsFilterJewelry { get; private set; }
        public HudCheckBox ItemsFilterCloaks { get; private set; }
        public HudCheckBox ItemsFilterSummons { get; private set; }
        public HudCheckBox ItemsFilterClothing { get; private set; }
        public HudCheckBox ItemsFilterAetheria { get; private set; }
        public HudCheckBox ItemsFilterSalvage { get; private set; }
        public HudCheckBox ItemsFilterOther { get; private set; }
        public HudCheckBox ItemsFilterDoubles { get; private set; }
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
            InventoryList.OnItemsListChanged = () => UpdateItemsList();
            InventoryList.OnQueueFinished = () => { ItemsAddAll.Text = "Add All"; UpdateItemsList(); };

            ItemsText = (HudStaticText)view["ItemsText"];
            ItemsText.FontHeight = 10;

            ItemsAdd = (HudButton)view["ItemsAdd"];
            ItemsAdd.Hit += ItemsAdd_Hit;

            ItemsAddAll = (HudButton)view["ItemsAddAll"];
            ItemsAddAll.Hit += ItemsAddAll_Hit;

            ItemsClear = (HudButton)view["ItemsClear"];
            ItemsClear.Hit += ItemsClear_Hit;

            ItemsClipboard = (HudButton)view["ItemsClipboard"];
            ItemsClipboard.Hit += ItemsClipboard_Hit;

            ItemsExportText = (HudButton)view["ItemsExportText"];
            ItemsExportText.Hit += ItemsExportText_Hit;

            ItemsExportCsv = (HudButton)view["ItemsExportCsv"];
            ItemsExportCsv.Hit += ItemsExportCsv_Hit;

            ItemsExportJson = (HudButton)view["ItemsExportJson"];
            ItemsExportJson.Hit += ItemsExportJson_Hit;

            ItemsAddSelected = (HudCheckBox)view["ItemsAddSelected"];
            ItemsAddSelected.Change += ItemsAddSelected_Change;

            ItemsFilterText = (HudTextBox)view["ItemsFilterText"];
            ItemsFilterText.Change += ItemsFilter_Change;

            ItemsFilterReset = (HudButton)view["ItemsFilterReset"];
            ItemsFilterReset.Hit += ItemsFilterReset_Hit;

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
            ItemsFilterDoubles = (HudCheckBox)view["ItemsFilterDoubles"];
            ItemsFilterDoubles.Change += ItemsFilter_Change;

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
            InventoryList.OnItemsListChanged = null;
            InventoryList.OnQueueFinished = null;
            ItemsAddSelected.Change -= ItemsAddSelected_Change;
            ItemsAdd.Hit -= ItemsAdd_Hit;
            ItemsAddAll.Hit -= ItemsAddAll_Hit;
            ItemsClear.Hit -= ItemsClear_Hit;
            ItemsClipboard.Hit -= ItemsClipboard_Hit;
            ItemsExportText.Hit -= ItemsExportText_Hit;
            ItemsExportCsv.Hit -= ItemsExportCsv_Hit;
            ItemsExportJson.Hit -= ItemsExportJson_Hit;
            ItemsFilterText.Change -= ItemsFilter_Change;
            ItemsFilterReset.Hit -= ItemsFilterReset_Hit;
            ItemsFilterWeapons.Change -= ItemsFilter_Change;
            ItemsFilterArmor.Change -= ItemsFilter_Change;
            ItemsFilterClothing.Change -= ItemsFilter_Change;
            ItemsFilterJewelry.Change -= ItemsFilter_Change;
            ItemsFilterCloaks.Change -= ItemsFilter_Change;
            ItemsFilterSummons.Change -= ItemsFilter_Change;
            ItemsFilterAetheria.Change -= ItemsFilter_Change;
            ItemsFilterSalvage.Change -= ItemsFilter_Change;
            ItemsFilterOther.Change -= ItemsFilter_Change;
            ItemsFilterDoubles.Change -= ItemsFilter_Change;
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

        // Build the filter from the tab's checkboxes + search box.
        private ItemFilter ItemsFilter()
        {
            return new ItemFilter
            {
                Text = ItemsFilterText?.Text ?? "",
                Weapons = ItemsFilterWeapons.Checked,
                Armor = ItemsFilterArmor.Checked,
                Clothing = ItemsFilterClothing.Checked,
                Jewelry = ItemsFilterJewelry.Checked,
                Cloaks = ItemsFilterCloaks.Checked,
                Summons = ItemsFilterSummons.Checked,
                Aetheria = ItemsFilterAetheria.Checked,
                Salvage = ItemsFilterSalvage.Checked,
                Other = ItemsFilterOther.Checked,
                Doubles = ItemsFilterDoubles.Checked,
            };
        }

        public void UpdateItemsList()
        {
            ItemFilter filter = ItemsFilter();
            List<Item> items = InventoryList.Items.Where(filter.Matches).ToList();

            // Appraise the exact on-screen rows (text + category) first, then fall back to the
            // category-only matches — so a search term doesn't leave the rest of the selected
            // categories un-appraised if you clear the text.
            InventoryList.PrioritizeIdentify(
                items.Select(t => t.Id),
                InventoryList.Items.Where(filter.MatchesCategory).Select(t => t.Id));

            ItemListRenderer.Render(ItemsList, items, AssignedImages, IconNotComplete, Target.CurrentTargetId);
            ItemsText.Text = ItemListRenderer.StatusText("Inventory Items", InventoryList.Items.Count, items.Count, InventoryList.UnidentifiedCount);
        }

        private void ItemsAddSelected_Change(object sender, EventArgs e)
        {
            InventoryList.AutoAddEnabled = ItemsAddSelected.Checked;
        }

        private void ItemsFilter_Change(object sender, EventArgs e)
        {
            if (suppressItemsFilter) return;

            // UpdateItemsList feeds the now-visible ids to PrioritizeIdentify, so the identify
            // pump appraises the filtered rows before the rest as in-flight slots free up.
            UpdateItemsList();
        }

        // Clear the filter text box and uncheck every category, then refresh once.
        private void ItemsFilterReset_Hit(object sender, EventArgs e)
        {
            suppressItemsFilter = true;
            ItemsFilterText.Text = "";
            ItemsFilterWeapons.Checked = false;
            ItemsFilterArmor.Checked = false;
            ItemsFilterClothing.Checked = false;
            ItemsFilterJewelry.Checked = false;
            ItemsFilterCloaks.Checked = false;
            ItemsFilterSummons.Checked = false;
            ItemsFilterAetheria.Checked = false;
            ItemsFilterSalvage.Checked = false;
            ItemsFilterOther.Checked = false;
            ItemsFilterDoubles.Checked = false;
            suppressItemsFilter = false;

            UpdateItemsList();
        }

        private void ItemsAdd_Hit(object sender, EventArgs e)
        {
            InventoryList.RequestAdd(Target.CurrentTargetId);
            UpdateItemsList();
        }

        private void ItemsAddAll_Hit(object sender, EventArgs e)
        {
            if (InventoryList.IsProcessingQueue)
            {
                InventoryList.CancelQueue();
                ItemsAddAll.Text = "Add All";
                UpdateItemsList();
                return;
            }

            ItemsAddAll.Text = "Adding...";
            InventoryList.AddAll();
            if (InventoryList.QueueCount == 0) { ItemsAddAll.Text = "Add All"; }
            UpdateItemsList();
        }

        private void ItemsClear_Hit(object sender, EventArgs e)
        {
            InventoryList.Clear();
            UpdateItemsList();
        }

        // The rows currently on screen: the underlying list narrowed by the active
        // category checkboxes + search box. Export/clipboard act on this, not the full
        // list, so what you save matches what you see.
        private List<Item> DisplayedItems() => InventoryList.Items.Where(ItemsFilter().Matches).ToList();

        private void ItemsExportText_Hit(object sender, EventArgs e)
        {
            List<Item> items = DisplayedItems();
            string path = ItemExport.ToText(items);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {items.Count} items to {path}");
        }

        private void ItemsExportCsv_Hit(object sender, EventArgs e)
        {
            List<Item> items = DisplayedItems();
            string path = ItemExport.ToCsv(items);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {items.Count} items to {path}");
        }

        private void ItemsExportJson_Hit(object sender, EventArgs e)
        {
            List<Item> items = DisplayedItems();
            string path = ItemExport.ToJson(items);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {items.Count} items to {path}");
        }

        private void ItemsClipboard_Hit(object sender, EventArgs e)
        {
            List<Item> items = DisplayedItems();
            string text = string.Join("\n", items.Select(t => t.Description));
            Util.ClipboardCopy(text);
            Util.Chat($"Copied {items.Count} items to clipboard");
        }

        private void ItemsList_Click(object sender, int row, int col)
        {
            try
            {
                int id = int.Parse(((HudStaticText)ItemsList[row][7]).Text);

                if (col == 0)
                {
                    InventoryList.Remove(id);
                    UpdateItemsList();
                }
                else
                {
                    // Select it in the world (highlights the row via the ItemSelected event) and
                    // (re)request its appraisal — fills any stub whose identify gave up.
                    CoreManager.Current.Actions.SelectItem(id);
                    CoreManager.Current.Actions.RequestId(id);
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void ItemsListSortComplete_Click(object sender, EventArgs e)
        {
            ItemsListSortName_Click(sender, e);
        }

        private void ItemsListSortName_Click(object sender, EventArgs e) { InventoryList.ToggleSort(ItemList.SortType.NameAscending, ItemList.SortType.NameDescending); UpdateItemsList(); }
        private void ItemsListSortCol1_Click(object sender, EventArgs e) { InventoryList.ToggleSort(ItemList.SortType.Col1Ascending, ItemList.SortType.Col1Descending); UpdateItemsList(); }
        private void ItemsListSortCol2_Click(object sender, EventArgs e) { InventoryList.ToggleSort(ItemList.SortType.Col2Ascending, ItemList.SortType.Col2Descending); UpdateItemsList(); }
        private void ItemsListSortCol3_Click(object sender, EventArgs e) { InventoryList.CycleCol3Sort(); UpdateItemsList(); }
        private void ItemsListSortCol4_Click(object sender, EventArgs e) { InventoryList.ToggleSort(ItemList.SortType.Col4Ascending, ItemList.SortType.Col4Descending); UpdateItemsList(); }
    }
}
