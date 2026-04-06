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

        private Dictionary<int, int> MainViewWidths = new Dictionary<int, int>
        {
            // Status Tab
            { 1_00, 210 }, // HUD
            { 1_01, 460 }, // Buffs
            { 1_02, 290 }, // Nearby
            { 1_03, 250 }, // Fellowship
            { 1_04, 950 }, // Trade

            // Character Tab
            { 2_00, 650 }, // Augmentations
            { 2_01, 350 }, // Cantrips
            { 2_02, 350 }, // Credits
            { 2_03, 650 }, // Luminance
            { 2_04, 350 }, // Recalls

            // Quests Tab
            { 3_00, 430 }, // John
            { 3_01, 430 }, // Markers
            { 3_02, 350 }, // Flags
            { 3_03, 450 }, // Facility Hub

            // Titles
            { 4_00, 560 }, // Available and Unavailable

            // About
            { 5_00, 350 }, // About
        };

        private Dictionary<int, int> MainViewHeights = new Dictionary<int, int>
        {
            // Status Tab
            { 1_00, 320 }, // HUD
            { 1_01, 545 }, // Buffs
            { 1_02, 320 }, // Nearbys
            { 1_03, 380 }, // Fellowship
            { 1_04, 450 }, // Trade

            // Character Tab
            { 2_00, 550 }, // Augmentations
            { 2_01, 550 }, // Cantrips
            { 2_02, 165 }, // Credits
            { 2_03, 550 }, // Luminance
            { 2_04, 435 }, // Recalls

            // Quests Tab
            { 3_00, 545}, // John
            { 3_01, 545 }, // Markers
            { 3_02, 520 }, // Flags
            { 3_03, 485 }, // Facility Hub

            // Titles
            { 4_00, 545 }, // Available

            // About
            { 5_00, 270 }, // About
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

                // Make the view resizable
                view.UserResizeable = true;
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

                InitStatusHud();
                InitBuffs();
                InitFellowship();
                InitTrade();
                InitNearby();
                InitJohn();
                InitMarkers();
                InitFlags();
                InitFacility();
                InitTitles();
                InitAugmentations();
                InitCantrips();
                InitCredits();
                InitRecalls();
                InitLuminance();

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

                DisposeTrade();
                DisposeNearby();
                DisposeFellowship();
                DisposeJohn();
                DisposeMarkers();
                DisposeFlags();
                DisposeFacility();
                DisposeTitles();
                DisposeAugmentations();
                DisposeCredits();
                DisposeRecalls();
                DisposeLuminance();

                // Other cleanup
                AssignedImages.Clear();
                view?.Dispose();
            }
        }

        public bool IsTradeTabActive() { return view.Visible && CurrentTab() == 1_04; }

        private int CurrentTab()
        {
            int mainTab = MainViewNotebook.CurrentTab + 1;

            if (mainTab == 1) { return (mainTab * 100) + StatusViewNotebook.CurrentTab; }
            if (mainTab == 2) { return (mainTab * 100) + CharacterViewNotebook.CurrentTab; }
            if (mainTab == 3) { return (mainTab * 100) + QuestsViewNotebook.CurrentTab; }

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
            // Save the new view height
            MainViewHeights[CurrentTab()] = view.Height;

            // Prevent width updates
            view.Width = MainViewWidths[CurrentTab()];
        }

        private void QuestFlagsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        private void AssignImage(HudPictureBox row, int icon)
        {
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
            if (currentTab == 1_04) { UpdateTrade(); }

            // Character Tab
            if (currentTab == 2_00) { UpdateAugmentations(); }
            if (currentTab == 2_01) { UpdateCantrips(); }
            if (currentTab == 2_02) { UpdateCredits(); }
            if (currentTab == 2_03) { UpdateLuminance(); }
            if (currentTab == 2_04) { UpdateRecalls(); }

            // Quests Tab
            if (currentTab == 3_00) { UpdateJohn(); }
            if (currentTab == 3_01) { UpdateMarkers(); }
            if (currentTab == 3_02) { UpdateFlags(); }
            if (currentTab == 3_03) { UpdateFacility(); }

            // Titles Tab
            if (currentTab == 4_00) { UpdateTitles(); }
            if (currentTab == 4_01) { UpdateTitles(); }

            // About
            if (currentTab == 5_00) {; }
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
