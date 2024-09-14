using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirindiViewService.Controls;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText SummoningLabel { get; private set; }
        public HudStaticText VersionLabel { get; private set; }
        public HudStaticText BuffsLabel { get; private set; }
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
                SummoningLabel = (HudStaticText)view["SummoningLabel"];
                VersionLabel = (HudStaticText)view["VersionLabel"];
                BuffsLabel = (HudStaticText)view["BuffsLabel"];
                BuffsList = (HudList)view["BuffsList"];

                Update();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        public void Update()
        {
            VersionLabel.Text = $"{DateTime.Now:HH:mm:ss}";

            BuffsLabel.Text = "Something\nOther\nThen";
            
            // Summoning
            Skill summoning = new Skill(CharFilterSkillType.Summoning);
            SummoningLabel.Text = "Current " + summoning.Current().ToString() + "Vitae " + summoning.Vitae().ToString() + "Vitae Minus " + summoning.VitaeMissing().ToString();

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
                    ((HudStaticText)row[2]).Text = enchantment.TimeRemaining.ToString();
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
            VersionLabel.Text = $"{DateTime.Now:HH:mm:ss}";

            VersionLabel.Text = $"{DateTime.Now:HH:mm:ss}";
            SummoningLabel.Text = "Oh man";

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

            SummoningLabel.Text = "Base " + summoning + ", Bonus " + bonus + ", Buffed " + buffed + ", Effective " + effective;

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
