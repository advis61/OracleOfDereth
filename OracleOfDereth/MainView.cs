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
using VirindiViewService;
using VirindiViewService.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;
        public HudTabView MainViewNotebook { get; private set; }
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
        public HudList BuffsList { get; private set; }

        // John Tracker
        public HudStaticText JohnLabel { get; private set; }
        public HudStaticText JohnText { get; private set; }

        public HudList JohnList { get; private set; }
        public HudButton JohnRefresh { get; private set; }

        private static readonly List<int> RareSpellIds = new List<int> {
            3679,  // Prodigal Acid Bane
            3680,  // Prodigal Acid Protection
            3681,  // Prodigal Alchemy Mastery
            3682,  // Prodigal Arcane Enlightenment
            3683,  // Prodigal Armor Expertise
            3684,  // Prodigal Armor
            3685,  // Prodigal Light Weapon Mastery
            3686,  // Prodigal Blade Bane
            3687,  // Prodigal Blade Protection
            3688,  // Prodigal Blood Drinker
            3689,  // Prodigal Bludgeon Bane
            3690,  // Prodigal Bludgeon Protection
            3691,  // Prodigal Missile Weapon Mastery
            3692,  // Prodigal Cold Protection
            3693,  // Prodigal Cooking Mastery
            3694,  // Prodigal Coordination
            3695,  // Prodigal Creature Enchantment Mastery
            3696,  // Prodigal Missile Weapon Mastery
            3697,  // Prodigal Finesse Weapon Mastery
            3698,  // Prodigal Deception Mastery
            3699,  // Prodigal Defender
            3700,  // Prodigal Endurance
            3701,  // Prodigal Fealty
            3702,  // Prodigal Fire Protection
            3703,  // Prodigal Flame Bane
            3704,  // Prodigal Fletching Mastery
            3705,  // Prodigal Focus
            3706,  // Prodigal Frost Bane
            3707,  // Prodigal Healing Mastery
            3708,  // Prodigal Heart Seeker
            3709,  // Prodigal Hermetic Link
            3710,  // Prodigal Impenetrability
            3711,  // Prodigal Impregnability
            3712,  // Prodigal Invulnerability
            3713,  // Prodigal Item Enchantment Mastery
            3714,  // Prodigal Item Expertise
            3715,  // Prodigal Jumping Mastery
            3716,  // Prodigal Leadership Mastery
            3717,  // Prodigal Life Magic Mastery
            3718,  // Prodigal Lightning Bane
            3719,  // Prodigal Lightning Protection
            3720,  // Prodigal Lockpick Mastery
            3721,  // Prodigal Light Weapon Mastery
            3722,  // Prodigal Magic Item Expertise
            3723,  // Prodigal Magic Resistance
            3724,  // Prodigal Mana Conversion Mastery
            3725,  // Prodigal Mana Renewal
            3726,  // Prodigal Monster Attunement
            3727,  // Prodigal Person Attunement
            3728,  // Prodigal Piercing Bane
            3729,  // Prodigal Piercing Protection
            3730,  // Prodigal Quickness
            3731,  // Prodigal Regeneration
            3732,  // Prodigal Rejuvenation
            3733,  // Prodigal Willpower
            3734,  // Prodigal Light Weapon Mastery
            3735,  // Prodigal Spirit Drinker
            3736,  // Prodigal Sprint
            3737,  // Prodigal Light Weapon Mastery
            3738,  // Prodigal Strength
            3739,  // Prodigal Swift Killer
            3740,  // Prodigal Heavy Weapon Mastery
            3741,  // Prodigal Missile Weapon Mastery
            3742,  // Prodigal Light Weapon Mastery
            3743,  // Prodigal War Magic Mastery
            3744,  // Prodigal Weapon Expertise
            5025,  // Prodigal Item Expertise
            5026,  // Prodigal Two Handed Combat Mastery
            5436,  // Prodigal Void Magic Mastery
            5903,  // Prodigal Dual Wield Mastery
            5905,  // Prodigal Recklessness Mastery
            5907,  // Prodigal Shield Mastery
            5909,  // Prodigal Sneak Attack Mastery
            5911,  // Prodigal Dirty Fighting Mastery
            4131, // Spectral Light Weapon Mastery
            4132, // Spectral Blood Drinker
            4133, // Spectral Missile Weapon Mastery
            4134, // Spectral Missile Weapon Mastery
            4135, // Spectral Finesse Weapon Mastery
            4136, // Spectral Light Weapon Mastery
            4137, // Spectral Light Weapon Mastery
            4138, // Spectral Light Weapon Mastery
            4139, // Spectral Heavy Weapon Mastery
            4140, // Spectral Missile Weapon Mastery
            4141, // Spectral Light Weapon Mastery
            4142, // Spectral War Magic Mastery
            4208, // Spectral Flame
            4221, // Spectral Life Magic Mastery
            5023, // Spectral Two Handed Combat Mastery
            5024, // Spectral Item Expertise
            5168, // a spectacular view of the Mhoire lands
            5169, // a descent into the Mhoire catacombs
            5170, // a descent into the Mhoire catacombs
            5171, // Spectral Fountain Sip (Feeling good)
            5172, // Spectral Fountain Sip (Blood poisoned)
            5173, // Spectral Fountain Sip (Wounds poisoned)
            5435, // Spectral Void Magic Mastery
            5904, // Spectral Dual Wield Mastery
            5906, // Spectral Recklessness Mastery
            5908, // Spectral Shield Mastery
            5910, // Spectral Sneak Attack Mastery
            5912, // Spectral Dirty Fighting Mastery
        };

        private static readonly List<int> HouseSpellIds = new List<int>
        {
            3896, // Dark Equilibrium
            3894, // Dark Persistence
            3897, // Dark Purpose
            3895, // Dark Reflexes
            6146, // Ride the Lightning
            4099, // Strength of Diemos
            4025, // Iron Cast Stomach
            3243, // Consencration
            3244, // Divine Manipulation
            3245, // Sacrosanct Touch
            3237, // Fanaticism
            3831, // Blessing of the Pitcher Plant
            3830, // Blessing of the Fly Trap
            2995, // Power of the Dragon
            2993, // Grace of the Unicorn
            2997, // Splendor of the Firebird
            3829, // Blessing of the Sundew
            3977, // Coordination Other Incantation
            3978, // Focus Other Incantation
            3979, // Strength Other Incantation
        };

        private static readonly List<int> BeerSpellIds = new List<int> {
            3531,
            3533,
            3862,
            3864,
            3530,
            3863
        };

        private static readonly List<int> PagesSpellIds = new List<int> {
            3869, // Incantation of the Black Book (Pages of Salt and Ash)
         };

        private static readonly List<int> DestructionSpellIds = new List<int> {
            5204, // Surge of Destruction
         };

        private static readonly List<int> RegenSpellIds = new List<int> {
            5208, // Surge of Regen
         };

        private static readonly List<int> ProtectionSpellIds = new List<int> {
            5206, // Surge of Protection
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

                // John Tab
                JohnText = (HudStaticText)view["JohnText"];
                JohnText.FontHeight = 10;

                JohnRefresh = (HudButton)view["JohnRefresh"];
                JohnRefresh.Hit += JohnRefresh_Hit;

                JohnList = (HudList)view["JohnList"];
                JohnList.Click += JohnList_Click;
                JohnList.ClearRows();

                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void MainViewNotebook_OpenTabChange(object sender, EventArgs e)
        {
            int currentTab = MainViewNotebook.CurrentTab;

            if (currentTab == 0) { // HUD
                view.Width = 190;
                view.Height = 310;
            } else if (currentTab == 1) { // Buffs
                view.Width = 460;
                view.Height = 310;
            } else if (currentTab == 2) {  // John
                view.Width = 425;
                view.Height = 430; // 790 for all
            } else if (currentTab == 3) {  // About
                view.Width = 190;
                view.Height = 310;
            } else {
                Util.Log("Invalid tab");
                view.Width = 190;
                view.Height = 310;
            }

            Update();
        }

        public void Update()
        {
            int currentTab = MainViewNotebook.CurrentTab;

            if (currentTab == 0) { UpdateHud(); }
            if (currentTab == 1) { UpdateBuffs(); }
            if (currentTab == 2) { UpdateJohn(); }
        }

        // HUD Tab
        public void UpdateHud()
        {
            UpdateBuffTimes();
            UpdateHouse();
            UpdateBeers();
            UpdatePages();

            UpdateLockpick();
            UpdateSummoning();
            UpdateLife();
            UpdateMeleeD();

            UpdateRare();
            UpdateDestruction();

            UpdateRegen();
            UpdateProtection();
        }

        private void UpdateSummoning()
        {
            Skill skill = new Skill(CharFilterSkillType.Summoning);
            if (skill.IsKnown()) { SummoningText.Text = skill.Current().ToString(); }
        }
        private void UpdateLockpick()
        {
            Skill skill = new Skill(CharFilterSkillType.Lockpick);

            if (skill.IsKnown()) {
                int value = skill.Current();
                int vr1 = 0;
                int vr2 = 0;

                if (value >= 575)
                {
                    vr1 = 3;
                    vr2 = 10;
                } else if (value >= 570) {
                    vr1 = 4;
                    vr2 = 11;
                } else if (value >= 565) {
                    vr1 = 5;
                    vr2 = 12;
                } else if (value >= 550) {
                    vr1 = 6;
                    vr2 = 13;
                } else if (value >= 525) {
                    vr1 = 6;
                    vr2 = 14;
                } else if (value >= 500) {
                    vr1 = 7;
                    vr2 = 15;
                } else {
                    vr1 = 10;
                    vr2 = 20;
                }

                if (value >= 500) {
                    LockpickText.Text = skill.Current().ToString() + " (VR " + vr1 + "/" + vr2 + ")";
                } else {
                    LockpickText.Text = skill.Current().ToString();
                }
            }
        }

        private void UpdateLife()
        {
            Skill skill = new Skill(CharFilterSkillType.LifeMagic);
            if (skill.IsKnown()) { LifeText.Text = skill.Current().ToString(); }
        }
        private void UpdateMeleeD()
        {
            Skill skill = new Skill(CharFilterSkillType.MeleeDefense);
            if (skill.IsKnown()) { MeleeDText.Text = skill.Current().ToString(); }
        }

        private void UpdateBuffTimes()
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => x.Duration > 900)
                .Where(x => x.TimeRemaining > 0)
                .Where(x => {
                    var spell = service.SpellTable.GetById(x.SpellId);
                    return spell != null && !spell.IsDebuff && spell.IsUntargetted;
                })
                .ToList();

            if (enchantments.Count == 0)
            {
                BuffsText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            BuffsText.Text = string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
        }

        private void UpdateHouse()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => HouseSpellIds.Contains(x.SpellId))
                .Where(x => x.TimeRemaining > 0)
                .ToList();

            if (enchantments.Count == 0)
            {
                HouseText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            HouseText.Text = string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
        }

        private void UpdateBeers()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => BeerSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0) {
                BeersText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            BeersText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        private void UpdatePages()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => PagesSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0)
            {
                PagesText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            PagesText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        private void UpdateRare()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => RareSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0)
            {
                RareText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            double cooldown = 180 - (900 - enchantments.Max(x => x.TimeRemaining));

            if (cooldown > 0)
            {
                RareText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ") " + cooldown.ToString() + "s";
            } else {
                RareText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
            }
        }

        private void UpdateDestruction()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => DestructionSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0)
            {
                DestructionText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            DestructionText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        private void UpdateRegen()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => RegenSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0)
            {
                RegenText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            RegenText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
        }

        private void UpdateProtection()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => ProtectionSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0)
            {
                ProtectionText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);
            TimeSpan time = TimeSpan.FromSeconds(duration);

            ProtectionText.Text = string.Format("{0:D1}:{1:D2}", time.Minutes, time.Seconds);
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
                .Where(x => x.Duration > 0)
                .Where(x => x.TimeRemaining > 0)
                .Where(x =>
                {
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
                Spell spell = service.SpellTable.GetById(enchantment.SpellId);

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
            //Debug.Log("buffs list clicked");
        }

        // John Tab
        public void UpdateJohn()
        {
            UpdateJohnList();
        }

        int JohnListCount = 0;

        private void UpdateJohnList()
        {
            // For each quest in JohnQuest.Quests, add a row to the JohnList
            // This function will be called multiple times, so we need to add or update

            int questCount = JohnQuest.Quests.Count;
            int questCompletedCount = 0;

            // Add or update rows
            for (int i = 0; i < questCount; i++)
            {
                HudList.HudListRowAccessor row;
                if (i >= JohnListCount)
                {
                    JohnListCount += 1;
                    row = JohnList.AddRow();
                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                }
                else
                {
                    row = JohnList[i];
                }

                var quest = JohnQuest.Quests[i];

                bool complete = quest.IsComplete();
                if (complete) { questCompletedCount += 1; }

                // Only update this if the /myquests changes
                if (QuestFlag.QuestsChanged)
                {
                    if (complete)
                    {
                        ((HudPictureBox)row[0]).Image = JohnQuest.IconComplete;
                    }
                    else
                    {
                        ((HudPictureBox)row[0]).Image = JohnQuest.IconNotComplete;
                    }

                    ((HudStaticText)row[1]).Text = quest.Name;
                    ((HudStaticText)row[4]).Text = quest.Flag;
                }

                // Always update this
                QuestFlag questFlag;
                QuestFlag.QuestFlags.TryGetValue(quest.Flag, out questFlag);

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
            JohnText.Text = $"Legendary John Quests: {questCompletedCount} completed";

            if (QuestFlag.QuestsChanged) { 
                Util.Chat("Quests updated", Util.ColorPink); 
                QuestFlag.QuestsChanged = false;
            }
        }
        void JohnList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)JohnList[row][4]).Text;

            JohnQuest johnQuest;
            johnQuest = JohnQuest.Quests.FirstOrDefault(x => x.Flag == flag);

            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(flag, out questFlag);

            // Quest URL
            if (col == 0) {
                if (johnQuest == null || johnQuest.Url == "") {
                    Util.Chat($"Missing quest wiki url", Util.ColorPink);
                } else {
                    Util.Think($"{johnQuest.Name}: {johnQuest.Url}");
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
                    Util.Chat($"Missing quest flag", Util.ColorPink);
                } else {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }

        void JohnRefresh_Hit(object sender, EventArgs e)
        {
            Util.Command("/myquests");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) view?.Dispose();
        }
    }
}
