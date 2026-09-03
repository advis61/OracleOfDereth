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
        AssertTradePriceBounds();
        AssertTradeSplitExpiresClosed();
        AssertSettingsRecovery();
        AssertQuestState();
        AssertMyQuestsParsing();
        AssertQuestAccountFlag();
        AssertQuestCatalogValidation();
        AssertConquestAugmentationEffects();
        Console.WriteLine("Regression tests passed.");
        return 0;
    }

    private static void AssertConquestAugmentationEffects()
    {
        AssertEffect("War", 2, "+4% war magic potency");
        AssertEffect("Void", 2, "+3% void magic potency");
        AssertEffect("Item", 10, "+10% attack/melee, +5 blood/spirit, +10 AL");
        AssertEffect("Life", 10, "+3% prot, +3% vuln, +1 regen, +1 surge rating");
        AssertEffect("Specialization", 5, "now 75");

        void AssertEffect(string name, int count, string expected)
        {
            ConquestAugmentation aug = ConquestAugmentation.Get(name);
            aug.Count = count;
            if (!aug.Effect().Contains(expected))
                throw new InvalidOperationException($"{name} effect did not contain '{expected}': {aug.Effect()}");
        }
    }

    private static void AssertQuestCatalogValidation()
    {
        MethodInfo validate = typeof(QuestCatalog).GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static);

        AssertValid(new[] { "Quest Flag,Quest Name,Repeatable,Future Column", "one,First,TRUE,anything", "two,Second,FALSE,else" }, true);
        AssertValid(new[] { "Quest Name,Repeatable", "First,TRUE" }, false);
        AssertValid(new[] { "Quest Flag,Repeatable", "one,TRUE" }, false);
        AssertValid(new[] { "Quest Flag,Quest Name", "one,First" }, false);
        AssertValid(new[] { "Quest Flag,Quest Name,Repeatable", "one,First,TRUE", "ONE,Duplicate,FALSE" }, false);
        AssertValid(new[] { "Quest Flag,Quest Name,Repeatable", ",Missing,TRUE" }, false);

        void AssertValid(string[] lines, bool expected)
        {
            object[] args = { lines, null };
            bool actual = (bool)validate.Invoke(null, args);
            if (actual != expected)
                throw new InvalidOperationException("Quest catalog validation returned " + actual + ": " + args[1]);
        }
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

    private static void AssertTradePriceBounds()
    {
        Trade.PointsPerMmd = 250;
        if (Trade.MmdsFor(251) != 2 || Trade.MmdsFor(0) != 0)
            throw new InvalidOperationException("Valid trade prices were converted incorrectly.");

        if (Trade.MmdsFor(double.NaN) != 0 ||
            Trade.MmdsFor(double.PositiveInfinity) != 0 ||
            Trade.MmdsFor(-1) != 0 ||
            Trade.MmdsFor((double)int.MaxValue * 251) != 0)
        {
            throw new InvalidOperationException("Invalid or overflowing trade prices were accepted.");
        }

        MethodInfo parse = typeof(Trade).GetMethod("ParsePoints", BindingFlags.NonPublic | BindingFlags.Static);
        object[] valid = { "1,250.5", 0d };
        if (!(bool)parse.Invoke(null, valid) || (double)valid[1] != 1250.5)
            throw new InvalidOperationException("A valid formatted trade price was rejected.");

        foreach (string malformed in new[] { "", "1.2.3", "NaN", "Infinity", "-1", "1e9" })
        {
            object[] args = { malformed, 0d };
            if ((bool)parse.Invoke(null, args))
                throw new InvalidOperationException("Malformed trade price was accepted: " + malformed);
        }
    }

    private static void AssertTradeSplitExpiresClosed()
    {
        Type requestType = typeof(Trade).GetNestedType("SplitRequest", BindingFlags.NonPublic);
        object request = Activator.CreateInstance(requestType);
        SetField(request, "Count", 5);
        SetField(request, "SourceId", 100);
        SetField(request, "SourceCount", 10);
        SetField(request, "CandidateId", 200);
        SetField(request, "Expires", DateTime.UtcNow - TimeSpan.FromSeconds(1));
        SetStaticField(typeof(Trade), "PendingSplit", request);

        Trade.Tick();

        FieldInfo pending = typeof(Trade).GetField("PendingSplit", BindingFlags.NonPublic | BindingFlags.Static);
        if (pending.GetValue(null) != null)
            throw new InvalidOperationException("Expired trade split state was not cleared.");
        if (Trade.TradeStatus.IndexOf("manually", StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException("Expired trade split did not fail closed.");
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

        if (new Quest().DisplayName() != "Unknown quest")
            throw new InvalidOperationException("Nameless quest did not receive its fallback label.");

        flag.Description = "Better description";
        QuestState.FlagChanged(flag, true);
        if (QuestCatalog.Quests.Count != 1 || QuestCatalog.Quests[0].Name != "Better description")
            throw new InvalidOperationException("Discovered quest metadata was not updated.");

        SetStaticField(typeof(QuestState), "lastFlagAt", DateTime.UtcNow - TimeSpan.FromSeconds(3));
        if (QuestState.RefreshStatus != QuestRefreshStatus.Loaded)
            throw new InvalidOperationException("Quest refresh did not settle after its quiet period.");

        QuestFlag.QuestFlags = new Dictionary<string, QuestFlag>(StringComparer.OrdinalIgnoreCase)
        {
            { "old", new QuestFlag { Key = "old" } }
        };
        var replacement = new Dictionary<string, QuestFlag>(StringComparer.OrdinalIgnoreCase)
        {
            { "new", new QuestFlag { Key = "new" } }
        };
        SetStaticField(typeof(QuestFlag), "pendingRefresh", replacement);
        QuestState.BeginRefresh();
        QuestState.RefreshFlagReceived();
        SetStaticField(typeof(QuestState), "lastFlagAt", DateTime.UtcNow - TimeSpan.FromSeconds(3));
        QuestState.Tick();
        if (!QuestFlag.QuestFlags.ContainsKey("new") || QuestFlag.QuestFlags.ContainsKey("old"))
            throw new InvalidOperationException("Successful quest refresh did not replace the live snapshot.");

        SetStaticField(typeof(QuestFlag), "pendingRefresh",
            new Dictionary<string, QuestFlag>(StringComparer.OrdinalIgnoreCase));
        QuestState.BeginRefresh();
        SetStaticField(typeof(QuestState), "requestedAt", DateTime.UtcNow - TimeSpan.FromSeconds(3));
        QuestState.Tick();
        if (!QuestFlag.QuestFlags.ContainsKey("new"))
            throw new InvalidOperationException("Empty quest refresh discarded the previous live snapshot.");
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
            "belindakilltasksstart - 1 solves (1672683021) \"Player has started Belindas Kill Tasks\" 1 0",
            "belindakilltasksstart", 1, 1, 0, "Player has started Belindas Kill Tasks", true);
        AssertMyQuest(
            "19:54:38 timestampedflag - 2 solves (1672683021)\"Plain timestamp prefix\" 3 60",
            "timestampedflag", 2, 3, 60, "Plain timestamp prefix", true);
        foreach (string prefix in new[] { "19:54 ", "7:54 PM ", "7:54:38 PM ", "[19:54:38] ", "[7:54 PM] " })
        {
            AssertMyQuest(
                prefix + "alltimestamps - 1 solves (1672683021)\"Timestamp format\" 1 0",
                "alltimestamps", 1, 1, 0, "Timestamp format", true);
        }

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

    private static void AssertQuestAccountFlag()
    {
        QuestAccountFlag.Init();
        QuestFlag.QuestFlags.Clear();
        QuestAccountFlag.ManualRefresh();

        string[] outOfOrder =
        {
            "19:54:40 ---- End of Account Quests ----",
            "19:54:38 AcademeyExitTokenGiven",
            "19:54:38 ---- Account Quests (8 unique, 12 QB) | XP Bonus: 0.2% ----",
            "19:54:38 BurunTreasureMapFound",
            "19:54:38 CallingStoneGiven",
            "19:54:38 DrudgeTreasureMapFound",
            "19:54:38 PathwardenComplete",
            "19:54:38 PathwardenFound1111",
            "19:54:38 PrayedAtTheTempleOfMules",
            "19:54:38 TuskerTreasureMapFound"
        };

        if (QuestAccountFlag.Capture("---------------------------"))
            throw new InvalidOperationException("A /myqstlist separator was parsed as a quest flag.");

        foreach (string line in outOfOrder)
        {
            if (!QuestAccountFlag.Capture(line))
                throw new InvalidOperationException("Did not recognize /myqstlist line: " + line);
        }

        if (QuestAccountFlag.Count != 8 || !QuestAccountFlag.Contains("pathwardencomplete"))
            throw new InvalidOperationException("The complete /myqstlist block was not loaded into memory.");

        QuestAccountFlag.ManualRefresh();
        QuestAccountFlag.Capture("[7:54 PM] ---- Account Quests (2) | XP Bonus: 0.1% ----");
        QuestAccountFlag.Capture("[7:54 PM] 1. ArantahKill1@GiveFigurine (Raen)");
        QuestAccountFlag.Capture("[7:54 PM] 2. BurFlagged(Permanent)");
        QuestAccountFlag.Capture("[7:54 PM] ---- End of Account Quests ----");
        if (QuestAccountFlag.Count != 2 ||
            !QuestAccountFlag.Contains("arantahkill1@givefigurine") ||
            !QuestAccountFlag.Contains("BURFLAGGED(PERMANENT)") ||
            QuestAccountFlag.Contains("pathwardencomplete"))
        {
            throw new InvalidOperationException("A completed /myqstlist refresh did not replace and normalize the account list.");
        }

        var accountQuest = new Quest { Flag = "ArantahKill1@GiveFigurine" };
        if (accountQuest.IsComplete() ||
            !accountQuest.IsCompleteInQuestView("Conquest") ||
            accountQuest.IsCompleteInQuestView("Levistras") ||
            accountQuest.StatusInQuestView() != "ready")
        {
            throw new InvalidOperationException("Server-specific completion icons affected character status data.");
        }

        QuestFlag.QuestFlags["seeninmyquests"] = new QuestFlag { Key = "seeninmyquests" };
        var characterQuest = new Quest { Flag = "seeninmyquests" };
        if (QuestAccountFlag.Contains("seeninmyquests") ||
            !QuestState.Observed("seeninmyquests") ||
            QuestState.ObservedCount != 3 ||
            characterQuest.IsCompleteInQuestView("Conquest") ||
            !characterQuest.IsCompleteInQuestView("Levistras"))
        {
            throw new InvalidOperationException("The account and character completion sources were not kept separate.");
        }

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

        var observedQuest = new Quest { Flag = "seeninmyquests", IsNew = true };
        if (!QuestSubmit.IsPending(observedQuest, "Levistras"))
            throw new InvalidOperationException("Character-list evidence was not eligible for submission.");
        observedQuest.IsNew = false;
        observedQuest.Verified = true;
        if (QuestSubmit.IsPending(observedQuest, "Levistras"))
            throw new InvalidOperationException("Verified observed evidence remained eligible for submission.");

        foreach (var fixture in new[]
        {
            new { Line = "You've stamped StampNormalFixture!", Flag = "stampnormalfixture" },
            new { Line = "You've stamped StampFirstFixture on first completion!", Flag = "stampfirstfixture" },
            new { Line = "19:54:38 You've stamped StampTimestampFixture!", Flag = "stamptimestampfixture" },
            new { Line = "[7:54 PM] You've stamped StampBracketFixture!", Flag = "stampbracketfixture" }
        })
        {
            if (!QuestFlag.Stamped(fixture.Line) ||
                !QuestFlag.QuestFlags.ContainsKey(fixture.Flag) ||
                !QuestAccountFlag.Contains(fixture.Flag) ||
                !QuestState.Observed(fixture.Flag) ||
                !QuestCatalog.Quests.Any(q => q.Flag == fixture.Flag && q.IsNew))
            {
                throw new InvalidOperationException("Failed to capture quest stamp fixture: " + fixture.Line);
            }
        }

        int beforeCooldown = QuestAccountFlag.Count;
        QuestAccountFlag.ManualRefresh();
        if (QuestAccountFlag.Capture("You can use /myqstlist again in 49s") ||
            QuestAccountFlag.Count != beforeCooldown)
        {
            throw new InvalidOperationException("A rejected /myqstlist command cleared the previous account list.");
        }

        QuestAccountFlag.ManualRefresh();
        QuestAccountFlag.Capture("---- Account Quests (2) | XP Bonus: 0.1% ----");
        QuestAccountFlag.Capture("OnlyOneRow");
        QuestAccountFlag.Capture("---- End of Account Quests ----");
        if (QuestAccountFlag.Count != 1 || !QuestAccountFlag.Contains("onlyonerow"))
            throw new InvalidOperationException("An incomplete /myqstlist response was not reflected in the live account list.");

    }

    private static void SetStaticField(Type type, string name, object value)
    {
        type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).SetValue(target, value);
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
