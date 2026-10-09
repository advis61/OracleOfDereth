using System;
using System.Reflection;
using OracleOfDereth;

internal static class IncomeTests
{
    public static void Run()
    {
        var session = new IncomeSession();
        var xp = session.Register("XP");
        var start = new DateTime(2026, 1, 1);
        session.Reset(start);
        xp.ObserveCounter(1_000_000);
        Equal(xp.Total, 0, "Initial XP is a baseline");
        Equal(xp.MinutePerHour, null, "Minute needs a complete interval");
        Equal(xp.FiveMinutePerHour, null, "Five minutes need complete history");
        for (int minute = 1; minute <= 5; minute++)
        {
            xp.ObserveCounter(1_000_000 + minute * 100);
            session.SampleMinute();
            Equal(xp.MinutePerHour, 6_000, "Minute sample rate");
            if (minute < 5) Equal(xp.FiveMinutePerHour, null, "Reporter five-minute warmup");
        }
        Equal(xp.FiveMinutePerHour, 6_000, "Reporter six-snapshot formula");
        xp.ObserveCounter(1_000_700);
        Equal(xp.Total, 700, "Live total includes gains after the last sample");
        Equal(xp.FiveMinutePerHour, 6_000, "Reporter rate holds until the next minute sample");
        Equal(xp.SessionPerHour(session.Elapsed(start.AddMinutes(5.5))), 700 / (5.5 / 60), "Session uses precise elapsed time");
        session.SampleMinute();
        Equal(xp.FiveMinutePerHour, 7_200, "Ring wraps and drops oldest minute");
        Equal(xp.MinutePerHour, 12_000, "Latest complete minute");
        for (int i = 0; i < 6; i++) session.SampleMinute();
        Equal(xp.FiveMinutePerHour, 0, "Idle minutes age gains out");
        xp.ObserveCounter(100); // enlightenment/counter reset
        Equal(xp.Total, 700, "Counter decreases do not create negative income");
        xp.ObserveCounter(150);
        Equal(xp.Total, 750, "Track gains after a counter reset");
        var coins = session.Register("Coins");
        coins.AddGain(5);
        Equal(xp.Total, 750, "Metrics are independent");
        Equal(coins.Total, 5, "Future currencies can feed award amounts");
        session.Reset(start.AddHours(1));
        xp.ObserveCounter(150);
        Equal(xp.Total, 0, "Reset discards earned totals");
        Equal(coins.Total, 0, "Reset affects every row");
        Equal(xp.FiveMinutePerHour, null, "Reset clears rolling history");
        Equal(xp.SessionPerHour(TimeSpan.Zero), 0, "Zero elapsed is finite");
        if (session.Elapsed(start.AddHours(26)).TotalHours != 25)
            throw new Exception("Session duration must not wrap at 24 hours.");

        // A queued callback after shutdown must return before accessing Decal.
        typeof(Income).GetMethod("SampleMinute", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { new object(), EventArgs.Empty });

        session.Reset(start);
        xp.ObserveCounter(5_000_000_000_000);
        xp.ObserveCounter(5_000_176_201_461);
        Equal(xp.Total, 176_201_461, "Large lifetime counters preserve session gains");
        Equal(xp.SessionPerHour(TimeSpan.FromSeconds(350)), 176_201_461d / (350d / 3600), "Reporter session formula");
        Luminance();
        ConquestReports();
        Kills();
        PetKills();
        ConquestCoins();
        Reports();
    }

