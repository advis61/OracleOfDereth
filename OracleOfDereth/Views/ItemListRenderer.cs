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
        public bool WeaponHW = false;
        public bool WeaponFW = false;
        public bool WeaponLW = false;
        public bool Weapon2H = false;
        public bool WeaponWar = false;
        public bool WeaponVoid = false;
        public bool WeaponOther = false;
        public bool WeaponTW = false;
        public bool WeaponBow = false;
        public bool WeaponXbow = false;
        public bool ElementSlash = false;
        public bool ElementPierce = false;
        public bool ElementBludge = false;
        public bool ElementFire = false;
        public bool ElementFrost = false;
        public bool ElementStorm = false;
        public bool ElementAcid = false;
        public bool ElementNether = false;
        public bool Armor = false;
        public bool ArmorSetAdept = false;
        public bool ArmorSetDefender = false;
        public bool ArmorSetDexterous = false;
        public bool ArmorSetHearty = false;
        public bool ArmorSetWise = false;
        public bool ArmorSetNoSet = false;
        public bool ArmorSetOther = false;
        public ItemInfo.ArmorSlot ArmorSlots = ItemInfo.ArmorSlot.None;
        public bool Clothing = false;
        public bool Jewelry = false;
        public bool Cloaks = false;
        public bool Summons = false;
        public bool Aetheria = false;
        public bool Salvage = false;
        public bool Other = false;

        // Not a category — an extra AND condition: items carrying two or more legendary spells.
        public bool Doubles = false;

        // True when the filter actually narrows the list (some category ticked, text typed, or Doubles set).
        public bool IsActive => AnyCategorySelected() || !string.IsNullOrWhiteSpace(Text) || Doubles;

        public bool Matches(ItemListRow t)
        {
            if (!IsCategoryVisible(t.SortCategory)) return false;
            if (Armor && t.SortCategory == 1 && ArmorSlots != ItemInfo.ArmorSlot.None &&
                (new ItemInfo(t.Item).GetArmorSlots() & ArmorSlots) == 0) return false;
            if (!MatchesArmorSet(t)) return false;
            if (!MatchesWeaponType(t)) return false;
            if (!MatchesWeaponElement(t)) return false;
            if (!MatchesDoubles(t)) return false;
            return MatchesText(t);
        }

        private bool MatchesArmorSet(ItemListRow row)
        {
            if (!Armor || row.SortCategory != 1 ||
                !(ArmorSetAdept || ArmorSetDefender || ArmorSetDexterous || ArmorSetHearty ||
                  ArmorSetWise || ArmorSetNoSet || ArmorSetOther)) return true;
            // Same Equipment Set property used by ItemInfo; unknown nonzero sets are Other.
            // Missing appraisal data cannot establish that an item has no set.
            if (!row.Item.TryGetValue((Decal.Adapter.Wrappers.LongValueKey)265, out int set) && !row.Item.HasIdData)
                return false;
            switch (set)
            {
                case 0: return ArmorSetNoSet;
                case 14: return ArmorSetAdept;
                case 16: return ArmorSetDefender;
                case 20: return ArmorSetDexterous;
                case 19: return ArmorSetHearty;
                case 21: return ArmorSetWise;
                default: return ArmorSetOther;
            }
        }

        // Subfilters narrow only the weapon category; selected armor/etc. still match.
        // Hidden selections are inactive when the parent Weapons checkbox is off.
        private bool MatchesWeaponType(ItemListRow row)
        {
            if (!Weapons || row.SortCategory != 0 ||
                !(WeaponHW || WeaponFW || WeaponLW || Weapon2H || WeaponWar || WeaponVoid || WeaponTW || WeaponBow || WeaponXbow || WeaponOther)) return true;
            switch (new ItemInfo(row.Item).GetWeaponTypeName())
            {
                case "Heavy": return WeaponHW;
                case "Finesse": return WeaponFW;
                case "Light": return WeaponLW;
                case "Two Hand": return Weapon2H;
                case "War": return WeaponWar;
                case "Nether": return WeaponVoid;
                case "Thrown": return WeaponTW;
                case "Bow": return WeaponBow;
                case "Crossbow": return WeaponXbow;
                default: return WeaponOther;
            }
        }

        // Search the Type summary (SummaryCol1), using the names displayed by the list.
        // Element choices are alternatives, combined with the weapon-type group by AND.
        private bool MatchesWeaponElement(ItemListRow row)
        {
            if (!Weapons || row.SortCategory != 0 ||
                !(ElementSlash || ElementPierce || ElementBludge || ElementFire ||
                  ElementFrost || ElementStorm || ElementAcid || ElementNether)) return true;
            string type = row.SummaryCol1;
            return (ElementSlash && HasElement(type, "Slash")) ||
                (ElementPierce && HasElement(type, "Pierce")) ||
                (ElementBludge && HasElement(type, "Bludge")) ||
                (ElementFire && HasElement(type, "Fire", "Flame")) ||
                (ElementFrost && HasElement(type, "Frost", "Cold")) ||
                (ElementStorm && HasElement(type, "Storm", "Lightning")) ||
                (ElementAcid && HasElement(type, "Acid")) ||
                (ElementNether && HasElement(type, "Void", "Nether"));
        }

        private static bool HasElement(string text, string name, string alias = null) =>
            text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (alias != null && text.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0);

        // Category-only match, ignoring the search text. Used for second-tier identify priority:
        // appraise everything in the selected categories once the exact (text + category) matches
        // are done, so clearing the search term finds the broader set already identified.
        public bool MatchesCategory(ItemListRow t) => IsCategoryVisible(t.SortCategory);

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

        // The item's searchable text: name plus every summary column, as one string.
        private static string Combined(ItemListRow t) => $"{t.DisplayName} {t.SummaryCol1} {t.SummaryCol2} {t.SummaryCol3} {t.SummaryCol4}";

        // "Doubles": items doubled up on their highest cantrip tier — two or more legendary, OR
        // two or more epic with no legendary, OR two or more major with no epic/legendary. Counts
        // the tier words in the row text (case-insensitive); a single higher-tier cantrip outranks
        // (disqualifies) a lower-tier double.
        private bool MatchesDoubles(ItemListRow t)
        {
            if (!Doubles) return true;

            string combined = Combined(t);

            int legendary = CountOccurrences(combined, "legendary");
            if (legendary >= 2) return true;
            if (legendary > 0) return false;   // a single legendary outranks any epic/major double

            int epic = CountOccurrences(combined, "epic");
            if (epic >= 2) return true;
            if (epic > 0) return false;        // a single epic outranks any major double

            return CountOccurrences(combined, "major") >= 2;
        }

        private bool MatchesText(ItemListRow t)
        {
            string trimmed = (Text ?? "").Trim();
            string[] terms = trimmed.Length > 0
                ? trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];

            if (terms.Length == 0) return true;
            string combined = Combined(t) + " " + t.Character;
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
                    if (parts.Length == 0) continue;   // bare ".*" — no real term, impose no constraint
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
        // the four summary columns, with the id stashed in the (hidden) last column. Takes
        // the "not complete" icon for column 0 and the id of the selected row.
        public static void Render(HudList list, List<ItemListRow> items, int iconNotComplete, int selectedId, bool showCharacter = false, ItemListRow selectedItem = null)
        {
            for (int x = 0; x < items.Count; x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= list.RowCount) { row = list.AddRow(); } else { row = list[x]; }

                ItemListRow item = items[x];

                if (showCharacter) ((HudStaticText)row[0]).Text = item.Character;
                else AssignImage((HudPictureBox)row[0], iconNotComplete);
                AssignImage((HudPictureBox)row[1], item.Icon);
                ((HudStaticText)row[2]).Text = item.DisplayName;
                ((HudStaticText)row[3]).Text = item.SummaryCol1;
                ((HudStaticText)row[4]).Text = item.SummaryCol2;
                ((HudStaticText)row[5]).Text = item.SummaryCol3;
                ((HudStaticText)row[6]).Text = item.SummaryCol4;
                ((HudStaticText)row[7]).Text = item.Id.ToString();

                // Center the Effect and Info columns; the rest keep their default left alignment.
                ((HudStaticText)row[4]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                ((HudStaticText)row[5]).TextAlignment = VirindiViewService.WriteTextFormats.Center;

                bool selected = showCharacter ? ReferenceEquals(item, selectedItem) : item.Id == selectedId && selectedId != 0;
                SetRowColor(row, selected, loading: !item.IsComplete, showCharacter: showCharacter);
            }

            // Trim surplus rows. Nothing to clean up alongside them: AssignImage keeps its
            // state on the box, so a destroyed row takes it with it.
            while (list.RowCount > items.Count)
            {
                list.RemoveRow(list.RowCount - 1);
            }
        }

        // Tint the row's text columns (Name..Details): highlighted when selected, dim grey
        // while still loading its appraisal, otherwise the default colour.
        public static void SetRowColor(HudList.HudListRowAccessor row, bool selected, bool loading, bool showCharacter = false)
        {
            for (int col = 2; col <= 6; col++)
            {
                HudStaticText cell = (HudStaticText)row[col];
                if (selected) cell.TextColor = ColorSelected;
                else if (loading) cell.TextColor = ColorLoading;
                else cell.ResetTextColor();
            }
            if (showCharacter)
            {
                HudStaticText character = (HudStaticText)row[0];
                if (selected) character.TextColor = ColorSelected;
                else character.ResetTextColor();
            }
        }

        // Only swap the image when it actually changes; assigning is comparatively expensive.
        // The box is its own record of what it's showing — an int converts implicitly to
        // ACImage and PortalImageID reads that id back — so there's nothing to cache, and no
        // dead keys to clean up when a row is destroyed. Mirrors MainView.AssignImage.
        private static void AssignImage(HudPictureBox box, int icon)
        {
            // A cleared box reads back as null rather than 0, which is the same "no image".
            int current = box.Image == null ? 0 : box.Image.PortalImageID;
            if (current == icon) return;

            if (icon == 0)
            {
                box.Image = null;
            }
            else
            {
                box.Image = icon;
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
