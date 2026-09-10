using System;
using System.Linq;
using Decal.Adapter;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        private static readonly string[] InventoryCategoryNames =
            { "Weapons", "Armor", "Clothing", "Jewelry", "Cloaks", "Summons", "Aetheria", "Salvage", "Other", "Doubles" };
        private HudButton vgInventorySavedSearch;
        private string availableInventorySearchToken;
        private SavedInventorySelection loadedInventorySelection;

        private void InitSavedInventorySearch()
        {
            vgInventorySavedSearch = (HudButton)view["VGInventorySavedSearch"];
            vgInventorySavedSearch.Visible = false;
            vgInventorySavedSearch.Hit += SavedInventorySearchHit;
        }

        private void DisposeSavedInventorySearch()
        {
            if (vgInventorySavedSearch != null) vgInventorySavedSearch.Hit -= SavedInventorySearchHit;
        }

        private void InventorySearchEdited()
        {
            loadedInventorySelection = null;
            hiddenInventorySelection = null;
            UpdateSavedInventorySearchButton(allowSave: true);
        }

        private void UpdateSavedInventorySearchButton(bool allowSave = false)
        {
            vgInventorySavedSearch.Text = availableInventorySearchToken == null ? "Save Search" : "Load Search";
            vgInventorySavedSearch.Visible = availableInventorySearchToken != null ||
                (allowSave && (selectedVGInventoryItem != null || VGInventoryFilter().IsActive));
        }

        // Poll only while the VGI inventory tab is active.
        private void PollSavedInventorySearch()
        {
            var character = CoreManager.Current?.CharacterFilter;
            if (character == null || character.LoginStatus < 1) return;
            try
            {
                string token = SavedInventorySearch.Peek(character.Server, character.Name)?.Token;
                if (token == availableInventorySearchToken) return;
                availableInventorySearchToken = token;
                UpdateSavedInventorySearchButton();
            }
            catch (Exception ex)
            {
                if (availableInventorySearchToken != null)
                {
                    availableInventorySearchToken = null;
                    UpdateSavedInventorySearchButton();
                }
                VGInventoryText.Text = "Saved search unavailable: " + ex.GetBaseException().Message;
            }
        }

        private void SavedInventorySearchHit(object sender, EventArgs e)
        {
            var character = CoreManager.Current?.CharacterFilter;
            if (character == null || character.LoginStatus < 1) return;
            try
            {
                if (availableInventorySearchToken == null)
                {
                    var item = selectedVGInventoryItem?.Item;
                    new SavedInventorySearch
                    {
                        Server = character.Server, SavedBy = character.Name, Filter = VGInventoryFilter(),
                        CategoryOrder = vgInventorySubfilters.CategoryOrder, Sort = SavedInventory.List.CurrentSortType,
                        Selection = item != null && item.Server == character.Server ? new SavedInventorySelection(item) : null
                    }.Save();
                    UpdateSavedInventorySearchButton();
                    Util.Chat("Search saved. On another character, open Server > Inventory and click Load Search. It can be loaded once.", Util.ColorPink);
                    return;
                }

                var saved = SavedInventorySearch.Take(character.Server, character.Name, availableInventorySearchToken);
                availableInventorySearchToken = null;
                UpdateSavedInventorySearchButton();
                if (saved == null)
                {
                    PollSavedInventorySearch();
                    Util.Chat("That saved search has already been loaded or replaced.", Util.ColorPink);
                    return;
                }

                suppressVGInventoryFilter = true;
                try
                {
                    VGInventoryFilterText.Text = saved.Filter.Text ?? "";
                    foreach (string name in InventoryCategoryNames)
                        ((HudCheckBox)view["VGInventoryFilter" + name]).Checked =
                            (bool)typeof(ItemFilter).GetField(name).GetValue(saved.Filter);
                    vgInventorySubfilters.Restore(saved.Filter, saved.CategoryOrder);
                }
                finally { suppressVGInventoryFilter = false; }
                SavedInventory.List.CurrentSortType = saved.Sort;
                RefreshVGInventory();
                loadedInventorySelection = saved.Selection;
                if (loadedInventorySelection == null)
                    Util.Chat("Loaded and cleared search.", Util.ColorPink);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void RestoreSavedInventorySelection()
        {
            var selection = loadedInventorySelection;
            loadedInventorySelection = null;
            if (selection == null) return;
            var core = CoreManager.Current;
            var character = core?.CharacterFilter;
            if (character == null || character.LoginStatus < 1) return;
            selectedVGInventoryItem = SavedInventory.List.Items.FirstOrDefault(row => selection.Matches(row.Item, character.Server));
            if (character.Name == selection.Owner)
            {
                var live = core.WorldFilter[selection.Id];
                if (live != null && ItemList.IsInInventory(live) && selection.Matches(new Item(live), character.Server))
                {
                    core.Actions.SelectItem(live.Id);
                    // Move the full item/stack to the first main-pack slot, without merging.
                    core.Actions.SelectedStackCount = Math.Max(1, live.Values(Decal.Adapter.Wrappers.LongValueKey.StackCount, 1));
                    core.Actions.MoveItem(live.Id, character.Id, 0, false);
                    Util.Chat($"Loaded and cleared search. Selected {selection.Name} and moving to main pack.", Util.ColorPink);
                    return;
                }
                else
                    Util.Chat($"Loaded and cleared search. {selection.Name} is not available in your inventory.", Util.ColorPink);
                return;
            }
            if (selectedVGInventoryItem == null)
                Util.Chat($"Loaded and cleared search. {selection.Name} on {selection.Owner} is not in these results.", Util.ColorPink);
            else
                Util.Chat("Loaded and cleared search.", Util.ColorPink);
        }

    }
}
