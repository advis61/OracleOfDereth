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
        var settings = new XmlDocument();
        settings.LoadXml("<Settings />");
        typeof(SettingsFile).GetField("_doc", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, settings);
        byte[] bytes = Convert.FromBase64String(Jambiya);
        var weapon = new VirindiObject("Conquest", "Weapon Mule", -42, "Flaming Jambiya", ObjectClass.MeleeWeapon, bytes);
        var info = new ItemInfo(weapon);
        Check(info.GetODValue() == 8, "Captured Jambiya should have +8 OD.");
        Check(Math.Abs(info.GetWeaponDamageLow() - 20.52) < .001, "Damage variance did not decode.");
        Check(weapon.Icon == 5598 && weapon.SpellCount == 4 && weapon.Spell(3) == 6089, "Icon or innate spell list did not decode.");
        Check(weapon.ActiveSpellCount == 0, "VGI must not invent active spells.");
        Check(weapon.Values((LongValueKey)999999, 17) == 17, "Missing property did not honor its default.");
        Check(weapon.Id == -42 && weapon.OwnerCharName == "Weapon Mule", "Owner metadata was lost.");
        Reject(bytes.Take(bytes.Length - 1).ToArray());
        Reject(new byte[] { 255, 255, 255, 255 });
        Reject(bytes.Concat(new byte[] { 0 }).ToArray());

        var generic = new VirindiObject("Conquest", "Mule B", -42, "Test Dagger", ObjectClass.MeleeWeapon, Fixture());
        Check(generic.Values((StringValueKey)16).Length == 140, "Multi-byte ASCII string prefix did not decode.");
        Check(generic.Values((BoolValueKey)1), "Boolean dictionary did not decode.");
        var equipped = new VirindiObject("Conquest", "Mule B", -42, "Test Dagger", ObjectClass.MeleeWeapon, Fixture(true));
        Check(new ItemInfo(equipped).GetODValue() == null, "Saved equipped weapon must not use a live holder or guess its buffs.");

        var list = new ItemList();
        list.Load(new[] { generic, new VirindiObject("Conquest", "Mule A", -42, "Other Dagger", ObjectClass.MeleeWeapon, Fixture()) });
        Check(list.Items.Count == 2 && list.QueueCount == 0, "Saved items with the same ID on different characters were lost or queued for ID.");
        list.Sort(ItemList.SortType.CharacterAscending);
        Check(list.Items[0].Character == "Mule A", "Character sort failed.");
        MethodInfo exportRow = typeof(ItemExport).GetMethod("Row", BindingFlags.NonPublic | BindingFlags.Static);
        var exported = (string[])exportRow.Invoke(null, new object[] { list.Items[0] });
        Check(exported[0] == "Mule A" && exported[1] == "Conquest" && exported[2] == "Other Dagger", "Saved export used the live character or world filter.");
        var incompleteExport = (string[])exportRow.Invoke(null, new object[] { new Item { Character = "Mule B", Server = "Conquest", Id = -42, Name = "Unreadable" } });
        Check(incompleteExport[0] == "Mule B" && incompleteExport[2] == "Unreadable", "Incomplete saved export used a live object with the same ID.");
        var filter = new ItemFilter { Text = "Mule B", Weapons = true };
        Check(list.Items.Count(filter.Matches) == 1, "Character search failed.");
        filter.Armor = true;
        filter.Weapons = false;
        Check(!list.Items.Any(filter.Matches), "Character search bypassed the category filter.");
        var misleadingOwner = new Item { Character = "Legendary Legendary", SummaryCol4 = "Legendary Blood Thirst" };
        Check(!new ItemFilter { Doubles = true }.Matches(misleadingOwner), "Character names must not count as item cantrips.");

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
        try { new VirindiObject("Conquest", "Mule", 1, "Broken", ObjectClass.MeleeWeapon, bytes); }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed VGI blob was accepted.");
    }

    private static byte[] Fixture(bool equipped = false)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.ASCII))
        {
            writer.Write(5);
            foreach (var pair in new[] { (218103842, 36), (159, 44), (353, 6), (47, 160), (10, equipped ? 1 : 0) })
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
                        var obj = new VirindiObject(reader.GetString(0), reader.GetString(1), unchecked((int)reader.GetInt64(2)), reader.GetString(3), (ObjectClass)reader.GetInt32(4), (byte[])reader.GetValue(5));
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
            Check(inventory.Search(new ItemFilter { Text = "Mule B" }).Count == 1, "Inventory search lost character ownership.");
            Check(inventory.List.QueueCount == 0, "Database read queued live identification.");
            Check(before.SequenceEqual(File.ReadAllBytes(database)), "Read modified the database.");
            Check(!inventory.Refresh("Levistras") && inventory.List.Items.Count == 0, "Server change retained another server's items.");
            Check(!File.Exists(Path.Combine(temporary, "_Levistras.db")), "Read created a missing database.");
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
