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

        // Reorder arrows on the Favorites tab, the same pair SSSort draws in its move-up /
        // move-down columns.
        readonly int IconArrowUp = 0x60028FC;
        readonly int IconArrowDown = 0x60028FD;
        readonly ACImage ImageDisabled = new ACImage(Color.FromArgb(255, 75, 75, 75));
        readonly Color ColorSelected = Color.Orange;

        // The clicked row on the Flags and Favorites tabs. Deliberately not ColorSelected: that one
        // already means "flag the master list doesn't know", and a row can be both at once.
        readonly Color ColorRowSelected = Color.DeepSkyBlue;

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
            { 3_02, 640 }, // Favorites (same columns as Flags)
            { 3_03, 350 }, // Flaggings
            { 3_04, 450 }, // Hub
            { 3_05, 430 }, // Markers
            { 3_06, 560 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 720 }, // Augs (Conquest)
            { 4_01, 720 }, // Experience (character summary + XP bonuses) — same width as Augs
            { 4_02, 580 }, // Bank
            { 4_03, 600 }, // Fship (recruiting fellowships)
            { 4_04, 430 }, // Quests (Custom Quests)
            { 4_05, 450 }, // Top (leaderboards; every sub-tab is the same shape)

            // About / Settings / Help
            { 5_00, 350 }, // About
            { 5_01, 430 }, // Decal (plugin bar order)
            { 5_02, 350 }, // Settings
            { 5_03, 590 }, // Help
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
            { 3_02, 545 }, // Favorites — one row of chrome, so shorter than Flags
            { 3_03, 490 }, // Flaggings
            { 3_04, 485 }, // Hub
            { 3_05, 545 }, // Markers
            { 3_06, 545 }, // Titles (Available and Unavailable)

            // Server
            { 4_00, 561 }, // Augs (Conquest) — advanced augs over enlightenment augs
            { 4_01, 310 }, // Experience (character summary + seven XP bonus rows)
            { 4_02, 350 }, // Bank
            { 4_03, 545 }, // Fship (recruiting fellowships)
            { 4_04, 545 }, // Quests (Custom Quests)
            { 4_05, 555 }, // Top (leaderboards) — taller: a third row of tabs above the list

            // About / Settings / Help
            { 5_00, 270 }, // About
            { 5_01, 545 }, // Decal (plugin bar order) — one row of chrome, like Favorites
            { 5_02, 400 }, // Settings
            { 5_03, 580 }, // Help
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
                InitFavorites();
                InitJohn();
                InitMarkers();
                InitFlags();
                InitFacility();
                InitTitles();
                InitCustomQuests();
                InitConquestAugmentations();
                InitConquestExperience();
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
                InitDecal();

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
            if (!disposing) return;

            try
            {
                DisposeComponent(() =>
                {
                    view.Resize -= MainView_Resized;
                    MainViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    CharacterViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    StatusViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    QuestsViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    ServerViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    TopViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                    AboutViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                });

                DisposeComponent(DisposeItems);
                DisposeComponent(DisposeNearby);
                DisposeComponent(DisposeFellowship);
                DisposeComponent(DisposeQuests);
                DisposeComponent(DisposeFavorites);
                DisposeComponent(DisposeJohn);
                DisposeComponent(DisposeMarkers);
                DisposeComponent(DisposeFlags);
                DisposeComponent(DisposeFacility);
                DisposeComponent(DisposeTitles);
                DisposeComponent(DisposeCustomQuests);
                DisposeComponent(DisposeConquestAugmentations);
                DisposeComponent(DisposeConquestExperience);
                DisposeComponent(DisposeConquestBank);
                DisposeComponent(DisposeConquestFship);
                DisposeComponent(DisposeTop);
                DisposeComponent(DisposeAugmentations);
                DisposeComponent(DisposeCantrips);
                DisposeComponent(DisposeCredits);
                DisposeComponent(DisposeRecalls);
                DisposeComponent(DisposeLuminance);
                DisposeComponent(DisposeSociety);
                DisposeComponent(DisposeSettings);
                DisposeComponent(DisposeDecal);
                DisposeComponent(DisposeButtonFlashes);
            }
            finally
            {
                // Always release the HUD, even if a partially initialized control cannot
                // unsubscribe cleanly.
                view?.Dispose();
            }
        }

        private static void DisposeComponent(Action dispose)
        {
            try { dispose(); }
            catch (Exception ex) { Util.Log(ex); }
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

        // A short label flash acknowledges refresh commands whose chat replies are suppressed.
        private const string RefreshingLabel = "Refreshing...";

        // The deadline prevents a click immediately before Tick from producing an invisible flash.
        private static readonly TimeSpan ButtonFlashDuration = TimeSpan.FromMilliseconds(500);

        private HudButton FlashedButton;
        private string FlashedButtonLabel;
        private DateTime FlashedUntil;

        private void FlashButton(HudButton button)
        {
            if (button == null) { return; }

            RestoreFlashedButton();

            FlashedButtonLabel = button.Text;
            button.Text = RefreshingLabel;
            FlashedButton = button;
            FlashedUntil = DateTime.UtcNow + ButtonFlashDuration;
        }

        // Called from Update() once the deadline passes, and from FlashButton when a second button
        // takes the slot early.
        private void RestoreFlashedButton()
        {
            if (FlashedButton == null) { return; }

            FlashedButton.Text = FlashedButtonLabel;
            FlashedButton = null;
            FlashedButtonLabel = null;
        }

        // Just drop the reference — the control is going away with the view.
        private void DisposeButtonFlashes()
        {
            FlashedButton = null;
            FlashedButtonLabel = null;
        }

        // Read PortalImageID instead of caching controls, which could pin removed rows in memory.
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

        // Tint by explicit colour, null to reset. The bool overload below is the two-state form;
        // this one exists for rows that have more than one reason to be tinted.
        private void AssignTint(HudList.HudListRowAccessor row, Color? color, List<int> columns)
        {
            foreach (int column in columns)
            {
                if (color.HasValue) {
                    ((HudStaticText)row[column]).TextColor = color.Value;
                } else {
                    ((HudStaticText)row[column]).ResetTextColor();
                }
            }
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
            if (observedQuestRevision != QuestState.Revision) { UpdateQuestFlags(); }

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
            if (currentTab == 3_02) { UpdateFavorites(); }
            if (currentTab == 3_03) { UpdateFlags(); }
            if (currentTab == 3_04) { UpdateFacility(); }
            if (currentTab == 3_05) { UpdateMarkers(); }
            if (currentTab == 3_06) { UpdateTitles(); }

            // Server Tab
            if (currentTab == 4_00) { UpdateConquestAugmentations(); }
            if (currentTab == 4_01) { UpdateConquestExperience(); }
            if (currentTab == 4_02) { UpdateConquestBank(); }
            if (currentTab == 4_03) { UpdateConquestFship(); }
            if (currentTab == 4_04) { UpdateCustomQuests(); }
            if (currentTab == 4_05) { UpdateTop(); }

            // About / Settings / Help
            if (currentTab == 5_00) {; }
            if (currentTab == 5_01) { UpdateDecal(); }
            if (currentTab == 5_02) { UpdateSettings(); }
            if (currentTab == 5_03) { UpdateHelp(); }
        }

        // Selected target changed
        public void UpdateTarget()
        {
            int currentTab = CurrentTab();
            if (currentTab == 1_02) { UpdateNearbyList(); }
            if (currentTab == 1_03) { UpdateFellowshipButtons(); }
        }

        // Quest Flag Changes
        private int observedQuestRevision;

        public void UpdateQuestFlags()
        {
            // Update anything that relies on quest flags
            UpdateJohnList();
            UpdateAugmentationQuestsList();
            UpdateCreditsList();
            UpdateFlagsList();
            UpdateLuminanceList();
            UpdateMarkersList();

            if (QuestState.LastChangeWasFlag)
            {
                Util.Chat($"Quest data updated. Found {QuestFlag.QuestFlags.Count} flags.", Util.ColorPink);
            }

            observedQuestRevision = QuestState.Revision;
        }
    }
}
