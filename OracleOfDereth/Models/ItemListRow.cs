using System;
using System.Collections.Generic;
using Decal.Adapter.Wrappers;

namespace OracleOfDereth
{
    // Explicit values preserve the existing category grouping when sorting rows.
    public enum ItemCategory
    {
        Weapons = 0,
        Armor = 1,
        Jewelry = 2,
        Cloaks = 3,
        Summons = 4,
        Aetheria = 5,
        Salvage = 6,
        Clothing = 7,
        Other = 9
    }

    // Cached display of an immutable item observation, shared by live and saved lists.
    public class ItemListRow
    {
        private readonly bool completeWithoutAppraisal;
        public Item Item { get; }
        public int Id => Item.Id;
        public int Icon => Item.Icon;
        public string Character => Item.Character;
        public string Server => Item.Server;
        public string AetheriaSurge { get; private set; } = "";
        public string DisplayName { get; private set; }
        // Ready for display, including items that do not require appraisal.
        public bool IsComplete { get; private set; }
        public ItemCategory SortCategory { get; private set; } = ItemCategory.Weapons;
        public string SummaryCol1 { get; private set; } = "";
        public string SummaryCol2 { get; private set; } = "";
        public string SummaryCol3 { get; private set; } = "";
        public string SummaryCol4 { get; private set; } = "";
        public int SortCol2 { get; private set; } = 0;
        public int SortCol3OD { get; private set; } = 0;     // OD value (Col3 cycle leads with this for weapons)
        public int SortCol3 { get; private set; } = 0;       // total attack modifier (Col3 secondary sort)
        public int SortCol3Melee { get; private set; } = 0;  // total melee-defense modifier (Col3 tertiary sort)
        public int SortCol3Work { get; private set; } = 0;   // workmanship (Col3 fourth sort)
        public int SortCol4 { get; private set; } = 0;
        public string Description { get; private set; } = "";
        public string DescriptionWithOwner => string.IsNullOrEmpty(Character) ? Description
            : Description + " (Last on " + Character + ")";

        public ItemListRow(WorldObject worldObject) : this(new Item(worldObject)) { }

        public ItemListRow(Item item, bool completeWithoutAppraisal = false)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            this.completeWithoutAppraisal = completeWithoutAppraisal;
            AetheriaSurge = "";
            DisplayName = Item.Name;
        }

        // Fill the base data available before ID. Type and category are derivable without
        // an appraisal, so set them now — that keeps the row in its final category (filter)
        // bucket from the start. Leaves IsComplete false; the detail columns stay blank.
        public void PopulateStub()
        {
            ItemInfo info = new ItemInfo(Item);

            AetheriaSurge = "";
            DisplayName = Item.Name;
            SummaryCol1 = GetSummaryCol1(info);
            SummaryCol2 = SummaryCol3 = SummaryCol4 = "";
            SortCol2 = SortCol3OD = SortCol3 = SortCol3Melee = SortCol3Work = SortCol4 = 0;
            Description = Item.Name + " (details unavailable)";
            SortCategory = GetSortCategory(info);
            IsComplete = false;
        }

        // Compute display text once when properties load/change, never during rendering.
        public void Populate()
        {
            if (!Item.HasIdData && !completeWithoutAppraisal) { PopulateStub(); return; }
            ItemInfo info = new ItemInfo(Item);
            AetheriaSurge = info.IsAetheria ? info.GetAetheriaSurge() : "";
            DisplayName = info.GetName();
            SortCategory = GetSortCategory(info);
            SummaryCol1 = GetSummaryCol1(info);
            SummaryCol2 = GetSummaryCol2(info);
            SummaryCol3 = GetSummaryCol3(info);
            SummaryCol4 = GetSummaryCol4(info);
            SortCol2 = 0; // Col2 now shows the imbue string; its sort falls through to SummaryCol2
            SortCol3OD = GetSortInt(info.GetODValue()); // Col3 cycle leads with OD, then the attack/melee mods
            SortCol3 = GetSortInt((int)info.GetTotalAttack());
            SortCol3Melee = GetSortInt((int)info.GetTotalMeleeDefense());
            SortCol3Work = GetSortInt(info.GetWorkmanshipValue());

            // Salvage's Col3 is a lone decimal workmanship, not the OD/attack/melee spread the
            // Col3 cycle steps through — so point every position of the cycle at it (x100 to keep
            // the decimals). Otherwise the sort falls through to the string, where "Work 10.00"
            // would come out ahead of "Work 6.15".
            if (info.IsSalvage)
            {
                int salvageWork = (int)(info.GetSalvageWorkmanshipValue() * 100);
                SortCol3OD = salvageWork;
                SortCol3 = salvageWork;
                SortCol3Melee = salvageWork;
                SortCol3Work = salvageWork;
            }
            SortCol4 = 0; // Col4 (cantrips) is a string; sort falls through to SummaryCol4
            Description = info.ToString();
            IsComplete = true;
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
            if (info.IsSalvage) return info.GetSalvageTinkerSkillString(); // e.g. "Weapon Tink"
            return "";
        }

        private static string GetSummaryCol3(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetWeaponODModsString(Setting.ShowWeaponScoreWorkmanship.IsYes);
            if (info.IsSalvage) return info.GetSalvageWorkmanshipString(); // e.g. "Work 9.50"
            if (info.IsHealingKit) return info.GetHealingKitString();      // e.g. "+250 Skill | +200% Bonus"
            if (info.IsManaStone) return info.GetManaStoneString();        // e.g. "250% Efficient | 10% Chance"
            if (info.IsGem) return info.GetGemUseString();                 // "Unlimited Use" / "Single Use"
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
            else col4 = info.GetFullDescription();

            // Append the wield requirement and tinks (e.g. "Tinks 5") to whatever the column
            // shows. Only skill-based wield reqs ("Two Handed Combat 420") in general; a plain
            // level req ("Wield Lvl 180") isn't worth a slot — except summons, whose wield level
            // is their key gating and is always shown.
            string wield = info.GetWieldReqString();
            string tinks = info.GetTinksString();

            var parts = new List<string>();

            // Weapons lead Col4 with their slayer bonus (e.g. "Virindi Slayer"), if any.
            if (info.IsWeapon)
            {
                string slayer = info.GetSlayerString();
                if (slayer.Length > 0) parts.Add(slayer);
            }

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

        private static ItemCategory GetSortCategory(ItemInfo info)
        {
            if (info.IsWeapon && !info.IsAmmo) return ItemCategory.Weapons;
            if (info.IsClothing) return ItemCategory.Clothing;
            if (info.IsArmorClothing) return ItemCategory.Armor;
            if (info.IsJewelry) return ItemCategory.Jewelry;
            if (info.IsCloak) return ItemCategory.Cloaks;
            if (info.IsSummon) return ItemCategory.Summons;
            if (info.IsAetheria) return ItemCategory.Aetheria;
            if (info.IsSalvage || info.IsFoolproof) return ItemCategory.Salvage;
            return ItemCategory.Other;
        }

        private static int GetSortInt(int? value)
        {
            return value ?? 0;
        }


        public ItemListRow Clone() => (ItemListRow)MemberwiseClone();
    }
}
