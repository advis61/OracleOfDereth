using System.Collections.Generic;

using Decal.Adapter.Wrappers;

namespace OracleOfDereth
{
    // A single row in an ItemList. Holds the display data and knows how to fill itself
    // from a WorldObject; the identify/stub/sort pipeline that drives this lives in ItemList.
    public class Item
    {
        public string Name = "";
        public int Id = 0;
        public int Icon = 0;
        public int SortCategory = 0; // Groups like items together: 0=weapon, 1=armor, 2=jewelry, 3=cloak, 4=summon, 5=aetheria, 9=other
        public string SummaryCol1 = "";
        public string SummaryCol2 = "";
        public string SummaryCol3 = "";
        public string SummaryCol4 = "";
        public int SortCol2 = 0;
        public int SortCol3OD = 0;     // OD value (Col3 cycle leads with this for weapons)
        public int SortCol3 = 0;       // total attack modifier (Col3 secondary sort)
        public int SortCol3Melee = 0;  // total melee-defense modifier (Col3 tertiary sort)
        public int SortCol4 = 0;
        public string Description = "";

        // False until the appraisal arrives. Stub rows (icon + name only) show
        // immediately on Add; the detail columns fill in once this flips true.
        public bool IsIdentified = false;

        // Fill the base data available before ID. Type and category are derivable without
        // an appraisal, so set them now — that keeps the row in its final category (filter)
        // bucket from the start. Leaves IsIdentified false; the detail columns stay blank.
        public void PopulateStub(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Name = wo.Name;
            Icon = wo.Icon;
            SummaryCol1 = GetSummaryCol1(info);
            SortCategory = GetSortCategory(info);
            IsIdentified = false;
        }

        // Fill the identify-dependent fields from the appraised WorldObject.
        public void Populate(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Name = info.GetName();
            Icon = wo.Icon;
            SortCategory = GetSortCategory(info);
            SummaryCol1 = GetSummaryCol1(info);
            SummaryCol2 = GetSummaryCol2(info);
            SummaryCol3 = GetSummaryCol3(info);
            SummaryCol4 = GetSummaryCol4(info);
            SortCol2 = 0; // Col2 now shows the imbue string; its sort falls through to SummaryCol2
            SortCol3OD = GetSortInt(info.GetODValue()); // Col3 cycle leads with OD, then the attack/melee mods
            SortCol3 = GetSortInt((int)info.GetTotalAttack());
            SortCol3Melee = GetSortInt((int)info.GetTotalMeleeDefense());
            SortCol4 = 0; // Col4 (cantrips) is a string; sort falls through to SummaryCol4
            Description = info.ToString();
            IsIdentified = true;
        }

        // Col1 — item type / slot. Weapons append their damage element (e.g. "Heavy Acid",
        // "Two Hand Bludgeon"). The imbue moved to Col2 and the OD value to Col3.
        private static string GetSummaryCol1(ItemInfo info)
        {
            string type = info.GetItemSlotName();

            if (info.IsWeapon)
            {
                string element = info.GetElementName();

                // Append the element (e.g. "Two Hand Fire"), unless it just repeats the type —
                // a Nether caster's type is also "Nether", so don't print "Nether Nether".
                if (element != "" && element != type) type += " " + element;
            }
            else if (info.IsSummon)
            {
                type = info.GetSummonSpecString(); // Primalist / Necromancer / Naturalist / Generic
            }
            return type;
        }

        private static string GetSummaryCol2(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetImbueString(); // full imbue list (may carry more than one)
            if (info.IsCloak) return info.GetCloakProc();
            if (info.IsArmorClothing) return info.GetSetName();
            if (info.IsJewelry) return info.GetSetName();
            return "";
        }

        private static string GetSummaryCol3(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetWeaponODModsString(); // OD + attack/melee mods, e.g. "OD +5 | 18% | 20%"
            if (info.IsCloak) return info.GetRatingsString();
            if (info.IsSummon) return info.GetSummonString(); // "DMG x% | DEF y%"
            if (info.IsArmorClothing) return info.GetRatingsString();
            if (info.IsJewelry) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol4(ItemInfo info)
        {
            string col4 = "";
            if (info.IsWeapon) col4 = info.GetCantripsString();
            else if (info.IsCloak) col4 = $"Level {info.GetCloakLevel()}, {info.GetFullSetName()}";
            else if (info.IsAetheria) col4 = info.GetAetheriaSummaryString(); // "Level 5, Defense, Destruction"
            else if (info.IsArmorClothing || info.IsJewelry) col4 = info.GetSpellsString();
            else if (info.IsRare) col4 = info.GetSpellsString();
            else if (info.IsSalvage) col4 = info.GetSalvageDescriptionString();
            else if (info.wo.ObjectClass == ObjectClass.BaseCooking || info.wo.ObjectClass == ObjectClass.CraftedCooking) col4 = info.GetFullDescription();

            // Append the wield requirement and tinks (e.g. "Tinks 5") to whatever the column
            // shows. Only skill-based wield reqs ("Two Handed Combat 420") in general; a plain
            // level req ("Wield Lvl 180") isn't worth a slot — except summons, whose wield level
            // is their key gating and is always shown.
            string wield = info.GetWieldReqString();
            string tinks = info.GetTinksString();

            var parts = new List<string>();
            if (col4.Length > 0) parts.Add(col4);
            if (wield.Length > 0 && (info.IsSummon || info.GetWieldReqName() != "Wield Lvl")) parts.Add(wield);
            if (tinks.Length > 0) parts.Add(tinks);

            // Weapons: tack any missile/magic defense bonus and the Multi-Strike flag on the end.
            if (info.IsWeapon)
            {
                string extras = info.GetWeaponExtrasString();
                if (extras.Length > 0) parts.Add(extras);
            }

            return string.Join(", ", parts);
        }

        private static int GetSortCategory(ItemInfo info)
        {
            if (info.IsWeapon) return 0;
            if (info.IsClothing) return 7;
            if (info.IsArmorClothing) return 1;
            if (info.IsJewelry) return 2;
            if (info.IsCloak) return 3;
            if (info.IsSummon) return 4;
            if (info.IsAetheria) return 5;
            if (info.IsSalvage || info.IsFoolproof) return 6;
            return 9;
        }

        private static int GetSortInt(int? value)
        {
            return value ?? 0;
        }

        // Shallow copy — all fields are value types or immutable strings, so this is a full,
        // independent copy. Used to stash/restore rows in ItemCache.
        public Item Clone() => (Item)MemberwiseClone();

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
