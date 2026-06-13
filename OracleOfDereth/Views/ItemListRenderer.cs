using System;
using System.Collections.Generic;
using System.Drawing;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    // View-agnostic filtering + row rendering for an ItemList, shared by the Items tab
    // (MainView.Items) and the standalone TradeView. Keeps both windows on one code path.

    // The current filter state for a list: which categories are visible plus the
    // free-text search box. Both views populate this from their checkboxes each refresh.
    public class ItemFilter
    {
        public string Text = "";
        public bool Weapons = true;
        public bool Armor = true;
        public bool Clothing = true;
        public bool Jewelry = true;
        public bool Cloaks = true;
        public bool Summons = true;
        public bool Aetheria = true;
        public bool Salvage = true;
        public bool Other = true;

        public bool Matches(Item t)
        {
            if (!IsCategoryVisible(t.SortCategory)) return false;
            return MatchesText(t);
        }

        private bool IsCategoryVisible(int sortCategory)
        {
            switch (sortCategory)
            {
                case 0: return Weapons;
                case 1: return Armor;
                case 2: return Jewelry;
                case 3: return Cloaks;
                case 4: return Summons;
                case 5: return Aetheria;
                case 6: return Salvage;
                case 7: return Clothing;
                default: return Other;
            }
        }

        private bool MatchesText(Item t)
        {
            string trimmed = (Text ?? "").Trim();
            string[] terms = trimmed.Length > 0
                ? trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];

            if (terms.Length == 0) return true;
            string combined = $"{t.Name} {t.SummaryCol1} {t.SummaryCol2} {t.SummaryCol3} {t.SummaryCol4}";
            foreach (string term in terms)
            {
                int requiredCount = 1;
                string word = term;
                int starIndex = term.LastIndexOf('*');

                if (starIndex > 0 && int.TryParse(term.Substring(starIndex + 1), out int n))
                {
                    word = term.Substring(0, starIndex);
                    requiredCount = n;
                }
                else if (term.Contains(".*"))
                {
                    string[] parts = term.Split(new[] { ".*" }, StringSplitOptions.RemoveEmptyEntries);
                    requiredCount = parts.Length;
                    word = parts[0];
                    bool allSame = true;
                    foreach (string p in parts) { if (!p.Equals(parts[0], StringComparison.OrdinalIgnoreCase)) { allSame = false; break; } }
                    if (!allSame)
                    {
                        bool allFound = true;
                        foreach (string p in parts) { if (combined.IndexOf(p, StringComparison.OrdinalIgnoreCase) < 0) { allFound = false; break; } }
                        if (!allFound) return false;
                        continue;
                    }
                }
                if (CountOccurrences(combined, word) < requiredCount) return false;
            }
            return true;
        }

        private static int CountOccurrences(string text, string term)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += term.Length;
            }
            return count;
        }
    }

    public static class ItemListRenderer
    {
        // Dim grey for rows still waiting on their appraisal details.
        private static readonly Color ColorLoading = Color.FromArgb(255, 150, 150, 150);

        // Paint the given items into the HudList: status/loading icon, item icon, name and
        // the four summary columns, with the id stashed in the (hidden) last column. The
        // caller passes its own image-tracking dict (so repeated repaints skip identical
        // images) and the "not complete" icon to show in column 0.
        public static void Render(HudList list, List<Item> items, Dictionary<HudPictureBox, int> assigned, int iconNotComplete)
        {
            for (int x = 0; x < items.Count; x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= list.RowCount) { row = list.AddRow(); } else { row = list[x]; }

                Item item = items[x];

                AssignImage(assigned, (HudPictureBox)row[0], iconNotComplete);
                AssignImage(assigned, (HudPictureBox)row[1], item.Icon);
                ((HudStaticText)row[2]).Text = item.Name;
                ((HudStaticText)row[3]).Text = item.SummaryCol1;
                ((HudStaticText)row[4]).Text = item.SummaryCol2;
                ((HudStaticText)row[5]).Text = item.SummaryCol3;
                ((HudStaticText)row[6]).Text = item.SummaryCol4;
                ((HudStaticText)row[7]).Text = item.Id.ToString();

                // Dim the row while it's still waiting on its appraisal.
                SetRowLoading(row, !item.IsIdentified);
            }

            while (list.RowCount > items.Count) { list.RemoveRow(list.RowCount - 1); }
        }

        // Tint the row's text columns (Name..Details) grey while loading, or reset
        // to the default colour once the item's details are filled in.
        private static void SetRowLoading(HudList.HudListRowAccessor row, bool loading)
        {
            for (int col = 2; col <= 6; col++)
            {
                if (loading) ((HudStaticText)row[col]).TextColor = ColorLoading;
                else ((HudStaticText)row[col]).ResetTextColor();
            }
        }

        // Only swap the image when it actually changes; assigning is comparatively expensive.
        private static void AssignImage(Dictionary<HudPictureBox, int> assigned, HudPictureBox box, int icon)
        {
            if (assigned.TryGetValue(box, out int assignedIcon) && assignedIcon == icon) return;

            if (icon == 0)
            {
                box.Image = null;
                assigned.Remove(box);
            }
            else
            {
                box.Image = icon;
                assigned[box] = icon;
            }
        }

        // One consistent format regardless of filters: total, plus optional
        // "(X shown)" when a filter hides some and "(N identifying)" while ids load.
        public static string StatusText(string label, int total, int shownCount, int identifying)
        {
            var notes = new List<string>();
            if (shownCount != total) notes.Add($"{shownCount} shown");
            if (identifying > 0) notes.Add($"{identifying} identifying");

            string status = $"{label}: {total}";
            if (notes.Count > 0) status += " (" + string.Join(", ", notes) + ")";
            return status;
        }
    }
}
