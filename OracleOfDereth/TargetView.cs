using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using MyClasses.MetaViewWrappers;
using MyClasses.MetaViewWrappers.DecalControls;
using MyClasses.MetaViewWrappers.VirindiViewServiceHudControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Text;
using System.Threading.Tasks;
using VirindiViewService;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    class TargetView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText TargetName { get; private set; }
        public HudList BuffsList { get; private set; }

        // Corrosion
        public HudFixedLayout CorrosionLayout { get; private set; }
        public HudPictureBox CorrosionIcon { get; private set; }
        public HudStaticText CorrosionText { get; private set; }

        // Corruption
        public HudFixedLayout CorruptionLayout { get; private set; }
        public HudPictureBox CorruptionIcon { get; private set; }
        public HudStaticText CorruptionText { get; private set; }
        
        // Curse
        public HudFixedLayout CurseLayout { get; private set; }
        public HudPictureBox CurseIcon { get; private set; }
        public HudStaticText CurseText { get; private set; }

        // Dest
        public HudFixedLayout DestLayout { get; private set; }
        public HudPictureBox DestIcon { get; private set; }
        public HudStaticText DestText { get; private set; }

        // Track last target
        private int LastTargetId = 0;

        public TargetView()
        {
            try
            {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("OracleOfDereth.targetView.xml", out properties, out controls);

                // Display the view
                view = new VirindiViewService.HudView(properties, controls);
                if (view == null) { return; }

                // Hide from Decal bar
                view.ShowInBar = false;

                // Corrosion
                CorrosionIcon = new HudPictureBox();
                CorrosionIcon.Image = 100691559; //  Corrosion icon
                CorrosionLayout = (HudFixedLayout)view["CorrosionIcon"];
                CorrosionLayout.AddControl(CorrosionIcon, new Rectangle(0, 3, 32, 32));

                CorrosionText = (HudStaticText)view["CorrosionText"];
                CorrosionText.FontHeight = 10;
                CorrosionText.TextAlignment = VirindiViewService.WriteTextFormats.Center;

                // Corruption
                CorruptionIcon = new HudPictureBox();
                CorruptionIcon.Image = 100691561; // Corruption icon
                CorruptionLayout = (HudFixedLayout)view["CorruptionIcon"];
                CorruptionLayout.AddControl(CorruptionIcon, new Rectangle(0, 1, 28, 28));

                CorruptionText = (HudStaticText)view["CorruptionText"];
                CorruptionText.FontHeight = 10;
                CorruptionText.TextAlignment = VirindiViewService.WriteTextFormats.Center;

                // Curse
                CurseIcon = new HudPictureBox();
                CurseIcon.Image = 100691551; // Curse icon
                CurseLayout = (HudFixedLayout)view["CurseIcon"];
                CurseLayout.AddControl(CurseIcon, new Rectangle(0, 1, 28, 28));

                CurseText = (HudStaticText)view["CurseText"];
                CurseText.FontHeight = 10;
                CurseText.TextAlignment = VirindiViewService.WriteTextFormats.Center;

                // Dest
                DestIcon = new HudPictureBox();
                DestIcon.Image = 100670995; // Dest icon
                DestLayout = (HudFixedLayout)view["DestIcon"];
                DestLayout.AddControl(DestIcon, new Rectangle(0, 1, 28, 28));

                DestText = (HudStaticText)view["DestText"];
                DestText.TextAlignment = VirindiViewService.WriteTextFormats.Center;
                DestText.TextColor = Target.DestructionColor;
                DestText.FontHeight = 11;

                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        public void Update()
        {
            Skill skill = new Skill(CharFilterSkillType.VoidMagic);
            if(skill.IsUnKnown()) { view.Visible = false; return; }

            UpdateVisibility();
            if(view.Visible == false) { return; }

            UpdateSpells();
        }

        public void UpdateVisibility()
        {
            view.Visible = Target.GetCurrent().IsTarget();
        }

        public void UpdateSpells()
        {
            Target target = Target.GetCurrent();

            // Texts
            CorrosionText.Text = target.CorrosionText();
            CorruptionText.Text = target.CorruptionText();
            CurseText.Text = target.CurseText();
            DestText.Text = target.DestructionText();

            // Colors
            List<Color> before = new List<Color> { CorrosionText.TextColor, CorruptionText.TextColor, CurseText.TextColor };

            CorrosionText.TextColor = target.CorrosionColor();
            CorruptionText.TextColor = target.CorruptionColor();
            CurseText.TextColor = target.CurseColor();

            if(CorrosionText.TextColor == Target.DestructionColor) { CorrosionText.FontHeight = 11; } else {  CorrosionText.FontHeight = 10; }
            if(CorruptionText.TextColor == Target.DestructionColor) { CorruptionText.FontHeight = 11; } else { CorruptionText.FontHeight = 10; }
            if(CurseText.TextColor == Target.DestructionColor) { CurseText.FontHeight = 11; } else { CurseText.FontHeight = 10; }

            List<Color> after = new List<Color> { CorrosionText.TextColor, CorruptionText.TextColor, CurseText.TextColor };

            // Nice
            if(target.Id == LastTargetId && DestText.Text != "" && before.Count(color => color == Target.DestructionColor) == 2 && after.Count(color => color == Target.DestructionColor) == 3)
            {
                CorrosionText.Text = "N";
                CorruptionText.Text = "I";
                CurseText.Text = "C";
                DestText.Text = "E";
            }

            LastTargetId = target.Id;
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