    private static void Reports()
    {
        var session = new IncomeSession();
        var xp = session.Register("Experience");
        var pets = session.Register("Kills (pet)");
        session.Reset(new DateTime(2026, 1, 1));
        xp.AddGain(1000);
        if (xp.Report(TimeSpan.FromMinutes(5)) != "You've made 1,000 XP in 00:05:00 for 12,000 XP/hour.")
            throw new Exception("Reporter summary must use whole-number rates and omit an unavailable five-minute window.");
        pets.AddGain(5);
        for (int i = 0; i < 5; i++) session.SampleMinute();
        if (xp.Report(TimeSpan.FromMinutes(5)) != "You've made 1,000 XP in 00:05:00 for 12,000 XP/hour (5min 12,000 XP/hour).")
            throw new Exception("Reporter summary must include the rolling rate once available.");
        if (pets.Report(TimeSpan.FromMinutes(5)) != "You've killed 5 creatures with your pet in 00:05:00 for 60 kills/hour (5min 60 kills/hour).")
            throw new Exception("Pet reports must distinguish pet kills.");
        session.Reset(new DateTime(2026, 1, 1));
        if (xp.Report(TimeSpan.Zero) != "You've made 0 XP in 00:00:00 for 0 XP/hour.")
            throw new Exception("A freshly reset report must not include stale rates or infinite values.");
        if (!xp.Report(TimeSpan.FromHours(25)).Contains("25:00:00"))
            throw new Exception("Report duration must preserve hours beyond a day.");
    }

    private static void ConquestCoins()
    {
        var session = new IncomeSession();
        var metric = session.Register("Conquest Coins");
        var source = new IncomeConquestCoins(metric);
        session.Reset(new DateTime(2026, 1, 1));
        source.NoteChat("Treasure Guardian gives you Conquest Coin.");
        source.NoteChat("Quest NPC gives you 45 Conquest Coins.");
        source.NoteChat("[12:34:56] Quest NPC gives you 1,000 Conquest Coins.");
        source.NoteChat("You have received (5) Conquest Coins!");
        source.NoteChat("You have received (50) Conquest Coins (Bonus)!");
        source.NoteChat("You have received Conquest Coins!");
        source.NoteChat("You have received Conquest Coin!");
        source.NoteChat("You have received 2 Conquest Coins!");
        Equal(metric.Total, 1105, "Single, stacked, grouped, and treasure-map bonus rewards sum their quantities");
        foreach (string text in new[] { null, "", "Quest NPC tries to give you 45 Conquest Coins.",
            "Deposited 45 Conquest Coins", "Withdrew 45 Conquest Coins", "[BANK] Conquest Coins (non-transferable): 45",
            "You have spent 45 Conquest Coins. Bank balance: 100 coins.",
            "Someone says, \"Treasure Guardian gives you Conquest Coin.\"",
            "Quest NPC gives you 1,00 Conquest Coins.", "Quest NPC gives you -45 Conquest Coins.",
            "Quest NPC gives you 999999999999999999999999 Conquest Coins.",
            "Quest NPC gives you Weakly Conquest Coins.", "You have received (50) Conquest Coins (Bonus)! trailing text" })
            source.NoteChat(text);
        Equal(metric.Total, 1105, "Transfers, failed rewards, quoted chat, other currencies, and invalid amounts do not count");
        for (int i = 0; i < 5; i++) session.SampleMinute();
        Equal(metric.FiveMinutePerHour, 13260, "Coins share rolling rates");
        session.Reset(new DateTime(2026, 1, 1));
        Equal(metric.Total, 0, "Reset clears coins");
        source.NoteChat("Quest NPC gives you 45 Conquest Coins.");
        Equal(metric.Total, 45, "Coin tracking continues after reset");
    }

    private static void Kills()
    {
        var session = new IncomeSession();
        var metric = session.Register("Kills");
        var source = new IncomeKills(metric);
        var start = new DateTime(2026, 1, 1);
        session.Reset(start);
        source.ObserveEvent(0xF7B0, 0x01AD);
        source.ObserveEvent(0xF7B0, 0x01AD);
        Equal(metric.Total, 2, "Each killer notification counts once regardless of its text");
        source.ObserveEvent(0xF7B0, 0x01AC); // victim notification
        source.ObserveEvent(0xF7B0, 0x13); // character properties
        source.ObserveEvent(0xF7B0, 0x00C9); // identify
        source.ObserveEvent(0xF7B1, 0x01AD); // wrong packet envelope
        Equal(metric.Total, 2, "Other events and nearby broadcasts do not count");
        Equal(metric.SessionPerHour(session.Elapsed(start.AddMinutes(1))), 120, "Kills share session hourly rates");
        for (int i = 0; i < 5; i++) session.SampleMinute();
        Equal(metric.FiveMinutePerHour, 24, "Kills use the existing rolling five-minute rate");
        session.Reset(start.AddHours(1));
        Equal(metric.Total, 0, "Reset clears kill total");
        Equal(metric.FiveMinutePerHour, null, "Reset clears kill history");
        source.ObserveEvent(0xF7B0, 0x01AD);
        Equal(metric.Total, 1, "Kill tracking continues after reset");
    }

