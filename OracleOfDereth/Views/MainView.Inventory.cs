using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        private readonly VGInventory SavedInventory = new VGInventory();
        private ItemSubfilters vgInventorySubfilters;
        private bool suppressVGInventoryFilter;
        private DateTime? vgInventorySearchDue;
        private System.Windows.Forms.Timer vgInventoryTimer;
        private List<ItemListRow> visibleVGInventory = new List<ItemListRow>();
        private ItemListRow selectedVGInventoryItem;
        private DateTime? vgInventoryHiddenSince;
        private bool vgInventoryResultsReleased;
        private SavedInventorySelection hiddenInventorySelection;
        private string hiddenInventorySelectionServer;

        public HudStaticText VGInventoryText { get; private set; }
        private HudButton vgInventoryHelp;
        private HudButton vgInventoryOpen;
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
        private HudPictureBox vgInventorySortIcon;
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
            vgInventoryHelp = (HudButton)view["VGInventoryHelp"];
            vgInventoryHelp.Hit += VGInventoryHelp_Hit;
            vgInventoryOpen = (HudButton)view["VGInventoryOpen"];
            vgInventoryOpen.Hit += VGInventoryOpen_Hit;
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
            vgInventorySubfilters = new ItemSubfilters((HudFixedLayout)view["VGInventorySubfilters"],
                category => (HudCheckBox)view["VGInventoryFilter" + category], VGInventoryFilter_Change);

            vgInventorySortIcon = new HudPictureBox { Image = IconSort };
            ((HudFixedLayout)view["VGInventoryListSort"]).AddControl(vgInventorySortIcon, new System.Drawing.Rectangle(0, 0, 16, 16));
            vgInventorySortIcon.Hit += VGInventoryListSortCharacter_Click;
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
            InitSavedInventorySearch();
        }

        private void DisposeVGInventory()
        {
            DisposeSavedInventorySearch();
            vgInventorySubfilters?.Dispose();
            if (vgInventoryTimer != null)
            {
                vgInventoryTimer.Stop();
                vgInventoryTimer.Tick -= VGInventorySearchTick;
                vgInventoryTimer.Dispose();
                vgInventoryTimer = null;
            }
            SavedInventory.CancelSearch();
            vgInventoryHelp.Hit -= VGInventoryHelp_Hit;
            vgInventoryOpen.Hit -= VGInventoryOpen_Hit;
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
            vgInventorySortIcon.Hit -= VGInventoryListSortCharacter_Click;
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

        private ItemFilter VGInventoryFilter() => vgInventorySubfilters.Apply(new ItemFilter
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
        });

        public void UpdateVGInventory()
        {
            vgInventoryOpen.Text = VGInventoryTracking.OpenButtonText();
            vgInventoryHiddenSince = null;
            if (DateTime.UtcNow.Second % 2 == 0) PollSavedInventorySearch();

            if (SavedInventory.ServerName != Server.Name
                || SavedInventory.List.PriorityCharacter != Decal.Adapter.CoreManager.Current.CharacterFilter.Name
                || (vgInventorySearchDue.HasValue && DateTime.UtcNow >= vgInventorySearchDue.Value))
                RefreshVGInventory(preserveSelection: true);
        }

        private void TickVGInventory(bool isActive)
        {
            if (isActive)
            {
                vgInventoryHiddenSince = null;
                return;
            }
            PauseVGInventorySearch();
            if (!vgInventoryHiddenSince.HasValue) vgInventoryHiddenSince = DateTime.UtcNow;
            if (vgInventoryResultsReleased || DateTime.UtcNow - vgInventoryHiddenSince.Value < TimeSpan.FromSeconds(30)) return;

            hiddenInventorySelection = selectedVGInventoryItem == null ? null : new SavedInventorySelection(selectedVGInventoryItem.Item);
            hiddenInventorySelectionServer = selectedVGInventoryItem?.Server;
            SavedInventory.ReleaseResults();
            visibleVGInventory = SavedInventory.List.Items;
            selectedVGInventoryItem = null;
            VGInventoryList.ClearRows();
            vgInventorySearchDue = DateTime.UtcNow;
            vgInventoryResultsReleased = true;
        }

        private void RefreshVGInventory(bool preserveSelection = false)
        {
            if (!preserveSelection)
            {
                loadedInventorySelection = null;
                hiddenInventorySelection = null;
            }
            vgInventoryResultsReleased = false;
            vgInventorySearchDue = null;
            SavedInventory.List.PriorityCharacter = Decal.Adapter.CoreManager.Current.CharacterFilter.Name;
            SavedInventory.BeginRefresh(Server.Name, VGInventoryFilter());
            vgInventoryTimer.Start();
            selectedVGInventoryItem = null;
            VGInventoryList.ClearRows();
            UpdateSavedInventorySearchButton(allowSave: vgInventorySavedSearch.Visible);
            UpdateVGInventoryList();
            VGInventoryText.Text = "Searching...";
        }

        private void PauseVGInventorySearch()
        {
            // Close SQLite while hidden; restart on return with the pending selection intact.
            if (SavedInventory.IsSearching)
            {
                SavedInventory.CancelSearch();
                vgInventorySearchDue = DateTime.UtcNow;
            }
            vgInventoryTimer.Stop();
        }

        private void AdvanceVGInventorySearch()
        {
            if (Decal.Adapter.CoreManager.Current.CharacterFilter.LoginStatus < 1)
            {
                SavedInventory.CancelSearch();
                vgInventoryTimer.Stop();
                return;
            }
            if (vgInventorySearchDue.HasValue && DateTime.UtcNow >= vgInventorySearchDue.Value) RefreshVGInventory(preserveSelection: true);
            if (!SavedInventory.IsSearching) return;
            if (SavedInventory.ServerName != Server.Name) { RefreshVGInventory(); return; }
            var slice = System.Diagnostics.Stopwatch.StartNew();

            while (SavedInventory.IsSearching && slice.ElapsedMilliseconds < 8) SavedInventory.AdvanceSearch();

            if (SavedInventory.IsSearching)
                VGInventoryText.Text = $"Searching: {SavedInventory.ScannedCount:N0} items read...";
            else
            {
                vgInventoryTimer.Stop();
                selectedVGInventoryItem = null;
                if (hiddenInventorySelection != null && hiddenInventorySelectionServer == Server.Name)
                    selectedVGInventoryItem = SavedInventory.List.Items.FirstOrDefault(row => hiddenInventorySelection.Matches(row.Item, Server.Name));
                hiddenInventorySelection = null;
                RestoreSavedInventorySelection();
                UpdateVGInventoryList();
                if (selectedVGInventoryItem != null)
                    VGInventoryList.ScrollPosition = visibleVGInventory.IndexOf(selectedVGInventoryItem);
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
                status = SavedInventory.Error;
            else if (SavedInventory.LoadedAt.HasValue && SavedInventory.TotalCount == 0)
                status = "VGI: Enable Track All Items from the Virindi Global Inventory decal plugin to begin";

            VGInventoryText.Text = status;
        }

        private void VGInventoryOpen_Hit(object sender, EventArgs e)
        {
            try
            {
                if (!VGInventoryTracking.ToggleView())
                    Util.Chat("VGI: Could not open its window. Make sure Virindi Global Inventory is loaded, or open it from the Decal bar.", Util.ColorPink);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Util.Chat("VGI: Could not open its window. Open it from the Decal bar.", Util.ColorPink);
            }
        }

        private void VGInventoryHelp_Hit(object sender, EventArgs e)
        {
            Util.Chat("This screen works with Virindi Global Inventory to show saved items across your characters on this server. If an item you expect is missing, make sure that character has Track All Items selected in Virindi Global Inventory.", Util.ColorPink);
        }

        private void VGInventoryFilter_Change(object sender, EventArgs e)
        {
            if (suppressVGInventoryFilter) return;
            selectedVGInventoryItem = null;
            InventorySearchEdited();
            vgInventorySubfilters.CategoryChanged(sender as HudCheckBox);
            SavedInventory.CancelSearch();
            vgInventorySearchDue = DateTime.UtcNow.AddMilliseconds(350);
            vgInventoryTimer.Start();
            VGInventoryText.Text = "Waiting to search - showing previous results.";
        }

        private void VGInventoryFilterReset_Hit(object sender, EventArgs e)
        {
            SavedInventory.List.CurrentSortType = ItemList.SortType.CurrentCharacterFirst;
            suppressVGInventoryFilter = true;
            vgInventorySubfilters.Reset();
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
            InventorySearchEdited();
            RefreshVGInventory();
        }

        private void VGInventoryList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= visibleVGInventory.Count) return;
            int previous = visibleVGInventory.IndexOf(selectedVGInventoryItem);
            if (previous != row)
            {
                if (previous >= 0 && previous < VGInventoryList.RowCount)
                    ItemListRenderer.SetRowColor(VGInventoryList[previous], false, !visibleVGInventory[previous].IsComplete, showCharacter: true);
                if (row < VGInventoryList.RowCount)
                    ItemListRenderer.SetRowColor(VGInventoryList[row], true, !visibleVGInventory[row].IsComplete, showCharacter: true);
            }
            selectedVGInventoryItem = visibleVGInventory[row];
            InventorySearchEdited();
            Item saved = selectedVGInventoryItem.Item;
            var core = Decal.Adapter.CoreManager.Current;
            if (saved.Server == Server.Name && !string.IsNullOrEmpty(saved.Character))
            {
                var worldObject = core?.WorldFilter?[saved.Id];
                if (worldObject != null)
                {
                    var live = new Item(worldObject);
                    if (saved.Character == live.Character && saved.Server == live.Server &&
                        saved.Name == live.Name && saved.ObjectClass == live.ObjectClass && saved.Icon == live.Icon)
                        core.Actions.SelectItem(live.Id);
                }
            }
            // Our character's live identification supplies the description.
            if (saved.Server == Server.Name && !string.IsNullOrEmpty(saved.Character) && saved.Character == core?.CharacterFilter?.Name) return;

            // Items on other characters still need their saved description.
            string description = selectedVGInventoryItem.DescriptionWithOwner;
            var modifiers = System.Windows.Forms.Control.ModifierKeys;
            if ((modifiers & (System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Control)) != 0)
                Util.Think(description);
            else
                Util.Chat(description, Util.ColorCyan);
        }

        private void VGInventoryClipboard_Hit(object sender, EventArgs e)
        {
            Util.ClipboardCopy(string.Join(Environment.NewLine + Environment.NewLine, visibleVGInventory.Select(t => t.DescriptionWithOwner)));
            Util.Chat($"Copied {visibleVGInventory.Count} items to clipboard");
        }

        private void VGInventoryExportText_Hit(object sender, EventArgs e)
        {
            ExportVGInventory(ItemExport.ToText);
        }

        private void VGInventoryExportCsv_Hit(object sender, EventArgs e)
        {
            ExportVGInventory(ItemExport.ToCsv);
        }

        private void VGInventoryExportJson_Hit(object sender, EventArgs e)
        {
            ExportVGInventory(ItemExport.ToJson);
        }

        private void ExportVGInventory(Func<List<ItemListRow>, string, string> writer)
        {
            try
            {
                string path = writer(visibleVGInventory, Server.Name + "-inventory");
                Util.ClipboardCopy(path);
                Util.Chat($"Exported {visibleVGInventory.Count:N0} items to {path}", Util.ColorPink);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }

        private void VGInventoryListSortCharacter_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.CharacterAscending, ItemList.SortType.CharacterDescending); RefreshVGInventory(); }
        private void VGInventoryListSortName_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.NameAscending, ItemList.SortType.NameDescending); RefreshVGInventory(); }
        private void VGInventoryListSortCol1_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col1Ascending, ItemList.SortType.Col1Descending); RefreshVGInventory(); }
        private void VGInventoryListSortCol2_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col2Ascending, ItemList.SortType.Col2Descending); RefreshVGInventory(); }
        private void VGInventoryListSortCol3_Click(object sender, EventArgs e) { SavedInventory.List.CycleCol3Sort(); RefreshVGInventory(); }
        private void VGInventoryListSortCol4_Click(object sender, EventArgs e) { SavedInventory.List.ToggleSort(ItemList.SortType.Col4Ascending, ItemList.SortType.Col4Descending); RefreshVGInventory(); }
    }
}
