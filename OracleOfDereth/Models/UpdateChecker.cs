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
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(10);
        private static readonly Regex TagNameRegex = new Regex("\"tag_name\"\\s*:\\s*\"v?(\\d+(?:\\.\\d+){1,3})\"", RegexOptions.IgnoreCase);
        private static readonly Regex AssetUrlRegex = new Regex("\"browser_download_url\"\\s*:\\s*\"(https://[^\"]+\\.exe)\"", RegexOptions.IgnoreCase);

        private static DateTime? armedAt;
        private static bool ran;

        public static void Arm()
        {
            if (ran) return;
            armedAt = DateTime.UtcNow;
        }

        public static void Tick()
        {
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
            catch (Exception ex) { Util.Log(ex); }

            if (remote == null)
            {
                if (verbose) Util.Chat("Update check failed. See log.txt for details.", Util.ColorPink);
                return;
            }

            if (remote > local)
            {
                Util.Chat($"Oracle of Dereth v{remote} update available (you have v{local}): {downloadUrl}", Util.ColorPink, "");
                return;
            }

            if (verbose) Util.Chat($"Oracle of Dereth is up to date (v{local}).", Util.ColorPink, "");
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
