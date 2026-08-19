using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace OracleOfDereth
{
    public static class UpdateChecker
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/advis61/OracleOfDereth/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/advis61/OracleOfDereth/releases";
        private const string LastCheckedKey = "LastCheckedForUpdates";
        private const string DateFormat = "yyyy-MM-dd";
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(10);
        private static readonly Regex TagNameRegex = new Regex("\"tag_name\"\\s*:\\s*\"v?(\\d+(?:\\.\\d+){1,3})\"", RegexOptions.IgnoreCase);
        private static readonly Regex AssetUrlRegex = new Regex("\"browser_download_url\"\\s*:\\s*\"(https://[^\"]+\\.exe)\"", RegexOptions.IgnoreCase);

        private static DateTime? armedAt;
        private static bool ran;

        // Results produced on the background fetch thread, drained on the main thread in Tick().
        // The client (chat/log) must only ever be touched on the game's main thread — calling it
        // from the worker thread is unsafe COM access that can silently no-op or crash.
        private static volatile string pendingMessage;
        private static volatile Exception pendingException;
        private static volatile bool pendingChecked;

        public static void Arm()
        {
            if (ran) return;
            if (SettingsFile.GetSetting(LastCheckedKey, "") == DateTime.Today.ToString(DateFormat)) { ran = true; return; }
            armedAt = DateTime.UtcNow;
        }

        public static void Tick()
        {
            // Emit anything the background fetch produced — on the main thread.
            if (pendingException != null) { Util.Log(pendingException); pendingException = null; }
            if (pendingChecked)
            {
                SettingsFile.PutSetting(LastCheckedKey, DateTime.Today.ToString(DateFormat));
                pendingChecked = false;
            }
            if (pendingMessage != null) { Util.Chat(pendingMessage, Util.ColorPink, ""); pendingMessage = null; }

            if (ran || armedAt == null) return;
            if (DateTime.UtcNow - armedAt.Value < Delay) return;

            ran = true;
            Check();
        }

        public static void Check(bool verbose = false)
        {
            new Thread(() => { 
                Thread.CurrentThread.IsBackground = true;
                Run(verbose);
            }).Start();
        }

        // Runs on a background thread: does network I/O and file I/O only, and hands any
        // user-facing result to the main thread via pendingMessage/pendingException (drained in
        // Tick). Never touches the client (chat) directly — see the field comment above.
        private static void Run(bool verbose)
        {
            Version local = Assembly.GetExecutingAssembly().GetName().Version;
            Version remote = null;
            string downloadUrl = ReleasesPageUrl;

            try
            {
                string json = Fetch(ReleasesApiUrl);

                Match m = TagNameRegex.Match(json);
                if (m.Success) Version.TryParse(m.Groups[1].Value, out remote);

                Match a = AssetUrlRegex.Match(json);
                if (a.Success) downloadUrl = a.Groups[1].Value;
            }
            catch (Exception ex) { pendingException = ex; }

            if (remote == null)
            {
                if (verbose) pendingMessage = "Oracle of Dereth update check failed. See errors.txt for details.";
                return;
            }

            pendingChecked = true;

            if (remote > local)
            {
                pendingMessage = $"Oracle of Dereth v{remote} update available (you have v{local}): {downloadUrl}";
                return;
            }

            if (verbose) pendingMessage = $"Oracle of Dereth is up to date (v{local})";
        }

        private static string Fetch(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "OracleOfDereth-Plugin");
                return client.DownloadString(url);
            }
        }
    }
}
