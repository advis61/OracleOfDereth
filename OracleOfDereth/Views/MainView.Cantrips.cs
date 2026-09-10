using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList CantripsList { get; private set; }
        private readonly List<int> CantripsListColumns = new List<int> { 1, 2, 3 };

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
            CantripsList.Click += CantripsList_Click;

            CantripsRatings = (HudStaticText)view["CantripsRatings"];
            CantripsRatings.FontHeight = 10;   // matches the header label on every other tab

            CantripsDisplayAll = (HudCheckBox)view["CantripsDisplayAll"];
            CantripsDisplayAll.Change += CantripsDisplayAll_Change;
        }

        private void DisposeCantrips()
        {
            if (CantripsList != null) CantripsList.Click -= CantripsList_Click;
            CantripsDisplayAll.Change -= CantripsDisplayAll_Change;
        }

        private void CantripsList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= CantripsList.RowCount) return;
            if (!int.TryParse(((HudStaticText)CantripsList[row][4]).Text, out int id) || id == 0) return;
            var item = CoreManager.Current.WorldFilter[id];
            if (item != null && item.Values((LongValueKey)10, 0) > 0)
                CoreManager.Current.Actions.SelectItem(id);
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
            var activeSpells = new HashSet<int>(CoreManager.Current.CharacterFilter.Enchantments.Select(x => x.SpellId));

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
                    SetText(row, 1, "");
                    SetText(row, 2, "");
                    SetText(row, 3, "");
                    SetText(row, 4, "");
                    continue;
                }

                AssignImage((HudPictureBox)row[0], cantrip.Icon());
                SetText(row, 1, cantrip.Name);
                int tier = cantrip.ActiveTier(activeSpells);
                SetText(row, 2, cantrip.Level(tier));
                var source = cantrip.EquippedSource(tier);
                SetText(row, 3, source.Name);
                SetText(row, 4, source.Id == 0 ? "" : source.Id.ToString());
            }

            while (CantripsList.RowCount > cantrips.Count()) { CantripsList.RemoveRow(CantripsList.RowCount-1); }
            UpdateCantripsSelection();
        }

        private void UpdateCantripsSelection()
        {
            int targetId = Target.GetCurrent().Id;
            for (int x = 0; x < CantripsList.RowCount; x++)
            {
                var row = CantripsList[x];
                bool selected = int.TryParse(((HudStaticText)row[4]).Text, out int id) && id != 0 && id == targetId;
                AssignSelected(row, selected, CantripsListColumns);
            }
        }
    }
}
