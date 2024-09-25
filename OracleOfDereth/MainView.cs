using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirindiViewService.Controls;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using VirindiViewService;
using System.Security.Authentication.ExtendedProtection.Configuration;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText SummoningText { get; private set; }
        public HudStaticText LockpickText { get; private set; }
        public HudStaticText LifeText { get; private set; }
        public HudStaticText BuffsText { get; private set; }
        public HudStaticText BeersText { get; private set; }
        public HudList BuffsList { get; private set; }

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

                // Assign the views objects to our local variables
                SummoningText = (HudStaticText)view["SummoningText"];
                LockpickText = (HudStaticText)view["LockpickText"];
                LifeText = (HudStaticText)view["LifeText"];
                BuffsText = (HudStaticText)view["BuffsText"];
                BeersText = (HudStaticText)view["BeersText"];
                BuffsList = (HudList)view["BuffsList"];

                Update();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        public void Update()
        {
            UpdateSummoning();
            UpdateLockpick();
            UpdateLife();
            UpdateBuffs();
            UpdateBeers();
            UpdateBuffsList();
        }

        private void UpdateSummoning()
        {
            Skill skill = new Skill(CharFilterSkillType.Summoning);
            SummoningText.Text = skill.Current().ToString();
        }
        private void UpdateLockpick()
        {
            Skill skill = new Skill(CharFilterSkillType.Lockpick);
            LockpickText.Text = skill.Current().ToString();
        }

        private void UpdateLife()
        {
            Skill skill = new Skill(CharFilterSkillType.LifeMagic);
            LifeText.Text = skill.Current().ToString();
        }

        private void UpdateBuffs()
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments
                .Where(x => x.Duration > 30)
                .Where(x => !BeerSpellIds.Contains(x.SpellId))
                .Where(x => !ExcludeSpellIds.Contains(x.SpellId))
                .Where(x => !service.SpellTable.GetById(x.SpellId).IsDebuff)
                .ToList();
           
            if (enchantments.Count == 0)
            {
                BuffsText.Text = "MISSING";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);

            // Convert to TimeSpan
            TimeSpan time = TimeSpan.FromSeconds(duration);

            // Format as H:M:S
            BuffsText.Text = string.Format("{0:D1}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds) + " (" + enchantments.Count().ToString() + ")";
        }

        private static readonly List<int> BeerSpellIds = new List<int> { 3531, 3533, 3862, 3864, 3530, 3863 };

        private static readonly List<int> ExcludeSpellIds = new List<int> {
            4024, // Asheron's Lesser Benediction
            3811  // Blackmoor's Favour
         };

        private void UpdateBeers()
        {
            List<EnchantmentWrapper> enchantments = CoreManager.Current.CharacterFilter.Enchantments.Where(x => BeerSpellIds.Contains(x.SpellId)).ToList();

            if (enchantments.Count == 0) { 
                BeersText.Text = "-";
                return;
            }

            double duration = enchantments.Min(x => x.TimeRemaining);

            // Convert to TimeSpan
            TimeSpan time = TimeSpan.FromSeconds(duration);

            // Format as H:M:S
            BeersText.Text = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        }

        private void UpdateBuffsList()
        {

            // List view
            FileService service = CoreManager.Current.Filter<FileService>();

            BuffsList.ClearRows();

            foreach (EnchantmentWrapper enchantment in CoreManager.Current.CharacterFilter.Enchantments)
            {
                if (enchantment.Duration > 0)
                {
                    HudList.HudListRowAccessor row = BuffsList.AddRow();

                    Spell spell = service.SpellTable.GetById(enchantment.SpellId);

                    ((HudPictureBox)row[0]).Image = spell.IconId;
                    ((HudStaticText)row[1]).Text = enchantment.SpellId.ToString();
                    ((HudStaticText)row[2]).Text = spell.Name;
                    ((HudStaticText)row[3]).Text = enchantment.TimeRemaining.ToString();
                }
            }
        }

        public void Update3()
        {
            LockpickText.Text = $"{DateTime.Now:HH:mm:ss}";

            LifeText.Text = "Something\nOther\nThen";
            
            // Summoning
            Skill summoning = new Skill(CharFilterSkillType.Summoning);
            SummoningText.Text = "Current " + summoning.Current().ToString() + "Vitae " + summoning.Vitae().ToString() + "Vitae Minus " + summoning.VitaeMissing().ToString();

            //foreach (WorldObject worldObject in Globals.Core.WorldFilter.GetByContainer(Globals.Core.CharacterFilter.Id))

            // List view
            FileService service = CoreManager.Current.Filter<FileService>();

            BuffsList.ClearRows();

            foreach (EnchantmentWrapper enchantment in CoreManager.Current.CharacterFilter.Enchantments)
            {
                if (enchantment.Duration > 0) {
                    HudList.HudListRowAccessor row = BuffsList.AddRow();

                    Spell buff = service.SpellTable.GetById(enchantment.SpellId);

                    ((HudPictureBox)row[0]).Image = buff.IconId;
                    ((HudStaticText)row[1]).Text = buff.Name;
                    ((HudStaticText)row[2]).Text = buff.Description;
                    ((HudStaticText)row[3]).Text = enchantment.TimeRemaining.ToString();
                }
            }

            //HudList.HudListRowAccessor newRow = mainView.ManaList.AddRow();

            //((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
            //((HudStaticText)newRow[1]).Text = wo.Name;
            //((HudStaticText)newRow[5]).Text = obj.Id.ToString(CultureInfo.InvariantCulture);

            //{
            //    if (obj.ItemState == EquipmentTrackedItemState.Active)
            //        ((HudPictureBox)mainView.ManaList[row - 1][2]).Image = IconActive;
            //    else if (obj.ItemState == EquipmentTrackedItemState.NotActive)
            //        ((HudPictureBox)mainView.ManaList[row - 1][2]).Image = IconNotActive;
            //    else if (obj.ItemState == EquipmentTrackedItemState.Unknown)
            //        ((HudPictureBox)mainView.ManaList[row - 1][2]).Image = IconUnknown;
            //    else
            //        ((HudPictureBox)mainView.ManaList[row - 1][2]).Image = IconNone;

            //    if (obj.ItemState != EquipmentTrackedItemState.Active && obj.ItemState != EquipmentTrackedItemState.NotActive)
            //    {
            //        ((HudStaticText)mainView.ManaList[row - 1][3]).Text = "-";
            //        ((HudStaticText)mainView.ManaList[row - 1][4]).Text = "-";
            //        ((HudStaticText)mainView.ManaList[row - 1][6]).Text = int.MaxValue.ToString(CultureInfo.InvariantCulture);
            //    }
            //    else
            //    {
            //        ((HudStaticText)mainView.ManaList[row - 1][3]).Text = obj.CalculatedCurrentMana + " / " + obj.MaximumMana;
            //        ((HudStaticText)mainView.ManaList[row - 1][4]).Text = string.Format("{0:d}h{1:d2}m", (int)obj.ManaTimeRemaining.TotalHours, obj.ManaTimeRemaining.Minutes);
            //        ((HudStaticText)mainView.ManaList[row - 1][6]).Text = obj.ManaTimeRemaining.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            //    }


        }

        private void Update2()
        {
            LockpickText.Text = $"{DateTime.Now:HH:mm:ss}";

            LockpickText.Text = $"{DateTime.Now:HH:mm:ss}";
            SummoningText.Text = "Oh man";

            // Summoning
            // https://gitlab.com/utilitybelt/utilitybelt.scripting/-/blob/master/Interop/Skill.cs?ref_type=heads#L106
            //string stam = CoreManager.Current.CharacterFilter.Vitals[CharFilterVitalType.Stamina].Base.ToString();

            //string summoning = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();
            //string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Buffed.ToString();
            //string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Current.ToString();
            //string summoning = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();

            // "413"
            string summoning = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Base.ToString();

            // "Specialized"
            string training = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Training.ToString();

            // "10" - Not sure where this 10 is coming from
            string bonus = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Bonus.ToString();

            // "546"
            string buffed = CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.Summoning].Buffed.ToString();

            // "546"
            string effective = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.Summoning].ToString();

            //CoreManager.Current.CharacterFilter.Augmentations.

            // Do I need to:
            // Consider Jack of All Trades?
            // Consider Luminance spec seer?
            // Consider Luminance worlds?
            // Consider Enlightens?

            SummoningText.Text = "Base " + summoning + ", Bonus " + bonus + ", Buffed " + buffed + ", Effective " + effective;

            // This is how you'd do it in UtilityBelt LUA script I think:
            //game.Character.Weenie.Skills[SkillId.Summoning].Current
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