    private static void PetKills()
    {
        var session = new IncomeSession();
        var direct = session.Register("Kills");
        var pet = session.Register("Kills (pet)");
        var source = new IncomePetKills(pet);
        session.Reset(new DateTime(2026, 1, 1));
        source.NoteChat("Your Fire Elemental killed Treasure Guardian!");
        source.NoteChat("[12:34:56] Your Your summon killed Drudge!");
        source.NoteChat("Your Bob's Pet killed Olthoi Warrior!");
        Equal(pet.Total, 3, "Count named pets and the server's unnamed-summon fallback");
        foreach (string text in new[] { null, "", "You killed Drudge!", "Someone's pet killed Drudge!",
            "Someone says, \"Your Fire Elemental killed Drudge!\"", "Your pet killed !",
            "Your  killed Drudge!", "Your pet killed Drudge! unrelated suffix" })
            source.NoteChat(text);
        Equal(pet.Total, 3, "Ignore other kill messages, quotes, and incomplete messages");
        Equal(direct.Total, 0, "Pet chat does not affect direct kills");
        for (int i = 0; i < 5; i++) session.SampleMinute();
        Equal(pet.FiveMinutePerHour, 36, "Pet kills share rolling rates");
        session.Reset(new DateTime(2026, 1, 1));
        Equal(pet.Total, 0, "Reset clears pet kills");
        Equal(pet.FiveMinutePerHour, null, "Reset clears pet-kill history");
        source.NoteChat("Your Fire Elemental killed Drudge!");
        Equal(pet.Total, 1, "Pet kills continue after reset");
    }

    private static void ConquestReports()
    {
        var session = new IncomeSession();
        var metric = session.Register("Luminance");
        session.Reset(new DateTime(2026, 1, 1));
        var source = new ConquestIncomeLuminance(metric);
        source.Reset();
        string Report(long total) => "[LPH] You've earned " + total.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
            + " Luminance in 00:01:00 for 60,000 Lum/hour (5min 0 Lum/hour).";
        if (source.NoteChat(Report(999), out _)) throw new Exception("Ignore reports before start acknowledgement.");
        if (!source.NoteChat("[LPH] Luminance tracking started.", out bool isReport) || isReport || !source.Ready)
            throw new Exception("Recognize /lph start acknowledgement.");
        source.NoteChat(Report(1000), out isReport);
        if (!isReport) throw new Exception("Recognize server luminance total.");
        Equal(metric.Total, 1000, "First LPH reply includes every gain since start");
        source.NoteChat(Report(1000), out _);
        Equal(metric.Total, 1000, "Repeated LPH reply does not double count");
        source.NoteChat("You've banked 203 Luminance.", out _);
        source.NoteChat("Someone says, \"" + Report(5000) + "\"", out _);
        Equal(metric.Total, 1000, "Bank awards and quoted reports are not additional income");
        // Reopening receives the total and retains the server's five-minute rate.
        source.NoteChat(Report(6000), out _);
        metric.ClearHistory();
        Equal(metric.Total, 6000, "Catch up silent passup and gains earned while closed");
        Equal(metric.FiveMinutePerHour, 0, "Use the server's reported zero during warmup");
        for (int minute = 1; minute <= 5; minute++)
        {
            source.NoteChat(Report(6000 + minute * 100), out _);
            session.SampleMinute();
        }
        Equal(metric.FiveMinutePerHour, 0, "Conquest's reported rate takes precedence over local samples");
        source.NoteChat("[LPH] Luminance tracking started.", out _);
        source.NoteChat(Report(100), out _);
        Equal(metric.Total, 6600, "Manual server restart preserves local earnings");
        session.Reset(new DateTime(2026, 1, 1));
        source.Reset();
        source.NoteChat(Report(9999), out _);
        Equal(metric.Total, 0, "Late old report cannot restore pre-reset gains");
        source.NoteChat("[12:34:56] [LPH] Luminance tracking started.", out _);
        source.NoteChat(Report(203), out _);
        Equal(metric.Total, 203, "Session reset begins a new server total");
        source.NoteChat(" [LPH] You've earned 574,997 Luminance in 00:14:18 for 2,409,894 Lum/hour (5min 2,365,764 Lum/hour).", out isReport);
        if (!isReport) throw new Exception("Parse the observed Conquest LPH report including leading whitespace.");
        Equal(metric.Total, 574997, "Parse reported luminance total");
        Equal(metric.FiveMinutePerHour, 2365764, "Use the exact Conquest five-minute rate");
        metric.ClearHistory();
        Equal(metric.FiveMinutePerHour, 2365764, "Reopening does not discard the server five-minute rate");
        if (!metric.Report(TimeSpan.FromMinutes(14)).Contains("5min 2,365,764 Luminance/hour"))
            throw new Exception("Click summary must use the same server rate as the table.");
        session.Reset(new DateTime(2026, 1, 1));
        source.Reset();
        Equal(metric.FiveMinutePerHour, null, "Reset discards the old server rate");
        source.NoteChat("[LPH] Luminance tracking started.", out _);
        Equal(metric.FiveMinutePerHour, 0, "Server restart begins a fresh five-minute block");
        source.NoteChat("[LPH] Tracking not active. Use /lph start to begin tracking.", out _);
        if (source.Ready) throw new Exception("Detect stopped server tracking.");
    }

