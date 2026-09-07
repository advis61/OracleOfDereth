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
        AssertElementSubfilters();
        AssertArmorSubfilters();
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

        var layout = new XmlDocument();
        using (Stream xml = typeof(Item).Assembly.GetManifestResourceStream("OracleOfDereth.mainView.xml")) layout.Load(xml);
        var tabs = layout.SelectNodes("//control[@name='ServerViewNotebook']/page");
        Check(tabs.Count == 7 && tabs[6].Attributes["label"].Value == "Inventory", "Inventory is not at Server tab index 6.");
        Check(layout.SelectNodes("//control[starts-with(@name,'VGInventoryFilter') and @progid='DecalControls.CheckBox']").Count == 10, "Inventory filters differ from Items.");
        Check(layout.SelectSingleNode("//control[@name='VGInventoryList']/column[1]").Attributes["name"].Value == "Character", "First column must be Character.");
    }

    private static void Reject(byte[] bytes)
    {
        try { VGInventory.DecodeItem("Conquest", "Mule", 1, "Broken", ObjectClass.MeleeWeapon, bytes); }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed VGI blob was accepted.");
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
