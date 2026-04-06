using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
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
        public HudStaticText CisText { get; private set; }

        private void InitStatusHud()
        {
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
            CisText = (HudStaticText)view["CisText"];

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
            CisText.FontHeight = 10;
        }

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
            CisText.Text = Hud.CisText();
        }
    }
}
