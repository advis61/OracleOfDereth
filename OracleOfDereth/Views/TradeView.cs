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

        // Icons (mirrors MainView's constants — these views don't share a base class).
        readonly int IconNotComplete = 0x60011F8;   // Red Circle
        readonly int IconSort = 0x60011F7;           // Sort Icon

        // Per-view image tracking so repeated repaints skip identical image assignments.
        private readonly Dictionary<HudPictureBox, int> AssignedImages = new Dictionary<HudPictureBox, int>();

        public HudStaticText TradeText { get; private set; }
        public HudStaticText TradeSelectedText { get; private set; }
        public HudButton TradeAddButton { get; private set; }
        public HudButton TradeCheckButton { get; private set; }
        public HudStaticText TradePriceText { get; private set; }
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

        // The item currently picked in the list — what Add/Check act on.
        private int selectedId = 0;
        private string selectedName = "";

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

                // Repaint whenever the item list or the trade session changes.
                TradeItems.OnItemsListChanged = () => UpdateList();
                Trade.OnChanged = () => UpdateList();

                TradeText = (HudStaticText)view["TradeText"];
                TradeText.FontHeight = 10;

                TradeSelectedText = (HudStaticText)view["TradeSelectedText"];
                TradeSelectedText.FontHeight = 10;

                TradeAddButton = (HudButton)view["TradeAddButton"];
                TradeAddButton.Hit += AddButton_Hit;

                TradeCheckButton = (HudButton)view["TradeCheckButton"];
                TradeCheckButton.Hit += CheckButton_Hit;

                TradePriceText = (HudStaticText)view["TradePriceText"];
                TradePriceText.FontHeight = 10;
                TradePriceText.TextColor = Color.FromArgb(255, 255, 215, 0); // gold

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

            // Fresh trade: drop the previous pick.
            selectedId = 0;
            selectedName = "";
            if (TradeSelectedText != null) TradeSelectedText.Text = "(none)";

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
            };
        }

        public void UpdateList()
        {
            if (view == null) return;

            ItemFilter filter = Filter();
            List<Item> items = TradeItems.Items.Where(filter.Matches).ToList();

            // Appraise what's on screen first (e.g. when filtered down to one category).
            TradeItems.PrioritizeIdentify(items.Select(t => t.Id));

            ItemListRenderer.Render(TradeList, items, AssignedImages, IconNotComplete);

            // Window title carries who we're trading with and whether it's a CyTrader bot.
            string title = "Trade";
            if (!string.IsNullOrEmpty(Trade.PartnerName))
                title = Trade.IsCyTrader ? $"{Trade.PartnerName} (CyTrader bot)" : Trade.PartnerName;
            view.Title = title;

            // The status line is just the item counts.
            TradeText.Text = ItemListRenderer.StatusText("Total Items", TradeItems.Items.Count, items.Count, TradeItems.UnidentifiedCount);

            // Show the last price the bot quoted (it names the item, so a stale quote is clear).
            TradePriceText.Text = Trade.PricePoints.Length > 0 ? $"{Trade.PricedItem}: {Trade.PricePoints} points" : "";
        }

        private void Filter_Change(object sender, EventArgs e)
        {
            if (suppressFilter) return;

            UpdateList();

            // Re-request the now-visible items so a filter (e.g. just Weapons) appraises
            // those first, even though the whole queue has already been sent.
            ItemFilter filter = Filter();
            if (filter.IsActive)
            {
                TradeItems.RequestIdentifyNow(
                    TradeItems.Items.Where(filter.Matches).Where(t => !t.IsIdentified).Select(t => t.Id).ToList());
            }
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
            suppressFilter = false;

            UpdateList();
        }

        // Click a row to pick the item: select it in the world, (re)request its appraisal
        // (fills stubs whose identify gave up), and make it the target of Add / Check Price.
        private void List_Click(object sender, int row, int col)
        {
            try
            {
                int id = int.Parse(((HudStaticText)TradeList[row][7]).Text);
                CoreManager.Current.Actions.SelectItem(id);
                CoreManager.Current.Actions.RequestId(id);

                selectedId = id;
                selectedName = TradeItems.Items.FirstOrDefault(t => t.Id == id)?.Name ?? "";
                TradeSelectedText.Text = selectedName.Length > 0 ? selectedName : "(none)";
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Tell the bot to add the picked item to the trade for purchase. We check its price
        // first so the bot's "worth N points" reply shows before it's added, then send the
        // add by item id (the bot's "add <itemId>" matches exactly, avoiding name ambiguity).
        private void AddButton_Hit(object sender, EventArgs e)
        {
            try
            {
                if (!CanCommandPartner()) return;
                Trade.SendCommand("check " + selectedName);
                Trade.SendCommand("add " + selectedId);
                Util.Chat($"Asked {Trade.PartnerName} to add {selectedName}", Util.ColorOrange, "[Oracle of Dereth] ");
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Ask the bot what the picked item is worth. The bot replies in a tell that the Trade
        // model parses, which repaints the price here.
        private void CheckButton_Hit(object sender, EventArgs e)
        {
            try
            {
                if (!CanCommandPartner()) return;
                Trade.SendCommand("check " + selectedName);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private bool CanCommandPartner()
        {
            if (string.IsNullOrEmpty(Trade.PartnerName))
            {
                Util.Chat("No trade partner.", Util.ColorOrange, "[Oracle of Dereth] ");
                return false;
            }
            if (selectedId == 0)
            {
                Util.Chat("Click an item in the trade window first.", Util.ColorOrange, "[Oracle of Dereth] ");
                return false;
            }
            return true;
        }

        private void SortName_Click(object sender, EventArgs e)
        {
            TradeItems.Sort(TradeItems.CurrentSortType == ItemList.SortType.NameAscending
                ? ItemList.SortType.NameDescending : ItemList.SortType.NameAscending);
            UpdateList();
        }

        private void SortCol1_Click(object sender, EventArgs e)
        {
            TradeItems.Sort(TradeItems.CurrentSortType == ItemList.SortType.Col1Ascending
                ? ItemList.SortType.Col1Descending : ItemList.SortType.Col1Ascending);
            UpdateList();
        }

        private void SortCol2_Click(object sender, EventArgs e)
        {
            TradeItems.Sort(TradeItems.CurrentSortType == ItemList.SortType.Col2Ascending
                ? ItemList.SortType.Col2Descending : ItemList.SortType.Col2Ascending);
            UpdateList();
        }

        private void SortCol3_Click(object sender, EventArgs e)
        {
            TradeItems.Sort(TradeItems.CurrentSortType == ItemList.SortType.Col3Ascending
                ? ItemList.SortType.Col3Descending : ItemList.SortType.Col3Ascending);
            UpdateList();
        }

        private void SortCol4_Click(object sender, EventArgs e)
        {
            TradeItems.Sort(TradeItems.CurrentSortType == ItemList.SortType.Col4Ascending
                ? ItemList.SortType.Col4Descending : ItemList.SortType.Col4Ascending);
            UpdateList();
        }

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
            if (TradeCheckButton != null) TradeCheckButton.Hit -= CheckButton_Hit;

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
