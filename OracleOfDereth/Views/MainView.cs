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
            { 3_00, 640 }, // Flags (every quest flag)
            { 3_01, 430 }, // John
            { 3_02, 430 }, // Markers
            { 3_03, 350 }, // Flaggings
            { 3_04, 450 }, // Facility Hub
            { 3_05, 560 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 650 }, // Augs (Conquest)
            { 4_01, 580 }, // Bank
            { 4_02, 600 }, // Fship (recruiting fellowships)
            { 4_03, 430 }, // Quests (Custom Quests)

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
            { 3_00, 570 }, // Flags (every quest flag) — taller: three rows of chrome above the list
            { 3_01, 545}, // John
            { 3_02, 545 }, // Markers
            { 3_03, 490 }, // Flaggings
            { 3_04, 485 }, // Facility Hub
            { 3_05, 545 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 505 }, // Augs (Conquest)
            { 4_01, 350 }, // Bank
            { 4_02, 545 }, // Fship (recruiting fellowships)
            { 4_03, 545 }, // Quests (Custom Quests)

            // About / Settings / Help
            { 5_00, 270 }, // About
            { 5_01, 400 }, // Settings
            { 5_02, 340 }, // Help
        };

        // Assign Images Tracking
        private Dictionary<HudPictureBox, int> AssignedImages = new Dictionary<HudPictureBox, int>();

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
                AssignedImages.Clear();

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
                DisposeAugmentations();
                DisposeCredits();
                DisposeRecalls();
                DisposeLuminance();
                DisposeSociety();
                DisposeSettings();

                // Other cleanup
                AssignedImages.Clear();
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

        private void QuestFlagsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        private void AssignImage(HudPictureBox row, int icon)
        {
            // Lists that rebuild via ClearRows()/RemoveRow() leave their old (destroyed) boxes
            // as dead keys here. Cap the cache so those can't accumulate without bound — the
            // live rows just re-cache on the next paint. Cheaper than cleaning every call site.
            // The cap has to clear the live set comfortably: the Flags tab alone holds a box per
            // quest flag (~4,300), and a cap below that would flush the cache on every paint,
            // costing an image assignment per row instead of saving one.
            if (AssignedImages.Count > 10000) AssignedImages.Clear();

            if (AssignedImages.TryGetValue(row, out int assignedIcon) && assignedIcon == icon) return;

            if (icon == 0) {
                row.Image = null;
                AssignedImages.Remove(row);
            } else {
                row.Image = icon;
                AssignedImages[row] = icon;
            }
        }

        private void AssignImage(HudPictureBox row, bool completed)
        {
            if (completed) { AssignImage(row, IconComplete); } else { AssignImage(row, IconNotComplete); }
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
            if (currentTab == 3_00) { UpdateQuests(); }
            if (currentTab == 3_01) { UpdateJohn(); }
            if (currentTab == 3_02) { UpdateMarkers(); }
            if (currentTab == 3_03) { UpdateFlags(); }
            if (currentTab == 3_04) { UpdateFacility(); }
            if (currentTab == 3_05) { UpdateTitles(); }

            // Server Tab
            if (currentTab == 4_00) { UpdateConquestAugmentations(); }
            if (currentTab == 4_01) { UpdateConquestBank(); }
            if (currentTab == 4_02) { UpdateConquestFship(); }
            if (currentTab == 4_03) { UpdateCustomQuests(); }

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
            // Update anything that relies on quest flags
            UpdateJohnList();
            UpdateAugmentationQuestsList();
            UpdateCreditsList();
            UpdateFlagsList();
            UpdateLuminanceList();
            UpdateMarkersList();

            // /myquests is the source of truth for what flags exist, so fold anything it
            // reported that quests.csv doesn't list into the collection before the next paint.
            Quest.MergeQuestFlags();

            // The Flags tab paints thousands of rows, so it only repaints on its own tick,
            // and only while it's the open tab — not from here, off-screen.
            questsListStale = true;

            // Display feedback
            Util.Chat("Quest data updated.", Util.ColorPink);

            // Quests are now unchanged
            QuestFlag.QuestsChanged = false;
        }
    }
}
