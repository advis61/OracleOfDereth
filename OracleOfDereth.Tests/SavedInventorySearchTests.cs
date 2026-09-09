using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class SavedInventorySearchTests
{
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OracleSavedSearch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Check(SavedInventorySearch.Peek("Conquest", "Atlas", directory) == null && Directory.GetFiles(directory).Length == 0, "Polling created a file.");
            var saved = new SavedInventorySearch
            {
                Server = "Conquest", SavedBy = "Advis", Sort = ItemList.SortType.CharacterDescending,
                CategoryOrder = new[] { "Armor", "Weapons" },
                Selection = new SavedInventorySelection(new Item("Conquest", "Atlas", -42, "Test Dagger", ObjectClass.MeleeWeapon, icon: 12))
            };
            // Include every boolean, hidden subfilters, a multi-slot mask, and XML-sensitive text.
            foreach (var field in typeof(ItemFilter).GetFields().Where(f => f.FieldType == typeof(bool))) field.SetValue(saved.Filter, true);
            saved.Filter.Text = "Atlas & <test> \"Nether\"";
            saved.Filter.ArmorSlots = ItemInfo.ArmorSlot.Head | ItemInfo.ArmorSlot.Feet;
            saved.Save(directory);
            Check(SavedInventorySearch.Peek("Conquest", "Advis", directory) == null, "Saver could load their own handoff.");
            Check(SavedInventorySearch.Peek("Other Server", "Atlas", directory) == null, "Search leaked to another server.");
            var read = SavedInventorySearch.Peek("Conquest", "Atlas", directory);
            foreach (var field in typeof(ItemFilter).GetFields())
                Check(Equals(field.GetValue(saved.Filter), field.GetValue(read.Filter)), "Lost filter " + field.Name);
            Check(read.CategoryOrder.SequenceEqual(saved.CategoryOrder) && read.Sort == saved.Sort, "Lost category order or sorting.");
            Check(read.Selection.Matches(new Item("Conquest", "Atlas", -42, "Test Dagger", ObjectClass.MeleeWeapon, icon: 12), "Conquest"), "Lost saved selection.");
            Check(!read.Selection.Matches(new Item("Conquest", "Atlas", -43, "Test Dagger", ObjectClass.MeleeWeapon, icon: 12), "Conquest"), "Matched a namesake.");
            Check(!read.Selection.Matches(new Item("Conquest", "Advis", -42, "Test Dagger", ObjectClass.MeleeWeapon, icon: 12), "Conquest"), "Matched a moved item.");
            Check(!read.Selection.Matches(new Item("Conquest", "Atlas", -42, "Changed Dagger", ObjectClass.MeleeWeapon, icon: 12), "Conquest"), "Matched a recycled ID.");
            Check(SavedInventorySearch.Take("Conquest", "Advis", saved.Token, directory) == null, "Saver consumed their own search.");

            var replacement = new SavedInventorySearch { Server = "Conquest", SavedBy = "Advis", Filter = new ItemFilter { Text = "Only text" } };
            replacement.Save(directory);
            Check(SavedInventorySearch.Take("Conquest", "Atlas", saved.Token, directory) == null, "Stale button consumed a newer save.");
            var taken = SavedInventorySearch.Take("Conquest", "Atlas", replacement.Token, directory);
            Check(taken != null && taken.Selection == null && taken.Filter.Text == "Only text", "Filter-only handoff failed.");
            Check(SavedInventorySearch.Peek("Conquest", "Raine", directory) == null && SavedInventorySearch.Take("Conquest", "Raine", replacement.Token, directory) == null,
                "Search was not one-time use.");
            Check(Directory.GetFiles(directory).Length == 0, "Consumed file was not deleted.");

            saved.Save(directory);
            using (var one = Consumer(directory, "Atlas", saved.Token))
            using (var two = Consumer(directory, "Raine", saved.Token))
            {
                Wait(one); Wait(two);
                Check(new[] { one.ExitCode, two.ExitCode }.OrderBy(code => code).SequenceEqual(new[] { 0, 10 }),
                    "Two processes did not consume exactly once: " + one.StandardError.ReadToEnd() + two.StandardError.ReadToEnd());
            }
            Check(SavedInventorySearch.Peek("Conquest", "Atlas", directory) == null, "Consumed search reappeared.");
            AssertSubfilterRestore(saved.Filter);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void AssertSubfilterRestore(ItemFilter allSelected)
    {
        var type = typeof(ItemFilter).Assembly.GetType("OracleOfDereth.ItemSubfilters", true);
        var definitions = (Array)type.GetField("Definitions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var isSelected = type.GetMethod("IsSelected", BindingFlags.NonPublic | BindingFlags.Static);
        var fields = typeof(ItemFilter).GetFields();
        var restored = new ItemFilter();
        foreach (var definition in definitions)
        {
            var apply = (Action<ItemFilter, bool>)definition.GetType().GetField("Apply").GetValue(definition);
            bool selected = (bool)isSelected.Invoke(null, new object[] { allSelected, apply });
            apply(restored, selected);
            var onlyThis = new ItemFilter();
            apply(onlyThis, true);
            Check((bool)isSelected.Invoke(null, new object[] { onlyThis, apply }), "Restore lost an individual subfilter.");
            Check(!(bool)isSelected.Invoke(null, new object[] { new ItemFilter(), apply }), "Restore selected an unset subfilter.");
            foreach (var field in fields.Where(f => f.FieldType == typeof(bool) && (bool)f.GetValue(onlyThis)))
                Check(Equals(field.GetValue(restored), field.GetValue(allSelected)), "Restore lost " + field.Name);
        }
        Check(restored.ArmorSlots == allSelected.ArmorSlots, "Restore changed armor slots.");
    }

    public static int Consume(string directory, string character, string token)
    {
        int attempts = 0;
        while (true)
        {
            try { return SavedInventorySearch.Take("Conquest", character, token, directory) == null ? 10 : 0; }
            catch (IOException) when (++attempts < 20) { Thread.Sleep(20); }
        }
    }

    private static Process Consumer(string directory, string character, string token) => Process.Start(new ProcessStartInfo
    {
        FileName = Assembly.GetExecutingAssembly().Location,
        Arguments = "--consume-search \"" + directory + "\" " + character + " " + token,
        UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
    });

    private static void Wait(Process process)
    {
        if (process.WaitForExit(30000)) return;
        process.Kill();
        process.WaitForExit();
        throw new InvalidOperationException("Saved-search consumer timed out.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
