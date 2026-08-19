using System;
using System.IO;
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
