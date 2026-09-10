using System;
using System.Reflection;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class ItemTextSearchTests
{
    public static void Run()
    {
        var gear = Row("Weapons Eveldan", "Legendary Frost Ward, Legendary Acid Ward", "Adept", "CD2");
        var unrelated = Row("Advis Eveldan", "Bludgeoning Protection, Frost Ward, Heavy Weapons 430");
        var bludgeon = Row("Armor Eveldan", "Legendary Bludgeon Ward");
        Check("\"Bludgeon Ward\"", bludgeon, true);
        Check("\"Bludgeon Ward\"", unrelated, false);
        Check("\"Weapons Eveldan\"", gear, true);
        Check("\"Weapons Eveldan\"", unrelated, false);
        Check("weapons eveldan", unrelated, true); // Existing free-form AND semantics.
        Check("\"Weapons Eveldan\" CD2 \"Legendary Frost\"", gear, true);
        Check("CD2 legendary frost", gear, true);
        Check("CD2 legendary flame", gear, false);
        Check("\"ward adept\"", gear, false); // Never join columns for a phrase.
        Check("legendary.*legendary", gear, true);
        Check("legendary.*legendary", bludgeon, false);
        Check("legendary (frost|flame|acid)", gear, true);
        Check("legendary (frost|flame|acid)", bludgeon, false);
        Check("legendary frost.*legendary acid", gear, true);
        Check("legendary (frost|flame|acid).+adept", gear, true);
        Check("legendary acid.*Adept", gear, true);
        Check("legendary acid.*Defender", gear, false);
        Check("legendary.*CD2", gear, true);
        Check("\"Weapons Eveldan\" legendary (frost|acid).*CD2", gear, true);
        Check("legendary*2 CD2", gear, true);
        Check("legendary*3", gear, false);
        Check("   ", gear, true);
        Check(".*", gear, true);
        Check("\"\"", gear, true);

        foreach (string tier in new[] { "Legendary", "Epic", "Major", "Minor" })
        {
            var exact = Row("Advis Eveldan", tier + " Bludgeoning Ward, Frost Ward", "Adept", "CD2");
            var split = Row("Advis Eveldan", tier + " Frost Ward, Bludgeoning Protection", "Adept", "CD2");
            Check("Advis " + tier + " Bludgeoning", exact, true);
            Check("Advis " + tier + " Bludgeoning", split, false);
            Check("Advis " + tier.ToLowerInvariant() + "\tBLUDGEONING CD2", exact, true);
            Check(tier + " Bludgeoning", Row("Bludgeoning Mule", tier + " Frost Ward"), false);
            Check("Advis " + tier, exact, true); // A trailing tier is still a standalone term.
            Check(tier + " \"Bludgeoning\" Advis", split, true); // Explicit quotes stay independent.
            Check(tier + ".*Bludgeoning", split, true); // Explicit regex is not rewritten.
        }
        Check("legendary frost legendary acid CD2", gear, true);
        Check("legendary frost epic acid", gear, false);
        Check("\"legendary\" frost", Row("Atlas", "Legendary Acid Ward, Frost Ward"), true);

        var filter = new ItemFilter { Text = "legendary (" };
        Assert(!filter.Matches(gear) && filter.SearchError != null, "Invalid regex was not handled.");
        filter.Text = "\"unfinished";
        Assert(!filter.Matches(gear) && filter.SearchError != null, "Unclosed quote was not handled.");
        filter.Text = "CD2";
        Assert(filter.Matches(gear) && filter.SearchError == null, "Editing did not replace the parsed query.");

        var longRow = Row("Atlas", new string('a', 10000) + "!");
        filter.Text = "(a+)+$";
        Assert(!filter.Matches(longRow) && filter.SearchError != null, "Slow regex was not stopped.");
        Assert(!filter.Matches(gear), "Timed-out regex ran again.");
        filter.Text = "CD2";
        Assert(filter.Matches(gear), "Could not recover after a regex timeout.");

        var inventory = new VGInventory();
        Assert(!inventory.Refresh("Conquest", new ItemFilter { Text = "(" }) &&
            inventory.Error.Contains("Invalid search regex"), "VGI did not report the search error.");
    }

    private static ItemListRow Row(string owner, string spells, string set = "", string ratings = "")
    {
        var row = new ItemListRow(new Item("Conquest", owner, 1, "Test Gear", ObjectClass.Armor));
        row.PopulateStub();
        typeof(ItemListRow).GetProperty("SummaryCol2").SetValue(row, set);
        typeof(ItemListRow).GetProperty("SummaryCol3").SetValue(row, ratings);
        typeof(ItemListRow).GetProperty("SummaryCol4").SetValue(row, spells);
        return row;
    }

    private static void Check(string query, ItemListRow row, bool expected) =>
        Assert(new ItemFilter { Text = query }.Matches(row) == expected, "Unexpected match for: " + query);

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
