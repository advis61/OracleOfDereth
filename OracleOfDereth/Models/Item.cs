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
        public int ObjectClassId = 0;
        public int SortCategory = 0; // Groups like items together: 0=weapon, 1=armor, 2=jewelry, 3=cloak, 4=summon, 5=aetheria, 9=other
        public string SummaryCol1 = "";
        public string SummaryCol2 = "";
        public string SummaryCol3 = "";
        public string SummaryCol4 = "";
        public int SortCol2 = 0;
        public int SortCol3 = 0;
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
            ObjectClassId = (int)wo.ObjectClass;
            SummaryCol1 = info.GetItemSlotName();
            SortCategory = GetSortCategory(info);
            IsIdentified = false;
        }

        // Fill the identify-dependent fields from the appraised WorldObject.
        public void Populate(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Name = info.GetName();
            Icon = wo.Icon;
            ObjectClassId = (int)wo.ObjectClass;
            SortCategory = GetSortCategory(info);
            SummaryCol1 = GetSummaryCol1(info);
            SummaryCol2 = GetSummaryCol2(info);
            SummaryCol3 = GetSummaryCol3(info);
            SummaryCol4 = GetSummaryCol4(info);
            SortCol2 = GetSortInt(info.GetODValue());
            SortCol3 = GetSortInt(info.GetOMValue());
            SortCol4 = GetSortInt(info.GetOAValue());
            Description = info.ToString();
            IsIdentified = true;
        }

        private static string GetSummaryCol1(ItemInfo info)
        {
            return info.GetItemSlotName();
        }

        private static string GetSummaryCol2(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetODString();
            if (info.IsCloak) return info.GetCloakProc();
            if (info.IsSummon) return "DMG " + info.GetSummonDamageString();
            if (info.IsAetheria) return info.GetSetName();
            if (info.IsArmorClothing) return info.GetSetName();
            return "";
        }

        private static string GetSummaryCol3(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOMString();
            if (info.IsCloak) return info.GetRatingsString();
            if (info.IsSummon) return "DEF " + info.GetSummonDefenseString();
            if (info.IsAetheria) return info.GetAetheriaSurge();
            if (info.IsArmorClothing) return info.GetRatingsString();
            if (info.IsJewelry) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol4(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOAString();
            if (info.IsCloak) return $"Level {info.GetCloakLevel()}, {info.GetFullSetName()}";
            if (info.IsAetheria) return info.GetAetheriaLevel() > 0 ? "Level " + info.GetAetheriaLevel() : "";
            if (info.IsArmorClothing || info.IsJewelry) return info.GetCantripsString();
            return "";
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
