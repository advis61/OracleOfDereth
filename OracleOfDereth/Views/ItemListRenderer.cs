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
        public bool Weapons = false;
        public bool Armor = false;
        public bool Clothing = false;
        public bool Jewelry = false;
        public bool Cloaks = false;
        public bool Summons = false;
        public bool Aetheria = false;
        public bool Salvage = false;
        public bool Other = false;

        // True when the filter actually narrows the list (some category ticked or text typed).
        public bool IsActive => AnyCategorySelected() || !string.IsNullOrWhiteSpace(Text);

        public bool Matches(Item t)
        {
            if (!IsCategoryVisible(t.SortCategory)) return false;
            return MatchesText(t);
        }

        // Category checkboxes act as a whitelist: with none ticked there's no category
        // filtering at all (everything shows); tick one or more to show only those.
        private bool AnyCategorySelected()
        {
            return Weapons || Armor || Clothing || Jewelry || Cloaks || Summons || Aetheria || Salvage || Other;
        }

        private bool IsCategoryVisible(int sortCategory)
        {
            if (!AnyCategorySelected()) return true;

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

        // Highlight for the currently-picked row.
        private static readonly Color ColorSelected = Color.FromArgb(255, 130, 210, 255);

        // Paint the given items into the HudList: status/loading icon, item icon, name and
        // the four summary columns, with the id stashed in the (hidden) last column. The
        // caller passes its own image-tracking dict (so repeated repaints skip identical
        // images), the "not complete" icon for column 0, and the id of the selected row.
        public static void Render(HudList list, List<Item> items, Dictionary<HudPictureBox, int> assigned, int iconNotComplete, int selectedId)
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

                SetRowColor(row, selected: item.Id == selectedId && selectedId != 0, loading: !item.IsIdentified);
            }

            // Trim surplus rows, dropping their image boxes from the tracking dict — otherwise
            // those (now destroyed) boxes leak as dead keys every time the list shrinks.
            while (list.RowCount > items.Count)
            {
                HudList.HudListRowAccessor row = list[list.RowCount - 1];
                assigned.Remove((HudPictureBox)row[0]);
                assigned.Remove((HudPictureBox)row[1]);
                list.RemoveRow(list.RowCount - 1);
            }
        }

        // Tint the row's text columns (Name..Details): highlighted when selected, dim grey
        // while still loading its appraisal, otherwise the default colour.
        private static void SetRowColor(HudList.HudListRowAccessor row, bool selected, bool loading)
        {
            for (int col = 2; col <= 6; col++)
            {
                HudStaticText cell = (HudStaticText)row[col];
                if (selected) cell.TextColor = ColorSelected;
                else if (loading) cell.TextColor = ColorLoading;
                else cell.ResetTextColor();
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
