using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OracleOfDereth;

internal static class Program
{
    private static int Main()
    {
        AssertJson(null, "null");
        AssertJson("", "\"\"");
        string quoteSlash = "quote " + (char)34 + " slash " + (char)92;
        string encodedQuoteSlash = (char)34 + "quote " + (char)92 + (char)34 + " slash " + (char)92 + (char)92 + (char)34;
        AssertJson(quoteSlash, encodedQuoteSlash);
        AssertJson("\b\f\n\r\t", "\"\\b\\f\\n\\r\\t\"");
        AssertJson("a\u0001b", "\"a\\u0001b\"");
        AssertNetworkStateReset();
        AssertSubmitCallbackCannotLeaveBusy();
        AssertSettingsRecovery();
        AssertQuestState();
        AssertMyQuestsParsing();
        AssertQuestHistory();
        Console.WriteLine("Regression tests passed.");
        return 0;
    }

    private static void AssertJson(string input, string expected)
    {
        string actual = Util.JsonString(input);
        if (actual != expected) throw new InvalidOperationException($"Expected {expected}, got {actual}");
    }

    private static void AssertNetworkStateReset()
    {
        SetStaticField(typeof(QuestSubmit), "sending", 1);
        QuestSubmit.Shutdown();
        AssertStaticInt(typeof(QuestSubmit), "sending", 0);

        SetStaticField(typeof(QuestFlagLookup), "running", 1);
        QuestFlagLookup.Shutdown();
        AssertStaticInt(typeof(QuestFlagLookup), "running", 0);
    }

    private static void AssertSubmitCallbackCannotLeaveBusy()
    {
        Type resultType = typeof(QuestSubmit).GetNestedType("SendResult", BindingFlags.NonPublic);
        object result = Activator.CreateInstance(resultType);
        resultType.GetField("Completed").SetValue(result, new Action<bool, string>((success, reason) =>
            throw new InvalidOperationException("test callback")));

        SetStaticField(typeof(QuestSubmit), "pendingResult", result);
        SetStaticField(typeof(QuestSubmit), "sending", 1);

        try { QuestSubmit.Tick(); }
        catch (InvalidOperationException ex) when (ex.Message == "test callback") { }

        AssertStaticInt(typeof(QuestSubmit), "sending", 0);
        QuestSubmit.Shutdown();
    }

    private static void AssertSettingsRecovery()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OracleOfDereth.Tests-" + Guid.NewGuid().ToString("N"));
        string settings = Path.Combine(directory, "settings.xml");
        Directory.CreateDirectory(directory);