    private static void Luminance()
    {
        var session = new IncomeSession();
        var metric = session.Register("Luminance");
        var start = new DateTime(2026, 1, 1);
        session.Reset(start);
        var conquest = new IncomeLuminance(metric);
        conquest.NoteChat("You've banked 203 Luminance.");
        conquest.NoteChat("[12:34:56] You've banked 1,234 Luminance.");
        conquest.NoteChat("You've banked 5000 Luminance.");
        Equal(metric.Total, 6437, "Conquest awards accept grouped and ungrouped amounts");
        foreach (string text in new[] { null, "", "Someone says, \"You've banked 203 Luminance.\"",
            "You've banked 20,3 Luminance.", "You've banked -203 Luminance.",
            "You've banked 999999999999999999999999 Luminance.", "You've banked 203 Coins." })
            conquest.NoteChat(text);
        Equal(metric.Total, 6437, "Reject unrelated and malformed chat");
        for (int minute = 0; minute < 5; minute++) session.SampleMinute();
        Equal(metric.FiveMinutePerHour, 6437 * 12, "Luminance shares the five-minute calculation");
        session.Reset(start);
        conquest.ResetBaseline();
        conquest.NoteChat("You've banked 203 Luminance.");
        Equal(metric.Total, 203, "Conquest reset clears prior awards");

        session.Reset(start);
        var standard = new IncomeLuminance(metric);
        standard.ObserveBalance(5_000_000_000);
        Equal(metric.Total, 0, "Initial luminance balance is not income");
        standard.ObserveBalance(5_000_000_203);
        standard.ObserveBalance(4_000_000_000); // spend
        standard.ObserveBalance(4_000_000_100);
        standard.NoteChat("You've banked 203 Luminance.");
        Equal(metric.Total, 506, "Property and bank chat gains add together; spending does not subtract income");
        for (int minute = 0; minute < 5; minute++) session.SampleMinute();
        Equal(metric.FiveMinutePerHour, 6072, "Non-Conquest luminance computes its own rolling rate from gains");
        session.Reset(start);
        standard.ResetBaseline();
        standard.ObserveBalance(4_000_000_150);
        Equal(metric.Total, 50, "First gain after reset uses the retained balance baseline");
    }

    private static void Equal(double? actual, double? expected, string message)
    {
        if (actual.HasValue != expected.HasValue || (actual.HasValue && Math.Abs(actual.Value - expected.Value) > 0.0001))
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }
}
