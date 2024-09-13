using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirindiViewService.Controls;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText SummoningLabel { get; private set; }
        public HudStaticText VersionLabel { get; private set; }

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

                Update();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        public void Update()
        {
            VersionLabel.Text = $"{DateTime.Now:HH:mm:ss}";
            
            // Summoning
            Skill summoning = new Skill(CharFilterSkillType.Summoning);
            SummoningLabel.Text = "Current " + summoning.Current().ToString() + "Vitae " + summoning.Vitae().ToString() + "Vitae Minus " + summoning.VitaeMissing().ToString();
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
