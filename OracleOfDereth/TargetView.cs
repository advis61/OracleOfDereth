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
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            //TargetName.Text = Target.CurrentTarget?.Name();

            if (Target.CurrentTarget != null) {
                TargetName.Text = Target.CurrentTarget.ToString();
            } else {
                TargetName.Text = "No target";
            }
        }

        private int BuffsListCount = 0;
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

                if (x >= BuffsListCount)
                {
                    BuffsListCount += 1;
                    row = BuffsList.AddRow();
                }
                else
                {
                    row = BuffsList[x];
                }
                
                EnchantmentWrapper enchantment = enchantments[x];
                Spell spell = service.SpellTable.GetById(enchantment.SpellId);

                double duration = enchantment.TimeRemaining;
                TimeSpan time = TimeSpan.FromSeconds(duration);

                ((HudPictureBox)row[0]).Image = spell.IconId;
                ((HudStaticText)row[1]).Text = string.Format("{0:D2}", time.Seconds);
            }

            while (BuffsListCount > enchantments.Count())
            {
                BuffsListCount -= 1;
                BuffsList.RemoveRow(BuffsListCount);
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
