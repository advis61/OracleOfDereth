using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using MyClasses.MetaViewWrappers;
using MyClasses.MetaViewWrappers.DecalControls;
using MyClasses.MetaViewWrappers.VirindiViewServiceHudControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirindiViewService;
using VirindiViewService.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        // Main View
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        readonly int IconComplete = 0x60011F9;   // Green Circle
        readonly int IconNotComplete = 0x60011F8;    // Red Circle

        public HudTabView MainViewNotebook { get; private set; }
        public HudTabView CharacterViewNotebook { get; private set; }

        // Status HUD
        public HudStaticText SummoningText { get; private set; }
        public HudStaticText LockpickText { get; private set; }
        public HudStaticText LifeText { get; private set; }
        public HudStaticText MeleeDText { get; private set; }
        public HudStaticText BuffsText { get; private set; }
        public HudStaticText BeersText { get; private set; }
        public HudStaticText HouseText { get; private set; }
        public HudStaticText PagesText { get; private set; }
        public HudStaticText RareText { get; private set; }
        public HudStaticText DestructionText { get; private set; }
        public HudStaticText RegenText { get; private set; }
        public HudStaticText ProtectionText { get; private set; }

        // Buffs
        public HudList BuffsList { get; private set; }

        // John
        public HudStaticText JohnLabel { get; private set; }
        public HudStaticText JohnText { get; private set; }

        public HudStaticText JohnListSortName { get; private set; }
        public HudStaticText JohnListSortReady { get; private set; }
        public HudStaticText JohnListSortSolves { get; private set; }

        public HudList JohnList { get; private set; }
        public HudButton JohnRefresh { get; private set; }

        // Character: Augmentations
        public HudList AugmentationsQuestsList { get; private set; }
        public HudList AugmentationsList { get; private set; }
        public HudButton AugmentationsRefresh { get; private set; }

        // Character: Luminance
        public HudStaticText LuminanceText { get; private set; }
        public HudList LuminanceList { get; private set; }

        // Character: Cantrips
        public HudList CantripsList { get; private set; }

        // Resize Tracking
        public bool wasResized = false;

        private Dictionary<int, int> MainViewWidths = new Dictionary<int, int>
        {
            { 0, 190 }, // Hud
            { 1, 460 }, // Buffs
            { 2, 350 }, // Character
            { 3, 430 }, // John
            { 4, 350 }, // About

            // Character Tab
            { 2_00, 650 }, // Augmentations
            { 2_01, 350 }, // Cantrips
            { 2_02, 350 }, // Credits
            { 2_03, 650 }, // Luminance
        };

        private Dictionary<int, int> MainViewHeights = new Dictionary<int, int>
        {
            { 0, 290 }, // Hud
            { 1, 310 }, // Buffs
            { 2, 490 }, // Character
            { 3, 490 }, // John
            { 4, 310 }, // About

            // Character Tab
            { 2_00, 550 }, // Augmentations
            { 2_01, 550 }, // Cantrips
            { 2_02, 550 }, // Credits
            { 2_03, 550 }, // Luminance
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

                // Make the view resizable
                view.UserResizeable = true;
                view.Resize += MainView_Resized;

                // Main Notebook
                MainViewNotebook = (HudTabView)view["MainViewNotebook"];
                MainViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // Character Notebook
                CharacterViewNotebook = (HudTabView)view["CharacterViewNotebook"];
                CharacterViewNotebook.OpenTabChange += Notebook_OpenTabChange;

                // HUD Tab
                BuffsText = (HudStaticText)view["BuffsText"];
                BeersText = (HudStaticText)view["BeersText"];
                HouseText = (HudStaticText)view["HouseText"];
                PagesText = (HudStaticText)view["PagesText"];

                SummoningText = (HudStaticText)view["SummoningText"];
                LockpickText = (HudStaticText)view["LockpickText"];
                LifeText = (HudStaticText)view["LifeText"];
                MeleeDText = (HudStaticText)view["MeleeDText"];

                RareText = (HudStaticText)view["RareText"];
                DestructionText = (HudStaticText)view["DestructionText"];

                RegenText = (HudStaticText)view["RegenText"];
                ProtectionText = (HudStaticText)view["ProtectionText"];

                BuffsText.FontHeight = 10;
                BeersText.FontHeight = 10;
                HouseText.FontHeight = 10;
                PagesText.FontHeight = 10;

                SummoningText.FontHeight = 10;
                LockpickText.FontHeight = 10;
                LifeText.FontHeight = 10;
                MeleeDText.FontHeight = 10;

                RareText.FontHeight = 10;
                DestructionText.FontHeight = 10;
                RegenText.FontHeight = 10;
                ProtectionText.FontHeight = 10;

                // Buffs Tab
                BuffsList = (HudList)view["BuffsList"];
                BuffsList.Click += BuffsList_Click;
                BuffsList.ClearRows();

                // John Tab
                JohnText = (HudStaticText)view["JohnText"];
                JohnText.FontHeight = 10;

                JohnRefresh = (HudButton)view["JohnRefresh"];
                JohnRefresh.Hit += JohnRefresh_Hit;

                JohnList = (HudList)view["JohnList"];
                JohnList.Click += JohnList_Click;
                JohnList.ClearRows();

                JohnListSortName = (HudStaticText)view["JohnListSortName"];
                JohnListSortName.Hit += JohnListSortName_Click;

                JohnListSortReady = (HudStaticText)view["JohnListSortReady"];
                JohnListSortReady.Hit += JohnListSortReady_Click;

                JohnListSortSolves = (HudStaticText)view["JohnListSortSolves"];
                JohnListSortSolves.Hit += JohnListSortSolves_Click;

                // Character: Augmentations
                AugmentationsRefresh = (HudButton)view["AugmentationsRefresh"];
                AugmentationsRefresh.Hit += AugmentationsRefresh_Hit;

                AugmentationsQuestsList = (HudList)view["AugmentationsQuestsList"];
                AugmentationsQuestsList.Click += AugmentationsQuestsList_Click;
                AugmentationsQuestsList.ClearRows();

                AugmentationsList = (HudList)view["AugmentationsList"];
                AugmentationsList.Click += AugmentationsList_Click;
                AugmentationsList.ClearRows();

                // Character :Cantrips
                CantripsList = (HudList)view["CantripsList"];
                CantripsList.Click += CantripsList_Click;
                CantripsList.ClearRows();

                // Character: Credits

                // Character: Luminance
                LuminanceText = (HudStaticText)view["LuminanceText"];
                LuminanceText.FontHeight = 10;

                LuminanceList = (HudList)view["LuminanceList"];
                LuminanceList.Click += LuminanceList_Click;
                LuminanceList.ClearRows();

                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private int CurrentTab()
        {
            int mainTab = MainViewNotebook.CurrentTab;
            int characterTab = CharacterViewNotebook.CurrentTab;

            if(mainTab == 2) { // Character Tab
                return (mainTab * 100) + characterTab;
            }

            return mainTab;
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

        // The Tick
        public void Update()
        {
            int currentTab = CurrentTab();

            if(QuestFlag.QuestsChanged) { UpdateQuestFlags(); }

            if (currentTab == 0) { UpdateHud(); }
            if (currentTab == 1) { UpdateBuffs(); }
            if (currentTab == 3) { UpdateJohn(); }

            // Character Tab
            if(currentTab == 200) { UpdateAugmentations(); }
            if(currentTab == 201) { UpdateCantrips(); }
            if(currentTab == 202) { UpdateCredits(); }
            if(currentTab == 203) { UpdateLuminance(); }
        }

        // Quest Flag Changes
        public void UpdateQuestFlags()
        {
            // Update anything that relies on quest flags
            UpdateJohnList(true);
            UpdateAugmentationQuestsList(true);

            Util.Chat("Quest data updated.", Util.ColorPink);

            // Quests are now unchanged
            QuestFlag.QuestsChanged = false;
        }

        // HUD Tab
        public void UpdateHud()
        {
            BuffsText.Text = Hud.BuffsText();
            HouseText.Text = Hud.HouseText();
            BeersText.Text = Hud.BeersText();
            PagesText.Text = Hud.PagesText();

            LockpickText.Text = Hud.LockpickText();
            SummoningText.Text = Hud.SummoningText();
            LifeText.Text = Hud.LifeText();
            MeleeDText.Text = Hud.MeleeDText();

            RareText.Text = Hud.RareText();

            DestructionText.Text = Hud.DestructionText();
            RegenText.Text = Hud.RegenText();
            ProtectionText.Text = Hud.ProtectionText();
        }

        // Buffs Tab
        private void UpdateBuffs()
        {
            UpdateBuffsList();
        }

        int BuffsListCount = 0;

        private void UpdateBuffsList()
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            // Get all buffs with a duration
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => x.Duration > 0 && x.TimeRemaining > 0)
                .Where(x => {
                    var spell = service.SpellTable.GetById(x.SpellId);
                    return spell != null;
                })
                .OrderBy(x => x.TimeRemaining)
                .ToList();

            // Go through all buffs and remove any rows that no longer exist
            for (int x = 0; x < enchantments.Count(); x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= BuffsListCount) {
                    BuffsListCount += 1;
                    row = BuffsList.AddRow();
                } else {
                    row = BuffsList[x];
                }

                EnchantmentWrapper enchantment = enchantments[x];
                Decal.Filters.Spell spell = service.SpellTable.GetById(enchantment.SpellId);

                double duration = enchantment.TimeRemaining;
                TimeSpan time = TimeSpan.FromSeconds(duration);

                ((HudPictureBox)row[0]).Image = spell.IconId;
                ((HudStaticText)row[1]).Text = enchantment.SpellId.ToString();
                ((HudStaticText)row[2]).Text = string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
                ((HudStaticText)row[3]).Text = spell.Name;
            }

            while (BuffsListCount > enchantments.Count())
            {
                BuffsListCount -= 1;
                BuffsList.RemoveRow(BuffsListCount);
            }
        }
        void BuffsList_Click(object sender, int row, int col) 
        {
        }

        // Cantrips Tab
        private void UpdateCantrips()
        {
            UpdateCantripsList();
        }

        int CantripsListCount = 0;
        private void UpdateCantripsList(bool force = false)
        {
            List<Cantrip> cantrips = Cantrip.Cantrips.Where(x => x.SkillIsKnown()).ToList();
            int count = cantrips.Count();

            // When empty
            if (CantripsListCount != count) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= CantripsListCount) {
                    row = CantripsList.AddRow();
                    CantripsListCount += 1;
                } else {
                    row = CantripsList[i];
                }

                var cantrip = cantrips[i];

                if(cantrip.Name == "Blank") { continue; }

                // Only update this if first time
                if (force)
                {
                    ((HudPictureBox)row[0]).Image = cantrip.Icon();
                    ((HudStaticText)row[1]).Text = cantrip.Name;
                }

                // Always update
                ((HudStaticText)row[2]).Text = cantrip.Level();
            }
        }

        void CantripsList_Click(object sender, int row, int col)
        {
        }

        // John Tab
        public void UpdateJohn()
        {
            if(QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateJohnList();
        }

        int JohnListCount = 0;
        private void UpdateJohnList(bool force = false)
        {
            // For each quest in JohnQuest.Quests, add a row to the JohnList
            // This function will be called multiple times, so we need to add or update

            int completed = 0;
            int count = JohnQuest.JohnQuests.Count;

            // When empty
            if (JohnListCount == 0) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= JohnListCount)
                {
                    row = JohnList.AddRow();
                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;

                    JohnListCount += 1;
                }
                else
                {
                    row = JohnList[i];
                }

                var johnQuest = JohnQuest.JohnQuests[i];

                bool complete = johnQuest.IsComplete();
                if (complete) { completed += 1; }

                // Only update this if the /myquests changes or sort order changes
                if (force)
                {
                    if (complete) {
                        ((HudPictureBox)row[0]).Image = IconComplete;
                    } else {
                        ((HudPictureBox)row[0]).Image = IconNotComplete;
                    }

                    ((HudStaticText)row[1]).Text = johnQuest.Name;
                    ((HudStaticText)row[4]).Text = johnQuest.Flag;
                }

                // Always update this
                QuestFlag questFlag;
                QuestFlag.QuestFlags.TryGetValue(johnQuest.Flag, out questFlag);

                if (questFlag == null)
                {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                }
                else
                {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }
            }

            // Update Top Text
            if (force)
            {
                JohnText.Text = $"Legendary John Quests: {completed} completed";
            }
        }
        void JohnList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)JohnList[row][4]).Text;

            JohnQuest johnQuest;
            johnQuest = JohnQuest.JohnQuests.FirstOrDefault(x => x.Flag == flag);

            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(flag, out questFlag);

            // Quest URL
            if (col == 0) {
                if (johnQuest == null || johnQuest.Url == "") {
                    Util.Chat($"Missing quest wiki url", Util.ColorPink);
                } else {
                    Util.Think($"{johnQuest.Name}: {johnQuest.Url}");

                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(johnQuest.Url);
                        Util.Chat("Quest URL copied to clipboard.", Util.ColorPink);
                    }
                    catch (Exception ex)
                    {
                        Util.Chat("Failed to copy URL to clipboard: " + ex.Message, Util.ColorPink);
                    }
                }
            }

            // Quest Hint
            if (col == 1) {
                if (johnQuest == null || johnQuest.Hint == "") {
                    Util.Chat($"Missing quest hint", Util.ColorPink);
                } else {
                    Util.Think($"{johnQuest.Name}: {johnQuest.Hint}");
                }
            }

            // Quest Flag
            if(col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Player has not completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }

        void JohnRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        void JohnListSortName_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.NameAscending) {
                JohnQuest.Sort(JohnQuest.SortType.NameDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.NameAscending);
            }

            UpdateJohnList(true);
        }

        void JohnListSortReady_Click(object sender, EventArgs e)
        {
            if( JohnQuest.CurrentSortType == JohnQuest.SortType.ReadyAscending) {
                JohnQuest.Sort(JohnQuest.SortType.ReadyDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.ReadyAscending);
            }

            UpdateJohnList(true);
        }

        void JohnListSortSolves_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.SolvesAscending) {
                JohnQuest.Sort(JohnQuest.SortType.SolvesDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.SolvesAscending);
            }

            UpdateJohnList(true);
        }

        // Character: Augmentations
        public void UpdateAugmentations() {
            if(QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateAugmentationQuestsList();
            UpdateAugmentationsList();
        }

        int AugmentationsListCount = 0;
        int AugmentationsCompleted = 0;
        private void UpdateAugmentationsList(bool force = false)
        {
            List<Augmentation> augmentations = Augmentation.XPAugmentations();
            int count = augmentations.Count();
            int completed = augmentations.Count(x => x.IsComplete());

            // When empty
            if (AugmentationsListCount != count) { force = true; }
            if (AugmentationsCompleted != completed) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= AugmentationsListCount)
                {
                    row = AugmentationsList.AddRow();
                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    AugmentationsListCount += 1;
                }
                else
                {
                    row = AugmentationsList[i];
                }

                var augmentation = augmentations[i];

                if (augmentation.Name == "Blank") { continue; }

                if (augmentation.Id == 0) {
                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    continue;
                }

                bool complete = augmentation.IsComplete();

                // Only update this if first time
                if (force)
                {
                    if (complete) {
                        ((HudPictureBox)row[0]).Image = IconComplete;
                    } else {
                        ((HudPictureBox)row[0]).Image = IconNotComplete;
                    }

                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    ((HudStaticText)row[3]).Text = augmentation.Effect;
                    ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
                }

                // Always update
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[4]).Text = augmentation.CostText();
            }

            AugmentationsCompleted = completed;
        }

        void AugmentationsList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)AugmentationsList[row][5]).Text;
            if(text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int id = int.Parse(text);

            Augmentation augmentation;
            augmentation = Augmentation.XPAugmentations().FirstOrDefault(x => x.Id == id);

            // Quest URL
            if (col == 0)
            {
                if (augmentation == null || augmentation.Url == "")
                {
                    // Nothing to do
                }
                else
                {
                    Util.Think($"{augmentation.Name}: {augmentation.Url}");

                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(augmentation.Url);
                        Util.Chat("URL copied to clipboard.", Util.ColorPink);
                    }
                    catch (Exception ex)
                    {
                        Util.Chat("Failed to copy URL to clipboard: " + ex.Message, Util.ColorPink);
                    }
                }
            }

            // Quest Hint
            if (col > 0)
            {
                if (augmentation == null || augmentation.Hint == "")
                {
                    // Nothing to do
                }
                else
                {
                    Util.Think($"{augmentation.Name}: {augmentation.Hint}");
                }
            }
        }

        // Character: Luminance
        public void UpdateLuminance()
        {
            UpdateLuminanceList();
            UpdateLuminanceText();
        }

        int LuminanceListCount = 0;
        int LuminanceCompleted = 0;
        private void UpdateLuminanceList(bool force = false)
        {
            List<Augmentation> augmentations = Augmentation.LuminanceAugmentations();
            int count = augmentations.Count();
            int completed = augmentations.Count(x => x.IsComplete());

            // When empty
            if (LuminanceListCount != count) { force = true; }
            if (LuminanceCompleted != completed) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= LuminanceListCount)
                {
                    row = LuminanceList.AddRow();
                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    LuminanceListCount += 1;
                }
                else
                {
                    row = LuminanceList[i];
                }

                var augmentation = augmentations[i];

                if (augmentation.Name == "Blank") { continue; }

                if (augmentation.Id == 0)
                {
                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    continue;
                }

                bool complete = augmentation.IsComplete();

                // Only update this if first time
                if (force)
                {
                    if (complete) {
                        ((HudPictureBox)row[0]).Image = IconComplete;
                    } else {
                        ((HudPictureBox)row[0]).Image = IconNotComplete;
                    }

                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    ((HudStaticText)row[3]).Text = augmentation.Effect;
                    ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
                }

                // Always update
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[4]).Text = augmentation.CostText();
            }

            LuminanceCompleted = completed;
        }

        private void UpdateLuminanceText()
        {
            LuminanceText.Text = $"Luminance: {Augmentation.TotalLuminanceSpent():N0} spent / {Augmentation.TotalLuminance():N0} ({Augmentation.TotalLuminancePercentage()}% complete, {Augmentation.TotalLuminanceRemaining():N0} to max)";
        }

        int AugmentationsQuestsListCount = 0;
        private void UpdateAugmentationQuestsList(bool force = false)
        {
            // For each quest in AugQuest.AugQuests, add a row to the AugQuestList
            // This function will be called multiple times, so we need to add or update

            int count = AugQuest.AugQuests.Count;

            // When empty
            if (AugmentationsQuestsListCount == 0) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= AugmentationsQuestsListCount)
                {
                    row = AugmentationsQuestsList.AddRow();
                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;

                    AugmentationsQuestsListCount += 1;
                }
                else
                {
                    row = AugmentationsQuestsList[i];
                }

                var augQuest = AugQuest.AugQuests[i];

                bool complete = augQuest.IsComplete();

                // Only update this if the /myquests changes or sort order changes
                if (force)
                {
                    if (complete) {
                        ((HudPictureBox)row[0]).Image = IconComplete;
                    } else {
                        ((HudPictureBox)row[0]).Image = IconNotComplete;
                    }

                    ((HudStaticText)row[1]).Text = augQuest.Name;
                    ((HudStaticText)row[4]).Text = augQuest.Flag;
                }

                // Always update this
                QuestFlag questFlag;
                QuestFlag.QuestFlags.TryGetValue(augQuest.Flag, out questFlag);

                if (questFlag == null)
                {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                }
                else
                {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }
            }
        }

        void AugmentationsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        void AugmentationsQuestsList_Click(object sender, int row, int col) {
            string flag = ((HudStaticText)AugmentationsQuestsList[row][4]).Text;

            AugQuest augQuest;
            augQuest = AugQuest.AugQuests.FirstOrDefault(x => x.Flag == flag);

            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(flag, out questFlag);

            // Quest URL
            if (col == 0)
            {
                if (augQuest == null || augQuest.Url == "")
                {
                    Util.Chat($"Missing quest wiki url", Util.ColorPink);
                }
                else
                {
                    Util.Think($"{augQuest.Name}: {augQuest.Url}");

                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(augQuest.Url);
                        Util.Chat("Quest URL copied to clipboard.", Util.ColorPink);
                    }
                    catch (Exception ex)
                    {
                        Util.Chat("Failed to copy URL to clipboard: " + ex.Message, Util.ColorPink);
                    }
                }
            }

            // Quest Hint
            if (col == 1)
            {
                if (augQuest == null || augQuest.Hint == "")
                {
                    Util.Chat($"Missing quest hint", Util.ColorPink);
                }
                else
                {
                    Util.Think($"{augQuest.Name}: {augQuest.Hint}");
                }
            }

            // Quest Flag
            if (col >= 2)
            {
                if (questFlag == null)
                {
                    Util.Chat($"{flag}: Player has not completed", Util.ColorPink);
                }
                else
                {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }

        void LuminanceList_Click(object sender, int row, int col) { 
        }

        public void UpdateCredits()
        {
            Util.Chat("Updating credits");
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
                MainViewNotebook.OpenTabChange -= Notebook_OpenTabChange;
                CharacterViewNotebook.OpenTabChange -= Notebook_OpenTabChange;

                BuffsList.Click -= BuffsList_Click;

                JohnRefresh.Hit -= JohnRefresh_Hit;
                JohnList.Click -= JohnList_Click;
                JohnListSortName.Hit -= JohnListSortName_Click;
                JohnListSortReady.Hit -= JohnListSortReady_Click;
                JohnListSortSolves.Hit -= JohnListSortSolves_Click;

                AugmentationsRefresh.Hit -= AugmentationsRefresh_Hit;
                AugmentationsQuestsList.Click -= AugmentationsQuestsList_Click;
                AugmentationsList.Click -= AugmentationsList_Click;

                CantripsList.Click -= CantripsList_Click;
                LuminanceList.Click -= LuminanceList_Click;

                view?.Dispose();
            }
        }
    }
}
