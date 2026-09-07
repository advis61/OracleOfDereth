using Decal.Adapter.Wrappers;
using OracleOfDereth;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

internal static class VGInventoryTests
{
    // Captured VGI 1.0.0.8 weapon blob. Owner metadata lives in the SQL row, not here.
    private const string Jambiya = "JAAAABgAAA2YQiGRAAAADdQOAAABAAAN3hUAABoAAA0BAAAAGwAADRIAAAATAAAAhxQAACMAAA0BAAAAEAAADQEAAAAPAAANAQAAAAIAAA0hFwBQDgAADQAAEAAFAAAAEgAAABQAAA0CAAAAgwAAADoAAAAnAAANARgCAKAAAACkAQAAsAAAACwAAACxAAAAAQAAAGEBAAAGAAAAYgAAADSkMGqyAAAAJwAAAHMAAACGAQAAaQAAAAcAAABqAAAAcgEAAGsAAABLBwAAbAAAAEsHAACsAAAABwAAAC0AAAAQAAAAbQAAAMgAAACeAAAAAgAAAC8AAACgAAAAnwAAACwAAAAhAAANEAAAAB8AAA0MAAAAIAAADSwAAAAiAAANJAAAAAIAAAABAAAAD0ZsYW1pbmcgSmFtYml5YRAAAAAgRmxhbWluZyBKYW1iaXlhIG9mIEJsb29kIERyaW5rZXIAAAAABwAAAAkAAAoAAAAAAAAcQAUAAAAAAAAgERGxvx0AAACPwvUoXI/yPwsAAAoAAADAHoXbPw4AAAoAAAAAAADwPw0AAAoAAAAAAADwPwwAAAoAAADgUbjyPwAAAAAEAAAAUAYAAMkQAABVEgAAyRcAAP////8=";

    public static void Run()
    {
        Setting.Init();
        AssertObservations();
        AssertWeaponSubfilters();
        AssertWeaponDoubles();
        AssertAmmunitionCategory();
        AssertElementSubfilters();
        AssertArmorSubfilters();
        AssertArmorSetSubfilters();
        AssertClothingSubfilters();
        AssertJewelrySubfilters();
        AssertCloakSubfilters();
        AssertSummonSubfilters();
        AssertAetheriaSubfilters();
        AssertOtherClassSubfilters();
        AssertSalvageSubfilters();
        AssertSharedSubfilterBindings();
        AssertCloakEffectSubfilters();
        var settings = new XmlDocument();
        settings.LoadXml("<Settings />");
        typeof(SettingsFile).GetField("_doc", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, settings);
        byte[] bytes = Convert.FromBase64String(Jambiya);
        var weapon = VGInventory.DecodeItem("Conquest", "Weapon Mule", -42, "Flaming Jambiya", ObjectClass.MeleeWeapon, bytes);
        var info = new ItemInfo(weapon);
        Check(info.GetODValue() == 8, "Captured Jambiya should have +8 OD.");
        Check(Math.Abs(info.GetWeaponDamageLow() - 20.52) < .001, "Damage variance did not decode.");
        Check(weapon.Icon == 5598 && weapon.SpellCount == 4 && weapon.Spell(3) == 6089, "Icon or innate spell list did not decode.");
        Check(weapon.ActiveSpellCount == 0, "VGI must not invent active spells.");
        Check(weapon.Values((LongValueKey)999999, 17) == 17, "Missing property did not honor its default.");
        Check(weapon.Id == -42 && weapon.Character == "Weapon Mule", "Owner metadata was lost.");
        Reject(bytes.Take(bytes.Length - 1).ToArray());
        Reject(new byte[] { 255, 255, 255, 255 });
        Reject(bytes.Concat(new byte[] { 0 }).ToArray());

        var generic = VGInventory.DecodeItem("Conquest", "Mule B", -42, "Test Dagger", ObjectClass.MeleeWeapon, Fixture());
        Check(generic.Values((StringValueKey)16).Length == 140, "Multi-byte ASCII string prefix did not decode.");
        Check(generic.Values((BoolValueKey)1), "Boolean dictionary did not decode.");
        var equipped = VGInventory.DecodeItem("Conquest", "Mule B", -42, "Test Dagger", ObjectClass.MeleeWeapon, Fixture(true));
        Check(new ItemInfo(equipped).GetODValue() == null, "Saved equipped weapon must not use a live holder or guess its buffs.");

        var list = new ItemList();
        list.Load(new[] { generic, VGInventory.DecodeItem("Conquest", "Mule A", -42, "Other Dagger", ObjectClass.MeleeWeapon, Fixture()) });
        Check(list.Items.Count == 2 && list.QueueCount == 0, "Saved items with the same ID on different characters were lost or queued for ID.");
        Check(list.Items.Any(i => ReferenceEquals(i.Item, generic)), "List entries must retain their immutable observation.");
        list.Sort(ItemList.SortType.CharacterAscending);
        Check(list.Items[0].Character == "Mule A", "Character sort failed.");
        MethodInfo exportRow = typeof(ItemExport).GetMethod("Row", BindingFlags.NonPublic | BindingFlags.Static);
        var exported = (string[])exportRow.Invoke(null, new object[] { list.Items[0] });
        Check(exported[0] == "Mule A" && exported[1] == "Conquest" && exported[2] == "Other Dagger", "Saved export used the live character or world filter.");
        var incompleteExport = (string[])exportRow.Invoke(null, new object[] { new ItemListRow(new Item("Conquest", "Mule B", -42, "Unreadable", ObjectClass.MeleeWeapon)) });
        Check(incompleteExport[0] == "Mule B" && incompleteExport[2] == "Unreadable", "Incomplete saved export used a live object with the same ID.");
        var filter = new ItemFilter { Text = "Mule B", Weapons = true };
        Check(list.Items.Count(filter.Matches) == 1, "Character search failed.");
        filter.Armor = true;
        filter.Weapons = false;
        Check(!list.Items.Any(filter.Matches), "Character search bypassed the category filter.");
        var misleadingOwner = new ItemListRow(new Item("Conquest", "Legendary Legendary", 1, "Legendary Dagger", ObjectClass.MeleeWeapon));
        Check(!new ItemFilter { Doubles = true }.Matches(misleadingOwner), "Character names must not count as item cantrips.");

        var iron = VGInventory.DecodeItem("Conquest", "Mule A", 7, "Test Dagger", ObjectClass.MeleeWeapon, Fixture(material: 61));
        var ironInfo = new ItemListRow(iron);
        ironInfo.Populate();
        Check(iron.Name == "Test Dagger" && ironInfo.DisplayName == "Iron Test Dagger", "Formatting changed the raw name or lost its material.");
        ironInfo.Populate();
        Check(ironInfo.DisplayName == "Iron Test Dagger", "Repeated population duplicated the material prefix.");
        Check(new ItemFilter { Text = "Iron" }.Matches(ironInfo), "Search stopped matching the displayed material.");
        var named = new ItemList();
        named.Load(new[] { VGInventory.DecodeItem("Conquest", "Mule A", 8, "Jade Dagger", ObjectClass.MeleeWeapon, Fixture()), iron });
        Check(ReferenceEquals(named.Items[0].Item, iron), "Name sorting must use the material-prefixed display name.");

        ItemCache.Init();
        ItemCache.Store(ironInfo);
        var cached = ItemCache.Get(iron.Id, iron.Name);
        Check(cached != null && !ReferenceEquals(cached, ironInfo) && cached.DisplayName == ironInfo.DisplayName, "Cache must return an independent item with its display intact.");
        cached.PopulateStub();
        Check(!cached.IsComplete && cached.SummaryCol3 == "" && cached.SortCol3OD == 0, "Stub population retained old appraisal details.");
        Check(ironInfo.IsComplete && ItemCache.Get(iron.Id, iron.Name).IsComplete, "Updating a cached copy changed the original or cache.");
        Check(ItemCache.Get(iron.Id, "Different item") == null, "Cache accepted a recycled object ID.");
        ItemCache.Clear();

        var missing = new VGInventory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Check(!missing.Refresh("Conquest") && missing.List.Items.Count == 0, "Missing database must not create an inventory.");
        Check(!missing.Refresh("../Conquest"), "Server names must not escape the inventory directory.");
        AssertMissingSQLite();

        var layout = new XmlDocument();
        using (Stream xml = typeof(Item).Assembly.GetManifestResourceStream("OracleOfDereth.mainView.xml")) layout.Load(xml);
        var tabs = layout.SelectNodes("//control[@name='ServerViewNotebook']/page");
        Check(tabs.Cast<XmlNode>().Select(tab => tab.Attributes["label"].Value).SequenceEqual(
            new[] { "Augs", "Bank", "Experience", "Fship", "Inventory", "Quests", "Top" }),
            "Server tabs are not in alphabetical order.");
        Check(layout.SelectNodes("//control[starts-with(@name,'VGInventoryFilter') and @progid='DecalControls.CheckBox']").Count == 10, "Inventory filters differ from Items.");
        Check(layout.SelectSingleNode("//control[@name='VGInventoryList']/column[1]").Attributes["name"].Value == "Character", "First column must be Character.");
    }

