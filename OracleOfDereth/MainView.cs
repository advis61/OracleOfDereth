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
        public HudButton LuminanceRefresh { get; private set; }

        // Character: Cantrips
        public HudList CantripsList { get; private set; }

        // Character: Credits
        public HudList CreditsList { get; private set; }
        public HudButton CreditsRefresh { get; private set; }

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
            { 1, 545 }, // Buffs
            { 2, 550 }, // Character
            { 3, 545 }, // John
            { 4, 290 }, // About

            // Character Tab
            { 2_00, 550 }, // Augmentations
            { 2_01, 550 }, // Cantrips
            { 2_02, 165 }, // Credits
            { 2_03, 550 }, // Luminance
        };

        // Assign Images Tracking
        private Dictionary<HudPictureBox,int> AssignedImages = new Dictionary<HudPictureBox, int>();

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
                BuffsList.ClearRows();

                // John Tab
                JohnText = (HudStaticText)view["JohnText"];
                JohnText.FontHeight = 10;

                JohnRefresh = (HudButton)view["JohnRefresh"];
                JohnRefresh.Hit += QuestFlagsRefresh_Hit;

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
                AugmentationsRefresh.Hit += QuestFlagsRefresh_Hit;

                AugmentationsQuestsList = (HudList)view["AugmentationsQuestsList"];
                AugmentationsQuestsList.Click += AugmentationsQuestsList_Click;
                AugmentationsQuestsList.ClearRows();

                AugmentationsList = (HudList)view["AugmentationsList"];
                AugmentationsList.Click += AugmentationsList_Click;
                AugmentationsList.ClearRows();

                // Character: Cantrips
                CantripsList = (HudList)view["CantripsList"];
                CantripsList.ClearRows();

                // Character: Credits
                CreditsRefresh = (HudButton)view["CreditsRefresh"];
                CreditsRefresh.Hit += QuestFlagsRefresh_Hit;

                CreditsList = (HudList)view["CreditsList"];
                CreditsList.Click += CreditsList_Click;
                CreditsList.ClearRows();

                // Character: Luminance
                LuminanceRefresh = (HudButton)view["LuminanceRefresh"];
                LuminanceRefresh.Hit += QuestFlagsRefresh_Hit;

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

            // Character Tab
            if(mainTab == 2) { return (mainTab * 100) + CharacterViewNotebook.CurrentTab; }

            // Main Tab
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

        private void QuestFlagsRefresh_Hit(object sender, EventArgs e)
        {
            QuestFlag.Refresh();
        }

        private void AssignImage(HudPictureBox row, int icon)
        {
            if (AssignedImages.TryGetValue(row, out int assignedIcon) && assignedIcon == icon) return;

            row.Image = icon;
            AssignedImages[row] = icon;
        }

        private void AssignImage(HudPictureBox row, bool completed)
        {
            if (completed) { AssignImage(row, IconComplete); } else { AssignImage(row, IconNotComplete); }
        }

        // The Tick
        public void Update()
        {
            if(QuestFlag.QuestsChanged) { UpdateQuestFlags(); }

            int currentTab = CurrentTab();

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
            UpdateJohnList();
            UpdateAugmentationQuestsList();
            UpdateCreditsList();
            UpdateLuminanceList();

            // Display feedback 
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
        public void UpdateBuffs() { 
            UpdateBuffsList(); 
        }

        // John Tab
        public void UpdateJohn()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateJohnList();
        }

        // Character: Augmentations
        public void UpdateAugmentations()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateAugmentationQuestsList();
            UpdateAugmentationsList();
        }

        // Character: Cantrips
        public void UpdateCantrips() { 
            UpdateCantripsList(); 
        }

        // Character: Credits
        public void UpdateCredits()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateCreditsList();
        }

        // Character: Luminance
        public void UpdateLuminance()
        {
            UpdateLuminanceList();
            UpdateLuminanceText();
        }

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
                if (x >= BuffsList.RowCount) { row = BuffsList.AddRow(); } else { row = BuffsList[x]; }

                // Update
                EnchantmentWrapper enchantment = enchantments[x];
                Decal.Filters.Spell spell = service.SpellTable.GetById(enchantment.SpellId);

                double duration = enchantment.TimeRemaining;
                TimeSpan time = TimeSpan.FromSeconds(duration);

                AssignImage((HudPictureBox)row[0], spell.IconId);
                ((HudStaticText)row[1]).Text = enchantment.SpellId.ToString();
                ((HudStaticText)row[2]).Text = string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
                ((HudStaticText)row[3]).Text = spell.Name;
            }

            while (BuffsList.RowCount > enchantments.Count()) { BuffsList.RemoveRow(BuffsList.RowCount-1); }
        }

        private void UpdateCantripsList()
        {
            List<Cantrip> cantrips = Cantrip.Cantrips.Where(x => x.SkillIsKnown()).ToList();

            for (int x = 0; x < cantrips.Count(); x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= CantripsList.RowCount) { row = CantripsList.AddRow(); } else { row = CantripsList[x]; }

                // Update
                Cantrip cantrip = cantrips[x];
                if(cantrip.Name == "Blank") { continue; }

                AssignImage((HudPictureBox)row[0], cantrip.Icon());
                ((HudStaticText)row[1]).Text = cantrip.Name;
                ((HudStaticText)row[2]).Text = cantrip.Level();
            }

            while (CantripsList.RowCount > cantrips.Count()) { CantripsList.RemoveRow(CantripsList.RowCount-1); }
        }

        private void UpdateJohnList()
        {
            List<JohnQuest> johnQuests = JohnQuest.JohnQuests.ToList();
            int completed = 0;

            for (int x = 0; x < johnQuests.Count; x++) 
            {
                HudList.HudListRowAccessor row;

                if (x >= JohnList.RowCount) {
                    row = JohnList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = JohnList[x];
                }

                // Update
                JohnQuest johnQuest = johnQuests[x];
                QuestFlag.QuestFlags.TryGetValue(johnQuest.Flag, out QuestFlag questFlag);

                bool complete = johnQuest.IsComplete();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                ((HudStaticText)row[1]).Text = johnQuest.Name;

                if (questFlag == null) {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                } else {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[4]).Text = johnQuest.Flag;
            }

            // Update Text
            JohnText.Text = $"Legendary John Quests: {completed} completed";
        }

        void JohnList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)JohnList[row][4]).Text;

            JohnQuest johnQuest = JohnQuest.JohnQuests.FirstOrDefault(x => x.Flag == flag);
            if(johnQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && johnQuest.Url.Length > 0) {
                Util.Think($"{johnQuest.Name}: {johnQuest.Url}");
                Util.ClipboardCopy(johnQuest.Url);
            }

            // Quest Hint
            if (col == 1 && johnQuest.Hint.Length > 0) {
                Util.Think($"{johnQuest.Name}: {johnQuest.Hint}");
            }

            // Quest Flag
            if(col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }

        void JohnListSortName_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.NameAscending) {
                JohnQuest.Sort(JohnQuest.SortType.NameDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.NameAscending);
            }

            UpdateJohnList();
        }

        void JohnListSortReady_Click(object sender, EventArgs e)
        {
            if(JohnQuest.CurrentSortType == JohnQuest.SortType.ReadyAscending) {
                JohnQuest.Sort(JohnQuest.SortType.ReadyDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.ReadyAscending);
            }

            UpdateJohnList();
        }

        void JohnListSortSolves_Click(object sender, EventArgs e)
        {
            if (JohnQuest.CurrentSortType == JohnQuest.SortType.SolvesAscending) {
                JohnQuest.Sort(JohnQuest.SortType.SolvesDescending);
            } else {
                JohnQuest.Sort(JohnQuest.SortType.SolvesAscending);
            }

            UpdateJohnList();
        }

        private void UpdateAugmentationsList()
        {
            List<Augmentation> augmentations = Augmentation.XPAugmentations().ToList();

            // Add or update rows
            for (int x = 0; x < augmentations.Count(); x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= AugmentationsList.RowCount) {
                    row = AugmentationsList.AddRow();

                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                } else {
                    row = AugmentationsList[x];
                }

                // Update
                Augmentation augmentation = augmentations[x];
                if (augmentation.Name == "Blank") { continue; }
                if (augmentation.Id == 0) { ((HudStaticText)row[2]).Text = augmentation.Name; continue; }

                AssignImage((HudPictureBox)row[0], augmentation.IsComplete());
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[2]).Text = augmentation.Name;
                ((HudStaticText)row[3]).Text = augmentation.Effect;
                ((HudStaticText)row[4]).Text = augmentation.CostText();
                ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
            }
        }

        void AugmentationsList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)AugmentationsList[row][5]).Text;
            if(text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int id = int.Parse(text);

            Augmentation augmentation = Augmentation.XPAugmentations().FirstOrDefault(x => x.Id == id);
            if(augmentation == null) { return; }

            // Quest URL
            if (col == 0 && augmentation.Url.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Url}");
                Util.ClipboardCopy(augmentation.Url);
            }

            // Quest Hint
            if (col > 0 && augmentation.Hint.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Hint}");
            }
        }

        private void UpdateLuminanceList()
        {
            List<Augmentation> augmentations = Augmentation.LuminanceAugmentations();

            for (int x = 0; x < augmentations.Count(); x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= LuminanceList.RowCount) {
                    row = LuminanceList.AddRow();

                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                } else {
                    row = LuminanceList[x];
                }

                // Update
                Augmentation augmentation = augmentations[x];
                if (augmentation.Name == "Blank") { continue; }
                if (augmentation.Id == 0) { ((HudStaticText)row[2]).Text = augmentation.Name; continue; }

                AssignImage((HudPictureBox)row[0], augmentation.IsComplete());
                ((HudStaticText)row[1]).Text = augmentation.Text();
                ((HudStaticText)row[2]).Text = augmentation.Name;
                ((HudStaticText)row[3]).Text = augmentation.Effect;
                //((HudStaticText)row[3]).Text = $"{augmentation.LuminanceSpent():N0}";
                ((HudStaticText)row[4]).Text = augmentation.CostText();
                ((HudStaticText)row[5]).Text = augmentation.Id.ToString();
            }
        }

        void LuminanceList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)LuminanceList[row][5]).Text;
            if (text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int id = int.Parse(text);

            Augmentation augmentation = Augmentation.LuminanceAugmentations().FirstOrDefault(x => x.Id == id);
            if (augmentation == null) { return; }

            // Quest URL
            if (col == 0 && augmentation.Url.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Url}");
                Util.ClipboardCopy(augmentation.Url);
            }

            // Quest Hint
            if (col > 0 && augmentation.Hint.Length > 0) {
                Util.Think($"{augmentation.Name}: {augmentation.Hint}");
            }
        }

        private void UpdateLuminanceText()
        {
            LuminanceText.Text = $"{Augmentation.TotalLuminanceSpent():N0} spent / {Augmentation.TotalLuminance():N0} ({Augmentation.TotalLuminancePercentage()}% complete, {Augmentation.TotalLuminanceRemaining():N0} to max)";
        }

        private void UpdateAugmentationQuestsList()
        {
            List<AugQuest> augQuests = AugQuest.AugQuests.ToList();

            for (int x = 0; x < augQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= AugmentationsQuestsList.RowCount) {
                    row = AugmentationsQuestsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = AugmentationsQuestsList[x];
                }

                // Update
                AugQuest augQuest = augQuests[x];
                QuestFlag.QuestFlags.TryGetValue(augQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], augQuest.IsComplete());
                ((HudStaticText)row[1]).Text = augQuest.Name;

                if (questFlag == null) {
                    ((HudStaticText)row[2]).Text = "ready";
                    ((HudStaticText)row[3]).Text = "";
                } else {
                    ((HudStaticText)row[2]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[3]).Text = $"{questFlag.Solves}";
                }

                ((HudStaticText)row[4]).Text = augQuest.Flag;
            }
        }

        private void AugmentationsQuestsList_Click(object sender, int row, int col) {
            string flag = ((HudStaticText)AugmentationsQuestsList[row][4]).Text;

            AugQuest augQuest = AugQuest.AugQuests.FirstOrDefault(x => x.Flag == flag);
            if(augQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && augQuest.Url.Length > 0) {
                Util.Think($"{augQuest.Name}: {augQuest.Url}");
                Util.ClipboardCopy(augQuest.Url);
            }

            // Quest Hint
            if (col == 1 && augQuest.Hint.Length > 0) {
                Util.Think($"{augQuest.Name}: {augQuest.Hint}");
            }

            // Quest Flag
            if (col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }

        // Credits
        private void UpdateCreditsList()
        {
            List<CreditQuest> creditQuests = CreditQuest.CreditQuests.ToList();

            for (int x = 0; x < creditQuests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= CreditsList.RowCount) {
                    row = CreditsList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = CreditsList[x];
                }

                // Update
                CreditQuest creditQuest = creditQuests[x];
                QuestFlag.QuestFlags.TryGetValue(creditQuest.Flag, out QuestFlag questFlag);

                AssignImage((HudPictureBox)row[0], creditQuest.IsComplete());
                ((HudStaticText)row[1]).Text = creditQuest.Name;

                if(creditQuest.IsComplete()) {
                    ((HudStaticText)row[2]).Text = "completed";
                } else {
                    ((HudStaticText)row[2]).Text = "ready";
                }

                ((HudStaticText)row[3]).Text = creditQuest.Flag;
            }
        }

        private void CreditsList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)CreditsList[row][3]).Text;

            CreditQuest creditQuest = CreditQuest.CreditQuests.FirstOrDefault(x => x.Flag == flag);
            if (creditQuest == null) { return; }

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL
            if (col == 0 && creditQuest.Url.Length > 0) {
                Util.Think($"{creditQuest.Name}: {creditQuest.Url}");
                Util.ClipboardCopy(creditQuest.Url);
            }

            // Quest Hint
            if (col == 1 && creditQuest.Hint.Length > 0) {
                Util.Think($"{creditQuest.Name}: {creditQuest.Hint}");
            }

            // Quest Flag
            if (col >= 2) {
                if (questFlag == null) {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
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

                JohnList.Click -= JohnList_Click;
                JohnListSortName.Hit -= JohnListSortName_Click;
                JohnListSortReady.Hit -= JohnListSortReady_Click;
                JohnListSortSolves.Hit -= JohnListSortSolves_Click;

                AugmentationsQuestsList.Click -= AugmentationsQuestsList_Click;
                AugmentationsList.Click -= AugmentationsList_Click;
                LuminanceList.Click -= LuminanceList_Click;
                CreditsList.Click -= CreditsList_Click;

                // Quest Flag Refresh Buttons
                JohnRefresh.Hit -= QuestFlagsRefresh_Hit;
                AugmentationsRefresh.Hit -= QuestFlagsRefresh_Hit;
                LuminanceRefresh.Hit -= QuestFlagsRefresh_Hit;
                CreditsRefresh.Hit -= QuestFlagsRefresh_Hit;

                // Other cleanup
                AssignedImages.Clear();
                view?.Dispose();
            }
        }
    }
}
