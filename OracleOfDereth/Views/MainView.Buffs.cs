using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList BuffsList { get; private set; }

        private void InitBuffs()
        {
            BuffsList = (HudList)view["BuffsList"];
            BuffsList.ClearRows();
        }

        public void UpdateBuffs() {
            UpdateBuffsList();
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

            while (BuffsList.RowCount > enchantments.Count()) { BuffsList.RemoveRow(BuffsList.RowCount - 1); }
        }
    }
}