    private static void AssertMissingSQLite()
    {
        Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "System.Data.SQLite"),
            "Missing SQLite test must run before loading the audit provider.");
        string temporary = Path.Combine(Path.GetTempPath(), "Oracle-missing-sqlite-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            // Reach the provider loader without needing a real database or changing the installation.
            File.WriteAllBytes(Path.Combine(temporary, "_Conquest.db"), new byte[0]);
            var inventory = new VGInventory(temporary);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Check(!inventory.Refresh("Conquest"), "Missing SQLite unexpectedly succeeded.");
                Check(inventory.Error == "VGI: Virindi Global Inventory's SQLite component is missing or could not load. Reinstall the VGI decal plugin",
                    "Missing SQLite did not show the installation guidance.");
                Check(!inventory.IsSearching && inventory.List.Items.Count == 0 && inventory.List.QueueCount == 0,
                    "Missing SQLite left a running search, results, or live identification requests.");
            }
        }
        finally { Directory.Delete(temporary, true); }
    }

    private static void Reject(byte[] bytes)
    {
        try { VGInventory.DecodeItem("Conquest", "Mule", 1, "Broken", ObjectClass.MeleeWeapon, bytes); }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed VGI blob was accepted.");
    }

    private static void AssertCloakEffectSubfilters()
    {
        var cases = new[] {
            (new ItemFilter { Cloaks = true, CloakProcDamage200 = true }, new[] { "-200 Damage" }),
            (new ItemFilter { Cloaks = true, CloakProcCiS = true }, new[] { "CiS" }),
            (new ItemFilter { Cloaks = true, CloakProcMelee = true }, new[] { "Melee Shroud" }),
            (new ItemFilter { Cloaks = true, CloakProcMissile = true }, new[] { "Missile Shroud" }),
            (new ItemFilter { Cloaks = true, CloakProcMagic = true }, new[] { "Magic Shroud" }),
            (new ItemFilter { Cloaks = true, CloakProcAoE = true }, new[] { "Blade Ring", "Bludgeon Ring", "Piercing Ring", "Acid Ring",
                "Fire Ring", "Frost Ring", "Lightning Ring", "Void Ring", "Melee Ring", "Magic Ring" }),
            (new ItemFilter { Cloaks = true, CloakProcOther = true }, new[] { "Shroud", "Unrecognized effect", "" })
        };
        var row = new ItemListRow(new Item("Conquest", "Mule", 1, "Cloak", ObjectClass.Clothing,
            new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = 0x8000000, [218103849] = 27704 }));
        row.PopulateStub();
        var summary = typeof(ItemListRow).GetProperty("SummaryCol2");
        for (int i = 0; i < cases.Length; i++)
            foreach (var effect in cases[i].Item2)
            {
                summary.SetValue(row, effect);
                for (int j = 0; j < cases.Length; j++)
                    Check(cases[j].Item1.Matches(row) == (i == j), "Cloak Effect filter mismatch: " + effect);
            }
        summary.SetValue(row, "CiS");
        Check(new ItemFilter { Cloaks = true, CloakLevel5 = true, CloakProcCiS = true }.Matches(row), "Level and Effect must combine.");
        Check(!new ItemFilter { Cloaks = true, CloakLevelOther = true, CloakProcCiS = true }.Matches(row), "Level Other matched level 5.");
        Check(!new ItemFilter { Cloaks = true, CloakLevel5 = true, CloakProcOther = true }.Matches(row), "Effect Other matched CiS.");
        Check(new ItemFilter { Cloaks = true, CloakProcCiS = true, CloakProcAoE = true }.Matches(row), "Effect choices must combine as alternatives.");
        Check(new ItemFilter { CloakProcOther = true }.Matches(row), "Inactive effect filter narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 2, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Cloaks = true, Weapons = true, CloakProcCiS = true }.Matches(weapon), "Cloak effects hid another category.");
    }

    private static void AssertSharedSubfilterBindings()
    {
        // Exercise the shared UI bindings without constructing a Decal window.
        var type = typeof(ItemFilter).Assembly.GetType("OracleOfDereth.ItemSubfilters", true);
        var definitions = (Array)type.GetField("Definitions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var mainFields = new HashSet<string> { "Weapons", "Armor", "Clothing", "Jewelry", "Cloaks",
            "Summons", "Aetheria", "Salvage", "Other", "Doubles" };
        var booleanFields = typeof(ItemFilter).GetFields().Where(field => field.FieldType == typeof(bool)).ToList();
        var boundFields = new HashSet<string>();
        var boundSlots = ItemInfo.ArmorSlot.None;
        foreach (var definition in definitions)
        {
            var apply = (Action<ItemFilter, bool>)definition.GetType().GetField("Apply").GetValue(definition);
            var filter = new ItemFilter();
            apply(filter, true);
            var selected = booleanFields.Where(field => (bool)field.GetValue(filter)).ToList();
            Check(selected.Count + (filter.ArmorSlots == ItemInfo.ArmorSlot.None ? 0 : 1) == 1,
                "A subfilter must change exactly one selection.");
            foreach (var field in selected)
                Check(!mainFields.Contains(field.Name) && boundFields.Add(field.Name), "Duplicate or main-category subfilter binding.");
            Check((boundSlots & filter.ArmorSlots) == 0, "Duplicate armor slot binding.");
            boundSlots |= filter.ArmorSlots;
            if (selected.Count != 0)
            {
                apply(filter, false);
                Check(selected.All(field => !(bool)field.GetValue(filter)), "Unchecked subfilter retained its selection.");
            }
        }
        Check(boundFields.SetEquals(booleanFields.Where(field => !mainFields.Contains(field.Name)).Select(field => field.Name)),
            "The shared controls omitted a subfilter.");
        var allSlots = ItemInfo.ArmorSlot.Head | ItemInfo.ArmorSlot.Chest | ItemInfo.ArmorSlot.Abdomen |
            ItemInfo.ArmorSlot.UpperArms | ItemInfo.ArmorSlot.LowerArms | ItemInfo.ArmorSlot.Hands |
            ItemInfo.ArmorSlot.UpperLegs | ItemInfo.ArmorSlot.LowerLegs | ItemInfo.ArmorSlot.Feet;
        Check(boundSlots == allSlots, "The shared controls omitted an armor slot.");
    }

    private static void AssertSalvageSubfilters()
    {
        var groups = new Dictionary<string, int[]> {
            ["Iron"] = new[] { 61 }, ["Granite"] = new[] { 67 }, ["Mahogany"] = new[] { 74 },
            ["GreenGarnet"] = new[] { 23 }, ["Velvet"] = new[] { 7 }, ["Brass"] = new[] { 57 },
            ["Steel"] = new[] { 64 },
            ["Rends"] = new[] { 35, 27, 26, 21, 15, 13, 47, 41, 32 },
            ["Imbues"] = new[] { 49, 50, 34, 25, 22, 16, 20, 38, 54, 62 }
        };
        // Cover every known material, missing material, and an unknown future value.
        var rows = Enumerable.Range(0, 78).Concat(new[] { 999 }).Select(material => {
            var row = new ItemListRow(new Item("Conquest", "Mule", material, "Salvage", ObjectClass.Salvage,
                new Dictionary<int, int> { [(int)LongValueKey.Material] = material }));
            row.PopulateStub();
            return row;
        }).ToList();
        foreach (var group in groups)
        {
            var filter = new ItemFilter { Salvage = true };
            typeof(ItemFilter).GetField("Salvage" + group.Key).SetValue(filter, true);
            Check(rows.Where(filter.Matches).SequenceEqual(rows.Where(row => group.Value.Contains(row.Item.Values(LongValueKey.Material)))),
                "Salvage material group mismatch: " + group.Key);
        }
        var named = new HashSet<int>(groups.Values.SelectMany(ids => ids));
        Check(rows.Where(new ItemFilter { Salvage = true, SalvageOther = true }.Matches).SequenceEqual(
            rows.Where(row => !named.Contains(row.Item.Values(LongValueKey.Material)))),
            "Other salvage must exclude all named groups.");
        Check(rows.Count(new ItemFilter { Salvage = true, SalvageIron = true, SalvageRends = true }.Matches) == 10,
            "Salvage selections must combine as alternatives.");
        Check(rows.All(new ItemFilter { Salvage = true }.Matches) && rows.All(new ItemFilter { SalvageIron = true }.Matches),
            "Empty or inactive Salvage subfilters narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 100, "Dagger", ObjectClass.MeleeWeapon,
            new Dictionary<int, int> { [(int)LongValueKey.Material] = 61 }));
        weapon.PopulateStub();
        Check(new ItemFilter { Salvage = true, Weapons = true, SalvageImbues = true }.Matches(weapon),
            "Salvage filters hid another selected category.");
    }

    private static void AssertOtherClassSubfilters()
    {
        var classes = new[] { ObjectClass.BaseAlchemy, ObjectClass.CraftedAlchemy, ObjectClass.SpellComponent,
            ObjectClass.BaseCooking, ObjectClass.CraftedCooking, ObjectClass.Food, ObjectClass.Gem,
            ObjectClass.HealingKit, ObjectClass.Key, ObjectClass.Lockpick, ObjectClass.ManaStone,
            ObjectClass.Misc, ObjectClass.Gem, ObjectClass.Book, ObjectClass.Unknown };
        var expected = new[] { "Alchemy", "Alchemy", "Component", "Cooking", "Cooking", "Food", "Gem",
            "HealingKit", "Key", "Lockpick", "ManaStone", "Misc", "Rare", "Other", "Other" };
        var rows = classes.Select((objectClass, i) => {
            var row = new ItemListRow(new Item("Conquest", "Mule", i, "Test", objectClass,
                new Dictionary<int, int> { [218103850] = expected[i] == "Rare" ? 23308 : 0 }));
            row.PopulateStub();
            Check(row.SortCategory == ItemCategory.Other, "Object class fixture must belong to Others.");
            return row;
        }).ToList();
        foreach (string name in expected.Distinct())
        {
            var filter = new ItemFilter { Other = true };
            typeof(ItemFilter).GetField("OtherClass" + name).SetValue(filter, true);
            Check(rows.Where(filter.Matches).SequenceEqual(rows.Where((row, i) => expected[i] == name)),
                "Others object class filter mismatch: " + name);
        }
        Check(rows.Count(new ItemFilter { Other = true, OtherClassGem = true, OtherClassRare = true }.Matches) == 2,
            "Others object class selections must combine as alternatives.");
        Check(rows.All(new ItemFilter { Other = true }.Matches) && rows.All(new ItemFilter { OtherClassGem = true }.Matches),
            "Empty or inactive Others subfilters narrowed results.");
        var aetheria = new ItemListRow(new Item("Conquest", "Mule", 99, "Aetheria", ObjectClass.Gem,
            new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = 0x10000000 }));
        aetheria.PopulateStub();
        Check(new ItemFilter { Other = true, Aetheria = true, OtherClassFood = true }.Matches(aetheria),
            "Others subfilters hid another selected category sharing the Gem object class.");
    }

    private static void AssertAetheriaSubfilters()
    {
        var levelRows = Enumerable.Range(0, 6).Select(level => {
            var row = new ItemListRow(new Item("Conquest", "Mule", level, "Aetheria", ObjectClass.Gem,
                new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = 0x10000000,
                    [218103849] = level == 0 ? 0 : 27699 + level, [265] = 35 }));
            row.PopulateStub();
            typeof(ItemListRow).GetProperty("AetheriaSurge").SetValue(row, "Protection");
            return row;
        }).ToList();
        for (int level = 1; level <= 5; level++)
        {
            var filter = new ItemFilter { Aetheria = true };
            typeof(ItemFilter).GetField("AetheriaLevel" + level).SetValue(filter, true);
            Check(levelRows.Where(filter.Matches).SequenceEqual(new[] { levelRows[level] }), "Aetheria level filter mismatch.");
        }
        var combined = new ItemFilter { Aetheria = true, AetheriaLevel1 = true, AetheriaLevel5 = true,
            AetheriaColorBlue = true, AetheriaSigilDefense = true, AetheriaSurgeProtection = true };
        Check(levelRows.Count(combined.Matches) == 2, "Aetheria levels must combine as alternatives with all other groups.");
        combined.AetheriaColorBlue = false;
        combined.AetheriaColorRed = true;
        Check(!levelRows.Any(combined.Matches), "Level selection bypassed the color filter.");
        Check(levelRows.All(new ItemFilter { Aetheria = true }.Matches) &&
            levelRows.All(new ItemFilter { AetheriaLevel1 = true }.Matches), "Empty or inactive Aetheria levels narrowed results.");
        var colorNames = new[] { "Blue", "Yellow", "Red" };
        var slots = new[] { 0x10000000, 0x20000000, 0x40000000 };
        var sigils = new[] { "Defense", "Destruction", "Fury", "Growth", "Vigor" };
        var surges = new[] { "Affliction", "Destruction", "Festering", "Protection", "Regeneration" };
        var rows = new List<ItemListRow>();
        for (int c = 0; c < slots.Length; c++)
            for (int g = 0; g < sigils.Length; g++)
                foreach (string surge in surges)
                {
                    var row = new ItemListRow(new Item("Conquest", "Mule", rows.Count, "Aetheria", ObjectClass.Gem,
                        new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = slots[c], [265] = 35 + g }));
                    row.PopulateStub();
                    Check(new ItemInfo(row.Item).GetAetheriaColor() == colorNames[c], "Aetheria color must follow the slot.");
                    Check(new ItemInfo(row.Item).GetSetName() == sigils[g], "Aetheria sigil mapping differs from ItemInfo.");
                    // Supply the resolved surge cached by Populate, independently of the sigil.
                    typeof(ItemListRow).GetProperty("AetheriaSurge").SetValue(row, surge);
                    rows.Add(row);
                }
        foreach (string color in colorNames)
        {
            var filter = new ItemFilter { Aetheria = true };
            typeof(ItemFilter).GetField("AetheriaColor" + color).SetValue(filter, true);
            Check(rows.Count(filter.Matches) == 25, "Aetheria color filter failed.");
        }
        foreach (string sigil in sigils)
        {
            var filter = new ItemFilter { Aetheria = true };
            typeof(ItemFilter).GetField("AetheriaSigil" + sigil).SetValue(filter, true);
            Check(rows.Count(filter.Matches) == 15, "Aetheria sigil filter failed.");
        }
        foreach (string surge in surges)
        {
            var filter = new ItemFilter { Aetheria = true };
            typeof(ItemFilter).GetField("AetheriaSurge" + surge).SetValue(filter, true);
            Check(rows.Count(filter.Matches) == 15, "Aetheria surge filter failed.");
        }
        var exact = new ItemFilter { Aetheria = true, AetheriaColorBlue = true,
            AetheriaSigilDefense = true, AetheriaSurgeDestruction = true };
        Check(rows.Count(exact.Matches) == 1, "Aetheria groups must combine with AND.");
        exact.AetheriaColorRed = true;
        Check(rows.Count(exact.Matches) == 2, "Selections inside a group must combine as alternatives.");
        Check(rows.Count(new ItemFilter { Aetheria = true, AetheriaSigilDestruction = true, AetheriaSurgeAffliction = true }.Matches) == 3,
            "Destruction sigil was confused with the surge.");
        Check(rows.All(new ItemFilter { Aetheria = true }.Matches) && rows.All(new ItemFilter { AetheriaColorBlue = true }.Matches),
            "Empty or inactive Aetheria subfilters narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 1, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Aetheria = true, Weapons = true, AetheriaColorRed = true }.Matches(weapon),
            "Aetheria filters hid another selected category.");
        rows[0].PopulateStub();
        Check(rows[0].AetheriaSurge == "", "Stub retained a cached surge.");
    }

    private static void AssertSummonSubfilters()
    {
        var names = new[] { "Moar Essence", "Zombie Essence", "Fire Elemental Essence", "Unknown Essence" };
        var filters = new[] {
            new ItemFilter { Summons = true, SummonNaturalist = true },
            new ItemFilter { Summons = true, SummonNecromancer = true },
            new ItemFilter { Summons = true, SummonPrimalist = true },
            new ItemFilter { Summons = true, SummonOther = true }
        };
        var rows = names.Select(name => {
            var row = new ItemListRow(new Item("Conquest", "Mule", 1, name, ObjectClass.Misc,
                new Dictionary<int, int> { [(int)LongValueKey.UsesTotal] = 50 }));
            row.PopulateStub(); return row;
        }).ToList();
        for (int i = 0; i < filters.Length; i++)
            Check(rows.Where(filters[i].Matches).SequenceEqual(new[] { rows[i] }), "Summon specialization filter mismatch.");
        Check(rows.Count(new ItemFilter { Summons = true, SummonNaturalist = true, SummonOther = true }.Matches) == 2,
            "Summon specializations must combine as alternatives.");
        Check(rows.All(new ItemFilter { Summons = true }.Matches) && rows.All(new ItemFilter { SummonPrimalist = true }.Matches),
            "Empty or inactive summon subfilters narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 9, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Summons = true, Weapons = true, SummonOther = true }.Matches(weapon),
            "Summon subfilters hid another selected category.");
    }

    private static void AssertCloakSubfilters()
    {
        var filters = new[] {
            new ItemFilter { Cloaks = true, CloakLevel1 = true },
            new ItemFilter { Cloaks = true, CloakLevel2 = true },
            new ItemFilter { Cloaks = true, CloakLevel3 = true },
            new ItemFilter { Cloaks = true, CloakLevel4 = true },
            new ItemFilter { Cloaks = true, CloakLevel5 = true }
        };
        var rows = Enumerable.Range(0, 6).Select(level => {
            var row = new ItemListRow(new Item("Conquest", "Mule", level, "Cloak", ObjectClass.Clothing,
                new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = 0x8000000,
                    [218103849] = level == 0 ? 0 : 27699 + level }));
            row.PopulateStub(); return row;
        }).ToList();
        for (int i = 0; i < filters.Length; i++)
            Check(rows.Where(filters[i].Matches).SequenceEqual(new[] { rows[i + 1] }), "Cloak level filter mismatch.");
        Check(rows.Where(new ItemFilter { Cloaks = true, CloakLevelOther = true }.Matches).SequenceEqual(new[] { rows[0] }),
            "Other cloak levels must exclude levels 1 through 5.");
        Check(rows.Count(new ItemFilter { Cloaks = true, CloakLevel1 = true, CloakLevel5 = true }.Matches) == 2,
            "Cloak levels must combine as alternatives.");
        Check(rows.All(new ItemFilter { Cloaks = true }.Matches) && rows.All(new ItemFilter { CloakLevel1 = true }.Matches),
            "Empty or inactive cloak subfilters narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 9, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Cloaks = true, Weapons = true, CloakLevel5 = true }.Matches(weapon),
            "Cloak subfilters hid another selected category.");
    }

    private static void AssertJewelrySubfilters()
    {
        ItemListRow Jewel(int slots, string name = "Unspecified")
        {
            var row = new ItemListRow(new Item("Conquest", "Mule", slots, name, ObjectClass.Jewelry,
                new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = slots }));
            row.PopulateStub(); return row;
        }
        var filters = new[] {
            new ItemFilter { Jewelry = true, JewelryNecklace = true },
            new ItemFilter { Jewelry = true, JewelryTrinket = true },
            new ItemFilter { Jewelry = true, JewelryBracelet = true },
            new ItemFilter { Jewelry = true, JewelryRing = true }
        };
        var masks = new[] { new[] { 0x8000 }, new[] { 0x4000000 },
            new[] { 0x10000, 0x20000, 0x30000 }, new[] { 0x40000, 0x80000, 0xc0000 } };
        for (int i = 0; i < masks.Length; i++)
            foreach (int mask in masks[i])
                for (int j = 0; j < filters.Length; j++)
                    Check(filters[j].Matches(Jewel(mask)) == (i == j), "Jewelry filter mismatch for slot " + mask);
        Check(filters[2].Matches(Jewel(0, "Bracelet")) && filters[3].Matches(Jewel(0, "Signet Ring")),
            "Jewelry filters must retain ItemInfo's pre-identification name fallback.");
        Check(filters[0].Matches(Jewel(0x8000, "Ring")), "Known slot must take precedence over item name.");
        var all = masks.SelectMany(m => m).Select(m => Jewel(m)).ToList();
        Check(all.All(new ItemFilter { Jewelry = true }.Matches) && all.All(new ItemFilter { JewelryRing = true }.Matches),
            "Empty or inactive jewelry subfilters must not narrow results.");
        Check(all.Count(new ItemFilter { Jewelry = true, JewelryRing = true, JewelryBracelet = true }.Matches) == 6,
            "Jewelry selections must combine as alternatives.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 1, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Jewelry = true, Weapons = true, JewelryRing = true }.Matches(weapon),
            "Jewelry subfilters hid another selected category.");
    }

    private static void AssertClothingSubfilters()
    {
        ItemListRow Garment(int slots, int coverage = 0)
        {
            var row = new ItemListRow(new Item("Conquest", "Mule", slots, "Garment", ObjectClass.Clothing,
                new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = slots,
                    [(int)LongValueKey.Coverage] = coverage }, hasIdData: true));
            row.PopulateStub(); return row;
        }
        var fullShirt = Garment(0x1a);
        var partialShirt = Garment(0x0a);
        var fullPants = Garment(0xc4);
        var partialPants = Garment(0xc0);
        var rows = new[] { fullShirt, partialShirt, fullPants, partialPants };
        Check(rows.All(r => r.SortCategory == ItemCategory.Clothing), "Clothing fixture was not classified as underclothing.");
        Check(rows.Where(new ItemFilter { Clothing = true, ClothingShirt = true }.Matches).SequenceEqual(rows.Take(2)),
            "Shirt filter must match the garment slot, not the item name.");
        Check(rows.Where(new ItemFilter { Clothing = true, ClothingPants = true }.Matches).SequenceEqual(rows.Skip(2)),
            "Pants filter must match the garment slot.");
        Check(rows.Where(new ItemFilter { Clothing = true, ClothingFullCoverage = true }.Matches).SequenceEqual(new[] { fullShirt, fullPants }),
            "Full coverage must require all three appropriate locations.");
        Check(rows.Where(new ItemFilter { Clothing = true, ClothingPartialCoverage = true }.Matches).SequenceEqual(new[] { partialShirt, partialPants }),
            "Partial coverage included a full garment.");
        Check(rows.Count(new ItemFilter { Clothing = true, ClothingPants = true, ClothingFullCoverage = true }.Matches) == 1,
            "Garment and coverage filters must combine with AND.");
        var full = new ItemFilter { Clothing = true, ClothingFullCoverage = true };
        Check(full.Matches(Garment(0x02, 0x68)) && full.Matches(Garment(0x40, 0x16)),
            "Coverage fallback failed for full shirts or pants.");
        Check(!full.Matches(Garment(0x0e)), "Any three slots must not count as full shirt coverage.");
        Check(rows.All(new ItemFilter { Clothing = true, ClothingShirt = true, ClothingPants = true,
            ClothingFullCoverage = true, ClothingPartialCoverage = true }.Matches), "Selecting all clothing options excluded garments.");
        Check(rows.All(new ItemFilter { Clothing = true }.Matches) && rows.All(new ItemFilter { ClothingPants = true, ClothingFullCoverage = true }.Matches),
            "Empty or inactive clothing subfilters narrowed results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 1, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Clothing = true, Weapons = true, ClothingShirt = true, ClothingFullCoverage = true }.Matches(weapon),
            "Clothing subfilters affected another category.");
    }

    private static void AssertArmorSetSubfilters()
    {
        var ids = new[] { 14, 16, 20, 19, 21, 0, 9999 };
        var filters = new[] {
            new ItemFilter { Armor = true, ArmorSetAdept = true },
            new ItemFilter { Armor = true, ArmorSetDefender = true },
            new ItemFilter { Armor = true, ArmorSetDexterous = true },
            new ItemFilter { Armor = true, ArmorSetHearty = true },
            new ItemFilter { Armor = true, ArmorSetWise = true },
            new ItemFilter { Armor = true, ArmorSetNoSet = true },
            new ItemFilter { Armor = true, ArmorSetOther = true }
        };
        var rows = ids.Select(id => {
            var row = new ItemListRow(new Item("Conquest", "Mule", id, "Armor", ObjectClass.Armor,
                new Dictionary<int, int> { [265] = id, [(int)LongValueKey.EquipableSlots] = 1 }, hasIdData: true));
            row.PopulateStub(); return row;
        }).ToList();
        for (int i = 0; i < filters.Length; i++)
            Check(rows.Where(filters[i].Matches).SequenceEqual(new[] { rows[i] }), "Armor set filter mismatch: " + ids[i]);
        var absent = new ItemListRow(new Item("Conquest", "Mule", 1, "Armor", ObjectClass.Armor, hasIdData: true));
        absent.PopulateStub();
        Check(filters[5].Matches(absent) && !filters[6].Matches(absent), "Absent set on appraised armor must be No Set.");
        var dedication = new ItemListRow(new Item("Conquest", "Mule", 30, "Armor", ObjectClass.Armor,
            new Dictionary<int, int> { [265] = 30 }, hasIdData: true));
        dedication.PopulateStub();
        Check(filters[6].Matches(dedication) && !filters[5].Matches(dedication), "Dedication must now match Other, not No Set.");
        var unknown = new ItemListRow(new Item("Conquest", "Mule", 2, "Unreadable armor", ObjectClass.Armor));
        unknown.PopulateStub();
        Check(!filters.Any(f => f.Matches(unknown)), "Unreadable armor must not invent a set or No Set.");
        Check(rows.Count(new ItemFilter { Armor = true, ArmorSetAdept = true, ArmorSetOther = true }.Matches) == 2,
            "Armor set choices must combine as alternatives.");
        Check(!new ItemFilter { Armor = true, ArmorSetAdept = true, ArmorSlots = ItemInfo.ArmorSlot.Feet }.Matches(rows[0]),
            "Armor set filter bypassed slot selection.");
        Check(rows.All(new ItemFilter { Armor = true }.Matches) && rows.All(new ItemFilter { ArmorSetAdept = true }.Matches),
            "Inactive set filters must not narrow results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 3, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Armor = true, Weapons = true, ArmorSetAdept = true }.Matches(weapon),
            "Armor set filter hid another selected category.");
    }

    private static void AssertArmorSubfilters()
    {
        var slots = new[] { ItemInfo.ArmorSlot.Head, ItemInfo.ArmorSlot.Chest, ItemInfo.ArmorSlot.Abdomen,
            ItemInfo.ArmorSlot.UpperArms, ItemInfo.ArmorSlot.LowerArms, ItemInfo.ArmorSlot.Hands,
            ItemInfo.ArmorSlot.UpperLegs, ItemInfo.ArmorSlot.LowerLegs, ItemInfo.ArmorSlot.Feet };
        var equip = new[] { 1, 0x200, 0x400, 0x800, 0x1000, 0x20, 0x2000, 0x4000, 0x100 };
        var coverage = new[] { 0x4000, 8, 16, 32, 64, 0x8000, 2, 4, 0x10000 };
        ItemListRow ArmorRow(int mask, int cover = 0)
        {
            var row = new ItemListRow(new Item("Conquest", "Mule", 1, "Armor", ObjectClass.Armor,
                new Dictionary<int, int> { [(int)LongValueKey.EquipableSlots] = mask, [(int)LongValueKey.Coverage] = cover }));
            row.PopulateStub(); return row;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            foreach (var row in new[] { ArmorRow(equip[i]), ArmorRow(0, coverage[i]) })
            {
                Check(new ItemInfo(row.Item).GetArmorSlots() == slots[i], "Armor slot/coverage mapping differs for " + slots[i]);
                for (int j = 0; j < slots.Length; j++)
                    Check(new ItemFilter { Armor = true, ArmorSlots = slots[j] }.Matches(row) == (i == j), "Armor filter matched wrong slot.");
            }
        }
        var coat = ArmorRow(0x200 | 0x400 | 0x800);
        foreach (var slot in new[] { ItemInfo.ArmorSlot.Chest, ItemInfo.ArmorSlot.Abdomen, ItemInfo.ArmorSlot.UpperArms })
            Check(new ItemFilter { Armor = true, ArmorSlots = slot }.Matches(coat), "Multi-slot armor lost a covered slot.");
        Check(new ItemFilter { Armor = true, ArmorSlots = ItemInfo.ArmorSlot.Feet | ItemInfo.ArmorSlot.Chest }.Matches(coat),
            "Armor slots must combine as alternatives.");
        Check(!new ItemFilter { Armor = true, ArmorSlots = ItemInfo.ArmorSlot.Feet }.Matches(coat), "Coat matched Feet.");
        Check(new ItemFilter { Armor = true }.Matches(coat) && new ItemFilter { ArmorSlots = ItemInfo.ArmorSlot.Feet }.Matches(coat),
            "Empty or inactive armor subfilters must not narrow results.");
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 2, "Dagger", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        Check(new ItemFilter { Armor = true, Weapons = true, ArmorSlots = ItemInfo.ArmorSlot.Head }.Matches(weapon),
            "Armor slots hid another selected category.");
    }

    private static void AssertElementSubfilters()
    {
        var cases = new[] {
            (new ItemFilter { Weapons = true, ElementSlash = true }, new[] { "Slash" }),
            (new ItemFilter { Weapons = true, ElementPierce = true }, new[] { "Pierce" }),
            (new ItemFilter { Weapons = true, ElementBludge = true }, new[] { "Bludge", "Bludgeon" }),
            (new ItemFilter { Weapons = true, ElementFire = true }, new[] { "Fire", "Flame" }),
            (new ItemFilter { Weapons = true, ElementFrost = true }, new[] { "Frost", "Cold" }),
            (new ItemFilter { Weapons = true, ElementStorm = true }, new[] { "Storm", "Lightning" }),
            (new ItemFilter { Weapons = true, ElementAcid = true }, new[] { "Acid" }),
            (new ItemFilter { Weapons = true, ElementNether = true }, new[] { "Void", "Nether" })
        };
        var row = new ItemListRow(new Item("Conquest", "Fire Mule", 1, "Flame Dagger", ObjectClass.MeleeWeapon,
            new Dictionary<int, int> { [159] = 44 }));
        row.PopulateStub();
        // Supply representative display text: the filter's contract is the Type column.
        PropertyInfo summary = typeof(ItemListRow).GetProperty("SummaryCol1");
        for (int i = 0; i < cases.Length; i++)
            foreach (string name in cases[i].Item2)
            {
                summary.SetValue(row, "Heavy " + name.ToLowerInvariant());
                for (int j = 0; j < cases.Length; j++)
                    Check(cases[j].Item1.Matches(row) == (i == j), "Element alias mismatch: " + name);
            }
        summary.SetValue(row, "Heavy Cold");
        Check(new ItemFilter { Weapons = true, WeaponHW = true, ElementFire = true, ElementFrost = true }.Matches(row),
            "Element choices must combine as alternatives.");
        Check(!new ItemFilter { Weapons = true, WeaponFW = true, ElementFrost = true }.Matches(row),
            "Element filter bypassed weapon-type selection.");
        Check(new ItemFilter { ElementFire = true }.Matches(row), "Disabled parent left an element filter active.");
        Check(new ItemFilter { Weapons = true }.Matches(row), "No element selection must allow all elements.");
        var armor = new ItemListRow(new Item("Conquest", "Mule", 2, "Armor", ObjectClass.Armor));
        armor.PopulateStub();
        Check(new ItemFilter { Weapons = true, Armor = true, ElementFire = true }.Matches(armor),
            "Weapon elements hid another selected category.");
    }

    private static void AssertAmmunitionCategory()
    {
        foreach (string name in new[] { "Burning Sands Arrow", "Quarrel", "Atlatl Dart" })
        {
            var row = new ItemListRow(new Item("Conquest", "Mule", 1, name, ObjectClass.MissileWeapon,
                new Dictionary<int, int> { [(int)LongValueKey.StackMax] = 250 }));
            row.PopulateStub();
            Check(!new ItemFilter { Weapons = true }.Matches(row), "Weapons included ammunition: " + name);
            Check(new ItemFilter { Other = true }.Matches(row) && new ItemFilter().Matches(row),
                "Ammunition must remain available under Others and in the full list.");
        }
        foreach (string name in new[] { "Longbow", "Crossbow", "Atlatl" })
        {
            var row = new ItemListRow(new Item("Conquest", "Mule", 2, name, ObjectClass.MissileWeapon));
            row.PopulateStub();
            Check(new ItemFilter { Weapons = true }.Matches(row), "Weapons excluded a missile launcher: " + name);
        }
    }

    private static void AssertWeaponDoubles()
    {
        var weapon = new ItemListRow(new Item("Conquest", "Mule", 1, "Legendary Legendary", ObjectClass.MeleeWeapon));
        weapon.PopulateStub();
        var filter = new ItemFilter { Doubles = true };
        var cases = new Dictionary<string, bool> {
            ["Legendary Blood Thirst, Legendary Defender"] = true,
            ["Epic Heart Seeker, Epic Spirit Drinker"] = true,
            ["Epic Blood Thirst, Epic Defender, Legendary Swift Hunter"] = true,
            ["Legendary Blood Thirst, Legendary Swift Hunter"] = false,
            ["Epic Blood Thirst, Epic Swift Hunter"] = false,
            ["Legendary Blood Thirst, Epic Defender, Epic Heart Seeker"] = false,
            ["Major Blood Thirst, Major Defender"] = false,
            ["Legendary Swift Hunter, Legendary Coordination"] = false,
            [""] = false
        };
        foreach (var test in cases)
        {
            typeof(ItemListRow).GetProperty("SummaryCol4").SetValue(weapon, test.Key);
            Check(filter.Matches(weapon) == test.Value, "Weapon doubles mismatch: " + test.Key);
        }
        var armor = new ItemListRow(new Item("Conquest", "Mule", 2, "Armor", ObjectClass.Armor));
        armor.PopulateStub();
        foreach (string tier in new[] { "Legendary", "Epic", "Major" })
        {
            typeof(ItemListRow).GetProperty("SummaryCol4").SetValue(armor, tier + " Coordination, " + tier + " Quickness");
            Check(filter.Matches(armor), "Nonweapon doubles behavior changed.");
        }
    }

    private static void AssertWeaponSubfilters()
    {
        var skills = new[] { 44, 46, 45, 41, 34, 43 };
        var filters = new[] {
            new ItemFilter { Weapons = true, WeaponHW = true },
            new ItemFilter { Weapons = true, WeaponFW = true },
            new ItemFilter { Weapons = true, WeaponLW = true },
            new ItemFilter { Weapons = true, Weapon2H = true },
            new ItemFilter { Weapons = true, WeaponWar = true },
            new ItemFilter { Weapons = true, WeaponVoid = true }
        };
        var rows = skills.Select(skill => {
            var item = new Item("Conquest", "Mule", skill, "Test Weapon", skill == 34 || skill == 43 ? ObjectClass.WandStaffOrb : ObjectClass.MeleeWeapon,
                new Dictionary<int, int> { [159] = skill, [(int)LongValueKey.WieldReqType] = 2, [(int)LongValueKey.WieldReqAttribute] = skill });
            var row = new ItemListRow(item); row.PopulateStub(); return row;
        }).ToList();
        for (int i = 0; i < filters.Length; i++)
            Check(rows.Where(filters[i].Matches).SequenceEqual(new[] { rows[i] }), "Weapon subfilter matched the wrong skill: " + skills[i]);
        Check(rows.All(new ItemFilter { Weapons = true }.Matches), "No selected weapon subfilters must allow every weapon.");
        Check(rows.Count(new ItemFilter { Weapons = true, WeaponHW = true, WeaponVoid = true }.Matches) == 2,
            "Weapon subfilters must combine as alternatives.");
        Check(rows.All(new ItemFilter { WeaponHW = true }.Matches), "Disabled parent left a weapon subfilter active.");
        var missileNames = new[] { "Atlatl", "Slingshot", "Longbow", "Crossbow", "Arbalest" };
        var missiles = missileNames.Select(name => {
            var row = new ItemListRow(new Item("Conquest", "Mule", 1, name, ObjectClass.MissileWeapon,
                new Dictionary<int, int> { [159] = 47 }));
            row.PopulateStub(); return row;
        }).ToList();
        Check(missiles.Where(new ItemFilter { Weapons = true, WeaponTW = true }.Matches).SequenceEqual(missiles.Take(2)),
            "TW must match the Thrown types shown in the Type column.");
        Check(missiles.Where(new ItemFilter { Weapons = true, WeaponBow = true }.Matches).SequenceEqual(missiles.Skip(2).Take(1)),
            "Bow must exclude thrown weapons and crossbows.");
        Check(missiles.Where(new ItemFilter { Weapons = true, WeaponXbow = true }.Matches).SequenceEqual(missiles.Skip(3)),
            "Xbow must match crossbows and arbalests.");
        Check(missiles.All(new ItemFilter { Weapons = true, WeaponTW = true, WeaponBow = true, WeaponXbow = true }.Matches),
            "Missile subfilters must combine as alternatives.");
        Check(!rows.Any(new ItemFilter { Weapons = true, WeaponTW = true, WeaponBow = true, WeaponXbow = true }.Matches),
            "Missile subfilters included melee weapons or casters.");
        var unclassified = new ItemListRow(new Item("Conquest", "Mule", 3, "Unclassified", ObjectClass.MeleeWeapon));
        unclassified.PopulateStub();
        var otherFilter = new ItemFilter { Weapons = true, WeaponOther = true };
        Check(otherFilter.Matches(unclassified) && !rows.Concat(missiles).Any(otherFilter.Matches),
            "Other must match only weapon types outside the named groups.");
        Check(rows.Concat(new[] { unclassified }).Count(new ItemFilter { Weapons = true, WeaponHW = true, WeaponOther = true }.Matches) == 2,
            "Other must combine with named groups as an alternative.");
        var armor = new ItemListRow(new Item("Conquest", "Mule", 1, "Armor", ObjectClass.Armor));
        armor.PopulateStub();
        Check(new ItemFilter { Weapons = true, WeaponHW = true, Armor = true }.Matches(armor),
            "Weapon subfilters hid another selected category.");
        Check(!new ItemFilter { Weapons = true, WeaponHW = true, Text = "NotPresent" }.Matches(rows[0]),
            "Weapon subfilter bypassed text filtering.");
    }

    private static void AssertObservations()
    {
        var zeroes = new Item("Conquest", "Mule", 1, "Dagger", ObjectClass.MeleeWeapon,
            new Dictionary<int, int> { [(int)LongValueKey.MaxDamage] = 0 },
            doubles: new Dictionary<int, double> { [(int)DoubleValueKey.AttackBonus] = 0 });
        Check(zeroes.TryGetValue(LongValueKey.MaxDamage, out int damage) && damage == 0,
            "An explicit zero damage must remain a present property.");
        Check(!zeroes.TryGetValue(LongValueKey.ElementalDmgBonus, out _),
            "Missing integer properties must remain distinguishable from zero.");
        Check(zeroes.TryGetValue(DoubleValueKey.AttackBonus, out double attack) && attack == 0,
            "An explicit zero attack bonus must remain a present property.");
        Check(!zeroes.TryGetValue(DoubleValueKey.MeleeDefenseBonus, out _),
            "Missing double properties must remain distinguishable from zero.");

        var integers = new Dictionary<int, int> { [218103842] = 56, [159] = 44, [353] = 6, [47] = 160 };
        var doubles = new Dictionary<int, double> { [167772171] = .43 };
        var strings = new Dictionary<int, string> { [16] = "Original description" };
        var booleans = new Dictionary<int, bool> { [1] = true };
        var spells = new List<int> { 6089 };
        var active = new List<int> { 1616 };
        var observation = new Item("Conquest", "Mule", 42, "Dagger", ObjectClass.MeleeWeapon,
            integers, strings, booleans, doubles, spells: spells, activeSpells: active, hasIdData: true);
        var info = new ItemInfo(observation);
        Check(info.GetODValue() == 8, "Captured active buff must be removed from OD exactly once.");
        integers[218103842] = 90;
        doubles[167772171] = .9;
        strings[16] = "Changed";
        booleans[1] = false;
        spells.Clear();
        active.Clear();
        Check(observation.Values(LongValueKey.MaxDamage) == 56 && Math.Abs(observation.Values(DoubleValueKey.Variance) - .43) < .0001,
            "Observation retained mutable numeric properties.");
        Check(observation.Values((StringValueKey)16) == "Original description" && observation.Values((BoolValueKey)1),
            "Observation retained mutable string/bool properties.");
        Check(observation.SpellCount == 1 && observation.ActiveSpellCount == 1 && info.GetODValue() == 8,
            "A later source change altered captured spells or an existing calculation.");

        var later = new Item("Conquest", "Mule", 42, "Dagger", ObjectClass.MeleeWeapon, integers, activeSpells: active, hasIdData: true);
        Check(later.Values(LongValueKey.MaxDamage) == 90 && later.HasActiveSpellData && later.ActiveSpellCount == 0,
            "New observation did not capture the later state or known empty buffs.");
        integers[10] = 1;
        var unknown = new Item("Conquest", "Mule", 42, "Dagger", ObjectClass.MeleeWeapon, integers, hasIdData: true, holderLevel: 275);
        Check(!unknown.HasActiveSpellData && new ItemInfo(unknown).GetODValue() == null,
            "Unknown active buffs were treated as a known empty list for an equipped item.");
        Check(typeof(Item).Assembly.GetType("OracleOfDereth.WorldObject") == null,
            "The plugin must not define a competing WorldObject type.");
        Check(typeof(Item).GetConstructor(new[] { typeof(Decal.Adapter.Wrappers.WorldObject) }) != null,
            "Live capture must accept Decal's WorldObject.");
        Check(!typeof(Item).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Any(f => typeof(Decal.Adapter.Wrappers.WorldObject).IsAssignableFrom(f.FieldType)),
            "Item retained a live WorldObject reference.");
    }

    private static byte[] Fixture(bool equipped = false, int material = 0, int damage = 36)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.ASCII))
        {
            writer.Write(6);
            foreach (var pair in new[] { (218103842, damage), (159, 44), (353, 6), (47, 160), (10, equipped ? 1 : 0), (131, material) })
            { writer.Write(pair.Item1); writer.Write(pair.Item2); }
            writer.Write(1); writer.Write(16); writer.Write(new string('a', 140));
            writer.Write(1); writer.Write(1); writer.Write(true);
            writer.Write(1); writer.Write(167772171); writer.Write(.43);
            writer.Write(1); writer.Write(900); writer.Write(12345678901234L);
            writer.Write(0); // innate spells
            writer.Write(-1);
            return stream.ToArray();
        }
    }

    // Optional integration audit against an installed VGI provider and read-only databases.
    // The temporary fixture database also checks server isolation and provider compatibility.
    public static void Audit(string directory)
    {
        Assembly provider = Assembly.LoadFrom(Path.Combine(directory, "System.Data.SQLite.dll"));
        foreach (string file in Directory.GetFiles(directory, "*.db"))
        {
            int count = 0;
            using (DbConnection connection = Connection(provider, file, true))
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OwnerServer,OwnerCharName,ObjectID,ObjectName,ObjectClass,SerializedData FROM ObjectData";
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var obj = VGInventory.DecodeItem(reader.GetString(0), reader.GetString(1), unchecked((int)reader.GetInt64(2)), reader.GetString(3), (ObjectClass)reader.GetInt32(4), (byte[])reader.GetValue(5));
                        var info = new ItemInfo(obj);
                        info.GetODValue(); info.GetOAValue(); info.GetOMValue();
                        count++;
                    }
                }
            }
            Console.WriteLine(Path.GetFileName(file) + ": decoded and checked overages for " + count + " items.");
        }

        string temporary = Path.Combine(Path.GetTempPath(), "Oracle-VGI-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        string database = Path.Combine(temporary, "_Conquest.db");
        try
        {
            using (DbConnection connection = Connection(provider, database, false))
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE ObjectData (OwnerServer TEXT,OwnerCharName TEXT,ObjectID INTEGER,ObjectName TEXT,ObjectClass INTEGER,SerializedData BLOB)";
                command.ExecuteNonQuery();
                foreach (var row in new[] { ("Conquest", "Mule A", Fixture()), ("Conquest", "Mule B", Fixture()), ("Levistras", "Mule C", Fixture()), ("Conquest", "Broken", new byte[0]) })
                {
                    command.CommandText = "INSERT INTO ObjectData VALUES (@server,@character,42,'Test Dagger',1,@data)";
                    command.Parameters.Clear();
                    foreach (var pair in new[] { ("@server", (object)row.Item1), ("@character", (object)row.Item2), ("@data", (object)row.Item3) })
                    { var p = command.CreateParameter(); p.ParameterName = pair.Item1; p.Value = pair.Item2; command.Parameters.Add(p); }
                    command.ExecuteNonQuery();
                }
            }
            byte[] before = File.ReadAllBytes(database);
            var inventory = new VGInventory(temporary);
            Check(inventory.Refresh("Conquest"), inventory.Error);
            Check(inventory.List.Items.Count == 3 && inventory.UnreadableCount == 1, "Server filter or unreadable-row preservation failed.");
            var broken = inventory.List.Items.Single(i => i.Character == "Broken");
            Check(!broken.IsComplete && new ItemFilter { Weapons = true }.Matches(broken), "Unreadable weapon lost its category or was marked identified.");
            broken.Populate();
            Check(!broken.IsComplete && broken.Description.Contains("unavailable"), "Unreadable item manufactured appraisal data.");
            Check(inventory.Refresh("Conquest", new ItemFilter { Text = "Mule B" }) && inventory.List.Items.Count == 1, "Inventory search lost character ownership.");
            Check(inventory.List.QueueCount == 0, "Database read queued live identification.");
            Check(before.SequenceEqual(File.ReadAllBytes(database)), "Read modified the database.");
            Check(!inventory.Refresh("Levistras") && inventory.List.Items.Count == 0, "Server change retained another server's items.");
            Check(!File.Exists(Path.Combine(temporary, "_Levistras.db")), "Read created a missing database.");
            // Large database: best names arrive last, and IDs repeat across characters.
            using (DbConnection connection = Connection(provider, database, false))
            using (DbTransaction transaction = connection.BeginTransaction())
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM ObjectData";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO ObjectData VALUES ('Conquest',@character,@id,@name,1,@data)";
                foreach (string key in new[] { "@character", "@id", "@name", "@data" })
                { var p = command.CreateParameter(); p.ParameterName = key; command.Parameters.Add(p); }
                for (int i = 0; i < 25000; i++)
                {
                    command.Parameters[0].Value = "Mule " + (i % 36).ToString("D2");
                    command.Parameters[1].Value = i / 36;
                    command.Parameters[2].Value = "Dagger " + (25000 - i).ToString("D5");
                    command.Parameters[3].Value = Fixture(material: i % 2 == 0 ? 61 : 0, damage: 36 + i % 41);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            long baseline = GC.GetTotalMemory(true);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Check(inventory.Refresh("Conquest"), inventory.Error);
            timer.Stop();
            Check(inventory.TotalCount == 25000 && inventory.MatchCount == 25000 && inventory.List.Items.Count == VGInventory.ResultLimit,
                "Large search must count all matches but retain only 2000 rows.");
            Check(inventory.List.Items[0].DisplayName == "Dagger 00001", "Top results were restricted to early database records.");
            Console.WriteLine($"25,000-item search: {timer.ElapsedMilliseconds} ms; retained managed delta {GC.GetTotalMemory(true) - baseline:N0} bytes; {inventory.List.Items.Count} rows.");
            var previousRows = inventory.List.Items;
            inventory.BeginRefresh("Conquest", new ItemFilter { Text = "Mule 35" });
            Check(inventory.AdvanceSearch() && inventory.ScannedCount == 32, "Search did not yield after a bounded batch.");
            Check(ReferenceEquals(previousRows, inventory.List.Items), "Partial results replaced the completed query.");
            inventory.CancelSearch();
            Check(!inventory.IsSearching && ReferenceEquals(previousRows, inventory.List.Items), "Cancellation published partial rows.");
            using (DbConnection connection = Connection(provider, database, false))
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "BEGIN EXCLUSIVE"; command.ExecuteNonQuery();
                command.CommandText = "ROLLBACK"; command.ExecuteNonQuery();
            }
            inventory.BeginRefresh("Conquest", new ItemFilter { Text = "No matches" });
            inventory.AdvanceSearch();
            Check(inventory.Refresh("Conquest", new ItemFilter { Text = "Mule 35" }) && inventory.List.Items.Count > 0 && inventory.List.Items.All(new ItemFilter { Text = "Mule 35" }.Matches),
                "A superseded query published stale results.");
            var expected = new List<ItemListRow>();
            for (int i = 0; i < 25000; i++)
            {
                var row = new ItemListRow(VGInventory.DecodeItem("Conquest", "Mule " + (i % 36).ToString("D2"), i / 36,
                    "Dagger " + (25000 - i).ToString("D5"), ObjectClass.MeleeWeapon, Fixture(material: i % 2 == 0 ? 61 : 0, damage: 36 + i % 41)));
                row.Populate(); expected.Add(row);
            }
            foreach (ItemList.SortType sort in Enum.GetValues(typeof(ItemList.SortType)))
            {
                inventory.List.CurrentSortType = sort;
                Check(inventory.Refresh("Conquest"), inventory.Error);
                Check(inventory.List.Items.Select(r => r.Character + ":" + r.Id).SequenceEqual(
                    ItemList.OrderRows(expected, sort).Take(VGInventory.ResultLimit).Select(r => r.Character + ":" + r.Id)),
                    "Bounded search differs from full-list sorting: " + sort);
            }
            var query = new ItemFilter { Text = "Mule 35", Weapons = true, WeaponHW = true };
            Check(inventory.Refresh("Conquest", query), inventory.Error);
            Check(inventory.MatchCount == expected.Count(query.Matches) && inventory.List.Items.All(query.Matches),
                "Filtered query searched only the previous capped results.");
            Check(inventory.Refresh("Conquest", new ItemFilter { Armor = true }) && inventory.MatchCount == 0 && inventory.List.Items.Count == 0,
                "No-match search retained old rows.");
            Console.WriteLine("25,000-item cap, all sort orders, and filter regression checks passed.");
            Console.WriteLine("VGI database integration tests passed.");
        }
        finally { File.Delete(database); Directory.Delete(temporary); }
    }

    private static DbConnection Connection(Assembly provider, string path, bool readOnly)
    {
        var connection = (DbConnection)Activator.CreateInstance(provider.GetType("System.Data.SQLite.SQLiteConnection"));
        connection.ConnectionString = new DbConnectionStringBuilder { ["Data Source"] = path, ["Read Only"] = readOnly, ["Pooling"] = false }.ConnectionString;
        connection.Open();
        return connection;
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
