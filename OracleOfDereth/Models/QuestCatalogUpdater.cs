using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace OracleOfDereth
{
    public static class QuestCatalogUpdater
    {
        private const string Url = "https://raw.githubusercontent.com/advis61/OracleOfDereth/master/OracleOfDereth/Resources/quests.csv";
        private const string LastCheckedKey = "LastCheckedQuestList";
        private const string VersionKey = "QuestListVersion";
        private const string InstalledAtKey = "QuestListInstalledAt";
        private const int TimeoutMs = 15000;
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(10);

        private static DateTime? armedAt;
        private static int running;
        private static int generation;
        private static readonly object lifecycleLock = new object();
        private static volatile bool pendingSuccess;
        private static volatile bool pendingChanged;
        private static volatile string pendingVersion;
        private static volatile string pendingMessage;
        private static volatile Exception pendingException;

        public static void Init()
        {
            if (Version().Length == 0) RecordCurrentVersion();

            if (Setting.AutoUpdateQuestList.IsYes && !CheckedToday())
                armedAt = DateTime.UtcNow;
        }

        public static void Tick()
        {
            if (pendingException != null) { Util.Log(pendingException); pendingException = null; }
            if (pendingSuccess)
            {
                pendingSuccess = false;
                SettingsFile.PutSetting(LastCheckedKey, DateTime.UtcNow.ToString("o"));
                SettingsFile.PutSetting(VersionKey, pendingVersion);
                if (pendingChanged || InstalledAt().Length == 0)
                    SettingsFile.PutSetting(InstalledAtKey, DateTime.UtcNow.ToString("o"));
                QuestCatalog.Reload();
            }
            if (pendingMessage != null) { Util.Chat(pendingMessage, Util.ColorPink, ""); pendingMessage = null; }

            if (armedAt == null || DateTime.UtcNow - armedAt.Value < Delay) return;
            armedAt = null;
            Start(false);
        }

        public static void UpdateNow()
        {
            if (!Start(true))
                Util.Chat("Oracle of Dereth is already checking the quest list.", Util.ColorPink, "");
        }

        public static string LastChecked() => SettingsFile.GetSetting(LastCheckedKey, "");
        public static string Version() => SettingsFile.GetSetting(VersionKey, "");
        public static string InstalledAt() => SettingsFile.GetSetting(InstalledAtKey, "");

        private static bool CheckedToday()
        {
            return DateTime.TryParse(LastChecked(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime checkedAt) &&
                checkedAt.ToLocalTime().Date == DateTime.Today;
        }

        public static void RecordCurrentVersion()
        {
            SettingsFile.PutSetting(VersionKey, QuestCatalog.ContentVersion());
            SettingsFile.PutSetting(InstalledAtKey, DateTime.UtcNow.ToString("o"));
        }

        internal static string ContentVersion(string content)
        {
            string normalized = (content ?? "").Replace("\r\n", "\n").TrimEnd('\n', '\r');
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                return BitConverter.ToString(hash, 0, 4).Replace("-", "");
            }
        }

        private static bool Start(bool verbose)
        {
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0) return false;
            int requestGeneration = Volatile.Read(ref generation);
            new Thread(() => Run(verbose, requestGeneration)) { IsBackground = true }.Start();
            return true;
        }

        private static void Run(bool verbose, int requestGeneration)
        {
            try
            {
                string[] downloaded = Fetch().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (!QuestCatalog.Validate(downloaded, out string error))
                    throw new InvalidDataException("Downloaded quests.csv is invalid: " + error);

                string[] current = File.Exists(QuestCatalog.FilePath)
                    ? File.ReadAllLines(QuestCatalog.FilePath)
                    : Array.Empty<string>();

                lock (lifecycleLock)
                {
                    if (requestGeneration != generation) return;

                    bool changed = !downloaded.SequenceEqual(current, StringComparer.Ordinal);
                    if (changed)
                        QuestDataFile.Write(QuestCatalog.FilePath, downloaded);

                    pendingVersion = ContentVersion(string.Join("\n", downloaded));
                    pendingChanged = changed;
                    pendingMessage = changed
                        ? "Oracle of Dereth quest list updated and reloaded."
                        : verbose ? "Oracle of Dereth quest list reloaded; it was already up to date." : null;
                    pendingSuccess = true;
                }
            }
            catch (Exception ex)
            {
                if (requestGeneration == Volatile.Read(ref generation))
                {
                    pendingException = ex;
                    if (verbose) pendingMessage = "Oracle of Dereth quest list update failed. See errors.txt for details.";
                }
            }
            finally
            {
                if (requestGeneration == Volatile.Read(ref generation))
                    Interlocked.Exchange(ref running, 0);
            }
        }

        public static void Shutdown()
        {
            lock (lifecycleLock)
            {
                Interlocked.Increment(ref generation);
                armedAt = null;
                pendingSuccess = false;
                pendingChanged = false;
                pendingVersion = null;
                pendingMessage = null;
                pendingException = null;
            }
            Interlocked.Exchange(ref running, 0);
        }

        private static string Fetch()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create(Url);
            request.UserAgent = "OracleOfDereth-Plugin";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
                return reader.ReadToEnd();
        }
    }
}
