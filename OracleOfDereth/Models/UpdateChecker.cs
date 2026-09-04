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
        private const int TimeoutMs = 15000;
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(10);
        private static readonly Regex TagNameRegex = new Regex("\"tag_name\"\\s*:\\s*\"v?(\\d+(?:\\.\\d+){1,3})\"", RegexOptions.IgnoreCase);
        private static readonly Regex AssetUrlRegex = new Regex("\"browser_download_url\"\\s*:\\s*\"(https://[^\"]+\\.exe)\"", RegexOptions.IgnoreCase);

        private static DateTime? armedAt;
        private static bool ran;
        private static int running;
        private static int generation;

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
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0) return;
            int requestGeneration = Volatile.Read(ref generation);
            new Thread(() => Run(verbose, requestGeneration)) { IsBackground = true }.Start();
        }

        // Runs on a background thread: does network I/O and file I/O only, and hands any
        // user-facing result to the main thread via pendingMessage/pendingException (drained in
        // Tick). Never touches the client (chat) directly — see the field comment above.
        private static void Run(bool verbose, int requestGeneration)
        {
            Version local = Assembly.GetExecutingAssembly().GetName().Version;
            Version remote = null;
            string downloadUrl = ReleasesPageUrl;
            Exception exception = null;

            try
            {
                string json = Fetch(ReleasesApiUrl);

                Match m = TagNameRegex.Match(json);
                if (m.Success) Version.TryParse(m.Groups[1].Value, out remote);

                Match a = AssetUrlRegex.Match(json);
                if (a.Success) downloadUrl = a.Groups[1].Value;
            }
            catch (Exception ex) { exception = ex; }

            if (requestGeneration == Volatile.Read(ref generation))
            {
                pendingException = exception;
                if (remote == null)
                {
                    if (verbose) pendingMessage = "Oracle of Dereth update check failed. See errors.txt for details.";
                }
                else
                {
                    pendingChecked = true;
                    if (remote > local)
                        pendingMessage = $"Oracle of Dereth v{remote} update available (you have v{local}): {downloadUrl}";
                    else if (verbose)
                        pendingMessage = $"Oracle of Dereth is up to date (v{local})";
                }
            }

            if (requestGeneration == Volatile.Read(ref generation))
                Interlocked.Exchange(ref running, 0);
        }

        public static void Shutdown()
        {
            Interlocked.Increment(ref generation);
            armedAt = null;
            ran = false;
            pendingMessage = null;
            pendingException = null;
            pendingChecked = false;
            Interlocked.Exchange(ref running, 0);
        }

        private static string Fetch(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "OracleOfDereth-Plugin";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            using (var response = request.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
