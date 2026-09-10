using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Decal.Adapter.Wrappers;
using Microsoft.VisualBasic.FileIO;
using OracleOfDereth;

internal static class ItemExportTests
{
    public static void Run()
    {
        var weapon = Row(new Item("Conquest", "Atlas", -42, "A \"special\",\ndagger", ObjectClass.MeleeWeapon,
            new Dictionary<int, int> { [(int)LongValueKey.Material] = 61, [(int)LongValueKey.DamageType] = 1024,
                [(int)LongValueKey.Workmanship] = 7, [(int)LongValueKey.NumberTimesTinkered] = 10 }, hasIdData: true));
        var salvage = Row(new Item("Conquest", "Atlas", 43, "Salvage (100)", ObjectClass.Salvage,
            new Dictionary<int, int> { [(int)LongValueKey.Material] = 61, [(int)LongValueKey.UsesRemaining] = 100 },
            doubles: new Dictionary<int, double> { [(int)DoubleValueKey.SalvageWorkmanship] = 8.333333 }, hasIdData: true));
        var keys = Row(new Item("Conquest", "Atlas", 44, "Keyring", ObjectClass.Key,
            new Dictionary<int, int> { [(int)LongValueKey.UsesRemaining] = 0, [(int)LongValueKey.KeysHeld] = 12 }, hasIdData: true));
        var stack = Row(new Item("Conquest", "Atlas", 45, "Component", ObjectClass.SpellComponent,
            new Dictionary<int, int> { [(int)LongValueKey.StackCount] = 250 }, hasIdData: true));
        var unavailable = Row(new Item("Conquest", "Atlas", -46, "Unknown", ObjectClass.Armor));
        var items = new List<ItemListRow> { weapon, salvage, keys, stack, unavailable };
        // Description formatting remains identical across population, cloning, and stub transitions.
        string expected = new ItemInfo(weapon.Item).ToString();
        var clone = weapon.Clone();
        Check(clone.Description == expected && weapon.Description == expected, "Deferred description changed output.");
        clone.PopulateStub();
        Check(clone.Description == clone.Item.Name + " (details unavailable)" && weapon.Description == expected,
            "Stub description retained appraisal text or affected another row.");
        clone.Populate();
        Check(clone.Description == expected, "Repopulation retained the stub description.");
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var csv = ParseCsv(Write("WriteCsv", items));
            var json = ParseJson(Write("WriteJson", items));
            var headers = csv[0];
            Check(headers.Skip(34).Take(8).SequenceEqual(new[] { "D", "DR", "C", "CR", "CD", "CDR", "HB", "V" }), "Rating headers changed.");
            Check(!headers.Contains("Craft") && headers.Contains("Workmanship"), "Workmanship is still a display string.");
            Check(csv.Count == items.Count + 1 && json.Count == items.Count, "Export lost rows.");
            for (int i = 0; i < items.Count; i++)
            {
                Check(csv[i + 1].Length == headers.Length && json[i].Count == headers.Length, "Export columns differ.");
                foreach (string header in headers)
                    Check(csv[i + 1][Array.IndexOf(headers, header)] == Convert.ToString(json[i][header], CultureInfo.InvariantCulture), "CSV/JSON mismatch: " + header);
            }
            Check(Convert.ToUInt32(json[0]["Item ID"]) == unchecked((uint)-42) && !(json[0]["Item ID"] is string), "Item ID lost bits or was not numeric.");
            Check(Equals(json[0]["Material"], "Iron") && Equals(json[0]["Element"], "Nether"), "Material or element missing.");
            Check(Convert.ToDouble(json[0]["Workmanship"]) == 7, "Ten-tinkered item's workmanship was suppressed.");
            Check(Convert.ToDouble(json[1]["Workmanship"]) == 8.333333 && !(json[1]["Workmanship"] is string), "Salvage workmanship was rounded, localized, or quoted.");
            Check(Convert.ToInt32(json[0]["Quantity"]) == 1 && Convert.ToInt32(json[3]["Quantity"]) == 250, "Quantity is not the item/stack count.");
            Check(Convert.ToInt32(json[1]["Uses Remaining"]) == 100 && Convert.ToInt32(json[2]["Uses Remaining"]) == 0,
                "Uses were lost, including a known zero.");
            Check(Convert.ToInt32(json[2]["Keys Held"]) == 12, "Keyring count was lost.");
            Check(json[4]["Quantity"] == null && json[4]["Workmanship"] == null && json[0]["Uses Remaining"] == null, "Missing data was fabricated.");
            Check(Convert.ToUInt32(json[4]["Item ID"]) == unchecked((uint)-46) && Equals(json[4]["ObjectClass"], "Armor"), "Incomplete item lost known identity.");
            Check(weapon.DescriptionWithOwner == weapon.Description + " (Last on Atlas)", "Text/chat ownership suffix differs.");
            var noOwner = Row(new Item("Conquest", "", 1, "Unknown", ObjectClass.Armor));
            Check(noOwner.DescriptionWithOwner == noOwner.Description, "Invented an owner.");

            var many = Enumerable.Repeat(stack, 5001).ToList();
            Check(ParseCsv(Write("WriteCsv", many)).Count == 5002 && ParseJson(Write("WriteJson", many)).Count == 5001,
                "Serializer capped exports at the display limit.");
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    private static ItemListRow Row(Item item)
    {
        var row = new ItemListRow(item);
        row.Populate();
        return row;
    }

    private static string Write(string method, List<ItemListRow> items)
    {
        using (var writer = new StringWriter())
        {
            typeof(ItemExport).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { writer, items });
            return writer.ToString();
        }
    }

    private static List<string[]> ParseCsv(string text)
    {
        var rows = new List<string[]>();
        using (var reader = new TextFieldParser(new StringReader(text)))
        {
            reader.SetDelimiters(",");
            reader.HasFieldsEnclosedInQuotes = true;
            reader.TrimWhiteSpace = false;
            while (!reader.EndOfData) rows.Add(reader.ReadFields());
        }
        return rows;
    }

    private static List<Dictionary<string, object>> ParseJson(string text)
    {
        var serializer = new DataContractJsonSerializer(typeof(List<Dictionary<string, object>>),
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            return (List<Dictionary<string, object>>)serializer.ReadObject(stream);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
