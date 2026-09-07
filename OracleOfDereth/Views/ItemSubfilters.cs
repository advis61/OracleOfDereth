using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    // One layout and binding definition for Inventory, Items, and Trade.
    // Each instance owns its selections and most-recently-enabled category.
    internal sealed class ItemSubfilters : IDisposable
    {
        private sealed class Definition
        {
            public readonly string Category;
            public readonly string Label;
            public readonly int Left;
            public readonly int Width;
            public readonly Action<ItemFilter, bool> Apply;

            public Definition(string category, string label, int left, int width, Action<ItemFilter, bool> apply)
            {
                Category = category;
                Label = label;
                Left = left;
                Width = width;
                Apply = apply;
            }
        }

        private static readonly Definition[] Definitions =
        {
            new Definition("Weapons", "HW", 5, 40, (filter, value) => { filter.WeaponHW = value; }),
            new Definition("Weapons", "FW", 53, 39, (filter, value) => { filter.WeaponFW = value; }),
            new Definition("Weapons", "LW", 100, 39, (filter, value) => { filter.WeaponLW = value; }),
            new Definition("Weapons", "2H", 147, 34, (filter, value) => { filter.Weapon2H = value; }),
            new Definition("Weapons", "War", 348, 42, (filter, value) => { filter.WeaponWar = value; }),
            new Definition("Weapons", "Void", 398, 46, (filter, value) => { filter.WeaponVoid = value; }),
            new Definition("Weapons", "Other", 452, 50, (filter, value) => { filter.WeaponOther = value; }),
            new Definition("Weapons", "TW", 301, 39, (filter, value) => { filter.WeaponTW = value; }),
            new Definition("Weapons", "Bow", 189, 43, (filter, value) => { filter.WeaponBow = value; }),
            new Definition("Weapons", "XBow", 240, 53, (filter, value) => { filter.WeaponXbow = value; }),
            new Definition("Weapons", "Bludge", 541, 70, (filter, value) => { filter.ElementBludge = value; }),
            new Definition("Weapons", "Pierce", 619, 64, (filter, value) => { filter.ElementPierce = value; }),
            new Definition("Weapons", "Slash", 691, 60, (filter, value) => { filter.ElementSlash = value; }),
            new Definition("Weapons", "Lightning", 759, 75, (filter, value) => { filter.ElementStorm = value; }),
            new Definition("Weapons", "Acid", 842, 70, (filter, value) => { filter.ElementAcid = value; }),
            new Definition("Weapons", "Fire", 920, 64, (filter, value) => { filter.ElementFire = value; }),
            new Definition("Weapons", "Cold", 992, 60, (filter, value) => { filter.ElementFrost = value; }),
            new Definition("Weapons", "Nether", 1060, 70, (filter, value) => { filter.ElementNether = value; }),
            new Definition("Armor", "Head", 5, 48, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.Head; }),
            new Definition("Armor", "Chest", 61, 52, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.Chest; }),
            new Definition("Armor", "Girth", 121, 48, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.Abdomen; }),
            new Definition("Armor", "UArm", 177, 51, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.UpperArms; }),
            new Definition("Armor", "LArm", 236, 51, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.LowerArms; }),
            new Definition("Armor", "Hands", 295, 54, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.Hands; }),
            new Definition("Armor", "ULeg", 357, 51, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.UpperLegs; }),
            new Definition("Armor", "LLeg", 416, 49, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.LowerLegs; }),
            new Definition("Armor", "Feet", 473, 44, (filter, value) => { if (value) filter.ArmorSlots |= ItemInfo.ArmorSlot.Feet; }),
            new Definition("Armor", "Adept", 691, 60, (filter, value) => { filter.ArmorSetAdept = value; }),
            new Definition("Armor", "Defender", 759, 75, (filter, value) => { filter.ArmorSetDefender = value; }),
            new Definition("Armor", "Dextrous", 842, 70, (filter, value) => { filter.ArmorSetDexterous = value; }),
            new Definition("Armor", "Hearty", 920, 64, (filter, value) => { filter.ArmorSetHearty = value; }),
            new Definition("Armor", "Wise", 992, 60, (filter, value) => { filter.ArmorSetWise = value; }),
            new Definition("Armor", "No Set", 619, 64, (filter, value) => { filter.ArmorSetNoSet = value; }),
            new Definition("Armor", "Other", 1060, 70, (filter, value) => { filter.ArmorSetOther = value; }),
            new Definition("Clothing", "Shirt", 5, 48, (filter, value) => { filter.ClothingShirt = value; }),
            new Definition("Clothing", "Pants", 61, 52, (filter, value) => { filter.ClothingPants = value; }),
            new Definition("Clothing", "Full coverage", 121, 100, (filter, value) => { filter.ClothingFullCoverage = value; }),
            new Definition("Clothing", "Partial coverage", 229, 120, (filter, value) => { filter.ClothingPartialCoverage = value; }),
            new Definition("Jewelry", "Necklace", 5, 76, (filter, value) => { filter.JewelryNecklace = value; }),
            new Definition("Jewelry", "Trinket", 89, 62, (filter, value) => { filter.JewelryTrinket = value; }),
            new Definition("Jewelry", "Bracelet", 159, 72, (filter, value) => { filter.JewelryBracelet = value; }),
            new Definition("Jewelry", "Ring", 239, 48, (filter, value) => { filter.JewelryRing = value; }),
            new Definition("Cloaks", "Level 1", 5, 64, (filter, value) => { filter.CloakLevel1 = value; }),
            new Definition("Cloaks", "Level 2", 77, 64, (filter, value) => { filter.CloakLevel2 = value; }),
            new Definition("Cloaks", "Level 3", 149, 64, (filter, value) => { filter.CloakLevel3 = value; }),
            new Definition("Cloaks", "Level 4", 221, 64, (filter, value) => { filter.CloakLevel4 = value; }),
            new Definition("Cloaks", "Level 5", 293, 64, (filter, value) => { filter.CloakLevel5 = value; }),
            new Definition("Cloaks", "Other", 1060, 70, (filter, value) => { filter.CloakProcOther = value; }),
            new Definition("Cloaks", "-200", 619, 64, (filter, value) => { filter.CloakProcDamage200 = value; }),
            new Definition("Cloaks", "CiS", 691, 60, (filter, value) => { filter.CloakProcCiS = value; }),
            new Definition("Cloaks", "Melee", 759, 75, (filter, value) => { filter.CloakProcMelee = value; }),
            new Definition("Cloaks", "Missile", 842, 70, (filter, value) => { filter.CloakProcMissile = value; }),
            new Definition("Cloaks", "Magic", 920, 64, (filter, value) => { filter.CloakProcMagic = value; }),
            new Definition("Cloaks", "AoE", 992, 60, (filter, value) => { filter.CloakProcAoE = value; }),
            new Definition("Cloaks", "Other", 365, 54, (filter, value) => { filter.CloakLevelOther = value; }),
            new Definition("Salvage", "Iron", 5, 44, (filter, value) => { filter.SalvageIron = value; }),
            new Definition("Salvage", "Granite", 57, 68, (filter, value) => { filter.SalvageGranite = value; }),
            new Definition("Salvage", "Mahogany", 133, 88, (filter, value) => { filter.SalvageMahogany = value; }),
            new Definition("Salvage", "Green Garnet", 229, 108, (filter, value) => { filter.SalvageGreenGarnet = value; }),
            new Definition("Salvage", "Velvet", 345, 60, (filter, value) => { filter.SalvageVelvet = value; }),
            new Definition("Salvage", "Brass", 413, 54, (filter, value) => { filter.SalvageBrass = value; }),
            new Definition("Salvage", "Steel", 475, 50, (filter, value) => { filter.SalvageSteel = value; }),
            new Definition("Salvage", "Rends", 533, 58, (filter, value) => { filter.SalvageRends = value; }),
            new Definition("Salvage", "Imbues", 599, 66, (filter, value) => { filter.SalvageImbues = value; }),
            new Definition("Salvage", "Other", 673, 54, (filter, value) => { filter.SalvageOther = value; }),
            new Definition("Other", "Alchemy", 5, 76, (filter, value) => { filter.OtherClassAlchemy = value; }),
            new Definition("Other", "Component", 89, 94, (filter, value) => { filter.OtherClassComponent = value; }),
            new Definition("Other", "Cooking", 191, 76, (filter, value) => { filter.OtherClassCooking = value; }),
            new Definition("Other", "Food", 275, 50, (filter, value) => { filter.OtherClassFood = value; }),
            new Definition("Other", "Gem", 333, 46, (filter, value) => { filter.OtherClassGem = value; }),
            new Definition("Other", "HealingKit", 387, 86, (filter, value) => { filter.OtherClassHealingKit = value; }),
            new Definition("Other", "Key", 481, 42, (filter, value) => { filter.OtherClassKey = value; }),
            new Definition("Other", "Lockpick", 531, 72, (filter, value) => { filter.OtherClassLockpick = value; }),
            new Definition("Other", "ManaStone", 611, 88, (filter, value) => { filter.OtherClassManaStone = value; }),
            new Definition("Other", "Misc", 707, 48, (filter, value) => { filter.OtherClassMisc = value; }),
            new Definition("Other", "Rare", 763, 50, (filter, value) => { filter.OtherClassRare = value; }),
            new Definition("Other", "Other", 821, 54, (filter, value) => { filter.OtherClassOther = value; }),
            new Definition("Summons", "Naturalist", 5, 85, (filter, value) => { filter.SummonNaturalist = value; }),
            new Definition("Summons", "Necromancer", 98, 108, (filter, value) => { filter.SummonNecromancer = value; }),
            new Definition("Summons", "Primalist", 214, 78, (filter, value) => { filter.SummonPrimalist = value; }),
            new Definition("Summons", "Other", 300, 54, (filter, value) => { filter.SummonOther = value; }),
            new Definition("Aetheria", "1", 5, 26, (filter, value) => { filter.AetheriaLevel1 = value; }),
            new Definition("Aetheria", "2", 35, 26, (filter, value) => { filter.AetheriaLevel2 = value; }),
            new Definition("Aetheria", "3", 65, 26, (filter, value) => { filter.AetheriaLevel3 = value; }),
            new Definition("Aetheria", "4", 95, 26, (filter, value) => { filter.AetheriaLevel4 = value; }),
            new Definition("Aetheria", "5", 125, 26, (filter, value) => { filter.AetheriaLevel5 = value; }),
            new Definition("Aetheria", "Blue", 163, 48, (filter, value) => { filter.AetheriaColorBlue = value; }),
            new Definition("Aetheria", "Yellow", 215, 62, (filter, value) => { filter.AetheriaColorYellow = value; }),
            new Definition("Aetheria", "Red", 281, 46, (filter, value) => { filter.AetheriaColorRed = value; }),
            new Definition("Aetheria", "Defense", 339, 70, (filter, value) => { filter.AetheriaSigilDefense = value; }),
            new Definition("Aetheria", "Destruction", 413, 90, (filter, value) => { filter.AetheriaSigilDestruction = value; }),
            new Definition("Aetheria", "Fury", 507, 46, (filter, value) => { filter.AetheriaSigilFury = value; }),
            new Definition("Aetheria", "Growth", 557, 66, (filter, value) => { filter.AetheriaSigilGrowth = value; }),
            new Definition("Aetheria", "Vigor", 627, 50, (filter, value) => { filter.AetheriaSigilVigor = value; }),
            new Definition("Aetheria", "Affliction", 699, 76, (filter, value) => { filter.AetheriaSurgeAffliction = value; }),
            new Definition("Aetheria", "Destruction", 779, 90, (filter, value) => { filter.AetheriaSurgeDestruction = value; }),
            new Definition("Aetheria", "Festering", 873, 74, (filter, value) => { filter.AetheriaSurgeFestering = value; }),
            new Definition("Aetheria", "Protection", 951, 80, (filter, value) => { filter.AetheriaSurgeProtection = value; }),
            new Definition("Aetheria", "Regeneration", 1035, 100, (filter, value) => { filter.AetheriaSurgeRegeneration = value; }),
        };

        private readonly List<HudCheckBox> controls = new List<HudCheckBox>();
        private readonly Dictionary<string, HudCheckBox> categories;
        private readonly List<HudCheckBox> categoryOrder = new List<HudCheckBox>();
        private readonly EventHandler changed;
        private bool resetting;

        public ItemSubfilters(HudFixedLayout layout, Func<string, HudCheckBox> findCategory, EventHandler changed)
        {
            this.changed = changed;
            categories = Definitions.Select(d => d.Category).Distinct().ToDictionary(name => name, findCategory);
            foreach (var definition in Definitions)
            {
                var checkbox = new HudCheckBox { Text = definition.Label };
                layout.AddControl(checkbox, new Rectangle(definition.Left, 0, definition.Width, 16));
                checkbox.Visible = false;
                checkbox.Change += SelectionChanged;
                controls.Add(checkbox);
            }
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            if (!resetting) changed(sender, e);
        }

        public void CategoryChanged(HudCheckBox checkbox)
        {
            if (checkbox != null && categories.ContainsValue(checkbox))
            {
                categoryOrder.Remove(checkbox);
                if (checkbox.Checked) categoryOrder.Add(checkbox);
            }
            var active = categoryOrder.LastOrDefault();
            for (int i = 0; i < controls.Count; i++)
                controls[i].Visible = categories[Definitions[i].Category] == active;
        }

        public ItemFilter Apply(ItemFilter filter)
        {
            filter.ArmorSlots = ItemInfo.ArmorSlot.None;
            for (int i = 0; i < controls.Count; i++)
                Definitions[i].Apply(filter, controls[i].Checked);
            return filter;
        }

        public void Reset()
        {
            resetting = true;
            try
            {
                categoryOrder.Clear();
                foreach (var checkbox in controls)
                {
                    checkbox.Checked = false;
                    checkbox.Visible = false;
                }
            }
            finally { resetting = false; }
        }

        public void Dispose()
        {
            foreach (var checkbox in controls) checkbox.Change -= SelectionChanged;
            controls.Clear();
            categoryOrder.Clear();
            categories.Clear();
            // The view owns and disposes the layout and its child controls.
        }
    }
}
