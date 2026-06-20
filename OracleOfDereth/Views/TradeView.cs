using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    // A standalone floating window (like TargetView) showing the items a trade partner
    // has dropped into the trade window. Backed by ItemList.Trade for the items and the
    // Trade model for the session (partner, bot detection, price quotes); PluginCore drives
    // Show/Hide and feeds Trade the chat. Pure presentation — no parsing lives here.
    class TradeView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        // Icon for the sortable column header (mirrors MainView's constant).
        readonly int IconSort = 0x60011F7;           // Sort Icon

        // Per-view image tracking so repeated repaints skip identical image assignments.
        private readonly Dictionary<HudPictureBox, int> AssignedImages = new Dictionary<HudPictureBox, int>();

        public HudStaticText TradeText { get; private set; }
        public HudButton TradeAddButton { get; private set; }
        public HudButton TradeWithdrawBank { get; private set; }
        public HudStaticText TradeStatusText { get; private set; }
        public HudButton TradeClipboard { get; private set; }
        public HudButton TradeExportText { get; private set; }
        public HudButton TradeExportCsv { get; private set; }
        public HudButton TradeExportJson { get; private set; }
        public HudTextBox TradeFilterText { get; private set; }
        public HudButton TradeFilterReset { get; private set; }
        public HudCheckBox TradeFilterWeapons { get; private set; }
        public HudCheckBox TradeFilterArmor { get; private set; }
        public HudCheckBox TradeFilterClothing { get; private set; }
        public HudCheckBox TradeFilterJewelry { get; private set; }
        public HudCheckBox TradeFilterCloaks { get; private set; }
        public HudCheckBox TradeFilterSummons { get; private set; }
        public HudCheckBox TradeFilterAetheria { get; private set; }
        public HudCheckBox TradeFilterSalvage { get; private set; }
        public HudCheckBox TradeFilterOther { get; private set; }
        public HudCheckBox TradeFilterDoubles { get; private set; }
        public HudFixedLayout TradeListSortComplete { get; private set; }
        public HudPictureBox TradeListSortCompleteIcon { get; private set; }
        public HudStaticText TradeListSortName { get; private set; }
        public HudStaticText TradeListSortCol1 { get; private set; }
        public HudStaticText TradeListSortCol2 { get; private set; }
        public HudStaticText TradeListSortCol3 { get; private set; }
        public HudStaticText TradeListSortCol4 { get; private set; }
        public HudList TradeList { get; private set; }

        // The partner's offered items. (The Trade model holds the session: partner, price, etc.)
        private ItemList TradeItems => ItemList.Trade;

        // Set while Reset clears the filter controls, so their Change events don't each
        // trigger a refresh/re-request — we refresh once at the end instead.
        private bool suppressFilter = false;
        public TradeView()
        {
            try
            {
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("OracleOfDereth.tradeView.xml", out properties, out controls);

                view = new VirindiViewService.HudView(properties, controls);
                if (view == null) { return; }

                // Hidden until a trade opens; never shown in the Decal bar.
                view.ShowInBar = false;
                view.Visible = false;

                // Let the user resize the window (horizontally and vertically). The default max
                // client area is the XML size, which caps how far it can be dragged — raise it.
                view.UserResizeable = true;
                view.MaximumClientArea = new Size(1920, 1080);

                // Repaint whenever the item list or the trade session changes.
                TradeItems.OnItemsListChanged = () => UpdateList();
                Trade.OnChanged = () => UpdateList();

                TradeText = (HudStaticText)view["TradeText"];
                TradeText.FontHeight = 10;

                TradeAddButton = (HudButton)view["TradeAddButton"];
                TradeAddButton.Hit += AddButton_Hit;

                TradeWithdrawBank = (HudButton)view["TradeWithdrawBank"];
                TradeWithdrawBank.Hit += WithdrawBankButton_Hit;

                TradeStatusText = (HudStaticText)view["TradeStatusText"];
                TradeStatusText.FontHeight = 8;

                TradeClipboard = (HudButton)view["TradeClipboard"];
                TradeClipboard.Hit += ClipboardButton_Hit;

                TradeExportText = (HudButton)view["TradeExportText"];
                TradeExportText.Hit += ExportTextButton_Hit;

                TradeExportCsv = (HudButton)view["TradeExportCsv"];
                TradeExportCsv.Hit += ExportCsvButton_Hit;

                TradeExportJson = (HudButton)view["TradeExportJson"];
                TradeExportJson.Hit += ExportJsonButton_Hit;

                TradeFilterText = (HudTextBox)view["TradeFilterText"];
                TradeFilterText.Change += Filter_Change;

                TradeFilterReset = (HudButton)view["TradeFilterReset"];
                TradeFilterReset.Hit += FilterReset_Hit;

                TradeFilterWeapons = (HudCheckBox)view["TradeFilterWeapons"];
                TradeFilterWeapons.Change += Filter_Change;
                TradeFilterArmor = (HudCheckBox)view["TradeFilterArmor"];
                TradeFilterArmor.Change += Filter_Change;
                TradeFilterClothing = (HudCheckBox)view["TradeFilterClothing"];
                TradeFilterClothing.Change += Filter_Change;
                TradeFilterJewelry = (HudCheckBox)view["TradeFilterJewelry"];
                TradeFilterJewelry.Change += Filter_Change;
                TradeFilterCloaks = (HudCheckBox)view["TradeFilterCloaks"];
                TradeFilterCloaks.Change += Filter_Change;
                TradeFilterSummons = (HudCheckBox)view["TradeFilterSummons"];
                TradeFilterSummons.Change += Filter_Change;
                TradeFilterAetheria = (HudCheckBox)view["TradeFilterAetheria"];
                TradeFilterAetheria.Change += Filter_Change;
                TradeFilterSalvage = (HudCheckBox)view["TradeFilterSalvage"];
                TradeFilterSalvage.Change += Filter_Change;
                TradeFilterOther = (HudCheckBox)view["TradeFilterOther"];
                TradeFilterOther.Change += Filter_Change;
                TradeFilterDoubles = (HudCheckBox)view["TradeFilterDoubles"];
                TradeFilterDoubles.Change += Filter_Change;

                TradeListSortCompleteIcon = new HudPictureBox();
                TradeListSortCompleteIcon.Image = IconSort;
                TradeListSortComplete = (HudFixedLayout)view["TradeListSortComplete"];
                TradeListSortComplete.AddControl(TradeListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
                TradeListSortCompleteIcon.Hit += SortName_Click;

                TradeListSortName = (HudStaticText)view["TradeListSortName"];
                TradeListSortName.Hit += SortName_Click;
                TradeListSortCol1 = (HudStaticText)view["TradeListSortCol1"];
                TradeListSortCol1.Hit += SortCol1_Click;
                TradeListSortCol2 = (HudStaticText)view["TradeListSortCol2"];
                TradeListSortCol2.Hit += SortCol2_Click;
                TradeListSortCol3 = (HudStaticText)view["TradeListSortCol3"];
                TradeListSortCol3.Hit += SortCol3_Click;
                TradeListSortCol4 = (HudStaticText)view["TradeListSortCol4"];
                TradeListSortCol4.Hit += SortCol4_Click;

                TradeList = (HudList)view["TradeList"];
                TradeList.Click += List_Click;
                TradeList.ClearRows();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public void Show()
        {
            if (view == null) return;
            view.Visible = true;
            UpdateList();
        }

        public void Hide()
        {
            if (view == null) return;
            view.Visible = false;
        }

        private ItemFilter Filter()
        {
            return new ItemFilter
            {
                Text = TradeFilterText?.Text ?? "",
                Weapons = TradeFilterWeapons.Checked,
                Armor = TradeFilterArmor.Checked,
                Clothing = TradeFilterClothing.Checked,
                Jewelry = TradeFilterJewelry.Checked,
                Cloaks = TradeFilterCloaks.Checked,
                Summons = TradeFilterSummons.Checked,
                Aetheria = TradeFilterAetheria.Checked,
                Salvage = TradeFilterSalvage.Checked,
                Other = TradeFilterOther.Checked,
                Doubles = TradeFilterDoubles.Checked,
            };
        }

        public void UpdateList()
        {
            if (view == null) return;

            ItemFilter filter = Filter();
            List<Item> items = TradeItems.Items.Where(filter.Matches).ToList();

            // Appraise the exact on-screen rows (text + category) first, then fall back to the
            // category-only matches — so a narrow search term like "defender armor" doesn't leave
            // the rest of the selected categories un-appraised if you clear the text.
            TradeItems.PrioritizeIdentify(
                items.Select(t => t.Id),
                TradeItems.Items.Where(filter.MatchesCategory).Select(t => t.Id));

            // Track the in-game selection so its row is highlighted (the buttons act on it).
            int selectedId = Target.CurrentTargetId;

            // Pass 0 for the column-0 icon: the trade view has no row-delete, so no red circle.
            ItemListRenderer.Render(TradeList, items, AssignedImages, 0, selectedId);

            // Window title carries who we're trading with and whether it's a CyTrader bot.
            string title = "Trade";
            if (!string.IsNullOrEmpty(Trade.PartnerName))
                title = Trade.IsCyTrader ? $"{Trade.PartnerName} (CyTrader bot)" : Trade.PartnerName;
            view.Title = title;

            // The status line is just the item counts.
            TradeText.Text = ItemListRenderer.StatusText("Total Items", TradeItems.Items.Count, items.Count, TradeItems.UnidentifiedCount);

            // One status line: the bot's points list, and (after a check/buy) the quoted price
            // with its MMD equivalent and whether we can afford it.
            TradeStatusText.Text = Trade.TradeStatus;

            // The Add button only applies to a bot trade; hide it otherwise. It reads "Checkout"
            // once a check shows we can afford the selected item.
            TradeAddButton.Visible = Trade.IsCyTrader;
            TradeAddButton.Text = Trade.CanCheckout ? "Checkout" : "Add to Trade";

            // Offer a one-click bank withdrawal when this server has bank and the last price check
            // left us short on notes. Withdraws exactly the shortfall in MMDs.
            Bank.ResolveKnownServer();
            TradeWithdrawBank.Visible = Bank.Supported == true && Trade.MmdShortfall > 0;
            if (TradeWithdrawBank.Visible)
                TradeWithdrawBank.Text = $"Withdraw {Trade.MmdShortfall} MMD from Bank";
        }

        private void Filter_Change(object sender, EventArgs e)
        {
            if (suppressFilter) return;

            // UpdateList feeds the now-visible ids to PrioritizeIdentify, so the identify
            // pump appraises the filtered rows before the rest as in-flight slots free up.
            UpdateList();
        }

        // Clear the filter text box and uncheck every category, then refresh once.
        private void FilterReset_Hit(object sender, EventArgs e)
        {
            suppressFilter = true;
            TradeFilterText.Text = "";
            TradeFilterWeapons.Checked = false;
            TradeFilterArmor.Checked = false;
            TradeFilterClothing.Checked = false;
            TradeFilterJewelry.Checked = false;
            TradeFilterCloaks.Checked = false;
            TradeFilterSummons.Checked = false;
            TradeFilterAetheria.Checked = false;
            TradeFilterSalvage.Checked = false;
            TradeFilterOther.Checked = false;
            TradeFilterDoubles.Checked = false;
            suppressFilter = false;

            UpdateList();
        }

        // Click a row to select that item in-game (which highlights the row via the
        // ItemSelected event) and re-request its appraisal — fills stubs whose identify gave up.
        private void List_Click(object sender, int row, int col)
        {
            try
            {
                int id = int.Parse(((HudStaticText)TradeList[row][7]).Text);
                CoreManager.Current.Actions.SelectItem(id);
                CoreManager.Current.Actions.RequestId(id);

                // Price-check the clicked item right away when trading with a bot.
                if (Trade.IsCyTrader) Trade.CheckPrice(id);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Tell the bot to add the selected item to the trade window. No re-check — selecting the
        // item already priced it; we pay the notes only if that check showed we can afford it.
        // Acts on the in-game-selected trade item; the add is by id (exact).
        private void AddButton_Hit(object sender, EventArgs e)
        {
            try
            {
                Item item = RequireSelectedTradeItem();
                if (item == null) return;
                Trade.Add(item.Id);
                Util.Chat($"Adding {item.Name} from {Trade.PartnerName}", Util.ColorPink, "[Oracle of Dereth] ");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Withdraw exactly the MMDs we're short by from the server bank, so the player can then
        // afford the checked item. The next price check (or re-select) will see the new notes.
        private void WithdrawBankButton_Hit(object sender, EventArgs e)
        {
            try { Trade.WithdrawShortfall(); }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Export the partner's offered items, same as the Items tab does for the inventory.
        private void ClipboardButton_Hit(object sender, EventArgs e)
        {
            try
            {
                string text = string.Join("\n", TradeItems.Items.Select(t => t.Description));
                Util.ClipboardCopy(text);
                Util.Chat($"Copied {TradeItems.Items.Count} items to clipboard");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // The rows currently on screen: the partner's offered items narrowed by the active
        // category checkboxes + search box. Export acts on this, not the full list, so what
        // you save matches what you see.
        private List<Item> DisplayedItems() => TradeItems.Items.Where(Filter().Matches).ToList();

        private void ExportTextButton_Hit(object sender, EventArgs e)
        {
            try
            {
                List<Item> items = DisplayedItems();
                string path = ItemExport.ToText(items, Trade.PartnerName);
                Util.ClipboardCopy(path);
                Util.Chat($"Exported {items.Count} items to {path}");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void ExportCsvButton_Hit(object sender, EventArgs e)
        {
            try
            {
                List<Item> items = DisplayedItems();
                string path = ItemExport.ToCsv(items, Trade.PartnerName);
                Util.ClipboardCopy(path);
                Util.Chat($"Exported {items.Count} items to {path}");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void ExportJsonButton_Hit(object sender, EventArgs e)
        {
            try
            {
                List<Item> items = DisplayedItems();
                string path = ItemExport.ToJson(items, Trade.PartnerName);
                Util.ClipboardCopy(path);
                Util.Chat($"Exported {items.Count} items to {path}");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // The in-game-selected trade item if we can act on it, else null (with a chat note).
        private Item RequireSelectedTradeItem()
        {
            if (string.IsNullOrEmpty(Trade.PartnerName))
            {
                Util.Chat("No trade partner.", Util.ColorPink, "[Oracle of Dereth] ");
                return null;
            }
            Item item = TradeItems.Items.FirstOrDefault(t => t.Id == Target.CurrentTargetId);
            if (item == null)
            {
                Util.Chat("Select one of the trade items first.", Util.ColorPink, "[Oracle of Dereth] ");
                return null;
            }
            return item;
        }

        private void SortName_Click(object sender, EventArgs e) { TradeItems.ToggleSort(ItemList.SortType.NameAscending, ItemList.SortType.NameDescending); UpdateList(); }
        private void SortCol1_Click(object sender, EventArgs e) { TradeItems.ToggleSort(ItemList.SortType.Col1Ascending, ItemList.SortType.Col1Descending); UpdateList(); }
        private void SortCol2_Click(object sender, EventArgs e) { TradeItems.ToggleSort(ItemList.SortType.Col2Ascending, ItemList.SortType.Col2Descending); UpdateList(); }
        private void SortCol3_Click(object sender, EventArgs e) { TradeItems.CycleCol3Sort(); UpdateList(); }
        private void SortCol4_Click(object sender, EventArgs e) { TradeItems.ToggleSort(ItemList.SortType.Col4Ascending, ItemList.SortType.Col4Descending); UpdateList(); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (TradeItems != null) TradeItems.OnItemsListChanged = null;
            Trade.OnChanged = null;

            if (TradeList != null) TradeList.Click -= List_Click;

            if (TradeAddButton != null) TradeAddButton.Hit -= AddButton_Hit;
            if (TradeWithdrawBank != null) TradeWithdrawBank.Hit -= WithdrawBankButton_Hit;
            if (TradeClipboard != null) TradeClipboard.Hit -= ClipboardButton_Hit;
            if (TradeExportText != null) TradeExportText.Hit -= ExportTextButton_Hit;
            if (TradeExportCsv != null) TradeExportCsv.Hit -= ExportCsvButton_Hit;
            if (TradeExportJson != null) TradeExportJson.Hit -= ExportJsonButton_Hit;

            if (TradeFilterText != null) TradeFilterText.Change -= Filter_Change;
            if (TradeFilterReset != null) TradeFilterReset.Hit -= FilterReset_Hit;
            if (TradeFilterWeapons != null) TradeFilterWeapons.Change -= Filter_Change;
            if (TradeFilterArmor != null) TradeFilterArmor.Change -= Filter_Change;
            if (TradeFilterClothing != null) TradeFilterClothing.Change -= Filter_Change;
            if (TradeFilterJewelry != null) TradeFilterJewelry.Change -= Filter_Change;
            if (TradeFilterCloaks != null) TradeFilterCloaks.Change -= Filter_Change;
            if (TradeFilterSummons != null) TradeFilterSummons.Change -= Filter_Change;
            if (TradeFilterAetheria != null) TradeFilterAetheria.Change -= Filter_Change;
            if (TradeFilterSalvage != null) TradeFilterSalvage.Change -= Filter_Change;
            if (TradeFilterOther != null) TradeFilterOther.Change -= Filter_Change;
            if (TradeFilterDoubles != null) TradeFilterDoubles.Change -= Filter_Change;

            if (TradeListSortCompleteIcon != null) TradeListSortCompleteIcon.Hit -= SortName_Click;
            if (TradeListSortName != null) TradeListSortName.Hit -= SortName_Click;
            if (TradeListSortCol1 != null) TradeListSortCol1.Hit -= SortCol1_Click;
            if (TradeListSortCol2 != null) TradeListSortCol2.Hit -= SortCol2_Click;
            if (TradeListSortCol3 != null) TradeListSortCol3.Hit -= SortCol3_Click;
            if (TradeListSortCol4 != null) TradeListSortCol4.Hit -= SortCol4_Click;

            AssignedImages.Clear();
            view?.Dispose();
        }
    }
}
