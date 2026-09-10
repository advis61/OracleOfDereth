using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using OracleOfDereth;

internal static class SettingsFileTests
{
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "OracleSettings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "settings.xml");
        try
        {
            File.WriteAllText(path, "<Settings><Existing>keep</Existing><Shared>old</Shared></Settings>");
            using (var one = Start(path, "One"))
            using (var two = Start(path, "Two"))
            {
                try
                {
                    var timer = Stopwatch.StartNew();
                    while (!File.Exists(path + ".One") || !File.Exists(path + ".Two"))
                    {
                        if (timer.ElapsedMilliseconds > 10000) throw new Exception("Settings clients did not initialize.");
                        Thread.Sleep(10);
                    }
                    // Both clients now hold the original snapshot; change another key before releasing them.
                    File.WriteAllText(path, "<Settings><Existing>keep</Existing><Shared>new</Shared></Settings>");
                    File.WriteAllText(path + ".go", "");
                    if (!one.WaitForExit(15000) || !two.WaitForExit(15000) || one.ExitCode != 0 || two.ExitCode != 0)
                        throw new Exception("Settings writer failed.");
                    var result = new XmlDocument();
                    result.Load(path);
                    if (result.SelectSingleNode("/Settings/Existing")?.InnerText != "keep" ||
                        result.SelectSingleNode("/Settings/Shared")?.InnerText != "new")
                        throw new Exception("A stale client overwrote another setting.");
                    for (int i = 0; i < 20; i++)
                        foreach (string client in new[] { "One", "Two" })
                            if (result.SelectSingleNode("/Settings/" + client + i)?.InnerText != "value & <" + i)
                                throw new Exception("Concurrent settings update was lost.");
                }
                finally
                {
                    foreach (var process in new[] { one, two })
                        if (!process.HasExited) { process.Kill(); process.WaitForExit(); }
                }
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    public static int Write(string path, string prefix)
    {
        typeof(SettingsFile).GetField("_filePath", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, path);
        typeof(SettingsFile).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        File.WriteAllText(path + "." + prefix, "");
        var timer = Stopwatch.StartNew();
        while (!File.Exists(path + ".go"))
        {
            if (timer.ElapsedMilliseconds > 10000) return 1;
            Thread.Sleep(10);
        }
        for (int i = 0; i < 20; i++) SettingsFile.PutSetting(prefix + i, "value & <" + i);
        return 0;
    }

    private static Process Start(string path, string prefix) => Process.Start(new ProcessStartInfo
    {
        FileName = Assembly.GetExecutingAssembly().Location,
        Arguments = "--write-settings \"" + path + "\" " + prefix,
        UseShellExecute = false, CreateNoWindow = true
    });
}
