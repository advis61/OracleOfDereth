using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList CantripsList { get; private set; }

        private void InitCantrips()
        {
            CantripsList = (HudList)view["CantripsList"];
            CantripsList.ClearRows();
        }

        public void UpdateCantrips() {
            UpdateCantripsList();
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
    }
}
