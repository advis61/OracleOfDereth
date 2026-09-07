using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        private readonly VGInventory SavedInventory = new VGInventory();
        private bool suppressVGInventoryFilter;
        private DateTime? vgInventorySearchDue;
        private System.Windows.Forms.Timer vgInventoryTimer;
        private List<ItemListRow> visibleVGInventory = new List<ItemListRow>();
        private ItemListRow selectedVGInventoryItem;

        public HudStaticText VGInventoryText { get; private set; }
        public HudButton VGInventoryRefresh { get; private set; }
        public HudButton VGInventoryClipboard { get; private set; }
        public HudButton VGInventoryExportText { get; private set; }
        public HudButton VGInventoryExportCsv { get; private set; }
        public HudButton VGInventoryExportJson { get; private set; }
        public HudTextBox VGInventoryFilterText { get; private set; }
        public HudButton VGInventoryFilterReset { get; private set; }
        public HudCheckBox VGInventoryFilterWeapons { get; private set; }
        public HudCheckBox VGInventoryFilterArmor { get; private set; }
        public HudCheckBox VGInventoryFilterClothing { get; private set; }
        public HudCheckBox VGInventoryFilterJewelry { get; private set; }
        public HudCheckBox VGInventoryFilterCloaks { get; private set; }
        public HudCheckBox VGInventoryFilterSummons { get; private set; }
        public HudCheckBox VGInventoryFilterAetheria { get; private set; }
        public HudCheckBox VGInventoryFilterSalvage { get; private set; }
        public HudCheckBox VGInventoryFilterOther { get; private set; }
        public HudCheckBox VGInventoryFilterDoubles { get; private set; }
        public HudStaticText VGInventoryListSortCharacter { get; private set; }
        public HudStaticText VGInventoryListSortName { get; private set; }
        public HudStaticText VGInventoryListSortCol1 { get; private set; }
        public HudStaticText VGInventoryListSortCol2 { get; private set; }
        public HudStaticText VGInventoryListSortCol3 { get; private set; }
        public HudStaticText VGInventoryListSortCol4 { get; private set; }
        public HudList VGInventoryList { get; private set; }

        private void InitVGInventory()
        {
            VGInventoryText = (HudStaticText)view["VGInventoryText"];
            VGInventoryText.FontHeight = 10;
            VGInventoryRefresh = (HudButton)view["VGInventoryRefresh"];
            VGInventoryRefresh.Hit += VGInventoryRefresh_Hit;
            VGInventoryClipboard = (HudButton)view["VGInventoryClipboard"];
            VGInventoryClipboard.Hit += VGInventoryClipboard_Hit;
            VGInventoryExportText = (HudButton)view["VGInventoryExportText"];
            VGInventoryExportText.Hit += VGInventoryExportText_Hit;
            VGInventoryExportCsv = (HudButton)view["VGInventoryExportCsv"];
            VGInventoryExportCsv.Hit += VGInventoryExportCsv_Hit;
            VGInventoryExportJson = (HudButton)view["VGInventoryExportJson"];
            VGInventoryExportJson.Hit += VGInventoryExportJson_Hit;
            VGInventoryFilterReset = (HudButton)view["VGInventoryFilterReset"];
            VGInventoryFilterReset.Hit += VGInventoryFilterReset_Hit;
            VGInventoryFilterText = (HudTextBox)view["VGInventoryFilterText"];
            VGInventoryFilterText.Change += VGInventoryFilter_Change;
            VGInventoryFilterWeapons = (HudCheckBox)view["VGInventoryFilterWeapons"];
            VGInventoryFilterWeapons.Change += VGInventoryFilter_Change;
            VGInventoryFilterArmor = (HudCheckBox)view["VGInventoryFilterArmor"];
            VGInventoryFilterArmor.Change += VGInventoryFilter_Change;
            VGInventoryFilterClothing = (HudCheckBox)view["VGInventoryFilterClothing"];
            VGInventoryFilterClothing.Change += VGInventoryFilter_Change;
            VGInventoryFilterJewelry = (HudCheckBox)view["VGInventoryFilterJewelry"];
            VGInventoryFilterJewelry.Change += VGInventoryFilter_Change;
            VGInventoryFilterCloaks = (HudCheckBox)view["VGInventoryFilterCloaks"];
            VGInventoryFilterCloaks.Change += VGInventoryFilter_Change;
            VGInventoryFilterSummons = (HudCheckBox)view["VGInventoryFilterSummons"];
            VGInventoryFilterSummons.Change += VGInventoryFilter_Change;
            VGInventoryFilterAetheria = (HudCheckBox)view["VGInventoryFilterAetheria"];
            VGInventoryFilterAetheria.Change += VGInventoryFilter_Change;
            VGInventoryFilterSalvage = (HudCheckBox)view["VGInventoryFilterSalvage"];
            VGInventoryFilterSalvage.Change += VGInventoryFilter_Change;
            VGInventoryFilterOther = (HudCheckBox)view["VGInventoryFilterOther"];
            VGInventoryFilterOther.Change += VGInventoryFilter_Change;
            VGInventoryFilterDoubles = (HudCheckBox)view["VGInventoryFilterDoubles"];
            VGInventoryFilterDoubles.Change += VGInventoryFilter_Change;
            VGInventoryListSortCharacter = (HudStaticText)view["VGInventoryListSortCharacter"];
            VGInventoryListSortCharacter.Hit += VGInventoryListSortCharacter_Click;
            VGInventoryListSortName = (HudStaticText)view["VGInventoryListSortName"];
            VGInventoryListSortName.Hit += VGInventoryListSortName_Click;
            VGInventoryListSortCol1 = (HudStaticText)view["VGInventoryListSortCol1"];
            VGInventoryListSortCol1.Hit += VGInventoryListSortCol1_Click;
            VGInventoryListSortCol2 = (HudStaticText)view["VGInventoryListSortCol2"];
            VGInventoryListSortCol2.Hit += VGInventoryListSortCol2_Click;
            VGInventoryListSortCol3 = (HudStaticText)view["VGInventoryListSortCol3"];
            VGInventoryListSortCol3.Hit += VGInventoryListSortCol3_Click;
            VGInventoryListSortCol4 = (HudStaticText)view["VGInventoryListSortCol4"];
            VGInventoryListSortCol4.Hit += VGInventoryListSortCol4_Click;
            VGInventoryList = (HudList)view["VGInventoryList"];
            VGInventoryList.Click += VGInventoryList_Click;
            VGInventoryList.ClearRows();
            vgInventoryTimer = new System.Windows.Forms.Timer { Interval = 25 };
            vgInventoryTimer.Tick += VGInventorySearchTick;
        }

        private void DisposeVGInventory()
        {
            vgInventoryTimer.Stop();
            vgInventoryTimer.Tick -= VGInventorySearchTick;
            vgInventoryTimer.Dispose();
            SavedInventory.CancelSearch();
            VGInventoryRefresh.Hit -= VGInventoryRefresh_Hit;
            VGInventoryClipboard.Hit -= VGInventoryClipboard_Hit;
            VGInventoryExportText.Hit -= VGInventoryExportText_Hit;
            VGInventoryExportCsv.Hit -= VGInventoryExportCsv_Hit;
            VGInventoryExportJson.Hit -= VGInventoryExportJson_Hit;
            VGInventoryFilterReset.Hit -= VGInventoryFilterReset_Hit;
            VGInventoryFilterText.Change -= VGInventoryFilter_Change;
            VGInventoryFilterWeapons.Change -= VGInventoryFilter_Change;
            VGInventoryFilterArmor.Change -= VGInventoryFilter_Change;
            VGInventoryFilterClothing.Change -= VGInventoryFilter_Change;
            VGInventoryFilterJewelry.Change -= VGInventoryFilter_Change;
            VGInventoryFilterCloaks.Change -= VGInventoryFilter_Change;
            VGInventoryFilterSummons.Change -= VGInventoryFilter_Change;
            VGInventoryFilterAetheria.Change -= VGInventoryFilter_Change;
            VGInventoryFilterSalvage.Change -= VGInventoryFilter_Change;
            VGInventoryFilterOther.Change -= VGInventoryFilter_Change;
            VGInventoryFilterDoubles.Change -= VGInventoryFilter_Change;
            VGInventoryListSortCharacter.Hit -= VGInventoryListSortCharacter_Click;
            VGInventoryListSortName.Hit -= VGInventoryListSortName_Click;
            VGInventoryListSortCol1.Hit -= VGInventoryListSortCol1_Click;
            VGInventoryListSortCol2.Hit -= VGInventoryListSortCol2_Click;
            VGInventoryListSortCol3.Hit -= VGInventoryListSortCol3_Click;
            VGInventoryListSortCol4.Hit -= VGInventoryListSortCol4_Click;
            VGInventoryList.Click -= VGInventoryList_Click;
            SavedInventory.List.Clear();
            visibleVGInventory.Clear();
            selectedVGInventoryItem = null;
        }

        private ItemFilter VGInventoryFilter() => new ItemFilter
        {
            Text = VGInventoryFilterText.Text,
            Weapons = VGInventoryFilterWeapons.Checked,
            Armor = VGInventoryFilterArmor.Checked,
            Clothing = VGInventoryFilterClothing.Checked,
            Jewelry = VGInventoryFilterJewelry.Checked,
            Cloaks = VGInventoryFilterCloaks.Checked,
            Summons = VGInventoryFilterSummons.Checked,
            Aetheria = VGInventoryFilterAetheria.Checked,
            Salvage = VGInventoryFilterSalvage.Checked,
            Other = VGInventoryFilterOther.Checked,
            Doubles = VGInventoryFilterDoubles.Checked,
        };

        public void UpdateVGInventory()
        {
            if (SavedInventory.ServerName != Server.Name ||
                (vgInventorySearchDue.HasValue && DateTime.UtcNow >= vgInventorySearchDue.Value))
                RefreshVGInventory();
        }

        private void RefreshVGInventory()
        {
            vgInventorySearchDue = null;
            SavedInventory.BeginRefresh(Server.Name, VGInventoryFilter());
            vgInventoryTimer.Start();
            selectedVGInventoryItem = null;
            UpdateVGInventoryList();
        }

        private void VGInventorySearchTick(object sender, EventArgs e)
        {
            if (vgInventorySearchDue.HasValue && DateTime.UtcNow >= vgInventorySearchDue.Value)
                RefreshVGInventory();
            if (!SavedInventory.IsSearching) return;
            if (SavedInventory.ServerName != Server.Name) { RefreshVGInventory(); return; }
            var slice = System.Diagnostics.Stopwatch.StartNew();
            while (SavedInventory.IsSearching && slice.ElapsedMilliseconds < 8)
                SavedInventory.AdvanceSearch();
            if (SavedInventory.IsSearching)
                VGInventoryText.Text = $"Searching: {SavedInventory.ScannedCount:N0} items read...";
            else
            {
                vgInventoryTimer.Stop();
                selectedVGInventoryItem = null;
                UpdateVGInventoryList();
            }
        }

        private void UpdateVGInventoryList()
        {
            visibleVGInventory = SavedInventory.List.Items;
            ItemListRenderer.Render(VGInventoryList, visibleVGInventory, 0, 0, showCharacter: true, selectedItem: selectedVGInventoryItem);
            string status = $"Showing {visibleVGInventory.Count:N0} of {SavedInventory.MatchCount:N0} matches ({SavedInventory.TotalCount:N0} items)";
            if (SavedInventory.MatchCount > VGInventory.ResultLimit) status += " - narrow your filters";
            if (SavedInventory.UnreadableCount > 0) status += " (" + SavedInventory.UnreadableCount + " saved details unavailable)";
            if (!string.IsNullOrEmpty(SavedInventory.Error))
                status = SavedInventory.Error + (SavedInventory.LoadedAt.HasValue ? " — showing previous read." : "");
            VGInventoryText.Text = status;
        }

        private void VGInventoryRefresh_Hit(object sender, EventArgs e)
        {
            RefreshVGInventory();
            FlashButton(VGInventoryRefresh);
        }

        private void VGInventoryFilter_Change(object sender, EventArgs e)
        {
            if (suppressVGInventoryFilter) return;
            SavedInventory.CancelSearch();
            vgInventorySearchDue = DateTime.UtcNow.AddMilliseconds(350);
            vgInventoryTimer.Start();
            VGInventoryText.Text = "Waiting to search - showing previous results.";
        }

        private void VGInventoryFilterReset_Hit(object sender, EventArgs e)
        {
            suppressVGInventoryFilter = true;
            VGInventoryFilterText.Text = "";
            VGInventoryFilterWeapons.Checked = false;
            VGInventoryFilterArmor.Checked = false;
            VGInventoryFilterClothing.Checked = false;
            VGInventoryFilterJewelry.Checked = false;
            VGInventoryFilterCloaks.Checked = false;
            VGInventoryFilterSummons.Checked = false;
            VGInventoryFilterAetheria.Checked = false;
            VGInventoryFilterSalvage.Checked = false;
            VGInventoryFilterOther.Checked = false;
            VGInventoryFilterDoubles.Checked = false;
            suppressVGInventoryFilter = false;
            RefreshVGInventory();
        }

        private void VGInventoryList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= visibleVGInventory.Count) return;
            selectedVGInventoryItem = visibleVGInventory[row];
            UpdateVGInventoryList();
            // Offline IDs are not selectable game targets. Show the saved description.
            Util.Chat(selectedVGInventoryItem.Character + ": " + selectedVGInventoryItem.Description, Util.ColorCyan);
        }

        private void VGInventoryClipboard_Hit(object sender, EventArgs e)
        {
            Util.ClipboardCopy(string.Join("\n", visibleVGInventory.Select(t => t.Character + ": " + t.Description)));
            Util.Chat($"Copied {visibleVGInventory.Count} items to clipboard");
        }

        private void VGInventoryExportText_Hit(object sender, EventArgs e)
        {
            string path = ItemExport.ToText(visibleVGInventory, Server.Name + "-inventory");
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {visibleVGInventory.Count} items to {path}");
        }

        private void VGInventoryExportCsv_Hit(object sender, EventArgs e)
        {
            string path = ItemExport.ToCsv(visibleVGInventory, Server.Name + "-inventory");
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {visibleVGInventory.Count} items to {path}");
        }

        private void VGInventoryExportJson_Hit(object sender, EventArgs e)
        {
            string path = ItemExport.ToJson(visibleVGInventory, Server.Name + "-inventory");
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {visibleVGInventory.Count} items to {path}");
        }

        private void VGInventoryListSortCharacter_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.CharacterAscending, ItemList.SortType.CharacterDescending); RefreshVGInventory(); }
        private void VGInventoryListSortName_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.NameAscending, ItemList.SortType.NameDescending); RefreshVGInventory(); }
        private void VGInventoryListSortCol1_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col1Ascending, ItemList.SortType.Col1Descending); RefreshVGInventory(); }
        private void VGInventoryListSortCol2_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col2Ascending, ItemList.SortType.Col2Descending); RefreshVGInventory(); }
        private void VGInventoryListSortCol3_Click(object sender, EventArgs e) { SavedInventory.List.CycleCol3Sort(); RefreshVGInventory(); }
        private void VGInventoryListSortCol4_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col4Ascending, ItemList.SortType.Col4Descending); RefreshVGInventory(); }
    }
}

