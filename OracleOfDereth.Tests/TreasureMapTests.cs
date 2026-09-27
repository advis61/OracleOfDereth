using System;
using System.Globalization;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class TreasureMapTests
{
    public static void Run()
    {
        const string name = "Hell Bovine's Treasure Map";
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Check(TreasureMap.Format(name, ObjectClass.Gem, -3, 43), name + ": 3.0S 43.0E");
            Check(TreasureMap.Format(name, ObjectClass.Gem, 10.5, -34.6), name + ": 10.5N 34.6W");
            Check(TreasureMap.Format(name, ObjectClass.Gem, 0, 0), name + ": 0.0N 0.0E");
            Check(TreasureMap.Format(name, ObjectClass.Gem, null, 43), null);
            Check(TreasureMap.Format(name, ObjectClass.Gem, -3, null), null);
            Check(TreasureMap.Format(name, ObjectClass.Gem, double.NaN, 43), null);
            Check(TreasureMap.Format(name, ObjectClass.Gem, -3, double.PositiveInfinity), null);
            Check(TreasureMap.Format("Unrelated Gem", ObjectClass.Gem, -3, 43), null);
            Check(TreasureMap.Format(name, ObjectClass.Player, -3, 43), null);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    private static void Check(string actual, string expected)
    {
        if (actual != expected) throw new InvalidOperationException("Unexpected treasure map output: " + actual);
    }
}
