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
            { 2_06, 420 }, // Conquest (Custom Augs)

            // Quests Tab
            { 3_00, 430 }, // John
            { 3_01, 430 }, // Markers
            { 3_02, 350 }, // Flags
            { 3_03, 450 }, // Facility Hub
            { 3_04, 560 }, // Titles (Available and Unavailable)
            { 3_05, 430 }, // Custom Quests

            // Server
            { 4_00, 580 }, // Bank

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
            { 2_06, 475 }, // Conquest (Custom Augs)

            // Quests Tab
            { 3_00, 545}, // John
            { 3_01, 545 }, // Markers
            { 3_02, 540 }, // Flags
            { 3_03, 485 }, // Facility Hub
            { 3_04, 545 }, // Titles (Available and Unavailable)
            { 3_05, 545 }, // Custom Quests

            // Server
            { 4_00, 300 }, // Bank

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
                InitJohn();
                InitMarkers();
                InitFlags();
                InitFacility();
                InitTitles();
                InitCustomQuests();
                InitConquestAugmentations();
                InitConquestBank();
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
                DisposeJohn();
                DisposeMarkers();
                DisposeFlags();
                DisposeFacility();
                DisposeTitles();
                DisposeCustomQuests();
                DisposeConquestAugmentations();
                DisposeConquestBank();
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
            if (AssignedImages.Count > 1000) AssignedImages.Clear();

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
            if (currentTab == 2_06) { UpdateConquestAugmentations(); }

            // Quests Tab
            if (currentTab == 3_00) { UpdateJohn(); }
            if (currentTab == 3_01) { UpdateMarkers(); }
            if (currentTab == 3_02) { UpdateFlags(); }
            if (currentTab == 3_03) { UpdateFacility(); }
            if (currentTab == 3_04) { UpdateTitles(); }
            if (currentTab == 3_05) { UpdateCustomQuests(); }

            // Server Tab
            if (currentTab == 4_00) { UpdateConquestBank(); }

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

            // Display feedback
            Util.Chat("Quest data updated.", Util.ColorPink);

            // Quests are now unchanged
            QuestFlag.QuestsChanged = false;
        }
    }
}
