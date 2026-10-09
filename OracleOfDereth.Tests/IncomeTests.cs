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
    }

    private static void Equal(double? actual, double? expected, string message)
    {
        if (actual.HasValue != expected.HasValue || (actual.HasValue && Math.Abs(actual.Value - expected.Value) > 0.0001))
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }
}
