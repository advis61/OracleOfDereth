using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList CantripsList { get; private set; }

        // Unticked by default, so the list opens showing only cantrips for skills this character
        // actually has — the useful view for nearly everyone. Ticking it drops the filter and shows
        // every cantrip in cantrips.csv, untrained skills included, for planning a respec or just
        // looking up what exists.
        public HudCheckBox CantripsDisplayAll { get; private set; }

        // Header line: the character's own combat ratings, which is the number you actually want
        // when you're looking at a list of cantrips.
        public HudStaticText CantripsRatings { get; private set; }

        private void InitCantrips()
        {
            CantripsList = (HudList)view["CantripsList"];
            CantripsList.ClearRows();

            CantripsRatings = (HudStaticText)view["CantripsRatings"];
            CantripsRatings.FontHeight = 10;   // matches the header label on every other tab

            CantripsDisplayAll = (HudCheckBox)view["CantripsDisplayAll"];
            CantripsDisplayAll.Change += CantripsDisplayAll_Change;
        }

        private void DisposeCantrips()
        {
            CantripsDisplayAll.Change -= CantripsDisplayAll_Change;
        }

        // Redraw straight away rather than waiting for the next tick, so the box feels responsive.
        private void CantripsDisplayAll_Change(object sender, System.EventArgs e)
        {
            UpdateCantrips();
        }

        public void UpdateCantrips() {
            UpdateCantripsRatings();
            UpdateCantripsList();
        }

        // "-" rather than an empty label before login, so the row doesn't collapse to nothing.
        private void UpdateCantripsRatings()
        {
            string summary = CharacterRating.Summary();
            CantripsRatings.Text = summary.Length > 0 ? summary : "-";
        }

        private void UpdateCantripsList()
        {
            // One inventory walk for the whole redraw; every Level() below reads it out of a
            // dictionary. Refreshed every tick like the rest of the tab, so swapping a piece of
            // gear updates the source counts without any invalidation step.
            Cantrip.RefreshGearSources();

            // SkillIsKnown() is already true for the non-skill rows (set bonuses, essences and the
            // "Blank" spacers all carry SkillId <= 0), so those show either way and only the real
            // skill-backed cantrips are affected by the box.
            List<Cantrip> cantrips = CantripsDisplayAll.Checked
                ? Cantrip.Cantrips.ToList()
                : Cantrip.Cantrips.Where(x => x.SkillIsKnown()).ToList();

            // Regroup the skill section by training every refresh rather than caching it. Both the
            // filter above and the ranks below read the character filter live, so specialising or
            // untraining a skill in game reshuffles the list on the next tick with no invalidation
            // step to forget.
            cantrips = Cantrip.SortSkillSection(cantrips);

            for (int x = 0; x < cantrips.Count(); x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= CantripsList.RowCount) { row = CantripsList.AddRow(); } else { row = CantripsList[x]; }

                // Update
                Cantrip cantrip = cantrips[x];

                // Blank entries are spacer rows. Clear them explicitly rather than
                // skipping, otherwise a row that previously held a real cantrip keeps
                // its stale content when the list shifts (e.g. after an in-game respec).
                if (cantrip.Name == "Blank") {
                    AssignImage((HudPictureBox)row[0], 0);
                    ((HudStaticText)row[1]).Text = "";
                    ((HudStaticText)row[2]).Text = "";
                    continue;
                }

                AssignImage((HudPictureBox)row[0], cantrip.Icon());
                ((HudStaticText)row[1]).Text = cantrip.Name;
                ((HudStaticText)row[2]).Text = cantrip.Level();
            }

            while (CantripsList.RowCount > cantrips.Count()) { CantripsList.RemoveRow(CantripsList.RowCount-1); }
        }
    }
}
