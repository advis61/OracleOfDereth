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
using MyClasses.MetaViewWrappers;
using MyClasses.MetaViewWrappers.DecalControls;
using MyClasses.MetaViewWrappers.VirindiViewServiceHudControls;

namespace OracleOfDereth
{
    class TargetView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText TargetName { get; private set; }
        public HudList BuffsList { get; private set; }
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

                TargetName = (HudStaticText)view["TargetName"];

                // BuffsList
                BuffsList = (HudList)view["BuffsList"];
                BuffsList.ClearRows();

                Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        void BuffsList_Click(object sender, int row, int col)
        {
            //Debug.Log("buffs list clicked");
        }
        public void Update()
        {
            UpdateTargetName();
            UpdateBuffsList();
        }

        private void UpdateTargetName()
        {
            if (Target.GetCurrentTarget() != null) {
                TargetName.Text = Target.GetCurrentTarget().ToString();
            } else {
                TargetName.Text = "";
            }
        }

        private int BuffsListCount = 0;
        private void UpdateBuffsList(bool force = false)
        {
            FileService service = CoreManager.Current.Filter<FileService>();

            // When empty
            if (BuffsListCount == 0) { force = true; }

            //for (int x = 0; x < SpellId.VoidSpellIds.Count(); x++)
            for (int x = 0; x < 1; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= BuffsListCount) {
                    BuffsListCount += 1;
                    row = BuffsList.AddRow();
                } else { 
                    row = BuffsList[x];
                }

                int spellId = SpellId.VoidSpellIds[x];
                Spell spell = service.SpellTable.GetById(spellId);

                if (force)
                {
                    ((HudPictureBox)row[0]).Image = spell.IconId;
                    ((HudStaticText)row[1]).Text = "-";
                }

                if(Target.GetCurrentTarget() == null) { continue; }

                // Always
                Target.GetCurrentTarget().ActiveSpells.TryGetValue(spellId, out DateTime spellTime);

                if (spellTime == null || spellTime == DateTime.MinValue)
                {
                    ((HudStaticText)row[1]).Text = "-";
                }
                else
                {
                    int seconds = (int)(DateTime.Now - spellTime).TotalSeconds;
                    ((HudStaticText)row[1]).Text = seconds.ToString();
                }
            }
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
