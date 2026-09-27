using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class TradeQuantityTests
{
    public static void Run()
    {
        var notes = MakeItem("Promissory Note (250,000)", 100, 100);
        Assert(Trade.SupportsQuantity(notes) && Trade.ValidQuantity(notes, 500), "Cannot buy across multiple stacks.");
        var one = MakeItem("Promissory Note (250,000)", 1, 100);
        Assert(Trade.SupportsQuantity(one), "A stack of one lost its quantity control.");
        Assert(!Trade.ValidQuantity(notes, 0) && !Trade.ValidQuantity(notes, -1), "Invalid quantity accepted.");
        var weapon = MakeItem("Sword", 0, 0);
        Assert(!Trade.SupportsQuantity(weapon) && !Trade.ValidQuantity(weapon, 2), "Non-stackable item allowed multiple units.");
        Assert(Trade.AddCommand(weapon, 1) == "add -123", "Single-item command changed.");
        var command = Trade.AddCommand(notes, 500);
        var match = Regex.Match(command, @"^add (.*) \* (\d+)$");
        Assert(match.Success && match.Groups[2].Value == "500", "Command does not match CyTrader's bulk syntax.");
        var exactName = new Regex(match.Groups[1].Value);
        Assert(exactName.IsMatch(notes.Name), "Bulk command does not match the original item name.");
        Assert(!exactName.IsMatch(notes.Name + " Extra") && !exactName.IsMatch("Other " + notes.Name), "Bulk name matched unrelated items.");
        Assert(!exactName.IsMatch("Promissory Note 250,000"), "Name punctuation was not escaped.");

        int previousRate = Trade.PointsPerMmd;
        try
        {
            Trade.PointsPerMmd = 250;
            Assert(Trade.TryQuantityPrice(1.2, 500, out int cost) && cost == 3, "Bulk price did not round after multiplying.");
            Assert(Trade.TryQuantityPrice(0, 500, out cost) && cost == 0, "Free bulk item was mispriced.");
            Assert(!Trade.TryQuantityPrice(double.MaxValue, 500, out cost), "Overflowing price accepted.");
            Assert(!Trade.TryQuantityPrice(2, 0, out cost), "Zero quantity priced.");
            Assert(!Trade.TryQuantityPrice(double.NaN, 500, out cost), "Invalid quote accepted.");
        }
        finally { Trade.PointsPerMmd = previousRate; }

        var previousItems = ItemList.Trade;
        ItemList.Trade = new ItemList();
        Trade.Init();
        try
        {
            Trade.StartQuantity(notes.Name, 1234, 500);
            Assert(Trade.PurchasePending, "Bulk purchase was not marked pending.");
            Assert(!Trade.ReceiveQuantity(1, "Other", 1234, 500), "Wrong item triggered payment.");
            Assert(!Trade.ReceiveQuantity(1, notes.Name, 4321, 500), "Wrong template triggered payment.");
            Assert(!Trade.ReceiveQuantity(1, notes.Name, 1234, 250), "Partial delivery triggered payment.");
            Assert(!Trade.ReceiveQuantity(1, notes.Name, 1234, 250), "Duplicate event triggered payment.");
            Assert(Trade.ReceiveQuantity(2, notes.Name, 1234, 250), "Complete delivery did not allow payment.");
            Trade.Reset();
            Assert(!Trade.ReceiveQuantity(2, notes.Name, 1234, 250), "Trade reset retained old arrivals.");
            Assert(!Trade.ReceiveQuantity(3, notes.Name, 1234, 300), "Excess delivery triggered incorrect payment.");
            Trade.End();
            Assert(!Trade.PurchasePending, "Closed trade retained a pending purchase.");
            Assert(!Trade.ReceiveQuantity(4, notes.Name, 1234, 500), "Closed trade accepted an old delivery.");
            Trade.StartQuantity(notes.Name, 1234, 1);
            Assert(!Trade.PurchasePending, "Single-item request unexpectedly enabled bulk tracking.");
        }
        finally
        {
            Trade.Init();
            ItemList.Trade = previousItems;
        }
    }

    private static Item MakeItem(string name, int count, int max) => new Item("Test", "Bot", -123, name, ObjectClass.Gem,
        integers: new Dictionary<int, int> { [(int)LongValueKey.StackCount] = count, [(int)LongValueKey.StackMax] = max });

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
