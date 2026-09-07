using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        private readonly VGInventory SavedInventory = new VGInventory();
        private HudCheckBox vgInventoryWeaponHW;
        private HudCheckBox vgInventoryWeaponFW;
        private HudCheckBox vgInventoryWeaponLW;
        private HudCheckBox vgInventoryWeapon2H;
        private HudCheckBox vgInventoryWeaponWar;
        private HudCheckBox vgInventoryWeaponVoid;
        private HudCheckBox vgInventoryWeaponOther;
        private HudCheckBox vgInventoryWeaponTW;
        private HudCheckBox vgInventoryWeaponBow;
        private HudCheckBox vgInventoryWeaponXbow;
        private readonly List<HudCheckBox> vgInventoryCategoryOrder = new List<HudCheckBox>();
        private HudCheckBox vgInventoryElementSlash;
        private HudCheckBox vgInventoryElementPierce;
        private HudCheckBox vgInventoryElementBludge;
        private HudCheckBox vgInventoryElementFire;
        private HudCheckBox vgInventoryElementFrost;
        private HudCheckBox vgInventoryElementStorm;
        private HudCheckBox vgInventoryElementAcid;
        private HudCheckBox vgInventoryElementNether;
        private HudCheckBox vgInventoryArmorHead;
        private HudCheckBox vgInventoryArmorChest;
        private HudCheckBox vgInventoryArmorAbdomen;
        private HudCheckBox vgInventoryArmorUpperArms;
        private HudCheckBox vgInventoryArmorLowerArms;
        private HudCheckBox vgInventoryArmorHands;
        private HudCheckBox vgInventoryArmorUpperLegs;
        private HudCheckBox vgInventoryArmorLowerLegs;
        private HudCheckBox vgInventoryArmorFeet;
        private HudCheckBox vgInventoryArmorSetAdept;
        private HudCheckBox vgInventoryArmorSetDefender;
        private HudCheckBox vgInventoryArmorSetDexterous;
        private HudCheckBox vgInventoryArmorSetHearty;
        private HudCheckBox vgInventoryArmorSetWise;
        private HudCheckBox vgInventoryArmorSetNoSet;
        private HudCheckBox vgInventoryArmorSetOther;
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
            vgInventoryWeaponHW = (HudCheckBox)view["VGInventoryWeaponHW"];
            vgInventoryWeaponHW.Visible = false;
            vgInventoryWeaponHW.Change += VGInventoryFilter_Change;
            vgInventoryWeaponFW = (HudCheckBox)view["VGInventoryWeaponFW"];
            vgInventoryWeaponFW.Visible = false;
            vgInventoryWeaponFW.Change += VGInventoryFilter_Change;
            vgInventoryWeaponLW = (HudCheckBox)view["VGInventoryWeaponLW"];
            vgInventoryWeaponLW.Visible = false;
            vgInventoryWeaponLW.Change += VGInventoryFilter_Change;
            vgInventoryWeapon2H = (HudCheckBox)view["VGInventoryWeapon2H"];
            vgInventoryWeapon2H.Visible = false;
            vgInventoryWeapon2H.Change += VGInventoryFilter_Change;
            vgInventoryWeaponWar = (HudCheckBox)view["VGInventoryWeaponWar"];
            vgInventoryWeaponWar.Visible = false;
            vgInventoryWeaponWar.Change += VGInventoryFilter_Change;
            vgInventoryWeaponVoid = (HudCheckBox)view["VGInventoryWeaponVoid"];
            vgInventoryWeaponOther = (HudCheckBox)view["VGInventoryWeaponOther"];
            vgInventoryWeaponTW = (HudCheckBox)view["VGInventoryWeaponTW"];
            vgInventoryWeaponBow = (HudCheckBox)view["VGInventoryWeaponBow"];
            vgInventoryWeaponXbow = (HudCheckBox)view["VGInventoryWeaponXbow"];
            vgInventoryWeaponVoid.Visible = false;
            vgInventoryWeaponOther.Visible = false;
            vgInventoryWeaponTW.Visible = false;
            vgInventoryWeaponBow.Visible = false;
            vgInventoryWeaponXbow.Visible = false;
            vgInventoryWeaponVoid.Change += VGInventoryFilter_Change;
            vgInventoryWeaponOther.Change += VGInventoryFilter_Change;
            vgInventoryWeaponTW.Change += VGInventoryFilter_Change;
            vgInventoryWeaponBow.Change += VGInventoryFilter_Change;
            vgInventoryWeaponXbow.Change += VGInventoryFilter_Change;
            vgInventoryElementSlash = (HudCheckBox)view["VGInventoryElementSlash"];
            vgInventoryElementSlash.Visible = false;
            vgInventoryElementSlash.Change += VGInventoryFilter_Change;
            vgInventoryElementPierce = (HudCheckBox)view["VGInventoryElementPierce"];
            vgInventoryElementPierce.Visible = false;
            vgInventoryElementPierce.Change += VGInventoryFilter_Change;
            vgInventoryElementBludge = (HudCheckBox)view["VGInventoryElementBludge"];
            vgInventoryElementBludge.Visible = false;
            vgInventoryElementBludge.Change += VGInventoryFilter_Change;
            vgInventoryElementFire = (HudCheckBox)view["VGInventoryElementFire"];
            vgInventoryElementFire.Visible = false;
            vgInventoryElementFire.Change += VGInventoryFilter_Change;
            vgInventoryElementFrost = (HudCheckBox)view["VGInventoryElementFrost"];
            vgInventoryElementFrost.Visible = false;
            vgInventoryElementFrost.Change += VGInventoryFilter_Change;
            vgInventoryElementStorm = (HudCheckBox)view["VGInventoryElementStorm"];
            vgInventoryElementStorm.Visible = false;
            vgInventoryElementStorm.Change += VGInventoryFilter_Change;
            vgInventoryElementAcid = (HudCheckBox)view["VGInventoryElementAcid"];
            vgInventoryElementAcid.Visible = false;
            vgInventoryElementAcid.Change += VGInventoryFilter_Change;
            vgInventoryElementNether = (HudCheckBox)view["VGInventoryElementNether"];
            vgInventoryElementNether.Visible = false;
            vgInventoryElementNether.Change += VGInventoryFilter_Change;
            vgInventoryArmorHead = (HudCheckBox)view["VGInventoryArmorHead"];
            vgInventoryArmorHead.Visible = false;
            vgInventoryArmorHead.Change += VGInventoryFilter_Change;
            vgInventoryArmorChest = (HudCheckBox)view["VGInventoryArmorChest"];
            vgInventoryArmorChest.Visible = false;
            vgInventoryArmorChest.Change += VGInventoryFilter_Change;
            vgInventoryArmorAbdomen = (HudCheckBox)view["VGInventoryArmorAbdomen"];
            vgInventoryArmorAbdomen.Visible = false;
            vgInventoryArmorAbdomen.Change += VGInventoryFilter_Change;
            vgInventoryArmorUpperArms = (HudCheckBox)view["VGInventoryArmorUpperArms"];
            vgInventoryArmorUpperArms.Visible = false;
            vgInventoryArmorUpperArms.Change += VGInventoryFilter_Change;
            vgInventoryArmorLowerArms = (HudCheckBox)view["VGInventoryArmorLowerArms"];
            vgInventoryArmorLowerArms.Visible = false;
            vgInventoryArmorLowerArms.Change += VGInventoryFilter_Change;
            vgInventoryArmorHands = (HudCheckBox)view["VGInventoryArmorHands"];
            vgInventoryArmorHands.Visible = false;
            vgInventoryArmorHands.Change += VGInventoryFilter_Change;
            vgInventoryArmorUpperLegs = (HudCheckBox)view["VGInventoryArmorUpperLegs"];
            vgInventoryArmorUpperLegs.Visible = false;
            vgInventoryArmorUpperLegs.Change += VGInventoryFilter_Change;
            vgInventoryArmorLowerLegs = (HudCheckBox)view["VGInventoryArmorLowerLegs"];
            vgInventoryArmorLowerLegs.Visible = false;
            vgInventoryArmorLowerLegs.Change += VGInventoryFilter_Change;
            vgInventoryArmorFeet = (HudCheckBox)view["VGInventoryArmorFeet"];
            vgInventoryArmorFeet.Visible = false;
            vgInventoryArmorFeet.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetAdept = (HudCheckBox)view["VGInventoryArmorSetAdept"];
            vgInventoryArmorSetAdept.Visible = false;
            vgInventoryArmorSetAdept.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetDefender = (HudCheckBox)view["VGInventoryArmorSetDefender"];
            vgInventoryArmorSetDefender.Visible = false;
            vgInventoryArmorSetDefender.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetDexterous = (HudCheckBox)view["VGInventoryArmorSetDexterous"];
            vgInventoryArmorSetDexterous.Visible = false;
            vgInventoryArmorSetDexterous.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetHearty = (HudCheckBox)view["VGInventoryArmorSetHearty"];
            vgInventoryArmorSetHearty.Visible = false;
            vgInventoryArmorSetHearty.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetWise = (HudCheckBox)view["VGInventoryArmorSetWise"];
            vgInventoryArmorSetWise.Visible = false;
            vgInventoryArmorSetWise.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetNoSet = (HudCheckBox)view["VGInventoryArmorSetNoSet"];
            vgInventoryArmorSetNoSet.Visible = false;
            vgInventoryArmorSetNoSet.Change += VGInventoryFilter_Change;
            vgInventoryArmorSetOther = (HudCheckBox)view["VGInventoryArmorSetOther"];
            vgInventoryArmorSetOther.Visible = false;
            vgInventoryArmorSetOther.Change += VGInventoryFilter_Change;
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
        }

        private void DisposeVGInventory()
        {
            vgInventoryTimer.Stop();
            vgInventoryTimer.Tick -= VGInventorySearchTick;
            vgInventoryTimer.Dispose();
            SavedInventory.CancelSearch();
            vgInventoryWeaponHW.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponFW.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponLW.Change -= VGInventoryFilter_Change;
            vgInventoryWeapon2H.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponWar.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponVoid.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponOther.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponTW.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponBow.Change -= VGInventoryFilter_Change;
            vgInventoryWeaponXbow.Change -= VGInventoryFilter_Change;
            vgInventoryElementSlash.Change -= VGInventoryFilter_Change;
            vgInventoryElementPierce.Change -= VGInventoryFilter_Change;
            vgInventoryElementBludge.Change -= VGInventoryFilter_Change;
            vgInventoryElementFire.Change -= VGInventoryFilter_Change;
            vgInventoryElementFrost.Change -= VGInventoryFilter_Change;
            vgInventoryElementStorm.Change -= VGInventoryFilter_Change;
            vgInventoryElementAcid.Change -= VGInventoryFilter_Change;
            vgInventoryElementNether.Change -= VGInventoryFilter_Change;
            vgInventoryArmorHead.Change -= VGInventoryFilter_Change;
            vgInventoryArmorChest.Change -= VGInventoryFilter_Change;
            vgInventoryArmorAbdomen.Change -= VGInventoryFilter_Change;
            vgInventoryArmorUpperArms.Change -= VGInventoryFilter_Change;
            vgInventoryArmorLowerArms.Change -= VGInventoryFilter_Change;
            vgInventoryArmorHands.Change -= VGInventoryFilter_Change;
            vgInventoryArmorUpperLegs.Change -= VGInventoryFilter_Change;
            vgInventoryArmorLowerLegs.Change -= VGInventoryFilter_Change;
            vgInventoryArmorFeet.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetAdept.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetDefender.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetDexterous.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetHearty.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetWise.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetNoSet.Change -= VGInventoryFilter_Change;
            vgInventoryArmorSetOther.Change -= VGInventoryFilter_Change;
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
            vgInventoryCategoryOrder.Clear();
            selectedVGInventoryItem = null;
        }

        private ItemFilter VGInventoryFilter() => new ItemFilter
        {
            Text = VGInventoryFilterText.Text,
            Weapons = VGInventoryFilterWeapons.Checked,
            ElementSlash = vgInventoryElementSlash.Checked,
            ElementPierce = vgInventoryElementPierce.Checked,
            ElementBludge = vgInventoryElementBludge.Checked,
            ElementFire = vgInventoryElementFire.Checked,
            ElementFrost = vgInventoryElementFrost.Checked,
            ElementStorm = vgInventoryElementStorm.Checked,
            ElementAcid = vgInventoryElementAcid.Checked,
            ElementNether = vgInventoryElementNether.Checked,
            WeaponHW = vgInventoryWeaponHW.Checked,
            WeaponFW = vgInventoryWeaponFW.Checked,
            WeaponLW = vgInventoryWeaponLW.Checked,
            Weapon2H = vgInventoryWeapon2H.Checked,
            WeaponWar = vgInventoryWeaponWar.Checked,
            WeaponVoid = vgInventoryWeaponVoid.Checked,
            WeaponOther = vgInventoryWeaponOther.Checked,
            WeaponTW = vgInventoryWeaponTW.Checked,
            WeaponBow = vgInventoryWeaponBow.Checked,
            WeaponXbow = vgInventoryWeaponXbow.Checked,
            Armor = VGInventoryFilterArmor.Checked,
            ArmorSetAdept = vgInventoryArmorSetAdept.Checked,
            ArmorSetDefender = vgInventoryArmorSetDefender.Checked,
            ArmorSetDexterous = vgInventoryArmorSetDexterous.Checked,
            ArmorSetHearty = vgInventoryArmorSetHearty.Checked,
            ArmorSetWise = vgInventoryArmorSetWise.Checked,
            ArmorSetNoSet = vgInventoryArmorSetNoSet.Checked,
            ArmorSetOther = vgInventoryArmorSetOther.Checked,
            ArmorSlots = (vgInventoryArmorHead.Checked ? ItemInfo.ArmorSlot.Head : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorChest.Checked ? ItemInfo.ArmorSlot.Chest : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorAbdomen.Checked ? ItemInfo.ArmorSlot.Abdomen : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorUpperArms.Checked ? ItemInfo.ArmorSlot.UpperArms : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorLowerArms.Checked ? ItemInfo.ArmorSlot.LowerArms : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorHands.Checked ? ItemInfo.ArmorSlot.Hands : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorUpperLegs.Checked ? ItemInfo.ArmorSlot.UpperLegs : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorLowerLegs.Checked ? ItemInfo.ArmorSlot.LowerLegs : ItemInfo.ArmorSlot.None) |
                (vgInventoryArmorFeet.Checked ? ItemInfo.ArmorSlot.Feet : ItemInfo.ArmorSlot.None),
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

        private void UpdateVGInventorySubfilters(HudCheckBox changed)
        {
            var categories = new[] { VGInventoryFilterWeapons, VGInventoryFilterArmor,
                VGInventoryFilterClothing, VGInventoryFilterJewelry, VGInventoryFilterCloaks,
                VGInventoryFilterSummons, VGInventoryFilterAetheria, VGInventoryFilterSalvage, VGInventoryFilterOther };
            if (categories.Contains(changed))
            {
                vgInventoryCategoryOrder.Remove(changed);
                if (changed.Checked) vgInventoryCategoryOrder.Add(changed);
            }
            HudCheckBox active = vgInventoryCategoryOrder.LastOrDefault();
            vgInventoryArmorSetAdept.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetDefender.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetDexterous.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetHearty.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetWise.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetNoSet.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorSetOther.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorHead.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorChest.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorAbdomen.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorUpperArms.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorLowerArms.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorHands.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorUpperLegs.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorLowerLegs.Visible = active == VGInventoryFilterArmor;
            vgInventoryArmorFeet.Visible = active == VGInventoryFilterArmor;
            vgInventoryElementSlash.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementPierce.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementBludge.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementFire.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementFrost.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementStorm.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementAcid.Visible = active == VGInventoryFilterWeapons;
            vgInventoryElementNether.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponHW.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponFW.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponLW.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeapon2H.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponWar.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponVoid.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponOther.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponTW.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponBow.Visible = active == VGInventoryFilterWeapons;
            vgInventoryWeaponXbow.Visible = active == VGInventoryFilterWeapons;
        }

        private void VGInventoryFilter_Change(object sender, EventArgs e)
        {
            if (suppressVGInventoryFilter) return;
            UpdateVGInventorySubfilters(sender as HudCheckBox);
            SavedInventory.CancelSearch();
            vgInventorySearchDue = DateTime.UtcNow.AddMilliseconds(350);
            vgInventoryTimer.Start();
            VGInventoryText.Text = "Waiting to search - showing previous results.";
        }

        private void VGInventoryFilterReset_Hit(object sender, EventArgs e)
        {
            suppressVGInventoryFilter = true;
            vgInventoryCategoryOrder.Clear();
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
            vgInventoryWeaponHW.Checked = false;
            vgInventoryWeaponHW.Visible = false;
            vgInventoryWeaponFW.Checked = false;
            vgInventoryWeaponFW.Visible = false;
            vgInventoryWeaponLW.Checked = false;
            vgInventoryWeaponLW.Visible = false;
            vgInventoryWeapon2H.Checked = false;
            vgInventoryWeapon2H.Visible = false;
            vgInventoryWeaponWar.Checked = false;
            vgInventoryWeaponWar.Visible = false;
            vgInventoryWeaponVoid.Checked = false;
            vgInventoryWeaponOther.Checked = false;
            vgInventoryWeaponTW.Checked = false;
            vgInventoryWeaponBow.Checked = false;
            vgInventoryWeaponXbow.Checked = false;
            vgInventoryWeaponVoid.Visible = false;
            vgInventoryWeaponOther.Visible = false;
            vgInventoryWeaponTW.Visible = false;
            vgInventoryWeaponBow.Visible = false;
            vgInventoryWeaponXbow.Visible = false;
            vgInventoryElementSlash.Checked = false;
            vgInventoryElementSlash.Visible = false;
            vgInventoryElementPierce.Checked = false;
            vgInventoryElementPierce.Visible = false;
            vgInventoryElementBludge.Checked = false;
            vgInventoryElementBludge.Visible = false;
            vgInventoryElementFire.Checked = false;
            vgInventoryElementFire.Visible = false;
            vgInventoryElementFrost.Checked = false;
            vgInventoryElementFrost.Visible = false;
            vgInventoryElementStorm.Checked = false;
            vgInventoryElementStorm.Visible = false;
            vgInventoryElementAcid.Checked = false;
            vgInventoryElementAcid.Visible = false;
            vgInventoryElementNether.Checked = false;
            vgInventoryElementNether.Visible = false;
            vgInventoryArmorHead.Checked = false;
            vgInventoryArmorHead.Visible = false;
            vgInventoryArmorChest.Checked = false;
            vgInventoryArmorChest.Visible = false;
            vgInventoryArmorAbdomen.Checked = false;
            vgInventoryArmorAbdomen.Visible = false;
            vgInventoryArmorUpperArms.Checked = false;
            vgInventoryArmorUpperArms.Visible = false;
            vgInventoryArmorLowerArms.Checked = false;
            vgInventoryArmorLowerArms.Visible = false;
            vgInventoryArmorHands.Checked = false;
            vgInventoryArmorHands.Visible = false;
            vgInventoryArmorUpperLegs.Checked = false;
            vgInventoryArmorUpperLegs.Visible = false;
            vgInventoryArmorLowerLegs.Checked = false;
            vgInventoryArmorLowerLegs.Visible = false;
            vgInventoryArmorFeet.Checked = false;
            vgInventoryArmorFeet.Visible = false;
            vgInventoryArmorSetAdept.Checked = false;
            vgInventoryArmorSetAdept.Visible = false;
            vgInventoryArmorSetDefender.Checked = false;
            vgInventoryArmorSetDefender.Visible = false;
            vgInventoryArmorSetDexterous.Checked = false;
            vgInventoryArmorSetDexterous.Visible = false;
            vgInventoryArmorSetHearty.Checked = false;
            vgInventoryArmorSetHearty.Visible = false;
            vgInventoryArmorSetWise.Checked = false;
            vgInventoryArmorSetWise.Visible = false;
            vgInventoryArmorSetNoSet.Checked = false;
            vgInventoryArmorSetNoSet.Visible = false;
            vgInventoryArmorSetOther.Checked = false;
            vgInventoryArmorSetOther.Visible = false;
            suppressVGInventoryFilter = false;
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
            // Always retain access to the saved description, including offline items.
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