        try
        {
            SetStaticField(typeof(SettingsFile), "_filePath", settings);
            InvokeStatic(typeof(SettingsFile), "Load");

            const string special = "quotes \" ampersand & angle < unicode \u263a";
            SettingsFile.PutSetting("Special", special);
            if (SettingsFile.GetSetting("Special", "") != special)
                throw new InvalidOperationException("Settings special-character round trip failed.");

            File.WriteAllText(settings, "<broken");
            InvokeStatic(typeof(SettingsFile), "Load");

            if (SettingsFile.GetSetting("Special", "default") != "default")
                throw new InvalidOperationException("Corrupt settings did not reset to defaults.");
            if (!File.Exists(settings) || File.ReadAllText(settings).IndexOf("<Settings", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Fresh settings file was not created.");
            if (Directory.GetFiles(directory, "settings.xml.corrupt-*").Length != 0)
                throw new InvalidOperationException("Corrupt settings file was preserved.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void AssertQuestState()
    {
        QuestCatalog.Quests.Clear();
        QuestFlag.QuestFlags.Clear();
        QuestState.Init();

        if (QuestState.RefreshStatus != QuestRefreshStatus.NotRequested)
            throw new InvalidOperationException("Quest refresh should start unrequested.");

        QuestState.BeginRefresh();
        if (!QuestState.HasRequestedRefresh || QuestState.RefreshStatus != QuestRefreshStatus.Loading)
            throw new InvalidOperationException("Quest refresh did not enter loading state.");

        var flag = new QuestFlag
        {
            Key = "testflag",
            Description = "First description",
            RepeatTime = TimeSpan.FromHours(1)
        };
        QuestFlag.QuestFlags[flag.Key] = flag;
        QuestState.FlagChanged(flag, true);

        if (QuestCatalog.Quests.Count != 1 || !QuestCatalog.Quests[0].IsNew || QuestCatalog.Quests[0].Name != "First description")
            throw new InvalidOperationException("Live quest flag was not merged into the catalog.");

        flag.Description = "Better description";
        QuestState.FlagChanged(flag, true);
        if (QuestCatalog.Quests.Count != 1 || QuestCatalog.Quests[0].Name != "Better description")
            throw new InvalidOperationException("Discovered quest metadata was not updated.");

        SetStaticField(typeof(QuestState), "lastFlagAt", DateTime.UtcNow - TimeSpan.FromSeconds(3));
        if (QuestState.RefreshStatus != QuestRefreshStatus.Loaded)
            throw new InvalidOperationException("Quest refresh did not settle after its quiet period.");
    }

    private static void AssertMyQuestsParsing()
    {
        AssertMyQuest(
            "pathwardencomplete - 1 solves (1672683021)\"Visited the Pathwarden\" 1 0",
            "pathwardencomplete", 1, 1, 0, "Visited the Pathwarden", true);
        AssertMyQuest(
            "pathwardenfound1111 - 1 solves (1672683022)\"Player talked to pathwarden greeter\" 1 0",
            "pathwardenfound1111", 1, 1, 0, "Player talked to pathwarden greeter", true);
        AssertMyQuest(
            "stipendscollectedinamonth - 2 solves (1773510196)\"Amount of stipends player has received within a 27 day period.\" 4 0",
            "stipendscollectedinamonth", 2, 4, 0,
            "Amount of stipends player has received within a 27 day period.", true);
        AssertMyQuest(
            "stipendtimer_0812 - 10 solves (1773510196)\"Amount of stipends received.\" -1 518400",
            "stipendtimer_0812", 10, -1, 518400, "Amount of stipends received.", true);
        AssertMyQuest(
            "stipendtimer_monthly - 3 solves (1764555457)\"Monthly timer for receiving up to 4 stipends.\" -1 2332800",
            "stipendtimer_monthly", 3, -1, 2332800,
            "Monthly timer for receiving up to 4 stipends.", true);

        AssertMyQuest(
            "notimestamp - 1 solves ()\"No completion timestamp\" 1 0",
            "notimestamp", 1, 1, 0, "No completion timestamp", false);
    }

    private static void AssertMyQuest(
        string line,
        string flag,
        int solves,
        int maxSolves,
        int repeatSeconds,
        string description,
        bool hasTimestamp)
    {
        QuestFlag parsed = QuestFlag.FromMyQuestsLine(line);
        if (parsed == null ||
            parsed.Key != flag ||
            parsed.Solves != solves ||
            parsed.MaxSolves != maxSolves ||
            parsed.RepeatTime != TimeSpan.FromSeconds(repeatSeconds) ||
            parsed.Description != description ||
            (parsed.CompletedOn != DateTime.MinValue) != hasTimestamp)
        {
            throw new InvalidOperationException("Failed to parse /myquests fixture: " + line);
        }
    }

    private static void AssertQuestHistory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OracleOfDereth.HistoryTests-" + Guid.NewGuid().ToString("N"));
        string history = Path.Combine(directory, "Character.csv");
        Directory.CreateDirectory(directory);

        try
        {
            SetStaticField(typeof(QuestHistory), "filePath", history);
            QuestHistory.ManualRefresh();

            string[] outOfOrder =
            {
                "---- End of Account Quests ----",
                "---- Account Quests (8) | XP Bonus: 0.2% ----",
                "AcademeyExitTokenGiven",
                "BurunTreasureMapFound",
                "CallingStoneGiven",
                "DrudgeTreasureMapFound",
                "PathwardenComplete",
                "PathwardenFound1111",
                "PrayedAtTheTempleOfMules",
                "TuskerTreasureMapFound"
            };

            if (QuestHistory.Capture("---------------------------"))
                throw new InvalidOperationException("A /myqstlist separator was parsed as a quest flag.");

            foreach (string line in outOfOrder)
            {
                if (!QuestHistory.Capture(line))
                    throw new InvalidOperationException("Did not recognize /myqstlist line: " + line);
            }

            if (QuestHistory.Count != 8 ||
                !QuestHistory.Contains("pathwardencomplete") ||
                !File.Exists(history) ||
                File.ReadAllLines(history).Length != 9)
            {
                throw new InvalidOperationException("Out-of-order /myqstlist block was not persisted.");
            }

            var historicalQuest = new Quest { Flag = "PathwardenComplete" };
            if (historicalQuest.IsComplete() || !historicalQuest.IsCompleteInQuestView())
                throw new InvalidOperationException("Account history leaked outside the Quests view.");

            var completedFilter = new QuestFilter { Completed = true };
            if (!completedFilter.Matches(historicalQuest))
                throw new InvalidOperationException("The Quests view did not include account history.");

            QuestHistory.AddSeen("SeenInMyQuests");
            if (!QuestHistory.Contains("seeninmyquests"))
                throw new InvalidOperationException("A /myquests flag was not merged into history.");

            var verifiedFilter = new QuestFilter { Verified = true };
            var newQuest = new Quest { Flag = "new", IsNew = true, Verified = true };
            if (verifiedFilter.Matches(newQuest) ||
                verifiedFilter.Matches(new Quest { Flag = "completed" }))
            {
                throw new InvalidOperationException("Verified filtering was not limited to verified catalog rows.");
            }

            var verificationColumns = new Dictionary<string, int>
            {
                { "verifiedconquest", 1 },
                { "verifiedlevistras", 2 }
            };
            int levistrasColumn = (int)typeof(QuestCatalog)
                .GetMethod("VerificationColumn", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { verificationColumns, "Levistras" });
            if (levistrasColumn != 2)
                throw new InvalidOperationException("The current server's verification column was not selected.");

            var historyOnly = new Quest { Flag = "PathwardenComplete", IsNew = true };
            if (!QuestSubmit.IsPending(historyOnly, "Levistras"))
                throw new InvalidOperationException("History-only evidence was not eligible for submission.");
            historyOnly.IsNew = false;
            historyOnly.Verified = true;
            if (QuestSubmit.IsPending(historyOnly, "Levistras"))
                throw new InvalidOperationException("Verified history remained eligible for submission.");

            QuestHistory.AddStamp("StampedAfterRefresh");
            if (!QuestHistory.Contains("stampedafterrefresh") ||
                !File.ReadAllLines(history).Any(line =>
                    string.Equals(Util.CsvParseLine(line).FirstOrDefault(), "StampedAfterRefresh", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Stamped quest was not added to account history. Contains=" +
                    QuestHistory.Contains("stampedafterrefresh") + " File=" +
                    string.Join("|", File.ReadAllLines(history)));
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void SetStaticField(Type type, string name, object value)
    {
        type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
    }

    private static void InvokeStatic(Type type, string name)
    {
        type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }

    private static void AssertStaticInt(Type type, string name, int expected)
    {
        int actual = (int)type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        if (actual != expected) throw new InvalidOperationException($"Expected {type.Name}.{name}={expected}, got {actual}");
    }
}
