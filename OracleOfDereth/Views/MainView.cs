using AcClient;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using MyClasses.MetaViewWrappers;
using MyClasses.MetaViewWrappers.DecalControls;
using MyClasses.MetaViewWrappers.VirindiViewServiceHudControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirindiViewService;
using VirindiViewService.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OracleOfDereth
{
    partial class MainView : IDisposable
    {
        // Main View
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        readonly int IconComplete = 0x60011F9;   // Green Circle
        readonly int IconNotComplete = 0x60011F8;    // Red Circle
        readonly int IconSort = 0x60011F7;    // Sort Icon 6D76
        readonly ACImage ImageDisabled = new ACImage(Color.FromArgb(255, 75, 75, 75));
        readonly Color ColorSelected = Color.Orange;

        public HudTabView MainViewNotebook { get; private set; }
        public HudTabView StatusViewNotebook { get; private set; }
        public HudTabView CharacterViewNotebook { get; private set; }
        public HudTabView QuestsViewNotebook { get; private set; }
        public HudTabView ServerViewNotebook { get; private set; }
        public HudTabView TopViewNotebook { get; private set; }
        public HudTabView AboutViewNotebook { get; private set; }

        private Dictionary<int, int> MainViewWidths = new Dictionary<int, int>
        {
            // Status Tab
            { 1_00, 240 }, // HUD
            { 1_01, 460 }, // Buffs
            { 1_02, 290 }, // Nearby
            { 1_03, 250 }, // Fellowship
            { 1_04, 1280 }, // Items

            // Character Tab
            { 2_00, 650 }, // Augmentations
            { 2_01, 420 }, // Cantrips
            { 2_02, 420 }, // Credits
            { 2_03, 650 }, // Luminance
            { 2_04, 420 }, // Recalls
            { 2_05, 530 }, // Society

            // Quests Tab
            { 3_00, 430 }, // John
            { 3_01, 640 }, // Flags (every quest flag)
            { 3_02, 350 }, // Flaggings
            { 3_03, 450 }, // Hub
            { 3_04, 430 }, // Markers
            { 3_05, 560 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 650 }, // Augs (Conquest)
            { 4_01, 580 }, // Bank
            { 4_02, 600 }, // Fship (recruiting fellowships)
            { 4_03, 430 }, // Quests (Custom Quests)
            { 4_04, 450 }, // Top (leaderboards; every sub-tab is the same shape)

            // About / Settings / Help
            { 5_00, 350 }, // About
            { 5_01, 350 }, // Settings
            { 5_02, 540 }, // Help
        };

        private Dictionary<int, int> MainViewHeights = new Dictionary<int, int>
        {
            // Status Tab
            { 1_00, 320 }, // HUD
            { 1_01, 545 }, // Buffs
            { 1_02, 320 }, // Nearbys
            { 1_03, 380 }, // Fellowship
            { 1_04, 570 }, // Items

            // Character Tab
            { 2_00, 550 }, // Augmentations
            { 2_01, 550 }, // Cantrips
            { 2_02, 165 }, // Credits
            { 2_03, 550 }, // Luminance
            { 2_04, 435 }, // Recalls
            { 2_05, 570 }, // Society

            // Quests Tab
            { 3_00, 545}, // John
            { 3_01, 570 }, // Flags (every quest flag) — taller: three rows of chrome above the list
            { 3_02, 490 }, // Flaggings
            { 3_03, 485 }, // Hub
            { 3_04, 545 }, // Markers
            { 3_05, 545 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 505 }, // Augs (Conquest)
            { 4_01, 350 }, // Bank
            { 4_02, 545 }, // Fship (recruiting fellowships)
            { 4_03, 545 }, // Quests (Custom Quests)
            { 4_04, 555 }, // Top (leaderboards) — taller: a third row of tabs above the list

            // About / Settings / Help
            { 5_00, 270 }, // About
            { 5_01, 400 }, // Settings
            { 5_02, 340 }, // Help
        };

        public MainView()
        {
            try
            {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("OracleOfDereth.mainView.xml", out properties, out controls);

                // Display the view
                view = new VirindiViewService.HudView(properties, controls);
                if (view == null) { return; }

                // Make the view resizable. Default max client area is the XML size, which caps
                // how wide the Items tab can be dragged — raise it (other tabs stay width-locked
                // in MainView_Resized).
                view.UserResizeable = true;
                view.MaximumClientArea = new Size(1920, 1080);
                view.Resize += MainView_Resized;

                // Main Notebook
                MainViewNotebook = (HudTabView)view["MainViewNotebook"];
                MainViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Hud Notebook
                StatusViewNotebook = (HudTabView)view["StatusViewNotebook"];
                StatusViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Character Notebook
                CharacterViewNotebook = (HudTabView)view["CharacterViewNotebook"];
                CharacterViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Quests Notebook
                QuestsViewNotebook = (HudTabView)view["QuestsViewNotebook"];
                QuestsViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Server (Conquest) Notebook
                ServerViewNotebook = (HudTabView)view["ServerViewNotebook"];
                ServerViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Top (leaderboards) Notebook, nested inside the Server tab
                TopViewNotebook = (HudTabView)view["TopViewNotebook"];
                TopViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // About Notebook
                AboutViewNotebook = (HudTabView)view["AboutViewNotebook"];
                AboutViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                InitStatusHud();
                InitBuffs();
                InitFellowship();
                InitItems();
                InitNearby();
                InitQuests();
                InitJohn();
                InitMarkers();
                InitFlags();
                InitFacility();
                InitTitles();
                InitCustomQuests();
                InitConquestAugmentations();
                InitConquestBank();
                InitConquestFship();
                InitTop();
                InitAugmentations();
                InitCantrips();
                InitCredits();
                InitRecalls();
                InitLuminance();
                InitSociety();
                InitSettings();
                InitHelp();

                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Shutdown
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                view.Resize -= MainView_Resized;
                MainViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                CharacterViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                StatusViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                QuestsViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                ServerViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                TopViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                AboutViewNotebook.OpenTabChange -= Notebook_OpenTabChange;

                DisposeItems();
                DisposeNearby();
                DisposeFellowship();
                DisposeQuests();
                DisposeJohn();
                DisposeMarkers();
                DisposeFlags();
                DisposeFacility();
                DisposeTitles();
                DisposeCustomQuests();
                DisposeConquestAugmentations();
                DisposeConquestBank();
                DisposeConquestFship();
                DisposeTop();
                DisposeAugmentations();
                DisposeCredits();
                DisposeRecalls();
                DisposeLuminance();
                DisposeSociety();
                DisposeSettings();
                DisposeButtonFlashes();

                // Other cleanup
                view?.Dispose();
            }
        }

        public bool IsItemsTabActive() { return view.Visible && CurrentTab() == 1_04; }

        private int CurrentTab()
        {
            int mainTab = MainViewNotebook.CurrentTab + 1;

            if (mainTab == 1) { return (mainTab * 100) + StatusViewNotebook.CurrentTab; }
            if (mainTab == 2) { return (mainTab * 100) + CharacterViewNotebook.CurrentTab; }
            if (mainTab == 3) { return (mainTab * 100) + QuestsViewNotebook.CurrentTab; }
            if (mainTab == 4) { return (mainTab * 100) + ServerViewNotebook.CurrentTab; }
            if (mainTab == 5) { return (mainTab * 100) + AboutViewNotebook.CurrentTab; }

            // Main Tab
            return mainTab * 100;
        }

        private void Notebook_OpenTabChange(object sender, EventArgs e)
        {
            view.Height = MainViewHeights[CurrentTab()];
            view.Width = MainViewWidths[CurrentTab()];
            Update();
        }

        private void MainView_Resized(object sender, EventArgs e)
        {
            int tab = CurrentTab();

            // Save the new view height
            MainViewHeights[tab] = view.Height;

            if (tab == 1_04)
            {
                // Items tab is freely widenable — remember its width instead of locking it.
                MainViewWidths[tab] = view.Width;
            }
            else
            {
                // Every other tab keeps its fixed width.
                view.Width = MainViewWidths[tab];
            }
        }

        // Shared by the ten quest-flag Refresh buttons across the Character and Quests tabs, so the
        // one that was clicked comes from the sender.
        private void QuestFlagsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
            FlashButton(sender as HudButton);
        }

        // ---- Refresh button feedback -----------------------------------------------------------
        // A Refresh button fires a server command whose reply is now kept out of chat (see
        // Setting.SuppressPluginRefreshChat), so without this a click has no visible effect at all
        // — the list just quietly updates a moment later. Flashing the label is the acknowledgement.
        //
        // Every one of these buttons reads "Refresh" (all 21 in mainView.xml), so there's nothing
        // per-button to remember except which one to put back. Only one can be mid-flash: a click
        // is a single button, and the auto-refresh flashes belong to whichever tab is drawing.
        // Flashing a second one just restores the first early rather than stranding its label.
        private const string RefreshLabel = "Refresh";
        private const string RefreshingLabel = "Refreshing...";

        // Restored on the first Update() tick at or after the deadline, and that tick is 1s — so
        // the label actually sits for between half a second and a second and a half, depending on
        // where the click lands in the tick. The deadline is what stops a click that lands just
        // before a tick from flashing for no visible time at all.
        private static readonly TimeSpan ButtonFlashDuration = TimeSpan.FromMilliseconds(500);

        private HudButton FlashedButton;
        private DateTime FlashedUntil;

        private void FlashButton(HudButton button)
        {
            if (button == null) { return; }

            RestoreFlashedButton();

            button.Text = RefreshingLabel;
            FlashedButton = button;
            FlashedUntil = DateTime.UtcNow + ButtonFlashDuration;
        }

        // Called from Update() once the deadline passes, and from FlashButton when a second button
        // takes the slot early.
        private void RestoreFlashedButton()
        {
            if (FlashedButton == null) { return; }

            FlashedButton.Text = RefreshLabel;
            FlashedButton = null;
        }

        // Just drop the reference — the control is going away with the view.
        private void DisposeButtonFlashes()
        {
            FlashedButton = null;
        }

        // Only swap the image when it actually changes; assigning is comparatively expensive on
        // the lists that paint thousands of rows a tick.
        //
        // The box itself is the record of what it's showing: an int converts implicitly to
        // ACImage and PortalImageID reads that id straight back, so there's nothing to cache.
        // A side dictionary keyed on the box would need every list that trims rows to remove
        // its boxes, or it pins destroyed controls alive forever — see the Items renderer,
        // which still carries one because it tracks two boxes per row.
        private void AssignImage(HudPictureBox row, int icon)
        {
            // A cleared box reads back as null rather than 0, which is the same "no image".
            int current = row.Image == null ? 0 : row.Image.PortalImageID;
            if (current == icon) return;

            if (icon == 0) {
                row.Image = null;
            } else {
                row.Image = icon;
            }
        }

        private void AssignImage(HudPictureBox row, bool completed)
        {
            if (completed) { AssignImage(row, IconComplete); } else { AssignImage(row, IconNotComplete); }
        }

        // VVS re-renders a cell on every Text assignment, so skip the ones that would write the
        // same string back. Barely matters on a 30-row list; on the thousands of rows the Flags
        // tab paints it's the difference between a free repaint and a costly one.
        private void SetText(HudList.HudListRowAccessor row, int column, string value)
        {
            HudStaticText cell = (HudStaticText)row[column];
            if (cell.Text != value) { cell.Text = value; }
        }

        // Same idea for a standalone label rather than a list cell: most of these are rewritten
        // every tick with the string they already hold.
        private void SetText(HudStaticText control, string value)
        {
            if (control.Text != value) { control.Text = value; }
        }

        private void AssignSelected(HudList.HudListRowAccessor row, bool selected, List<int> columns)
        {
            foreach (int column in columns)
            {
                if (selected) {
                    ((HudStaticText)row[column]).TextColor = ColorSelected;
                } else {
                    ((HudStaticText)row[column]).ResetTextColor();
                }
            }
        }

        // The Tick
        public void Update()
        {
            if (QuestFlag.QuestsChanged) { UpdateQuestFlags(); }

            // Runs for every tab, so a flashed Refresh label restores even if you switch away.
            if (FlashedButton != null && DateTime.UtcNow >= FlashedUntil) { RestoreFlashedButton(); }

            // Runs every tick regardless of the active tab, so auto-deposit still fires while you're
            // on another tab (or the window is closed).
            Bank.AutoDepositTick();

            int currentTab = CurrentTab();

            // Status Tab
            if (currentTab == 1_00) { UpdateHud(); }
            if (currentTab == 1_01) { UpdateBuffs(); }
            if (currentTab == 1_02) { UpdateNearby(); } // If this changes update UpdateTarget() method below
            if (currentTab == 1_03) { UpdateFellowship(); } // If this changes update UpdateTarget() method below
            if (currentTab == 1_04) { UpdateItems(); }

            // Character Tab
            if (currentTab == 2_00) { UpdateAugmentations(); }
            if (currentTab == 2_01) { UpdateCantrips(); }
            if (currentTab == 2_02) { UpdateCredits(); }
            if (currentTab == 2_03) { UpdateLuminance(); }
            if (currentTab == 2_04) { UpdateRecalls(); }
            if (currentTab == 2_05) { UpdateSociety(); }

            // Quests Tab
            if (currentTab == 3_00) { UpdateJohn(); }
            if (currentTab == 3_01) { UpdateQuests(); }
            if (currentTab == 3_02) { UpdateFlags(); }
            if (currentTab == 3_03) { UpdateFacility(); }
            if (currentTab == 3_04) { UpdateMarkers(); }
            if (currentTab == 3_05) { UpdateTitles(); }

            // Server Tab
            if (currentTab == 4_00) { UpdateConquestAugmentations(); }
            if (currentTab == 4_01) { UpdateConquestBank(); }
            if (currentTab == 4_02) { UpdateConquestFship(); }
            if (currentTab == 4_03) { UpdateCustomQuests(); }
            if (currentTab == 4_04) { UpdateTop(); }

            // About / Settings / Help
            if (currentTab == 5_00) {; }
            if (currentTab == 5_01) { UpdateSettings(); }
            if (currentTab == 5_02) { UpdateHelp(); }
        }

        // Selected target changed
        public void UpdateTarget()
        {
            int currentTab = CurrentTab();
            if (currentTab == 1_02) { UpdateNearbyList(); }
            if (currentTab == 1_03) { UpdateFellowshipButtons(); }
        }

        // Quest Flag Changes
        public void UpdateQuestFlags()
        {
            // /myquests is the source of truth for what flags exist, so fold anything it
            // reported that quests.csv doesn't list into the collection before anything reads
            // it. No repaint from here: the Flags tab paints thousands of rows and does it on
            // its own tick, only while it's the open tab — unlike the small lists below, which
            // are cheap to redraw.
            Quest.MergeQuestFlags();

            // Update anything that relies on quest flags
            UpdateJohnList();
            UpdateAugmentationQuestsList();
            UpdateCreditsList();
            UpdateFlagsList();
            UpdateLuminanceList();
            UpdateMarkersList();

            // Display feedback
            Util.Chat("Quest data updated.", Util.ColorPink);

            // Quests are now unchanged
            QuestFlag.QuestsChanged = false;
        }
    }
}
