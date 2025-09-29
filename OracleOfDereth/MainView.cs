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
        public HudTabView MainViewNotebook { get; private set; }

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

        // Augs
        public HudList AugQuestsList { get; private set; }
        public HudList AugXPList { get; private set; }
        public HudList AugLuminanceList { get; private set; }
        public HudButton AugQuestsRefresh { get; private set; }
        public HudStaticText AugLuminanceText { get; private set; }

        // Buffs
        public HudList BuffsList { get; private set; }

        // Cantrips
        public HudList CantripsList { get; private set; }

        // John Tracker
        public HudStaticText JohnLabel { get; private set; }
        public HudStaticText JohnText { get; private set; }

        public HudStaticText JohnListSortName { get; private set; }
        public HudStaticText JohnListSortReady { get; private set; }
        public HudStaticText JohnListSortSolves { get; private set; }

        public HudList JohnList { get; private set; }
        public HudButton JohnRefresh { get; private set; }

        public bool wasResized = false; // Ever resized

        private Dictionary<int, int> MainViewWidths = new Dictionary<int, int>
        {
            { 0, 190 }, // Hud
            { 1, 650 }, // Augs
            { 2, 460 }, // Buffs
            { 3, 350 }, // Cantrips
            { 4, 430 }, // John
            { 5, 350 }  // About
        };

        private Dictionary<int, int> MainViewHeights = new Dictionary<int, int>
        {
            { 0, 290 }, // Hud
            { 1, 550 }, // Augs
            { 2, 310 }, // Buffs
            { 3, 490 }, // Cantrips
            { 4, 490 }, // John (810 for full list or 340)
            { 5, 310 }  // About
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

                // The Notebook
                MainViewNotebook = (HudTabView)view["MainViewNotebook"];
                MainViewNotebook.OpenTabChange += MainViewNotebook_OpenTabChange;

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

                // Cantrips Tab
                CantripsList = (HudList)view["CantripsList"];
                CantripsList.Click += CantripsList_Click;
                CantripsList.ClearRows();

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

                // Augs Tab
                AugLuminanceText = (HudStaticText)view["AugLuminanceText"];
                AugLuminanceText.FontHeight = 10;

                AugQuestsRefresh = (HudButton)view["AugQuestsRefresh"];
                AugQuestsRefresh.Hit += AugQuestsRefresh_Hit;

                AugQuestsList = (HudList)view["AugQuestsList"];
                AugQuestsList.Click += AugQuestsList_Click;
                AugQuestsList.ClearRows();

                AugXPList = (HudList)view["AugXPList"];
                AugXPList.Click += AugXPList_Click;
                AugXPList.ClearRows();

                AugLuminanceList = (HudList)view["AugLuminanceList"];
                AugLuminanceList.Click += AugLuminanceList_Click;
                AugLuminanceList.ClearRows();


                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void MainViewNotebook_OpenTabChange(object sender, EventArgs e)
        {
            int currentTab = MainViewNotebook.CurrentTab;

            view.Height = MainViewHeights[currentTab];
            view.Width = MainViewWidths[currentTab];

            Update();
        }

        private void MainView_Resized(object sender, EventArgs e)
        {
            int currentTab = MainViewNotebook.CurrentTab;

            // Save the new view height
            MainViewHeights[currentTab] = view.Height;

            // Prevent width updates
            view.Width = MainViewWidths[currentTab];
        }

        // The Tick

        public void Update()
        {
            int currentTab = MainViewNotebook.CurrentTab;

            if(QuestFlag.QuestsChanged) { UpdateQuestFlags(); }
            if (currentTab == 0) { UpdateHud(); }
            if (currentTab == 1) { UpdateAugs(); }
            if (currentTab == 2) { UpdateBuffs(); }
            if (currentTab == 3) { UpdateCantrips(); }
            if (currentTab == 4) { UpdateJohn(); }
        }

        // Quest Flag Changes
        public void UpdateQuestFlags()
        {
            // Update anything that relies on quest flags
            UpdateJohnList(true);

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
            // For each cantrip in Cantrip.Cantrips, add a row to the CantripsList
            // This function will be called multiple times, so we need to add or update

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
                    if (complete)
                    {
                        ((HudPictureBox)row[0]).Image = JohnQuest.IconComplete;
                    }
                    else
                    {
                        ((HudPictureBox)row[0]).Image = JohnQuest.IconNotComplete;
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

        // Augs List
        public void UpdateAugs() {
            if(QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateAugQuestsList();
            UpdateAugXPList();
            UpdateAugLuminanceList();
            UpdateAugLuminanceText();
        }

        int AugXPListCount = 0;
        int AugXPListCompleted = 0;
        private void UpdateAugXPList(bool force = false)
        {
            List<Augmentation> augmentations = Augmentation.XPAugmentations();
            int count = augmentations.Count();
            int completed = augmentations.Count(x => x.IsComplete());

            // When empty
            if (AugXPListCount != count) { force = true; }
            if (AugXPListCompleted != completed) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= AugXPListCount)
                {
                    row = AugXPList.AddRow();
                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    AugXPListCount += 1;
                }
                else
                {
                    row = AugXPList[i];
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
                    if (complete)
                    {
                        ((HudPictureBox)row[0]).Image = Augmentation.IconComplete;
                    }
                    else
                    {
                        ((HudPictureBox)row[0]).Image = Augmentation.IconNotComplete;
                    }

                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    ((HudStaticText)row[3]).Text = augmentation.Effect;
                    ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
                }

                // Always update
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[4]).Text = augmentation.CostText();
            }

            AugXPListCompleted = completed;
        }

        void AugXPList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)AugXPList[row][5]).Text;
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


        int AugLuminanceListCount = 0;
        int AugLuminanceListCompleted = 0;
        private void UpdateAugLuminanceList(bool force = false)
        {
            List<Augmentation> augmentations = Augmentation.LuminanceAugmentations();
            int count = augmentations.Count();
            int completed = augmentations.Count(x => x.IsComplete());

            // When empty
            if (AugLuminanceListCount != count) { force = true; }
            if (AugLuminanceListCompleted != completed) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= AugLuminanceListCount)
                {
                    row = AugLuminanceList.AddRow();
                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    AugLuminanceListCount += 1;
                }
                else
                {
                    row = AugLuminanceList[i];
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
                    if (complete)
                    {
                        ((HudPictureBox)row[0]).Image = Augmentation.IconComplete;
                    }
                    else
                    {
                        ((HudPictureBox)row[0]).Image = Augmentation.IconNotComplete;
                    }

                    ((HudStaticText)row[2]).Text = augmentation.Name;
                    ((HudStaticText)row[3]).Text = augmentation.Effect;
                    ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
                }

                // Always update
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[4]).Text = augmentation.CostText();
            }

            AugLuminanceListCompleted = completed;
        }

        private void UpdateAugLuminanceText()
        {
            AugLuminanceText.Text = $"Luminance: {Augmentation.TotalLuminanceSpent():N0} spent / {Augmentation.TotalLuminance():N0} ({Augmentation.TotalLuminancePercentage()}% complete, {Augmentation.TotalLuminanceRemaining():N0} to max)";
        }

        int AugQuestsListCount = 0;
        private void UpdateAugQuestsList(bool force = false)
        {
            // For each quest in AugQuest.AugQuests, add a row to the AugQuestList
            // This function will be called multiple times, so we need to add or update

            int count = AugQuest.AugQuests.Count;

            // When empty
            if (AugQuestsListCount == 0) { force = true; }

            // Add or update rows
            for (int i = 0; i < count; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= AugQuestsListCount)
                {
                    row = AugQuestsList.AddRow();
                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;

                    AugQuestsListCount += 1;
                }
                else
                {
                    row = AugQuestsList[i];
                }

                var augQuest = AugQuest.AugQuests[i];

                bool complete = augQuest.IsComplete();

                // Only update this if the /myquests changes or sort order changes
                if (force)
                {
                    if (complete)
                    {
                        ((HudPictureBox)row[0]).Image = AugQuest.IconComplete;
                    }
                    else
                    {
                        ((HudPictureBox)row[0]).Image = AugQuest.IconNotComplete;
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

        void AugQuestsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        void AugQuestsList_Click(object sender, int row, int col) {
            string flag = ((HudStaticText)AugQuestsList[row][4]).Text;

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

        void AugLuminanceList_Click(object sender, int row, int col) { 
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
                MainViewNotebook.OpenTabChange -= MainViewNotebook_OpenTabChange;

                BuffsList.Click -= BuffsList_Click;

                CantripsList.Click -= CantripsList_Click;

                JohnRefresh.Hit -= JohnRefresh_Hit;
                JohnList.Click -= JohnList_Click;
                JohnListSortName.Hit -= JohnListSortName_Click;
                JohnListSortReady.Hit -= JohnListSortReady_Click;
                JohnListSortSolves.Hit -= JohnListSortSolves_Click;

                AugQuestsRefresh.Hit -= AugQuestsRefresh_Hit;
                AugQuestsList.Click -= AugQuestsList_Click;
                AugXPList.Click -= AugXPList_Click;
                AugLuminanceList.Click -= AugLuminanceList_Click;

                view?.Dispose();
            }
        }
    }
}
